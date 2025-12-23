using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// DTO dla ¿¹dania przes³ania rezultatu realizacji zamówienia
    /// </summary>
    public class UploadOrderRealizationRequest
    {
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<OrderRealizationItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO dla pozycji zrealizowanego zamówienia
    /// </summary>
    public class OrderRealizationItemDto
    {
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }

        [JsonPropertyName("seriesNumber")]
        public string? SeriesNumber { get; set; }
    }

    /// <summary>
    /// DTO dla odpowiedzi przes³ania rezultatu realizacji zamówienia
    /// </summary>
    public class UploadOrderRealizationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("itemsProcessed")]
        public int ItemsProcessed { get; set; }
    }
}
