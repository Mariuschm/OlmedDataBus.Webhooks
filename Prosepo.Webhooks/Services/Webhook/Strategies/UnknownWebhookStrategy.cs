using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;

namespace Prosepo.Webhooks.Services.Webhook.Strategies
{
    /// <summary>
    /// Strategia przetwarzania nierozpoznanych webhooków
    /// </summary>
    public class UnknownWebhookStrategy : IWebhookProcessingStrategy
    {
        private readonly IQueueService _queueService;
        private readonly IFirmyService? _firmyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnknownWebhookStrategy> _logger;
        private readonly FileLoggingService _fileLoggingService;

        public string StrategyName => "UnknownWebhook";

        public UnknownWebhookStrategy(
            IQueueService queueService,
            IFirmyService? firmyService,
            IConfiguration configuration,
            ILogger<UnknownWebhookStrategy> logger,
            FileLoggingService fileLoggingService)
        {
            _queueService = queueService;
            _firmyService = firmyService;
            _configuration = configuration;
            _logger = logger;
            _fileLoggingService = fileLoggingService;
        }

        public bool CanProcess(WebhookProcessingContext context)
        {
            // Ta strategia obs³uguje wszystko co nie zosta³o rozpoznane
            return !context.ParseResult.IsRecognized;
        }

        public async Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context)
        {
            var result = new WebhookProcessingResult { Success = true };

            try
            {
                _logger.LogWarning(
                    "Webhook nie zawiera rozpoznawalnych danych ProductDto ani OrderDto - GUID: {Guid}, WebhookType: {WebhookType}",
                    context.Guid, context.WebhookType);

                var queueItem = await CreateUnknownQueueItemAsync(context);
                result.CreatedQueueItems.Add(queueItem);

                // Logowanie
                var companyName = await GetCompanyNameAsync(context.DefaultFirmaId);
                _logger.LogInformation(
                    "Dodano nieznane dane webhook do kolejki - GUID: {Guid}, WebhookType: {WebhookType}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}",
                    context.Guid, context.WebhookType, queueItem.Id, companyName, context.ChangeType ?? "N/A");

                await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                    "Dodano nieznane dane webhook do kolejki", null, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        QueueId = queueItem.Id,
                        QueueScope = -1,
                        Company = companyName,
                        CompanyId = context.DefaultFirmaId,
                        ChangeType = context.ChangeType,
                        RawDataSize = context.DecryptedJson?.Length ?? 0
                    });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "B³¹d podczas przetwarzania UnknownWebhook - GUID: {Guid}, WebhookType: {WebhookType}",
                    context.Guid, context.WebhookType);

                await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas przetwarzania UnknownWebhook", ex, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType
                    });

                throw;
            }

            return result;
        }

        private async Task<Queue> CreateUnknownQueueItemAsync(WebhookProcessingContext context)
        {
            var queueItem = new Queue
            {
                FirmaId = context.DefaultFirmaId,
                Scope = -1, // Scope dla nieznanych webhooków
                Request = string.Empty, // Request jest pusty dla nieznanych danych
                Description = "",
                TargetID = 0,
                Flg = -1, // Flag dla nieznanych webhooków
                DateAddDateTime = DateTime.UtcNow,
                DateModDateTime = DateTime.UtcNow,
                WebhookRawData = context.DecryptedJson,
                ChangeType = context.ChangeType ?? string.Empty
            };

            return await _queueService.AddAsync(queueItem);
        }

        private async Task<string> GetCompanyNameAsync(int companyId)
        {
            if (_firmyService == null) return "Unknown";

            var company = await _firmyService.GetByIdAsync(companyId);
            return company?.NazwaFirmy ?? "Unknown";
        }
    }
}
