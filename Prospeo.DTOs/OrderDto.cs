using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{

    public class OrderDto
    {
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        [SpecialProperty("Attribute")]
        public int Id { get; set; }

        [JsonPropertyName("masterSystemId")]
        [SpecialProperty("Attribute")]
        public int MasterSystemId { get; set; }

        [JsonPropertyName("parentOrderId")]
        [SpecialProperty("Attribute")]
        public int ParentOrderId { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [SpecialProperty("Attribute")]
        public int Type { get; set; }

        [JsonPropertyName("courier")]
        public int Courier { get; set; }

        [JsonPropertyName("deliveryPointId")]
        [SpecialProperty("Attribute")]
        public string DeliveryPointId { get; set; } = string.Empty;

        [JsonPropertyName("deliveryServiceId")]
        [SpecialProperty("Attribute")]
        public string DeliveryServiceId { get; set; } = string.Empty;

        [JsonPropertyName("allegroSellerId")]
        [SpecialProperty("Attribute")]
        public string AllegroSellerId { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("realizationDatetime")]
        public string RealizationDatetime { get; set; } = string.Empty;

        [JsonPropertyName("wmsStatus")]
        [SpecialProperty("Attribute")]
        public int WmsStatus { get; set; }

        [JsonPropertyName("recipient")]
        public OrderRecipientDto Recipient { get; set; } = new();

        [JsonPropertyName("buyer")]
        public OrderBuyerDto Buyer { get; set; } = new();

        [JsonPropertyName("invoice")]
        public OrderInvoiceDto Invoice { get; set; } = new();

        [JsonPropertyName("receiver")]
        public OrderReceiverDto Receiver { get; set; } = new();

        [JsonPropertyName("isCOD")]
        [SpecialProperty("Attribute")]
        public bool IsCOD { get; set; }

        [JsonPropertyName("shipmentValue")]
        [SpecialProperty("Attribute")]
        public decimal ShipmentValue { get; set; }

        [JsonPropertyName("shipmentPackagesCount")]
        [SpecialProperty("Attribute")]
        public int ShipmentPackagesCount { get; set; }

        [JsonPropertyName("shipmentPackagesExactNumberRequired")]
        [SpecialProperty("Attribute")]
        public bool ShipmentPackagesExactNumberRequired { get; set; }

        [JsonPropertyName("orderItems")]
        public List<OrderItemDto> OrderItems { get; set; } = new();

        [JsonPropertyName("orderSummaries")]
        public List<OrderSummaryDto> OrderSummaries { get; set; } = new();

        [JsonPropertyName("marketplaceAdditionalData")]
        public OrderMarketplaceAdditionalDataDto MarketplaceAdditionalData { get; set; } = new();
        public int XlOrderId { get; set; }
    }
}
