using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prosepo.Webhooks.Services.Webhook.Strategies
{
    /// <summary>
    /// Strategia przetwarzania webhooków z danymi zamówieñ
    /// </summary>
    public class OrderWebhookStrategy : IWebhookProcessingStrategy
    {
        private readonly IQueueService _queueService;
        private readonly IFirmyService? _firmyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderWebhookStrategy> _logger;
        private readonly FileLoggingService _fileLoggingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public string StrategyName => "OrderWebhook";

        public OrderWebhookStrategy(
            IQueueService queueService,
            IFirmyService? firmyService,
            IConfiguration configuration,
            ILogger<OrderWebhookStrategy> logger,
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
            return context.ParseResult.OrderData != null;
        }

        public async Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context)
        {
            var result = new WebhookProcessingResult { Success = true };
            var orderData = context.ParseResult.OrderData!;

            try
            {
                var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                // Okreœl docelow¹ firmê na podstawie marketplace
                var targetCompanyId = DetermineTargetCompanyId(orderData.Marketplace, context);

                var queueItem = await CreateOrderQueueItemAsync(
                    context, orderData, targetCompanyId, orderScope, webhookProcessingFlag);

                result.CreatedQueueItems.Add(queueItem);

                // Logowanie
                var companyName = await GetCompanyNameAsync(targetCompanyId);
                _logger.LogInformation(
                    "Dodano OrderDto do kolejki - GUID: {Guid}, OrderNumber: {OrderNumber}, OrderID: {OrderId}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}",
                    context.Guid, orderData.Number, orderData.Id, queueItem.Id, companyName, context.ChangeType ?? "N/A");

                await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                    "Dodano OrderDto do kolejki", null, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        OrderNumber = orderData.Number,
                        OrderId = orderData.Id,
                        OrderMarketplace = orderData.Marketplace,
                        OrderItemsCount = orderData.OrderItems?.Count ?? 0,
                        QueueId = queueItem.Id,
                        QueueScope = orderScope,
                        Company = companyName,
                        CompanyId = targetCompanyId,
                        ChangeType = context.ChangeType
                    });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "B³¹d podczas przetwarzania OrderWebhook - GUID: {Guid}, OrderNumber: {OrderNumber}",
                    context.Guid, orderData?.Number);

                await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas przetwarzania OrderWebhook", ex, new
                    {
                        Guid = context.Guid,
                        WebhookType = context.WebhookType,
                        OrderNumber = orderData?.Number
                    });

                throw;
            }

            return result;
        }

        private int DetermineTargetCompanyId(string marketplace, WebhookProcessingContext context)
        {
            // Logika routingu - zamówienia ZAWISZA id¹ do SecondFirmaId
            if (marketplace.Contains("ZAWISZA", StringComparison.CurrentCultureIgnoreCase))
            {
                return context.SecondFirmaId;
            }

            return context.DefaultFirmaId;
        }

        private async Task<Queue> CreateOrderQueueItemAsync(
            WebhookProcessingContext context,
            Prospeo.DTOs.Order.OrderDto orderData,
            int companyId,
            int scope,
            int flag)
        {
            var queueItem = new Queue
            {
                FirmaId = companyId,
                Scope = scope,
                Request = JsonSerializer.Serialize(orderData, _jsonOptions),
                Description = "",
                TargetID = 0,
                Flg = flag,
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
