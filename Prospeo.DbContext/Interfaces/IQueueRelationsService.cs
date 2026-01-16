using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Interfaces;

/// <summary>
/// Interfejs serwisu obs³ugi relacji miêdzy elementami kolejki
/// </summary>
public interface IQueueRelationsService
{
    /// <summary>
    /// Tworzy now¹ relacjê miêdzy elementami kolejki
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Utworzona relacja</returns>
    /// <exception cref="InvalidOperationException">Gdy relacja ju¿ istnieje</exception>
    Task<QueueRelations> CreateRelationAsync(int sourceItemId, int targetItemId);

    /// <summary>
    /// Tworzy now¹ relacjê miêdzy elementami kolejki (bez wyj¹tku jeœli istnieje)
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Utworzona lub istniej¹ca relacja</returns>
    Task<QueueRelations> CreateOrGetRelationAsync(int sourceItemId, int targetItemId);

    /// <summary>
    /// Pobiera relacjê po ID
    /// </summary>
    /// <param name="id">ID relacji</param>
    /// <returns>Relacja lub null jeœli nie znaleziono</returns>
    Task<QueueRelations?> GetByIdAsync(int id);

    /// <summary>
    /// Pobiera relacjê miêdzy dwoma elementami kolejki
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Relacja lub null jeœli nie znaleziono</returns>
    Task<QueueRelations?> GetRelationAsync(int sourceItemId, int targetItemId);

    /// <summary>
    /// Pobiera wszystkie relacje wychodz¹ce z elementu (source relations)
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <returns>Lista relacji wychodz¹cych</returns>
    Task<IEnumerable<QueueRelations>> GetSourceRelationsAsync(int sourceItemId);

    /// <summary>
    /// Pobiera wszystkie relacje przychodz¹ce do elementu (target relations)
    /// </summary>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Lista relacji przychodz¹cych</returns>
    Task<IEnumerable<QueueRelations>> GetTargetRelationsAsync(int targetItemId);

    /// <summary>
    /// Pobiera wszystkie relacje dla elementu (zarówno source jak i target)
    /// </summary>
    /// <param name="itemId">ID elementu kolejki</param>
    /// <returns>Lista wszystkich relacji elementu</returns>
    Task<IEnumerable<QueueRelations>> GetAllRelationsForItemAsync(int itemId);

    /// <summary>
    /// Pobiera elementy docelowe dla elementu Ÿród³owego (dzieci)
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <returns>Lista elementów docelowych</returns>
    Task<IEnumerable<Queue>> GetTargetItemsAsync(int sourceItemId);

    /// <summary>
    /// Pobiera elementy Ÿród³owe dla elementu docelowego (rodzice)
    /// </summary>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Lista elementów Ÿród³owych</returns>
    Task<IEnumerable<Queue>> GetSourceItemsAsync(int targetItemId);

    /// <summary>
    /// Usuwa relacjê
    /// </summary>
    /// <param name="id">ID relacji</param>
    /// <returns>True jeœli usuniêto, False jeœli nie znaleziono</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Usuwa relacjê miêdzy elementami
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>True jeœli usuniêto, False jeœli nie znaleziono</returns>
    Task<bool> DeleteRelationAsync(int sourceItemId, int targetItemId);

    /// <summary>
    /// Usuwa wszystkie relacje wychodz¹ce z elementu
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <returns>Liczba usuniêtych relacji</returns>
    Task<int> DeleteSourceRelationsAsync(int sourceItemId);

    /// <summary>
    /// Usuwa wszystkie relacje przychodz¹ce do elementu
    /// </summary>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Liczba usuniêtych relacji</returns>
    Task<int> DeleteTargetRelationsAsync(int targetItemId);

    /// <summary>
    /// Usuwa wszystkie relacje dla elementu
    /// </summary>
    /// <param name="itemId">ID elementu kolejki</param>
    /// <returns>Liczba usuniêtych relacji</returns>
    Task<int> DeleteAllRelationsForItemAsync(int itemId);

    /// <summary>
    /// Sprawdza czy relacja istnieje
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>True jeœli istnieje</returns>
    Task<bool> RelationExistsAsync(int sourceItemId, int targetItemId);

    /// <summary>
    /// Pobiera liczbê relacji wychodz¹cych z elementu
    /// </summary>
    /// <param name="sourceItemId">ID elementu Ÿród³owego</param>
    /// <returns>Liczba relacji</returns>
    Task<int> GetSourceRelationsCountAsync(int sourceItemId);

    /// <summary>
    /// Pobiera liczbê relacji przychodz¹cych do elementu
    /// </summary>
    /// <param name="targetItemId">ID elementu docelowego</param>
    /// <returns>Liczba relacji</returns>
    Task<int> GetTargetRelationsCountAsync(int targetItemId);

    /// <summary>
    /// Pobiera wszystkie relacje z mo¿liwoœci¹ eager loading
    /// </summary>
    /// <param name="includeSourceItems">Czy do³¹czyæ dane source items</param>
    /// <param name="includeTargetItems">Czy do³¹czyæ dane target items</param>
    /// <returns>Lista relacji</returns>
    Task<IEnumerable<QueueRelations>> GetAllAsync(bool includeSourceItems = false, bool includeTargetItems = false);
}
