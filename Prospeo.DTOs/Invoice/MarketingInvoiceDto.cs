using Prospeo.DTOs.Core;
using System;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Represents a marketing invoice DTO for OLMED_CSM marketplace.
    /// Used to transfer marketing invoice data between different layers of the application.
    /// </summary>
    /// <remarks>
    /// This class is used to create marketing invoices in the system. It contains information about
    /// user assignments (online/offline), service details, payment terms, and GTU markings.
    /// 
    /// <para>
    /// The online and offline percentages must sum to 100. Payment terms are restricted to
    /// specific day values: 7, 14, 21, 30, 40, 45, 60, or 90 days.
    /// </para>
    /// </remarks>
    public class MarketingInvoiceDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// Always set to "OLMED_CSM" for marketing invoices.
        /// </value>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = "OLMED_CSM";

        /// <summary>
        /// Gets or sets the ID of the user creating the order.
        /// </summary>
        /// <value>
        /// The unique identifier of the user who created this marketing invoice order.
        /// </value>
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the online channel supervisor.
        /// </summary>
        /// <value>
        /// The unique identifier of the user responsible for online sales supervision.
        /// </value>
        [JsonPropertyName("onlineUserId")]
        public int OnlineUserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the offline channel supervisor.
        /// </summary>
        /// <value>
        /// The unique identifier of the user responsible for offline sales supervision.
        /// </value>
        [JsonPropertyName("offlineUserId")]
        public int OfflineUserId { get; set; }

        /// <summary>
        /// Gets or sets the percentage share of online sales in the order.
        /// </summary>
        /// <value>
        /// A value between 0 and 100. The sum of <see cref="OnlinePercentage"/> and 
        /// <see cref="OfflinePercentage"/> must equal 100.
        /// </value>
        [JsonPropertyName("onlinePercentage")]
        public float OnlinePercentage { get; set; }

        /// <summary>
        /// Gets or sets the percentage share of offline sales in the order.
        /// </summary>
        /// <value>
        /// A value between 0 and 100. The sum of <see cref="OnlinePercentage"/> and 
        /// <see cref="OfflinePercentage"/> must equal 100.
        /// </value>
        [JsonPropertyName("offlinePercentage")]
        public float OfflinePercentage { get; set; }

        /// <summary>
        /// Gets or sets the quarter for which the invoice is issued.
        /// </summary>
        /// <value>
        /// One of the following values: 'Q1', 'Q2', 'Q3', or 'Q4'.
        /// </value>
        [JsonPropertyName("quarter")]
        public string Quarter { get; set; }

        /// <summary>
        /// Gets or sets the name of the person requesting the invoice on behalf of the client.
        /// </summary>
        /// <value>
        /// The full name or identifier of the requester from the client's organization.
        /// </value>
        [JsonPropertyName("requester")]
        public string Requester { get; set; }

        /// <summary>
        /// Gets or sets the order/invoice number.
        /// </summary>
        /// <value>
        /// The unique number identifying this marketing invoice order.
        /// </value>
        [JsonPropertyName("number")]
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the payment status of the invoice.
        /// </summary>
        /// <value>
        /// 0 for unpaid, 1 for paid.
        /// </value>
        [JsonPropertyName("isPaid")]
        public int IsPaid { get; set; }

        /// <summary>
        /// Gets or sets the net value of the order.
        /// </summary>
        /// <value>
        /// The total net amount (excluding VAT) of the invoice.
        /// </value>
        [JsonPropertyName("valueNet")]
        public float ValueNet { get; set; }

        /// <summary>
        /// Gets or sets the name of the service being invoiced.
        /// </summary>
        /// <value>
        /// A descriptive name of the marketing service provided.
        /// </value>
        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the GTU-12 marking for the invoice.
        /// </summary>
        /// <value>
        /// 0 for no GTU-12 marking, 1 for GTU-12 marking applied.
        /// GTU-12 is used for services related to intermediation and other services 
        /// related to the delivery of goods and services.
        /// </value>
        [JsonPropertyName("markGtu12")]
        public int MarkGtu12 { get; set; }

        /// <summary>
        /// Gets or sets the start date of the service period.
        /// </summary>
        /// <value>
        /// The date when the marketing service period begins, in ISO 8601 format (yyyy-MM-dd).
        /// </value>
        [JsonPropertyName("dateStart")]
        public string DateStart { get; set; }

        /// <summary>
        /// Gets or sets the end date of the service period.
        /// </summary>
        /// <value>
        /// The date when the marketing service period ends, in ISO 8601 format (yyyy-MM-dd).
        /// </value>
        [JsonPropertyName("dateEnd")]
        public string DateEnd { get; set; }

        /// <summary>
        /// Gets or sets the payment terms in days.
        /// </summary>
        /// <value>
        /// The number of days until payment is due. Valid values are: 7, 14, 21, 30, 40, 45, 60, or 90.
        /// </value>
        [JsonPropertyName("paymentTerms")]
        public int PaymentTerms { get; set; }

        /// <summary>
        /// Gets or sets the payment type/method.
        /// </summary>
        /// <value>
        /// The payment method identifier:
        /// <list type="bullet">
        /// <item><description>1 - Bank transfer (przelew)</description></item>
        /// <item><description>2 - Card payment (karta)</description></item>
        /// <item><description>3 - Cash (gotówka)</description></item>
        /// <item><description>4 - Compensation (kompensata)</description></item>
        /// </list>
        /// </value>
        [JsonPropertyName("paymentType")]
        public int PaymentType { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        /// <value>
        /// The unique identifier of the customer associated with this marketing invoice.
        /// </value>
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the internal invoice identifier.
        /// </summary>
        /// <value>
        /// Internal invoice ID used for tracking and reference within the system. 
        /// This is not the same as the invoice number visible to clients.
        /// </value>
        [JsonPropertyName("invoiceId")]
        public int InvoiceId { get; set; }
    }
}
