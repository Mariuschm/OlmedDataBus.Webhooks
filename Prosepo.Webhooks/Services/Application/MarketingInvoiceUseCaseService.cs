using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs.Order;

namespace Prosepo.Webhooks.Services.Application;

public class MarketingInvoiceUseCaseService : IMarketingInvoiceUseCaseService
{
    private readonly OlmedApiService _olmedService;
    private readonly ILogger<MarketingInvoiceUseCaseService> _logger;

    public MarketingInvoiceUseCaseService(OlmedApiService olmedService, ILogger<MarketingInvoiceUseCaseService> logger)
    {
        _olmedService = olmedService;
        _logger = logger;
    }

    public async Task<IActionResult> UploadDocumentToMarketingOrderAsync(HttpRequest request, string? firmaId, string? firmaNazwa)
    {
        UploadDocumentToOrderRequest? parsedRequest = null;
        IFormFile? uploadedFile = null;

        try
        {
            var contentType = request.ContentType?.ToLower() ?? string.Empty;
            if (!contentType.Contains("multipart/form-data"))
            {
                return ApiResultFactory.ValidationError("Nieprawidłowy Content-Type", "Endpoint akceptuje tylko multipart/form-data z plikiem binarnym");
            }

            var form = await request.ReadFormAsync();
            uploadedFile = form.Files.GetFile("documentFile") ?? form.Files.FirstOrDefault();

            parsedRequest = new UploadDocumentToOrderRequest
            {
                Marketplace = form["marketplace"].ToString(),
                OrderNumber = form["orderNumber"].ToString(),
                DocumentType = form["documentType"].ToString(),
                FileFormat = form["fileFormat"].ToString(),
                DocumentFile = new StreamContent(uploadedFile?.OpenReadStream() ?? Stream.Null),
                DocumentNumber = form["documentNumber"].ToString(),
                DocumentDateIssue = form["documentDateIssue"].ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd odczytu żądania UploadDocumentToMarketingOrder");
            return ApiResultFactory.ValidationError("Błąd odczytu żądania", $"Nie można odczytać danych: {ex.Message}");
        }

        _logger.LogInformation("Żądanie przesłania dokumentu do zamówienia marketingowego: OrderNumber={OrderNumber}, DocumentType={DocumentType}, FileFormat={FileFormat}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId}), ContentType={ContentType}, FileSize={FileSize}",
            parsedRequest.OrderNumber, parsedRequest.DocumentType, parsedRequest.FileFormat, parsedRequest.Marketplace, firmaNazwa, firmaId, request.ContentType, uploadedFile?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(parsedRequest.OrderNumber))
            return new BadRequestObjectResult(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie może być pusty" });
        if (string.IsNullOrWhiteSpace(parsedRequest.Marketplace))
            return new BadRequestObjectResult(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie może być pusty" });
        if (string.IsNullOrWhiteSpace(parsedRequest.DocumentType))
            return new BadRequestObjectResult(new { success = false, error = "DocumentType jest wymagany", message = "Parametr documentType nie może być pusty" });
        if (string.IsNullOrWhiteSpace(parsedRequest.FileFormat))
            return new BadRequestObjectResult(new { success = false, error = "FileFormat jest wymagany", message = "Parametr fileFormat nie może być pusty" });
        if (string.IsNullOrWhiteSpace(parsedRequest.DocumentNumber))
            return new BadRequestObjectResult(new { success = false, error = "DocumentNumber jest wymagany", message = "Parametr documentNumber nie może być pusty" });
        if (uploadedFile == null || uploadedFile.Length == 0)
            return new BadRequestObjectResult(new { success = false, error = "DocumentFile jest wymagany", message = "Plik nie może być pusty" });

        const long maxFileSize = 10 * 1024 * 1024;
        if (uploadedFile.Length > maxFileSize)
            return new BadRequestObjectResult(new { success = false, error = "Plik zbyt duży", message = $"Maksymalny rozmiar pliku to {maxFileSize / 1024 / 1024} MB" });

        var validFormats = new[] { "xml", "pdf" };
        if (!validFormats.Contains(parsedRequest.FileFormat.ToLowerInvariant()))
            return new BadRequestObjectResult(new { success = false, error = "Nieprawidłowy format pliku", message = $"FileFormat musi być jedną z wartości: {string.Join(", ", validFormats)}" });

        var validDocumentTypes = new[] { "invoice", "correction" };
        if (!validDocumentTypes.Contains(parsedRequest.DocumentType.ToLowerInvariant()))
            return new BadRequestObjectResult(new { success = false, error = "Nieprawidłowy typ dokumentu", message = $"DocumentType musi być jedną z wartości: {string.Join(", ", validDocumentTypes)}" });

        try
        {
            var fileName = $"Invoice_{parsedRequest.DocumentNumber.Replace("/", "_").Replace("-", "_").Replace("(", "_").Replace(")", "_")}.pdf";
            var (success, response, statusCode) = await _olmedService.PostBinaryFileAsync(
                "/erp-api/marketing-orders/upload-document-to-marketing-order",
                parsedRequest.Marketplace,
                parsedRequest.OrderNumber,
                parsedRequest.DocumentType,
                parsedRequest.FileFormat,
                new StreamContent(uploadedFile.OpenReadStream()),
                parsedRequest.DocumentNumber,
                parsedRequest.DocumentDateIssue,
                fileName);

            if (success)
            {
                _logger.LogInformation("Pomyślnie przesłano dokument do zamówienia {OrderNumber}, typ: {DocumentType}, format: {FileFormat}, rozmiar: {FileSize} bajtów",
                    parsedRequest.OrderNumber, parsedRequest.DocumentType, parsedRequest.FileFormat, uploadedFile.Length);
                return new OkObjectResult(new UploadDocumentToOrderResponse
                {
                    Success = true,
                    Message = "Dokument został pomyślnie przesłany do zamówienia",
                    OrderNumber = parsedRequest.OrderNumber,
                    DocumentType = parsedRequest.DocumentType
                });
            }

            _logger.LogWarning("Błąd podczas przesyłania dokumentu do zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                parsedRequest.OrderNumber, statusCode, response);
            return ApiResultFactory.Error(statusCode, "Błąd podczas przesyłania dokumentu w systemie Olmed", response ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wyjątek podczas przesyłania dokumentu do zamówienia {OrderNumber}", parsedRequest.OrderNumber);
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił nieoczekiwany błąd podczas przesyłania dokumentu do zamówienia");
        }
    }
}