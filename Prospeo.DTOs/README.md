# Prospeo.DTOs

## Overview

The **Prospeo.DTOs** project is a .NET 9.0 class library that contains Data Transfer Objects (DTOs) used for communication between the OlmedDataBus webhook service and external systems. This library provides a centralized, strongly-typed data contract layer for all API endpoints and webhook integrations.

## Project Information

- **Target Framework**: .NET 9.0
- **Language Version**: C# 13.0
- **Platform**: AnyCPU
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled

## Purpose

This library serves as the contract layer for:
- Webhook payload structures
- Product synchronization
- Order management
- Stock updates
- Invoice processing
- API request/response models

## Core Components

### Base Classes

#### `DTOModelBase`
Abstract base class for all DTOs that provides automatic initialization of string properties to empty strings, preventing null reference issues.

**Features:**
- Automatically initializes all string properties to `string.Empty` in the constructor
- Uses reflection to scan and initialize properties at instantiation
- Provides a consistent foundation for all DTO models

**Usage:**
```csharp
public class MyDto : DTOModelBase
{
    public string Name { get; set; }
    // Name will be initialized to string.Empty automatically
}
```

### Webhook Models

#### `WebhookPayload`
Core webhook payload structure for data transfer.

**Properties:**
- `guid` - Unique identifier for the webhook payload
- `webhookType` - Category or type of webhook event
- `webhookData` - Serialized data payload

#### `ProcessedWebhookPayloadDto`
Extended webhook payload with processing information.

### Product Models

#### `ProductDto`
Complete product information model with extensive metadata.

**Key Properties:**
- `Id` - Product identifier
- `Sku` - Stock Keeping Unit
- `Ean` - European Article Number
- `Name` - Product name
- `Marketplace` - Marketplace identifier
- `IsActive` - Active status
- `Dimensions` - Product dimensions (ProductDimensionsDto)
- `Weight` - Product weight
- `VatRate` - VAT rate
- `LastModifyDateTime` - Last modification timestamp
- Various flags: `IsExpirationDateRequired`, `IsSeriesNumberRequired`, `IsQualityControlRequired`, `IsRefrigeratedStorage`, `IsRefrigeratedTransport`, `IsFragile`, `IsDiscounted`, etc.

**Special Features:**
- Uses `CustomDateTimeConverter` for handling "yyyy-MM-dd HH:mm:ss" date format
- Includes `SpecialPropertyAttribute` for property categorization
- Handles invalid dates like "0000-00-00 00:00:00" by converting to current DateTime

#### `ProductDimensionsDto`
Three-dimensional product measurements.

**Properties:**
- `X` - Length dimension
- `Y` - Width dimension
- `Z` - Height dimension

### Stock Models

#### `StockDto`
Stock level information for products.

**Properties:**
- `Marketplace` - Marketplace identifier (e.g., "APTEKA_OLMED")
- `Skus` - Dictionary of SKU to StockItemDto mappings

#### `StockItemDto`
Individual stock item details.

**Properties:**
- `Stock` - Available stock quantity
- `AveragePurchasePrice` - Average purchase price

#### `StockUpdateRequest`
Request model for stock updates.

#### `StockUpdateResponse`
Response model for stock update operations.

### Order Models

#### `OrderDto`
Complete order information model.

**Key Properties:**
- `Id` - Order identifier
- `Number` - Order number
- `Marketplace` - Marketplace identifier
- `Type` - Order type
- `Courier` - Courier identifier
- `DeliveryPointId` - Delivery point identifier
- `Remarks` - Order remarks
- `RealizationDatetime` - Realization date and time
- `WmsStatus` - Warehouse Management System status
- `IsCOD` - Cash on Delivery flag
- `ShipmentValue` - Shipment value
- `ShipmentPackagesCount` - Number of packages
- `Recipient` - Recipient details (OrderRecipientDto)
- `Buyer` - Buyer details (OrderBuyerDto)
- `Invoice` - Invoice details (OrderInvoiceDto)
- `Receiver` - Receiver details (OrderReceiverDto)
- `OrderItems` - List of order items (OrderItemDto)
- `OrderSummaries` - List of order summaries (OrderSummaryDto)
- `MarketplaceAdditionalData` - Additional marketplace data (OrderMarketplaceAdditionalDataDto)

#### `OrderItemDto`
Individual order line item.

#### `OrderBuyerDto`
Buyer information for an order.

