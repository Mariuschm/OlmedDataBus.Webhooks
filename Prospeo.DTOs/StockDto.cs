using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents current stock level information for products in a marketplace.
    /// Contains a dictionary of SKUs with their available quantities and pricing information.
    /// </summary>
    /// <remarks>
    /// This DTO is used to query and retrieve current inventory levels for products
    /// across different marketplaces. It provides a snapshot of stock availability
    /// and average purchase prices at a specific point in time.
    /// 
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item><description>Retrieving current stock levels for display or reporting</description></item>
    /// <item><description>Synchronizing inventory data between systems</description></item>
    /// <item><description>Validating available quantities before order processing</description></item>
    /// <item><description>Inventory analysis and forecasting</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StockDto
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// The marketplace name or code (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Identifies which marketplace's inventory is represented in this DTO.
        /// Each marketplace may have different stock allocations, so the same product
        /// may show different availability across different marketplaces.
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dictionary of stock levels for individual product SKUs.
        /// </summary>
        /// <value>
        /// A dictionary where keys are product SKUs (strings) and values are <see cref="StockItemDto"/>
        /// objects containing the current stock quantity and average purchase price.
        /// </value>
        /// <remarks>
        /// The dictionary structure provides efficient lookups of stock information by SKU.
        /// 
        /// <para>
        /// Each entry in the dictionary represents:
        /// <list type="bullet">
        /// <item><description>Key: The unique product SKU identifier</description></item>
        /// <item><description>Value: Stock quantity and pricing information for that SKU</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// Example structure:
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
        public Dictionary<string, StockItemDto> Skus { get; set; } = new();
    }

    /// <summary>
    /// Represents stock level and pricing information for a single product.
    /// Contains the available quantity and the average purchase cost.
    /// </summary>
    /// <remarks>
    /// This DTO encapsulates both inventory quantity and cost data for a product,
    /// enabling comprehensive stock management and financial analysis.
    /// 
    /// <para>
    /// The combination of stock level and purchase price is essential for:
    /// <list type="bullet">
    /// <item><description>Inventory valuation calculations</description></item>
    /// <item><description>Profit margin analysis</description></item>
    /// <item><description>Pricing strategy decisions</description></item>
    /// <item><description>Cost of goods sold (COGS) reporting</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StockItemDto
    {
        /// <summary>
        /// Gets or sets the available stock quantity.
        /// </summary>
        /// <value>
        /// The number of units currently available for sale. Supports decimal values.
        /// </value>
        /// <remarks>
        /// This represents the available-to-sell quantity, which should account for:
        /// <list type="bullet">
        /// <item><description>Physical inventory on hand</description></item>
        /// <item><description>Minus any reserved or allocated stock</description></item>
        /// <item><description>Minus any quality holds or quarantined items</description></item>
        /// <item><description>Plus any available stock in transit (optional, depends on policy)</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Decimal Support:</strong> Decimal quantities accommodate products sold in
        /// fractional units such as weight-based (kg, liters) or length-based (meters) products.
        /// </para>
        /// 
        /// <para>
        /// <strong>Zero Stock:</strong> A value of 0 indicates the product is currently out of stock
        /// and unavailable for purchase.
        /// </para>
        /// </remarks>
        [JsonPropertyName("stock")]
        public decimal Stock { get; set; }

        /// <summary>
        /// Gets or sets the average purchase price of the product.
        /// </summary>
        /// <value>
        /// The weighted average cost at which the product was acquired from suppliers.
        /// </value>
        /// <remarks>
        /// The average purchase price represents the weighted average cost of all units in inventory:
        /// <code>
        /// Average Purchase Price = Total Cost of All Inventory Units / Total Number of Units
        /// </code>
        /// 
        /// <para>
        /// This price is used for:
        /// <list type="bullet">
        /// <item><description>Inventory asset valuation on financial statements</description></item>
        /// <item><description>Calculating gross profit margins (Selling Price - Purchase Price)</description></item>
        /// <item><description>Setting minimum acceptable selling prices</description></item>
        /// <item><description>Cost of goods sold (COGS) calculations when items are sold</description></item>
        /// <item><description>Pricing strategy and competitive analysis</description></item>
        /// <item><description>Inventory write-downs and loss calculations</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// <strong>Currency:</strong> This value is typically in the system's base currency
        /// and represents the net purchase price (excluding VAT or other taxes).
        /// </para>
        /// 
        /// <para>
        /// <strong>Calculation Note:</strong> The average purchase price updates automatically
        /// as new inventory is purchased at different costs, using the weighted average method
        /// to smooth out price fluctuations over time.
        /// </para>
        /// </remarks>
        [JsonPropertyName("average_purchase_price")]
        public decimal AveragePurchasePrice { get; set; }
    }
}
