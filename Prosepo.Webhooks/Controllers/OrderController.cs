using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs;
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
    public class OrderController : ControllerBase
    {
        private readonly OlmedApiService _olmedService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            OlmedApiService olmedService,
            ILogger<OrderController> logger)
        {
            _olmedService = olmedService;
            _logger = logger;
        }

        /// <summary>
        /// Aktualizuje status zamówienia w systemie Olmed
        /// </summary>
        /// <param name="orderId">Identyfikator zamówienia</param>
        /// <param name="orderStatus">Nowy status zamówienia (wartoœæ int)</param>
        /// <returns>Wynik operacji aktualizacji statusu</returns>
        /// <remarks>
        /// Przyk³ad u¿ycia:
        /// GET /api/order/update-status?orderId=ORD-12345&amp;orderStatus=2
        /// 
        /// Wymagany nag³ówek: X-API-Key
        /// </remarks>
        [HttpGet("update-status")]
        [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(
            [FromQuery] string orderId,
            [FromQuery] int orderStatus)
        {
            // Pobierz informacje o zalogowanej firmie
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie aktualizacji statusu zamówienia: OrderId={OrderId}, Status={Status}, Firma={FirmaNazwa} (ID: {FirmaId})",
                orderId, orderStatus, firmaNazwa, firmaId);

            // Walidacja parametrów
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "OrderId jest wymagany",
                    message = "Parametr orderId nie mo¿e byæ pusty"
                });
            }

            if (orderStatus < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Nieprawid³owy status",
                    message = "Parametr orderStatus musi byæ wartoœci¹ dodatni¹"
                });
            }

            try
            {
                // Przygotuj dane do wys³ania do Olmed
                var requestData = new
                {
                    orderId = orderId,
                    orderStatus = orderStatus,
                    updatedBy = firmaNazwa ?? "Unknown",
                    updatedAt = DateTime.UtcNow
                };

                // Wyœlij ¿¹danie do Olmed API
                var (success, response, statusCode) = await _olmedService.PostAsync(
                    "/erp-api/orders/update-status",
                    requestData);

                if (success)
                {
                    _logger.LogInformation(
                        "Pomyœlnie zaktualizowano status zamówienia {OrderId} na {Status}",
                        orderId, orderStatus);

                    return Ok(new UpdateOrderStatusResponse
                    {
                        Success = true,
                        Message = "Status zamówienia zosta³ pomyœlnie zaktualizowany",
                        OrderId = orderId,
                        NewStatus = orderStatus
                    });
                }
                else
                {
                    _logger.LogWarning(
                        "B³¹d podczas aktualizacji statusu zamówienia {OrderId}: StatusCode={StatusCode}, Response={Response}",
                        orderId, statusCode, response);

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
                    "Wyj¹tek podczas aktualizacji statusu zamówienia {OrderId}",
                    orderId);

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas aktualizacji statusu zamówienia"
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
