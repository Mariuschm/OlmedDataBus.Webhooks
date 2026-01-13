# ProductsApiClient - Quick Reference

Dedykowany klient API do komunikacji z `ProductsController` w projekcie Prosepo.Webhooks.

## ?? Lokalizacja Dokumentacji

**Pe³na dokumentacja z kodem Ÿród³owym:**
[OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md](OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md)

## ?? Funkcjonalnoœæ

- ? Aktualizacja stanów magazynowych produktów
- ? Aktualizacja œrednich cen zakupu
- ? Aktualizacja pojedynczego produktu
- ? Aktualizacja wielu produktów jednoczeœnie (batch)
- ? Pe³na obs³uga b³êdów
- ? Async/await support
- ? CancellationToken support
- ? Automatyczna serializacja JSON
- ? Typowane odpowiedzi
- ? Walidacja danych po stronie klienta

## ?? Szybki Start

```csharp
// 1. Dodaj using
using YourProject.ProductsClient;
using Prospeo.DTOs.Product;

// 2. Utwórz klienta
using var client = new ProductsApiClient(
    "https://your-api.com", 
    "your-api-key"
);

// 3. U¿yj - Pojedynczy produkt
var result = await client.UpdateSingleProductStockAsync(
    marketplace: "APTEKA_OLMED",
    sku: "PROD-001",
    stock: 150,
    averagePurchasePrice: 25.50m,
    notes: "Aktualizacja po dostawie"
);

if (result.IsSuccess)
    Console.WriteLine($"? Zaktualizowano {result.Data.UpdatedCount} produktów");
else
    Console.WriteLine($"? B³¹d: {result.ErrorMessage}");
```

## ?? Wymagania

- .NET 8.0+
- System.Text.Json >= 8.0.0
- Prospeo.DTOs (project reference)

## ?? G³ówne Metody

### UpdateSingleProductStockAsync
Aktualizacja pojedynczego produktu - najprostsza metoda.

```csharp
await client.UpdateSingleProductStockAsync(
    marketplace: "APTEKA_OLMED",
    sku: "PROD-001",
    stock: 100,
    averagePurchasePrice: 25.00m,
    notes: "Opcjonalna notatka"
);
```

### UpdateMultipleProductStocksAsync
Aktualizacja wielu produktów jednoczeœnie - zalecane dla batch updates.

```csharp
var stockUpdates = new Dictionary<string, (decimal stock, decimal price)>
{
    ["PROD-001"] = (150, 25.50m),
    ["PROD-002"] = (0, 30.00m),
    ["PROD-003"] = (75, 18.99m)
};

await client.UpdateMultipleProductStocksAsync(
    marketplace: "APTEKA_OLMED",
    stockUpdates: stockUpdates,
    notes: "Codzienna synchronizacja"
);
```

### UpdateProductStocksAsync
Pe³na kontrola - przyjmuje ca³y `StockUpdateRequest`.

```csharp
var request = new StockUpdateRequest
{
    Marketplace = "APTEKA_OLMED",
    Skus = new Dictionary<string, StockUpdateItemDto>
    {
        ["SKU001"] = new() { Stock = 100, AveragePurchasePrice = 25.00m },
        ["SKU002"] = new() { Stock = 50, AveragePurchasePrice = 30.00m }
    },
    Notes = "Aktualizacja po inwentaryzacji",
    UpdateDate = DateTime.UtcNow
};

await client.UpdateProductStocksAsync(request);
```

## ?? Walidacja Automatyczna

Klient automatycznie waliduje:
- ? Marketplace nie mo¿e byæ pusty
- ? Lista SKU nie mo¿e byæ pusta  
- ? Stan magazynowy musi byæ >= 0
- ? Cena zakupu musi byæ >= 0

## ?? Autoryzacja

Wymaga nag³ówka **X-API-Key** (dodawany automatycznie).
API Key musi byæ aktywny w tabeli `Firmy` w bazie danych.

## ?? Szczegó³y

Zobacz pe³n¹ dokumentacjê w [ProductsApiClient.README.md](OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md):
- Kompletny kod Ÿród³owy do skopiowania
- Szczegó³owe przyk³ady u¿ycia (8 scenariuszy)
- Instrukcje integracji
- Obs³uga b³êdów
- Testy jednostkowe
- Best practices
- Batch processing
- Retry logic z Polly
- Troubleshooting

## ?? Powi¹zane Dokumenty

- [ProductsController.cs](Prosepo.Webhooks/Controllers/ProductsController.cs) - Implementacja serwera
- [ORDERS_API_CLIENT.md](ORDERS_API_CLIENT.md) - Analogiczny klient dla zamówieñ
- [StockUpdateRequest.cs](Prospeo.DTOs/Product/StockUpdateRequest.cs) - DTO ¿¹dania
- [StockUpdateResponse.cs](Prospeo.DTOs/Product/StockUpdateResponse.cs) - DTO odpowiedzi
