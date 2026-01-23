using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Text.Json;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Kontroler do zarz¹dzania zamówieniami
    /// Wymaga autentykacji tokenem API (X-API-Key)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly OlmedApiService _olmedService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            OlmedApiService olmedService,
            ILogger<OrdersController> logger)
        {
            _olmedService = olmedService;
            _logger = logger;
        }

        /// <summary>
        /// Aktualizuje status zamówienia w systemie Olmed
        /// </summary>
        /// <param name="request">Dane ¿¹dania aktualizacji statusu</param>
        /// <returns>Wynik operacji aktualizacji statusu</returns>
        [HttpPost("update-status")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie aktualizacji statusu zamówienia: OrderNumber={OrderNumber}, Status={Status}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.OrderNumber, request.Status, request.Marketplace, firmaNazwa, firmaId);

            if (string.IsNullOrWhiteSpace(request.OrderNumber))
                return BadRequest(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie mo¿e byæ pusty" });

            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { success = false, error = "Status jest wymagany", message = "Parametr status nie mo¿e byæ pusty" });

            if (string.IsNullOrWhiteSpace(request.Marketplace))
                return BadRequest(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie mo¿e byæ pusty" });

            try
            {
                var requestData = new
                {
                    marketplace = request.Marketplace,
                    orderNumber = request.OrderNumber,
                    status = request.Status,
                    note = request.Note,
                    trackingNumber = request.TrackingNumber,
                    updatedBy = firmaNazwa ?? "Unknown",
                    updatedAt = DateTime.UtcNow
                };

                var (success, response, statusCode) = await _olmedService.PostAsync("/erp-api/orders/change-status", requestData);

                if (success)
                {
                    _logger.LogInformation("Pomyœlnie zaktualizowano status zamówienia {OrderNumber} na {Status}", request.OrderNumber, request.Status);
                    return Ok(new UpdateOrderStatusResponse
                    {
                        Success = true,
                        Message = "Status zamówienia zosta³ pomyœlnie zaktualizowany",
                        OrderNumber = request.OrderNumber,
                        NewStatus = request.Status
                    });
                }
                else
                {
                    _logger.LogWarning("B³¹d podczas aktualizacji statusu zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);
                    return StatusCode(statusCode, new { success = false, error = "B³¹d podczas aktualizacji statusu w systemie Olmed", message = response, statusCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyj¹tek podczas aktualizacji statusu zamówienia {OrderNumber}", request.OrderNumber);
                return StatusCode(500, new { success = false, error = "B³¹d serwera", message = "Wyst¹pi³ nieoczekiwany b³¹d podczas aktualizacji statusu zamówienia" });
            }
        }

        /// <summary>
        /// Przesy³a wyniki realizacji zamówienia do systemu Olmed
        /// </summary>
        [HttpPost("upload-order-realization-result")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(UploadOrderRealizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadOrderRealizationResult([FromBody] UploadOrderRealizationRequest request)
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation("¯¹danie przes³ania wyników realizacji zamówienia: OrderNumber={OrderNumber}, ItemsCount={ItemsCount}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.OrderNumber, request.Items?.Count ?? 0, request.Marketplace, firmaNazwa, firmaId);

            if (string.IsNullOrWhiteSpace(request.OrderNumber))
                return BadRequest(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie mo¿e byæ pusty" });

            if (string.IsNullOrWhiteSpace(request.Marketplace))
                return BadRequest(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie mo¿e byæ pusty" });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { success = false, error = "Items s¹ wymagane", message = "Lista items nie mo¿e byæ pusta" });

            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (string.IsNullOrWhiteSpace(item.Sku))
                    return BadRequest(new { success = false, error = "SKU jest wymagany", message = $"SKU pozycji {i + 1} nie mo¿e byæ pusty" });
                if (item.Quantity <= 0)
                    return BadRequest(new { success = false, error = "Nieprawid³owa iloœæ", message = $"Iloœæ pozycji {i + 1} musi byæ wartoœci¹ dodatni¹" });
            }

            try
            {
                var requestData = new
                {
                    marketplace = request.Marketplace,
                    orderNumber = request.OrderNumber,
                    items = request.Items.Select(item => new
                    {
                        sku = item.Sku,
                        quantity = item.Quantity,
                        expirationDate = item.ExpirationDate,
                        seriesNumber = item.SeriesNumber
                    }).ToList()
                };

                var (success, response, statusCode) = await _olmedService.PostAsync("/erp-api/orders/upload-realization-result", requestData);

                if (success)
                {
                    _logger.LogInformation("Pomyœlnie przes³ano wyniki realizacji zamówienia {OrderNumber}, pozycji: {ItemsCount}", request.OrderNumber, request.Items.Count);
                    return Ok(new UploadOrderRealizationResponse
                    {
                        Success = true,
                        Message = "Wyniki realizacji zamówienia zosta³y pomyœlnie przes³ane",
                        OrderNumber = request.OrderNumber,
                        ItemsProcessed = request.Items.Count
                    });
                }
                else
                {
                    _logger.LogWarning("B³¹d podczas przesy³ania wyników realizacji zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);
                    return StatusCode(statusCode, new { success = false, error = "B³¹d podczas przesy³ania wyników realizacji w systemie Olmed", message = response, statusCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyj¹tek podczas przesy³ania wyników realizacji zamówienia {OrderNumber}", request.OrderNumber);
                return StatusCode(500, new { success = false, error = "B³¹d serwera", message = "Wyst¹pi³ nieoczekiwany b³¹d podczas przesy³ania wyników realizacji zamówienia" });
            }
        }

        /// <summary>
        /// Przesy³a dokument (faktura lub korekta) do zamówienia w systemie Olmed
        /// Akceptuje: JSON z base64, form-data z plikiem binarnym, lub plain text
        /// </summary>
        [HttpPost("upload-document-to-order")]
        [Consumes("application/json", "application/x-www-form-urlencoded", "multipart/form-data", "text/plain")]
        [ProducesResponseType(typeof(UploadDocumentToOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocumentToOrder()
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();
            UploadDocumentToOrderRequest? request = null;
            byte[]? fileBytes = null;
            
            try
            {
                var contentType = Request.ContentType?.ToLower() ?? "";
                
                if (contentType.Contains("multipart/form-data"))
                {
                    // Multipart form data - plik mo¿e byæ przes³any jako IFormFile
                    var form = await Request.ReadFormAsync();
                    
                    // SprawdŸ czy jest plik
                    IFormFile? uploadedFile = form.Files.GetFile("documentFile") ?? form.Files.FirstOrDefault();
                    
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        // Odczytaj plik jako bajty
                        using var memoryStream = new MemoryStream();
                        await uploadedFile.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                        
                        _logger.LogInformation("Otrzymano plik binarny: {FileName}, rozmiar: {FileSize} bajtów", 
                            uploadedFile.FileName, fileBytes.Length);
                    }
                    else
                    {
                        // Brak pliku - sprawdŸ czy jest base64 w formularzu
                        var base64String = form["documentFile"].ToString();
                        if (!string.IsNullOrWhiteSpace(base64String))
                        {
                            try
                            {
                                fileBytes = Convert.FromBase64String(base64String);
                                _logger.LogInformation("Zdekodowano base64 z formularza, rozmiar: {FileSize} bajtów", fileBytes.Length);
                            }
                            catch (FormatException)
                            {
                                _logger.LogWarning("Nieprawid³owy format base64 w polu documentFile");
                            }
                        }
                    }
                    
                    request = new UploadDocumentToOrderRequest
                    {
                        Marketplace = form["marketplace"].ToString(),
                        OrderNumber = form["orderNumber"].ToString(),
                        DocumentType = form["documentType"].ToString(),
                        FileFormat = form["fileFormat"].ToString(),
                        DocumentFile = fileBytes != null ? Convert.ToBase64String(fileBytes) : string.Empty,
                        DocumentNumber = form["documentNumber"].ToString()
                    };
                }
                else if (contentType.Contains("application/x-www-form-urlencoded"))
                {
                    // URL encoded form
                    var form = await Request.ReadFormAsync();
                    var base64String = form["documentFile"].ToString();
                    
                    if (!string.IsNullOrWhiteSpace(base64String))
                    {
                        try
                        {
                            fileBytes = Convert.FromBase64String(base64String);
                            _logger.LogInformation("Zdekodowano base64 z form-urlencoded, rozmiar: {FileSize} bajtów", fileBytes.Length);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Nieprawid³owy format base64 w polu documentFile");
                        }
                    }
                    
                    request = new UploadDocumentToOrderRequest
                    {
                        Marketplace = form["marketplace"].ToString(),
                        OrderNumber = form["orderNumber"].ToString(),
                        DocumentType = form["documentType"].ToString(),
                        FileFormat = form["fileFormat"].ToString(),
                        DocumentFile = base64String,
                        DocumentNumber = form["documentNumber"].ToString()
                    };
                }
                else if (contentType.Contains("application/json"))
                {
                    // JSON - documentFile jako base64
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(body))
                        return BadRequest(new { success = false, error = "Puste body", message = "Request body nie mo¿e byæ pusty" });
                    
                    request = JsonSerializer.Deserialize<UploadDocumentToOrderRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (request != null && !string.IsNullOrWhiteSpace(request.DocumentFile))
                    {
                        try
                        {
                            fileBytes = Convert.FromBase64String(request.DocumentFile);
                            _logger.LogInformation("Zdekodowano base64 z JSON, rozmiar: {FileSize} bajtów", fileBytes.Length);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Nieprawid³owy format base64 w JSON");
                        }
                    }
                }
                else
                {
                    // Plain text lub brak Content-Type - próbuj JSON
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(body))
                        return BadRequest(new { success = false, error = "Puste body", message = "Request body nie mo¿e byæ pusty" });
                    
                    request = JsonSerializer.Deserialize<UploadDocumentToOrderRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (request != null && !string.IsNullOrWhiteSpace(request.DocumentFile))
                    {
                        try
                        {
                            fileBytes = Convert.FromBase64String(request.DocumentFile);
                            _logger.LogInformation("Zdekodowano base64 z plain text, rozmiar: {FileSize} bajtów", fileBytes.Length);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Nieprawid³owy format base64");
                        }
                    }
                }
                
                if (request == null)
                    return BadRequest(new { success = false, error = "Nieprawid³owe dane ¿¹dania", message = "Nie mo¿na zdeserializowaæ danych ¿¹dania" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "B³¹d deserializacji ¿¹dania UploadDocumentToOrder");
                return BadRequest(new { success = false, error = "Nieprawid³owy format JSON", message = $"B³¹d parsowania JSON: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d odczytu ¿¹dania UploadDocumentToOrder");
                return BadRequest(new { success = false, error = "B³¹d odczytu ¿¹dania", message = $"Nie mo¿na odczytaæ danych: {ex.Message}" });
            }

            _logger.LogInformation("¯¹danie przes³ania dokumentu do zamówienia: OrderNumber={OrderNumber}, DocumentType={DocumentType}, FileFormat={FileFormat}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId}), ContentType={ContentType}, FileSize={FileSize}",
                request.OrderNumber, request.DocumentType, request.FileFormat, request.Marketplace, firmaNazwa, firmaId, Request.ContentType, fileBytes?.Length ?? 0);

            // Walidacje
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
                return BadRequest(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie mo¿e byæ pusty" });
            if (string.IsNullOrWhiteSpace(request.Marketplace))
                return BadRequest(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie mo¿e byæ pusty" });
            if (string.IsNullOrWhiteSpace(request.DocumentType))
                return BadRequest(new { success = false, error = "DocumentType jest wymagany", message = "Parametr documentType nie mo¿e byæ pusty" });
            if (string.IsNullOrWhiteSpace(request.FileFormat))
                return BadRequest(new { success = false, error = "FileFormat jest wymagany", message = "Parametr fileFormat nie mo¿e byæ pusty" });
            if (fileBytes == null || fileBytes.Length == 0)
                return BadRequest(new { success = false, error = "DocumentFile jest wymagany", message = "Plik nie mo¿e byæ pusty" });

            var validFormats = new[] { "xml", "pdf" };
            if (!validFormats.Contains(request.FileFormat.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawid³owy format pliku", message = $"FileFormat musi byæ jedn¹ z wartoœci: {string.Join(", ", validFormats)}" });

            var validDocumentTypes = new[] { "invoice", "correction" };
            if (!validDocumentTypes.Contains(request.DocumentType.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawid³owy typ dokumentu", message = $"DocumentType musi byæ jedn¹ z wartoœci: {string.Join(", ", validDocumentTypes)}" });

            try
            {
                // Przygotuj dane do wys³ania do Olmed - z plikiem binarnym jako base64
                var requestData = new
                {
                    marketplace = request.Marketplace,
                    orderNumber = request.OrderNumber,
                    documentType = request.DocumentType,
                    fileFormat = request.FileFormat,
                    documentFile = Convert.ToBase64String(fileBytes),
                    documentNumber = request.DocumentNumber
                };

                var (success, response, statusCode) = await _olmedService.PostAsync("/erp-api/orders/upload-document-to-order", requestData);

                if (success)
                {
                    _logger.LogInformation("Pomyœlnie przes³ano dokument do zamówienia {OrderNumber}, typ: {DocumentType}, format: {FileFormat}, rozmiar: {FileSize} bajtów",
                        request.OrderNumber, request.DocumentType, request.FileFormat, fileBytes.Length);
                    return Ok(new UploadDocumentToOrderResponse
                    {
                        Success = true,
                        Message = "Dokument zosta³ pomyœlnie przes³any do zamówienia",
                        OrderNumber = request.OrderNumber,
                        DocumentType = request.DocumentType
                    });
                }
                else
                {
                    _logger.LogWarning("B³¹d podczas przesy³ania dokumentu do zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);
                    return StatusCode(statusCode, new { success = false, error = "B³¹d podczas przesy³ania dokumentu w systemie Olmed", message = response, statusCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyj¹tek podczas przesy³ania dokumentu do zamówienia {OrderNumber}", request.OrderNumber);
                return StatusCode(500, new { success = false, error = "B³¹d serwera", message = "Wyst¹pi³ nieoczekiwany b³¹d podczas przesy³ania dokumentu do zamówienia" });
            }
        }

        /// <summary>
        /// Pobiera informacje o zalogowanej firmie na podstawie API Key
        /// </summary>
        [HttpGet("authenticated-firma")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAuthenticatedFirma()
        {
            return Ok(new
            {
                firmaId = HttpContext.Items["FirmaId"],
                firmaNazwa = HttpContext.Items["FirmaNazwa"],
                message = "Pomyœlnie zautoryzowano"
            });
        }
    }
}
