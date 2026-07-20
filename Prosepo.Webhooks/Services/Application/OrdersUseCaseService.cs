using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;

namespace Prosepo.Webhooks.Services.Application;

public class OrdersUseCaseService : IOrdersUseCaseService
{
    private readonly OlmedApiService _olmedService;
    private readonly ILogger<OrdersUseCaseService> _logger;

    public OrdersUseCaseService(OlmedApiService olmedService, ILogger<OrdersUseCaseService> logger)
    {
        _olmedService = olmedService;
        _logger = logger;
    }

    public async Task<IActionResult> UpdateStatusAsync(UpdateOrderStatusRequest request, string? firmaId, string? firmaNazwa)
    {
        _logger.LogInformation(
            "Żądanie aktualizacji statusu zamówienia: OrderNumber={OrderNumber}, Status={Status}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
            request.OrderNumber, request.Status, request.Marketplace, firmaNazwa, firmaId);

        if (string.IsNullOrWhiteSpace(request.OrderNumber))
            return new BadRequestObjectResult(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie może być pusty" });

        if (string.IsNullOrWhiteSpace(request.Status))
            return ApiResultFactory.ValidationError("Status jest wymagany", "Parametr status nie może być pusty");

        if (string.IsNullOrWhiteSpace(request.Marketplace))
            return new BadRequestObjectResult(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie może być pusty" });

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
                _logger.LogInformation("Pomyślnie zaktualizowano status zamówienia {OrderNumber} na {Status}", request.OrderNumber, request.Status);
                return new OkObjectResult(new UpdateOrderStatusResponse
                {
                    Success = true,
                    Message = "Status zamówienia został pomyślnie zaktualizowany",
                    OrderNumber = request.OrderNumber,
                    NewStatus = request.Status
                });
            }

            _logger.LogWarning("Błąd podczas aktualizacji statusu zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                request.OrderNumber, statusCode, response);
            return ApiResultFactory.Error(statusCode, "Błąd podczas aktualizacji statusu w systemie Olmed", response ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wyjątek podczas aktualizacji statusu zamówienia {OrderNumber}", request.OrderNumber);
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił nieoczekiwany błąd podczas aktualizacji statusu zamówienia");
        }
    }

    public async Task<IActionResult> UploadOrderRealizationResultAsync(UploadOrderRealizationRequest request, string? firmaId, string? firmaNazwa)
    {
        _logger.LogInformation("Żądanie przesłania wyników realizacji zamówienia: OrderNumber={OrderNumber}, ItemsCount={ItemsCount}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
            request.OrderNumber, request.Items?.Count ?? 0, request.Marketplace, firmaNazwa, firmaId);

        if (string.IsNullOrWhiteSpace(request.OrderNumber))
            return new BadRequestObjectResult(new { success = false, error = "OrderNumber jest wymagany", message = "Parametr orderNumber nie może być pusty" });

        if (string.IsNullOrWhiteSpace(request.Marketplace))
            return new BadRequestObjectResult(new { success = false, error = "Marketplace jest wymagany", message = "Parametr marketplace nie może być pusty" });

        if (request.Items == null || request.Items.Count == 0)
            return new BadRequestObjectResult(new { success = false, error = "Items są wymagane", message = "Lista items nie może być pusta" });

        for (int i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            if (string.IsNullOrWhiteSpace(item.Sku))
                return new BadRequestObjectResult(new { success = false, error = "SKU jest wymagany", message = $"SKU pozycji {i + 1} nie może być pusty" });
            if (item.Quantity <= 0)
                return new BadRequestObjectResult(new { success = false, error = "Nieprawidłowa ilość", message = $"Ilość pozycji {i + 1} musi być wartością dodatnią" });
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
                _logger.LogInformation("Pomyślnie przesłano wyniki realizacji zamówienia {OrderNumber}, pozycji: {ItemsCount}", request.OrderNumber, request.Items.Count);
                return new OkObjectResult(new UploadOrderRealizationResponse
                {
                    Success = true,
                    Message = "Wyniki realizacji zamówienia zostały pomyślnie przesłane",
                    OrderNumber = request.OrderNumber,
                    ItemsProcessed = request.Items.Count
                });
            }

            _logger.LogWarning("Błąd podczas przesyłania wyników realizacji zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                request.OrderNumber, statusCode, response);
            return new ObjectResult(new { success = false, error = "Błąd podczas przesyłania wyników realizacji w systemie Olmed", message = response, statusCode })
            {
                StatusCode = statusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wyjątek podczas przesyłania wyników realizacji zamówienia {OrderNumber}", request.OrderNumber);
            return new ObjectResult(new { success = false, error = "Błąd serwera", message = "Wystąpił nieoczekiwany błąd podczas przesyłania wyników realizacji zamówienia" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<IActionResult> UploadDocumentToOrderAsync(HttpRequest request, string? firmaId, string? firmaNazwa)
    {
        UploadDocumentToOrderRequest? parsedRequest = null;
        StreamContent uploadedFileStream;
        IFormFile? uploadedFile;

        try
        {
            var contentType = request.ContentType?.ToLower() ?? string.Empty;
            if (!contentType.Contains("multipart/form-data"))
            {
                return new BadRequestObjectResult(new { success = false, error = "Nieprawidłowy Content-Type", message = "Endpoint akceptuje tylko multipart/form-data z plikiem binarnym" });
            }

            var form = await request.ReadFormAsync();
            uploadedFile = form.Files.GetFile("documentFile") ?? form.Files.FirstOrDefault();
            uploadedFileStream = new StreamContent(uploadedFile!.OpenReadStream());

            parsedRequest = new UploadDocumentToOrderRequest
            {
                Marketplace = form["marketplace"].ToString(),
                OrderNumber = form["orderNumber"].ToString(),
                DocumentType = form["documentType"].ToString(),
                FileFormat = form["fileFormat"].ToString(),
                DocumentFile = uploadedFileStream,
                DocumentNumber = form["documentNumber"].ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd odczytu żądania UploadDocumentToOrder");
            return new BadRequestObjectResult(new { success = false, error = "Błąd odczytu żądania", message = $"Nie można odczytać danych: {ex.Message}" });
        }

        _logger.LogInformation("Żądanie przesłania dokumentu do zamówienia: OrderNumber={OrderNumber}, DocumentType={DocumentType}, FileFormat={FileFormat}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId}), ContentType={ContentType}, FileSize={FileSize}",
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
            var (success, response, statusCode) = await _olmedService.PostBinaryFileAsync(
                "/erp-api/orders/upload-document-to-order",
                parsedRequest.Marketplace,
                parsedRequest.OrderNumber,
                parsedRequest.DocumentType,
                parsedRequest.FileFormat,
                uploadedFileStream,
                parsedRequest.DocumentNumber,
                null,
                null);

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
            return new ObjectResult(new { success = false, error = "Błąd podczas przesyłania dokumentu w systemie Olmed", message = response, statusCode })
            {
                StatusCode = statusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wyjątek podczas przesyłania dokumentu do zamówienia {OrderNumber}", parsedRequest.OrderNumber);
            return new ObjectResult(new { success = false, error = "Błąd serwera", message = "Wystąpił nieoczekiwany błąd podczas przesyłania dokumentu do zamówienia" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}