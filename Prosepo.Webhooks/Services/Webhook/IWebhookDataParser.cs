using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;

namespace Prosepo.Webhooks.Services.Webhook
{
    /// <summary>
    /// Wynik parsowania danych webhook
    /// </summary>
    public class WebhookParseResult
    {
        public ProductDto? ProductData { get; set; }
        public OrderDto? OrderData { get; set; }
        public string? ChangeType { get; set; }
        public bool IsRecognized => ProductData != null || OrderData != null;
    }

    /// <summary>
    /// Interfejs parsera danych webhook
    /// </summary>
    public interface IWebhookDataParser
    {
        /// <summary>
        /// Parsuje odszyfrowane dane JSON webhook
        /// </summary>
        /// <param name="decryptedJson">Odszyfrowane dane JSON</param>
        /// <param name="webhookType">Typ webhook</param>
        /// <returns>Wynik parsowania</returns>
        Task<WebhookParseResult> ParseAsync(string decryptedJson, string webhookType);
    }
}
