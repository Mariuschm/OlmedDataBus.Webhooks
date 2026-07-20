using Prosepo.Webhooks.Helpers;
using Prosepo.Webhooks.Services.Webhook.Parsing;
using Prospeo.DTOs.Core;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prosepo.Webhooks.Services.Webhook
{
    public class WebhookDataParser : IWebhookDataParser
    {
        private readonly ILogger<WebhookDataParser> _logger;
        private readonly IEnumerable<IWebhookParseHandler> _handlers;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebhookDataParser(ILogger<WebhookDataParser> logger, IEnumerable<IWebhookParseHandler> handlers)
        {
            _logger = logger;
            _handlers = handlers;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new DTOModelBase.CustomDateTimeConverter() }
            };
        }

        public Task<WebhookParseResult> ParseAsync(string decryptedJson, string webhookType)
        {
            var result = new WebhookParseResult();

            try
            {
                using var document = JsonDocument.Parse(decryptedJson);
                var root = document.RootElement;

                if (root.TryGetProperty("changeType", out var changeTypeElement))
                {
                    result.ChangeType = changeTypeElement.GetString();
                }

                foreach (var handler in _handlers)
                {
                    if (handler.TryParse(root, decryptedJson, webhookType, _jsonOptions, _logger, result))
                    {
                        return Task.FromResult(result);
                    }
                }

                _logger.LogWarning("Nie rozpoznano typu danych webhook");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas parsowania danych webhook");
                throw;
            }
        }
    }
}
