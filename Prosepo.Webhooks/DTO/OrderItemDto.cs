using System.Text.Json.Serialization;
namespace Prosepo.Webhooks.DTO
{

    public class OrderItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("sourceIssueDocumentId")]
        public int SourceIssueDocumentId { get; set; }

        [JsonPropertyName("sourceArticleSKU")]
        public string SourceArticleSKU { get; set; } = string.Empty;

        [JsonPropertyName("quantityOrdered")]
        public decimal QuantityOrdered { get; set; }
    }

}
