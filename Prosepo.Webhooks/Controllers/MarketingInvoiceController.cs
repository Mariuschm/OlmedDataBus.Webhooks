using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Security;
using Prosepo.Webhooks.Services.Application;
using Prospeo.DTOs.Order;

namespace Prosepo.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    [Produces("application/json")]
    public class MarketingInvoiceController : ControllerBase
    {
        private readonly IMarketingInvoiceUseCaseService _marketingInvoiceUseCaseService;

        public MarketingInvoiceController(IMarketingInvoiceUseCaseService marketingInvoiceUseCaseService)
        {
            _marketingInvoiceUseCaseService = marketingInvoiceUseCaseService;
        }

        [HttpPost("upload-document-to-order")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadDocumentToOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocumentToMarketingOrder()
        {
            var firmaId = HttpContext.Items["FirmaId"]?.ToString();
            var firmaNazwa = HttpContext.Items["FirmaNazwa"]?.ToString();
            return await _marketingInvoiceUseCaseService.UploadDocumentToMarketingOrderAsync(Request, firmaId, firmaNazwa);
        }
    }
}
