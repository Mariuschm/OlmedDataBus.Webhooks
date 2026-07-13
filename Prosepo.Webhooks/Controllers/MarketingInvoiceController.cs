using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs.Order;

namespace Prosepo.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    [Produces("application/json")]
    public class MarketingInvoiceController: ControllerBase
    {
        private readonly OlmedApiService _olmedService;
        private readonly ILogger<MarketingInvoiceController> _logger;
        public MarketingInvoiceController(
            OlmedApiService olmedService,
            ILogger<MarketingInvoiceController> logger)
        {
            _olmedService = olmedService;
            _logger = logger;
        }

        /// <summary>
        /// Przesyła dokument (faktura lub korekta) do zamówienia w systemie Olmed
        /// Akceptuje: multipart/form-data z plikiem binarnym lub JSON z binarnym polem
        /// </summary>
        [HttpPost("upload-document-to-order")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadDocumentToOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocumentToMarketingOrder()
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
                    // Multipart form data - plik przesłany jako IFormFile
                    var form = await Request.ReadFormAsync();
                    fileBytes = System.Text.Encoding.UTF8.GetBytes(form["documentFile"].ToString());
                    request = new UploadDocumentToOrderRequest
                    {
                        Marketplace = form["marketplace"].ToString(),
                        OrderNumber = form["orderNumber"].ToString(),
                        DocumentType = form["documentType"].ToString(),
                        FileFormat = form["fileFormat"].ToString(),
                        DocumentFile = fileBytes ?? Array.Empty<byte>(),
                        DocumentNumber = form["documentNumber"].ToString(),
                        DocumentDateIssue = DateTime.Today
                    }; 
                }
                else
                {
                    return BadRequest(new { success = false, error = "Nieprawidłowy Content-Type", message = "Endpoint akceptuje tylko multipart/form-data z plikiem binarnym" });
                }

                if (request == null)
                    return BadRequest(new { success = false, error = "Nieprawidłowe dane żądania", message = "Nie można odczytać danych żądania" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd odczytu żądania UploadDocumentToOrder");
                return BadRequest(new { success = false, error = "Błąd odczytu żądania", message = $"Nie można odczytać danych: {ex.Message}" });
            }

            fileBytes = request.DocumentFile;
            _logger.LogInformation(request.DocumentFile.ToString());
            _logger.LogInformation("Żądanie przesłania dokumentu do zamówienia: OrderNumber={OrderNumber}, DocumentType={DocumentType}, FileFormat={FileFormat}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId}), ContentType={ContentType}, FileSize={FileSize}",
                request.OrderNumber, request.DocumentType, request.FileFormat, request.Marketplace, firmaNazwa, firmaId, Request.ContentType, fileBytes?.Length ?? 0);

            // Walidacje
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
                return BadRequest(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie może być pusty" });
            if (string.IsNullOrWhiteSpace(request.Marketplace))
                return BadRequest(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie może być pusty" });
            if (string.IsNullOrWhiteSpace(request.DocumentType))
                return BadRequest(new { success = false, error = "DocumentType jest wymagany", message = "Parametr documentType nie może być pusty" });
            if (string.IsNullOrWhiteSpace(request.FileFormat))
                return BadRequest(new { success = false, error = "FileFormat jest wymagany", message = "Parametr fileFormat nie może być pusty" });
            if (string.IsNullOrWhiteSpace(request.DocumentNumber))
                return BadRequest(new { success = false, error = "DocumentNumber jest wymagany", message = "Parametr documentNumber nie może być pusty" });
            if (fileBytes == null || fileBytes.Length == 0)
                return BadRequest(new { success = false, error = "DocumentFile jest wymagany", message = "Plik nie może być pusty" });

            const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
            if (fileBytes.Length > MaxFileSize)
                return BadRequest(new { success = false, error = "Plik zbyt duży", message = $"Maksymalny rozmiar pliku to {MaxFileSize / 1024 / 1024} MB" });

            var validFormats = new[] { "xml", "pdf" };
            if (!validFormats.Contains(request.FileFormat.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawidłowy format pliku", message = $"FileFormat musi być jedną z wartości: {string.Join(", ", validFormats)}" });

            var validDocumentTypes = new[] { "invoice", "correction" };
            if (!validDocumentTypes.Contains(request.DocumentType.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawidłowy typ dokumentu", message = $"DocumentType musi być jedną z wartości: {string.Join(", ", validDocumentTypes)}" });

            // Walidacja formatu pliku po magic bytes
            if (request.FileFormat.ToLower() == "pdf")
            {
                // PDF zaczyna się od: %PDF (0x25 0x50 0x44 0x46)
                if (fileBytes.Length < 4 || fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
                    return BadRequest(new { success = false, error = "Nieprawidłowy format pliku", message = "Plik nie jest prawidłowym PDF" });
            }
            else if (request.FileFormat.ToLower() == "xml")
            {
                // XML zwykle zaczyna się od: < lub UTF-8 BOM
                var startsWithXml = fileBytes[0] == 0x3C || // '<'
                                    (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF); // UTF-8 BOM

                if (!startsWithXml)
                    return BadRequest(new { success = false, error = "Nieprawidłowy format pliku", message = "Plik nie jest prawidłowym XML" });
            }

            try
            {
                // Przygotuj dane do wysłania do Olmed - z plikiem binarnym
                var (success, response, statusCode) = await _olmedService.PostBinaryFileAsync(
                    "/erp-api/marketing-orders/upload-document-to-marketing-order",
                    request.Marketplace,
                    request.OrderNumber,
                    request.DocumentType,
                    request.FileFormat,
                    fileBytes,
                    request.DocumentNumber, new DateTime(2026,7,10));

                if (success)
                {
                    _logger.LogInformation("Pomyślnie przesłano dokument do zamówienia {OrderNumber}, typ: {DocumentType}, format: {FileFormat}, rozmiar: {FileSize} bajtów",
                        request.OrderNumber, request.DocumentType, request.FileFormat, fileBytes.Length);
                    return Ok(new UploadDocumentToOrderResponse
                    {
                        Success = true,
                        Message = "Dokument został pomyślnie przesłany do zamówienia",
                        OrderNumber = request.OrderNumber,
                        DocumentType = request.DocumentType
                    });
                }
                else
                {
                    _logger.LogWarning("Błąd podczas przesyłania dokumentu do zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);
                    return StatusCode(statusCode, new { success = false, error = "Błąd podczas przesyłania dokumentu w systemie Olmed", message = response, statusCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyjątek podczas przesyłania dokumentu do zamówienia {OrderNumber}", request.OrderNumber);
                return StatusCode(500, new { success = false, error = "Błąd serwera", message = "Wystąpił nieoczekiwany błąd podczas przesyłania dokumentu do zamówienia" });
            }
        }
    }
}
