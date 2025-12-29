using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents the response after processing an order return request.
    /// Provides confirmation and detailed information about the return operation.
    /// </summary>
    /// <remarks>
    /// This DTO is returned after processing an <see cref="OrderReturnDto"/> to inform
    /// the caller about the outcome of the return operation. It includes both success/failure
    /// status and detailed information about which items were processed.
    /// 
    /// <para>
    /// The response is used for:
    /// <list type="bullet">
    /// <item><description>Confirming successful return registration</description></item>
    /// <item><description>Identifying which products were returned to inventory</description></item>
    /// <item><description>Troubleshooting failed or partial returns</description></item>
    /// <item><description>Triggering refund processes</description></item>
    /// <item><description>Logging and auditing return transactions</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderReturnResponse : DTOModelBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the return operation was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if all returned items were processed successfully;
        /// <c>false</c> if the operation failed or was only partially successful.
        /// </value>
        /// <remarks>
        /// A <c>false</c> value indicates that at least one error occurred during processing.
        /// Check the <see cref="Message"/> property for details about the failure.
        /// 
        /// <para>
        /// Even if <see cref="Success"/> is <c>false</c>, some items may have been processed
        /// successfully. Check <see cref="ReturnedItemsCount"/> and <see cref="ReturnedSkus"/>
        /// to determine what was actually returned.
        /// </para>
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
        /// <see cref="Success"/> is <c>false</c>. Common error scenarios include:
        /// <list type="bullet">
        /// <item><description>Order not found or invalid reference number</description></item>
        /// <item><description>Return window expired (outside return policy period)</description></item>
        /// <item><description>Invalid SKUs or quantities exceeding original order</description></item>
        /// <item><description>Warehouse not found or inactive</description></item>
        /// <item><description>System errors (database issues, connectivity problems, etc.)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reference number of the original order that was returned.
        /// </summary>
        /// <value>
        /// The order number from the request, echoed back for confirmation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Echoing back the reference number allows the caller to:
        /// <list type="bullet">
        /// <item><description>Verify that the correct order was processed</description></item>
        /// <item><description>Correlate the response with the request in async processing scenarios</description></item>
        /// <item><description>Maintain audit trails linking returns to orders</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the marketplace identifier where the original order was placed.
        /// </summary>
        /// <value>
        /// The marketplace name or code from the request.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Confirms which marketplace's return policies and procedures were applied
        /// to this return. Important for:
        /// <list type="bullet">
        /// <item><description>Routing refund requests to the correct payment system</description></item>
        /// <item><description>Applying marketplace-specific return policies</description></item>
        /// <item><description>Return metrics and analysis by marketplace</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of individual line items that were successfully returned.
        /// </summary>
        /// <value>
        /// The count of distinct SKUs (line items) whose return was successfully processed.
        /// </value>
        /// <remarks>
        /// This count represents the number of different products that were returned,
        /// not the total quantity of units.
        /// 
        /// <para>
        /// For example:
        /// <list type="bullet">
        /// <item><description>Returning 5 units of SKU-A and 3 units of SKU-B = 2 items</description></item>
        /// <item><description>Returning 10 units of a single SKU = 1 item</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// Compare this with the number of items in the request to detect partial failures.
        /// If <see cref="ReturnedItemsCount"/> is less than requested, some items failed processing.
        /// </para>
        /// </remarks>
        [JsonPropertyName("returnedItemsCount")]
        public int ReturnedItemsCount { get; set; }

        /// <summary>
        /// Gets or sets the list of SKUs that were successfully returned to inventory.
        /// </summary>
        /// <value>
        /// A collection of SKU strings representing products whose return was successfully processed.
        /// </value>
        /// <remarks>
        /// This list provides explicit confirmation of which products were returned,
        /// enabling the caller to:
        /// <list type="bullet">
        /// <item><description>Verify that specific critical products were returned successfully</description></item>
        /// <item><description>Identify which SKUs failed by comparing with the request</description></item>
        /// <item><description>Implement retry logic for only the failed SKUs</description></item>
        /// <item><description>Generate detailed logs for audit and compliance purposes</description></item>
        /// <item><description>Trigger refund processes for successfully returned items</description></item>
        /// <item><description>Update order status and customer communications</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> If a SKU appears in this list, it means the return was successfully
        /// registered in the system, but it doesn't necessarily mean the refund has been processed yet.
        /// </para>
        /// </remarks>
        [JsonPropertyName("returnedSkus")]
        public List<string> ReturnedSkus { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the return was processed.
        /// </summary>
        /// <value>
        /// The date and time (UTC) when the return operation completed.
        /// </value>
        /// <remarks>
        /// This timestamp is crucial for:
        /// <list type="bullet">
        /// <item><description>Audit trails and compliance reporting</description></item>
        /// <item><description>Tracking return processing times and SLA compliance</description></item>
        /// <item><description>Calculating refund timelines (e.g., "refund within 7 days of return processing")</description></item>
        /// <item><description>Financial period allocation for accounting purposes</description></item>
        /// <item><description>Performance monitoring of return processing systems</description></item>
        /// <item><description>Synchronization with external systems (marketplaces, payment providers)</description></item>
        /// </list>
        /// 
        /// <para>
        /// <strong>Note:</strong> This represents the server time when processing completed,
        /// which may be different from when the physical goods were received at the warehouse
        /// or when the customer initiated the return.
        /// </para>
        /// </remarks>
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; }
    }
}
