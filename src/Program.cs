namespace SpillAlerts
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddHttpClient();
            builder.Services.Configure<AppConfiguration>(opts =>
            {
                builder.Configuration.GetSection("AppConfiguration").Bind(opts);

                // Digital Ocean doesn't like nested env var overrides, so do this!
                opts.SmtpPassword = builder.Configuration["SMTP_PASSWORD"] ?? string.Empty;
                opts.NotificationEmails = builder.Configuration["NOTIFICATION_EMAILS"] ?? string.Empty;
            });


            Console.WriteLine($"SMTP_PASSWORD: {builder.Configuration["SMTP_PASSWORD"]}");

            var host = builder.Build();
            host.Run();
        }
    }
}