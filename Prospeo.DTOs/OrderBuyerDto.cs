using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents the buyer information for an order.
    /// Contains complete contact and address details of the person or entity making the purchase.
    /// </summary>
    /// <remarks>
    /// The buyer is the person or entity who places and pays for the order.
    /// This may differ from the delivery recipient (e.g., when sending gifts) or the invoice recipient
    /// (e.g., in corporate purchase scenarios where a central office pays but goods go to branch locations).
    /// 
    /// <para>
    /// This information is essential for:
    /// <list type="bullet">
    /// <item><description>Customer communication and support</description></item>
    /// <item><description>Order confirmations and notifications</description></item>
    /// <item><description>Payment verification and processing</description></item>
    /// <item><description>Marketing and customer relationship management</description></item>
    /// <item><description>Returns and refunds processing</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderBuyerDto
    {
        /// <summary>
        /// Gets or sets the full name of the buyer.
        /// </summary>
        /// <value>
        /// The complete name of the person making the purchase (e.g., "Anna Nowak").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should be the name associated with the payment method and used for
        /// order confirmations and customer communications.
        /// </remarks>
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the street name for the buyer's address.
        /// </summary>
        /// <value>
        /// The street name without building number (e.g., "ul. D≥uga").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Combined with <see cref="BuyerBuildingNo"/> and optionally <see cref="BuyerFlatNo"/>
        /// to form the complete street address. Used for billing verification and customer records.
        /// </remarks>
        [JsonPropertyName("buyerStreet")]
        public string BuyerStreet { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company name for the buyer (if applicable).
        /// </summary>
        /// <value>
        /// The company or organization name if the purchase is made by a business.
        /// Returns <see cref="string.Empty"/> if not applicable (individual purchase) or not provided.
        /// </value>
        /// <remarks>
        /// For B2B transactions, this contains the company name. For B2C transactions,
        /// this may be empty. Used for:
        /// <list type="bullet">
        /// <item><description>B2B customer segmentation</description></item>
        /// <item><description>Corporate account management</description></item>
        /// <item><description>Business verification and credit checks</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("buyerCompany")]
        public string BuyerCompany { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city name for the buyer's address.
        /// </summary>
        /// <value>
        /// The city or town name (e.g., "£Ûdü", "Katowice").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Used for address verification, geographical analysis, and customer segmentation.
        /// </remarks>
        [JsonPropertyName("buyerCity")]
        public string BuyerCity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code for the buyer's address.
        /// </summary>
        /// <value>
        /// The postal code in the country-specific format (e.g., "90-001" for Poland).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Used for address validation and geographical segmentation.
        /// The format should match the requirements of <see cref="BuyerCountryCode"/>.
        /// </remarks>
        [JsonPropertyName("buyerPostalCode")]
        public string BuyerPostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the building number for the buyer's address.
        /// </summary>
        /// <value>
        /// The building number (e.g., "67", "23A").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Part of the complete street address. May include letters or ranges (e.g., "12A", "15/17").
        /// </remarks>
        [JsonPropertyName("buyerBuildingNo")]
        public string BuyerBuildingNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the apartment/flat number for the buyer's address.
        /// </summary>
        /// <value>
        /// The apartment, flat, or unit number (e.g., "8", "3C").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Used when the buyer's address is within a multi-unit building.
        /// </remarks>
        [JsonPropertyName("buyerFlatNo")]
        public string BuyerFlatNo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country code for the buyer's address.
        /// </summary>
        /// <value>
        /// The ISO 3166-1 alpha-2 country code (e.g., "PL" for Poland, "DE" for Germany).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Used for:
        /// <list type="bullet">
        /// <item><description>International order processing</description></item>
        /// <item><description>Payment method availability</description></item>
        /// <item><description>Currency selection</description></item>
        /// <item><description>Customer segmentation and regional analysis</description></item>
        /// <item><description>Compliance with regional regulations</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("buyerCountryCode")]
        public string BuyerCountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the buyer's phone number.
        /// </summary>
        /// <value>
        /// The phone number in international or local format (e.g., "+48 987 654 321").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Critical for:
        /// <list type="bullet">
        /// <item><description>Order confirmation calls</description></item>
        /// <item><description>Customer service and support</description></item>
        /// <item><description>Payment verification (especially for COD)</description></item>
        /// <item><description>Fraud prevention and order validation</description></item>
        /// <item><description>SMS notifications about order status</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("buyerPhone")]
        public string BuyerPhone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the buyer's email address.
        /// </summary>
        /// <value>
        /// The email address for order communications.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Essential for:
        /// <list type="bullet">
        /// <item><description>Order confirmations and receipts</description></item>
        /// <item><description>Shipping notifications and tracking links</description></item>
        /// <item><description>Digital invoices and tax documents</description></item>
        /// <item><description>Customer service communication</description></item>
        /// <item><description>Marketing communications (with consent)</description></item>
        /// <item><description>Password resets and account management</description></item>
        /// </list>
        /// 
        /// <para>
        /// This is typically the primary communication channel with the customer
        /// throughout the order lifecycle.
        /// </para>
        /// </remarks>
        [JsonPropertyName("buyerEmail")]
        public string BuyerEmail { get; set; } = string.Empty;
    }
}
