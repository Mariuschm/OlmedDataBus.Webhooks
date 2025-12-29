using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents a single line item within an order.
    /// Contains product information, pricing, and quantity details for an ordered item.
    /// </summary>
    /// <remarks>
    /// This DTO captures all essential information about a product being ordered,
    /// including its identification, quantity, pricing, and tax information.
    /// It is typically used as a collection within the <see cref="OrderDto"/> class.
    /// 
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item><description>Links to source document and article through IDs and SKU</description></item>
    /// <item><description>Supports decimal quantities for fractional unit orders</description></item>
    /// <item><description>Includes VAT/tax rate information for financial calculations</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderItemDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the unique identifier for this order item.
        /// </summary>
        /// <value>
        /// A unique integer identifier for the order line item within the system.
        /// </value>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the source issue document identifier.
        /// </summary>
        /// <value>
        /// The ID of the source document from which this order item originates.
        /// This could reference a warehouse issue document, picking list, or similar source.
        /// </value>
        /// <remarks>
        /// This property is used for traceability and linking order items back to
        /// their originating documents in the warehouse management system.
        /// </remarks>
        [JsonPropertyName("sourceIssueDocumentId")]
        public int SourceIssueDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the Stock Keeping Unit (SKU) of the article from the source system.
        /// </summary>
        /// <value>
        /// The unique product identifier (SKU) used to identify the article in the inventory system.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The SKU is the primary identifier for linking this order item to the correct
        /// product in the inventory management system. It must match the SKU in the product master data.
        /// </remarks>
        [JsonPropertyName("sourceArticleSKU")]
        public string SourceArticleSKU { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity ordered.
        /// </summary>
        /// <value>
        /// The number of units ordered for this item. Supports decimal values for fractional quantities.
        /// </value>
        /// <remarks>
        /// Decimal quantities allow for products sold in fractional units (e.g., 2.5 kg, 1.75 meters).
        /// The unit of measure should align with the product's base unit in the inventory system.
        /// </remarks>
        [JsonPropertyName("quantityOrdered")]
        public decimal QuantityOrdered { get; set; }

        /// <summary>
        /// Gets or sets the unit price of the item.
        /// </summary>
        /// <value>
        /// The price per unit of the ordered item. This is typically the net price before tax.
        /// </value>
        /// <remarks>
        /// The price should be in the currency agreed upon with the marketplace or customer.
        /// This value combined with <see cref="QuantityOrdered"/> determines the line item subtotal.
        /// </remarks>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the VAT (Value Added Tax) rate applicable to this item.
        /// </summary>
        /// <value>
        /// The VAT rate as a string (e.g., "23%", "8%", "0%", "ZW" for exempt).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The VAT rate is used for tax calculations and invoice generation.
        /// Common Polish VAT rates include: "23%", "8%", "5%", "0%", and "ZW" (zwolniony - exempt).
        /// </remarks>
        [JsonPropertyName("vatRate")]
        public string VatRate { get; set; } = string.Empty;
    }
}
