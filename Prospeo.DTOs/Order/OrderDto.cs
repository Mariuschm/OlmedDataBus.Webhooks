using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents a complete order in the system.
    /// Contains all information about an order including delivery details, line items, and financial summaries.
    /// </summary>
    /// <remarks>
    /// This DTO serves as the main container for order information and includes:
    /// <list type="bullet">
    /// <item><description>Order identification and marketplace details</description></item>
    /// <item><description>Customer information (buyer, recipient, invoice, receiver)</description></item>
    /// <item><description>Delivery and shipment details</description></item>
    /// <item><description>Order line items and financial summaries</description></item>
    /// <item><description>Payment and COD information</description></item>
    /// <item><description>Warehouse management system (WMS) status</description></item>
    /// </list>
    /// 
    /// <para>
    /// The order may contain different parties for buyer, recipient, invoice recipient, and receiver,
    /// allowing for complex B2B and B2C scenarios including gift deliveries, corporate orders,
    /// and drop shipping arrangements.
    /// </para>
    /// </remarks>
    public class OrderDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace where this order was placed.
        /// </summary>
        /// <value>
        /// The marketplace identifier or name (e.g., "Allegro", "Amazon", "Shopify").
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique internal order identifier.
        /// </summary>
        /// <value>
        /// The system-generated unique identifier for this order.
        /// </value>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the master system identifier.
        /// </summary>
        /// <value>
        /// The identifier from the master system that manages this order.
        /// Used for integration and synchronization purposes.
        /// </value>
        [JsonPropertyName("masterSystemId")]
        public int MasterSystemId { get; set; }

        /// <summary>
        /// Gets or sets the parent order identifier.
        /// </summary>
        /// <value>
        /// The ID of the parent order if this is a child or split order.
        /// Set to 0 if this is a standalone order with no parent.
        /// </value>
        /// <remarks>
        /// Used in scenarios where orders are split into multiple shipments or partial fulfillments.
        /// </remarks>
        [JsonPropertyName("parentOrderId")]
        public int ParentOrderId { get; set; }

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        /// <value>
        /// The human-readable order number displayed to customers.
        /// Returns <see cref="string.Empty"/> if not provided.
        /// </value>
        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order type.
        /// </summary>
        /// <value>
        /// An integer representing the order type (e.g., 1 = Standard, 2 = Express, 3 = Pre-order).
        /// </value>
        [JsonPropertyName("type")]
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the courier service identifier.
        /// </summary>
        /// <value>
        /// An integer representing the courier service used for delivery.
        /// </value>
        [JsonPropertyName("courier")]
        public int Courier { get; set; }

        /// <summary>
        /// Gets or sets the delivery point identifier.
        /// </summary>
        /// <value>
        /// The unique identifier for a pickup point, parcel locker, or delivery location.
        /// Returns <see cref="string.Empty"/> if home delivery or not applicable.
        /// </value>
        [JsonPropertyName("deliveryPointId")]
        public string DeliveryPointId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the delivery service identifier.
        /// </summary>
        /// <value>
        /// The identifier for the specific delivery service option selected.
        /// Returns <see cref="string.Empty"/> if not specified.
        /// </value>
        [JsonPropertyName("deliveryServiceId")]
        public string DeliveryServiceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Allegro seller identifier.
        /// </summary>
        /// <value>
        /// The unique seller identifier from the Allegro marketplace platform.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        [JsonPropertyName("allegroSellerId")]
        public string AllegroSellerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets order remarks or special instructions.
        /// </summary>
        /// <value>
        /// Free-text notes or special delivery instructions from the customer.
        /// Returns <see cref="string.Empty"/> if no remarks.
        /// </value>
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected realization/fulfillment datetime.
        /// </summary>
        /// <value>
        /// The date and time when the order should be fulfilled or delivered.
        /// Returns <see cref="string.Empty"/> if not specified.
        /// </value>
        [JsonPropertyName("realizationDatetime")]
        public string RealizationDatetime { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warehouse management system status.
        /// </summary>
        /// <value>
        /// An integer representing the current WMS processing status (e.g., 1 = New, 2 = Picking, 3 = Packed, 4 = Shipped).
        /// </value>
        [JsonPropertyName("wmsStatus")]
        public int WmsStatus { get; set; }

        /// <summary>
        /// Gets or sets the delivery recipient information.
        /// </summary>
        /// <value>
        /// Complete address and contact details for the delivery recipient.
        /// </value>
        [JsonPropertyName("recipient")]
        public OrderRecipientDto Recipient { get; set; } = new();

        /// <summary>
        /// Gets or sets the buyer information.
        /// </summary>
        /// <value>
        /// Complete details of the person or entity who placed and is paying for the order.
        /// </value>
        [JsonPropertyName("buyer")]
        public OrderBuyerDto Buyer { get; set; } = new();

        /// <summary>
        /// Gets or sets the invoice/billing information.
        /// </summary>
        /// <value>
        /// Complete billing address and tax details for invoice generation.
        /// </value>
        [JsonPropertyName("invoice")]
        public OrderInvoiceDto Invoice { get; set; } = new();

        /// <summary>
        /// Gets or sets the goods receiver information.
        /// </summary>
        /// <value>
        /// Legal entity or person details for the goods receiver (may differ from delivery recipient).
        /// </value>
        [JsonPropertyName("receiver")]
        public OrderReceiverDto Receiver { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether this is a Cash on Delivery (COD) order.
        /// </summary>
        /// <value>
        /// <c>true</c> if payment will be collected upon delivery; otherwise, <c>false</c>.
        /// </value>
        [JsonPropertyName("isCOD")]
        public bool IsCOD { get; set; }

        /// <summary>
        /// Gets or sets the shipment value for COD orders.
        /// </summary>
        /// <value>
        /// The amount to be collected from the customer on delivery.
        /// Set to 0 if not a COD order.
        /// </value>
        [JsonPropertyName("shipmentValue")]
        public decimal ShipmentValue { get; set; }

        /// <summary>
        /// Gets or sets the  order value without shippmentValue.
        /// </summary>
        /// <value>
        /// The total monetary value of the order including all items and charges.
        /// </value>
        [JsonPropertyName("orderValue")]
        public decimal OrderValue { get; set; }

        /// <summary>
        /// Gets or sets the number of packages in the shipment.
        /// </summary>
        /// <value>
        /// The expected or required number of packages for this order.
        /// </value>
        [JsonPropertyName("shipmentPackagesCount")]
        public int ShipmentPackagesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the exact number of packages is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the order must be shipped in exactly the number of packages specified in <see cref="ShipmentPackagesCount"/>;
        /// otherwise, <c>false</c> if the package count is flexible.
        /// </value>
        [JsonPropertyName("shipmentPackagesExactNumberRequired")]
        public bool ShipmentPackagesExactNumberRequired { get; set; }

        /// <summary>
        /// Gets or sets the collection of line items in the order.
        /// </summary>
        /// <value>
        /// A list of <see cref="OrderItemDto"/> representing each product ordered with quantities and pricing.
        /// </value>
        [JsonPropertyName("orderItems")]
        public List<OrderItemDto> OrderItems { get; set; } = new();

        /// <summary>
        /// Gets or sets the collection of order summaries.
        /// </summary>
        /// <value>
        /// A list of <see cref="OrderSummaryDto"/> representing additional charges, discounts, or fees
        /// (e.g., shipping costs, COD fees, discounts).
        /// </value>
        [JsonPropertyName("orderSummaries")]
        public List<OrderSummaryDto> OrderSummaries { get; set; } = new();

        /// <summary>
        /// Gets or sets marketplace-specific additional data.
        /// </summary>
        /// <value>
        /// Additional information specific to the marketplace platform where the order originated.
        /// </value>
        [JsonPropertyName("marketplaceAdditionalData")]
        public OrderMarketplaceAdditionalDataDto MarketplaceAdditionalData { get; set; } = new();

        /// <summary>
        /// Gets or sets the XL system order identifier.
        /// </summary>
        /// <value>
        /// The order identifier from the XL integration system.
        /// Used for cross-system tracking and synchronization.
        /// </value>
        public int XlOrderId { get; set; }
        /// <summary>
        /// Gets or sets the date and time when the product was last modified.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> representing the last modification timestamp.
        /// </value>
        /// <remarks>
        /// This timestamp is crucial for:
        /// <list type="bullet">
        /// <item><description>Data synchronization between systems (detecting changes)</description></item>
        /// <item><description>Audit trails and change tracking</description></item>
        /// <item><description>Determining which products need updates in marketplaces</description></item>
        /// <item><description>Version control and conflict resolution</description></item>
        /// </list>
        /// 
        /// <para>
        /// Uses <see cref="CustomDateTimeConverter"/> to handle the specific format "yyyy-MM-dd HH:mm:ss"
        /// and gracefully handle invalid dates like "0000-00-00 00:00:00".
        /// </para>
        /// </remarks>
        [JsonPropertyName("lastModifyDateTime")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime LastModifyDateTime { get; set; }
    }
}
