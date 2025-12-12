using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.DTO
{
    /// <summary>
    /// Reprezentuje dane odbiorcy w zamówieniu
    /// </summary>
    public class OrderReceiverDto
    {
        [JsonPropertyName("receiverNIP")]
        public string ReceiverNIP { get; set; } = string.Empty;

        [JsonPropertyName("receiverName")]
        public string ReceiverName { get; set; } = string.Empty;

        [JsonPropertyName("receiverStreet")]
        public string ReceiverStreet { get; set; } = string.Empty;

        [JsonPropertyName("receiverCity")]
        public string ReceiverCity { get; set; } = string.Empty;

        [JsonPropertyName("receiverPostalCode")]
        public string ReceiverPostalCode { get; set; } = string.Empty;

        [JsonPropertyName("receiverBuildingNo")]
        public string ReceiverBuildingNo { get; set; } = string.Empty;

        [JsonPropertyName("receiverFlatNo")]
        public string ReceiverFlatNo { get; set; } = string.Empty;

        [JsonPropertyName("receiverCountryCode")]
        public string ReceiverCountryCode { get; set; } = string.Empty;
    }
}
