using Prospeo.DTOs.Invoice;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Text.Json;

namespace Prosepo.Webhooks.Services.Webhook.Parsing;

public class FallbackParseHandler : IWebhookParseHandler
{
    public bool TryParse(
        JsonElement root,
        string decryptedJson,
        string webhookType,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        WebhookParseResult result)
    {
        try
        {
            result.ProductData = JsonSerializer.Deserialize<ProductDto>(decryptedJson, jsonOptions);
            if (result.ProductData?.Sku != null)
            {
                logger.LogDebug("Deserializowano jako ProductDto (fallback)");
                return true;
            }
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Nie udało się deserializować jako ProductDto (fallback)");
        }

        try
        {
            result.OrderData = JsonSerializer.Deserialize<OrderDto>(decryptedJson, jsonOptions);
            if (result.OrderData?.Number != null)
            {
                logger.LogDebug("Deserializowano jako OrderDto (fallback)");
                return true;
            }
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Nie udało się deserializować jako OrderDto (fallback)");
        }

        try
        {
            result.MarketingInvoiceData = JsonSerializer.Deserialize<MarketingInvoiceDto>(decryptedJson, jsonOptions);
            if (result.MarketingInvoiceData?.Number != null)
            {
                logger.LogDebug("Deserializowano jako MarketingInvoiceDto (fallback)");
                return true;
            }
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Nie udało się deserializować jako MarketingInvoiceDto (fallback)");
        }

        return false;
    }
}