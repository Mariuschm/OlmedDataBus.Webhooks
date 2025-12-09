using System.Text.Json.Serialization;
namespace Prosepo.Webhooks.DTO
{

    public class OrderRecipientDto
    {
        [JsonPropertyName("recipientName")]
        public string RecipientName { get; set; } = string.Empty;

        [JsonPropertyName("recipientStreet")]
        public string RecipientStreet { get; set; } = string.Empty;

        [JsonPropertyName("recipientCompany")]
        public string RecipientCompany { get; set; } = string.Empty;

        [JsonPropertyName("recipientCity")]
        public string RecipientCity { get; set; } = string.Empty;

        [JsonPropertyName("recipientPostalCode")]
        public string RecipientPostalCode { get; set; } = string.Empty;

        [JsonPropertyName("recipientBuildingNo")]
        public string RecipientBuildingNo { get; set; } = string.Empty;

        [JsonPropertyName("recipientFlatNo")]
        public string RecipientFlatNo { get; set; } = string.Empty;

        [JsonPropertyName("recipientCountryCode")]
        public string RecipientCountryCode { get; set; } = string.Empty;

        [JsonPropertyName("recipientPhone")]
        public string RecipientPhone { get; set; } = string.Empty;

        [JsonPropertyName("recipientEmail")]
        public string RecipientEmail { get; set; } = string.Empty;
    }

}
