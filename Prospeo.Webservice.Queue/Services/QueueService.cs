using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prospeo.Webservice.Queue.Data;
using Prospeo.Webservice.Queue.Models;
using System.Text.Json;

namespace Prospeo.Webservice.Queue.Services;

/// <summary>
/// Implementacja serwisu obs³ugi kolejki zadañ
/// </summary>
public class QueueService : IQueueService
{
    private readonly QueueDbContext _context;
    private readonly ILogger<QueueService> _logger;

    public QueueService(QueueDbContext context, ILogger<QueueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<long> EnqueueAsync<T>(string type, T payload, string? correlationId = null, string? source = null, int priority = 0, int maxAttempts = 3)
    {
        var queueItem = new QueueItem
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            Source = source,
            Priority = priority,
            MaxAttempts = maxAttempts,
            Status = QueueItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.QueueItems.Add(queueItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Dodano zadanie do kolejki {Id} typu {Type} z korelacj¹ {CorrelationId}", 
            queueItem.Id, type, correlationId);

        return queueItem.Id;
    }

    public async Task<QueueItem?> DequeueAsync()
    {
        // Pobiera zadanie o najwy¿szym priorytecie, które jest gotowe do przetworzenia
        var item = await _context.QueueItems
            .Where(x => (x.Status == QueueItemStatus.Pending || 
                        (x.Status == QueueItemStatus.Failed && x.NextRetryAt <= DateTime.UtcNow)) &&
                       x.Attempts < x.MaxAttempts)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (item != null)
        {
            _logger.LogDebug("Pobrano zadanie z kolejki {Id} typu {Type}", item.Id, item.Type);
        }

        return item;
    }

    public async Task MarkAsProcessingAsync(long id)
    {
        var item = await _context.QueueItems.FindAsync(id);
        if (item != null)
        {
            item.Status = QueueItemStatus.Processing;
            item.Attempts++;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Oznaczono zadanie {Id} jako przetwarzane (próba {Attempts})", id, item.Attempts);
        }
    }

    public async Task MarkAsCompletedAsync(long id)
    {
        var item = await _context.QueueItems.FindAsync(id);
        if (item != null)
        {
            item.Status = QueueItemStatus.Completed;
            item.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Oznaczono zadanie {Id} jako zakoñczone pomyœlnie", id);
        }
    }

    public async Task MarkAsFailedAsync(long id, string errorMessage, string? stackTrace = null)
    {
        var item = await _context.QueueItems.FindAsync(id);
        if (item != null)
        {
            item.Status = QueueItemStatus.Failed;
            item.ErrorMessage = errorMessage;
            item.ErrorStackTrace = stackTrace;
            item.ProcessedAt = DateTime.UtcNow;

            // Jeœli nie przekroczono maksymalnej liczby prób, zaplanuj ponown¹ próbê
            if (item.Attempts < item.MaxAttempts)
            {
                var retryDelay = CalculateRetryDelay(item.Attempts);
                item.NextRetryAt = DateTime.UtcNow.Add(retryDelay);
                
                _logger.LogWarning("Oznaczono zadanie {Id} jako nieudane, ponowna próba o {NextRetryAt} (próba {Attempts}/{MaxAttempts})", 
                    id, item.NextRetryAt, item.Attempts, item.MaxAttempts);
            }
            else
            {
                _logger.LogError("Oznaczono zadanie {Id} jako definitywnie nieudane po {Attempts} próbach: {ErrorMessage}", 
                    id, item.Attempts, errorMessage);
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task ScheduleRetryAsync(long id, DateTime nextRetryAt)
    {
        var item = await _context.QueueItems.FindAsync(id);
        if (item != null)
        {
            item.NextRetryAt = nextRetryAt;
            item.Status = QueueItemStatus.Failed; // Ustawia jako Failed aby mog³o byæ ponownie przetworzone
            await _context.SaveChangesAsync();

            _logger.LogInformation("Zaplanowano ponown¹ próbê dla zadania {Id} na {NextRetryAt}", id, nextRetryAt);
        }
    }

    public async Task CancelAsync(long id)
    {
        var item = await _context.QueueItems.FindAsync(id);
        if (item != null)
        {
            item.Status = QueueItemStatus.Cancelled;
            item.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Anulowano zadanie {Id}", id);
        }
    }

    public async Task<IEnumerable<QueueItem>> GetPendingItemsAsync(int count = 10)
    {
        return await _context.QueueItems
            .Where(x => x.Status == QueueItemStatus.Pending)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<QueueItem?> GetByIdAsync(long id)
    {
        return await _context.QueueItems.FindAsync(id);
    }

    public async Task<IEnumerable<QueueItem>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.QueueItems
            .Where(x => x.CorrelationId == correlationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QueueItem>> GetByStatusAsync(QueueItemStatus status, int skip = 0, int take = 100)
    {
        return await _context.QueueItems
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> DeleteCompletedItemsAsync(TimeSpan olderThan)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var itemsToDelete = await _context.QueueItems
            .Where(x => x.Status == QueueItemStatus.Completed && x.ProcessedAt < cutoffDate)
            .ToListAsync();

        if (itemsToDelete.Any())
        {
            _context.QueueItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuniêto {Count} zakoñczonych zadañ starszych ni¿ {CutoffDate}", 
                itemsToDelete.Count, cutoffDate);
        }

        return itemsToDelete.Count;
    }

    public async Task<int> GetQueueCountAsync(QueueItemStatus? status = null)
    {
        var query = _context.QueueItems.AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Oblicza opóŸnienie dla ponownej próby z wyk³adniczym backoff-em
    /// </summary>
    /// <param name="attemptNumber">Numer próby</param>
    /// <returns>Czas opóŸnienia</returns>
    private static TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        // Wyk³adniczy backoff: 1min, 2min, 4min, 8min, itd.
        var delayMinutes = Math.Pow(2, Math.Min(attemptNumber - 1, 6)); // Maksymalnie 64 minuty
        return TimeSpan.FromMinutes(delayMinutes);
    }
}