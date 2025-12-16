using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Reprezentuje podsumowanie zamówienia
    /// </summary>
    public class OrderSummaryDto
    {
        // Based on the JSON example, this is an empty array
        // Add properties here when the structure is known
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public decimal? Value { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
