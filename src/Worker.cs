using Microsoft.Extensions.Options;
using SpillAlerts.Models;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using HandlebarsDotNet;

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
        private readonly HashSet<string> receivingWaterCourses =
        [
            "river avon",
            "badsey brook",
            "ban brook",
            "bell brook",
            "bow brook",
            "broadway brook",
            "canley brook",
            "carrant brook",
            "cattle brook",
            "fishers brook",
            "hammock brook",
            "harvington brook",
            "kemerton brook",
            "lenchwick stream",
            "littleton brook",
            "mill avon",
            "piddle brook",
            "pingle brook",
            "princethorpe brook",
            "river alne",
            "river arrow",
            "river dene",
            "river isbourne",
            "river itchen",
            "river leam",
            "river sowe",
            "rush brook",
            "seeley brook",
            "sherbourne brook",
            "shottery brook",
            "whitson brook",
            "river swift"
        ];

        private Dictionary<string, SpillMemory> PreviousSpills { get; set; } = [];

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
                                receivingWaterCourses.Any(w =>
                                    s.Properties.ReceivingWaterCourse.ToLower().EndsWith(w.ToLower())))
                    .ToList();

                logger.LogInformation("active spills: {ActiveSpills}", JsonSerializer.Serialize(activeSpills));

                var newSpills = activeSpills
                    .Where(s =>
                        !PreviousSpills.TryGetValue(s.Properties.Id!, out var memory) ||
                        memory.StatusStart != s.Properties.StatusStart)
                    .ToList();

                // Save all instances of a spill which started at a certain time, so we don't alert again
                // for what is probably the same spill.
                foreach (var spill in activeSpills)
                {
                    PreviousSpills[spill.Properties.Id!] = new SpillMemory
                    {
                        StatusStart = spill.Properties.StatusStart,
                        LastSeen = DateTimeOffset.UtcNow
                    };
                }

                var locations = new List<LocationDto>();

                foreach (var spill in newSpills)
                {
                    logger.LogInformation("New spill! ID: {Id}", spill.Id);

                    var lon = spill.Geometry.Coordinates![0];
                    var lat = spill.Geometry.Coordinates![1];

                    var details = SiteHelper.GetDetails(spill.Properties.Id!);
                    locations.Add(new LocationDto
                    {
                        Name = details?.Name ?? "Unknown Site",
                        Code = spill.Properties.Id!,
                        StartTime = DateTimeOffset.FromUnixTimeMilliseconds(spill.Properties.StatusStart).UtcDateTime,
                        Latitude = lat,
                        Longitude = lon,
                        StationType = details.StormDischargeAssetType,
                        ReceivingWaterCourse = spill.Properties.ReceivingWaterCourse
                    });
                }

                if (firstRun)
                {
                    logger.LogInformation("Ignoring any active spills on first run");
                    firstRun = false;
                    continue;
                }

                foreach(var location in locations)
                {
                    SendEmail(location);
                }

                // Clearup any saved spills that haven't been seen in the last 12 hours
                var expiry = DateTimeOffset.UtcNow.AddHours(-12);
                PreviousSpills = PreviousSpills
                    .Where(kvp => kvp.Value.LastSeen > expiry)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                logger.LogInformation("Spills in memory: {Spills}", JsonSerializer.Serialize(PreviousSpills));

                // Check every 5 minutes
                await Task.Delay(300000, stoppingToken);
            }
        }

        /// <summary>
        /// Send an email to the configured addresses with the new sewage spill locations.
        /// </summary>
        private void SendEmail(LocationDto location)
        {
            var emails = appConfig.Value.NotificationEmails
                .Split(',')
                .Select(email => email.Trim())
                .ToList();

            var templateSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EmailTemplate.handlebars"));
            var template = Handlebars.Compile(templateSource);

            var data = new Dictionary<string, string> 
            {
                { "location", location.Name },
                { "mapUrl", location.MapUrl },
                { "locationCode", location.Code },
                { "startTime", location.StartTime.ToString("dd/MM/yyyy 'at' HH:mm") },
                { "stationType", location.StationType! },
                { "lat", location.Latitude.ToString() },
                { "long", location.Longitude.ToString() },
                { "receivingWaterCourse", location.ReceivingWaterCourse! },
            };

            var body = template(data);

            var message = new MailMessage
            {
                From = new MailAddress(appConfig.Value.FromEmail, "ARAG Sewage Alerts"),
                Subject = "New Sewage Spills Found",
                Body = body,
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
                logger.LogError(ex, "Failed to send notification email. Location: {Location}", location.Name);
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

        private class LocationDto
        {
            public required string Name { get; set; }
            public required string Code { get; set; }
            public string MapUrl => $"https://www.sewagemap.co.uk/?asset_id={Code}&company=Severn%20Trent%20Water";
            public DateTime StartTime { get; set; }
            public string? StationType { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? ReceivingWaterCourse { get; set; }
        }

        private class SpillMemory
        {
            public long StatusStart { get; set; }
            public DateTimeOffset LastSeen { get; set; }
        }
    }
}
