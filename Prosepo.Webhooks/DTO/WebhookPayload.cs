namespace Prosepo.Webhooks.DTO
{
    /// <summary>
    /// Represents the payload structure for webhook data transfer.
    /// </summary>
    public class WebhookPayload
    {
        /// <summary>
        /// Gets or sets the unique identifier for the webhook payload.
        /// </summary>
        /// <value>A GUID string that uniquely identifies this webhook payload instance.</value>
        public string guid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the webhook.
        /// </summary>
        /// <value>A string indicating the category or type of webhook event.</value>
        public string webhookType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data content of the webhook.
        /// </summary>
        /// <value>A string containing the serialized data payload for the webhook.</value>
        public string webhookData { get; set; } = string.Empty;
    }
}
