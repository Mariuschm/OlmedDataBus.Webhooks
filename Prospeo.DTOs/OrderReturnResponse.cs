using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Model odpowiedzi na zwrot zamówienia
    /// </summary>
    public class OrderReturnResponse
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
        /// Numer referencyjny zamówienia
        /// </summary>
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa marketplace
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Liczba zwróconych pozycji
        /// </summary>
        [JsonPropertyName("returnedItemsCount")]
        public int ReturnedItemsCount { get; set; }

        /// <summary>
        /// Lista zwróconych SKU
        /// </summary>
        [JsonPropertyName("returnedSkus")]
        public List<string> ReturnedSkus { get; set; } = new();

        /// <summary>
        /// Data przetworzenia
        /// </summary>
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
