using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prospeo.DbContext.Models;

/// <summary>
/// Model reprezentuj¹cy status kolejki w systemie ProRWS
/// </summary>
[Table("QueueStatus", Schema = "ProRWS")]
public class QueueStatus
{
    /// <summary>
    /// Identyfikator statusu kolejki (klucz g³ówny, auto-increment)
    /// </summary>
    [Key]
    [Column("Id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unikalny identyfikator wiersza (GUID)
    /// </summary>
    [Column("RowID")]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid RowID { get; set; }

    /// <summary>
    /// Nazwa statusu (maksymalnie 16 znaków)
    /// </summary>
    [Column("Name")]
    [Required]
    [MaxLength(16)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Wartoœæ numeryczna statusu (unikalna)
    /// </summary>
    [Column("Value")]
    [Required]
    public int Value { get; set; }

    /// <summary>
    /// Opis statusu (maksymalnie 1024 znaki)
    /// </summary>
    [Column("Description")]
    [Required]
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}