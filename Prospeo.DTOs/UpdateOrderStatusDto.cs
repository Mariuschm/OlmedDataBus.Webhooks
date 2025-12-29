using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents a request to update the status of an order in the system.
    /// Used for tracking order lifecycle and coordinating between different systems.
    /// </summary>
    /// <remarks>
    /// This DTO enables external systems (marketplaces, warehouse systems, logistics providers)
    /// to update the order status as it progresses through different stages of fulfillment.
    /// 
    /// <para>
    /// Common use cases:
    /// <list type="bullet">
    /// <item><description>Warehouse confirming order acceptance for fulfillment</description></item>
    /// <item><description>Marking orders as ready for shipment</description></item>
    /// <item><description>Recording handoff to courier/delivery service</description></item>
    /// <item><description>Canceling orders or reporting inventory shortages</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UpdateOrderStatusRequest
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// The marketplace name or code where the order originated (e.g., "APTEKA_OLMED", "ALLEGRO").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Required to identify which marketplace's order is being updated,
        /// as order numbers may not be unique across all marketplaces.
        /// </remarks>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number to be updated.
        /// </summary>
        /// <value>
        /// The unique order identifier within the specified marketplace.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// This should match the order number from the original order data.
        /// Combined with <see cref="Marketplace"/>, uniquely identifies the order to update.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status for the order.
        /// </summary>
        /// <value>
        /// The target status code or name (e.g., "PROCESSING", "READY_TO_SHIP", "SHIPPED", "CANCELED").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// The status value should correspond to one of the valid order statuses defined in the system.
        /// See <see cref="OrderStatus"/> enum for standard status codes used in the Polish market.
        /// 
        /// <para>
        /// Status transitions should follow business rules (e.g., can't skip from "pending" to "delivered"
        /// without going through intermediate statuses).
        /// </para>
        /// </remarks>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional note or comment about the status change.
        /// </summary>
        /// <value>
        /// Additional context or explanation for the status change.
        /// Returns <c>null</c> if not provided.
        /// </value>
        /// <remarks>
        /// Useful for providing context such as:
        /// <list type="bullet">
        /// <item><description>Reason for cancellation</description></item>
        /// <item><description>Delay explanations</description></item>
        /// <item><description>Special handling instructions</description></item>
        /// <item><description>Customer service notes</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the tracking number for the shipment (if applicable).
        /// </summary>
        /// <value>
        /// The courier's tracking/waybill number for the shipment.
        /// Returns <c>null</c> if not applicable or not yet available.
        /// </value>
        /// <remarks>
        /// Should be provided when the status indicates the order has been handed to a courier
        /// (e.g., "SHIPPED" or "IN_TRANSIT"). Enables customers to track their shipments.
        /// 
        /// <para>
        /// This number should be from the courier service's system and can be used
        /// on their tracking website or API.
        /// </para>
        /// </remarks>
        [JsonPropertyName("trackingNumber")]
        public string? TrackingNumber { get; set; }
    }

    /// <summary>
    /// Defines standard order status codes used in the Polish market.
    /// These statuses represent typical stages in the order fulfillment lifecycle.
    /// </summary>
    /// <remarks>
    /// This enumeration provides standardized status codes that correspond to common
    /// order processing stages in Polish e-commerce and logistics systems.
    /// 
    /// <para>
    /// <strong>Status Flow:</strong> Orders typically progress through these statuses in sequence,
    /// though some statuses (like cancellation) can occur at various points in the lifecycle.
    /// </para>
    /// </remarks>
    public enum OrderStatus
    {
        /// <summary>
        /// Order requires customer service intervention or information hotline contact.
        /// Status value: -1
        /// </summary>
        /// <remarks>
        /// Indicates that the order cannot proceed automatically and requires human intervention,
        /// such as missing information, payment issues, or special requests.
        /// </remarks>
        Infolinia = -1,

        /// <summary>
        /// Order is awaiting acceptance for processing.
        /// Status value: 0
        /// </summary>
        /// <remarks>
        /// Initial status when order is received but not yet accepted into the fulfillment system.
        /// Awaiting validation, payment confirmation, or warehouse acceptance.
        /// </remarks>
        Oczekuje_na_przyjecie = 0,

        /// <summary>
        /// Order has been accepted and is being processed/prepared for shipment.
        /// Status value: 1
        /// </summary>
        /// <remarks>
        /// Order has been validated and accepted into the warehouse system.
        /// Items are being picked, packed, and prepared for delivery.
        /// </remarks>
        Przyjeto_do_realizacji = 1,

        /// <summary>
        /// Order is packed and ready for shipment/courier pickup.
        /// Status value: 5
        /// </summary>
        /// <remarks>
        /// All items have been picked and packed. The order is ready to be
        /// handed over to the courier or delivery service.
        /// </remarks>
        Gotowe_do_wysylki = 5,

        /// <summary>
        /// Order has been canceled.
        /// Status value: 8
        /// </summary>
        /// <remarks>
        /// Order was canceled by customer, system, or customer service.
        /// No further processing will occur. May trigger refund processes.
        /// </remarks>
        Anulowano = 8,

        /// <summary>
        /// Order has been handed over to the courier/delivery service.
        /// Status value: 9
        /// </summary>
        /// <remarks>
        /// Order is now in transit with the courier. A tracking number should be available.
        /// Customer should be notified with tracking information.
        /// </remarks>
        Przekazane_do_kuriera = 9,

        /// <summary>
        /// Inventory shortages have been reported for this order.
        /// Status value: 100
        /// </summary>
        /// <remarks>
        /// Some or all items in the order are out of stock. Customer service needs to
        /// contact the customer to discuss options (wait, substitute, partial fulfillment, or cancel).
        /// </remarks>
        Zgloszono_braki = 100
    }

    /// <summary>
    /// Represents the response after updating an order's status.
    /// Provides confirmation and details about the status change operation.
    /// </summary>
    /// <remarks>
    /// This response confirms whether the status update was successful and provides
    /// the updated status information for verification.
    /// </remarks>
    public class UpdateOrderStatusResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the status update was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the order status was successfully updated;
        /// otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// A <c>false</c> value indicates the update failed. Check <see cref="Message"/>
        /// for details about why the update was rejected.
        /// </remarks>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the result of the operation.
        /// </summary>
        /// <value>
        /// A descriptive message. On success, confirms the status change.
        /// On failure, explains why the update was rejected.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Error messages may include:
        /// <list type="bullet">
        /// <item><description>Order not found</description></item>
        /// <item><description>Invalid status transition</description></item>
        /// <item><description>Order already in final status (delivered/canceled)</description></item>
        /// <item><description>Missing required information (e.g., tracking number for shipped status)</description></item>
        /// </list>
        /// </remarks>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order number that was processed.
        /// </summary>
        /// <value>
        /// The order number from the request, echoed back for confirmation.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        /// <remarks>
        /// Allows the caller to verify that the correct order was updated,
        /// especially important when processing multiple status updates concurrently.
        /// </remarks>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status that was applied to the order.
        /// </summary>
        /// <value>
        /// The status code or name that the order now has after the update.
        /// Returns <see cref="string.Empty"/> if not provided or update failed.
        /// </value>
        /// <remarks>
        /// This confirms the actual status stored in the system, which should match
        /// the requested status from <see cref="UpdateOrderStatusRequest.Status"/>.
        /// If they differ, it may indicate a normalization or validation occurred.
        /// </remarks>
        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; } = string.Empty;
    }
}
