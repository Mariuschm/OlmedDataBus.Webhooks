# OrdersApiClient - Quick Reference

Dedykowany klient API do komunikacji z `OrdersController` w projekcie Prosepo.Webhooks.

## ?? Lokalizacja Dokumentacji

**Pe³na dokumentacja z kodem Ÿród³owym:**
[OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md](OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md)

## ?? Funkcjonalnoœæ

- ? Aktualizacja statusów zamówieñ
- ? Przesy³anie wyników realizacji zamówieñ  
- ? Weryfikacja autoryzacji API Key
- ? Pe³na obs³uga b³êdów
- ? Async/await support
- ? CancellationToken support
- ? Automatyczna serializacja JSON
- ? Typowane odpowiedzi

## ?? Szybki Start

```csharp
// 1. Dodaj using
using YourProject.OrdersClient;
using Prospeo.DTOs.Product;
using Prospeo.DTOs.Order;

// 2. Utwórz klienta
using var client = new OrdersApiClient(
    "https://your-api.com", 
    "your-api-key"
);

// 3. U¿yj
var result = await client.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
{
    Marketplace = "APTEKA_OLMED",
    OrderNumber = "ORD/2024/01/0001",
    Status = "1"
});

if (result.IsSuccess)
    Console.WriteLine($"? Status: {result.Data.NewStatus}");
else
    Console.WriteLine($"? B³¹d: {result.ErrorMessage}");
```

## ?? Wymagania

- .NET 8.0+
- System.Text.Json >= 8.0.0
- Prospeo.DTOs (project reference)

## ?? Szczegó³y

Zobacz pe³n¹ dokumentacjê w [OrdersApiClient.README.md](OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md):
- Kompletny kod Ÿród³owy do skopiowania
- Szczegó³owe przyk³ady u¿ycia
- Instrukcje integracji
- Obs³uga b³êdów
- Testy jednostkowe
- Troubleshooting

## ?? Powi¹zane Dokumenty

- [README_ORDER_INVOICE_API.md](Prosepo.Webhooks/README_ORDER_INVOICE_API.md) - Dokumentacja API
- [OrdersController.cs](Prosepo.Webhooks/Controllers/OrdersController.cs) - Implementacja serwera
