using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Model danych stanu magazynowego produktu
    /// </summary>
    public class StockDto
    {
        /// <summary>
        /// Nazwa marketplace (np. "APTEKA_OLMED")
        /// </summary>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// S³ownik stanów magazynowych dla poszczególnych SKU
        /// Klucz: SKU produktu (string)
        /// Wartoœæ: StockItemDto z danymi o stanie i cenie
        /// </summary>
        [JsonPropertyName("skus")]
        public Dictionary<string, StockItemDto> Skus { get; set; } = new();
    }

    /// <summary>
    /// Model danych pojedynczego stanu magazynowego
    /// </summary>
    public class StockItemDto
    {
        /// <summary>
        /// Stan magazynowy produktu
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
