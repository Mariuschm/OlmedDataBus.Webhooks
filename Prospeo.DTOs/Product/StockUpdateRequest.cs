using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Product
{
    /// <summary>
    /// Represents a request to update stock levels for one or more products in a marketplace.
    /// Used for synchronizing inventory levels between the warehouse management system and marketplaces.
    /// </summary>
    /// <remarks>
    /// This DTO enables real-time or batch synchronization of product stock levels,
    /// ensuring that marketplaces display accurate availability information to customers.
    /// 
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item><description>Real-time inventory updates after order fulfillment</description></item>
    /// <item><description>Batch synchronization at regular intervals</description></item>
    /// <item><description>Stock corrections after physical inventory counts</description></item>
    /// <item><description>Price updates based on average purchase prices</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Best Practice:</strong> Send updates only for products whose stock or price has changed
    /// to minimize API calls and improve performance.
    /// </para>
    /// </remarks>
    public class StockUpdateRequest : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier for which stock is being updated.
        /// </summary>
        /// <value>
        /// The marketplace name or code (e.g., "APTEKA_OLMED", "ALLEGRO", "AMAZON").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Identifies the target marketplace for the stock update. Each marketplace may have
        /// different inventory allocations, so stock levels can vary across marketplaces.
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dictionary of SKUs to be updated with their new stock levels and prices.
        /// </summary>
        /// <value>
        /// A dictionary where keys are product SKUs (strings) and values are <see cref="StockUpdateItemDto"/>
        /// objects containing the new stock quantity and average purchase price.
        /// </value>
        /// <remarks>
        /// The dictionary structure allows for efficient updates of multiple products in a single request.
        /// 
        /// <para>
        /// <strong>Performance Note:</strong> While there's no hard limit, consider breaking very large
        /// updates (thousands of SKUs) into multiple smaller requests to avoid timeouts and enable
        /// partial success handling.
        /// </para>
        /// 
        /// <para>
        /// Example:
        /// <code>
        /// {
        ///   "SKU001": { "stock": 100, "average_purchase_price": 25.50 },
        ///   "SKU002": { "stock": 0, "average_purchase_price": 15.00 },
        ///   "SKU003": { "stock": 250, "average_purchase_price": 42.99 }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        [JsonPropertyName("skus")]
        public Dictionary<string, StockUpdateItemDto> Skus { get; set; } = new();

        /// <summary>
        /// Gets or sets optional notes or comments about this stock update operation.
        /// </summary>
        /// <value>
        /// Additional context or explanation for the stock update.
        /// Returns <c>null</c> if not provided.
        /// </value>
        /// <remarks>
        /// Can be used to provide context such as:
        /// <list type="bullet">
        /// <item><description>Source of the update (e.g., "Scheduled sync", "Manual correction")</description></item>
        /// <item><description>Reason for large stock changes</description></item>
        /// <item><description>Reference to related documents (e.g., "After inventory count 2024-01-15")</description></item>
        /// <item><description>Operator or system that initiated the update</description></item>
        /// </list>
        /// 
        /// <para>
        /// This information is useful for audit trails and troubleshooting.
        /// </para>
        /// </remarks>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this stock data was captured or should be effective.
        /// </summary>
        /// <value>
        /// The timestamp representing when the stock levels were accurate or should become effective.
        /// Returns <c>null</c> to use the current server time.
        /// </value>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>If null, the system uses the current time as the update timestamp</description></item>
        /// <item><description>If provided, represents when the stock data was captured in the source system</description></item>
        /// <item><description>Can be used to detect and handle out-of-sequence updates</description></item>
        /// <item><description>Useful for batch processing of historical updates</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> The system may reject updates with timestamps far in the past
        /// or future to prevent data inconsistencies.
        /// </para>
        /// </remarks>
        [JsonPropertyName("updateDate")]
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Represents stock and pricing information for a single product SKU in a stock update.
    /// Contains the new quantity available and the average purchase price.
    /// </summary>
    /// <remarks>
    /// This DTO encapsulates both inventory quantity and cost information, enabling
    /// synchronized updates of both stock availability and pricing data.
    /// 
    /// <para>
    /// The average purchase price is particularly important for:
    /// <list type="bullet">
    /// <item><description>Calculating profit margins</description></item>
    /// <item><description>Dynamic pricing strategies</description></item>
    /// <item><description>Inventory valuation</description></item>
    /// <item><description>Cost of goods sold (COGS) calculations</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StockUpdateItemDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the new stock quantity available for sale.
        /// </summary>
        /// <value>
        /// The number of units available in inventory. Supports decimal values for fractional quantities.
        /// Zero indicates the product is out of stock.
        /// </value>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Should represent available-to-sell quantity (not including reserved or allocated stock)</description></item>
        /// <item><description>Decimal values support fractional units (e.g., 2.5 kg, 1.75 liters)</description></item>
        /// <item><description>Zero stock typically triggers "out of stock" status on marketplaces</description></item>
        /// <item><description>Negative values should be avoided and may be rejected by the system</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Best Practice:</strong> Consider safety stock levels and buffer inventory
        /// to avoid overselling due to synchronization delays.
        /// </para>
        /// </remarks>
        [JsonPropertyName("stock")]
        public decimal Stock { get; set; }

        /// <summary>
        /// Gets or sets the average purchase price of the product.
        /// </summary>
        /// <value>
        /// The weighted average cost at which the product was purchased from suppliers.
        /// Used for cost accounting and pricing decisions.
        /// </value>
        /// <remarks>
        /// The average purchase price is calculated based on the weighted average of all purchases:
        /// <code>
        /// Average Price = Total Cost of All Units / Total Number of Units
        /// </code>
        /// 
        /// <para>
        /// This value is used for:
        /// <list type="bullet">
        /// <item><description>Inventory valuation on balance sheets</description></item>
        /// <item><description>Gross margin calculations</description></item>
        /// <item><description>Minimum acceptable selling price determination</description></item>
        /// <item><description>Financial reporting and cost analysis</description></item>
        /// <item><description>Dynamic pricing algorithms</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// <strong>Note:</strong> This is typically the net purchase price (excluding VAT)
        /// in the system's base currency.
        /// </para>
        /// </remarks>
        [JsonPropertyName("average_purchase_price")]
        public decimal AveragePurchasePrice { get; set; }
    }
}
