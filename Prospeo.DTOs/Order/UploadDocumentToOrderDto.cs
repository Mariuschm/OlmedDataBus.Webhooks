using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents a request to upload a document (invoice or correction) to an order.
    /// Used to attach invoices and corrective invoices to orders in the system.
    /// </summary>
    /// <remarks>
    /// This DTO enables external systems to upload order-related documents such as:
    /// <list type="bullet">
    /// <item><description>VAT invoices (faktura VAT)</description></item>
    /// <item><description>Corrective invoices (faktura koryguj¹ca)</description></item>
    /// <item><description>Pro-forma invoices</description></item>
    /// </list>
    /// 
    /// Documents can be uploaded in XML or PDF format depending on the business requirements
    /// and integration capabilities.
    /// </remarks>
    public class UploadDocumentToOrderRequest : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// The marketplace name or code where the order originated (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Required to identify which marketplace's order is receiving the document,
        /// as order numbers may not be unique across all marketplaces.
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number to which the document will be attached.
        /// </summary>
        /// <value>
        /// The unique order identifier within the specified marketplace.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should match the order number from the original order data.
        /// Combined with <see cref="Marketplace"/>, uniquely identifies the order.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of document being uploaded.
        /// </summary>
        /// <value>
        /// The document type identifier (e.g., "invoice", "correction").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Common document types:
        /// <list type="bullet">
        /// <item><description>"invoice" - Standard VAT invoice</description></item>
        /// <item><description>"correction" - Corrective invoice (faktura koryguj¹ca)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file format of the document.
        /// </summary>
        /// <value>
        /// The file format identifier (e.g., "xml", "pdf").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Supported formats:
        /// <list type="bullet">
        /// <item><description>"xml" - XML format (e.g., for structured e-invoicing)</description></item>
        /// <item><description>"pdf" - PDF format (for human-readable documents)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("fileFormat")]
        public string FileFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the binary content of the document file.
        /// </summary>
        /// <value>
        /// The document file content encoded as a base64 string.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The file must be encoded as a base64 string before sending.
        /// The system will decode and store the file in its original format.
        /// 
        /// <para>
        /// Maximum file size and format validation is performed on the server side.
        /// </para>
        /// </remarks>
        [JsonPropertyName("documentFile")]
        public string DocumentFile { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response after successfully uploading a document to an order.
    /// Provides confirmation and details about the upload operation.
    /// </summary>
    /// <remarks>
    /// This response confirms that the document was received, validated, and attached
    /// to the specified order.
    /// </remarks>
    public class UploadDocumentToOrderResponse : DTOModelBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the upload was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the document was successfully uploaded and attached to the order;
        /// otherwise, <c>false</c>.
        /// </value>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the result of the operation.
        /// </summary>
        /// <value>
        /// A descriptive message indicating success or explaining any errors that occurred.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// On success, this typically contains a confirmation message.
        /// On failure, this should contain detailed error information such as:
        /// <list type="bullet">
        /// <item><description>Order not found</description></item>
        /// <item><description>Invalid file format</description></item>
        /// <item><description>File size exceeds limit</description></item>
        /// <item><description>Invalid base64 encoding</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number that received the document.
        /// </summary>
        /// <value>
        /// The order number from the request, echoed back for confirmation and correlation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This allows the caller to verify that the document was attached to the correct order,
        /// especially when uploading documents to multiple orders concurrently.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of document that was uploaded.
        /// </summary>
        /// <value>
        /// The document type from the request, echoed back for confirmation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;
    }
}
