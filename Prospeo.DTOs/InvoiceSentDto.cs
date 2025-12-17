using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// DTO dla ¿¹dania wys³ania faktury
    /// </summary>
    public class InvoiceSentRequest
    {
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("sentDate")]
        public DateTime SentDate { get; set; }

        [JsonPropertyName("recipientEmail")]
        public string? RecipientEmail { get; set; }

        [JsonPropertyName("additionalData")]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// DTO dla odpowiedzi wys³ania faktury
    /// </summary>
    public class InvoiceSentResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
