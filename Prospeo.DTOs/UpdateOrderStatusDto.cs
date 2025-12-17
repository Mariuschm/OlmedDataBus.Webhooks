using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// DTO dla ¿¹dania aktualizacji statusu zamówienia
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("orderStatus")]
        public int OrderStatus { get; set; }
    }

    /// <summary>
    /// DTO dla odpowiedzi aktualizacji statusu zamówienia
    /// </summary>
    public class UpdateOrderStatusResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("newStatus")]
        public int NewStatus { get; set; }
    }
}
