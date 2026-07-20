using Microsoft.AspNetCore.Mvc;

namespace Prosepo.Webhooks.Services.Application;

public interface IQueueUseCaseService
{
    Task<IActionResult> GetProductQueueItemsAsync(int limit);
    Task<IActionResult> GetOrderQueueItemsAsync(int limit);
    Task<IActionResult> GetQueueItemByIdAsync(int id);
    Task<IActionResult> GetQueueStatisticsAsync();
    Task<IActionResult> RetryQueueItemAsync(int id);
}