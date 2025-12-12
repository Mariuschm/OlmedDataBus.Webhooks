using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.DTO
{
    /// <summary>
    /// Reprezentuje dane faktury w zamówieniu
    /// </summary>
    public class OrderInvoiceDto
    {
        [JsonPropertyName("invoiceNIP")]
        public string InvoiceNIP { get; set; } = string.Empty;

        [JsonPropertyName("invoiceName")]
        public string InvoiceName { get; set; } = string.Empty;

        [JsonPropertyName("invoiceStreet")]
        public string InvoiceStreet { get; set; } = string.Empty;

        [JsonPropertyName("invoiceCompany")]
        public string InvoiceCompany { get; set; } = string.Empty;

        [JsonPropertyName("invoiceCity")]
        public string InvoiceCity { get; set; } = string.Empty;

        [JsonPropertyName("invoicePostalCode")]
        public string InvoicePostalCode { get; set; } = string.Empty;

        [JsonPropertyName("invoiceBuildingNo")]
        public string InvoiceBuildingNo { get; set; } = string.Empty;

        [JsonPropertyName("invoiceFlatNo")]
        public string InvoiceFlatNo { get; set; } = string.Empty;

        [JsonPropertyName("invoiceCountryCode")]
        public string InvoiceCountryCode { get; set; } = string.Empty;
    }
}
