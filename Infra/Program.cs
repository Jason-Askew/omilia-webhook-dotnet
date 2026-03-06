using Amazon.CDK;

namespace OmiliaWebhook.Infra;

public class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        var webhookSecret = (string?)app.Node.TryGetContext("webhookSecret")
            ?? System.Environment.GetEnvironmentVariable("OMILIA_WEBHOOK_SECRET");
        var jwksUri = (string?)app.Node.TryGetContext("jwksUri")
            ?? System.Environment.GetEnvironmentVariable("JWKS_URI");
        var jwtIssuer = (string?)app.Node.TryGetContext("jwtIssuer")
            ?? System.Environment.GetEnvironmentVariable("JWT_ISSUER");
        var jwtAudience = (string?)app.Node.TryGetContext("jwtAudience")
            ?? System.Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        new WebhookStack(app, "OmiliaWebhookStack", new WebhookStackProps
        {
            WebhookSecret = webhookSecret,
            JwksUri = jwksUri,
            JwtIssuer = jwtIssuer,
            JwtAudience = jwtAudience,
        });

        app.Synth();
    }
}
