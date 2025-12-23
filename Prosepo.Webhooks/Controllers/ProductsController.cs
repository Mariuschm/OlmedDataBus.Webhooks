using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DTOs;
using System.Text.Json;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Kontroler do zarz¹dzania stanami magazynowymi
    /// Wymaga autentykacji tokenem API (X-API-Key)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class ProductsController : ControllerBase
    {
        private readonly OlmedApiService _olmedService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            OlmedApiService olmedService,
            ILogger<ProductsController> logger)
        {
            _olmedService = olmedService;
            _logger = logger;
        }

        /// <summary>
        /// Aktualizuje stany magazynowe produktów w systemie Olmed
        /// </summary>
        /// <param name="request">Dane do aktualizacji stanów magazynowych (JSON w body)</param>
        /// <returns>Wynik operacji aktualizacji stanów</returns>
        /// <remarks>
        /// Przyk³ad u¿ycia:
        /// POST /api/stocks/update
        /// Content-Type: application/json
        /// X-API-Key: {your-api-key}
        /// 
        /// Body:
        /// {
        ///   "marketplace": "APTEKA_OLMED",
        ///   "skus": {
        ///     "14978": {
        ///       "stock": 35,
        ///       "average_purchase_price": 10.04
        ///     },
        ///     "111714": {
        ///       "stock": 120,
        ///       "average_purchase_price": 15.14
        ///     }
        ///   },
        ///   "notes": "Aktualizacja stanów magazynowych",
        ///   "updateDate": "2024-01-15T10:30:00Z"
        /// }
        /// 
        /// Wymagany nag³ówek: X-API-Key
        /// </remarks>
        [HttpPost("update-product-stocks")]
        [ProducesResponseType(typeof(StockUpdateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStock([FromBody] StockUpdateRequest request)
        {
            // Pobierz informacje o zalogowanej firmie
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();

            _logger.LogInformation(
                "¯¹danie aktualizacji stanów magazynowych: Marketplace={Marketplace}, SkuCount={SkuCount}, Firma={FirmaNazwa} (ID: {FirmaId})",
                request.Marketplace, request.Skus?.Count ?? 0, firmaNazwa, firmaId);

            // Walidacja parametrów
            if (string.IsNullOrWhiteSpace(request.Marketplace))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Marketplace jest wymagany",
                    message = "Parametr marketplace nie mo¿e byæ pusty"
                });
            }

            if (request.Skus == null || request.Skus.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Brak SKU do aktualizacji",
                    message = "Parametr skus musi zawieraæ co najmniej jeden element"
                });
            }

            // Walidacja poszczególnych SKU
            foreach (var sku in request.Skus)
            {
                if (sku.Value.Stock < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Nieprawid³owy stan magazynowy",
                        message = $"Stan magazynowy dla SKU '{sku.Key}' nie mo¿e byæ ujemny"
                    });
                }

                if (sku.Value.AveragePurchasePrice < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Nieprawid³owa cena zakupu",
                        message = $"Œrednia cena zakupu dla SKU '{sku.Key}' nie mo¿e byæ ujemna"
                    });
                }
            }
            try
            {
                // Przygotuj dane do wys³ania do Olmed
                var requestData = new
                {
                    marketplace = request.Marketplace,
                    skus = request.Skus,
                    notes = request.Notes,
                    updateDate = request.UpdateDate ?? DateTime.UtcNow,
                    updatedBy = firmaNazwa ?? "Unknown",
                    firmaId = firmaId
                };

                // Wyœlij ¿¹danie do Olmed API
                var (success, response, statusCode) = await _olmedService.PostAsync(
                    "/erp-api/products/update-product-stocks",
                    requestData);

                if (success)
                {
                    _logger.LogInformation(
                        "Pomyœlnie zaktualizowano stany magazynowe dla {SkuCount} SKU w marketplace {Marketplace}",
                        request.Skus.Count, request.Marketplace);

                    return Ok(new StockUpdateResponse
                    {
                        Success = true,
                        Message = "Stany magazynowe zosta³y pomyœlnie zaktualizowane",
                        Marketplace = request.Marketplace,
                        UpdatedCount = request.Skus.Count,
                        UpdatedSkus = request.Skus.Keys.ToList(),
                        ProcessedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning(
                        "B³¹d podczas aktualizacji stanów magazynowych dla marketplace {Marketplace}: StatusCode={StatusCode}, Response={Response}",
                        request.Marketplace, statusCode, response);

                    return StatusCode(statusCode, new
                    {
                        success = false,
                        error = "B³¹d podczas aktualizacji stanów magazynowych w systemie Olmed",
                        message = response,
                        statusCode = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wyj¹tek podczas aktualizacji stanów magazynowych dla marketplace {Marketplace}",
                    request.Marketplace);

                return StatusCode(500, new
                {
                    success = false,
                    error = "B³¹d serwera",
                    message = "Wyst¹pi³ nieoczekiwany b³¹d podczas aktualizacji stanów magazynowych"
                });
            }
        }
        
    }
}
