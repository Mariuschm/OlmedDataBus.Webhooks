namespace Prosepo.Webhooks.Services.Webhook
{
    /// <summary>
    /// Orchestrator przetwarzania webhooków - u¿ywa strategii do przetworzenia danych
    /// </summary>
    public interface IWebhookProcessingOrchestrator
    {
        /// <summary>
        /// Przetwarza webhook u¿ywaj¹c odpowiedniej strategii
        /// </summary>
        Task<WebhookProcessingResult> ProcessWebhookAsync(WebhookProcessingContext context);
    }

    /// <summary>
    /// Implementacja orchestratora przetwarzania webhooków
    /// </summary>
    public class WebhookProcessingOrchestrator : IWebhookProcessingOrchestrator
    {
        private readonly IEnumerable<IWebhookProcessingStrategy> _strategies;
        private readonly ILogger<WebhookProcessingOrchestrator> _logger;

        public WebhookProcessingOrchestrator(
            IEnumerable<IWebhookProcessingStrategy> strategies,
            ILogger<WebhookProcessingOrchestrator> logger)
        {
            _strategies = strategies;
            _logger = logger;
        }

        public async Task<WebhookProcessingResult> ProcessWebhookAsync(WebhookProcessingContext context)
        {
            // ZnajdŸ pierwsz¹ strategiê która mo¿e przetworzyæ webhook
            var strategy = _strategies.FirstOrDefault(s => s.CanProcess(context));

            if (strategy == null)
            {
                _logger.LogError("Nie znaleziono strategii do przetworzenia webhook - GUID: {Guid}", context.Guid);
                return new WebhookProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Nie znaleziono odpowiedniej strategii przetwarzania"
                };
            }

            _logger.LogDebug("U¿ywam strategii: {StrategyName} dla webhook GUID: {Guid}",
                strategy.StrategyName, context.Guid);

            return await strategy.ProcessAsync(context);
        }
    }
}
