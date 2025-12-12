using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.DTO;
using Prosepo.Webhooks.Services;
using Prospeo.DbContext.Services;
using Prospeo.DbContext.Models;
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
        private readonly IQueueService? _queueService;
        private readonly IFirmyService? _firmyService;

        /// <summary>
        /// Inicjalizuje now¹ instancjê WebhookController.
        /// </summary>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="fileLoggingService">Serwis logowania do plików</param>
        /// <param name="queueService">Serwis obs³ugi kolejki (opcjonalny)</param>
        /// <param name="firmyService">Serwis obs³ugi firm (opcjonalny)</param>
        public WebhookController(IConfiguration configuration, ILogger<WebhookController> logger, 
            FileLoggingService fileLoggingService, IQueueService? queueService = null, IFirmyService? firmyService = null)
        {
            var encryptionKey = configuration["OlmedDataBus:WebhookKeys:EncryptionKey"] ?? string.Empty;
            var hmacKey = configuration["OlmedDataBus:WebhookKeys:HmacKey"] ?? string.Empty;
            _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
            _logger = logger;
            _configuration = configuration;
            _fileLoggingService = fileLoggingService;
            _queueService = queueService;
            _firmyService = firmyService;

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
                
                await _fileLoggingService.LogAsync("webhook", LogLevel.Warning, errorMessage, null, new { 
                    Guid = payload?.guid, 
                    WebhookType = payload?.webhookType,
                    ProcessingTime = DateTime.UtcNow - processingStartTime
                });

                return BadRequest(errorMessage);
            }

            _logger.LogInformation("Otrzymano webhook - GUID: {Guid}, Typ: {Type}", payload.guid, payload.webhookType);

            // Strukturalne logowanie otrzymania webhook do pliku
            await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook received", new {
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

                // Dodanie ProductDto do kolejki jeœli webhook zawiera dane produktu
                try
                {
                    await AddProductToQueueIfApplicable(payload.guid, payload.webhookType, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "B³¹d podczas dodawania do kolejki - GUID: {Guid}", payload.guid);
                    // Nie przerywamy przetwarzania - webhook zosta³ ju¿ odebrany
                }

                var processingTime = DateTime.UtcNow - processingStartTime;

                // Strukturalne logowanie sukcesu do pliku
                await _fileLoggingService.LogStructuredAsync("webhook", LogLevel.Information, "Webhook processed successfully", new {
                    Guid = payload.guid,
                    WebhookType = payload.webhookType,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = processingTime.TotalMilliseconds,
                    DecryptionSuccess = true
                });

                return Ok();
            }

            var decryptionErrorMessage = "Nie uda³o siê zweryfikowaæ lub odszyfrowaæ webhook";
            _logger.LogError("{Error} - GUID: {Guid}", decryptionErrorMessage, payload.guid);

            // Logowanie b³êdu deszyfracji do pliku
            await _fileLoggingService.LogAsync("webhook", LogLevel.Error, decryptionErrorMessage, null, new {
                Guid = payload.guid,
                WebhookType = payload.webhookType,
                SignatureLength = signature?.Length ?? 0,
                PayloadSize = payload.webhookData?.Length ?? 0,
                ProcessingTime = DateTime.UtcNow - processingStartTime
            });

            return BadRequest(decryptionErrorMessage);
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
        /// Dodaje ProductDto do kolejki jeœli webhook zawiera odpowiednie dane produktu.
        /// </summary>
        /// <param name="guid">Identyfikator webhook</param>
        /// <param name="webhookType">Typ webhook</param>
        /// <param name="decryptedJson">Odszyfrowane dane JSON</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task AddProductToQueueIfApplicable(string guid, string webhookType, string decryptedJson)
        {
            // SprawdŸ czy serwis kolejki jest dostêpny
            if (_queueService == null)
            {
                _logger.LogWarning("QueueService nie jest dostêpny - pominiêto dodanie do kolejki - GUID: {Guid}", guid);
                return;
            }

            try
            {
                // JsonSerializerOptions z custom converter dla DateTime
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new CustomDateTimeConverter() }
                };

                // Parsuj JSON aby sprawdziæ czy zawiera productData
                using var document = JsonDocument.Parse(decryptedJson);
                var root = document.RootElement;
                
                ProductDto? productData = null;

                // SprawdŸ czy webhook zawiera productData
                if (root.TryGetProperty("productData", out var productDataElement))
                {
                    var productDataJson = productDataElement.GetRawText();
                    productData = JsonSerializer.Deserialize<ProductDto>(productDataJson, jsonOptions);
                }
                // Jeœli ca³oœæ jest ProductDto (bez zagnie¿d¿enia)
                else if (webhookType?.ToLower().Contains("product") == true)
                {
                    try
                    {
                        productData = JsonSerializer.Deserialize<ProductDto>(decryptedJson, jsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Nie uda³o siê deserializowaæ jako ProductDto
                        productData = null;
                    }
                }

                if (productData != null)
                {
                    // Pobierz konfiguracjê z appsettings
                    var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
                    var defaultFirmaId = _configuration.GetValue<int>("Queue:DefaultFirmaId", 1);
                    var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);
                    
                    // Pobierz nazwê firmy dla logowania
                    var companyName = "Unknown";
                    if (_firmyService != null)
                    {
                        var company = await _firmyService.GetByIdAsync(defaultFirmaId);
                        companyName = company?.NazwaFirmy ?? "Unknown";
                    }

                    // Utwórz zadanie kolejki
                    var queueItem = new Queue
                    {
                        FirmaId = defaultFirmaId,
                        Scope = productScope,
                        Request = JsonSerializer.Serialize(productData, new JsonSerializerOptions { WriteIndented = true }),
                        Description = "",//Always empty for new entries
                        TargetID = 0, //Always zero for new entries
                        Flg = webhookProcessingFlag,//Default flag for webhook processing = 0
                        DateAddDateTime = DateTime.UtcNow,
                        DateModDateTime = DateTime.UtcNow
                    };

                    // Dodaj do kolejki
                    var addedItem = await _queueService.AddAsync(queueItem);

                    _logger.LogInformation("Dodano ProductDto do kolejki - GUID: {Guid}, SKU: {Sku}, QueueID: {QueueId}, Firma: {Firma}", 
                        guid, productData.Sku, addedItem.Id, companyName);

                    // Logowanie do pliku
                    await _fileLoggingService.LogAsync("webhook", LogLevel.Information, 
                        "Dodano ProductDto do kolejki", null, new { 
                            Guid = guid,
                            WebhookType = webhookType,
                            ProductSku = productData.Sku,
                            ProductName = productData.Name,
                            ProductId = productData.Id,
                            QueueId = addedItem.Id,
                            QueueScope = productScope,
                            Company = companyName
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas dodawania ProductDto do kolejki - GUID: {Guid}", guid);
                
                // Logowanie b³êdu do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas dodawania ProductDto do kolejki", ex, new { 
                        Guid = guid,
                        WebhookType = webhookType
                    });
                
                // Re-throw aby nadrzêdna metoda mog³a obs³u¿yæ b³¹d
                throw;
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

        /// <summary>
        /// Pobiera zdefiniowane foldery, które s¹ dostêpne do archiwizacji
        /// </summary>
        /// <returns>Lista folderów do archiwizacji</returns>
        /// <response code="200">Foldery zosta³y pomyœlnie pobrane</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas pobierania folderów</response>
        [HttpGet("archive/folders")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult GetArchiveFolders()
        {
            try
            {
                var folders = _configuration.GetSection("Archive:Folders").Get<List<string>>() ?? new List<string>();

                return Ok(new 
                { 
                    Success = true, 
                    Folders = folders 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania folderów archiwum");
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Rozpoczyna archiwizacjê dla zdefiniowanych folderów.
        /// </summary>
        /// <returns>Status operacji archiwizacji</returns>
        /// <response code="200">Archiwizacja zosta³a pomyœlnie rozpoczêta</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas uruchamiania archiwizacji</response>
        [HttpPost("archive/start")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult StartArchivization()
        {
            try
            {
                // Tutaj dodaj logikê do rozpoczynania archiwizacji, np. ustawienie flagi w bazie danych
                // lub wywo³anie zewnêtrznego serwisu odpowiedzialnego za archiwizacjê.

                return Ok(new { Success = true, Message = "Archiwizacja zosta³a pomyœlnie rozpoczêta." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas uruchamiania archiwizacji");
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Zatrzymuje bie¿¹cy proces archiwizacji.
        /// </summary>
        /// <returns>Status operacji zatrzymywania archiwizacji</returns>
        /// <response code="200">Archiwizacja zosta³a pomyœlnie zatrzymana</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas zatrzymywania archiwizacji</response>
        [HttpPost("archive/stop")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult StopArchivization()
        {
            try
            {
                // Tutaj dodaj logikê do zatrzymywania archiwizacji, np. resetowanie flagi w bazie danych
                // lub powiadomienie zewnêtrznego serwisu odpowiedzialnego za archiwizacjê.

                return Ok(new { Success = true, Message = "Archiwizacja zosta³a pomyœlnie zatrzymana." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zatrzymywania archiwizacji");
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Pobiera status bie¿¹cego procesu archiwizacji.
        /// </summary>
        /// <returns>Status archiwizacji</returns>
        /// <response code="200">Status archiwizacji zosta³ pomyœlnie pobrany</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas pobierania statusu archiwizacji</response>
        [HttpGet("archive/status")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult GetArchivizationStatus()
        {
            try
            {
                // Tutaj dodaj logikê do pobierania statusu archiwizacji, np. odczytanie flagi z bazy danych
                // lub zapytanie zewnêtrznego serwisu odpowiedzialnego za archiwizacjê.

                var status = new 
                {
                    IsRunning = false, // Przyk³adowa wartoœæ, zmieñ na odpowiedni¹ logikê
                    StartedAt = (DateTime?)null, // Przyk³adowa wartoœæ, zmieñ na odpowiedni¹ logikê
                    CompletedAt = (DateTime?)null, // Przyk³adowa wartoœæ, zmieñ na odpowiedni¹ logikê
                    ErrorMessage = (string)null // Przyk³adowa wartoœæ, zmieñ na odpowiedni¹ logikê
                };

                return Ok(new 
                { 
                    Success = true, 
                    Status = status 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania statusu archiwizacji");
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Pobiera listê zadañ z kolejki dla produktów przetworzonych przez webhook.
        /// </summary>
        /// <returns>Lista zadañ z kolejki dla produktów</returns>
        /// <response code="200">Lista zadañ zosta³a pobrana</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        [HttpGet("queue/products")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 503)]
        public async Task<IActionResult> GetProductQueueItems()
        {
            try
            {
                if (_queueService == null)
                {
                    return StatusCode(503, new { 
                        Success = false, 
                        Message = "Queue service nie jest dostêpny" 
                    });
                }

                var productScope = _configuration.GetValue<int>("Queue:ProductScope", 1001);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 100);

                // Pobierz zadania z odpowiednim scope (produkty)
                var queueItems = await _queueService.GetByScopeAsync(productScope);
                
                // Filtruj tylko zadania z webhook (po flagach)
                var webhookItems = queueItems.Where(q => q.Flg == webhookProcessingFlag).ToList();

                var result = webhookItems.Select(item => new
                {
                    Id = item.Id,
                    RowID = item.RowID,
                    FirmaId = item.FirmaId,
                    Description = item.Description,
                    TargetID = item.TargetID,
                    DateAdd = item.DateAddDateTime,
                    DateMod = item.DateModDateTime,
                    RequestPreview = item.Request.Length > 200 ? item.Request.Substring(0, 200) + "..." : item.Request
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    TotalItems = result.Count,
                    ProductScope = productScope,
                    WebhookFlag = webhookProcessingFlag,
                    QueueItems = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania zadañ z kolejki produktów");
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas pobierania zadañ z kolejki produktów", ex);
                
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
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
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { 
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
                    "Test po³¹czenia z baz¹ danych zakoñczony sukcesem", null, new { 
                        ConnectionString = connectionString.Replace(_configuration["ConnectionStrings:DefaultConnection"]?.Split("Password=")[1]?.Split(";")[0] ?? "", "***"),
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
                
                return StatusCode(500, new { 
                    Success = false, 
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    InnerException = ex.InnerException?.Message
                });
            }
        }
    }
}
