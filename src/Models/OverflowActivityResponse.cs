using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace SpillAlerts.Models
{
    public class OverFlowActivityResponse
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public RootProperties? Properties { get; set; }

        [JsonPropertyName("features")]
        public List<Feature> Features { get; set; } = [];
    }

    public class RootProperties
    {
        [JsonPropertyName("exceededTransferLimit")]
        public bool ExceededTransferLimit { get; set; }
    }

    public class Feature
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; } = new Geometry();

        [JsonPropertyName("properties")]
        public FeatureProperties Properties { get; set; } = new FeatureProperties();
    }

    public class Geometry
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("coordinates")]
        public List<double>? Coordinates { get; set; }
    }

    public class FeatureProperties
    {
        [JsonPropertyName("OBJECTID")]
        public long ObjectId { get; set; }

        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("Company")]
        public string? Company { get; set; }

        [JsonPropertyName("Status")]
        public int Status { get; set; }

        [JsonPropertyName("StatusStart")]
        public long StatusStart { get; set; }

        [JsonPropertyName("LatestEventStart")]
        public long? LatestEventStart { get; set; }

        [JsonPropertyName("LatestEventEnd")]
        public long? LatestEventEnd { get; set; }

        [JsonPropertyName("Latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("Longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("ReceivingWaterCourse")]
        public string? ReceivingWaterCourse { get; set; }

        [JsonPropertyName("LastUpdated")]
        public long LastUpdated { get; set; }
    }
}


