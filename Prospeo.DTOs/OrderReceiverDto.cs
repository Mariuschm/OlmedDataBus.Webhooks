using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents the goods receiver information for an order.
    /// This may differ from both the delivery recipient and invoice recipient in certain business scenarios.
    /// </summary>
    /// <remarks>
    /// The receiver is the legal entity or person who takes ownership of the goods upon delivery.
    /// In many cases, this will be the same as the delivery recipient, but can differ in scenarios such as:
    /// 
    /// <para>
    /// <list type="bullet">
    /// <item><description>Drop shipping where goods are delivered to end customer but received by a distributor</description></item>
    /// <item><description>Consignment deliveries</description></item>
    /// <item><description>Third-party logistics arrangements</description></item>
    /// <item><description>Complex B2B transactions with multiple parties</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// This information is particularly important for customs documentation, legal ownership tracking,
    /// and compliance with trade regulations.
    /// </para>
    /// </remarks>
    public class OrderReceiverDto
    {
        /// <summary>
        /// Gets or sets the tax identification number (NIP) of the receiver.
        /// </summary>
        /// <value>
        /// The NIP (Numer Identyfikacji Podatkowej) or equivalent tax ID.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Required for:
        /// <list type="bullet">
        /// <item><description>B2B transactions for tax purposes</description></item>
        /// <item><description>Customs declarations for international shipments</description></item>
        /// <item><description>VAT compliance and reporting</description></item>
        /// <item><description>Regulatory requirements in various jurisdictions</description></item>
        /// </list>
        /// 
        /// <para>
        /// The NIP format should be validated according to the country's tax regulations.
        /// For Poland, this is a 10-digit number.
        /// </para>
        /// </remarks>
        [JsonPropertyName("receiverNIP")]
        public string ReceiverNIP { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full name of the receiver.
        /// </summary>
        /// <value>
        /// The legal name of the person or entity receiving the goods.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// For business receivers, this should be the registered company name.
        /// For individual receivers, this should be the person's full legal name.
        /// 
        /// <para>
        /// This name is used in:
        /// <list type="bullet">
        /// <item><description>Customs documentation</description></item>
        /// <item><description>Proof of delivery documents</description></item>
        /// <item><description>Legal ownership records</description></item>
        /// <item><description>Compliance and audit trails</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("receiverName")]
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the street name for the receiver's address.
        /// </summary>
        /// <value>
        /// The street name without building number (e.g., "ul. Polna").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Should represent the legal registered address of the receiving entity.
        /// Used in combination with <see cref="ReceiverBuildingNo"/> and optionally
        /// <see cref="ReceiverFlatNo"/> for the complete street address.
        /// </remarks>
        [JsonPropertyName("receiverStreet")]
        public string ReceiverStreet { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city name for the receiver's address.
        /// </summary>
        /// <value>
        /// The city or town name (e.g., "Gdañsk", "Wroc³aw").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Should match the official registered city for business entities.
        /// Used in legal documents, customs declarations, and compliance reporting.
        /// </remarks>
        [JsonPropertyName("receiverCity")]
        public string ReceiverCity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code for the receiver's address.
        /// </summary>
        /// <value>
        /// The postal code in the country-specific format (e.g., "80-001" for Poland).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The postal code format should match the requirements of <see cref="ReceiverCountryCode"/>.
        /// Used for customs declarations and address validation.
        /// </remarks>
        [JsonPropertyName("receiverPostalCode")]
        public string ReceiverPostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the building number for the receiver's address.
        /// </summary>
        /// <value>
        /// The building number (e.g., "88", "12B").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Combines with <see cref="ReceiverStreet"/> to form the complete street address.
        /// May include letters or ranges (e.g., "12A", "15/17").
        /// </remarks>
        [JsonPropertyName("receiverBuildingNo")]
        public string ReceiverBuildingNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the apartment/flat/unit number for the receiver's address.
        /// </summary>
        /// <value>
        /// The apartment, flat, suite, or unit number (e.g., "10", "5B").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Used when the receiver's registered address is within a multi-unit building.
        /// Important for ensuring accurate legal documentation.
        /// </remarks>
        [JsonPropertyName("receiverFlatNo")]
        public string ReceiverFlatNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country code for the receiver's address.
        /// </summary>
        /// <value>
        /// The ISO 3166-1 alpha-2 country code (e.g., "PL" for Poland, "DE" for Germany).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Critical for determining:
        /// <list type="bullet">
        /// <item><description>Customs requirements and declarations</description></item>
        /// <item><description>International trade compliance</description></item>
        /// <item><description>VAT and tax treatment (intra-EU vs. third country)</description></item>
        /// <item><description>Export control regulations</description></item>
        /// <item><description>Shipping documentation requirements</description></item>
        /// </list>
        /// 
        /// <para>
        /// This is particularly important for cross-border transactions where customs
        /// declarations and tax treatment depend on the receiver's country.
        /// </para>
        /// </remarks>
        [JsonPropertyName("receiverCountryCode")]
        public string ReceiverCountryCode { get; set; } = string.Empty;
    }
}
