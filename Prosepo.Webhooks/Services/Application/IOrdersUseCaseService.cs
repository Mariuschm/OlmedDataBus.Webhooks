using Microsoft.AspNetCore.Mvc;
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;

namespace Prosepo.Webhooks.Services.Application;

public interface IOrdersUseCaseService
{
    Task<IActionResult> UpdateStatusAsync(UpdateOrderStatusRequest request, string? firmaId, string? firmaNazwa);
    Task<IActionResult> UploadOrderRealizationResultAsync(UploadOrderRealizationRequest request, string? firmaId, string? firmaNazwa);
    Task<IActionResult> UploadDocumentToOrderAsync(HttpRequest request, string? firmaId, string? firmaNazwa);
}