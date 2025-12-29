using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Represents a request to notify the system that an invoice has been sent to a customer.
    /// Used for tracking invoice delivery and maintaining audit trails.
    /// </summary>
    /// <remarks>
    /// This DTO is used when an invoice has been generated and sent to a customer,
    /// allowing the system to track invoice delivery status and maintain compliance records.
    /// 
    /// <para>
    /// Common scenarios:
    /// <list type="bullet">
    /// <item><description>Confirmation of email invoice delivery</description></item>
    /// <item><description>Recording physical invoice mailing</description></item>
    /// <item><description>Integration with electronic invoicing systems</description></item>
    /// <item><description>Audit trail for tax compliance</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class InvoiceSentRequest : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the invoice number that was sent.
        /// </summary>
        /// <value>
        /// The unique invoice identifier (e.g., "FV/2024/01/0001").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should match the invoice number in the accounting system and on the physical/electronic invoice.
        /// Required for tracking and auditing purposes.
        /// </remarks>
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the associated order identifier (if applicable).
        /// </summary>
        /// <value>
        /// The order ID or number that this invoice relates to.
        /// Returns <c>null</c> if the invoice is not directly associated with an order.
        /// </value>
        /// <remarks>
        /// Links the invoice to the original order for reference and tracking.
        /// May be null for invoices not tied to specific orders (e.g., service invoices, corrections).
        /// </remarks>
        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the invoice was sent.
        /// </summary>
        /// <value>
        /// The timestamp when the invoice was delivered to the customer.
        /// </value>
        /// <remarks>
        /// Important for:
        /// <list type="bullet">
        /// <item><description>Calculating payment due dates</description></item>
        /// <item><description>Tax reporting and compliance</description></item>
        /// <item><description>Tracking delivery SLAs</description></item>
        /// <item><description>Customer service and dispute resolution</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("sentDate")]
        public DateTime SentDate { get; set; }

        /// <summary>
        /// Gets or sets the email address where the invoice was sent (if applicable).
        /// </summary>
        /// <value>
        /// The recipient's email address for electronic invoice delivery.
        /// Returns <c>null</c> if the invoice was sent via other means (e.g., postal mail).
        /// </value>
        /// <remarks>
        /// Useful for:
        /// <list type="bullet">
        /// <item><description>Verifying correct recipient</description></item>
        /// <item><description>Troubleshooting delivery issues</description></item>
        /// <item><description>Resending if needed</description></item>
        /// <item><description>Audit trails for electronic invoicing</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("recipientEmail")]
        public string? RecipientEmail { get; set; }

        /// <summary>
        /// Gets or sets additional data related to the invoice delivery.
        /// </summary>
        /// <value>
        /// A flexible dictionary for storing supplementary information about the invoice delivery.
        /// Returns <c>null</c> if no additional data is provided.
        /// </value>
        /// <remarks>
        /// Can be used to store various contextual information such as:
        /// <list type="bullet">
        /// <item><description>Delivery method (email, postal, portal)</description></item>
        /// <item><description>Email tracking ID</description></item>
        /// <item><description>Electronic invoicing system reference</description></item>
        /// <item><description>Postal tracking number for physical invoices</description></item>
        /// <item><description>User who triggered the send operation</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("additionalData")]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Represents the response after recording that an invoice has been sent.
    /// Provides confirmation and processing details.
    /// </summary>
    /// <remarks>
    /// This response confirms that the invoice sent notification was received and processed
    /// by the system, updating relevant records and maintaining compliance logs.
    /// </remarks>
    public class InvoiceSentResponse : DTOModelBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the invoice sent notification was successfully recorded;
        /// otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// A <c>false</c> value indicates that the notification could not be processed.
        /// Check the <see cref="Message"/> property for error details.
        /// </remarks>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the result of the operation.
        /// </summary>
        /// <value>
        /// A descriptive message. On success, typically contains a confirmation message.
        /// On failure, contains error details.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Error messages should provide actionable information for troubleshooting, such as:
        /// <list type="bullet">
        /// <item><description>Invoice not found</description></item>
        /// <item><description>Invalid invoice number format</description></item>
        /// <item><description>Invoice already marked as sent</description></item>
        /// <item><description>Database or system errors</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the invoice number that was processed.
        /// </summary>
        /// <value>
        /// The invoice number from the request, echoed back for confirmation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Allows the caller to verify that the correct invoice record was updated,
        /// particularly important when processing multiple invoices concurrently.
        /// </remarks>
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the notification was processed.
        /// </summary>
        /// <value>
        /// The date and time (UTC) when the system processed this notification.
        /// </value>
        /// <remarks>
        /// This represents the server time when the record was updated, which may differ
        /// from the <see cref="InvoiceSentRequest.SentDate"/> (when the invoice was actually sent).
        /// 
        /// <para>
        /// Useful for:
        /// <list type="bullet">
        /// <item><description>Audit trails and compliance</description></item>
        /// <item><description>Detecting delays between sending and recording</description></item>
        /// <item><description>System performance monitoring</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
