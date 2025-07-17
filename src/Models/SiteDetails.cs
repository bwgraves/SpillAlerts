namespace SpillAlerts.Models
{
    public record SiteDetails(string? Name, string? StormDischargeAssetType)
    {
        public string? Name { get; set; } = Name;
        public string? StormDischargeAssetType { get; set; } = StormDischargeAssetType;
    }
}
