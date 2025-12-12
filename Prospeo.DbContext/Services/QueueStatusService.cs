using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Services;

/// <summary>
/// Implementacja serwisu obs³ugi statusów kolejki
/// </summary>
public class QueueStatusService : IQueueStatusService
{
    private readonly ProspeoDataContext _context;
    private readonly ILogger<QueueStatusService> _logger;

    public QueueStatusService(ProspeoDataContext context, ILogger<QueueStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<QueueStatus>> GetAllAsync()
    {
        _logger.LogDebug("Pobieranie wszystkich statusów kolejki");
        return await _context.QueueStatuses
            .OrderBy(q => q.Value)
            .ToListAsync();
    }

    public async Task<QueueStatus?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Pobieranie statusu kolejki po ID: {Id}", id);
        return await _context.QueueStatuses.FindAsync(id);
    }

    public async Task<QueueStatus?> GetByRowIdAsync(Guid rowId)
    {
        _logger.LogDebug("Pobieranie statusu kolejki po RowID: {RowId}", rowId);
        return await _context.QueueStatuses
            .FirstOrDefaultAsync(q => q.RowID == rowId);
    }

    public async Task<QueueStatus?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        _logger.LogDebug("Pobieranie statusu kolejki po nazwie: {Name}", name);
        return await _context.QueueStatuses
            .FirstOrDefaultAsync(q => q.Name == name);
    }

    public async Task<QueueStatus?> GetByValueAsync(int value)
    {
        _logger.LogDebug("Pobieranie statusu kolejki po wartoœci: {Value}", value);
        return await _context.QueueStatuses
            .FirstOrDefaultAsync(q => q.Value == value);
    }

    public async Task<QueueStatus> AddAsync(QueueStatus queueStatus)
    {
        if (queueStatus == null)
            throw new ArgumentNullException(nameof(queueStatus));

        _logger.LogInformation("Dodawanie nowego statusu kolejki: {Name} (wartoœæ: {Value})", 
            queueStatus.Name, queueStatus.Value);

        // SprawdŸ unikalnoœæ nazwy
        var isNameUnique = await IsNameUniqueAsync(queueStatus.Name);
        if (!isNameUnique)
        {
            throw new InvalidOperationException($"Status o nazwie '{queueStatus.Name}' ju¿ istnieje w systemie");
        }

        // SprawdŸ unikalnoœæ wartoœci
        var isValueUnique = await IsValueUniqueAsync(queueStatus.Value);
        if (!isValueUnique)
        {
            throw new InvalidOperationException($"Status o wartoœci '{queueStatus.Value}' ju¿ istnieje w systemie");
        }

        _context.QueueStatuses.Add(queueStatus);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Dodano status kolejki {Name} z ID: {Id}", 
            queueStatus.Name, queueStatus.Id);
        return queueStatus;
    }

    public async Task<bool> UpdateAsync(QueueStatus queueStatus)
    {
        if (queueStatus == null)
            throw new ArgumentNullException(nameof(queueStatus));

        _logger.LogInformation("Aktualizacja statusu kolejki ID: {Id}", queueStatus.Id);

        var existingStatus = await _context.QueueStatuses.FindAsync(queueStatus.Id);
        if (existingStatus == null)
        {
            _logger.LogWarning("Nie znaleziono statusu kolejki o ID: {Id}", queueStatus.Id);
            return false;
        }

        // SprawdŸ unikalnoœæ nazwy jeœli zosta³a zmieniona
        if (queueStatus.Name != existingStatus.Name)
        {
            var isNameUnique = await IsNameUniqueAsync(queueStatus.Name, queueStatus.Id);
            if (!isNameUnique)
            {
                throw new InvalidOperationException($"Status o nazwie '{queueStatus.Name}' ju¿ istnieje w systemie");
            }
        }

        // SprawdŸ unikalnoœæ wartoœci jeœli zosta³a zmieniona
        if (queueStatus.Value != existingStatus.Value)
        {
            var isValueUnique = await IsValueUniqueAsync(queueStatus.Value, queueStatus.Id);
            if (!isValueUnique)
            {
                throw new InvalidOperationException($"Status o wartoœci '{queueStatus.Value}' ju¿ istnieje w systemie");
            }
        }

        // Aktualizuj w³aœciwoœci
        existingStatus.Name = queueStatus.Name;
        existingStatus.Value = queueStatus.Value;
        existingStatus.Description = queueStatus.Description;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Zaktualizowano status kolejki {Name} (ID: {Id})", 
            queueStatus.Name, queueStatus.Id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Usuwanie statusu kolejki ID: {Id}", id);

        var queueStatus = await _context.QueueStatuses.FindAsync(id);
        if (queueStatus == null)
        {
            _logger.LogWarning("Nie znaleziono statusu kolejki o ID: {Id}", id);
            return false;
        }

        _context.QueueStatuses.Remove(queueStatus);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuniêto status kolejki {Name} (ID: {Id})", 
            queueStatus.Name, id);
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.QueueStatuses.AnyAsync(q => q.Id == id);
    }

    public async Task<bool> IsValueUniqueAsync(int value, int? excludeId = null)
    {
        var query = _context.QueueStatuses.Where(q => q.Value == value);
        
        if (excludeId.HasValue)
        {
            query = query.Where(q => q.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false; // Pusta nazwa nie jest dozwolona

        var query = _context.QueueStatuses.Where(q => q.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(q => q.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }
}