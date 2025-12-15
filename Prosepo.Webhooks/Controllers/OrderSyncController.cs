using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Kontroler demonstracyjny dla OrderSyncConfigurationService.
    /// Pokazuje przyk³ady u¿ycia serwisu do zarz¹dzania konfiguracjami synchronizacji zamówieñ.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderSyncController : ControllerBase
    {
        private readonly OrderSyncConfigurationService _configService;
        private readonly ILogger<OrderSyncController> _logger;

        public OrderSyncController(
            OrderSyncConfigurationService configService,
            ILogger<OrderSyncController> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Pobiera wszystkie aktywne konfiguracje synchronizacji zamówieñ.
        /// </summary>
        /// <returns>Lista aktywnych konfiguracji</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(List<OrderSyncConfiguration>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveConfigurations()
        {
            try
            {
                var configurations = await _configService.GetActiveConfigurationsAsync();
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania aktywnych konfiguracji");
                return StatusCode(500, new { error = "B³¹d podczas pobierania konfiguracji" });
            }
        }

        /// <summary>
        /// Pobiera wszystkie konfiguracje synchronizacji zamówieñ (aktywne i nieaktywne).
        /// </summary>
        /// <returns>Lista wszystkich konfiguracji</returns>
        [HttpGet("all")]
        [ProducesResponseType(typeof(List<OrderSyncConfiguration>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllConfigurations()
        {
            try
            {
                var configurations = await _configService.GetAllConfigurationsAsync();
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania wszystkich konfiguracji");
                return StatusCode(500, new { error = "B³¹d podczas pobierania konfiguracji" });
            }
        }

        /// <summary>
        /// Pobiera konkretn¹ konfiguracjê po ID.
        /// </summary>
        /// <param name="id">Identyfikator konfiguracji</param>
        /// <returns>Konfiguracja jeœli istnieje</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderSyncConfiguration), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConfigurationById(string id)
        {
            try
            {
                var configuration = await _configService.GetConfigurationByIdAsync(id);
                
                if (configuration == null)
                {
                    return NotFound(new { error = $"Konfiguracja o ID '{id}' nie zosta³a znaleziona" });
                }

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji {Id}", id);
                return StatusCode(500, new { error = "B³¹d podczas pobierania konfiguracji" });
            }
        }

        /// <summary>
        /// Generuje podgl¹d ¿¹dania HTTP dla danej konfiguracji (bez wykonywania).
        /// Pokazuje URL, headers, body z dynamicznie wygenerowanymi datami.
        /// </summary>
        /// <param name="id">Identyfikator konfiguracji</param>
        /// <returns>Podgl¹d ¿¹dania</returns>
        [HttpGet("{id}/preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRequestPreview(string id)
        {
            try
            {
                var preview = await _configService.GetRequestPreviewAsync(id);
                
                if (preview == null)
                {
                    return NotFound(new { error = $"Konfiguracja o ID '{id}' nie zosta³a znaleziona" });
                }

                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas generowania podgl¹du dla {Id}", id);
                return StatusCode(500, new { error = "B³¹d podczas generowania podgl¹du" });
            }
        }

        /// <summary>
        /// Generuje tylko body ¿¹dania z dynamicznymi datami dla danej konfiguracji.
        /// </summary>
        /// <param name="id">Identyfikator konfiguracji</param>
        /// <returns>Body ¿¹dania jako JSON string</returns>
        [HttpGet("{id}/body")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateRequestBody(string id)
        {
            try
            {
                var configuration = await _configService.GetConfigurationByIdAsync(id);
                
                if (configuration == null)
                {
                    return NotFound(new { error = $"Konfiguracja o ID '{id}' nie zosta³a znaleziona" });
                }

                var body = _configService.GenerateRequestBody(configuration);
                
                return Ok(new
                {
                    configurationId = id,
                    body = body,
                    bodyParsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas generowania body dla {Id}", id);
                return StatusCode(500, new { error = "B³¹d podczas generowania body" });
            }
        }

        /// <summary>
        /// Dodaje lub aktualizuje konfiguracjê synchronizacji zamówieñ.
        /// </summary>
        /// <param name="configuration">Konfiguracja do zapisania</param>
        /// <returns>Wynik operacji</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveConfiguration([FromBody] OrderSyncConfiguration configuration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configuration.Id))
                {
                    return BadRequest(new { error = "ID konfiguracji nie mo¿e byæ puste" });
                }

                if (string.IsNullOrWhiteSpace(configuration.Marketplace))
                {
                    return BadRequest(new { error = "Marketplace nie mo¿e byæ pusty" });
                }

                if (string.IsNullOrWhiteSpace(configuration.Url))
                {
                    return BadRequest(new { error = "URL nie mo¿e byæ pusty" });
                }

                var success = await _configService.SaveConfigurationAsync(configuration);
                
                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Konfiguracja zosta³a zapisana pomyœlnie",
                        configurationId = configuration.Id
                    });
                }

                return StatusCode(500, new { error = "B³¹d podczas zapisywania konfiguracji" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania konfiguracji");
                return StatusCode(500, new { error = "B³¹d podczas zapisywania konfiguracji" });
            }
        }

        /// <summary>
        /// Usuwa konfiguracjê synchronizacji zamówieñ.
        /// </summary>
        /// <param name="id">Identyfikator konfiguracji do usuniêcia</param>
        /// <returns>Wynik operacji</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteConfiguration(string id)
        {
            try
            {
                var deleted = await _configService.DeleteConfigurationAsync(id);
                
                if (deleted)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Konfiguracja '{id}' zosta³a usuniêta"
                    });
                }

                return NotFound(new { error = $"Konfiguracja o ID '{id}' nie zosta³a znaleziona" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas usuwania konfiguracji {Id}", id);
                return StatusCode(500, new { error = "B³¹d podczas usuwania konfiguracji" });
            }
        }

        /// <summary>
        /// Odœwie¿a cache konfiguracji.
        /// Wymusza ponowne wczytanie pliku konfiguracyjnego.
        /// </summary>
        /// <returns>Wynik operacji</returns>
        [HttpPost("refresh-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult RefreshCache()
        {
            try
            {
                _configService.RefreshCache();
                return Ok(new
                {
                    success = true,
                    message = "Cache konfiguracji zosta³ odœwie¿ony"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas odœwie¿ania cache");
                return StatusCode(500, new { error = "B³¹d podczas odœwie¿ania cache" });
            }
        }

        /// <summary>
        /// Tworzy przyk³adow¹ konfiguracjê demonstracyjn¹.
        /// U¿yteczne do testowania i jako szablon.
        /// </summary>
        /// <returns>Przyk³adowa konfiguracja</returns>
        [HttpGet("example")]
        [ProducesResponseType(typeof(OrderSyncConfiguration), StatusCodes.Status200OK)]
        public IActionResult GetExampleConfiguration()
        {
            var example = new OrderSyncConfiguration
            {
                Id = "example-sync-orders",
                Name = "Przyk³adowa synchronizacja zamówieñ",
                Description = "Konfiguracja demonstracyjna - pobieranie zamówieñ co 4 godziny",
                IsActive = false, // Domyœlnie nieaktywna
                IntervalSeconds = 14400, // 4 godziny
                Method = "POST",
                Url = "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
                UseOlmedAuth = true,
                Headers = new Dictionary<string, string>
                {
                    { "accept", "application/json" },
                    { "Content-Type", "application/json" },
                    { "X-CSRF-TOKEN", "" }
                },
                Marketplace = "APTEKA_OLMED",
                DateRangeDays = 3, // 3 dni wstecz
                UseCurrentDateAsEndDate = true,
                DateFormat = "yyyy-MM-dd",
                AdditionalParameters = new Dictionary<string, object>()
            };

            return Ok(example);
        }
    }
}
