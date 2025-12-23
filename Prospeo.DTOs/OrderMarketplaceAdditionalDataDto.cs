using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Reprezentuje dodatkowe dane marketplace w zamówieniu
    /// </summary>
    public class OrderMarketplaceAdditionalDataDto
    {
        [JsonPropertyName("allegroOrderId")]
        [SpecialProperty("Atrybut")]
        public string AllegroOrderId { get; set; } = string.Empty;

        [JsonPropertyName("paymentProvider")]
        [SpecialProperty("Atrybut")]
        public string PaymentProvider { get; set; } = string.Empty;

        [JsonPropertyName("paymentId")]
        [SpecialProperty("Atrybut")]
        public string PaymentId { get; set; } = string.Empty;
    }
}
