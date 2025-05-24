using Microsoft.Extensions.Options;
using SpillAlerts.Models;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Text;

namespace SpillAlerts
{
    /// <summary>
    /// Background worker that checks for new sewage spills and sends email notifications.
    /// </summary>
    public class Worker(
        ILogger<Worker> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<AppConfiguration> appConfig) : BackgroundService
    {
        private HashSet<string> PreviousSpills { get; set; } = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Avon Sewage Alerts (alerts@bernielabs.com)");
            var firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                var response = await client.GetAsync(appConfig.Value.OverflowDataEndpoint);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OverFlowActivityResponse>(json) ?? new OverFlowActivityResponse();

                var activeSpills = result.Features
                    .Where(s => s.Properties.LatestEventStart != null &&
                                s.Properties.LatestEventEnd == null &&
                                s.Properties.ReceivingWaterCourse!.ToLower().EndsWith("river avon"))
                    .ToList();

                var newSpills = activeSpills
                    .Where(s => !PreviousSpills.Contains(s.Properties.Id!))
                    .ToList();

                var locationNameTasks = new List<Task<string>>();

                foreach (var spill in newSpills)
                {
                    var lon = spill.Geometry.Coordinates![1];
                    var lat = spill.Geometry.Coordinates![0];

                    locationNameTasks.Add(GetLocationName(client, lon, lat));
                    PreviousSpills.Add(spill.Properties.Id!);
                }

                if (firstRun)
                {
                    firstRun = false;
                    continue;
                }

                var locationNames = await Task.WhenAll(locationNameTasks);

                if (locationNames.Length > 0)
                {
                    SendEmail(locationNames);
                }

                // Remove spills that are no longer active
                var activeSpillIds = activeSpills
                    .Select(s => s.Properties.Id!)
                    .ToHashSet();
                PreviousSpills.RemoveWhere(id => !activeSpillIds.Contains(id));

                // Check every 20 minutes
                await Task.Delay(1200000, stoppingToken);
            }
        }

        /// <summary>
        /// Send an email to the configured addresses with the new sewage spill locations.
        /// </summary>
        private void SendEmail(string[] locations)
        {
            var emails = appConfig.Value.NotificationEmails
                .Split(',')
                .Select(email => email.Trim())
                .ToList();

            var body = new StringBuilder();

            body.AppendLine("<p>Hi,</p>");
            body.AppendLine("<p>We've detected some new sewage spills into the Warwickshire Avon under Seven Trent in the areas below:</p>");
            body.AppendLine("<ul>");
            locations.ToList().ForEach(location =>
            {
                body.AppendLine($"<li>{location}</li>");
            });
            body.AppendLine("</ul>");
            body.AppendLine("<p>The details of these and future spills can be monitored further at <a href=\"https://sewagemap.co.uk\">https://sewagemap.co.uk</a>.");
            body.AppendLine("If you think anything looks incorrect, please reply to this email with any details.</p>");
            body.AppendLine("<p>Kind Regards,<br />");
            body.AppendLine("ARAG Sewage Alerts</p>");
            body.AppendLine("<p>P.S. If you wish to opt-out of these alerts, please reply with \"optout\" and you'll be taken off the list.</p>");

            var message = new MailMessage
            {
                From = new MailAddress(appConfig.Value.FromEmail, "ARAG Sewage Alerts"),
                Subject = "New Sewage Spills Found",
                Body = body.ToString(),
                IsBodyHtml = true,
            };

            foreach (var email in emails)
            {
                message.Bcc.Add(new MailAddress(email));
            }

            using var smtp = new SmtpClient(appConfig.Value.SmtpHost, appConfig.Value.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(appConfig.Value.SmtpUser, appConfig.Value.SmtpPassword)
            };

            try
            {
                logger.LogInformation("Sending notification email");
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                var locationsInfo = string.Join(',', locations);
                logger.LogError(ex, "Failed to send notification email. Locations where: {Locations}", locationsInfo);
            }
        }

        /// <summary>
        /// Reverse geocode the latitude and longitude to get a human-readable location name.
        /// </summary>
        private static async Task<string> GetLocationName(HttpClient client, double lat, double lon)
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat}&lon={lon}&format=json";
                var response = await client.GetStringAsync(url);
                var result = JsonDocument.Parse(response);

                return result.RootElement
                    .GetProperty("display_name")
                    .GetString()!;
            }
    }
}
