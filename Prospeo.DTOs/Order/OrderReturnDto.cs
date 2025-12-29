using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents a request to process a return of ordered products.
    /// Contains all information needed to register returned items back into inventory.
    /// </summary>
    /// <remarks>
    /// This DTO facilitates the returns management process by capturing detailed information
    /// about returned products, including quantities, batch information, and destination warehouse.
    /// 
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item><description>Customer returns (damaged, unwanted, or wrong items)</description></item>
    /// <item><description>Rejected deliveries (customer refused acceptance)</description></item>
    /// <item><description>Quality issues identified after delivery</description></item>
    /// <item><description>Warranty returns</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Important:</strong> For pharmaceutical and regulated products, accurate tracking
    /// of batch numbers and expiration dates is critical for compliance and safety.
    /// </para>
    /// </remarks>
    public class OrderReturnDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier where the original order was placed.
        /// </summary>
        /// <value>
        /// The marketplace name or code (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Links the return to the original marketplace, which is important for:
        /// <list type="bullet">
        /// <item><description>Tracking return rates by marketplace</description></item>
        /// <item><description>Processing marketplace-specific return policies</description></item>
        /// <item><description>Initiating refunds through the correct payment system</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reference number of the original order being returned.
        /// </summary>
        /// <value>
        /// The original order number or reference (e.g., "ORD/2024/01/0001").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This reference links the return to the original order, enabling the system to:
        /// <list type="bullet">
        /// <item><description>Validate that returned items match the order</description></item>
        /// <item><description>Verify return eligibility (within return window, etc.)</description></item>
        /// <item><description>Process refunds to the correct payment method</description></item>
        /// <item><description>Update order status to reflect the return</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional note or comment about the return.
        /// </summary>
        /// <value>
        /// Additional information about the return reason, condition, or handling instructions.
        /// Returns <c>null</c> if not provided.
        /// </value>
        /// <remarks>
        /// This field can capture important contextual information such as:
        /// <list type="bullet">
        /// <item><description>Return reason (damaged, wrong item, changed mind, etc.)</description></item>
        /// <item><description>Product condition observations</description></item>
        /// <item><description>Customer comments or complaints</description></item>
        /// <item><description>Quality control notes</description></item>
        /// <item><description>Special handling requirements</description></item>
        /// </list>
        /// 
        /// <para>
        /// This information is valuable for quality improvement, supplier feedback,
        /// and customer service analysis.
        /// </para>
        /// </remarks>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the list of items being returned.
        /// </summary>
        /// <value>
        /// A collection of <see cref="OrderReturnItemDto"/> objects, each representing
        /// a specific product and quantity being returned.
        /// </value>
        /// <remarks>
        /// Each item in the list contains detailed information about what is being returned,
        /// including SKU, quantity, batch numbers, and expiration dates where applicable.
        /// 
        /// <para>
        /// <strong>Note:</strong> The returned quantities may be partial (less than originally ordered)
        /// if the customer is only returning some items from the order.
        /// </para>
        /// </remarks>
        [JsonPropertyName("items")]
        public List<OrderReturnItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Represents a single item within an order return.
    /// Contains detailed product identification, quantity, and traceability information.
    /// </summary>
    /// <remarks>
    /// This DTO captures all necessary information to properly receive returned goods back into inventory,
    /// including critical traceability data for pharmaceuticals and regulated products.
    /// 
    /// <para>
    /// The information in this DTO enables:
    /// <list type="bullet">
    /// <item><description>Accurate inventory adjustments</description></item>
    /// <item><description>Product traceability and quality control</description></item>
    /// <item><description>Compliance with pharmaceutical regulations</description></item>
    /// <item><description>Proper warehouse placement of returned goods</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderReturnItemDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the Stock Keeping Unit (SKU) of the returned product.
        /// </summary>
        /// <value>
        /// The unique product identifier matching the inventory system.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Must match an existing SKU in the product master data.
        /// Used to identify which product is being returned and update the correct inventory record.
        /// </remarks>
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity being returned.
        /// </summary>
        /// <value>
        /// The number of units being returned. Supports decimal values for fractional quantities.
        /// </value>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Must be positive (zero or negative values should be rejected)</description></item>
        /// <item><description>Should not exceed the originally ordered quantity</description></item>
        /// <item><description>Decimal values support fractional units for weight/volume-based products</description></item>
        /// <item><description>The system may validate against the original order to prevent return fraud</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the returned product batch.
        /// </summary>
        /// <value>
        /// The expiration date in ISO 8601 format (YYYY-MM-DD).
        /// Returns <c>null</c> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Critical for pharmaceutical and food products. This information:
        /// <list type="bullet">
        /// <item><description>Enables proper FEFO (First Expired, First Out) inventory management</description></item>
        /// <item><description>Helps determine if returned products can be resold</description></item>
        /// <item><description>Supports product recall processes</description></item>
        /// <item><description>Ensures compliance with pharmaceutical regulations</description></item>
        /// <item><description>May trigger quarantine for products near expiration</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Regulatory Note:</strong> For pharmaceuticals, tracking expiration dates
        /// on returns is often a legal requirement.
        /// </para>
        /// </remarks>
        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the series/batch/lot number of the returned product.
        /// </summary>
        /// <value>
        /// The manufacturer's batch or lot number of the returned product.
        /// Returns <c>null</c> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// Batch number tracking is essential for:
        /// <list type="bullet">
        /// <item><description>Product traceability throughout the supply chain</description></item>
        /// <item><description>Quality issue investigation (identifying defective batches)</description></item>
        /// <item><description>Targeted product recalls</description></item>
        /// <item><description>Pharmaceutical regulatory compliance</description></item>
        /// <item><description>Supplier quality management and claims</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Important:</strong> For pharmaceuticals and medical devices, batch tracking
        /// is typically mandatory and must be recorded accurately.
        /// </para>
        /// </remarks>
        [JsonPropertyName("seriesNumber")]
        public string? SeriesNumber { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier where the returned goods should be received.
        /// </summary>
        /// <value>
        /// The numeric identifier of the destination warehouse for the returned products.
        /// </value>
        /// <remarks>
        /// Specifies which warehouse location should receive and process the returned goods.
        /// This determines:
        /// <list type="bullet">
        /// <item><description>Where the returned items will be physically stored</description></item>
        /// <item><description>Which inventory location will be credited with the return</description></item>
        /// <item><description>Routing for returned goods inspection and processing</description></item>
        /// <item><description>Regional availability of returned (restockable) items</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> Returns may go to a different warehouse than the original shipment,
        /// especially for damaged or defective items requiring inspection or quarantine.
        /// </para>
        /// </remarks>
        [JsonPropertyName("warehouse")]
        public int Warehouse { get; set; }
    }
}
