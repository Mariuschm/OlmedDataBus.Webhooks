using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prosepo.Webhooks.Services.Webhook.Strategies
{
    /// <summary>
    /// Strategia przetwarzania webhooków z danymi produktów
    /// </summary>
    public class ProductWebhookStrategy : IWebhookProcessingStrategy
    {
        private readonly IQueueService _queueService;
        private readonly IFirmyService? _firmyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductWebhookStrategy> _logger;
        private readonly FileLoggingService _fileLoggingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public string StrategyName => "ProductWebhook";

        public ProductWebhookStrategy(
            IQueueService queueService,
            IFirmyService? firmyService,
            IConfiguration configuration,
            ILogger<ProductWebhookStrategy> logger,
            FileLoggingService fileLoggingService)
        {
            _queueService = queueService;
            _firmyService = firmyService;
            _configuration = configuration;
            _logger = logger;
            _fileLoggingService = fileLoggingService;
            
            // Konfiguruj JsonSerializerOptions zgodnie z .NET 9 requirements
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
        }

        public bool CanProcess(WebhookProcessingContext context)
        {
            return context.ParseResult.ProductData != null;
        }

        public async Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context)
        {
            var result = new WebhookProcessingResult { Success = true };
            var productData = context.ParseResult.ProductData!;

            try
            {
                var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                // Produkty dodajemy do obydwu firm
                var companyIds = new[] { context.DefaultFirmaId, context.SecondFirmaId };

                foreach (var companyId in companyIds)
                {
                    var queueItem = await CreateProductQueueItemAsync(
                        context, productData, companyId, productScope, webhookProcessingFlag);

                    result.CreatedQueueItems.Add(queueItem);

                    // Logowanie
                    var companyName = await GetCompanyNameAsync(companyId);
                    _logger.LogInformation(
                        "Dodano ProductDto do kolejki - GUID: {Guid}, SKU: {Sku}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}",
                        context.Guid, productData.Sku, queueItem.Id, companyName, context.ChangeType ?? "N/A");

                    await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                        "Dodano ProductDto do kolejki", null, new
                        {
                            Guid = context.Guid,
                            WebhookType = context.WebhookType,
                            ProductSku = productData.Sku,
                            ProductName = productData.Name,
                            ProductId = productData.Id,
                            QueueId = queueItem.Id,
                            QueueScope = productScope,
                            Company = companyName,
                            CompanyId = companyId,
                            ChangeType = context.ChangeType
                        });
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "B³¹d podczas przetwarzania ProductWebhook - GUID: {Guid}, SKU: {Sku}",
                    context.Guid, productData?.Sku);

                await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas przetwarzania ProductWebhook", ex, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        ProductSku = productData?.Sku
                    });

                throw;
            }

            return result;
        }

        private async Task<Queue> CreateProductQueueItemAsync(
            WebhookProcessingContext context,
            Prospeo.DTOs.Product.ProductDto productData,
            int companyId,
            int scope,
            int flag)
        {
            var queueItem = new Queue
            {
                FirmaId = companyId,
                Scope = scope,
                Request = JsonSerializer.Serialize(productData, _jsonOptions),
                Description = "",
                TargetID = 0,
                Flg = flag,
                DateAddDateTime = DateTime.UtcNow,
                DateModDateTime = DateTime.UtcNow,
                ChangeType = context.WebhookType ?? string.Empty,
                WebhookRawData = context.DecryptedJson
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
