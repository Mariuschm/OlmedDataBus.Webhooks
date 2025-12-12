using System.ComponentModel.DataAnnotations;

namespace Prospeo.DbContext.DTOs;

/// <summary>
/// DTO dla wyœwietlania danych firmy
/// </summary>
public class FirmaDto
{
    /// <summary>Identyfikator firmy</summary>
    public int Id { get; set; }

    /// <summary>Unikalny identyfikator wiersza</summary>
    public Guid RowID { get; set; }

    /// <summary>Nazwa firmy</summary>
    public string NazwaFirmy { get; set; } = string.Empty;

    /// <summary>Nazwa bazy danych ERP</summary>
    public string NazwaBazyERP { get; set; } = string.Empty;

    /// <summary>Czy firma jest testowa</summary>
    public bool CzyTestowa { get; set; }

    /// <summary>Czy ma klucz API (bez ujawniania wartoœci)</summary>
    public bool MaApiKey { get; set; }

    /// <summary>Czy autoryzowaæ wszystkie endpointy</summary>
    public bool? AuthorizeAllEndpoints { get; set; }
}

/// <summary>
/// DTO dla tworzenia nowej firmy
/// </summary>
public class CreateFirmaDto
{
    /// <summary>Nazwa firmy</summary>
    [Required(ErrorMessage = "Nazwa firmy jest wymagana")]
    [StringLength(255, ErrorMessage = "Nazwa firmy nie mo¿e przekraczaæ 255 znaków")]
    public string NazwaFirmy { get; set; } = string.Empty;

    /// <summary>Nazwa bazy danych ERP</summary>
    [Required(ErrorMessage = "Nazwa bazy ERP jest wymagana")]
    [StringLength(255, ErrorMessage = "Nazwa bazy ERP nie mo¿e przekraczaæ 255 znaków")]
    public string NazwaBazyERP { get; set; } = string.Empty;

    /// <summary>Czy firma jest testowa</summary>
    public bool CzyTestowa { get; set; }

    /// <summary>Klucz API (opcjonalny)</summary>
    [StringLength(255, ErrorMessage = "Klucz API nie mo¿e przekraczaæ 255 znaków")]
    public string? ApiKey { get; set; }

    /// <summary>Czy autoryzowaæ wszystkie endpointy</summary>
    public bool? AuthorizeAllEndpoints { get; set; }
}

/// <summary>
/// DTO dla aktualizacji firmy
/// </summary>
public class UpdateFirmaDto
{
    /// <summary>Nazwa firmy</summary>
    [Required(ErrorMessage = "Nazwa firmy jest wymagana")]
    [StringLength(255, ErrorMessage = "Nazwa firmy nie mo¿e przekraczaæ 255 znaków")]
    public string NazwaFirmy { get; set; } = string.Empty;

    /// <summary>Nazwa bazy danych ERP</summary>
    [Required(ErrorMessage = "Nazwa bazy ERP jest wymagana")]
    [StringLength(255, ErrorMessage = "Nazwa bazy ERP nie mo¿e przekraczaæ 255 znaków")]
    public string NazwaBazyERP { get; set; } = string.Empty;

    /// <summary>Czy firma jest testowa</summary>
    public bool CzyTestowa { get; set; }

    /// <summary>Klucz API (opcjonalny, null oznacza brak zmiany)</summary>
    [StringLength(255, ErrorMessage = "Klucz API nie mo¿e przekraczaæ 255 znaków")]
    public string? ApiKey { get; set; }

    /// <summary>Czy autoryzowaæ wszystkie endpointy</summary>
    public bool? AuthorizeAllEndpoints { get; set; }
}

/// <summary>
/// DTO dla wyszukiwania firm
/// </summary>
public class FirmaSearchDto
{
    /// <summary>Wyszukiwanie po nazwie firmy</summary>
    public string? NazwaFirmy { get; set; }

    /// <summary>Filtr: tylko firmy testowe/produkcyjne</summary>
    public bool? CzyTestowa { get; set; }

    /// <summary>Filtr: tylko firmy z kluczem API</summary>
    public bool? MaApiKey { get; set; }

    /// <summary>Numer strony (1-based)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Numer strony musi byæ wiêkszy od 0")]
    public int Page { get; set; } = 1;

    /// <summary>Rozmiar strony</summary>
    [Range(1, 100, ErrorMessage = "Rozmiar strony musi byæ miêdzy 1 a 100")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// DTO dla stronicowanej listy firm
/// </summary>
public class PagedFirmyDto
{
    /// <summary>Lista firm na bie¿¹cej stronie</summary>
    public IEnumerable<FirmaDto> Items { get; set; } = new List<FirmaDto>();

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