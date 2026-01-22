using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prospeo.DbContext.Enums;

namespace Prospeo.DbContext.Models;

/// <summary>
/// Model reprezentuj¹cy kolejkê zadañ w systemie ProRWS
/// </summary>
[Table("Queue", Schema = "ProRWS")]
public class Queue
{
    /// <summary>
    /// Identyfikator kolejki (klucz g³ówny, auto-increment)
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
    /// Identyfikator firmy (klucz obcy do tabeli Firmy)
    /// </summary>
    [Column("Firma")]
    [Required]
    [ForeignKey(nameof(Firma))]
    public int FirmaId { get; set; }

    /// <summary>
    /// Zakres operacji (typ enum QueueScope)
    /// </summary>
    [Column("Scope")]
    [Required]
    public int Scope { get; set; }

    /// <summary>
    /// Zakres operacji jako enum
    /// </summary>
    [NotMapped]
    public QueueScope ScopeEnum
    {
        get => (QueueScope)Scope;
        set => Scope = (int)value;
    }

    /// <summary>
    /// ¯¹danie w formacie JSON/XML (bez ograniczenia d³ugoœci)
    /// </summary>
    [Column("Request", TypeName = "varchar(max)")]
    [Required]
    public string Request { get; set; } = string.Empty;

    /// <summary>
    /// Data dodania (timestamp Unix)
    /// </summary>
    [Column("DateAdd")]
    [Required]
    public int DateAdd { get; set; }

    /// <summary>
    /// Data modyfikacji (timestamp Unix)
    /// </summary>
    [Column("DateMod")]
    [Required]
    public int DateMod { get; set; }

    /// <summary>
    /// Flagi statusu zadania (0=oczekuje, 5=w trakcie, 1=zakoñczone, -1=b³¹d)
    /// </summary>
    [Column("Flg")]
    [Required]
    public int Flg { get; set; }

    /// <summary>
    /// Status zadania jako enum
    /// </summary>
    [NotMapped]
    public Enums.QueueStatusEnum FlgEnum
    {
        get => (Enums.QueueStatusEnum)Flg;
        set => Flg = (int)value;
    }

    /// <summary>
    /// Opis zadania (maksymalnie 1024 znaki), domyœlnie pusty - tam zapisywane s¹ dano winservcice
    /// </summary>
    [Column("Description", TypeName = "varchar(1024)")]
    [Required]
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Identyfikator docelowy - jako insert 0, do update identyfikator obiektu docelowego
    /// </summary>
    [Column("TargetID")]
    [Required]
    public int TargetID { get; set; }

    public string? WebhookRawData { get; set; } = string.Empty;
    public string? ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Relacja do firmy
    /// </summary>
    public virtual Firmy? Firma { get; set; }

    /// <summary>
    /// Relacje, gdzie ten element jest Ÿród³em (parent)
    /// </summary>
    /// <remarks>
    /// Kolekcja wszystkich relacji, gdzie to zadanie jest elementem nadrzêdnym.
    /// Przyk³ad: zamówienie mo¿e mieæ wiele wydañ magazynowych jako "dzieci".
    /// </remarks>
    [InverseProperty(nameof(QueueRelations.SourceItem))]
    public virtual ICollection<QueueRelations> SourceRelations { get; set; } = new HashSet<QueueRelations>();

    /// <summary>
    /// Relacje, gdzie ten element jest celem (child)
    /// </summary>
    /// <remarks>
    /// Kolekcja wszystkich relacji, gdzie to zadanie jest elementem zale¿nym.
    /// Przyk³ad: faktura mo¿e byæ powi¹zana z jednym lub wiêcej zamówieniami.
    /// </remarks>
    [InverseProperty(nameof(QueueRelations.TargetItem))]
    public virtual ICollection<QueueRelations> TargetRelations { get; set; } = new HashSet<QueueRelations>();

    /// <summary>
    /// W³aœciwoœæ pomocnicza - konwertuje DateAdd na DateTime (format Clarion: sekundy od 1990-01-01)
    /// </summary>
    [NotMapped]
    public DateTime DateAddDateTime 
    { 
        get
        {
            var clarionEpoch = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return clarionEpoch.AddSeconds(DateAdd);
        }
        set
        {
            var clarionEpoch = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateAdd = (int)(value - clarionEpoch).TotalSeconds;
        }
    }

    /// <summary>
    /// W³aœciwoœæ pomocnicza - konwertuje DateMod na DateTime (format Clarion: sekundy od 1990-01-01)
    /// </summary>
    [NotMapped]
    public DateTime DateModDateTime 
    { 
        get
        {
            var clarionEpoch = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return clarionEpoch.AddSeconds(DateMod);
        }
        set
        {
            var clarionEpoch = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateMod = (int)(value - clarionEpoch).TotalSeconds;
        }
    }
}