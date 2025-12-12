using Prospeo.Webservice.Queue.Models;

namespace Prospeo.Webservice.Queue.Services;

/// <summary>
/// Interfejs serwisu obs³ugi kolejki zadañ
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Dodaje nowe zadanie do kolejki
    /// </summary>
    /// <typeparam name="T">Typ danych zadania</typeparam>
    /// <param name="type">Typ zadania</param>
    /// <param name="payload">Dane zadania</param>
    /// <param name="correlationId">Opcjonalny identyfikator korelacji</param>
    /// <param name="source">Opcjonalne Ÿród³o zadania</param>
    /// <param name="priority">Priorytet (wy¿szy = wa¿niejszy)</param>
    /// <param name="maxAttempts">Maksymalna liczba prób</param>
    /// <returns>ID utworzonego zadania</returns>
    Task<long> EnqueueAsync<T>(string type, T payload, string? correlationId = null, string? source = null, int priority = 0, int maxAttempts = 3);

    /// <summary>
    /// Pobiera nastêpne zadanie do przetworzenia z kolejki
    /// </summary>
    /// <returns>Zadanie do przetworzenia lub null jeœli kolejka jest pusta</returns>
    Task<QueueItem?> DequeueAsync();

    /// <summary>
    /// Oznacza zadanie jako aktualnie przetwarzane
    /// </summary>
    /// <param name="id">ID zadania</param>
    Task MarkAsProcessingAsync(long id);

    /// <summary>
    /// Oznacza zadanie jako zakoñczone pomyœlnie
    /// </summary>
    /// <param name="id">ID zadania</param>
    Task MarkAsCompletedAsync(long id);

    /// <summary>
    /// Oznacza zadanie jako zakoñczone niepowodzeniem
    /// </summary>
    /// <param name="id">ID zadania</param>
    /// <param name="errorMessage">Komunikat b³êdu</param>
    /// <param name="stackTrace">Stack trace b³êdu</param>
    Task MarkAsFailedAsync(long id, string errorMessage, string? stackTrace = null);

    /// <summary>
    /// Planuje ponown¹ próbê przetworzenia zadania
    /// </summary>
    /// <param name="id">ID zadania</param>
    /// <param name="nextRetryAt">Data kolejnej próby</param>
    Task ScheduleRetryAsync(long id, DateTime nextRetryAt);

    /// <summary>
    /// Anuluje zadanie
    /// </summary>
    /// <param name="id">ID zadania</param>
    Task CancelAsync(long id);

    /// <summary>
    /// Pobiera zadania oczekuj¹ce na przetworzenie
    /// </summary>
    /// <param name="count">Maksymalna liczba zadañ</param>
    /// <returns>Lista zadañ oczekuj¹cych</returns>
    Task<IEnumerable<QueueItem>> GetPendingItemsAsync(int count = 10);

    /// <summary>
    /// Pobiera zadanie po ID
    /// </summary>
    /// <param name="id">ID zadania</param>
    /// <returns>Zadanie lub null jeœli nie znaleziono</returns>
    Task<QueueItem?> GetByIdAsync(long id);

    /// <summary>
    /// Pobiera zadania po identyfikatorze korelacji
    /// </summary>
    /// <param name="correlationId">Identyfikator korelacji</param>
    /// <returns>Lista zadañ</returns>
    Task<IEnumerable<QueueItem>> GetByCorrelationIdAsync(string correlationId);

    /// <summary>
    /// Pobiera zadania o okreœlonym statusie
    /// </summary>
    /// <param name="status">Status zadañ</param>
    /// <param name="skip">Liczba zadañ do pominiêcia</param>
    /// <param name="take">Liczba zadañ do pobrania</param>
    /// <returns>Lista zadañ</returns>
    Task<IEnumerable<QueueItem>> GetByStatusAsync(QueueItemStatus status, int skip = 0, int take = 100);

    /// <summary>
    /// Usuwa zakoñczone zadania starsze ni¿ okreœlony czas
    /// </summary>
    /// <param name="olderThan">Czas po jakim zadania mog¹ zostaæ usuniête</param>
    /// <returns>Liczba usuniêtych zadañ</returns>
    Task<int> DeleteCompletedItemsAsync(TimeSpan olderThan);

    /// <summary>
    /// Pobiera liczbê zadañ w kolejce
    /// </summary>
    /// <param name="status">Opcjonalny filtr statusu</param>
    /// <returns>Liczba zadañ</returns>
    Task<int> GetQueueCountAsync(QueueItemStatus? status = null);
}