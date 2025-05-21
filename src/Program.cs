namespace SpillAlerts
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddHttpClient();
            builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection("AppConfiguration"));

            var host = builder.Build();
            host.Run();
        }
    }
}