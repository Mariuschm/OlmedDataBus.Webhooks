using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Security;
using Prosepo.Webhooks.Services.Application;

namespace Prosepo.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public class QueueController : ControllerBase
    {
        private readonly IQueueUseCaseService _queueUseCaseService;

        public QueueController(IQueueUseCaseService queueUseCaseService)
        {
            _queueUseCaseService = queueUseCaseService;
        }

        [HttpGet("products")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetProductQueueItems([FromQuery] int limit = 100)
        {
            return _queueUseCaseService.GetProductQueueItemsAsync(limit);
        }

        [HttpGet("orders")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetOrderQueueItems([FromQuery] int limit = 100)
        {
            return _queueUseCaseService.GetOrderQueueItemsAsync(limit);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetQueueItemById(int id)
        {
            return _queueUseCaseService.GetQueueItemByIdAsync(id);
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetQueueStatistics()
        {
            return _queueUseCaseService.GetQueueStatisticsAsync();
        }

        [HttpPost("{id}/retry")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> RetryQueueItem(int id)
        {
            return _queueUseCaseService.RetryQueueItemAsync(id);
        }
    }
}
