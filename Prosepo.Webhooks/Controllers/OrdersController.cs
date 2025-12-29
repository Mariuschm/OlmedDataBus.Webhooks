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
        /// <remarks>
        /// Przyk³ad u¿ycia:
        /// POST /api/order/update-status
        /// Body:
        /// {
        ///   "marketplace": "APTEKA_OLMED",
        ///   "orderNumber": "ORD/2024/01/0001",
        ///   "status": "2",
        ///   "note": "Status changed due to ...",
        ///   "trackingNumber": "TRACK123456"
        /// }
        /// 
        /// Wymagany nag³ówek: X-API-Key
        /// </remarks>
        [HttpPost("update-status")]
        [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request)
        {
            // Pobierz informacje o zalogowanej firmie
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie aktualizacji statusu zamówienia: OrderNumber={OrderNumber}, Status={Status}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.OrderNumber, request.Status, request.Marketplace, firmaNazwa, firmaId);

            // Walidacja parametrów
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "OrderNumber jest wymagany",
                    message = "Parametr orderNumber nie mo¿e byæ pusty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Status jest wymagany",
                    message = "Parametr status nie mo¿e byæ pusty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Marketplace))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Marketplace jest wymagany",
                    message = "Parametr marketplace nie mo¿e byæ pusty"
                });
            }

            try
            {
                // Przygotuj dane do wys³ania do Olmed
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

                // Wyœlij ¿¹danie do Olmed API
                var (success, response, statusCode) = await _olmedService.PostAsync(
                    "/erp-api/orders/change-status",
                    requestData);

                if (success)
                {
                    _logger.LogInformation(
                        "Pomyœlnie zaktualizowano status zamówienia {OrderNumber} na {Status}",
                        request.OrderNumber, request.Status);

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
                    _logger.LogWarning(
                        "B³¹d podczas aktualizacji statusu zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        error = "B³¹d podczas aktualizacji statusu w systemie Olmed",
                        message = response,
                        statusCode = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wyj¹tek podczas aktualizacji statusu zamówienia {OrderNumber}",
                    request.OrderNumber);

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas aktualizacji statusu zamówienia"
                });
            }
        }

        /// <summary>
        /// Przesy³a wyniki realizacji zamówienia do systemu Olmed
        /// </summary>
        /// <param name="request">Dane ¿¹dania z wynikami realizacji</param>
        /// <returns>Wynik operacji przes³ania wyników realizacji</returns>
        /// <remarks>
        /// Przyk³ad u¿ycia:
        /// POST /api/order/upload-order-realization-result
        /// Body:
        /// {
        ///   "marketplace": "APTEKA_OLMED",
        ///   "orderNumber": "ORD/2024/01/0001",
        ///   "items": [
        ///     {
        ///       "sku": "SKU123",
        ///       "quantity": 5,
        ///       "expirationDate": "2025-12-31",
        ///       "seriesNumber": "SER12345"
        ///     }
        ///   ]
        /// }
        /// 
        /// Wymagany nag³ówek: X-API-Key
        /// </remarks>
        [HttpPost("upload-order-realization-result")]
        [ProducesResponseType(typeof(UploadOrderRealizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadOrderRealizationResult([FromBody] UploadOrderRealizationRequest request)
        {
            // Pobierz informacje o zalogowanej firmie
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie przes³ania wyników realizacji zamówienia: OrderNumber={OrderNumber}, ItemsCount={ItemsCount}, Marketplace={Marketplace}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.OrderNumber, request.Items?.Count ?? 0, request.Marketplace, firmaNazwa, firmaId);

            // Walidacja parametrów
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "OrderNumber jest wymagany",
                    message = "Parametr orderNumber nie mo¿e byæ pusty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Marketplace))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Marketplace jest wymagany",
                    message = "Parametr marketplace nie mo¿e byæ pusty"
                });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Items s¹ wymagane",
                    message = "Lista items nie mo¿e byæ pusta"
                });
            }

            // Walidacja pojedynczych pozycji
            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                
                if (string.IsNullOrWhiteSpace(item.Sku))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "SKU jest wymagany",
                        message = $"SKU pozycji {i + 1} nie mo¿e byæ pusty"
                    });
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Nieprawid³owa iloœæ",
                        message = $"Iloœæ pozycji {i + 1} musi byæ wartoœci¹ dodatni¹"
                    });
                }
            }

            try
            {
                // Przygotuj dane do wys³ania do Olmed
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

                // Wyœlij ¿¹danie do Olmed API
                var (success, response, statusCode) = await _olmedService.PostAsync(
                    "/erp-api/orders/upload-realization-result",
                    requestData);

                if (success)
                {
                    _logger.LogInformation(
                        "Pomyœlnie przes³ano wyniki realizacji zamówienia {OrderNumber}, pozycji: {ItemsCount}",
                        request.OrderNumber, request.Items.Count);

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
                    _logger.LogWarning(
                        "B³¹d podczas przesy³ania wyników realizacji zamówienia {OrderNumber}: StatusCode={StatusCode}, Response={Response}",
                        request.OrderNumber, statusCode, response);

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        error = "B³¹d podczas przesy³ania wyników realizacji w systemie Olmed",
                        message = response,
                        statusCode = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wyj¹tek podczas przesy³ania wyników realizacji zamówienia {OrderNumber}",
                    request.OrderNumber);

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas przesy³ania wyników realizacji zamówienia"
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
