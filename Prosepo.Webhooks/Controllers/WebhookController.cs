using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Helpers;
using Prosepo.Webhooks.Services;
using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using Prospeo.DTOs.Webhook;
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
        private readonly string secureKey = Environment.GetEnvironmentVariable("PROSPEO_KEY") ?? "CPNFWqXE3TMY925xMgUPlUnWkjSyo9182PpYM69HM44=";
        
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
           
            var encryptionKey = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedDataBus:WebhookKeys:EncryptionKey"], secureKey)?? string.Empty;
            var hmacKey = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedDataBus:WebhookKeys:HmacKey"], secureKey)?? string.Empty;
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
                    await AddToQueue(payload.guid, payload.webhookType, json);
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
        private async Task AddToQueue(string guid, string webhookType, string decryptedJson)
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

                // Parsuj JSON aby sprawdziæ czy zawiera productData lub orderData
                using var document = JsonDocument.Parse(decryptedJson);
                var root = document.RootElement;
                
                ProductDto? productData = null;
                OrderDto? orderData = null;
                string? changeType = null;

                // SprawdŸ czy webhook zawiera changeType
                if (root.TryGetProperty("changeType", out var changeTypeElement))
                {
                    changeType = changeTypeElement.GetString();
                }

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

                // SprawdŸ czy webhook zawiera orderData
                if (root.TryGetProperty("orderData", out var orderDataElement))
                {
                    var orderDataJson = orderDataElement.GetRawText();
                    orderData = JsonSerializer.Deserialize<OrderDto>(orderDataJson, jsonOptions);
                }
                // Jeœli ca³oœæ jest OrderDto (bez zagnie¿d¿enia)
                else if (webhookType?.ToLower().Contains("order") == true)
                {
                    try
                    {
                        orderData = JsonSerializer.Deserialize<OrderDto>(decryptedJson, jsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Nie uda³o siê deserializowaæ jako OrderDto
                        orderData = null;
                    }
                }

                // Pobierz konfiguracjê z appsettings
                var olmedId = _configuration.GetValue<int>("Queue:DefaultFirmaId", 1);
                var zawiszaId = _configuration.GetValue<int>("Queue:SecondFirmaId", 1);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);
                
                // Pobierz nazwê firmy dla logowania
                var companyName = "Unknown";
                if (_firmyService != null)
                {
                    var company = await _firmyService.GetByIdAsync(olmedId);
                    companyName = company?.NazwaFirmy ?? "Unknown";
                }

                // Przetwarzanie ProductDto
                if (productData != null)
                {
                    //Produkty dodajemy do obydwu firm
                    await AddProductToQueueAsync(guid, webhookType, changeType, productData, olmedId);
                    await AddProductToQueueAsync(guid, webhookType, changeType, productData, zawiszaId);
                }

                // Przetwarzanie OrderDto
                if (orderData != null)
                {
                    var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
                    var targetCompanyId = orderData.Marketplace?.ToLower().Contains("zawisza") == true ? zawiszaId : olmedId;
                    // Utwórz zadanie kolejki dla zamówienia
                    var queueItem = new Queue
                    {
                        FirmaId = targetCompanyId,
                        Scope = orderScope,
                        Request = JsonSerializer.Serialize(orderData, new JsonSerializerOptions { WriteIndented = true }),
                        Description = "",//Always empty for new entries
                        TargetID = 0, //Always zero for new entries
                        Flg = webhookProcessingFlag,//Default flag for webhook processing = 0
                        DateAddDateTime = DateTime.UtcNow,
                        DateModDateTime = DateTime.UtcNow
                    };

                    // Dodaj do kolejki
                    var addedItem = await _queueService.AddAsync(queueItem);

                    _logger.LogInformation("Dodano OrderDto do kolejki - GUID: {Guid}, OrderNumber: {OrderNumber}, OrderID: {OrderId}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}", 
                        guid, orderData.Number, orderData.Id, addedItem.Id, companyName, changeType ?? "N/A");

                    // Logowanie do pliku
                    await _fileLoggingService.LogAsync("webhook", LogLevel.Information, 
                        "Dodano OrderDto do kolejki", null, new { 
                            Guid = guid,
                            WebhookType = webhookType,
                            OrderNumber = orderData.Number,
                            OrderId = orderData.Id,
                            OrderMarketplace = orderData.Marketplace,
                            OrderItemsCount = orderData.OrderItems?.Count ?? 0,
                            QueueId = addedItem.Id,
                            QueueScope = orderScope,
                            Company = companyName,
                            ChangeType = changeType
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas dodawania do kolejki - GUID: {Guid}", guid);
                
                // Logowanie b³êdu do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas dodawania do kolejki", ex, new { 
                        Guid = guid,
                        WebhookType = webhookType
                    });
                
                // Re-throw aby nadrzêdna metoda mog³a obs³u¿yæ b³¹d
                throw;
            }
        }

        /// <summary>
        /// Dodaje ProductDto do kolejki jako osobne zadanie.
        /// </summary>
        /// <param name="guid">Identyfikator webhook</param>
        /// <param name="webhookType">Typ webhook</param>
        /// <param name="changeType">Typ zmiany (opcjonalny)</param>
        /// <param name="productData">Dane produktu do dodania</param>
        /// <param name="companyId">Identyfikator firmy</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task AddProductToQueueAsync(string guid, string webhookType, string? changeType, ProductDto productData, int companyId)
        {
            if (_queueService == null)
            {
                _logger.LogWarning("QueueService nie jest dostêpny - pominiêto dodanie produktu do kolejki - GUID: {Guid}", guid);
                return;
            }

            try
            {
                var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                // Pobierz nazwê firmy dla logowania
                var companyName = "Unknown";
                if (_firmyService != null)
                {
                    var company = await _firmyService.GetByIdAsync(companyId);
                    companyName = company?.NazwaFirmy ?? "Unknown";
                }

                // Utwórz zadanie kolejki dla produktu
                var queueItem = new Queue
                {
                    FirmaId = companyId,
                    Scope = productScope,
                    Request = JsonSerializer.Serialize(productData, new JsonSerializerOptions { WriteIndented = true }),
                    Description = "", // Always empty for new entries
                    TargetID = 0, // Always zero for new entries
                    Flg = webhookProcessingFlag, // Default flag for webhook processing = 0
                    DateAddDateTime = DateTime.UtcNow,
                    DateModDateTime = DateTime.UtcNow
                };

                // Dodaj do kolejki
                var addedItem = await _queueService.AddAsync(queueItem);

                _logger.LogInformation("Dodano ProductDto do kolejki - GUID: {Guid}, SKU: {Sku}, QueueID: {QueueId}, Firma: {Firma}, ChangeType: {ChangeType}", 
                    guid, productData.Sku, addedItem.Id, companyName, changeType ?? "N/A");

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
                        Company = companyName,
                        CompanyId = companyId,
                        ChangeType = changeType
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas dodawania produktu do kolejki - GUID: {Guid}, SKU: {Sku}", 
                    guid, productData?.Sku);
                
                // Logowanie b³êdu do pliku
                await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                    "B³¹d podczas dodawania produktu do kolejki", ex, new { 
                        Guid = guid,
                        WebhookType = webhookType,
                        ProductSku = productData?.Sku,
                        CompanyId = companyId
                    });
                
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
