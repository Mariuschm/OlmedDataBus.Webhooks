using Prospeo.DTOs.Invoice;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Text.Json;

namespace Prosepo.Webhooks.Services.Webhook.Parsing;

public class WebhookTypeHintParseHandler : IWebhookParseHandler
{
    public bool TryParse(
        JsonElement root,
        string decryptedJson,
        string webhookType,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        WebhookParseResult result)
    {
        if (webhookType?.Contains("product", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                result.ProductData = JsonSerializer.Deserialize<ProductDto>(decryptedJson, jsonOptions);
                if (result.ProductData != null)
                {
                    logger.LogDebug("Deserializowano jako ProductDto na podstawie webhookType");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Nie udało się deserializować jako ProductDto");
            }
        }

        if (webhookType?.Contains("order", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                result.OrderData = JsonSerializer.Deserialize<OrderDto>(decryptedJson, jsonOptions);
                if (result.OrderData != null)
                {
                    logger.LogDebug("Deserializowano jako OrderDto na podstawie webhookType");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Nie udało się deserializować jako OrderDto");
            }
        }

        if (webhookType?.Contains("marketinginvoice", StringComparison.OrdinalIgnoreCase) == true ||
            webhookType?.Contains("invoice", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                result.MarketingInvoiceData = JsonSerializer.Deserialize<MarketingInvoiceDto>(decryptedJson, jsonOptions);
                if (result.MarketingInvoiceData != null)
                {
                    logger.LogDebug("Deserializowano jako MarketingInvoiceDto na podstawie webhookType");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Nie udało się deserializować jako MarketingInvoiceDto");
            }
        }

        return false;
    }
}