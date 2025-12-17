using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;
using System;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Custom JSON converter for DateTime that handles the format "yyyy-MM-dd HH:mm:ss"
    /// </summary>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
            {
                return default;
            }

            // Check for invalid date format "0000-00-00 00:00:00" and return current DateTime
            if (dateString == "0000-00-00 00:00:00")
            {
                return DateTime.Now;
            }

            if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback to standard parsing if custom format fails
            if (DateTime.TryParse(dateString, out var fallbackResult))
            {
                return fallbackResult;
            }

            throw new JsonException($"Unable to parse '{dateString}' as DateTime. Expected format: {DateFormat}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Attribute to mark properties for special processing or grouping
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SpecialPropertyAttribute : Attribute
    {
        public string Category { get; set; }

        public SpecialPropertyAttribute(string category = "Default")
        {
            Category = category;
        }
    }

    public class ProductDto
    {
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("ean")]
        public string Ean { get; set; } = string.Empty;

        [JsonPropertyName("bloz7")]
        public string Bloz7 { get; set; } = string.Empty;

        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("baseUom")]
        public string BaseUom { get; set; } = string.Empty;

        [JsonPropertyName("isExpirationDateRequired")]
        public bool IsExpirationDateRequired { get; set; }

        [JsonPropertyName("isSeriesNumberRequired")]
        public bool IsSeriesNumberRequired { get; set; }

        [JsonPropertyName("isQualityControlRequired")]
        [SpecialProperty("ProductFlags")]
        public bool IsQualityControlRequired { get; set; }

        [JsonPropertyName("isRefrigeratedStorage")]
        [SpecialProperty("ProductFlags")]
        public bool IsRefrigeratedStorage { get; set; }

        [JsonPropertyName("isRefrigeratedTransport")]
        [SpecialProperty("ProductFlags")]
        public bool IsRefrigeratedTransport { get; set; }

        [JsonPropertyName("isFragile")]
        [SpecialProperty("ProductFlags")]
        public bool IsFragile { get; set; }

        [JsonPropertyName("isDiscounted")]
        [SpecialProperty("ProductFlags")]
        public bool IsDiscounted { get; set; }

        [JsonPropertyName("isNotForReception")]
        [SpecialProperty("ProductFlags")]
        public bool IsNotForReception { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("parentArticleSKU")]
        [SpecialProperty("ProductFlags")]
        public string ParentArticleSKU { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("dimensions")]
        public ProductDimensionsDto Dimensions { get; set; } = new();

        [JsonPropertyName("packageQuantity")]
        [SpecialProperty("ProductFlags")]
        public decimal PackageQuantity { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("producer")]
        [SpecialProperty("ProductFlags")]
        public string Producer { get; set; } = string.Empty;

        [JsonPropertyName("lastModifyDateTime")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime LastModifyDateTime { get; set; }

        [JsonPropertyName("vatRate")]
        public string VatRate { get; set; } = string.Empty;

        [JsonPropertyName("supervisor")]
        [SpecialProperty("ProductFlags")]
        public string Supervisor { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [SpecialProperty("ProductFlags")]
        public string? Type { get; set; }

        [JsonPropertyName("isPackage")]
        [SpecialProperty("ProductFlags")]
        public bool IsPackage { get; set; }

        public int XlItemId { get; set; }
    }
    
    public class ProductDimensionsDto
    {
        [JsonPropertyName("x")]
        public decimal X { get; set; }
        
        [JsonPropertyName("y")]
        public decimal Y { get; set; }
        
        [JsonPropertyName("z")]
        public decimal Z { get; set; }
    }
}
