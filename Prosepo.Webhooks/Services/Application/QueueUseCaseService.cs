using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Application.Common;
using Prospeo.DbContext.Interfaces;

namespace Prosepo.Webhooks.Services.Application;

public class QueueUseCaseService : IQueueUseCaseService
{
    private readonly IQueueService _queueService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueueUseCaseService> _logger;
    private readonly FileLoggingService _fileLoggingService;

    public QueueUseCaseService(
        IQueueService queueService,
        IConfiguration configuration,
        ILogger<QueueUseCaseService> logger,
        FileLoggingService fileLoggingService)
    {
        _queueService = queueService;
        _configuration = configuration;
        _logger = logger;
        _fileLoggingService = fileLoggingService;
    }

    public async Task<IActionResult> GetProductQueueItemsAsync(int limit)
    {
        try
        {
            if (limit is < 1 or > 1000)
            {
                return ApiResultFactory.ValidationError("Nieprawidłowy limit", "Limit musi być w zakresie 1-1000");
            }

            var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
            var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);
            var result = (await _queueService.GetByScopeAsync(productScope))
                .Where(q => q.Flg == webhookProcessingFlag)
                .OrderByDescending(q => q.DateAddDateTime)
                .Take(limit)
                .Select(item => new
                {
                    item.Id,
                    item.RowID,
                    item.FirmaId,
                    item.Scope,
                    item.Description,
                    item.TargetID,
                    item.Flg,
                    DateAdd = item.DateAddDateTime,
                    DateMod = item.DateModDateTime,
                    RequestPreview = item.Request?.Length > 200 ? item.Request[..200] + "..." : item.Request
                })
                .ToList();

            await _fileLoggingService.LogAsync("queue", LogLevel.Information, "Product queue items retrieved", null, new { ItemCount = result.Count, ProductScope = productScope, WebhookFlag = webhookProcessingFlag, Limit = limit });

            return new OkObjectResult(new ApiSuccessResponse<object>
            {
                Data = new { Success = true, TotalItems = result.Count, ProductScope = productScope, WebhookFlag = webhookProcessingFlag, RequestedLimit = limit, QueueItems = result }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching product queue items");
            await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Error fetching product queue items", ex);
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił błąd podczas pobierania zadań z kolejki");
        }
    }

    public async Task<IActionResult> GetOrderQueueItemsAsync(int limit)
    {
        try
        {
            if (limit is < 1 or > 1000)
            {
                return ApiResultFactory.ValidationError("Nieprawidłowy limit", "Limit musi być w zakresie 1-1000");
            }

            var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
            var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);
            var result = (await _queueService.GetByScopeAsync(orderScope))
                .Where(q => q.Flg == webhookProcessingFlag)
                .OrderByDescending(q => q.DateAddDateTime)
                .Take(limit)
                .Select(item => new
                {
                    item.Id,
                    item.RowID,
                    item.FirmaId,
                    item.Scope,
                    item.Description,
                    item.TargetID,
                    item.Flg,
                    DateAdd = item.DateAddDateTime,
                    DateMod = item.DateModDateTime,
                    RequestPreview = item.Request?.Length > 200 ? item.Request[..200] + "..." : item.Request
                })
                .ToList();

            await _fileLoggingService.LogAsync("queue", LogLevel.Information, "Order queue items retrieved", null, new { ItemCount = result.Count, OrderScope = orderScope, WebhookFlag = webhookProcessingFlag, Limit = limit });

            return new OkObjectResult(new ApiSuccessResponse<object>
            {
                Data = new { Success = true, TotalItems = result.Count, OrderScope = orderScope, WebhookFlag = webhookProcessingFlag, RequestedLimit = limit, QueueItems = result }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching order queue items");
            await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Error fetching order queue items", ex);
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił błąd podczas pobierania zadań z kolejki");
        }
    }

    public async Task<IActionResult> GetQueueItemByIdAsync(int id)
    {
        try
        {
            var queueItem = await _queueService.GetByIdAsync(id);
            if (queueItem is null)
            {
                return ApiResultFactory.Error(StatusCodes.Status404NotFound, "Nie znaleziono zadania", $"Zadanie z ID {id} nie zostało znalezione");
            }

            return new OkObjectResult(new ApiSuccessResponse<object>
            {
                Data = new
                {
                    Success = true,
                    QueueItem = new
                    {
                        queueItem.Id,
                        queueItem.RowID,
                        queueItem.FirmaId,
                        queueItem.Scope,
                        queueItem.Description,
                        queueItem.TargetID,
                        queueItem.Flg,
                        DateAdd = queueItem.DateAddDateTime,
                        DateMod = queueItem.DateModDateTime,
                        queueItem.Request
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching queue item with ID: {Id}", id);
            await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Error fetching queue item by ID", ex, new { QueueItemId = id });
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił błąd podczas pobierania zadania");
        }
    }

    public async Task<IActionResult> GetQueueStatisticsAsync()
    {
        try
        {
            var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
            var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
            var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);
            var allItems = (await _queueService.GetAllAsync()).ToList();
            var productItems = allItems.Where(q => q.Scope == productScope).ToList();
            var productWebhookItems = productItems.Where(q => q.Flg == webhookProcessingFlag).ToList();
            var orderItems = allItems.Where(q => q.Scope == orderScope).ToList();
            var orderWebhookItems = orderItems.Where(q => q.Flg == webhookProcessingFlag).ToList();

            var statistics = new
            {
                TotalItems = allItems.Count,
                ProductItems = new
                {
                    Total = productItems.Count,
                    FromWebhook = productWebhookItems.Count,
                    ByFlag = productItems.GroupBy(q => q.Flg).Select(g => new { Flag = g.Key, Count = g.Count() }).ToList()
                },
                OrderItems = new
                {
                    Total = orderItems.Count,
                    FromWebhook = orderWebhookItems.Count,
                    ByFlag = orderItems.GroupBy(q => q.Flg).Select(g => new { Flag = g.Key, Count = g.Count() }).ToList()
                },
                OtherScopes = allItems.Where(q => q.Scope != productScope && q.Scope != orderScope).GroupBy(q => q.Scope).Select(g => new { Scope = g.Key, Count = g.Count() }).ToList()
            };

            await _fileLoggingService.LogAsync("queue", LogLevel.Information, "Queue statistics retrieved", null, statistics);
            return new OkObjectResult(new ApiSuccessResponse<object> { Data = new { Success = true, Timestamp = DateTime.UtcNow, Statistics = statistics } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calculating queue statistics");
            await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Error calculating queue statistics", ex);
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił błąd podczas pobierania statystyk kolejki");
        }
    }

    public async Task<IActionResult> RetryQueueItemAsync(int id)
    {
        try
        {
            var queueItem = await _queueService.GetByIdAsync(id);
            if (queueItem is null)
            {
                await _fileLoggingService.LogAsync("queue", LogLevel.Warning, "Queue item not found for retry", null, new { QueueItemId = id });
                return ApiResultFactory.Error(StatusCodes.Status404NotFound, "Nie znaleziono zadania", $"Zadanie z ID {id} nie zostało znalezione");
            }

            if (queueItem.Flg is 0 or 1)
            {
                var statusName = queueItem.Flg == 0 ? "Pending (oczekuje)" : "Completed (zakończone)";
                await _fileLoggingService.LogAsync("queue", LogLevel.Warning, "Attempted to retry queue item with invalid status", null, new { QueueItemId = id, CurrentFlg = queueItem.Flg, StatusName = statusName, Scope = queueItem.Scope, FirmaId = queueItem.FirmaId });
                return ApiResultFactory.ValidationError($"Nie można ponowić zadania ze statusem {statusName}", queueItem.Flg == 0 ? "Zadanie już oczekuje na przetworzenie" : "Zadanie zostało już pomyślnie zakończone");
            }

            var oldFlg = queueItem.Flg;
            var oldDescription = queueItem.Description;
            queueItem.Flg = 0;
            queueItem.DateModDateTime = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(queueItem.Description))
            {
                queueItem.Description = $"[RETRY at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Previous: {queueItem.Description}";
                if (queueItem.Description.Length > 1024)
                {
                    queueItem.Description = queueItem.Description[..1024];
                }
            }

            var updateResult = await _queueService.UpdateAsync(queueItem);
            if (!updateResult)
            {
                await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Failed to update queue item for retry", null, new { QueueItemId = id });
                return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Nie udało się zaktualizować zadania w kolejce");
            }

            await _fileLoggingService.LogAsync("queue", LogLevel.Information, "Queue item reset for retry", null, new { QueueItemId = id, RowID = queueItem.RowID, Scope = queueItem.Scope, FirmaId = queueItem.FirmaId, OldFlg = oldFlg, NewFlg = queueItem.Flg, OldDescription = oldDescription, NewDescription = queueItem.Description, RetryTimestamp = DateTime.UtcNow });

            return new OkObjectResult(new ApiSuccessResponse<object>
            {
                Data = new
                {
                    Success = true,
                    Message = "Zadanie zostało pomyślnie zresetowane do ponownego przetworzenia",
                    QueueItem = new { queueItem.Id, queueItem.RowID, queueItem.FirmaId, queueItem.Scope, PreviousStatus = oldFlg, CurrentStatus = queueItem.Flg, DateMod = queueItem.DateModDateTime, queueItem.Description }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrying queue item with ID: {Id}", id);
            await _fileLoggingService.LogAsync("queue", LogLevel.Error, "Error retrying queue item", ex, new { QueueItemId = id });
            return ApiResultFactory.Error(StatusCodes.Status500InternalServerError, "Błąd serwera", "Wystąpił błąd podczas ponowienia zadania");
        }
    }
}
