using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.DTO;
using Prosepo.Webhooks.Services;
using SecureWebhook;
using System.Text.Json;

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

        /// <summary>
        /// Inicjalizuje now¹ instancjê WebhookController.
        /// </summary>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="fileLoggingService">Serwis logowania do plików</param>
        public WebhookController(IConfiguration configuration, ILogger<WebhookController> logger, FileLoggingService fileLoggingService)
        {
            var encryptionKey = configuration["OlmedDataBus:WebhookKeys:EncryptionKey"] ?? string.Empty;
            var hmacKey = configuration["OlmedDataBus:WebhookKeys:HmacKey"] ?? string.Empty;
            _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
            _logger = logger;
            _configuration = configuration;
            _fileLoggingService = fileLoggingService;

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
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> Receive(
            [FromBody] WebhookPayload payload,
            [FromHeader(Name = "X-OLMED-ERP-API-SIGNATURE")] string signature)
        {
            var processingStartTime = DateTime.UtcNow;
            
            if (string.IsNullOrWhiteSpace(signature))
            {
                var errorMessage = "Odrzucono webhook - brak nag³ówka X-OLMED-ERP-API-SIGNATURE";
                _logger.LogWarning(errorMessage);
                
                // Logowanie do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Warning, errorMessage, null, new { 
                    Guid = payload?.guid, 
                    WebhookType = payload?.webhookType,
                    ProcessingTime = DateTime.UtcNow - processingStartTime
                });

                return BadRequest("Brak nag³ówka X-OLMED-ERP-API-SIGNATURE.");
            }

            _logger.LogInformation("Otrzymano webhook - GUID: {Guid}, Typ: {Type}", payload.guid, payload.webhookType);

            // Strukturalne logowanie otrzymania webhook do pliku
            await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook received", new {
                Guid = payload.guid,
                WebhookType = payload.webhookType,
                ReceivedAt = processingStartTime,
                SignaturePresent = !string.IsNullOrWhiteSpace(signature),
                PayloadSize = payload.webhookData?.Length ?? 0
            });

            // Zapisanie surowych danych webhook (zaszyfrowanych)
            await SaveRawWebhookData(payload, signature);

            // Próba deszyfracji i weryfikacji
            if (_helper.TryDecryptAndVerifyWithIvPrefix(payload.guid, payload.webhookType, payload.webhookData, signature, out var json))
            {
                var successMessage = $"Pomyœlnie odszyfrowano webhook - GUID: {payload.guid}";
                _logger.LogInformation(successMessage);

                // Zapisanie odszyfrowanych danych
                await SaveDecryptedWebhookData(payload.guid, payload.webhookType, json);

                var processingTime = DateTime.UtcNow - processingStartTime;

                // Strukturalne logowanie sukcesu do pliku
                await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook processed successfully", new {
                    Guid = payload.guid,
                    WebhookType = payload.webhookType,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = processingTime.TotalMilliseconds,
                    DecryptionSuccess = true,
                    DecryptedDataLength = json?.Length ?? 0
                });

                return Ok(new 
                { 
                    Success = true, 
                    Decrypted = json,
                    Message = "Webhook przetworzony i zapisany pomyœlnie",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = processingTime.TotalMilliseconds
                });
            }

            var decryptionErrorMessage = $"Nie uda³o siê zweryfikowaæ lub odszyfrowaæ webhook - GUID: {payload.guid}";
            _logger.LogError(decryptionErrorMessage);

            // Logowanie b³êdu deszyfracji do pliku
            await _fileLoggingService.LogAsync("webhook", LogLevel.Error, decryptionErrorMessage, null, new {
                Guid = payload.guid,
                WebhookType = payload.webhookType,
                SignatureLength = signature?.Length ?? 0,
                PayloadSize = payload.webhookData?.Length ?? 0,
                ProcessingTime = DateTime.UtcNow - processingStartTime
            });

            return BadRequest("Invalid signature or decryption failed.");
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
        /// Zapisuje surowe (zaszyfrowane) dane webhook do pliku.
        /// </summary>
        /// <param name="payload">Dane webhook do zapisania</param>
        /// <param name="signature">Podpis HMAC</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task SaveRawWebhookData(WebhookPayload payload, string signature)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var filename = $"webhook_raw_{payload.webhookType}_{payload.guid}_{timestamp}.json";
                var filePath = Path.Combine(_webhookDataDirectory, filename);

                // Tworzenie obiektu z pe³nymi danymi webhook
                var webhookRecord = new
                {
                    ReceivedAt = DateTime.UtcNow,
                    Guid = payload.guid,
                    WebhookType = payload.webhookType,
                    Signature = signature,
                    EncryptedData = payload.webhookData,
                    ProcessingStatus = "Raw"
                };

                var jsonContent = JsonSerializer.Serialize(webhookRecord, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);
                _logger.LogInformation("Zapisano surowe dane webhook do pliku: {FilePath}", filePath);

                // Logowanie do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Information, 
                    "Zapisano surowe dane webhook", null, new { 
                        Guid = payload.guid,
                        FileName = filename,
                        FilePath = filePath,
                        DataSize = jsonContent.Length
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania surowych danych webhook - GUID: {Guid}", payload.guid);
                
                // Logowanie b³êdu do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas zapisywania surowych danych webhook", ex, new { 
                        Guid = payload.guid,
                        WebhookType = payload.webhookType
                    });
            }
        }

        /// <summary>
        /// Zapisuje odszyfrowane dane webhook do pliku.
        /// </summary>
        /// <param name="guid">Identyfikator webhook</param>
        /// <param name="webhookType">Typ webhook</param>
        /// <param name="decryptedJson">Odszyfrowane dane JSON</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task SaveDecryptedWebhookData(string guid, string webhookType, string decryptedJson)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var filename = $"webhook_decrypted_{webhookType}_{guid}_{timestamp}.json";
                var filePath = Path.Combine(_webhookDataDirectory, filename);

                // Parsowanie JSON w celu ³adnego formatowania
                object? parsedJson = null;
                try
                {
                    parsedJson = JsonSerializer.Deserialize<object>(decryptedJson);
                }
                catch
                {
                    // Jeœli parsowanie siê nie powiedzie, u¿yj surowego stringa
                    parsedJson = decryptedJson;
                }

                // Tworzenie obiektu z metadanymi i odszyfrowanymi danymi
                var webhookRecord = new
                {
                    ProcessedAt = DateTime.UtcNow,
                    Guid = guid,
                    WebhookType = webhookType,
                    ProcessingStatus = "Decrypted",
                    DecryptedData = parsedJson
                };

                var jsonContent = JsonSerializer.Serialize(webhookRecord, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);
                _logger.LogInformation("Zapisano odszyfrowane dane webhook do pliku: {FilePath}", filePath);

                // Logowanie do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Information, 
                    "Zapisano odszyfrowane dane webhook", null, new { 
                        Guid = guid,
                        WebhookType = webhookType,
                        FileName = filename,
                        FilePath = filePath,
                        DecryptedDataSize = decryptedJson?.Length ?? 0
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania odszyfrowanych danych webhook - GUID: {Guid}", guid);
                
                // Logowanie b³êdu do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas zapisywania odszyfrowanych danych webhook", ex, new { 
                        Guid = guid,
                        WebhookType = webhookType
                    });
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
                "Health check wykonany", null, new { 
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
        /// Pobiera listê dostêpnych plików logów webhook.
        /// </summary>
        /// <returns>Lista plików logów</returns>
        /// <response code="200">Lista plików zosta³a pobrana</response>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetWebhookLogs()
        {
            try
            {
                var logFiles = await _fileLoggingService.GetLogFilesAsync();
                var webhookLogs = logFiles.Where(f => f.FileName.Contains("webhook")).ToList();

                return Ok(new
                {
                    Success = true,
                    TotalFiles = webhookLogs.Count,
                    LogFiles = webhookLogs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania listy plików logów webhook");
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas pobierania listy plików logów", ex);
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }
}
