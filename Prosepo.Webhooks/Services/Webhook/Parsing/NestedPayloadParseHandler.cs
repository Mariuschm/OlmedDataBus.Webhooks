using Prospeo.DTOs.Invoice;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Text.Json;

namespace Prosepo.Webhooks.Services.Webhook.Parsing;

public class NestedPayloadParseHandler : IWebhookParseHandler
{
    public bool TryParse(
        JsonElement root,
        string decryptedJson,
        string webhookType,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        WebhookParseResult result)
    {
        if (root.TryGetProperty("productData", out var productDataElement))
        {
            result.ProductData = JsonSerializer.Deserialize<ProductDto>(productDataElement.GetRawText(), jsonOptions);
            logger.LogDebug("Znaleziono zagnieżdżone productData");
            return result.ProductData != null;
        }

        if (root.TryGetProperty("orderData", out var orderDataElement))
        {
            result.OrderData = JsonSerializer.Deserialize<OrderDto>(orderDataElement.GetRawText(), jsonOptions);
            logger.LogDebug("Znaleziono zagnieżdżone orderData");
            return result.OrderData != null;
        }

        if (root.TryGetProperty("marketingOrderData", out var marketingOrderDataElement))
        {
            result.MarketingInvoiceData = JsonSerializer.Deserialize<MarketingInvoiceDto>(marketingOrderDataElement.GetRawText(), jsonOptions);
            logger.LogDebug("Znaleziono zagnieżdżone marketingOrderData");
            return result.MarketingInvoiceData != null;
        }

        return false;
    }
}