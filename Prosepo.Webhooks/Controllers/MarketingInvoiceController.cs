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
    public class MarketingInvoiceController : ControllerBase
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
            var uploadedFile = null as IFormFile;
            try
            {
                var contentType = Request.ContentType?.ToLower() ?? "";

                if (contentType.Contains("multipart/form-data"))
                {
                    // Multipart form data - plik przesłany jako IFormFile
                    var form = await Request.ReadFormAsync();
                    uploadedFile = form.Files.GetFile("documentFile") ?? form.Files.FirstOrDefault();
                    var fileName = $"Invoice_{form["documentNumber"].ToString().Replace("/", "_").Replace("-", "_").Replace("(", "_").Replace(")", "_")}.pdf";

                    request = new UploadDocumentToOrderRequest
                    {
                        Marketplace = form["marketplace"].ToString(),
                        OrderNumber = form["orderNumber"].ToString(),
                        DocumentType = form["documentType"].ToString(),
                        FileFormat = form["fileFormat"].ToString(),
                        DocumentFile = new StreamContent(uploadedFile?.OpenReadStream() ?? Stream.Null),
                        DocumentNumber = form["documentNumber"].ToString(),
                        DocumentDateIssue = form["documentDateIssue"].ToString(),

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


            _logger.LogInformation("Żądanie przesłania dokumentu do zamówienia: OrderNumber={OrderNumber}, DocumentType={DocumentType}, FileFormat={FileFormat}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId}), ContentType={ContentType}, FileSize={FileSize}",
                request.OrderNumber, request.DocumentType, request.FileFormat, request.Marketplace, firmaNazwa, firmaId, Request.ContentType, uploadedFile?.Length ?? 0);

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
            if (uploadedFile == null || uploadedFile.Length == 0)
                return BadRequest(new { success = false, error = "DocumentFile jest wymagany", message = "Plik nie może być pusty" });

            const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
            if (uploadedFile.Length > MaxFileSize)
                return BadRequest(new { success = false, error = "Plik zbyt duży", message = $"Maksymalny rozmiar pliku to {MaxFileSize / 1024 / 1024} MB" });

            var validFormats = new[] { "xml", "pdf" };
            if (!validFormats.Contains(request.FileFormat.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawidłowy format pliku", message = $"FileFormat musi być jedną z wartości: {string.Join(", ", validFormats)}" });

            var validDocumentTypes = new[] { "invoice", "correction" };
            if (!validDocumentTypes.Contains(request.DocumentType.ToLower()))
                return BadRequest(new { success = false, error = "Nieprawidłowy typ dokumentu", message = $"DocumentType musi być jedną z wartości: {string.Join(", ", validDocumentTypes)}" });

            

            try
            {
                // Przygotuj dane do wysłania do Olmed - z plikiem binarnym
                var fileName = $"Invoice_{request.DocumentNumber.Replace("/", "_").Replace("-", "_").Replace("(", "_").Replace(")", "_")}.pdf";
                var (success, response, statusCode) = await _olmedService.PostBinaryFileAsync(
                    "/erp-api/marketing-orders/upload-document-to-marketing-order",
                    request.Marketplace,
                    request.OrderNumber,
                    request.DocumentType,
                    request.FileFormat,
                    new StreamContent(uploadedFile.OpenReadStream()),
                    request.DocumentNumber, request.DocumentDateIssue, fileName);

                if (success)
                {
                    _logger.LogInformation("Pomyślnie przesłano dokument do zamówienia {OrderNumber}, typ: {DocumentType}, format: {FileFormat}, rozmiar: {FileSize} bajtów",
                        request.OrderNumber, request.DocumentType, request.FileFormat, uploadedFile.Length);
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
