using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents the response to a stock update operation.
    /// Provides detailed information about the success, scope, and results of the stock synchronization.
    /// </summary>
    /// <remarks>
    /// This DTO is returned after processing a <see cref="StockUpdateRequest"/> to inform
    /// the caller about the outcome of the stock update operation. It includes both
    /// success/failure status and detailed information about which SKUs were updated.
    /// 
    /// <para>
    /// Typical use cases:
    /// <list type="bullet">
    /// <item><description>Confirming successful inventory synchronization</description></item>
    /// <item><description>Identifying which products were updated</description></item>
    /// <item><description>Troubleshooting failed or partial updates</description></item>
    /// <item><description>Logging and auditing stock changes</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StockUpdateResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the stock update operation was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if all stock updates were processed successfully;
        /// <c>false</c> if the operation failed or was only partially successful.
        /// </value>
        /// <remarks>
        /// A <c>false</c> value indicates that at least one error occurred during processing.
        /// Check the <see cref="Message"/> property for details about the failure.
        /// </remarks>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the result of the operation.
        /// </summary>
        /// <value>
        /// A descriptive message about the operation result. On success, typically contains
        /// a confirmation message. On failure, contains error details.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This message should provide actionable information for troubleshooting when
        /// <see cref="Success"/> is <c>false</c>. It may include details such as:
        /// <list type="bullet">
        /// <item><description>Validation errors (invalid SKUs, negative quantities, etc.)</description></item>
        /// <item><description>System errors (database connection issues, API failures, etc.)</description></item>
        /// <item><description>Business logic errors (insufficient permissions, locked records, etc.)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the marketplace identifier for which stock was updated.
        /// </summary>
        /// <value>
        /// The marketplace name or code (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This echoes back the marketplace from the request to confirm which marketplace's
        /// stock levels were affected. Useful when processing updates for multiple marketplaces.
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of SKUs that were successfully updated.
        /// </summary>
        /// <value>
        /// The count of unique SKUs whose stock levels were successfully modified.
        /// </value>
        /// <remarks>
        /// This count represents the number of products that had their inventory levels
        /// successfully updated in the system. Compare this with the number of SKUs
        /// in the request to detect partial failures.
        /// 
        /// <para>
        /// If <see cref="UpdatedCount"/> is less than the number of requested SKUs,
        /// some updates failed. Check <see cref="Message"/> for details and
        /// <see cref="UpdatedSkus"/> to see which SKUs were successful.
        /// </para>
        /// </remarks>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Gets or sets the list of SKUs that were successfully updated.
        /// </summary>
        /// <value>
        /// A collection of SKU strings representing products whose stock levels were successfully modified.
        /// </value>
        /// <remarks>
        /// This list provides explicit confirmation of which products were updated,
        /// enabling the caller to:
        /// <list type="bullet">
        /// <item><description>Verify that specific critical products were updated</description></item>
        /// <item><description>Identify which SKUs failed by comparing with the request</description></item>
        /// <item><description>Implement retry logic for failed SKUs only</description></item>
        /// <item><description>Generate detailed logs for audit and compliance</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("updatedSkus")]
        public List<string> UpdatedSkus { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the stock update was processed.
        /// </summary>
        /// <value>
        /// The date and time (UTC) when the stock update operation completed.
        /// </value>
        /// <remarks>
        /// This timestamp is crucial for:
        /// <list type="bullet">
        /// <item><description>Audit trails and compliance reporting</description></item>
        /// <item><description>Detecting stale or out-of-sequence updates</description></item>
        /// <item><description>Performance monitoring and SLA tracking</description></item>
        /// <item><description>Troubleshooting timing-related issues in distributed systems</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> This should represent the server time when processing completed,
        /// not when the request was received or initiated.
        /// </para>
        /// </remarks>
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
