using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Interfaces;

/// <summary>
/// Interfejs serwisu obs³ugi firm
/// </summary>
public interface IFirmyService
{
    /// <summary>
    /// Pobiera wszystkie firmy
    /// </summary>
    /// <returns>Lista firm</returns>
    Task<IEnumerable<Firmy>> GetAllAsync();

    /// <summary>
    /// Pobiera firmê po ID
    /// </summary>
    /// <param name="id">ID firmy</param>
    /// <returns>Firma lub null jeœli nie znaleziono</returns>
    Task<Firmy?> GetByIdAsync(int id);

    /// <summary>
    /// Pobiera firmê po RowID
    /// </summary>
    /// <param name="rowId">RowID firmy</param>
    /// <returns>Firma lub null jeœli nie znaleziono</returns>
    Task<Firmy?> GetByRowIdAsync(Guid rowId);

    /// <summary>
    /// Pobiera firmê po kluczu API
    /// </summary>
    /// <param name="apiKey">Klucz API</param>
    /// <returns>Firma lub null jeœli nie znaleziono</returns>
    Task<Firmy?> GetByApiKeyAsync(string apiKey);

    /// <summary>
    /// Pobiera firmy testowe
    /// </summary>
    /// <param name="czyTestowa">True dla testowych, False dla produkcyjnych</param>
    /// <returns>Lista firm</returns>
    Task<IEnumerable<Firmy>> GetByTestFlagAsync(bool czyTestowa);

    /// <summary>
    /// Pobiera firmê po nazwie bazy ERP
    /// </summary>
    /// <param name="nazwaBazyERP">Nazwa bazy ERP</param>
    /// <returns>Firma lub null jeœli nie znaleziono</returns>
    Task<Firmy?> GetByNazwaBazyERPAsync(string nazwaBazyERP);

    /// <summary>
    /// Dodaje now¹ firmê
    /// </summary>
    /// <param name="firma">Dane firmy</param>
    /// <returns>Dodana firma z wygenerowanym ID</returns>
    Task<Firmy> AddAsync(Firmy firma);

    /// <summary>
    /// Aktualizuje istniej¹c¹ firmê
    /// </summary>
    /// <param name="firma">Zaktualizowane dane firmy</param>
    /// <returns>True jeœli zaktualizowano, False jeœli nie znaleziono</returns>
    Task<bool> UpdateAsync(Firmy firma);

    /// <summary>
    /// Usuwa firmê
    /// </summary>
    /// <param name="id">ID firmy do usuniêcia</param>
    /// <returns>True jeœli usuniêto, False jeœli nie znaleziono</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Sprawdza czy firma istnieje
    /// </summary>
    /// <param name="id">ID firmy</param>
    /// <returns>True jeœli istnieje</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Sprawdza czy klucz API jest unikalny
    /// </summary>
    /// <param name="apiKey">Klucz API do sprawdzenia</param>
    /// <param name="excludeId">ID firmy do wykluczenia z sprawdzenia (dla aktualizacji)</param>
    /// <returns>True jeœli klucz jest unikalny</returns>
    Task<bool> IsApiKeyUniqueAsync(string apiKey, int? excludeId = null);
}