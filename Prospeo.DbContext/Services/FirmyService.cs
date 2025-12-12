using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Services;

/// <summary>
/// Implementacja serwisu obs³ugi firm
/// </summary>
public class FirmyService : IFirmyService
{
    private readonly ProspeoDataContext _context;
    private readonly ILogger<FirmyService> _logger;

    public FirmyService(ProspeoDataContext context, ILogger<FirmyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Firmy>> GetAllAsync()
    {
        _logger.LogDebug("Pobieranie wszystkich firm");
        return await _context.Firmy
            .OrderBy(f => f.NazwaFirmy)
            .ToListAsync();
    }

    public async Task<Firmy?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Pobieranie firmy po ID: {Id}", id);
        return await _context.Firmy.FindAsync(id);
    }

    public async Task<Firmy?> GetByRowIdAsync(Guid rowId)
    {
        _logger.LogDebug("Pobieranie firmy po RowID: {RowId}", rowId);
        return await _context.Firmy
            .FirstOrDefaultAsync(f => f.RowID == rowId);
    }

    public async Task<Firmy?> GetByApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        _logger.LogDebug("Pobieranie firmy po kluczu API");
        return await _context.Firmy
            .FirstOrDefaultAsync(f => f.ApiKey == apiKey);
    }

    public async Task<IEnumerable<Firmy>> GetByTestFlagAsync(bool czyTestowa)
    {
        _logger.LogDebug("Pobieranie firm {Type}", czyTestowa ? "testowych" : "produkcyjnych");
        return await _context.Firmy
            .Where(f => f.CzyTestowa == czyTestowa)
            .OrderBy(f => f.NazwaFirmy)
            .ToListAsync();
    }

    public async Task<Firmy?> GetByNazwaBazyERPAsync(string nazwaBazyERP)
    {
        if (string.IsNullOrWhiteSpace(nazwaBazyERP))
            return null;

        _logger.LogDebug("Pobieranie firmy po nazwie bazy ERP: {NazwaBazyERP}", nazwaBazyERP);
        return await _context.Firmy
            .FirstOrDefaultAsync(f => f.NazwaBazyERP == nazwaBazyERP);
    }

    public async Task<Firmy> AddAsync(Firmy firma)
    {
        if (firma == null)
            throw new ArgumentNullException(nameof(firma));

        _logger.LogInformation("Dodawanie nowej firmy: {NazwaFirmy}", firma.NazwaFirmy);

        // SprawdŸ unikalnoœæ klucza API jeœli zosta³ podany
        if (!string.IsNullOrWhiteSpace(firma.ApiKey))
        {
            var isUnique = await IsApiKeyUniqueAsync(firma.ApiKey);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Klucz API '{firma.ApiKey}' ju¿ istnieje w systemie");
            }
        }

        // SprawdŸ unikalnoœæ nazwy bazy ERP
        var existingFirma = await GetByNazwaBazyERPAsync(firma.NazwaBazyERP);
        if (existingFirma != null)
        {
            throw new InvalidOperationException($"Firma z baz¹ ERP '{firma.NazwaBazyERP}' ju¿ istnieje");
        }

        _context.Firmy.Add(firma);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Dodano firmê {NazwaFirmy} z ID: {Id}", firma.NazwaFirmy, firma.Id);
        return firma;
    }

    public async Task<bool> UpdateAsync(Firmy firma)
    {
        if (firma == null)
            throw new ArgumentNullException(nameof(firma));

        _logger.LogInformation("Aktualizacja firmy ID: {Id}", firma.Id);

        var existingFirma = await _context.Firmy.FindAsync(firma.Id);
        if (existingFirma == null)
        {
            _logger.LogWarning("Nie znaleziono firmy o ID: {Id}", firma.Id);
            return false;
        }

        // SprawdŸ unikalnoœæ klucza API jeœli zosta³ zmieniony
        if (!string.IsNullOrWhiteSpace(firma.ApiKey) && firma.ApiKey != existingFirma.ApiKey)
        {
            var isUnique = await IsApiKeyUniqueAsync(firma.ApiKey, firma.Id);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Klucz API '{firma.ApiKey}' ju¿ istnieje w systemie");
            }
        }

        // SprawdŸ unikalnoœæ nazwy bazy ERP jeœli zosta³a zmieniona
        if (firma.NazwaBazyERP != existingFirma.NazwaBazyERP)
        {
            var firmaBaza = await GetByNazwaBazyERPAsync(firma.NazwaBazyERP);
            if (firmaBaza != null && firmaBaza.Id != firma.Id)
            {
                throw new InvalidOperationException($"Firma z baz¹ ERP '{firma.NazwaBazyERP}' ju¿ istnieje");
            }
        }

        // Aktualizuj w³aœciwoœci
        existingFirma.NazwaFirmy = firma.NazwaFirmy;
        existingFirma.NazwaBazyERP = firma.NazwaBazyERP;
        existingFirma.CzyTestowa = firma.CzyTestowa;
        existingFirma.ApiKey = firma.ApiKey;
        existingFirma.AuthorizeAllEndpoints = firma.AuthorizeAllEndpoints;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Zaktualizowano firmê {NazwaFirmy} (ID: {Id})", firma.NazwaFirmy, firma.Id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Usuwanie firmy ID: {Id}", id);

        var firma = await _context.Firmy.FindAsync(id);
        if (firma == null)
        {
            _logger.LogWarning("Nie znaleziono firmy o ID: {Id}", id);
            return false;
        }

        _context.Firmy.Remove(firma);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuniêto firmê {NazwaFirmy} (ID: {Id})", firma.NazwaFirmy, id);
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Firmy.AnyAsync(f => f.Id == id);
    }

    public async Task<bool> IsApiKeyUniqueAsync(string apiKey, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return true; // Null/puste klucze API s¹ dozwolone

        var query = _context.Firmy.Where(f => f.ApiKey == apiKey);
        
        if (excludeId.HasValue)
        {
            query = query.Where(f => f.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }
}