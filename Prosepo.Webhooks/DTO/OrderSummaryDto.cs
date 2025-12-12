using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.DTO
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
