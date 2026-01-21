using Prosepo.Webhooks.Helpers;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prosepo.Webhooks.Services.Webhook
{
    /// <summary>
    /// Parser danych webhook - implementuje Chain of Responsibility
    /// dla ró¿nych strategii parsowania
    /// </summary>
    public class WebhookDataParser : IWebhookDataParser
    {
        private readonly ILogger<WebhookDataParser> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebhookDataParser(ILogger<WebhookDataParser> logger)
        {
            _logger = logger;
            
            // Konfiguruj JsonSerializerOptions zgodnie z .NET 9 requirements
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(), // W³¹cz reflection-based serialization
                Converters = { new CustomDateTimeConverter() }
            };
        }

        public async Task<WebhookParseResult> ParseAsync(string decryptedJson, string webhookType)
        {
            var result = new WebhookParseResult();

            try
            {
                // Parsuj JSON
                using var document = JsonDocument.Parse(decryptedJson);
                var root = document.RootElement;

                // Pobierz changeType jeœli istnieje
                if (root.TryGetProperty("changeType", out var changeTypeElement))
                {
                    result.ChangeType = changeTypeElement.GetString();
                }

                // Strategia 1: SprawdŸ czy zawiera zagnie¿d¿one productData
                if (root.TryGetProperty("productData", out var productDataElement))
                {
                    var productDataJson = productDataElement.GetRawText();
                    result.ProductData = JsonSerializer.Deserialize<ProductDto>(productDataJson, _jsonOptions);
                    _logger.LogDebug("Znaleziono zagnie¿d¿one productData");
                    return result;
                }

                // Strategia 2: SprawdŸ czy zawiera zagnie¿d¿one orderData
                if (root.TryGetProperty("orderData", out var orderDataElement))
                {
                    var orderDataJson = orderDataElement.GetRawText();
                    result.OrderData = JsonSerializer.Deserialize<OrderDto>(orderDataJson, _jsonOptions);
                    _logger.LogDebug("Znaleziono zagnie¿d¿one orderData");
                    return result;
                }

                // Strategia 3: Spróbuj deserializowaæ jako ProductDto na podstawie webhookType
                if (webhookType?.ToLower().Contains("product") == true)
                {
                    try
                    {
                        result.ProductData = JsonSerializer.Deserialize<ProductDto>(decryptedJson, _jsonOptions);
                        if (result.ProductData != null)
                        {
                            _logger.LogDebug("Deserializowano jako ProductDto na podstawie webhookType");
                            return result;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug(ex, "Nie uda³o siê deserializowaæ jako ProductDto");
                    }
                }

                // Strategia 4: Spróbuj deserializowaæ jako OrderDto na podstawie webhookType
                if (webhookType?.ToLower().Contains("order") == true)
                {
                    try
                    {
                        result.OrderData = JsonSerializer.Deserialize<OrderDto>(decryptedJson, _jsonOptions);
                        if (result.OrderData != null)
                        {
                            _logger.LogDebug("Deserializowano jako OrderDto na podstawie webhookType");
                            return result;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug(ex, "Nie uda³o siê deserializowaæ jako OrderDto");
                    }
                }

                // Strategia 5: Spróbuj deserializowaæ jako ProductDto (fallback)
                try
                {
                    result.ProductData = JsonSerializer.Deserialize<ProductDto>(decryptedJson, _jsonOptions);
                    if (result.ProductData?.Sku != null)
                    {
                        _logger.LogDebug("Deserializowano jako ProductDto (fallback)");
                        return result;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Nie uda³o siê deserializowaæ jako ProductDto (fallback)");
                }

                // Strategia 6: Spróbuj deserializowaæ jako OrderDto (fallback)
                try
                {
                    result.OrderData = JsonSerializer.Deserialize<OrderDto>(decryptedJson, _jsonOptions);
                    if (result.OrderData?.Number != null)
                    {
                        _logger.LogDebug("Deserializowano jako OrderDto (fallback)");
                        return result;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Nie uda³o siê deserializowaæ jako OrderDto (fallback)");
                }

                _logger.LogWarning("Nie rozpoznano typu danych webhook");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas parsowania danych webhook");
                throw;
            }
        }
    }
}
