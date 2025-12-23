using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Model odpowiedzi na aktualizacjê stanu magazynowego
    /// </summary>
    public class StockUpdateResponse
    {
        /// <summary>
        /// Czy operacja zakoñczy³a siê sukcesem
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Komunikat zwrotny
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa marketplace
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Liczba zaktualizowanych SKU
        /// </summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Lista zaktualizowanych SKU
        /// </summary>
        [JsonPropertyName("updatedSkus")]
        public List<string> UpdatedSkus { get; set; } = new();

        /// <summary>
        /// Data przetworzenia
        /// </summary>
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
