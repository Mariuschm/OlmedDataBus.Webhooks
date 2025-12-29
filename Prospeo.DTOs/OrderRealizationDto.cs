using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents a request to upload order realization/fulfillment results to the system.
    /// Used when an order has been fulfilled and the actual quantities and details need to be reported.
    /// </summary>
    /// <remarks>
    /// This DTO is used by warehouse or fulfillment systems to report back the actual items
    /// that were picked, packed, and shipped for an order. This may differ from the original
    /// order if substitutions or partial fulfillment occurred.
    /// 
    /// <para>
    /// Typical workflow:
    /// <list type="number">
    /// <item><description>Order is received and processed</description></item>
    /// <item><description>Warehouse fulfills the order (picks and packs items)</description></item>
    /// <item><description>System sends this request with actual quantities and batch information</description></item>
    /// <item><description>Order status is updated based on fulfillment results</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UploadOrderRealizationRequest
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// The name or code of the marketplace (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        /// <value>
        /// The unique order number or reference identifier for the order being fulfilled.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should match the order number from the original order data.
        /// It's used to locate and update the correct order in the system.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of items that were actually fulfilled/realized in this order.
        /// </summary>
        /// <value>
        /// A collection of <see cref="OrderRealizationItemDto"/> objects representing each line item
        /// that was picked and packed for this order.
        /// </value>
        /// <remarks>
        /// Each item in this list represents an actual product that was included in the shipment,
        /// with specific batch, expiration date, and serial number information where applicable.
        /// </remarks>
        [JsonPropertyName("items")]
        public List<OrderRealizationItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Represents a single item within an order realization/fulfillment.
    /// Contains the actual product details including batch, expiration, and serial number information.
    /// </summary>
    /// <remarks>
    /// This DTO captures critical traceability information required for:
    /// <list type="bullet">
    /// <item><description>Pharmaceutical products (expiration dates, batch numbers)</description></item>
    /// <item><description>Serialized items (serial numbers for tracking)</description></item>
    /// <item><description>Inventory management (actual quantities shipped)</description></item>
    /// <item><description>Regulatory compliance and product recalls</description></item>
    /// </list>
    /// </remarks>
    public class OrderRealizationItemDto
    {
        /// <summary>
        /// Gets or sets the Stock Keeping Unit (SKU) of the fulfilled product.
        /// </summary>
        /// <value>
        /// The unique product identifier (SKU) for the item that was actually shipped.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The SKU must match the product master data and may differ from the ordered SKU
        /// if substitutions were made during fulfillment.
        /// </remarks>
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actual quantity fulfilled/shipped.
        /// </summary>
        /// <value>
        /// The number of units actually included in the shipment. Supports decimal values.
        /// </value>
        /// <remarks>
        /// This quantity may differ from the ordered quantity in cases of:
        /// <list type="bullet">
        /// <item><description>Partial fulfillment due to stock shortages</description></item>
        /// <item><description>Over-shipment to account for unit packaging</description></item>
        /// <item><description>Split shipments where only part of the order is sent</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the product batch.
        /// </summary>
        /// <value>
        /// The expiration date in ISO 8601 format (YYYY-MM-DD), or null if not applicable.
        /// </value>
        /// <remarks>
        /// This field is critical for pharmaceutical and food products. It enables:
        /// <list type="bullet">
        /// <item><description>Tracking product shelf life</description></item>
        /// <item><description>Managing product recalls</description></item>
        /// <item><description>FEFO (First Expired, First Out) inventory management</description></item>
        /// <item><description>Regulatory compliance and reporting</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the series/batch number of the product.
        /// </summary>
        /// <value>
        /// The manufacturer's batch or lot number, or null if not applicable.
        /// </value>
        /// <remarks>
        /// Batch/series numbers are essential for:
        /// <list type="bullet">
        /// <item><description>Product traceability throughout the supply chain</description></item>
        /// <item><description>Quality control and defect tracking</description></item>
        /// <item><description>Targeted product recalls</description></item>
        /// <item><description>Regulatory compliance (especially for pharmaceuticals)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("seriesNumber")]
        public string? SeriesNumber { get; set; }
    }

    /// <summary>
    /// Represents the response after successfully uploading order realization/fulfillment results.
    /// Provides confirmation and summary information about the processed fulfillment.
    /// </summary>
    /// <remarks>
    /// This response confirms that the order realization data was received and processed,
    /// and provides summary information for verification and logging purposes.
    /// </remarks>
    public class UploadOrderRealizationResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the order realization was successfully processed and recorded;
        /// otherwise, <c>false</c>.
        /// </value>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the result of the operation.
        /// </summary>
        /// <value>
        /// A descriptive message indicating success or explaining any errors that occurred.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// On success, this typically contains a confirmation message.
        /// On failure, this should contain detailed error information to help diagnose the issue.
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number that was processed.
        /// </summary>
        /// <value>
        /// The order number from the request, echoed back for confirmation and correlation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This allows the caller to verify that the correct order was processed,
        /// especially when processing multiple orders concurrently.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of order items that were successfully processed.
        /// </summary>
        /// <value>
        /// The count of order line items from the request that were successfully recorded.
        /// </value>
        /// <remarks>
        /// This count should match the number of items in the request if all items
        /// were processed successfully. A mismatch may indicate partial processing or errors.
        /// </remarks>
        [JsonPropertyName("itemsProcessed")]
        public int ItemsProcessed { get; set; }
    }
}
