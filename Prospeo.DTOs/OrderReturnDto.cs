using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Model danych zwrotu zamówienia
    /// </summary>
    public class OrderReturnDto
    {
        /// <summary>
        /// Nazwa marketplace (np. "APTEKA_OLMED")
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Numer referencyjny zamówienia (np. "ORD/2024/01/0001")
        /// </summary>
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Notatka do zwrotu
        /// </summary>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Lista pozycji zwracanego zamówienia
        /// </summary>
        [JsonPropertyName("items")]
        public List<OrderReturnItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Model danych pojedynczej pozycji zwrotu
    /// </summary>
    public class OrderReturnItemDto
    {
        /// <summary>
        /// SKU produktu
        /// </summary>
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Iloœæ zwracana
        /// </summary>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Data wa¿noœci produktu (opcjonalna, format: "YYYY-MM-DD")
        /// </summary>
        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Numer serii produktu (opcjonalny)
        /// </summary>
        [JsonPropertyName("seriesNumber")]
        public string? SeriesNumber { get; set; }

        /// <summary>
        /// Identyfikator magazynu
        /// </summary>
        [JsonPropertyName("warehouse")]
        public int Warehouse { get; set; }
    }
}
