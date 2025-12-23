using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// DTO dla ¿¹dania aktualizacji statusu zamówienia
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("trackingNumber")]
        public string? TrackingNumber { get; set; }
    }


    public enum OrderStatus
    {
        Infolinia=-1,
        Oczekuje_na_przyjecie =0,
        Przyjeto_do_realizacji =1,
        Gotowe_do_wysylki = 5,
        Anulowano = 8,
        Przekazane_do_kuriera = 9,
        Zgloszono_braki = 100
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

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; } = string.Empty;
    }
}
