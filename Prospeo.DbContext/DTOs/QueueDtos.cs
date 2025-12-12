using System.ComponentModel.DataAnnotations;
using Prospeo.DbContext.DTOs;

namespace Prospeo.DbContext.DTOs;

/// <summary>
/// DTO dla wyœwietlania danych zadania kolejki
/// </summary>
public class QueueDto
{
    /// <summary>Identyfikator zadania</summary>
    public int Id { get; set; }

    /// <summary>Unikalny identyfikator wiersza</summary>
    public Guid RowID { get; set; }

    /// <summary>Identyfikator firmy</summary>
    public int FirmaId { get; set; }

    /// <summary>Informacje o firmie</summary>
    public FirmaDto? Firma { get; set; }

    /// <summary>Zakres operacji</summary>
    public int Scope { get; set; }

    /// <summary>¯¹danie (skrócone dla bezpieczeñstwa)</summary>
    public string RequestPreview { get; set; } = string.Empty;

    /// <summary>Data dodania</summary>
    public DateTime DateAdd { get; set; }

    /// <summary>Data modyfikacji</summary>
    public DateTime DateMod { get; set; }

    /// <summary>Flagi statusu</summary>
    public int Flg { get; set; }

    /// <summary>Opis zadania</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Identyfikator docelowy</summary>
    public int TargetID { get; set; }
}

/// <summary>
/// DTO dla tworzenia nowego zadania kolejki
/// </summary>
public class CreateQueueDto
{
    /// <summary>Identyfikator firmy</summary>
    [Required(ErrorMessage = "ID firmy jest wymagane")]
    public int FirmaId { get; set; }

    /// <summary>Zakres operacji</summary>
    [Required(ErrorMessage = "Zakres operacji jest wymagany")]
    public int Scope { get; set; }

    /// <summary>¯¹danie w formacie JSON/XML</summary>
    [Required(ErrorMessage = "¯¹danie jest wymagane")]
    public string Request { get; set; } = string.Empty;

    /// <summary>Flagi statusu</summary>
    public int Flg { get; set; } = 0;

    /// <summary>Opis zadania</summary>
    [Required(ErrorMessage = "Opis zadania jest wymagany")]
    [StringLength(1024, ErrorMessage = "Opis nie mo¿e przekraczaæ 1024 znaków")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Identyfikator docelowy</summary>
    [Required(ErrorMessage = "ID docelowe jest wymagane")]
    public int TargetID { get; set; }
}

/// <summary>
/// DTO dla aktualizacji zadania kolejki
/// </summary>
public class UpdateQueueDto
{
    /// <summary>Identyfikator firmy</summary>
    [Required(ErrorMessage = "ID firmy jest wymagane")]
    public int FirmaId { get; set; }

    /// <summary>Zakres operacji</summary>
    [Required(ErrorMessage = "Zakres operacji jest wymagany")]
    public int Scope { get; set; }

    /// <summary>¯¹danie w formacie JSON/XML</summary>
    [Required(ErrorMessage = "¯¹danie jest wymagane")]
    public string Request { get; set; } = string.Empty;

    /// <summary>Flagi statusu</summary>
    public int Flg { get; set; }

    /// <summary>Opis zadania</summary>
    [Required(ErrorMessage = "Opis zadania jest wymagany")]
    [StringLength(1024, ErrorMessage = "Opis nie mo¿e przekraczaæ 1024 znaków")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Identyfikator docelowy</summary>
    [Required(ErrorMessage = "ID docelowe jest wymagane")]
    public int TargetID { get; set; }
}

/// <summary>
/// DTO dla wyszukiwania zadañ kolejki
/// </summary>
public class QueueSearchDto
{
    /// <summary>Filtr: ID firmy</summary>
    public int? FirmaId { get; set; }

    /// <summary>Filtr: zakres operacji</summary>
    public int? Scope { get; set; }

    /// <summary>Filtr: flagi statusu</summary>
    public int? Flg { get; set; }

    /// <summary>Filtr: identyfikator docelowy</summary>
    public int? TargetID { get; set; }

    /// <summary>Filtr: data od</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Filtr: data do</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Wyszukiwanie w opisie</summary>
    public string? Description { get; set; }

    /// <summary>Numer strony (1-based)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Numer strony musi byæ wiêkszy od 0")]
    public int Page { get; set; } = 1;

    /// <summary>Rozmiar strony</summary>
    [Range(1, 100, ErrorMessage = "Rozmiar strony musi byæ miêdzy 1 a 100")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// DTO dla stronicowanej listy zadañ kolejki
/// </summary>
public class PagedQueueDto
{
    /// <summary>Lista zadañ na bie¿¹cej stronie</summary>
    public IEnumerable<QueueDto> Items { get; set; } = new List<QueueDto>();

    /// <summary>Bie¿¹ca strona</summary>
    public int CurrentPage { get; set; }

    /// <summary>Rozmiar strony</summary>
    public int PageSize { get; set; }

    /// <summary>Ca³kowita liczba rekordów</summary>
    public int TotalCount { get; set; }

    /// <summary>Ca³kowita liczba stron</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Czy jest poprzednia strona</summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>Czy jest nastêpna strona</summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// DTO dla pe³nych danych zadania kolejki (z pe³nym Request)
/// </summary>
public class QueueDetailDto : QueueDto
{
    /// <summary>Pe³ne ¿¹danie (tylko dla uprawnionychu¿ytkowników)</summary>
    public string FullRequest { get; set; } = string.Empty;
}

/// <summary>
/// DTO dla statystyk kolejki
/// </summary>
public class QueueStatsDto
{
    /// <summary>Ca³kowita liczba zadañ</summary>
    public int TotalCount { get; set; }

    /// <summary>Liczba zadañ na firmê</summary>
    public Dictionary<int, int> CountByFirma { get; set; } = new();

    /// <summary>Liczba zadañ na zakres</summary>
    public Dictionary<int, int> CountByScope { get; set; } = new();

    /// <summary>Liczba zadañ na flagê</summary>
    public Dictionary<int, int> CountByFlag { get; set; } = new();

    /// <summary>Najstarsze zadanie</summary>
    public DateTime? OldestTask { get; set; }

    /// <summary>Najnowsze zadanie</summary>
    public DateTime? NewestTask { get; set; }
}