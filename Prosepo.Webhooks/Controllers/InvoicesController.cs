using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DbContext.Models;
using System.Text.Json;
using Prospeo.DbContext.Enums;
using Prospeo.DbContext.Interfaces;
using Prospeo.DTOs.Invoice;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Kontroler do zarz¹dzania fakturami
    /// Wymaga autentykacji tokenem API (X-API-Key)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class InvoicesController : ControllerBase
    {
        private readonly OlmedApiService _olmedService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IQueueService? _queueService;
        private readonly IFirmyService? _firmyService;


        public InvoicesController(
            OlmedApiService olmedService,
            ILogger<InvoicesController> logger,
             IQueueService? queueService = null, IFirmyService? firmyService = null)
        {
            _olmedService = olmedService;
            _logger = logger;
            _queueService = queueService;
            _firmyService = firmyService;
        }

        /// <summary>
        /// Zg³asza wys³anie faktury do systemu Olmed
        /// </summary>
        /// <param name="request">Dane o wys³anej fakturze (JSON w body)</param>
        /// <returns>Wynik operacji zg³oszenia wys³ania faktury</returns>
        /// <remarks>
        /// Przyk³ad u¿ycia:
        /// POST /api/invoice/sent
        /// Content-Type: application/json
        /// X-API-Key: {your-api-key}
        /// 
        /// Body:
        /// {
        ///   "invoiceNumber": "FV/2024/001",
        ///   "orderId": "ORD-12345",
        ///   "sentDate": "2024-01-15T10:30:00Z",
        ///   "recipientEmail": "customer@example.com",
        ///   "additionalData": {
        ///     "deliveryMethod": "email",
        ///     "notes": "Faktura wys³ana mailem"
        ///   }
        /// }
        /// </remarks>
        [HttpPost("sent")]
        [ProducesResponseType(typeof(InvoiceSentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Sent([FromBody] InvoiceSentRequest request)
        {
            // Pobierz informacje o zalogowanej firmie
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie zg³oszenia wys³ania faktury: InvoiceNumber={InvoiceNumber}, OrderId={OrderId}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.InvoiceNumber, request.OrderId, firmaNazwa, firmaId);

            // Walidacja parametrów
            if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "InvoiceNumber jest wymagany",
                    message = "Numer faktury nie mo¿e byæ pusty"
                });
            }

            if (request.SentDate == default)
            {
                request.SentDate = DateTime.UtcNow;
            }

            try
            {
                // Przygotuj dane do wys³ania do Olmed
                var requestData = new
                {
                    invoiceNumber = request.InvoiceNumber,
                    orderId = request.OrderId,
                    sentDate = request.SentDate,
                    recipientEmail = request.RecipientEmail,
                    additionalData = request.AdditionalData,
                    reportedBy = firmaNazwa ?? "Unknown",
                    reportedAt = DateTime.UtcNow,
                    firmaId = firmaId
                };

                // Wyœlij ¿¹danie do Olmed API
                var (success, response, statusCode) = await _olmedService.PostAsync(
                    "/erp-api/invoices/sent",
                    requestData);

                if (success)
                {
                    _logger.LogInformation(
                        "Pomyœlnie zg³oszono wys³anie faktury {InvoiceNumber} dla zamówienia {OrderId}",
                        request.InvoiceNumber, request.OrderId);

                    return Ok(new InvoiceSentResponse
                    {
                        Success = true,
                        Message = "Wys³anie faktury zosta³o pomyœlnie zg³oszone",
                        InvoiceNumber = request.InvoiceNumber,
                        ProcessedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning(
                        "B³¹d podczas zg³aszania wys³ania faktury {InvoiceNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.InvoiceNumber, statusCode, response);

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        error = "B³¹d podczas zg³aszania wys³ania faktury w systemie Olmed",
                        message = response,
                        statusCode = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wyj¹tek podczas zg³aszania wys³ania faktury {InvoiceNumber}",
                    request.InvoiceNumber);

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas zg³aszania wys³ania faktury"
                });
            }
        }

        /// <summary>
        /// Pobiera listê faktur z systemu Olmed
        /// </summary>
        /// <param name="orderId">Opcjonalny identyfikator zamówienia do filtrowania</param>
        /// <returns>Lista faktur</returns>
        [HttpGet("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInvoices([FromQuery] string? orderId = null)
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie pobrania listy faktur: OrderId={OrderId}, Firma={FirmaNazwa} (ID: {FirmaId})",
                orderId, firmaNazwa, firmaId);

            try
            {
                var endpoint = string.IsNullOrWhiteSpace(orderId)
                    ? "/erp-api/invoices/list"
                    : $"/erp-api/invoices/list?orderId={orderId}";

                var (success, response, statusCode) = await _olmedService.GetAsync(endpoint);

                if (success)
                {
                    _logger.LogInformation("Pomyœlnie pobrano listê faktur");
                    
                    // Parsuj odpowiedŸ jako JSON
                    var invoices = JsonSerializer.Deserialize<object>(response ?? "{}");
                    
                    return Ok(new
                    {
                        success = true,
                        data = invoices
                    });
                }
                else
                {
                    _logger.LogWarning(
                        "B³¹d podczas pobierania listy faktur: StatusCode={StatusCode}, Response={Response}",
                        statusCode, response);

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        error = "B³¹d podczas pobierania listy faktur z systemu Olmed",
                        message = response,
                        statusCode = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyj¹tek podczas pobierania listy faktur");

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas pobierania listy faktur"
                });
            }
        }

        /// <summary>
        /// Wewnêtrzny endpoint do zg³aszania wys³ania faktury zakupu
        /// Dodaje fakturê do kolejki przetwarzania dla okreœlonej firmy
        /// </summary>
        /// <param name="request">Model faktury zakupowej zawieraj¹cy dane do przetworzenia</param>
        /// <param name="companyId">Identyfikator firmy, dla której dodawana jest faktura do kolejki</param>
        /// <returns>Wynik operacji dodania faktury do kolejki</returns>
        /// <remarks>
        /// Endpoint wewnêtrzny przeznaczony do komunikacji miêdzy serwisami.
        /// Faktura jest dodawana do kolejki w formacie JSON z nastêpuj¹cymi parametrami:
        /// - Scope: okreœlany na podstawie typu dokumentu (request.Typ)
        /// - Request: serializowany model faktury
        /// - Description: pusty dla nowych wpisów
        /// - TargetID: 0 dla nowych wpisów
        /// - Flg: 0 (domyœlna wartoœæ dla przetwarzania webhook)
        /// 
        /// Przyk³ad u¿ycia:
        /// POST /api/invoice/sent-interal?companyId=1
        /// Content-Type: application/json
        /// X-API-Key: {your-api-key}
        /// 
        /// Body:
        /// {
        ///   "dokumentObcy": "FZ/2024/001",
        ///   "termin": 45000,
        ///   "typ": 1,
        ///   "akronim": "DOSTAWCA01"
        /// }
        /// </remarks>
        [HttpPost("sent-interal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SentInternal([FromBody] PurchaseInvoiceModelDTO request, [FromQuery] int companyId)
        {

            try
            {
                var scope = (QueueScope)request.Typ;
                var queueItem = new Queue
                {
                    FirmaId = companyId,
                    Scope = (int)scope,
                    Request = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }),
                    Description = "",//Always empty for new entries
                    TargetID = 0, //Always zero for new entries
                    Flg = 0,//Default flag for webhook processing = 0
                    DateAddDateTime = DateTime.UtcNow,
                    DateModDateTime = DateTime.UtcNow
                };
                await _queueService.AddAsync(queueItem);
                return Ok(new
                {
                    success = true,
                    message = "Faktura zosta³a dodana do kolejki przetwarzania"
                });
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wyj¹tek podczas dodawania faktury do kolejki: FirmaId={FirmaId}, InvoiceNumber={InvoiceNumber}",
                    companyId, request.DokumentObcy);
                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas dodawania faktury do kolejki"
                });
            }
        }

        /// <summary>
        /// Pobiera informacje o zalogowanej firmie na podstawie API Key
        /// </summary>
        /// <returns>Dane firmy</returns>
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
