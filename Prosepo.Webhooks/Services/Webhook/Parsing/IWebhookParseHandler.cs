using System.Text.Json;

namespace Prosepo.Webhooks.Services.Webhook.Parsing;

public interface IWebhookParseHandler
{
    bool TryParse(
        JsonElement root,
        string decryptedJson,
        string webhookType,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        WebhookParseResult result);
}