using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Security;
using Prosepo.Webhooks.Services.Application;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;

namespace Prosepo.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersUseCaseService _ordersUseCaseService;

        public OrdersController(IOrdersUseCaseService ordersUseCaseService)
        {
            _ordersUseCaseService = ordersUseCaseService;
        }

        [HttpPost("update-status")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();
            return await _ordersUseCaseService.UpdateStatusAsync(request, firmaId, firmaNazwa);
        }

        [HttpPost("upload-order-realization-result")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(UploadOrderRealizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadOrderRealizationResult([FromBody] UploadOrderRealizationRequest request)
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();
            return await _ordersUseCaseService.UploadOrderRealizationResultAsync(request, firmaId, firmaNazwa);
        }

        [HttpPost("upload-document-to-order")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadDocumentToOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocumentToOrder()
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();
            return await _ordersUseCaseService.UploadDocumentToOrderAsync(Request, firmaId, firmaNazwa);
        }

        [HttpGet("authenticated-firma")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAuthenticatedFirma()
        {
            return Ok(new
            {
                firmaId = HttpContext.Items["FirmaId"],
                firmaNazwa = HttpContext.Items["FirmaNazwa"],
                message = "Pomyślnie zautoryzowano"
            });
        }
    }
}
