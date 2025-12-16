using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Reprezentuje dane kupuj¹cego w zamówieniu
    /// </summary>
    public class OrderBuyerDto
    {
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; } = string.Empty;

        [JsonPropertyName("buyerStreet")]
        public string BuyerStreet { get; set; } = string.Empty;

        [JsonPropertyName("buyerCompany")]
        public string BuyerCompany { get; set; } = string.Empty;

        [JsonPropertyName("buyerCity")]
        public string BuyerCity { get; set; } = string.Empty;

        [JsonPropertyName("buyerPostalCode")]
        public string BuyerPostalCode { get; set; } = string.Empty;

        [JsonPropertyName("buyerBuildingNo")]
        public string BuyerBuildingNo { get; set; } = string.Empty;

        [JsonPropertyName("buyerFlatNo")]
        public string BuyerFlatNo { get; set; } = string.Empty;

        [JsonPropertyName("buyerCountryCode")]
        public string BuyerCountryCode { get; set; } = string.Empty;

        [JsonPropertyName("buyerPhone")]
        public string BuyerPhone { get; set; } = string.Empty;

        [JsonPropertyName("buyerEmail")]
        public string BuyerEmail { get; set; } = string.Empty;
    }
}
