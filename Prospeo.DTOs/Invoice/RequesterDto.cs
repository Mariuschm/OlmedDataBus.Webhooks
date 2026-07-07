using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Represents the requester (contractor/counterparty) information for a marketing invoice.
    /// Contains detailed address and identification information about the entity requesting the invoice.
    /// </summary>
    /// <remarks>
    /// This DTO contains the contractor (counterparty) details that are used for invoice generation
    /// and billing purposes. All address fields are required for proper invoicing.
    /// </remarks>
    public class RequesterDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the name of the requester (contractor/counterparty).
        /// </summary>
        /// <value>
        /// The full legal name of the company or individual requesting the invoice.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the street address of the requester.
        /// </summary>
        /// <value>
        /// The street name and number of the requester's address.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("addressStreet")]
        public string AddressStreet { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code of the requester's address.
        /// </summary>
        /// <value>
        /// The ZIP code or postal code for the requester's location.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("addressZipCode")]
        public string AddressZipCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city of the requester's address.
        /// </summary>
        /// <value>
        /// The city name where the requester is located.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("addressCity")]
        public string AddressCity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the NIP (National Identification Number) of the requester.
        /// </summary>
        /// <value>
        /// The Polish tax identification number (NIP - Numer Identyfikacji Podatkowej).
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("nip")]
        public string Nip { get; set; } = string.Empty;
    }
}
