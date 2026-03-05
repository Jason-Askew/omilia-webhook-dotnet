using Amazon.CDK;

namespace OmiliaWebhook.Infra;

public class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        var webhookSecret = (string?)app.Node.TryGetContext("webhookSecret")
            ?? System.Environment.GetEnvironmentVariable("OMILIA_WEBHOOK_SECRET");

        new WebhookStack(app, "OmiliaWebhookStack", new WebhookStackProps
        {
            WebhookSecret = webhookSecret,
        });

        app.Synth();
    }
}
