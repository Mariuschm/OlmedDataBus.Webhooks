using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Prospeo.DbContext.Interfaces;
using Prospeo.DTOs.Core;
using Prospeo.DTOs.Misc;
using Prosepo.Webhooks.Attributes;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Controller providing miscellaneous utility endpoints for the webhook service.
    /// </summary>
    /// <remarks>
    /// This controller handles non-specific operations such as health checks and service information queries.
    /// It does not require specific business domain knowledge and serves general system management purposes.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class MiscController : ControllerBase
    {
        private readonly ILogger<MiscController> _logger;
        private readonly IFirmyService? _firmyService;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration service for accessing settings.</param>
        /// <param name="logger">The logger instance for logging controller operations.</param>
        /// <param name="firmyService">The service for managing company (Firmy) data. May be null if not available.</param>
        public MiscController(IConfiguration configuration, ILogger<MiscController> logger, IFirmyService? firmyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _firmyService = firmyService;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Performs a health check and returns basic system information.
        /// </summary>
        /// <returns>
        /// An <see cref="ActionResult{T}"/> containing a <see cref="PingDto"/> with system information
        /// including the registered company name.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves the default company (Firma) based on the configured DefaultFirmaId
        /// and returns information about the system registration. It's useful for verifying service
        /// availability and configuration.
        /// This endpoint requires authentication via API Key in the X-API-Key header.
        /// </remarks>
        /// <response code="200">Returns the ping response with system information.</response>
        /// <response code="401">Unauthorized - authentication required.</response>
        /// <response code="404">Company not found in the database.</response>
        /// <response code="500">Internal server error occurred during processing.</response>
        /// <response code="503">Service unavailable - database connection not configured.</response>
        [HttpGet("ping")]
        //[ApiKeyAuth]
        [ProducesResponseType(typeof(PingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<PingDto>> Ping()
        {
            try
            {
                // Sprawdź czy serwis firm jest dostępny
                if (_firmyService == null)
                {
                    _logger.LogError("FirmyService is not available. Database connection may not be configured.");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        error = "Service unavailable",
                        message = "Database connection is not configured"
                    });
                }

                var defaultFirmaId = _configuration.GetValue<int>("Queue:DefaultFirmaId", 1);
                _logger.LogDebug("Retrieving company information for FirmaId: {FirmaId}", defaultFirmaId);

                var firma = await _firmyService.GetByIdAsync(defaultFirmaId);

                if (firma == null)
                {
                    _logger.LogWarning("Company with ID {FirmaId} not found in database", defaultFirmaId);
                    return NotFound(new
                    {
                        error = "Company not found",
                        message = $"Company with ID {defaultFirmaId} does not exist in the database"
                    });
                }

                _logger.LogInformation("Ping successful for company: {CompanyName}", firma.NazwaFirmy);

                return Ok(new PingDto
                {
                    RegisteredFor = firma.NazwaFirmy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing ping request");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "Internal server error",
                    message = "An error occurred while processing the request"
                });
            }
        }
    }
}
