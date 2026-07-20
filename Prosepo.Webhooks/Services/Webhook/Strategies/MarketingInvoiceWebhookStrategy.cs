using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prosepo.Webhooks.Services.Webhook.Strategies
{
    /// <summary>
    /// Strategia przetwarzania webhooków z danymi faktur marketingowych
    /// </summary>
    public class MarketingInvoiceWebhookStrategy : IWebhookProcessingStrategy
    {
        private readonly IQueueService _queueService;
        private readonly IFirmyService? _firmyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarketingInvoiceWebhookStrategy> _logger;
        private readonly FileLoggingService _fileLoggingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public string StrategyName => "MarketingInvoiceWebhook";

        public MarketingInvoiceWebhookStrategy(
            IQueueService queueService,
            IFirmyService? firmyService,
            IConfiguration configuration,
            ILogger<MarketingInvoiceWebhookStrategy> logger,
            FileLoggingService fileLoggingService)
        {
            _queueService = queueService;
            _firmyService = firmyService;
            _configuration = configuration;
            _logger = logger;
            _fileLoggingService = fileLoggingService;
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
        }

        public bool CanProcess(WebhookProcessingContext context)
        {
            return context.ParseResult.MarketingInvoiceData != null;
        }

        public async Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context)
        {
            var result = new WebhookProcessingResult { Success = true };
            var invoiceData = context.ParseResult.MarketingInvoiceData!;

            try
            {
                var invoiceScope = _configuration.GetValue<int>("Queue:MarketingInvoiceScope", 18);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                // Okreœl docelow¹ firmê na podstawie marketplace
                var targetCompanyId = DetermineTargetCompanyId(invoiceData.Marketplace, context);

                var queueItem = await CreateMarketingInvoiceQueueItemAsync(
                    context, invoiceData, targetCompanyId, invoiceScope, webhookProcessingFlag);

                result.CreatedQueueItems.Add(queueItem);

                // Logowanie
                var companyName = await GetCompanyNameAsync(targetCompanyId);
                _logger.LogInformation(
                    "Dodano MarketingInvoiceDto do kolejki - GUID: {Guid}, InvoiceNumber: {InvoiceNumber}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}",
                    context.Guid, invoiceData.Number, queueItem.Id, companyName, context.ChangeType ?? "N/A");

                await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                    "Dodano MarketingInvoiceDto do kolejki", null, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        InvoiceNumber = invoiceData.Number,
                        InvoiceMarketplace = invoiceData.Marketplace,
                        ValueNet = invoiceData.ValueNet,
                        Quarter = invoiceData.Quarter,
                        QueueId = queueItem.Id,
                        QueueScope = queueItem.Scope,
                        Company = companyName,
                        CompanyId = targetCompanyId,
                        ChangeType = context.ChangeType
                    });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "B³¹d podczas przetwarzania MarketingInvoiceWebhook - GUID: {Guid}, InvoiceNumber: {InvoiceNumber}",
                    context.Guid, invoiceData?.Number);

                await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas przetwarzania MarketingInvoiceWebhook", ex, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        InvoiceNumber = invoiceData?.Number,
                        ErrorMessage = ex.Message
                    });
            }

            return result;
        }

        private int DetermineTargetCompanyId(string? marketplace, WebhookProcessingContext context)
        {
            if (string.IsNullOrEmpty(marketplace))
            {
                _logger.LogWarning("Marketplace jest pusty dla GUID: {Guid}, u¿ywam domyœlnej firmy", context.Guid);
                return context.DefaultFirmaId;
            }

            // Logika routingu - mo¿na rozszerzyæ w przysz³oœci
            if (marketplace.Contains("ZAWISZA", StringComparison.CurrentCultureIgnoreCase))
            {
                return context.SecondFirmaId;
            }

            return context.DefaultFirmaId;
        }

        private async Task<Queue> CreateMarketingInvoiceQueueItemAsync(
            WebhookProcessingContext context,
            Prospeo.DTOs.Invoice.MarketingInvoiceDto invoiceData,
            int targetCompanyId,
            int invoiceScope,
            int webhookProcessingFlag)
        {
            var resolvedScope = ResolveMarketingScope(context.WebhookType, invoiceScope);

            var queueItem = new Queue
            {
                FirmaId = targetCompanyId,
                Scope = resolvedScope,
                Request = JsonSerializer.Serialize(invoiceData, _jsonOptions),
                Description = "",
                TargetID = 0,
                Flg = webhookProcessingFlag,
                DateAddDateTime = DateTime.UtcNow,
                DateModDateTime = DateTime.UtcNow,
                WebhookRawData = context.DecryptedJson,
                ChangeType = context.WebhookType ?? string.Empty
            };

            return await _queueService.AddAsync(queueItem);
        }
        /// <summary>
        /// Rozwi¹zuje odpowiedni zakres dla webhooków marketingowych na podstawie typu webhooka
        /// </summary>
        /// <param name="webhookType">Typ webhooka</param>
        /// <param name="defaultScope">Domyœlny zakres</param>
        /// <returns>Rozwi¹zany zakres</returns>
        private static int ResolveMarketingScope(string? webhookType, int defaultScope)
        {
            return webhookType?.Trim() switch
            {
                "New marketing order placed" => 18,
                "Marketing order deleted" => 20,
                "Marketing order edited" => 21,
                _ => defaultScope
            };
        }

        private async Task<string> GetCompanyNameAsync(int companyId)
        {
            if (_firmyService == null)
            {
                return $"CompanyId: {companyId}";
            }

            try
            {
                var company = await _firmyService.GetByIdAsync(companyId);
                return company?.NazwaFirmy ?? $"CompanyId: {companyId}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie mo¿na pobraæ nazwy firmy dla CompanyId: {CompanyId}", companyId);
                return $"CompanyId: {companyId}";
            }
        }
    }
}
