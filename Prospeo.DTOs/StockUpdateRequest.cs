using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Model ¿¹dania aktualizacji stanu magazynowego
    /// </summary>
    public class StockUpdateRequest
    {
        /// <summary>
        /// Nazwa marketplace (np. "APTEKA_OLMED")
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// S³ownik SKU do aktualizacji
        /// Klucz: SKU produktu (string)
        /// Wartoœæ: StockUpdateItemDto z danymi o stanie i cenie
        /// </summary>
        [JsonPropertyName("skus")]
        public Dictionary<string, StockUpdateItemDto> Skus { get; set; } = new();

        /// <summary>
        /// Dodatkowe informacje o aktualizacji
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Data aktualizacji (opcjonalna, domyœlnie bie¿¹ca)
        /// </summary>
        [JsonPropertyName("updateDate")]
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Model danych pojedynczej aktualizacji stanu
    /// </summary>
    public class StockUpdateItemDto
    {
        /// <summary>
        /// Nowy stan magazynowy
        /// </summary>
        [JsonPropertyName("stock")]
        public decimal Stock { get; set; }

        /// <summary>
        /// Œrednia cena zakupu
        /// </summary>
        [JsonPropertyName("average_purchase_price")]
        public decimal AveragePurchasePrice { get; set; }
    }
}
