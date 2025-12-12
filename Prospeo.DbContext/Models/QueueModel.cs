using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prospeo.DbContext.Models;

/// <summary>
/// Model kolejki w schemacie ProRWS
/// UWAGA: Ten model może być zastąpiony przez Prospeo.Webservice.Queue.Models.QueueItem
/// Zachowany dla kompatybilności wstecznej lub specyficznych potrzeb schematu ProRWS
/// </summary>
[Table("Queue", Schema = "ProRWS")]
public class QueueModel
{
    /// <summary>
    /// Identyfikator elementu kolejki
    /// </summary>
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Typ zadania w kolejce
    /// </summary>
    [Column("Type")]
    [MaxLength(500)]
    public string? Type { get; set; }

    /// <summary>
    /// Dane zadania w formacie JSON
    /// </summary>
    [Column("Payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Status zadania (0=Pending, 1=Processing, 2=Completed, 3=Failed)
    /// </summary>
    [Column("Status")]
    public int Status { get; set; }

    /// <summary>
    /// Data utworzenia
    /// </summary>
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data przetworzenia
    /// </summary>
    [Column("ProcessedAt")]
    public DateTime? ProcessedAt { get; set; }

    // Dodaj inne pola zgodnie z potrzebami schematu ProRWS
}

// UWAGA: Rozważ użycie Prospeo.Webservice.Queue zamiast tego modelu
// dla pełniejszej funkcjonalności kolejki
