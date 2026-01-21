using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Helpers;
using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Services.Webhook;
using Prospeo.DbContext.Interfaces;
using Prospeo.DTOs.Webhook;
using SecureWebhook;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Controller do obs³ugi webhooków z API Olmed.
    /// Obs³uguje deszyfracjê, weryfikacjê i zapisywanie danych webhook do plików.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly SecureWebhookHelper _helper;
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _webhookDataDirectory;
        private readonly FileLoggingService _fileLoggingService;
        private readonly IWebhookDataParser _webhookDataParser;
        private readonly IWebhookProcessingOrchestrator _processingOrchestrator;
        private readonly string secureKey = Environment.GetEnvironmentVariable("PROSPEO_KEY") ?? "CPNFWqXE3TMY925xMgUPlUnWkjSyo9182PpYM69HM44=";

        /// <summary>
        /// Inicjalizuje now¹ instancjê WebhookController.
        /// </summary>
        public WebhookController(
            IConfiguration configuration,
            ILogger<WebhookController> logger,
            FileLoggingService fileLoggingService,
            IWebhookDataParser webhookDataParser,
            IWebhookProcessingOrchestrator processingOrchestrator)
        {
            var encryptionKey = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedDataBus:WebhookKeys:EncryptionKey"], secureKey) ?? string.Empty;
            var hmacKey = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedDataBus:WebhookKeys:HmacKey"], secureKey) ?? string.Empty;
            _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
            _logger = logger;
            _configuration = configuration;
            _fileLoggingService = fileLoggingService;
            _webhookDataParser = webhookDataParser;
            _processingOrchestrator = processingOrchestrator;

            // Tworzenie katalogu dla zapisywania danych webhook
            _webhookDataDirectory = _configuration["WebhookStorage:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "WebhookData");
            EnsureWebhookDirectoryExists();
        }

        /// <summary>
        /// Odbiera i przetwarza webhook z API Olmed.
        /// Weryfikuje podpis, deszyfruje dane i zapisuje je do plików.
        /// </summary>
        /// <param name="payload">Dane webhook z zaszyfrowan¹ zawartoœci¹</param>
        /// <param name="signature">Podpis HMAC do weryfikacji autentycznoœci</param>
        /// <returns>Wynik przetwarzania webhook</returns>
        /// <response code="200">Webhook zosta³ pomyœlnie przetworzony</response>
        /// <response code="400">B³¹d weryfikacji podpisu lub brak wymaganego nag³ówka</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> Receive(
            [FromBody] WebhookPayload payload,
            [FromHeader(Name = "X-OLMED-ERP-API-SIGNATURE")] string signature)
        {
            var processingStartTime = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(signature))
            {
                var errorMessage = "Brak nag³ówka X-OLMED-ERP-API-SIGNATURE";
                _logger.LogWarning("Odrzucono webhook - {Error} - GUID: {Guid}", errorMessage, payload?.guid);

                await _fileLoggingService.LogAsync("webhook", LogLevel.Warning, errorMessage, null, new
                {
                    Guid = payload?.guid,
                    WebhookType = payload?.webhookType,
                    ProcessingTime = DateTime.UtcNow - processingStartTime
                });

                return BadRequest(errorMessage);
            }

            _logger.LogInformation("Otrzymano webhook - GUID: {Guid}, Typ: {Type}", payload.guid, payload.webhookType);

            // Strukturalne logowanie otrzymania webhook do pliku
            await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook received", new
            {
                Guid = payload.guid,
                WebhookType = payload.webhookType,
                ReceivedAt = processingStartTime,
                SignaturePresent = true,
                PayloadSize = payload.webhookData?.Length ?? 0
            });

            // Próba deszyfracji i weryfikacji
            if (_helper.TryDecryptAndVerifyWithIvPrefix(payload.guid, payload.webhookType, payload.webhookData, signature, out var json))
            {
                _logger.LogInformation("Pomyœlnie odszyfrowano webhook - GUID: {Guid}", payload.guid);

                try
                {
                    var processingSuccess = await ProcessWebhookDataAsync(payload.guid, payload.webhookType, json);
                    
                    if (!processingSuccess)
                    {
                        _logger.LogError("Przetwarzanie webhook zakoñczone niepowodzeniem - GUID: {Guid}", payload.guid);
                        
                        await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                            "Przetwarzanie webhook zakoñczone niepowodzeniem", null, new
                            {
                                Guid = payload.guid,
                                WebhookType = payload.webhookType,
                                ProcessingTime = DateTime.UtcNow - processingStartTime
                            });

                        return BadRequest(new
                        {
                            success = false,
                            error = "B³¹d podczas przetwarzania webhook",
                            message = "Nie uda³o siê przetworzyæ danych webhook",
                            guid = payload.guid
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "B³¹d podczas przetwarzania webhook - GUID: {Guid}", payload.guid);
                    
                    await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                        "Wyj¹tek podczas przetwarzania webhook", ex, new
                        {
                            Guid = payload.guid,
                            WebhookType = payload.webhookType,
                            ProcessingTime = DateTime.UtcNow - processingStartTime
                        });

                    return BadRequest(new
                    {
                        success = false,
                        error = "B³¹d podczas przetwarzania webhook",
                        message = ex.Message,
                        guid = payload.guid
                    });
                }

                var processingTime = DateTime.UtcNow - processingStartTime;

                // Strukturalne logowanie sukcesu do pliku
                await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook processed successfully", new
                {
                    Guid = payload.guid,
                    WebhookType = payload.webhookType,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = processingTime.TotalMilliseconds,
                    DecryptionSuccess = true
                });

                return Ok(new
                {
                    success = true,
                    message = "Webhook przetworzony pomyœlnie",
                    guid = payload.guid
                });
            }

            var decryptionErrorMessage = "Nie uda³o siê zweryfikowaæ lub odszyfrowaæ webhook";
            _logger.LogError("{Error} - GUID: {Guid}", decryptionErrorMessage, payload.guid);

            // Logowanie b³êdu deszyfracji do pliku
            await _fileLoggingService.LogAsync("webhook", LogLevel.Error, decryptionErrorMessage, null, new
            {
                Guid = payload.guid,
                WebhookType = payload.webhookType,
                SignatureLength = signature?.Length ?? 0,
                PayloadSize = payload.webhookData?.Length ?? 0,
                ProcessingTime = DateTime.UtcNow - processingStartTime
            });

            return BadRequest(decryptionErrorMessage);
        }

        /// <summary>
        /// Przetwarza odszyfrowane dane webhook u¿ywaj¹c wzorca Strategy + Chain of Responsibility
        /// </summary>
        /// <returns>True jeœli przetwarzanie zakoñczone sukcesem, False w przeciwnym razie</returns>
        private async Task<bool> ProcessWebhookDataAsync(string guid, string webhookType, string decryptedJson)
        {
            try
            {
                // Krok 1: Parsuj dane webhook
                var parseResult = await _webhookDataParser.ParseAsync(decryptedJson, webhookType);

                // Krok 2: Przygotuj kontekst przetwarzania
                var context = new WebhookProcessingContext
                {
                    Guid = guid,
                    WebhookType = webhookType,
                    ChangeType = parseResult.ChangeType,
                    DecryptedJson = decryptedJson,
                    DefaultFirmaId = _configuration.GetValue<int>("Queue:DefaultFirmaId", 1),
                    SecondFirmaId = _configuration.GetValue<int>("Queue:SecondFirmaId", 1),
                    ParseResult = parseResult
                };

                // Krok 3: U¿yj orchestratora do przetworzenia webhook
                var result = await _processingOrchestrator.ProcessWebhookAsync(context);

                if (!result.Success)
                {
                    _logger.LogError("Nie uda³o siê przetworzyæ webhook - GUID: {Guid}, Error: {Error}",
                        guid, result.ErrorMessage);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas przetwarzania danych webhook - GUID: {Guid}", guid);
                throw; // Re-throw aby by³ przechwycony w Receive()
            }
        }

        /// <summary>
        /// Zapewnia istnienie katalogu do przechowywania danych webhook.
        /// Tworzy katalog jeœli nie istnieje.
        /// </summary>
        private void EnsureWebhookDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_webhookDataDirectory))
                {
                    Directory.CreateDirectory(_webhookDataDirectory);
                    _logger.LogInformation("Utworzono katalog webhook: {Directory}", _webhookDataDirectory);

                    // Logowanie do pliku
                    _ = Task.Run(async () => await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                        "Utworzono katalog webhook", null, new { Directory = _webhookDataDirectory }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia katalogu webhook: {Directory}", _webhookDataDirectory);

                // Logowanie b³êdu do pliku
                _ = Task.Run(async () => await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas tworzenia katalogu webhook", ex, new { Directory = _webhookDataDirectory }));
            }
        }

        /// <summary>
        /// Endpoint testowy do weryfikacji dzia³ania controllera.
        /// </summary>
        /// <returns>Status kontrolera</returns>
        /// <response code="200">Controller dzia³a poprawnie</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> HealthCheck()
        {
            // Logowanie health check do pliku
            await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                "Health check wykonany", null, new
                {
                    Endpoint = "webhook/health",
                    WebhookDirectory = _webhookDataDirectory,
                    DirectoryExists = Directory.Exists(_webhookDataDirectory)
                });

            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                WebhookDirectory = _webhookDataDirectory,
                DirectoryExists = Directory.Exists(_webhookDataDirectory)
            });
        }

        /// <summary>
        /// Testuje po³¹czenie z baz¹ danych, aby zweryfikowaæ, ¿e Microsoft.Data.SqlClient dzia³a poprawnie.
        /// </summary>
        /// <returns>Status po³¹czenia z baz¹ danych</returns>
        /// <response code="200">Po³¹czenie z baz¹ danych dzia³a poprawnie</response>
        /// <response code="500">B³¹d po³¹czenia z baz¹ danych</response>
        [HttpGet("test/database")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var connectionString = StringEncryptionHelper.DecryptIfEncrypted(_configuration.GetConnectionString("DefaultConnection"), secureKey);

                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Connection string nie jest skonfigurowany"
                    });
                }

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Wykonaj proste zapytanie testowe
                using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1 as TestResult", connection);
                var result = await command.ExecuteScalarAsync();

                await _fileLoggingService.LogAsync("webhook", LogLevel.Information,
                    "Test po³¹czenia z baz¹ danych zakoñczony sukcesem", null, new
                    {
                        ConnectionString = connectionString.Replace(connectionString?.Split("Password=")[1]?.Split(";")[0] ?? "", "***"),
                        TestResult = result
                    });

                return Ok(new
                {
                    Success = true,
                    Message = "Po³¹czenie z baz¹ danych dzia³a poprawnie",
                    TestResult = result,
                    SqlClientVersion = typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.GetName().Version?.ToString(),
                    ConnectionState = connection.State.ToString(),
                    ServerVersion = connection.ServerVersion,
                    Database = connection.Database
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas testowania po³¹czenia z baz¹ danych");

                await _fileLoggingService.LogAsync("webhook", LogLevel.Error,
                    "B³¹d podczas testowania po³¹czenia z baz¹ danych", ex);

                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    InnerException = ex.InnerException?.Message
                });
            }
        }
    }
}
