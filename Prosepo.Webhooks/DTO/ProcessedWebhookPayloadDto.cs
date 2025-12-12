using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.DTO
{
    /// <summary>
    /// Represents a processed webhook payload with decrypted data.
    /// </summary>
    public class ProcessedWebhookPayloadDto
    {
        /// <summary>
        /// Gets or sets the timestamp when the webhook was processed.
        /// </summary>
        [JsonPropertyName("ProcessedAt")]
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the webhook payload.
        /// </summary>
        [JsonPropertyName("Guid")]
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the webhook.
        /// </summary>
        [JsonPropertyName("WebhookType")]
        public string WebhookType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing status of the webhook.
        /// </summary>
        [JsonPropertyName("ProcessingStatus")]
        public string ProcessingStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the decrypted data content of the webhook.
        /// The content varies based on the WebhookType.
        /// </summary>
        [JsonPropertyName("DecryptedData")]
        public DecryptedDataDto DecryptedData { get; set; } = new();
    }

    /// <summary>
    /// Represents the decrypted data section of a processed webhook.
    /// Contains variable content based on the webhook type.
    /// </summary>
    public class DecryptedDataDto
    {
        /// <summary>
        /// Gets or sets the account name.
        /// </summary>
        [JsonPropertyName("accountName")]
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product data (when WebhookType is product-related).
        /// </summary>
        [JsonPropertyName("productData")]
        public ProductDto? ProductData { get; set; }

        /// <summary>
        /// Gets or sets the order data (when WebhookType is order-related).
        /// </summary>
        [JsonPropertyName("orderData")]
        public OrderDto? OrderData { get; set; }

        /// <summary>
        /// Gets or sets the change type for the webhook (e.g., "products", "orders").
        /// </summary>
        [JsonPropertyName("changeType")]
        public string? ChangeType { get; set; }
    }
}