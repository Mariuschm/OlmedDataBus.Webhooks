using Prospeo.Webservice.Queue.Models;

namespace Prospeo.Webservice.Queue.Services;

/// <summary>
/// Interfejs do przetwarzania elementów kolejki.
/// Implementuj ten interfejs w swojej aplikacji, aby zdefiniowaæ logikê przetwarzania.
/// </summary>
public interface IQueueProcessor
{
    /// <summary>
    /// Przetwarza element kolejki
    /// </summary>
    /// <param name="item">Element kolejki do przetworzenia</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>True jeœli przetwarzanie siê powiod³o, False w przeciwnym razie</returns>
    Task<bool> ProcessAsync(QueueItem item, CancellationToken cancellationToken = default);
}