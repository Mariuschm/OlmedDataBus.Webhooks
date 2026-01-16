using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Services;

/// <summary>
/// Implementacja serwisu obs³ugi relacji miêdzy elementami kolejki
/// </summary>
public class QueueRelationsService : IQueueRelationsService
{
    private readonly ProspeoDataContext _context;
    private readonly ILogger<QueueRelationsService> _logger;

    public QueueRelationsService(ProspeoDataContext context, ILogger<QueueRelationsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueueRelations> CreateRelationAsync(int sourceItemId, int targetItemId)
    {
        _logger.LogDebug("Tworzenie relacji: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);

        // SprawdŸ czy relacja ju¿ istnieje
        var existing = await GetRelationAsync(sourceItemId, targetItemId);
        if (existing != null)
        {
            _logger.LogWarning("Relacja ju¿ istnieje: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);
            throw new InvalidOperationException($"Relacja miêdzy elementami {sourceItemId} i {targetItemId} ju¿ istnieje");
        }

        // SprawdŸ czy elementy kolejki istniej¹
        var sourceExists = await _context.Queues.AnyAsync(q => q.Id == sourceItemId);
        var targetExists = await _context.Queues.AnyAsync(q => q.Id == targetItemId);

        if (!sourceExists)
            throw new ArgumentException($"Element Ÿród³owy o ID {sourceItemId} nie istnieje", nameof(sourceItemId));
        if (!targetExists)
            throw new ArgumentException($"Element docelowy o ID {targetItemId} nie istnieje", nameof(targetItemId));

        var relation = new QueueRelations
        {
            SourceItemId = sourceItemId,
            TargetItemId = targetItemId
        };

        _context.QueueRelations.Add(relation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Utworzono relacjê ID={Id}: Source={SourceId} -> Target={TargetId}", 
            relation.Id, sourceItemId, targetItemId);

        return relation;
    }

    public async Task<QueueRelations> CreateOrGetRelationAsync(int sourceItemId, int targetItemId)
    {
        _logger.LogDebug("Tworzenie lub pobieranie relacji: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);

        var existing = await GetRelationAsync(sourceItemId, targetItemId);
        if (existing != null)
        {
            _logger.LogDebug("Relacja ju¿ istnieje: ID={Id}", existing.Id);
            return existing;
        }

        return await CreateRelationAsync(sourceItemId, targetItemId);
    }

    public async Task<QueueRelations?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Pobieranie relacji po ID: {Id}", id);
        return await _context.QueueRelations
            .Include(r => r.SourceItem)
            .Include(r => r.TargetItem)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<QueueRelations?> GetRelationAsync(int sourceItemId, int targetItemId)
    {
        _logger.LogDebug("Pobieranie relacji: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);
        return await _context.QueueRelations
            .FirstOrDefaultAsync(r => r.SourceItemId == sourceItemId && r.TargetItemId == targetItemId);
    }

    public async Task<IEnumerable<QueueRelations>> GetSourceRelationsAsync(int sourceItemId)
    {
        _logger.LogDebug("Pobieranie relacji wychodz¹cych dla elementu: {SourceId}", sourceItemId);
        return await _context.QueueRelations
            .Include(r => r.TargetItem)
            .Where(r => r.SourceItemId == sourceItemId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QueueRelations>> GetTargetRelationsAsync(int targetItemId)
    {
        _logger.LogDebug("Pobieranie relacji przychodz¹cych dla elementu: {TargetId}", targetItemId);
        return await _context.QueueRelations
            .Include(r => r.SourceItem)
            .Where(r => r.TargetItemId == targetItemId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QueueRelations>> GetAllRelationsForItemAsync(int itemId)
    {
        _logger.LogDebug("Pobieranie wszystkich relacji dla elementu: {ItemId}", itemId);
        return await _context.QueueRelations
            .Include(r => r.SourceItem)
            .Include(r => r.TargetItem)
            .Where(r => r.SourceItemId == itemId || r.TargetItemId == itemId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetTargetItemsAsync(int sourceItemId)
    {
        _logger.LogDebug("Pobieranie elementów docelowych dla Ÿród³a: {SourceId}", sourceItemId);
        return await _context.QueueRelations
            .Where(r => r.SourceItemId == sourceItemId)
            .Select(r => r.TargetItem!)
            .ToListAsync();
    }

    public async Task<IEnumerable<Queue>> GetSourceItemsAsync(int targetItemId)
    {
        _logger.LogDebug("Pobieranie elementów Ÿród³owych dla celu: {TargetId}", targetItemId);
        return await _context.QueueRelations
            .Where(r => r.TargetItemId == targetItemId)
            .Select(r => r.SourceItem!)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Usuwanie relacji ID: {Id}", id);

        var relation = await _context.QueueRelations.FindAsync(id);
        if (relation == null)
        {
            _logger.LogWarning("Nie znaleziono relacji o ID: {Id}", id);
            return false;
        }

        _context.QueueRelations.Remove(relation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuniêto relacjê ID: {Id}", id);
        return true;
    }

    public async Task<bool> DeleteRelationAsync(int sourceItemId, int targetItemId)
    {
        _logger.LogInformation("Usuwanie relacji: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);

        var relation = await GetRelationAsync(sourceItemId, targetItemId);
        if (relation == null)
        {
            _logger.LogWarning("Nie znaleziono relacji: Source={SourceId} -> Target={TargetId}", sourceItemId, targetItemId);
            return false;
        }

        _context.QueueRelations.Remove(relation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuniêto relacjê ID: {Id}", relation.Id);
        return true;
    }

    public async Task<int> DeleteSourceRelationsAsync(int sourceItemId)
    {
        _logger.LogInformation("Usuwanie wszystkich relacji wychodz¹cych dla elementu: {SourceId}", sourceItemId);

        var relations = await _context.QueueRelations
            .Where(r => r.SourceItemId == sourceItemId)
            .ToListAsync();

        if (relations.Any())
        {
            _context.QueueRelations.RemoveRange(relations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuniêto {Count} relacji wychodz¹cych dla elementu: {SourceId}", 
                relations.Count, sourceItemId);
        }

        return relations.Count;
    }

    public async Task<int> DeleteTargetRelationsAsync(int targetItemId)
    {
        _logger.LogInformation("Usuwanie wszystkich relacji przychodz¹cych dla elementu: {TargetId}", targetItemId);

        var relations = await _context.QueueRelations
            .Where(r => r.TargetItemId == targetItemId)
            .ToListAsync();

        if (relations.Any())
        {
            _context.QueueRelations.RemoveRange(relations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuniêto {Count} relacji przychodz¹cych dla elementu: {TargetId}", 
                relations.Count, targetItemId);
        }

        return relations.Count;
    }

    public async Task<int> DeleteAllRelationsForItemAsync(int itemId)
    {
        _logger.LogInformation("Usuwanie wszystkich relacji dla elementu: {ItemId}", itemId);

        var relations = await _context.QueueRelations
            .Where(r => r.SourceItemId == itemId || r.TargetItemId == itemId)
            .ToListAsync();

        if (relations.Any())
        {
            _context.QueueRelations.RemoveRange(relations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuniêto {Count} relacji dla elementu: {ItemId}", 
                relations.Count, itemId);
        }

        return relations.Count;
    }

    public async Task<bool> RelationExistsAsync(int sourceItemId, int targetItemId)
    {
        return await _context.QueueRelations
            .AnyAsync(r => r.SourceItemId == sourceItemId && r.TargetItemId == targetItemId);
    }

    public async Task<int> GetSourceRelationsCountAsync(int sourceItemId)
    {
        return await _context.QueueRelations
            .CountAsync(r => r.SourceItemId == sourceItemId);
    }

    public async Task<int> GetTargetRelationsCountAsync(int targetItemId)
    {
        return await _context.QueueRelations
            .CountAsync(r => r.TargetItemId == targetItemId);
    }

    public async Task<IEnumerable<QueueRelations>> GetAllAsync(bool includeSourceItems = false, bool includeTargetItems = false)
    {
        _logger.LogDebug("Pobieranie wszystkich relacji (includeSource={IncludeSource}, includeTarget={IncludeTarget})", 
            includeSourceItems, includeTargetItems);

        var query = _context.QueueRelations.AsQueryable();

        if (includeSourceItems)
            query = query.Include(r => r.SourceItem);

        if (includeTargetItems)
            query = query.Include(r => r.TargetItem);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
