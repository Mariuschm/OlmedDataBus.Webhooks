using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.DTO
{
    /// <summary>
    /// Reprezentuje dodatkowe dane marketplace w zamówieniu
    /// </summary>
    public class OrderMarketplaceAdditionalDataDto
    {
        [JsonPropertyName("allegroOrderId")]
        public string AllegroOrderId { get; set; } = string.Empty;

        [JsonPropertyName("paymentProvider")]
        public string PaymentProvider { get; set; } = string.Empty;

        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; } = string.Empty;
    }
}
