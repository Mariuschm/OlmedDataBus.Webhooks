using Prospeo.DTOs.Core;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Product
{
    /// <summary>
    /// Custom JSON converter for DateTime that handles the specific format "yyyy-MM-dd HH:mm:ss".
    /// Provides special handling for invalid dates and flexible parsing fallback mechanisms.
    /// </summary>
    /// <remarks>
    /// This converter is designed to handle DateTime serialization/deserialization with specific requirements:
    /// 
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item><description>Primary format: "yyyy-MM-dd HH:mm:ss" (e.g., "2024-01-15 14:30:00")</description></item>
    /// <item><description>Handles invalid date strings like "0000-00-00 00:00:00" by returning current DateTime</description></item>
    /// <item><description>Provides fallback to standard DateTime parsing if custom format fails</description></item>
    /// <item><description>Throws JsonException for completely unparseable dates</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Use Cases:</strong>
    /// This converter is particularly useful when dealing with:
    /// <list type="bullet">
    /// <item><description>Legacy systems that may produce invalid date values</description></item>
    /// <item><description>External APIs with specific date format requirements</description></item>
    /// <item><description>Database systems that allow zero dates (like MySQL)</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// [JsonPropertyName("lastModifyDateTime")]
    /// [JsonConverter(typeof(CustomDateTimeConverter))]
    /// public DateTime LastModifyDateTime { get; set; }
    /// </code>
    /// </para>
    /// </remarks>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// The expected DateTime format string.
        /// </summary>
        /// <remarks>
        /// Format: "yyyy-MM-dd HH:mm:ss" (e.g., "2024-01-15 14:30:00")
        /// <list type="bullet">
        /// <item><description>yyyy: 4-digit year</description></item>
        /// <item><description>MM: 2-digit month (01-12)</description></item>
        /// <item><description>dd: 2-digit day (01-31)</description></item>
        /// <item><description>HH: 2-digit hour in 24-hour format (00-23)</description></item>
        /// <item><description>mm: 2-digit minute (00-59)</description></item>
        /// <item><description>ss: 2-digit second (00-59)</description></item>
        /// </list>
        /// </remarks>
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Reads and converts JSON to a DateTime value.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
        /// <param name="typeToConvert">The type being converted (DateTime).</param>
        /// <param name="options">Serializer options (not used in this implementation).</param>
        /// <returns>
        /// A <see cref="DateTime"/> value parsed from the JSON string.
        /// Returns <see cref="DateTime.Now"/> if the date string is "0000-00-00 00:00:00".
        /// Returns <see cref="default"/> if the string is null or empty.
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the date string cannot be parsed using the expected format or standard parsing.
        /// </exception>
        /// <remarks>
        /// The method follows a three-step parsing strategy:
        /// <list type="number">
        /// <item><description>Checks for null/empty strings - returns default(DateTime)</description></item>
        /// <item><description>Checks for invalid date "0000-00-00 00:00:00" - returns DateTime.Now</description></item>
        /// <item><description>Attempts to parse using the specific format "yyyy-MM-dd HH:mm:ss"</description></item>
        /// <item><description>Falls back to standard DateTime.TryParse for flexibility</description></item>
        /// <item><description>Throws JsonException if all parsing attempts fail</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Special Case Handling:</strong>
        /// The "0000-00-00 00:00:00" date is common in legacy databases (like MySQL) when no date is set.
        /// Instead of failing, we return the current DateTime to ensure the application continues to function.
        /// </para>
        /// </remarks>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the date string from JSON
            var dateString = reader.GetString();
            
            // Handle null or empty strings by returning the default DateTime (01/01/0001 00:00:00)
            if (string.IsNullOrEmpty(dateString))
            {
                return default;
            }

            // Check for invalid date format "0000-00-00 00:00:00" and return current DateTime
            // This is common in MySQL databases when no date is set
            if (dateString == "0000-00-00 00:00:00")
            {
                return DateTime.Now;
            }

            // Try to parse using the exact expected format
            if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback to standard parsing if custom format fails
            // This provides flexibility for slightly different date formats
            if (DateTime.TryParse(dateString, out var fallbackResult))
            {
                return fallbackResult;
            }

            // If all parsing attempts fail, throw an exception with details
            throw new JsonException($"Unable to parse '{dateString}' as DateTime. Expected format: {DateFormat}");
        }

        /// <summary>
        /// Writes a DateTime value to JSON using the specific format.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
        /// <param name="value">The DateTime value to serialize.</param>
        /// <param name="options">Serializer options (not used in this implementation).</param>
        /// <remarks>
        /// Serializes the DateTime value using the format "yyyy-MM-dd HH:mm:ss" with invariant culture
        /// to ensure consistent output regardless of the server's culture settings.
        /// 
        /// <para>
        /// Example output: "2024-01-15 14:30:00"
        /// </para>
        /// </remarks>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Write the DateTime as a formatted string using invariant culture
            writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Attribute to mark properties for special processing or grouping.
    /// Used to categorize properties that require special handling in the application logic.
    /// </summary>
    /// <remarks>
    /// This attribute can be applied to properties to indicate they belong to a specific category
    /// that requires special processing, validation, or display logic.
    /// 
    /// <para>
    /// <strong>Common Categories:</strong>
    /// <list type="bullet">
    /// <item><description>"ProductFlags" - Boolean flags indicating product characteristics</description></item>
    /// <item><description>"Attribute" - Properties that are attributes in the XML/system sense</description></item>
    /// <item><description>"Default" - Default category when none is specified</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// [SpecialProperty("ProductFlags")]
    /// public bool IsRefrigeratedStorage { get; set; }
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// This attribute enables:
    /// <list type="bullet">
    /// <item><description>Runtime categorization of properties using reflection</description></item>
    /// <item><description>Grouping properties for UI rendering or validation</description></item>
    /// <item><description>Conditional processing based on property categories</description></item>
    /// <item><description>Dynamic form generation and metadata-driven operations</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SpecialPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the category name for this special property.
        /// </summary>
        /// <value>
        /// A string representing the category or group this property belongs to.
        /// Common values include "ProductFlags", "Attribute", etc.
        /// </value>
        /// <remarks>
        /// The category can be used by the application to:
        /// <list type="bullet">
        /// <item><description>Group related properties together in UI forms</description></item>
        /// <item><description>Apply category-specific validation rules</description></item>
        /// <item><description>Filter or process properties based on their category</description></item>
        /// <item><description>Generate metadata for API documentation</description></item>
        /// </list>
        /// </remarks>
        public string Category { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialPropertyAttribute"/> class.
        /// </summary>
        /// <param name="category">
        /// The category name for this property. Defaults to "Default" if not specified.
        /// </param>
        /// <remarks>
        /// <strong>Example Usage:</strong>
        /// <code>
        /// // Using a specific category
        /// [SpecialProperty("ProductFlags")]
        /// public bool IsActive { get; set; }
        /// 
        /// // Using default category
        /// [SpecialProperty()]
        /// public string CustomField { get; set; }
        /// </code>
        /// </remarks>
        public SpecialPropertyAttribute(string category = "Default")
        {
            Category = category;
        }
    }

    /// <summary>
    /// Represents complete product information including identification, characteristics, and metadata.
    /// This DTO is used for product synchronization between systems and marketplaces.
    /// </summary>
    /// <remarks>
    /// This is a comprehensive product data transfer object that contains all essential information
    /// about a product, including:
    /// 
    /// <para>
    /// <strong>Basic Identification:</strong>
    /// <list type="bullet">
    /// <item><description>SKU (Stock Keeping Unit) - unique product identifier</description></item>
    /// <item><description>EAN (European Article Number) - barcode identifier</description></item>
    /// <item><description>Name - product display name</description></item>
    /// <item><description>Marketplace - the marketplace/platform this product is associated with</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Physical Properties:</strong>
    /// <list type="bullet">
    /// <item><description>Dimensions (X, Y, Z) - product size in centimeters</description></item>
    /// <item><description>Weight - product weight in kilograms</description></item>
    /// <item><description>Package quantity - number of units per package</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Storage and Handling Requirements:</strong>
    /// <list type="bullet">
    /// <item><description>Refrigerated storage/transport requirements</description></item>
    /// <item><description>Fragility indicator</description></item>
    /// <item><description>Expiration date tracking requirement</description></item>
    /// <item><description>Series number tracking requirement</description></item>
    /// <item><description>Quality control requirement</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Business Logic:</strong>
    /// <list type="bullet">
    /// <item><description>Active/inactive status</description></item>
    /// <item><description>Discount eligibility</description></item>
    /// <item><description>Reception restrictions</description></item>
    /// <item><description>Package/free item indicators</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// Inherits from <see cref="DTOModelBase"/> to ensure all string properties are automatically
    /// initialized to empty strings, preventing null reference exceptions.
    /// </para>
    /// </remarks>
    public class ProductDto: DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier where this product is sold.
        /// </summary>
        /// <value>
        /// The marketplace name or code (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The marketplace identifier is crucial for:
        /// <list type="bullet">
        /// <item><description>Multi-marketplace inventory management</description></item>
        /// <item><description>Platform-specific product variations</description></item>
        /// <item><description>Marketplace-specific pricing and availability</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique product identifier in the system.
        /// </summary>
        /// <value>
        /// An integer representing the internal system ID for this product.
        /// </value>
        /// <remarks>
        /// This is the primary key in the product database and is used for:
        /// <list type="bullet">
        /// <item><description>Internal product referencing</description></item>
        /// <item><description>Database relationships and joins</description></item>
        /// <item><description>System-wide product identification</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the product name or title.
        /// </summary>
        /// <value>
        /// The display name of the product as it appears to customers.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The product name should be:
        /// <list type="bullet">
        /// <item><description>Clear and descriptive for customers</description></item>
        /// <item><description>Consistent across all marketplaces (or marketplace-specific when needed)</description></item>
        /// <item><description>SEO-optimized for online marketplaces</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Stock Keeping Unit (SKU) - the unique product identifier.
        /// </summary>
        /// <value>
        /// A unique alphanumeric code identifying this specific product variant.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The SKU is the primary business identifier for products and is used for:
        /// <list type="bullet">
        /// <item><description>Inventory tracking across all systems</description></item>
        /// <item><description>Order processing and fulfillment</description></item>
        /// <item><description>Cross-system product matching</description></item>
        /// <item><description>Warehouse operations and picking</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Important:</strong> The SKU must be unique within the system and should remain
        /// consistent across all integrations and marketplaces.
        /// </para>
        /// </remarks>
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the European Article Number (EAN) barcode.
        /// </summary>
        /// <value>
        /// The 8, 13, or 14 digit EAN barcode number.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The EAN (also known as GTIN) is used for:
        /// <list type="bullet">
        /// <item><description>Barcode scanning in warehouses and stores</description></item>
        /// <item><description>Product identification in supply chain</description></item>
        /// <item><description>Marketplace product matching and catalog integration</description></item>
        /// <item><description>Point of sale (POS) systems</description></item>
        /// </list>
        /// 
        /// <para>
        /// Common EAN formats:
        /// <list type="bullet">
        /// <item><description>EAN-8: 8 digits</description></item>
        /// <item><description>EAN-13: 13 digits (most common)</description></item>
        /// <item><description>EAN-14: 14 digits (for trade items)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("ean")]
        public string Ean { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the BLOZ7 classification code.
        /// </summary>
        /// <value>
        /// A specialized classification code used in the Polish pharmaceutical industry.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// BLOZ7 is a Polish pharmaceutical classification system used for:
        /// <list type="bullet">
        /// <item><description>Product categorization in pharmaceutical systems</description></item>
        /// <item><description>Regulatory compliance and reporting</description></item>
        /// <item><description>Integration with Polish healthcare systems</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("bloz7")]
        [SpecialProperty("ProductFlags")]
        public string Bloz7 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product group or category.
        /// </summary>
        /// <value>
        /// The group/category name this product belongs to (e.g., "Medications", "Supplements").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Product groups are used for:
        /// <list type="bullet">
        /// <item><description>Catalog organization and navigation</description></item>
        /// <item><description>Reporting and analytics</description></item>
        /// <item><description>Applying category-specific business rules</description></item>
        /// <item><description>Marketing and merchandising</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base unit of measure (UOM).
        /// </summary>
        /// <value>
        /// The unit of measure for this product (e.g., "PCS" for pieces, "KG" for kilograms, "L" for liters).
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The base UOM defines how the product is counted and sold:
        /// <list type="bullet">
        /// <item><description>"PCS" or "EA" - individual pieces/items</description></item>
        /// <item><description>"KG" - kilograms (weight-based)</description></item>
        /// <item><description>"L" - liters (volume-based)</description></item>
        /// <item><description>"M" - meters (length-based)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("baseUom")]
        public string BaseUom { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether expiration date tracking is required for this product.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product requires expiration date tracking; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Expiration date tracking is critical for:
        /// <list type="bullet">
        /// <item><description>Pharmaceutical products with shelf life limits</description></item>
        /// <item><description>Food and beverage items</description></item>
        /// <item><description>Cosmetics and personal care products</description></item>
        /// <item><description>FEFO (First Expired, First Out) inventory management</description></item>
        /// <item><description>Regulatory compliance and product recalls</description></item>
        /// </list>
        /// 
        /// <para>
        /// When <c>true</c>, the system must track expiration dates at the batch/lot level
        /// for all inventory movements and sales.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isExpirationDateRequired")]
        public bool IsExpirationDateRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether series/serial number tracking is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product requires series/serial number tracking; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Series number tracking is essential for:
        /// <list type="bullet">
        /// <item><description>Medical devices and pharmaceutical products</description></item>
        /// <item><description>High-value items requiring individual tracking</description></item>
        /// <item><description>Products subject to warranty or recall procedures</description></item>
        /// <item><description>Regulatory compliance and traceability</description></item>
        /// <item><description>Anti-counterfeiting measures</description></item>
        /// </list>
        /// 
        /// <para>
        /// When <c>true</c>, each unit must be tracked with a unique series/serial number
        /// throughout its lifecycle from receipt to sale.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isSeriesNumberRequired")]
        public bool IsSeriesNumberRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether quality control checks are required.
        /// </summary>
        /// <value>
        /// <c>true</c> if quality control is required; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Quality control requirements apply to products that need:
        /// <list type="bullet">
        /// <item><description>Visual inspection upon receipt</description></item>
        /// <item><description>Temperature verification for temperature-sensitive items</description></item>
        /// <item><description>Integrity checks for packaging and seals</description></item>
        /// <item><description>Compliance verification with regulatory standards</description></item>
        /// </list>
        /// 
        /// <para>
        /// Products flagged for quality control may require quarantine upon receipt
        /// until QC procedures are completed and approved.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isQualityControlRequired")]
        [SpecialProperty("ProductFlags")]
        public bool IsQualityControlRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether refrigerated storage is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product must be stored in refrigerated conditions; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Refrigerated storage requirements affect:
        /// <list type="bullet">
        /// <item><description>Warehouse location assignment (cold storage zones)</description></item>
        /// <item><description>Inventory management and stock rotation</description></item>
        /// <item><description>Temperature monitoring and compliance</description></item>
        /// <item><description>Storage cost calculations</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Typical temperature ranges:</strong>
        /// <list type="bullet">
        /// <item><description>Refrigerated: 2°C to 8°C</description></item>
        /// <item><description>Some products may require frozen storage below 0°C</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("isRefrigeratedStorage")]
        [SpecialProperty("ProductFlags")]
        public bool IsRefrigeratedStorage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether refrigerated transport is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product must be transported in refrigerated conditions; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Refrigerated transport requirements impact:
        /// <list type="bullet">
        /// <item><description>Courier/carrier selection (must have cold chain capability)</description></item>
        /// <item><description>Shipping cost calculations</description></item>
        /// <item><description>Delivery time windows and scheduling</description></item>
        /// <item><description>Packaging requirements (insulated boxes, gel packs)</description></item>
        /// <item><description>Temperature monitoring during transit</description></item>
        /// </list>
        /// 
        /// <para>
        /// Products requiring refrigerated transport typically also require refrigerated storage.
        /// Maintaining the cold chain is critical for product efficacy and regulatory compliance.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isRefrigeratedTransport")]
        [SpecialProperty("ProductFlags")]
        public bool IsRefrigeratedTransport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is fragile and requires special handling.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product is fragile; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Fragile products require:
        /// <list type="bullet">
        /// <item><description>Special packaging materials (bubble wrap, foam inserts)</description></item>
        /// <item><description>Careful handling instructions for warehouse staff</description></item>
        /// <item><description>"Fragile" labels on shipping boxes</description></item>
        /// <item><description>Restrictions on stacking and storage</description></item>
        /// <item><description>Special considerations during picking and packing</description></item>
        /// </list>
        /// 
        /// <para>
        /// Examples include: glass items, electronics, ceramics, or products with delicate components.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isFragile")]
        [SpecialProperty("ProductFlags")]
        public bool IsFragile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is currently eligible for discounts.
        /// </summary>
        /// <value>
        /// <c>true</c> if discounts can be applied; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Discount eligibility affects:
        /// <list type="bullet">
        /// <item><description>Promotional campaigns and sales</description></item>
        /// <item><description>Coupon applicability</description></item>
        /// <item><description>Volume discount calculations</description></item>
        /// <item><description>Loyalty program benefits</description></item>
        /// </list>
        /// 
        /// <para>
        /// Products may be excluded from discounts due to:
        /// <list type="bullet">
        /// <item><description>Regulatory restrictions (e.g., prescription medications)</description></item>
        /// <item><description>Supplier agreements and MAP (Minimum Advertised Price) policies</description></item>
        /// <item><description>Already reduced prices or clearance items</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("isDiscounted")]
        [SpecialProperty("ProductFlags")]
        public bool IsDiscounted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is restricted from being received into inventory.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product cannot be received; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Products may be flagged as "not for reception" when:
        /// <list type="bullet">
        /// <item><description>The product is being phased out or discontinued</description></item>
        /// <item><description>There are regulatory issues or holds</description></item>
        /// <item><description>Quality issues have been identified</description></item>
        /// <item><description>The product is for display or promotional use only</description></item>
        /// </list>
        /// 
        /// <para>
        /// This flag prevents new inventory from being added, though existing stock
        /// can still be sold until depleted.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isNotForReception")]
        [SpecialProperty("ProductFlags")]
        public bool IsNotForReception { get; set; }

        /// <summary>
        /// Gets or sets the URL to the product image.
        /// </summary>
        /// <value>
        /// A fully qualified URL pointing to the product's primary image.
        /// Returns <see cref="string.Empty"/> if no image is available.
        /// </value>
        /// <remarks>
        /// The image URL is used for:
        /// <list type="bullet">
        /// <item><description>Product display on websites and marketplaces</description></item>
        /// <item><description>Mobile applications</description></item>
        /// <item><description>Picking and packing visual confirmation</description></item>
        /// <item><description>Marketing materials</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Best practices:</strong>
        /// <list type="bullet">
        /// <item><description>Use secure HTTPS URLs</description></item>
        /// <item><description>Ensure images are optimized for web (compressed but high quality)</description></item>
        /// <item><description>Provide standard aspect ratios (e.g., 1:1 or 4:3)</description></item>
        /// <item><description>Include fallback image for missing products</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SKU of the parent article for product variants or bundles.
        /// </summary>
        /// <value>
        /// The SKU of the parent product if this is a variant or child product.
        /// Returns <see cref="string.Empty"/> if this is a standalone product.
        /// </value>
        /// <remarks>
        /// Parent-child relationships are used for:
        /// <list type="bullet">
        /// <item><description>Product variants (e.g., different sizes, colors, or packages of the same product)</description></item>
        /// <item><description>Product bundles or kits where components reference a parent</description></item>
        /// <item><description>Hierarchical product structures in catalogs</description></item>
        /// <item><description>Aggregated inventory and sales reporting</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Example:</strong> A medication available in 30-tablet and 60-tablet packages
        /// might share a common parent SKU while having unique child SKUs for each package size.
        /// </para>
        /// </remarks>
        [JsonPropertyName("parentArticleSKU")]
        [SpecialProperty("ProductFlags")]
        public string ParentArticleSKU { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the product is currently active and available for sale.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product is active; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The active status controls:
        /// <list type="bullet">
        /// <item><description>Product visibility on websites and marketplaces</description></item>
        /// <item><description>Searchability in catalogs</description></item>
        /// <item><description>Availability for new orders</description></item>
        /// <item><description>Inventory synchronization to external systems</description></item>
        /// </list>
        /// 
        /// <para>
        /// Inactive products:
        /// <list type="bullet">
        /// <item><description>Are hidden from customers</description></item>
        /// <item><description>Cannot be ordered (though existing orders can be fulfilled)</description></item>
        /// <item><description>May still appear in historical data and reports</description></item>
        /// <item><description>Can be reactivated when needed</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the product dimensions (length, width, height).
        /// </summary>
        /// <value>
        /// A <see cref="ProductDimensionsDto"/> object containing the X, Y, Z dimensions.
        /// Initialized to a new instance to prevent null references.
        /// </value>
        /// <remarks>
        /// Product dimensions are used for:
        /// <list type="bullet">
        /// <item><description>Shipping cost calculations (dimensional weight)</description></item>
        /// <item><description>Packaging selection and optimization</description></item>
        /// <item><description>Warehouse space allocation</description></item>
        /// <item><description>Pallet and container loading optimization</description></item>
        /// </list>
        /// 
        /// <para>
        /// Dimensions are typically measured in centimeters (cm) and represent the
        /// packaged product dimensions, not the product itself.
        /// </para>
        /// </remarks>
        [JsonPropertyName("dimensions")]
        public ProductDimensionsDto Dimensions { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of units contained in one package.
        /// </summary>
        /// <value>
        /// The quantity of individual units per package. Supports decimal values.
        /// </value>
        /// <remarks>
        /// Package quantity is important for:
        /// <list type="bullet">
        /// <item><description>Bulk orders and wholesale pricing</description></item>
        /// <item><description>Inventory conversions between units and packages</description></item>
        /// <item><description>Picking efficiency (e.g., pick by case rather than individual units)</description></item>
        /// <item><description>Storage and transportation optimization</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        /// <item><description>A case of 24 bottles: PackageQuantity = 24</description></item>
        /// <item><description>A box of 100 tablets: PackageQuantity = 100</description></item>
        /// <item><description>A pallet of 1000 units: PackageQuantity = 1000</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("packageQuantity")]
        [SpecialProperty("ProductFlags")]
        public decimal PackageQuantity { get; set; }

        /// <summary>
        /// Gets or sets the product weight.
        /// </summary>
        /// <value>
        /// The weight in kilograms (kg). Supports decimal values for precise measurements.
        /// </value>
        /// <remarks>
        /// Product weight is used for:
        /// <list type="bullet">
        /// <item><description>Shipping cost calculations</description></item>
        /// <item><description>Carrier selection (weight limits for different shipping methods)</description></item>
        /// <item><description>Customs documentation for international shipments</description></item>
        /// <item><description>Loading and capacity planning for vehicles</description></item>
        /// </list>
        /// 
        /// <para>
        /// The weight should include packaging materials to represent the actual
        /// shipping weight of the product.
        /// </para>
        /// </remarks>
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer or producer name.
        /// </summary>
        /// <value>
        /// The name of the company that manufactures or produces this product.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Producer information is important for:
        /// <list type="bullet">
        /// <item><description>Product authenticity verification</description></item>
        /// <item><description>Warranty and support claims</description></item>
        /// <item><description>Supplier management and purchasing</description></item>
        /// <item><description>Product recalls and quality issues</description></item>
        /// <item><description>Customer information and transparency</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("producer")]
        [SpecialProperty("ProductFlags")]
        public string Producer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the product was last modified.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> representing the last modification timestamp.
        /// </value>
        /// <remarks>
        /// This timestamp is crucial for:
        /// <list type="bullet">
        /// <item><description>Data synchronization between systems (detecting changes)</description></item>
        /// <item><description>Audit trails and change tracking</description></item>
        /// <item><description>Determining which products need updates in marketplaces</description></item>
        /// <item><description>Version control and conflict resolution</description></item>
        /// </list>
        /// 
        /// <para>
        /// Uses <see cref="CustomDateTimeConverter"/> to handle the specific format "yyyy-MM-dd HH:mm:ss"
        /// and gracefully handle invalid dates like "0000-00-00 00:00:00".
        /// </para>
        /// </remarks>
        [JsonPropertyName("lastModifyDateTime")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime LastModifyDateTime { get; set; }

        /// <summary>
        /// Gets or sets the VAT (Value Added Tax) rate applicable to this product.
        /// </summary>
        /// <value>
        /// The VAT rate as a string (e.g., "23%", "8%", "0%", "ZW").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// VAT rates in Poland:
        /// <list type="bullet">
        /// <item><description>"23%" - Standard VAT rate</description></item>
        /// <item><description>"8%" - Reduced rate (certain foods, books, etc.)</description></item>
        /// <item><description>"5%" - Further reduced rate (specific items)</description></item>
        /// <item><description>"0%" - Zero-rated (exports, certain services)</description></item>
        /// <item><description>"ZW" - Exempt (zwolniony)</description></item>
        /// <item><description>"NP" - Not subject to VAT (nie podlega)</description></item>
        /// </list>
        /// 
        /// <para>
        /// The VAT rate affects:
        /// <list type="bullet">
        /// <item><description>Final price calculations for customers</description></item>
        /// <item><description>Invoice generation</description></item>
        /// <item><description>Tax reporting and compliance</description></item>
        /// <item><description>Accounting entries</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("vatRate")]
        public string VatRate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the supervisor or responsible person for this product.
        /// </summary>
        /// <value>
        /// The name or identifier of the person supervising or responsible for this product line.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The supervisor designation is used for:
        /// <list type="bullet">
        /// <item><description>Product management accountability</description></item>
        /// <item><description>Category management and merchandising decisions</description></item>
        /// <item><description>Quality issues and escalations</description></item>
        /// <item><description>Pricing and promotional approvals</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("supervisor")]
        [SpecialProperty("ProductFlags")]
        public string Supervisor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product type or classification.
        /// </summary>
        /// <value>
        /// A string describing the product type. Can be null if not applicable.
        /// </value>
        /// <remarks>
        /// Product types can indicate:
        /// <list type="bullet">
        /// <item><description>Product category (e.g., "Medication", "Supplement", "Device")</description></item>
        /// <item><description>Regulatory classification</description></item>
        /// <item><description>Business logic grouping</description></item>
        /// <item><description>Special handling requirements</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("type")]
        [SpecialProperty("ProductFlags")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this product is a package or bundle.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product is a package/bundle containing multiple items; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Package/bundle products:
        /// <list type="bullet">
        /// <item><description>Consist of multiple individual items sold as a single unit</description></item>
        /// <item><description>May have special pricing (discounted bundle pricing)</description></item>
        /// <item><description>Require special handling during picking (assemble components)</description></item>
        /// <item><description>May have relationships to component products in the system</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Examples:</strong> Gift sets, promotional bundles, starter kits, or multi-packs.
        /// </para>
        /// </remarks>
        [JsonPropertyName("isPackage")]
        [SpecialProperty("ProductFlags")]
        public bool IsPackage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this product is provided free of charge.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product is free (no charge to customer); otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Free products include:
        /// <list type="bullet">
        /// <item><description>Promotional gifts or samples</description></item>
        /// <item><description>Marketing materials (brochures, catalogs)</description></item>
        /// <item><description>Replacement items under warranty</description></item>
        /// <item><description>Complimentary add-ons with purchases</description></item>
        /// </list>
        /// 
        /// <para>
        /// Free products typically:
        /// <list type="bullet">
        /// <item><description>Have a price of zero in orders</description></item>
        /// <item><description>Still need inventory tracking</description></item>
        /// <item><description>May have associated costs for accounting purposes</description></item>
        /// <item><description>Require special handling in invoicing</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("IsFree")]
        [SpecialProperty("ProductFlags")]
        public bool IsFree { get; set; }

        /// <summary>
        /// Gets or sets the XL system item identifier.
        /// </summary>
        /// <value>
        /// The unique identifier for this product in the XL ERP system.
        /// </value>
        /// <remarks>
        /// The XL Item ID is used for:
        /// <list type="bullet">
        /// <item><description>Integration with legacy XL ERP systems</description></item>
        /// <item><description>Cross-system product matching and synchronization</description></item>
        /// <item><description>Maintaining backwards compatibility</description></item>
        /// <item><description>Data migration and import/export operations</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> This property is specific to organizations using XL software
        /// and may not be relevant for other systems.
        /// </para>
        /// </remarks>
        public int XlItemId { get; set; }
    }

    /// <summary>
    /// Represents the three-dimensional measurements of a product.
    /// Used for shipping calculations, warehouse space planning, and packaging optimization.
    /// </summary>
    /// <remarks>
    /// Product dimensions are essential for:
    /// 
    /// <para>
    /// <strong>Logistics and Shipping:</strong>
    /// <list type="bullet">
    /// <item><description>Calculating dimensional weight (volumetric weight) for shipping costs</description></item>
    /// <item><description>Determining if products fit in standard packaging</description></item>
    /// <item><description>Optimizing multi-item shipments (bin packing algorithms)</description></item>
    /// <item><description>Selecting appropriate shipping methods and carriers</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Warehouse Management:</strong>
    /// <list type="bullet">
    /// <item><description>Space allocation and slot assignment</description></item>
    /// <item><description>Pallet and shelf layout optimization</description></item>
    /// <item><description>Storage density calculations</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Measurement Convention:</strong>
    /// Dimensions are typically in centimeters (cm) and represent the packaged product:
    /// <list type="bullet">
    /// <item><description>X = Length (longest side)</description></item>
    /// <item><description>Y = Width (second longest side)</description></item>
    /// <item><description>Z = Height (shortest side, typically vertical)</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// Inherits from <see cref="DTOModelBase"/> to ensure consistent behavior with other DTOs.
    /// </para>
    /// </remarks>
    public class ProductDimensionsDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the X dimension (length) of the product.
        /// </summary>
        /// <value>
        /// The length in centimeters. Supports decimal values for precise measurements.
        /// </value>
        /// <remarks>
        /// The X dimension typically represents the longest side of the product package.
        /// This measurement is critical for:
        /// <list type="bullet">
        /// <item><description>Calculating package volume (X × Y × Z)</description></item>
        /// <item><description>Determining maximum package size for shipping methods</description></item>
        /// <item><description>Warehouse bin and shelf sizing</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("x")]
        public decimal X { get; set; }

        /// <summary>
        /// Gets or sets the Y dimension (width) of the product.
        /// </summary>
        /// <value>
        /// The width in centimeters. Supports decimal values for precise measurements.
        /// </value>
        /// <remarks>
        /// The Y dimension typically represents the second longest side of the product package.
        /// Used in conjunction with X and Z for complete dimensional analysis.
        /// </remarks>
        [JsonPropertyName("y")]
        public decimal Y { get; set; }

        /// <summary>
        /// Gets or sets the Z dimension (height) of the product.
        /// </summary>
        /// <value>
        /// The height in centimeters. Supports decimal values for precise measurements.
        /// </value>
        /// <remarks>
        /// The Z dimension typically represents the shortest side or vertical height of the product package.
        /// 
        /// <para>
        /// Height is particularly important for:
        /// <list type="bullet">
        /// <item><description>Stacking products in warehouse (knowing safe stacking height)</description></item>
        /// <item><description>Determining how many units fit vertically on a shelf</description></item>
        /// <item><description>Pallet loading calculations (not exceeding maximum pallet height)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("z")]
        public decimal Z { get; set; }
    }
}
