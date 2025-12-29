using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Interfaces;

/// <summary>
/// Interfejs serwisu obs³ugi statusów kolejki
/// </summary>
public interface IQueueStatusService
{
    /// <summary>
    /// Pobiera wszystkie statusy kolejki
    /// </summary>
    /// <returns>Lista statusów kolejki</returns>
    Task<IEnumerable<QueueStatus>> GetAllAsync();

    /// <summary>
    /// Pobiera status kolejki po ID
    /// </summary>
    /// <param name="id">ID statusu</param>
    /// <returns>Status kolejki lub null jeœli nie znaleziono</returns>
    Task<QueueStatus?> GetByIdAsync(int id);

    /// <summary>
    /// Pobiera status kolejki po RowID
    /// </summary>
    /// <param name="rowId">RowID statusu</param>
    /// <returns>Status kolejki lub null jeœli nie znaleziono</returns>
    Task<QueueStatus?> GetByRowIdAsync(Guid rowId);

    /// <summary>
    /// Pobiera status kolejki po nazwie
    /// </summary>
    /// <param name="name">Nazwa statusu</param>
    /// <returns>Status kolejki lub null jeœli nie znaleziono</returns>
    Task<QueueStatus?> GetByNameAsync(string name);

    /// <summary>
    /// Pobiera status kolejki po wartoœci
    /// </summary>
    /// <param name="value">Wartoœæ statusu</param>
    /// <returns>Status kolejki lub null jeœli nie znaleziono</returns>
    Task<QueueStatus?> GetByValueAsync(int value);

    /// <summary>
    /// Dodaje nowy status kolejki
    /// </summary>
    /// <param name="queueStatus">Dane statusu kolejki</param>
    /// <returns>Dodany status kolejki z wygenerowanym ID</returns>
    Task<QueueStatus> AddAsync(QueueStatus queueStatus);

    /// <summary>
    /// Aktualizuje istniej¹cy status kolejki
    /// </summary>
    /// <param name="queueStatus">Zaktualizowane dane statusu kolejki</param>
    /// <returns>True jeœli zaktualizowano, False jeœli nie znaleziono</returns>
    Task<bool> UpdateAsync(QueueStatus queueStatus);

    /// <summary>
    /// Usuwa status kolejki
    /// </summary>
    /// <param name="id">ID statusu do usuniêcia</param>
    /// <returns>True jeœli usuniêto, False jeœli nie znaleziono</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Sprawdza czy status kolejki istnieje
    /// </summary>
    /// <param name="id">ID statusu</param>
    /// <returns>True jeœli istnieje</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Sprawdza czy wartoœæ statusu jest unikalna
    /// </summary>
    /// <param name="value">Wartoœæ do sprawdzenia</param>
    /// <param name="excludeId">ID statusu do wykluczenia z sprawdzenia (dla aktualizacji)</param>
    /// <returns>True jeœli wartoœæ jest unikalna</returns>
    Task<bool> IsValueUniqueAsync(int value, int? excludeId = null);

    /// <summary>
    /// Sprawdza czy nazwa statusu jest unikalna
    /// </summary>
    /// <param name="name">Nazwa do sprawdzenia</param>
    /// <param name="excludeId">ID statusu do wykluczenia z sprawdzenia (dla aktualizacji)</param>
    /// <returns>True jeœli nazwa jest unikalna</returns>
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null);
}