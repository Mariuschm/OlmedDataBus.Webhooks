using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.DTO;
using SecureWebhook;

namespace Prosepo.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly SecureWebhookHelper _helper;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IConfiguration configuration, ILogger<ProductsController> logger)
        {
            var encryptionKey = configuration["OlmedDataBus:WebhookKeys:EncryptionKey"] ?? string.Empty;
            var hmacKey = configuration["OlmedDataBus:WebhookKeys:HmacKey"] ?? string.Empty;
            _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Receive(
            [FromBody] WebhookPayload payload,
            [FromHeader(Name = "X-OLMED-ERP-API-SIGNATURE")] string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return BadRequest("Brak nag³ówka X-OLMED-ERP-API-SIGNATURE.");

            if (_helper.TryDecryptAndVerifyWithIvPrefix(payload.guid, payload.webhookType, payload.webhookData, signature, out var json))
            {
                _logger.LogInformation("Odszyfrowana zawartoœæ webhooka: {Decrypted}", json);
                return Ok(new { Success = true, Decrypted = json });
            }
            return BadRequest("Invalid signature or decryption failed.");
        }

        public class WebhookPayload
        {
            public string guid { get; set; } = string.Empty;
            public string webhookType { get; set; } = string.Empty;
            public string webhookData { get; set; } = string.Empty;
        }
    }
}
