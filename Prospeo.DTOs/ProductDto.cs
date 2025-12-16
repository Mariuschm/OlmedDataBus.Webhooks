using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

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
        public bool IsQualityControlRequired { get; set; }

        [JsonPropertyName("isRefrigeratedStorage")]
        public bool IsRefrigeratedStorage { get; set; }

        [JsonPropertyName("isRefrigeratedTransport")]
        public bool IsRefrigeratedTransport { get; set; }

        [JsonPropertyName("isFragile")]
        public bool IsFragile { get; set; }

        [JsonPropertyName("isDiscounted")]
        public bool IsDiscounted { get; set; }

        [JsonPropertyName("isNotForReception")]
        public bool IsNotForReception { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("parentArticleSKU")]
        public string ParentArticleSKU { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("dimensions")]
        public ProductDimensionsDto Dimensions { get; set; } = new();

        [JsonPropertyName("packageQuantity")]
        public decimal PackageQuantity { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("producer")]
        public string Producer { get; set; } = string.Empty;

        [JsonPropertyName("lastModifyDateTime")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime LastModifyDateTime { get; set; }

        [JsonPropertyName("vatRate")]
        public string VatRate { get; set; } = string.Empty;

        [JsonPropertyName("supervisor")]
        public string Supervisor { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("isPackage")]
        public bool IsPackage { get; set; }
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
