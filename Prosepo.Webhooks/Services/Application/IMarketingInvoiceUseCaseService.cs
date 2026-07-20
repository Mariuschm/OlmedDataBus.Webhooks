using Microsoft.AspNetCore.Mvc;

namespace Prosepo.Webhooks.Services.Application;

public interface IMarketingInvoiceUseCaseService
{
    Task<IActionResult> UploadDocumentToMarketingOrderAsync(HttpRequest request, string? firmaId, string? firmaNazwa);
}