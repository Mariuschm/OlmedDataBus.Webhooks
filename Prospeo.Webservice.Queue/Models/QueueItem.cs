using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prospeo.Webservice.Queue.Models;

/// <summary>
/// Model elementu kolejki w bazie danych
/// </summary>
public class QueueItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Typ zadania w kolejce (np. "email", "webhook", "notification")
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Dane zadania w formacie JSON
    /// </summary>
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Aktualny status zadania
    /// </summary>
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;

    /// <summary>
    /// Liczba prÛb przetworzenia
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Maksymalna liczba prÛb
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Data utworzenia zadania
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data zakoÒczenia przetwarzania (udanego lub nieudanego)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Planowana data kolejnej prÛby dla nieudanych zadaÒ
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Komunikat b≥Ídu z ostatniej prÛby
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace b≥Ídu z ostatniej prÛby
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Identyfikator korelacji dla grupowania powiπzanych zadaÒ
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// èrÛd≥o zadania (np. nazwa serwisu, ktÛry je utworzy≥)
    /// </summary>
    [MaxLength(200)]
    public string? Source { get; set; }

    /// <summary>
    /// Priorytet zadania (wyøszy numer = wyøszy priorytet)
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Statusy elementÛw kolejki
/// </summary>
public enum QueueItemStatus
{
    /// <summary>Oczekuje na przetworzenie</summary>
    Pending = 0,
    
    /// <summary>Aktualnie przetwarzane</summary>
    Processing = 1,
    
    /// <summary>ZakoÒczone pomyúlnie</summary>
    Completed = 2,
    
    /// <summary>ZakoÒczone niepowodzeniem</summary>
    Failed = 3,
    
    /// <summary>Anulowane</summary>
    Cancelled = 4
}