#### `OrderRecipientDto`
Recipient information for an order.

#### `OrderReceiverDto`
Receiver information for an order.

#### `OrderInvoiceDto`
Invoice details associated with an order.

#### `OrderSummaryDto`
Order summary information.

#### `OrderMarketplaceAdditionalDataDto`
Additional marketplace-specific data.

#### `OrderRealizationDto`
Order realization/fulfillment details.

#### `OrderReturnDto`
Order return request model.

#### `OrderReturnResponse`
Order return response model.

#### `UpdateOrderStatusDto`
Model for updating order status.

### Invoice Models

#### `PurchaseInvoiceModelDTO`
Purchase invoice information model.

#### `InvoiceSentDto`
Invoice sent notification model.

## Custom Attributes

### `SpecialPropertyAttribute`
Marks properties for special processing or grouping.

**Usage:**
```csharp
[SpecialProperty("ProductFlags")]
public bool IsRefrigeratedStorage { get; set; }
```

**Categories:**
- `"Attribute"` - Properties that are attributes
- `"ProductFlags"` - Product-specific flags
- `"Default"` - Default category

## Custom JSON Converters

### `CustomDateTimeConverter`
Handles DateTime serialization/deserialization with specific format requirements.

**Features:**
- Parses "yyyy-MM-dd HH:mm:ss" format
- Handles invalid dates "0000-00-00 00:00:00" by returning `DateTime.Now`
- Fallback to standard DateTime parsing
- Throws `JsonException` for unparseable dates

**Usage:**
```csharp
[JsonPropertyName("lastModifyDateTime")]
[JsonConverter(typeof(CustomDateTimeConverter))]
public DateTime LastModifyDateTime { get; set; }
```

## JSON Serialization

All DTOs use `System.Text.Json` with `JsonPropertyName` attributes for property mapping. This ensures:
- Consistent JSON property naming (typically camelCase)
- Clear mapping between C# properties and JSON fields
- Type-safe serialization/deserialization

## Dependencies

This project has no external dependencies beyond the .NET 9.0 framework. It uses only:
- `System.Text.Json` for serialization
- `System.Reflection` for property initialization in `DTOModelBase`
- Standard .NET collections

## Usage in Projects

The DTOs in this library are referenced by:
- **Prosepo.Webhooks** - Main webhook service for API endpoints
- **OlmedDataBus.Webhooks.Client** - Client library for webhook consumers
- **OlmedDataBus.Webhooks.TestHost** - Testing infrastructure

## Best Practices

1. **Inherit from DTOModelBase**: All new DTOs should inherit from `DTOModelBase` to ensure string properties are initialized
2. **Use JsonPropertyName**: Always specify JSON property names explicitly
3. **Initialize Collections**: Initialize list/dictionary properties to empty collections to prevent null reference exceptions
4. **Use SpecialPropertyAttribute**: Mark properties that need special processing or grouping
5. **Document Properties**: Add XML documentation comments for complex properties
6. **Nullable Reference Types**: Enable and respect nullable reference type annotations

## Example DTO Creation

```csharp
using System.Text.Json.Serialization;

namespace Prospeo.DTOs
{
    public class MyNewDto : DTOModelBase
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<MyItemDto> Items { get; set; } = new();

        [JsonPropertyName("isActive")]
        [SpecialProperty("Flags")]
        public bool IsActive { get; set; }
    }
}
```

## Version History

- **Current Version**: .NET 9.0
- Supports modern C# features including records, init-only properties, and pattern matching
- Utilizes nullable reference types for improved null safety

## Related Documentation

- [Order Sync API](../Prosepo.Webhooks/README_ORDER_SYNC.md)
- [Stock Management API](../Prosepo.Webhooks/README_STOCKS_API.md)
- [Order and Invoice API](../Prosepo.Webhooks/README_ORDER_INVOICE_API.md)
- [Webhook Integration](../Prosepo.Webhooks/README_QUEUE_INTEGRATION.md)

## Contributing

When adding new DTOs:
1. Inherit from `DTOModelBase`
2. Use `JsonPropertyName` attributes
3. Initialize all reference type properties
4. Add XML documentation comments
5. Follow existing naming conventions
6. Consider using `SpecialPropertyAttribute` where appropriate
7. Add custom converters if special serialization is needed

## License

This library is part of the OlmedDataBus project and follows the project's licensing terms.
