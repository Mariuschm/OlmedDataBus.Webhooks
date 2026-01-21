using Prospeo.DbContext.Models;

namespace Prosepo.Webhooks.Services.Webhook
{
    /// <summary>
    /// Kontekst przetwarzania webhook - zawiera wszystkie dane potrzebne do przetworzenia
    /// </summary>
    public class WebhookProcessingContext
    {
        public string Guid { get; set; } = string.Empty;
        public string WebhookType { get; set; } = string.Empty;
        public string? ChangeType { get; set; }
        public string DecryptedJson { get; set; } = string.Empty;
        public int DefaultFirmaId { get; set; }
        public int SecondFirmaId { get; set; }
        public WebhookParseResult ParseResult { get; set; } = new();
    }

    /// <summary>
    /// Wynik przetwarzania webhook przez strategiê
    /// </summary>
    public class WebhookProcessingResult
    {
        public bool Success { get; set; }
        public List<Queue> CreatedQueueItems { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interfejs strategii przetwarzania webhook
    /// </summary>
    public interface IWebhookProcessingStrategy
    {
        /// <summary>
        /// Sprawdza czy strategia mo¿e przetworzyæ dany kontekst
        /// </summary>
        bool CanProcess(WebhookProcessingContext context);

        /// <summary>
        /// Przetwarza webhook i dodaje odpowiednie wpisy do kolejki
        /// </summary>
        Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context);

        /// <summary>
        /// Nazwa strategii (do logowania)
        /// </summary>
        string StrategyName { get; }
    }
}
