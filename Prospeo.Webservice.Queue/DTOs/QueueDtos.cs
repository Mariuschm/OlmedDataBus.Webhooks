using Prospeo.Webservice.Queue.Models;

namespace Prospeo.Webservice.Queue.DTOs;

/// <summary>
/// DTO dla elementu kolejki (bez payload dla bezpieczeñstwa)
/// </summary>
public class QueueItemDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public QueueItemStatus Status { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public string? Source { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// ¯¹danie dodania zadania do kolejki
/// </summary>
public class EnqueueRequest
{
    /// <summary>Typ zadania</summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>Dane zadania</summary>
    public object Payload { get; set; } = null!;
    
    /// <summary>Opcjonalny identyfikator korelacji</summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>Opcjonalne Ÿród³o zadania</summary>
    public string? Source { get; set; }
    
    /// <summary>Priorytet zadania</summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>Maksymalna liczba prób</summary>
    public int MaxAttempts { get; set; } = 3;
}

/// <summary>
/// OdpowiedŸ na ¿¹danie dodania zadania
/// </summary>
public class EnqueueResponse
{
    /// <summary>ID utworzonego zadania</summary>
    public long Id { get; set; }
    
    /// <summary>Czy operacja siê powiod³a</summary>
    public bool Success { get; set; }
    
    /// <summary>Komunikat b³êdu (jeœli wyst¹pi³)</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Statystyki kolejki
/// </summary>
public class QueueStatsDto
{
    /// <summary>Liczba zadañ oczekuj¹cych</summary>
    public int PendingCount { get; set; }
    
    /// <summary>Liczba zadañ przetwarzanych</summary>
    public int ProcessingCount { get; set; }
    
    /// <summary>Liczba zadañ zakoñczonych</summary>
    public int CompletedCount { get; set; }
    
    /// <summary>Liczba zadañ nieudanych</summary>
    public int FailedCount { get; set; }
    
    /// <summary>Liczba zadañ anulowanych</summary>
    public int CancelledCount { get; set; }
    
    /// <summary>Ca³kowita liczba zadañ</summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// ¯¹danie aktualizacji statusu zadania
/// </summary>
public class UpdateQueueItemStatusRequest
{
    /// <summary>Nowy status</summary>
    public QueueItemStatus Status { get; set; }
    
    /// <summary>Komunikat b³êdu (dla statusu Failed)</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Stack trace b³êdu (dla statusu Failed)</summary>
    public string? ErrorStackTrace { get; set; }
    
    /// <summary>Data kolejnej próby (dla Failed z retry)</summary>
    public DateTime? NextRetryAt { get; set; }
}