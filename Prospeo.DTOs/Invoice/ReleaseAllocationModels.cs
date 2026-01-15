using System;
using System.Collections.Generic;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Reprezentuje alokacjê pozycji dokumentu wydania (WZ) do linii zamówienia
    /// </summary>
    public class ReleaseLineAllocation
    {
        /// <summary>
        /// Numer linii zamówienia
        /// </summary>
        public int OrderLineNumber { get; set; }

        /// <summary>
        /// Kod SKU towaru
        /// </summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Zamówiona iloœæ
        /// </summary>
        public decimal OrderedQuantity { get; set; }

        /// <summary>
        /// Zrealizowana iloœæ
        /// </summary>
        public decimal AllocatedQuantity { get; set; }

        /// <summary>
        /// Pozosta³a iloœæ do realizacji
        /// </summary>
        public decimal RemainingQuantity => OrderedQuantity - AllocatedQuantity;

        /// <summary>
        /// Czy linia zosta³a w pe³ni zrealizowana
        /// </summary>
        public bool IsFullyAllocated => AllocatedQuantity >= OrderedQuantity;

        /// <summary>
        /// Czy nast¹pi³o przekroczenie zamówionej iloœci
        /// </summary>
        public bool IsOverAllocated => AllocatedQuantity > OrderedQuantity;

        /// <summary>
        /// Lista przypisanych pozycji dokumentu wydania
        /// </summary>
        public List<ReleaseItemAllocation> AllocatedItems { get; set; } = new();
    }

    /// <summary>
    /// Reprezentuje pojedyncz¹ pozycjê dokumentu wydania przypisan¹ do linii zamówienia.
    /// Zawiera wszystkie pola z AgileroIssueDocumentItem potrzebne do zapisu w bazie danych.
    /// </summary>
    public class ReleaseItemAllocation
    {
        /// <summary>
        /// Kod nadrzêdnego artyku³u (SKU)
        /// </summary>
        public string ParentArticleCode { get; set; } = string.Empty;

        /// <summary>
        /// Kod artyku³u
        /// </summary>
        public string ArticleCode { get; set; } = string.Empty;

        /// <summary>
        /// Iloœæ zrealizowana
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Numer serii towaru
        /// </summary>
        public string SeriesNumber { get; set; } = string.Empty;

        /// <summary>
        /// Data wa¿noœci towaru
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// ID magazynu
        /// </summary>
        public int Warehouse { get; set; }

        /// <summary>
        /// ID Ÿród³owego artyku³u
        /// </summary>
        public int SourceArticle_Id { get; set; }

        /// <summary>
        /// ID artyku³u w systemie g³ównym
        /// </summary>
        public int ArticleMasterSystemId { get; set; }
    }

    /// <summary>
    /// Wynik walidacji alokacji pozycji dokumentu wydania do linii zamówienia
    /// </summary>
    public class ReleaseAllocationValidationResult
    {
        /// <summary>
        /// Czy alokacja jest poprawna (wszystkie linie w pe³ni zrealizowane)
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Lista b³êdów walidacji (niedobory, nadmiary)
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Lista ostrze¿eñ (np. nieprzypisane pozycje)
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Lista alokacji dla wszystkich linii zamówienia
        /// </summary>
        public List<ReleaseLineAllocation> Allocations { get; set; } = new();

        /// <summary>
        /// Pozycje dokumentu wydania, które nie zosta³y przypisane do ¿adnej linii zamówienia
        /// </summary>
        public List<AgileroIssueDocumentItem> UnallocatedItems { get; set; } = new();
    }
}
