using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Services;

/// <summary>
/// Implementacja serwisu obs³ugi kolejki zadañ
/// </summary>
public class QueueService : IQueueService
{
    private readonly ProspeoDataContext _context;
    private readonly ILogger<QueueService> _logger;

    public QueueService(ProspeoDataContext context, ILogger<QueueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Queue>> GetAllAsync()
    {
        _logger.LogDebug("Pobieranie wszystkich zadañ z kolejki");
        return await _context.Queues
            .Include(q => q.Firma)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<Queue?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Pobieranie zadania z kolejki po ID: {Id}", id);
        return await _context.Queues
            .Include(q => q.Firma)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Queue?> GetByRowIdAsync(Guid rowId)
    {
        _logger.LogDebug("Pobieranie zadania z kolejki po RowID: {RowId}", rowId);
        return await _context.Queues
            .Include(q => q.Firma)
            .FirstOrDefaultAsync(q => q.RowID == rowId);
    }

    public async Task<IEnumerable<Queue>> GetByFirmaIdAsync(int firmaId)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki dla firmy: {FirmaId}", firmaId);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.FirmaId == firmaId)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetByScopeAsync(int scope)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki dla zakresu: {Scope}", scope);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.Scope == scope)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetByFlagAsync(int flg)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki z flag¹: {Flg}", flg);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.Flg == flg)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetByTargetIdAsync(int targetId)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki dla TargetID: {TargetId}", targetId);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.TargetID == targetId)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetByDateRangeAsync(int dateFrom, int dateTo)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki z zakresu dat: {DateFrom} - {DateTo}", dateFrom, dateTo);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.DateAdd >= dateFrom && q.DateAdd <= dateTo)
            .OrderByDescending(q => q.DateAdd)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetByDateRangeAsync(DateTime dateFrom, DateTime dateTo)
    {
        var unixFrom = (int)new DateTimeOffset(dateFrom).ToUnixTimeSeconds();
        var unixTo = (int)new DateTimeOffset(dateTo).ToUnixTimeSeconds();
        return await GetByDateRangeAsync(unixFrom, unixTo);
    }

    public async Task<Queue> AddAsync(Queue queue)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        _logger.LogInformation("Dodawanie nowego zadania do kolejki dla firmy: {FirmaId}, Scope: {Scope}", 
            queue.FirmaId, queue.Scope);

        // Ustaw daty jeœli nie zosta³y podane
        var currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (queue.DateAdd == 0)
            queue.DateAdd = currentTimestamp;
        if (queue.DateMod == 0)
            queue.DateMod = currentTimestamp;

        _context.Queues.Add(queue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Dodano zadanie do kolejki z ID: {Id}", queue.Id);
        return queue;
    }

    public async Task<bool> UpdateAsync(Queue queue)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        _logger.LogInformation("Aktualizacja zadania w kolejce ID: {Id}", queue.Id);

        var existingQueue = await _context.Queues.FindAsync(queue.Id);
        if (existingQueue == null)
        {
            _logger.LogWarning("Nie znaleziono zadania w kolejce o ID: {Id}", queue.Id);
            return false;
        }

        // Aktualizuj w³aœciwoœci
        existingQueue.FirmaId = queue.FirmaId;
        existingQueue.Scope = queue.Scope;
        existingQueue.Request = queue.Request;
        existingQueue.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        existingQueue.Flg = queue.Flg;
        existingQueue.Description = queue.Description;
        existingQueue.TargetID = queue.TargetID;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Zaktualizowano zadanie w kolejce ID: {Id}", queue.Id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Usuwanie zadania z kolejki ID: {Id}", id);

        var queue = await _context.Queues.FindAsync(id);
        if (queue == null)
        {
            _logger.LogWarning("Nie znaleziono zadania w kolejce o ID: {Id}", id);
            return false;
        }

        _context.Queues.Remove(queue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuniêto zadanie z kolejki ID: {Id}", id);
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Queues.AnyAsync(q => q.Id == id);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Queues.CountAsync();
    }

    public async Task<int> GetCountByFirmaAsync(int firmaId)
    {
        return await _context.Queues.CountAsync(q => q.FirmaId == firmaId);
    }

    public async Task<IEnumerable<Queue>> GetPagedAsync(int skip, int take)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki - stronicowanie: skip={Skip}, take={Take}", skip, take);
        return await _context.Queues
            .Include(q => q.Firma)
            .OrderByDescending(q => q.DateAdd)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetPagedByFirmaAsync(int firmaId, int skip, int take)
    {
        _logger.LogDebug("Pobieranie zadañ z kolejki dla firmy {FirmaId} - stronicowanie: skip={Skip}, take={Take}", 
            firmaId, skip, take);
        return await _context.Queues
            .Include(q => q.Firma)
            .Where(q => q.FirmaId == firmaId)
            .OrderByDescending(q => q.DateAdd)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> DeleteOldItemsAsync(int olderThanDays)
    {
        _logger.LogInformation("Usuwanie starych zadañ z kolejki starszych ni¿ {Days} dni", olderThanDays);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var cutoffTimestamp = (int)cutoffDate.ToUnixTimeSeconds();

        var itemsToDelete = await _context.Queues
            .Where(q => q.DateAdd < cutoffTimestamp)
            .ToListAsync();

        if (itemsToDelete.Any())
        {
            _context.Queues.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuniêto {Count} starych zadañ z kolejki", itemsToDelete.Count);
        }

        return itemsToDelete.Count;
    }
}