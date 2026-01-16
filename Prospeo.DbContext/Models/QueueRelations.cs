using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prospeo.DbContext.Models;

/// <summary>
/// Model reprezentuj¹cy relacje miêdzy elementami kolejki w systemie ProRWS.
/// U¿ywany do œledzenia zale¿noœci miêdzy zadaniami w kolejce.
/// </summary>
/// <remarks>
/// Ta tabela pozwala na:
/// <list type="bullet">
/// <item><description>Œledzenie hierarchii zadañ (parent-child relationships)</description></item>
/// <item><description>Mapowanie zale¿noœci miêdzy ró¿nymi typami operacji (np. zamówienie ? faktura)</description></item>
/// <item><description>Zachowanie spójnoœci danych podczas przetwarzania kolejki</description></item>
/// <item><description>Umo¿liwienie rozproszonych transakcji i rollback'ów</description></item>
/// </list>
/// 
/// <para>
/// Przyk³ady u¿ycia:
/// <list type="bullet">
/// <item><description>Zamówienie (OrderQueueService) generuje fakturê (InvoiceQueueService) - zapisujemy relacjê</description></item>
/// <item><description>Korekta faktury (CorrectionQueueService) musi wiedzieæ, któr¹ fakturê koryguje</description></item>
/// <item><description>Wydanie magazynowe (MmwQueueService) jest powi¹zane z zamówieniem</description></item>
/// </list>
/// </para>
/// </remarks>
[Table("QueueRelations", Schema = "ProRWS")]
public class QueueRelations
{
    /// <summary>
    /// Identyfikator relacji (klucz g³ówny, auto-increment)
    /// </summary>
    [Key]
    [Column("Id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Identyfikator Ÿród³owego elementu kolejki (klucz obcy do tabeli Queue)
    /// </summary>
    /// <remarks>
    /// To jest element nadrzêdny w relacji - na przyk³ad zamówienie, które inicjuje 
    /// utworzenie faktury lub wydania magazynowego.
    /// </remarks>
    [Column("SourceItemId")]
    [Required]
    [ForeignKey(nameof(SourceItem))]
    public int SourceItemId { get; set; }

    /// <summary>
    /// Identyfikator docelowego elementu kolejki (klucz obcy do tabeli Queue)
    /// </summary>
    /// <remarks>
    /// To jest element zale¿ny w relacji - na przyk³ad faktura utworzona na podstawie 
    /// zamówienia lub dokument koryguj¹cy powsta³y z oryginalnej faktury.
    /// </remarks>
    [Column("TargetItemId")]
    [Required]
    [ForeignKey(nameof(TargetItem))]
    public int TargetItemId { get; set; }

    /// <summary>
    /// Data utworzenia relacji (automatycznie ustawiana przez bazê danych)
    /// </summary>
    /// <remarks>
    /// U¿ywa DATETIME2(0) dla lepszej precyzji ni¿ DATETIME, ale bez milisekund.
    /// Wartoœæ domyœlna ustawiana przez SYSDATETIME() w SQL Server.
    /// </remarks>
    [Column("CreatedAt")]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Relacja nawigacyjna do Ÿród³owego elementu kolejki
    /// </summary>
    /// <remarks>
    /// Pozwala na ³atwe pobieranie danych Ÿród³owego zadania poprzez Entity Framework:
    /// <code>
    /// var relation = await context.QueueRelations
    ///     .Include(r => r.SourceItem)
    ///     .FirstOrDefaultAsync(r => r.Id == relationId);
    /// </code>
    /// </remarks>
    public virtual Queue? SourceItem { get; set; }

    /// <summary>
    /// Relacja nawigacyjna do docelowego elementu kolejki
    /// </summary>
    /// <remarks>
    /// Pozwala na ³atwe pobieranie danych docelowego zadania poprzez Entity Framework:
    /// <code>
    /// var relation = await context.QueueRelations
    ///     .Include(r => r.TargetItem)
    ///     .FirstOrDefaultAsync(r => r.Id == relationId);
    /// </code>
    /// </remarks>
    public virtual Queue? TargetItem { get; set; }
}
