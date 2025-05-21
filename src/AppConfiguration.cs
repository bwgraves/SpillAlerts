namespace SpillAlerts
{
    public class AppConfiguration
    {
        public required string OverflowDataEndpoint { get; set; }
        public required string NotificationEmails { get; set; }

        public required string FromEmail { get; set; }
        public int SmtpPort { get; set; }
        public required string SmtpUser { get; set; }
        public required string SmtpPassword { get; set; }
        public required string SmtpHost { get; set; }
    }
}
