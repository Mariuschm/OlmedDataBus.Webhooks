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
        /// Akceptuje: multipart/form-data z plikiem binarnym lub JSON z binarnym polem
        /// </summary>
        [HttpPost("upload-document-to-order")]
        [Consumes("multipart/form-data")]
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
                    // Multipart form data - plik przes³any jako IFormFile
                    var form = await Request.ReadFormAsync();

                    //// SprawdŸ czy jest plik
                    //IFormFile? uploadedFile = form.Files.GetFile("documentFile") ?? form.Files.FirstOrDefault();

                    //if (uploadedFile != null && uploadedFile.Length > 0)
                    //{
                    //    // Odczytaj plik jako bajty
                    //    using var memoryStream = new MemoryStream();
                    //    await uploadedFile.CopyToAsync(memoryStream);
                    //    fileBytes = memoryStream.ToArray();

                    //    _logger.LogInformation("Otrzymano plik binarny: {FileName}, rozmiar: {FileSize} bajtów", 
                    //        uploadedFile.FileName, fileBytes.Length);
                    //}
                    fileBytes = System.Text.Encoding.UTF8.GetBytes(form["documentFile"].ToString());
                    request = new UploadDocumentToOrderRequest
                    {
                        Marketplace = form["marketplace"].ToString(),
                        OrderNumber = form["orderNumber"].ToString(),
                        DocumentType = form["documentType"].ToString(),
                        FileFormat = form["fileFormat"].ToString(),
                        DocumentFile = fileBytes ?? Array.Empty<byte>(),
                        DocumentNumber = form["documentNumber"].ToString()
                    };
                }
                else
                {
                    return BadRequest(new { success = false, error = "Nieprawid³owy Content-Type", message = "Endpoint akceptuje tylko multipart/form-data z plikiem binarnym" });
                }
                
                if (request == null)
                    return BadRequest(new { success = false, error = "Nieprawid³owe dane ¿¹dania", message = "Nie mo¿na odczytaæ danych ¿¹dania" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d odczytu ¿¹dania UploadDocumentToOrder");
                return BadRequest(new { success = false, error = "B³¹d odczytu ¿¹dania", message = $"Nie mo¿na odczytaæ danych: {ex.Message}" });
            }

            fileBytes = request.DocumentFile;
            _logger.LogInformation(request.DocumentFile.ToString());
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

            const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
            if (fileBytes.Length > MaxFileSize)
                return BadRequest(new { success = false, error = "Plik zbyt du¿y", message = $"Maksymalny rozmiar pliku to {MaxFileSize / 1024 / 1024} MB" });

            var validFormats = new[] { "xml", "pdf" };
            if (!validFormats.Contains(request.FileFormat.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawid³owy format pliku", message = $"FileFormat musi byæ jedn¹ z wartoœci: {string.Join(", ", validFormats)}" });

            var validDocumentTypes = new[] { "invoice", "correction" };
            if (!validDocumentTypes.Contains(request.DocumentType.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawid³owy typ dokumentu", message = $"DocumentType musi byæ jedn¹ z wartoœci: {string.Join(", ", validDocumentTypes)}" });

            // Walidacja formatu pliku po magic bytes
            if (request.FileFormat.ToLower() == "pdf")
            {
                // PDF zaczyna siê od: %PDF (0x25 0x50 0x44 0x46)
                if (fileBytes.Length < 4 || fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
                    return BadRequest(new { success = false, error = "Nieprawid³owy format pliku", message = "Plik nie jest prawid³owym PDF" });
            }
            else if (request.FileFormat.ToLower() == "xml")
            {
                // XML zwykle zaczyna siê od: < lub UTF-8 BOM
                var startsWithXml = fileBytes[0] == 0x3C || // '<'
                                    (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF); // UTF-8 BOM
                
                if (!startsWithXml)
                    return BadRequest(new { success = false, error = "Nieprawid³owy format pliku", message = "Plik nie jest prawid³owym XML" });
            }

            try
            {
                // Przygotuj dane do wys³ania do Olmed - z plikiem binarnym
                var (success, response, statusCode) = await _olmedService.PostBinaryFileAsync(
                    "/erp-api/orders/upload-document-to-order",
                    request.Marketplace,
                    request.OrderNumber,
                    request.DocumentType,
                    request.FileFormat,
                    fileBytes,
                    request.DocumentNumber);

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
