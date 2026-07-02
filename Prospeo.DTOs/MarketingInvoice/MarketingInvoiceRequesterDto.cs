using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.MarketingInvoice
{
    /// <summary>
    /// Represents the requester details for a marketing invoice.
    /// </summary>
    public class MarketingInvoiceRequesterDto : DTOModelBase
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("addressStreet")]
        public string AddressStreet { get; set; } = string.Empty;

        [JsonPropertyName("addressZipCode")]
        public string AddressZipCode { get; set; } = string.Empty;

        [JsonPropertyName("addressCity")]
        public string AddressCity { get; set; } = string.Empty;

        [JsonPropertyName("nip")]
        public string Nip { get; set; } = string.Empty;
    }
}
