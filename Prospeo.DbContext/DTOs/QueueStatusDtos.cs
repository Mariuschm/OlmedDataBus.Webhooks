using System.ComponentModel.DataAnnotations;

namespace Prospeo.DbContext.DTOs;

/// <summary>
/// DTO dla wyœwietlania danych statusu kolejki
/// </summary>
public class QueueStatusDto
{
    /// <summary>Identyfikator statusu</summary>
    public int Id { get; set; }

    /// <summary>Unikalny identyfikator wiersza</summary>
    public Guid RowID { get; set; }

    /// <summary>Nazwa statusu</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Wartoœæ numeryczna statusu</summary>
    public int Value { get; set; }

    /// <summary>Opis statusu</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO dla tworzenia nowego statusu kolejki
/// </summary>
public class CreateQueueStatusDto
{
    /// <summary>Nazwa statusu</summary>
    [Required(ErrorMessage = "Nazwa statusu jest wymagana")]
    [StringLength(16, ErrorMessage = "Nazwa statusu nie mo¿e przekraczaæ 16 znaków")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Wartoœæ numeryczna statusu (unikalna)</summary>
    [Required(ErrorMessage = "Wartoœæ statusu jest wymagana")]
    public int Value { get; set; }

    /// <summary>Opis statusu</summary>
    [Required(ErrorMessage = "Opis statusu jest wymagany")]
    [StringLength(1024, ErrorMessage = "Opis statusu nie mo¿e przekraczaæ 1024 znaków")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO dla aktualizacji statusu kolejki
/// </summary>
public class UpdateQueueStatusDto
{
    /// <summary>Nazwa statusu</summary>
    [Required(ErrorMessage = "Nazwa statusu jest wymagana")]
    [StringLength(16, ErrorMessage = "Nazwa statusu nie mo¿e przekraczaæ 16 znaków")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Wartoœæ numeryczna statusu (unikalna)</summary>
    [Required(ErrorMessage = "Wartoœæ statusu jest wymagana")]
    public int Value { get; set; }

    /// <summary>Opis statusu</summary>
    [Required(ErrorMessage = "Opis statusu jest wymagany")]
    [StringLength(1024, ErrorMessage = "Opis statusu nie mo¿e przekraczaæ 1024 znaków")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO dla wyszukiwania statusów kolejki
/// </summary>
public class QueueStatusSearchDto
{
    /// <summary>Wyszukiwanie po nazwie statusu</summary>
    public string? Name { get; set; }

    /// <summary>Filtr: zakres wartoœci od</summary>
    public int? ValueFrom { get; set; }

    /// <summary>Filtr: zakres wartoœci do</summary>
    public int? ValueTo { get; set; }

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
/// DTO dla stronicowanej listy statusów kolejki
/// </summary>
public class PagedQueueStatusDto
{
    /// <summary>Lista statusów na bie¿¹cej stronie</summary>
    public IEnumerable<QueueStatusDto> Items { get; set; } = new List<QueueStatusDto>();

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
/// Prosty DTO dla listy wyboru statusów
/// </summary>
public class QueueStatusLookupDto
{
    /// <summary>Wartoœæ statusu</summary>
    public int Value { get; set; }

    /// <summary>Nazwa statusu</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opis statusu</summary>
    public string Description { get; set; } = string.Empty;
}