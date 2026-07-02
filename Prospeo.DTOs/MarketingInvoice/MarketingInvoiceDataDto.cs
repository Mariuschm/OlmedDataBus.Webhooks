using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.MarketingInvoice
{
    /// <summary>
    /// Represents marketing invoice data associated with a marketplace order.
    /// This DTO contains marketplace-specific billing and service information.
    /// </summary>
    /// <remarks>
    /// This class captures marketplace-specific marketing invoice information including
    /// user identification, service details, payment terms, and date ranges.
    /// 
    /// <para>
    /// Common use cases include:
    /// <list type="bullet">
    /// <item><description>Tracking marketing services provided to customers</description></item>
    /// <item><description>Recording marketplace-specific invoice numbers and payment status</description></item>
    /// <item><description>Managing service periods and billing dates</description></item>
    /// <item><description>Storing online/offline transaction percentages</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MarketingInvoiceDataDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// The marketplace platform identifier (e.g., "OLMED_CSM").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("marketplace")]
        [SpecialProperty("Atrybut")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>
        /// The primary user identifier for the marketplace transaction.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("userId")]
        [SpecialProperty("Atrybut")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the online user identifier.
        /// </summary>
        /// <value>
        /// The user identifier for online transactions.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("onlineUserId")]
        [SpecialProperty("Atrybut")]
        public int OnlineUserId { get; set; }

        /// <summary>
        /// Gets or sets the offline user identifier.
        /// </summary>
        /// <value>
        /// The user identifier for offline transactions.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("offlineUserId")]
        [SpecialProperty("Atrybut")]
        public int OfflineUserId { get; set; }

        /// <summary>
        /// Gets or sets the online transaction percentage.
        /// </summary>
        /// <value>
        /// The percentage of transactions conducted online (0-100).
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("onlinePercentage")]
        [SpecialProperty("Atrybut")]
        public int OnlinePercentage { get; set; }

        /// <summary>
        /// Gets or sets the offline transaction percentage.
        /// </summary>
        /// <value>
        /// The percentage of transactions conducted offline (0-100).
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("offlinePercentage")]
        [SpecialProperty("Atrybut")]
        public int OfflinePercentage { get; set; }

        /// <summary>
        /// Gets or sets the quarter identifier.
        /// </summary>
        /// <value>
        /// The business quarter identifier (e.g., "Q1", "Q2").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("quarter")]
        [SpecialProperty("Atrybut")]
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requester details.
        /// </summary>
        /// <value>
        /// The requester data including name, address, and tax identification number.
        /// </value>
        [JsonPropertyName("requester")]
        public MarketingInvoiceRequesterDto Requester { get; set; } = new();

        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        /// <value>
        /// The unique invoice number (e.g., "2026/03/014").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("number")]
        [SpecialProperty("Atrybut")]
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the invoice is paid.
        /// </summary>
        /// <value>
        /// 1 if the invoice is paid, 0 otherwise.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("isPaid")]
        [SpecialProperty("Atrybut")]
        public int IsPaid { get; set; }

        /// <summary>
        /// Gets or sets the net value of the invoice.
        /// </summary>
        /// <value>
        /// The net monetary value of the invoice.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("valueNet")]
        [SpecialProperty("Atrybut")]
        public decimal ValueNet { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        /// <value>
        /// The description of the marketing service provided (e.g., "Us³uga marketingowa").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("serviceName")]
        [SpecialProperty("Atrybut")]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the GTU12 mark flag.
        /// </summary>
        /// <value>
        /// 1 if marked as GTU12 category, 0 otherwise.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("markGtu12")]
        [SpecialProperty("Atrybut")]
        public int MarkGtu12 { get; set; }

        /// <summary>
        /// Gets or sets the service start date.
        /// </summary>
        /// <value>
        /// The date when the service period begins (format: "YYYY-MM-DD").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("dateStart")]
        [SpecialProperty("Atrybut")]
        public string DateStart { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the service end date.
        /// </summary>
        /// <value>
        /// The date when the service period ends (format: "YYYY-MM-DD").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("dateEnd")]
        [SpecialProperty("Atrybut")]
        public string DateEnd { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payment terms identifier.
        /// </summary>
        /// <value>
        /// The identifier representing the payment terms conditions.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("paymentTerms")]
        [SpecialProperty("Atrybut")]
        public int PaymentTerms { get; set; }

        /// <summary>
        /// Gets or sets the payment type identifier.
        /// </summary>
        /// <value>
        /// The identifier representing the payment method type.
        /// Returns 0 if not applicable or not provided.
        /// </value>
        [JsonPropertyName("paymentType")]
        [SpecialProperty("Atrybut")]
        public int PaymentType { get; set; }
    }
}
