using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents the invoice/billing information for an order.
    /// Contains the complete billing address and tax identification details for invoice generation.
    /// </summary>
    /// <remarks>
    /// This DTO captures all information required to generate a legally compliant invoice.
    /// It may differ from the delivery recipient address, especially for business orders
    /// where goods are delivered to one location but billed to another (e.g., headquarters).
    /// 
    /// <para>
    /// Key use cases:
    /// <list type="bullet">
    /// <item><description>Generating VAT invoices for business customers</description></item>
    /// <item><description>Tax reporting and compliance</description></item>
    /// <item><description>Financial record-keeping and accounting</description></item>
    /// <item><description>B2B transactions requiring specific billing details</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderInvoiceDto
    {
        /// <summary>
        /// Gets or sets the tax identification number (NIP) for the invoice recipient.
        /// </summary>
        /// <value>
        /// The NIP (Numer Identyfikacji Podatkowej) or equivalent tax ID in other countries.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Required for B2B transactions and VAT invoices in Poland.
        /// The NIP format should be validated according to Polish tax regulations (10 digits).
        /// 
        /// <para>
        /// <strong>Note:</strong> For B2C transactions or when the customer doesn't require
        /// an invoice, this field may be empty.
        /// </para>
        /// </remarks>
        [JsonPropertyName("invoiceNIP")]
        public string InvoiceNIP { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the person or entity to be invoiced.
        /// </summary>
        /// <value>
        /// The full legal name as it should appear on the invoice (e.g., company name or person's name).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// For business customers, this should be the registered company name.
        /// For individual customers, this should match the person's legal name.
        /// This field is crucial for tax and accounting purposes.
        /// </remarks>
        [JsonPropertyName("invoiceName")]
        public string InvoiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the street name for the billing address.
        /// </summary>
        /// <value>
        /// The street name without building number (e.g., "ul. Marsza³kowska").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Should match the official registered address for business entities.
        /// Used in combination with <see cref="InvoiceBuildingNo"/> and optionally
        /// <see cref="InvoiceFlatNo"/> for the complete street address.
        /// </remarks>
        [JsonPropertyName("invoiceStreet")]
        public string InvoiceStreet { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company name for the invoice recipient (if applicable).
        /// </summary>
        /// <value>
        /// The legal company name for business invoices.
        /// Returns <see cref="string.Empty"/> if not applicable (individual customer) or not provided.
        /// </value>
        /// <remarks>
        /// For B2B transactions, this should contain the full legal entity name.
        /// May be the same as <see cref="InvoiceName"/> for business customers,
        /// or empty for individual consumers.
        /// </remarks>
        [JsonPropertyName("invoiceCompany")]
        public string InvoiceCompany { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city name for the billing address.
        /// </summary>
        /// <value>
        /// The city or town name (e.g., "Warszawa", "Poznañ").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Should match the official registered city for business entities.
        /// Required for invoice generation and tax reporting.
        /// </remarks>
        [JsonPropertyName("invoiceCity")]
        public string InvoiceCity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code for the billing address.
        /// </summary>
        /// <value>
        /// The postal code in the country-specific format (e.g., "00-001" for Poland).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Should match the official registered postal code for business entities.
        /// The format should be validated according to <see cref="InvoiceCountryCode"/> requirements.
        /// </remarks>
        [JsonPropertyName("invoicePostalCode")]
        public string InvoicePostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the building number for the billing address.
        /// </summary>
        /// <value>
        /// The building number (e.g., "123", "45A").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Part of the complete street address. May include letters or ranges (e.g., "12A", "15/17").
        /// Should match the official registered address for business entities.
        /// </remarks>
        [JsonPropertyName("invoiceBuildingNo")]
        public string InvoiceBuildingNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the apartment/flat/unit number for the billing address.
        /// </summary>
        /// <value>
        /// The apartment, flat, suite, or unit number (e.g., "5", "12B").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Used when the billing address is within a multi-unit building.
        /// Should match the official registered address for business entities.
        /// </remarks>
        [JsonPropertyName("invoiceFlatNo")]
        public string InvoiceFlatNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country code for the billing address.
        /// </summary>
        /// <value>
        /// The ISO 3166-1 alpha-2 country code (e.g., "PL" for Poland, "DE" for Germany).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Critical for determining:
        /// <list type="bullet">
        /// <item><description>VAT rules and rates (domestic vs. EU vs. international)</description></item>
        /// <item><description>Invoice format and legal requirements</description></item>
        /// <item><description>Tax reporting obligations (e.g., VIES for EU transactions)</description></item>
        /// <item><description>Currency and language preferences for the invoice</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("invoiceCountryCode")]
        public string InvoiceCountryCode { get; set; } = string.Empty;
    }
}
