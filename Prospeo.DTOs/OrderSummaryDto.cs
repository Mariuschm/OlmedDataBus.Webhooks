using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents order summary information, typically used for financial totals or additional charges.
    /// This is a flexible DTO that can capture various types of order-level summaries.
    /// </summary>
    /// <remarks>
    /// This DTO is designed to be flexible and can represent different types of order summaries
    /// such as shipping costs, discounts, taxes, or other order-level charges.
    /// 
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item><description>Shipping and handling fees</description></item>
    /// <item><description>Order-level discounts or promotions</description></item>
    /// <item><description>Additional charges (e.g., COD fees, packaging)</description></item>
    /// <item><description>Tax summaries</description></item>
    /// <item><description>Payment processing fees</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Note:</strong> All properties are nullable to accommodate different summary types
    /// that may not require all fields.
    /// </para>
    /// </remarks>
    public class OrderSummaryDto
    {
        /// <summary>
        /// Gets or sets the type of the order summary entry.
        /// </summary>
        /// <value>
        /// A string describing the category or type of this summary item
        /// (e.g., "SHIPPING", "DISCOUNT", "TAX", "COD_FEE", "HANDLING").
        /// Returns <c>null</c> if not specified.
        /// </value>
        /// <remarks>
        /// This field categorizes the summary entry and helps systems understand
        /// how to process or display the information. Common types include:
        /// <list type="bullet">
        /// <item><description>"SHIPPING" - Shipping and delivery costs</description></item>
        /// <item><description>"DISCOUNT" - Promotional discounts or vouchers</description></item>
        /// <item><description>"TAX" - Tax calculations</description></item>
        /// <item><description>"COD_FEE" - Cash on Delivery fees</description></item>
        /// <item><description>"HANDLING" - Handling or packaging fees</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the monetary value associated with this summary entry.
        /// </summary>
        /// <value>
        /// The amount in the order's currency. May be positive (charges) or negative (discounts).
        /// Returns <c>null</c> if not applicable or not specified.
        /// </value>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Positive values represent charges or fees</description></item>
        /// <item><description>Negative values represent discounts or credits</description></item>
        /// <item><description>Zero values may indicate free shipping or waived fees</description></item>
        /// </list>
        /// 
        /// <para>
        /// The currency is typically determined by the order's marketplace or region
        /// and should be consistent with other monetary values in the order.
        /// </para>
        /// </remarks>
        [JsonPropertyName("value")]
        public decimal? Value { get; set; }

        /// <summary>
        /// Gets or sets a human-readable description of this summary entry.
        /// </summary>
        /// <value>
        /// A text description providing additional context about this summary item
        /// (e.g., "Standard Shipping", "20% Promotional Discount", "VAT 23%").
        /// Returns <c>null</c> if not specified.
        /// </value>
        /// <remarks>
        /// This description can be displayed to users in order summaries, invoices, or receipts.
        /// It should be clear and informative, helping customers understand the charge or discount.
        /// 
        /// <para>
        /// Examples:
        /// <list type="bullet">
        /// <item><description>"Express Delivery - Next Day"</description></item>
        /// <item><description>"Discount Code: SUMMER2024"</description></item>
        /// <item><description>"Cash on Delivery Fee"</description></item>
        /// <item><description>"Gift Wrapping"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
