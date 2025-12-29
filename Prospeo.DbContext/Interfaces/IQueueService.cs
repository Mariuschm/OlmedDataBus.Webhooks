using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Interfaces;

/// <summary>
/// Interfejs serwisu obs³ugi kolejki zadañ
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Pobiera wszystkie zadania z kolejki
    /// </summary>
    /// <returns>Lista zadañ z kolejki</returns>
    Task<IEnumerable<Queue>> GetAllAsync();

    /// <summary>
    /// Pobiera zadanie z kolejki po ID
    /// </summary>
    /// <param name="id">ID zadania</param>
    /// <returns>Zadanie z kolejki lub null jeœli nie znaleziono</returns>
    Task<Queue?> GetByIdAsync(int id);

    /// <summary>
    /// Pobiera zadanie z kolejki po RowID
    /// </summary>
    /// <param name="rowId">RowID zadania</param>
    /// <returns>Zadanie z kolejki lub null jeœli nie znaleziono</returns>
    Task<Queue?> GetByRowIdAsync(Guid rowId);

    /// <summary>
    /// Pobiera zadania z kolejki dla okreœlonej firmy
    /// </summary>
    /// <param name="firmaId">ID firmy</param>
    /// <returns>Lista zadañ dla firmy</returns>
    Task<IEnumerable<Queue>> GetByFirmaIdAsync(int firmaId);

    /// <summary>
    /// Pobiera zadania z kolejki dla okreœlonego zakresu
    /// </summary>
    /// <param name="scope">Zakres operacji</param>
    /// <returns>Lista zadañ dla zakresu</returns>
    Task<IEnumerable<Queue>> GetByScopeAsync(int scope);

    /// <summary>
    /// Pobiera zadania z kolejki z okreœlonymi flagami
    /// </summary>
    /// <param name="flg">Flagi statusu</param>
    /// <returns>Lista zadañ z okreœlonymi flagami</returns>
    Task<IEnumerable<Queue>> GetByFlagAsync(int flg);

    /// <summary>
    /// Pobiera zadania z kolejki dla okreœlonego TargetID
    /// </summary>
    /// <param name="targetId">ID docelowy</param>
    /// <returns>Lista zadañ dla TargetID</returns>
    Task<IEnumerable<Queue>> GetByTargetIdAsync(int targetId);

    /// <summary>
    /// Pobiera zadania z kolejki z okreœlonego zakresu dat
    /// </summary>
    /// <param name="dateFrom">Data od (Unix timestamp)</param>
    /// <param name="dateTo">Data do (Unix timestamp)</param>
    /// <returns>Lista zadañ z zakresu dat</returns>
    Task<IEnumerable<Queue>> GetByDateRangeAsync(int dateFrom, int dateTo);

    /// <summary>
    /// Pobiera zadania z kolejki z okreœlonego zakresu dat (DateTime)
    /// </summary>
    /// <param name="dateFrom">Data od</param>
    /// <param name="dateTo">Data do</param>
    /// <returns>Lista zadañ z zakresu dat</returns>
    Task<IEnumerable<Queue>> GetByDateRangeAsync(DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// Dodaje nowe zadanie do kolejki
    /// </summary>
    /// <param name="queue">Dane zadania kolejki</param>
    /// <returns>Dodane zadanie z wygenerowanym ID</returns>
    Task<Queue> AddAsync(Queue queue);

    /// <summary>
    /// Aktualizuje istniej¹ce zadanie w kolejce
    /// </summary>
    /// <param name="queue">Zaktualizowane dane zadania kolejki</param>
    /// <returns>True jeœli zaktualizowano, False jeœli nie znaleziono</returns>
    Task<bool> UpdateAsync(Queue queue);

    /// <summary>
    /// Usuwa zadanie z kolejki
    /// </summary>
    /// <param name="id">ID zadania do usuniêcia</param>
    /// <returns>True jeœli usuniêto, False jeœli nie znaleziono</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Sprawdza czy zadanie w kolejce istnieje
    /// </summary>
    /// <param name="id">ID zadania</param>
    /// <returns>True jeœli istnieje</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Pobiera liczbê zadañ w kolejce
    /// </summary>
    /// <returns>Liczba zadañ w kolejce</returns>
    Task<int> GetCountAsync();

    /// <summary>
    /// Pobiera liczbê zadañ w kolejce dla okreœlonej firmy
    /// </summary>
    /// <param name="firmaId">ID firmy</param>
    /// <returns>Liczba zadañ dla firmy</returns>
    Task<int> GetCountByFirmaAsync(int firmaId);

    /// <summary>
    /// Pobiera zadania z kolejki z uwzglêdnieniem stronicowania
    /// </summary>
    /// <param name="skip">Liczba zadañ do pominiêcia</param>
    /// <param name="take">Liczba zadañ do pobrania</param>
    /// <returns>Lista zadañ</returns>
    Task<IEnumerable<Queue>> GetPagedAsync(int skip, int take);

    /// <summary>
    /// Pobiera zadania z kolejki dla firmy z uwzglêdnieniem stronicowania
    /// </summary>
    /// <param name="firmaId">ID firmy</param>
    /// <param name="skip">Liczba zadañ do pominiêcia</param>
    /// <param name="take">Liczba zadañ do pobrania</param>
    /// <returns>Lista zadañ</returns>
    Task<IEnumerable<Queue>> GetPagedByFirmaAsync(int firmaId, int skip, int take);

    /// <summary>
    /// Usuwa stare zadania z kolejki
    /// </summary>
    /// <param name="olderThanDays">Liczba dni - zadania starsze ni¿ ta wartoœæ zostan¹ usuniête</param>
    /// <returns>Liczba usuniêtych zadañ</returns>
    Task<int> DeleteOldItemsAsync(int olderThanDays);
}