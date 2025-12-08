using System.Text.Json.Serialization;
namespace Prosepo.Webhooks.DTO
{
  

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
        public DateTime LastModifyDateTime { get; set; }

        [JsonPropertyName("vatRate")]
        public string VatRate { get; set; } = string.Empty;

        [JsonPropertyName("supervisor")]
        public string Supervisor { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
    public class ProductDimensionsDto
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Z { get; set; }
    }
}
