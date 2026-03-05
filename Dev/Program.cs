namespace OmiliaWebhook.Dev;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Contains("send"))
            await SendTestEvents.Run(args);
        else
            await Server.Run(args);
    }
}
