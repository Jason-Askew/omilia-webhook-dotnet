using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.Kinesis;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace OmiliaWebhook.Infra;

public class WebhookStackProps : StackProps
{
    public string? WebhookSecret { get; set; }
}

public class WebhookStack : Stack
{
    public WebhookStack(Construct scope, string id, WebhookStackProps props) : base(scope, id, props)
    {
        // ── Kinesis Data Stream ──────────────────────────────
        var stream = new Amazon.CDK.AWS.Kinesis.Stream(this, "CdrStream", new StreamProps
        {
            StreamName = "omilia-cdr-events",
            ShardCount = 2,
            RetentionPeriod = Duration.Hours(48),
            Encryption = StreamEncryption.MANAGED,
        });

        // ── Lambda Function ──────────────────────────────────
        var environment = new Dictionary<string, string>
        {
            ["KINESIS_STREAM_NAME"] = stream.StreamName,
        };
        if (props.WebhookSecret is not null)
            environment["WEBHOOK_SECRET"] = props.WebhookSecret;

        var fn = new Function(this, "WebhookHandler", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Architecture = Architecture.ARM_64,
            Handler = "OmiliaWebhook::OmiliaWebhook.Handlers.WebhookHandler::Handler",
            Code = Code.FromAsset("../src/bin/Release/net8.0/publish"),
            MemorySize = 256,
            Timeout = Duration.Seconds(15),
            Tracing = Tracing.ACTIVE,
            LogRetention = RetentionDays.TWO_WEEKS,
            Environment = environment,
        });

        stream.GrantWrite(fn);

        // ── HTTP API ─────────────────────────────────────────
        var api = new CfnApi(this, "WebhookApi", new CfnApiProps
        {
            Name = "omilia-webhook",
            ProtocolType = "HTTP",
        });

        var integration = new CfnIntegration(this, "LambdaIntegration", new CfnIntegrationProps
        {
            ApiId = api.Ref,
            IntegrationType = "AWS_PROXY",
            IntegrationUri = fn.FunctionArn,
            PayloadFormatVersion = "2.0",
        });

        new CfnRoute(this, "WebhookRoute", new CfnRouteProps
        {
            ApiId = api.Ref,
            RouteKey = "POST /webhook",
            Target = $"integrations/{integration.Ref}",
        });

        new CfnRoute(this, "HealthRoute", new CfnRouteProps
        {
            ApiId = api.Ref,
            RouteKey = "GET /health",
            Target = $"integrations/{integration.Ref}",
        });

        var stage = new CfnStage(this, "DefaultStage", new CfnStageProps
        {
            ApiId = api.Ref,
            StageName = "$default",
            AutoDeploy = true,
        });

        // Allow API Gateway to invoke Lambda
        fn.AddPermission("ApiGwInvoke", new Permission
        {
            Principal = new Amazon.CDK.AWS.IAM.ServicePrincipal("apigateway.amazonaws.com"),
            SourceArn = $"arn:aws:execute-api:{this.Region}:{this.Account}:{api.Ref}/*",
        });

        // ── Outputs ──────────────────────────────────────────
        var apiUrl = $"https://{api.Ref}.execute-api.{this.Region}.amazonaws.com/";
        new CfnOutput(this, "WebhookUrl", new CfnOutputProps { Value = $"{apiUrl}webhook" });
        new CfnOutput(this, "HealthUrl", new CfnOutputProps { Value = $"{apiUrl}health" });
        new CfnOutput(this, "KinesisStreamName", new CfnOutputProps { Value = stream.StreamName });
        new CfnOutput(this, "KinesisStreamArn", new CfnOutputProps { Value = stream.StreamArn });
    }
}
