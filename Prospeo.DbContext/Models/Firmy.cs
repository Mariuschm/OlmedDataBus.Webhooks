using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Prospeo.DbContext.Models;

/// <summary>
/// Model reprezentujący firmę w systemie ProRWS
/// </summary>
[Table("Firmy", Schema = "ProRWS")]
public class Firmy
{
    /// <summary>
    /// Identyfikator firmy (klucz główny, auto-increment)
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
    /// Nazwa firmy
    /// </summary>
    [Column("NazwaFirmy")]
    [Required]
    [MaxLength(255)]
    public string NazwaFirmy { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa bazy danych ERP dla tej firmy
    /// </summary>
    [Column("NazwaBazyERP")]
    [Required]
    [MaxLength(255)]
    public string NazwaBazyERP { get; set; } = string.Empty;

    /// <summary>
    /// Określa czy firma jest testowa
    /// </summary>
    [Column("CzyTestowa")]
    [Required]
    public bool CzyTestowa { get; set; }

    /// <summary>
    /// Klucz API dla autoryzacji (opcjonalny)
    /// </summary>
    [Column("ApiKey")]
    [MaxLength(255)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Określa czy autoryzować wszystkie endpointy (opcjonalny)
    /// </summary>
    [Column("AuthorizeAllEndpoints")]
    public bool? AuthorizeAllEndpoints { get; set; }

    /// <summary>
    /// Adres URL endpointu API dla tej firmy (opcjonalny)
    /// </summary>
    [Column("Endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Kolekcja zadań w kolejce powiązanych z tą firmą
    /// </summary>
    public virtual ICollection<Queue> QueueItems { get; set; } = new List<Queue>();
}
