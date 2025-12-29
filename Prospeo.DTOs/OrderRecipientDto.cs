using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents the delivery recipient information for an order.
    /// Contains the complete delivery address and contact details for the person or entity receiving the shipment.
    /// </summary>
    /// <remarks>
    /// This DTO captures all necessary information for successfully delivering an order to the recipient.
    /// It includes physical address components, contact information, and is used by courier services
    /// and warehouse systems to ensure proper delivery.
    /// 
    /// <para>
    /// The recipient may differ from the buyer or invoice recipient, especially in cases of:
    /// <list type="bullet">
    /// <item><description>Gift deliveries</description></item>
    /// <item><description>Business orders delivered to a different location</description></item>
    /// <item><description>Drop shipping scenarios</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderRecipientDto
    {
        /// <summary>
        /// Gets or sets the full name of the delivery recipient.
        /// </summary>
        /// <value>
        /// The complete name of the person receiving the delivery (e.g., "Jan Kowalski").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should be the full name as it should appear on the shipping label.
        /// Required for courier services to identify the recipient.
        /// </remarks>
        [JsonPropertyName("recipientName")]
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the street name for the delivery address.
        /// </summary>
        /// <value>
        /// The street name without building number (e.g., "ul. Kwiatowa").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Street name is separated from building number to allow for better address parsing
        /// and validation. Combine with <see cref="RecipientBuildingNo"/> and optionally
        /// <see cref="RecipientFlatNo"/> for the complete street address.
        /// </remarks>
        [JsonPropertyName("recipientStreet")]
        public string RecipientStreet { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company name for the delivery recipient (if applicable).
        /// </summary>
        /// <value>
        /// The company or organization name at the delivery location.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Used for business deliveries or when shipping to an organization.
        /// This appears on shipping labels and helps couriers locate the correct business premises.
        /// </remarks>
        [JsonPropertyName("recipientCompany")]
        public string RecipientCompany { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city name for the delivery address.
        /// </summary>
        /// <value>
        /// The city or town name (e.g., "Warszawa", "Kraków").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        [JsonPropertyName("recipientCity")]
        public string RecipientCity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code for the delivery address.
        /// </summary>
        /// <value>
        /// The postal code in the country-specific format (e.g., "00-001" for Poland).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The postal code format should match the requirements of <see cref="RecipientCountryCode"/>.
        /// Used for routing deliveries and calculating shipping costs.
        /// </remarks>
        [JsonPropertyName("recipientPostalCode")]
        public string RecipientPostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the building number for the delivery address.
        /// </summary>
        /// <value>
        /// The building number (e.g., "123", "45A").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Combines with <see cref="RecipientStreet"/> to form the complete street address.
        /// May include letters or other identifiers (e.g., "12A", "15/17").
        /// </remarks>
        [JsonPropertyName("recipientBuildingNo")]
        public string RecipientBuildingNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the apartment/flat number for the delivery address.
        /// </summary>
        /// <value>
        /// The apartment, flat, or unit number (e.g., "5", "12B").
        /// Returns <see cref="string.Empty"/> if not applicable (e.g., house delivery) or not provided.
        /// </value>
        /// <remarks>
        /// Required for apartment deliveries. Helps couriers locate the exact unit
        /// within multi-unit buildings.
        /// </remarks>
        [JsonPropertyName("recipientFlatNo")]
        public string RecipientFlatNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country code for the delivery address.
        /// </summary>
        /// <value>
        /// The ISO 3166-1 alpha-2 country code (e.g., "PL" for Poland, "DE" for Germany).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Used to determine:
        /// <list type="bullet">
        /// <item><description>International vs. domestic shipping</description></item>
        /// <item><description>Customs requirements</description></item>
        /// <item><description>Shipping costs and available courier services</description></item>
        /// <item><description>Address validation rules</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("recipientCountryCode")]
        public string RecipientCountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient's phone number.
        /// </summary>
        /// <value>
        /// The phone number in international or local format (e.g., "+48 123 456 789", "123456789").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Essential for courier services to:
        /// <list type="bullet">
        /// <item><description>Contact the recipient for delivery coordination</description></item>
        /// <item><description>Notify about delivery attempts</description></item>
        /// <item><description>Resolve delivery issues</description></item>
        /// <item><description>Send SMS notifications about shipment status</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("recipientPhone")]
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient's email address.
        /// </summary>
        /// <value>
        /// The email address for delivery notifications and communication.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Used for:
        /// <list type="bullet">
        /// <item><description>Sending tracking information and shipment notifications</description></item>
        /// <item><description>Delivery confirmation emails</description></item>
        /// <item><description>Communication about delivery issues</description></item>
        /// <item><description>Electronic proof of delivery receipts</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("recipientEmail")]
        public string RecipientEmail { get; set; } = string.Empty;
    }
}
