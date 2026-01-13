# OlmedDataBus.Webhooks.Client

## ⚠️ WAŻNE: Bezpieczeństwo Konfiguracji

**PRZED pierwszym uruchomieniem** musisz skonfigurować wrażliwe dane (hasła, klucze):

### 🚀 Szybki Start dla Deweloperów

```powershell
# 1. Sklonuj repozytorium
git clone https://192.168.88.204/Prospeo/26161_Webbhook
cd OlmedDataBus

# 2. Skonfiguruj User Secrets (WYMAGANE!)
.\setup-user-secrets.ps1

# 3. Skonfiguruj API Keys dla kontrolerów Order/Invoice (opcjonalne)
# Uruchom skrypt SQL: setup-api-keys.sql

# 4. Uruchom aplikację
cd Prosepo.Webhooks
dotnet run
```

📖 **Szczegółowa dokumentacja bezpieczeństwa:** [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md)  
📖 **Przewodnik Quick Start:** [QUICK-START.md](QUICK-START.md)  
📖 **Dokumentacja API Order/Invoice:** [README_ORDER_INVOICE_API.md](Prosepo.Webhooks/README_ORDER_INVOICE_API.md)

---

## Opis

`OlmedDataBus.Webhooks.Client` to biblioteka .NET umożliwiająca bezpieczną obsługę webhooków pochodzących z szyny danych OLMED. Umożliwia ona:
- weryfikację podpisu HMAC SHA256 przesyłanych danych,
- odszyfrowanie zaszyfrowanego payloadu (AES-256-CBC),
- prostą integrację z aplikacjami .NET (np. ASP.NET Core, Windows Service, itp.).
- **NOWE:** kontrolery Order, Invoice i Products z autentykacją API Key do komunikacji z systemem Olmed
- **NOWE:** dedykowane klasy klientów API do użycia w innych projektach

---

## 🆕 API Endpoints

### OrdersController
- **POST** `/api/orders/update-status` - Aktualizacja statusu zamówienia
- **POST** `/api/orders/upload-order-realization-result` - Przesłanie wyników realizacji
- **GET** `/api/orders/authenticated-firma` - Weryfikacja API Key
- **Autoryzacja:** API Key (nagłówek X-API-Key)

### ProductsController
- **POST** `/api/products/update-product-stocks` - Aktualizacja stanów magazynowych
- **Autoryzacja:** API Key (nagłówek X-API-Key)

### InvoiceController
- **POST** `/api/invoice/sent` - Zgłoszenie wysłania faktury
- **Autoryzacja:** API Key (nagłówek X-API-Key)

📖 **Pełna dokumentacja API:** [README_ORDER_INVOICE_API.md](Prosepo.Webhooks/README_ORDER_INVOICE_API.md)

---

## 🚀 Klienty API dla Zewnętrznych Projektów

### OrdersApiClient
Dedykowany klient do komunikacji z OrdersController.

**Quick Start:**
```csharp
using var client = new OrdersApiClient("https://api.com", "api-key");

var result = await client.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
{
    Marketplace = "APTEKA_OLMED",
    OrderNumber = "ORD/2024/01/0001",
    Status = "1"
});

if (result.IsSuccess)
    Console.WriteLine($"✓ Status: {result.Data.NewStatus}");
```

📖 **Pełna dokumentacja:** [ORDERS_API_CLIENT.md](ORDERS_API_CLIENT.md)  
📖 **Kod źródłowy i przykłady:** [OrdersApiClient.README.md](OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md)

### ProductsApiClient
Dedykowany klient do komunikacji z ProductsController.

**Quick Start:**
```csharp
using var client = new ProductsApiClient("https://api.com", "api-key");

var result = await client.UpdateSingleProductStockAsync(
    marketplace: "APTEKA_OLMED",
    sku: "PROD-001",
    stock: 150,
    averagePurchasePrice: 25.50m
);

if (result.IsSuccess)
    Console.WriteLine($"✓ Zaktualizowano {result.Data.UpdatedCount} produktów");
```

📖 **Pełna dokumentacja:** [PRODUCTS_API_CLIENT.md](PRODUCTS_API_CLIENT.md)  
📖 **Kod źródłowy i przykłady:** [ProductsApiClient.README.md](OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md)

---

## Przykładowe użycie (na podstawie projektu TestHost)

Poniżej znajduje się uproszczony przykład użycia biblioteki w projekcie ASP.NET Core Web API (`OlmedDataBus.Webhooks.TestHost`):

```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly SecureWebhookHelper _helper;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IConfiguration configuration, ILogger<WebhookController> logger)
    {
        var encryptionKey = configuration["OlmedDataBus:WebhookKeys:EncryptionKey"] ?? string.Empty;
        var hmacKey = configuration["OlmedDataBus:WebhookKeys:HmacKey"] ?? string.Empty;
        _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Receive(
        [FromBody] WebhookPayload payload,
        [FromHeader(Name = "X-OLMED-ERP-API-SIGNATURE")] string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest("Brak nagłówka X-OLMED-ERP-API-SIGNATURE.");

        if (_helper.TryDecryptAndVerifyWithIvPrefix(payload.guid, payload.webhookType, payload.webhookData, signature, out var json))
        {
            // Zamiana odszyfrowanego JSON na obiekt
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            _logger.LogInformation("Odszyfrowana zawartość webhooka: {Decrypted}", json);
            return Ok(new { Success = true, Decrypted = obj });
        }
        return BadRequest("Invalid signature or decryption failed.");
    }
}

public class WebhookPayload
{
    public string guid { get; set; } = string.Empty;
    public string webhookType { get; set; } = string.Empty;
    public string webhookData { get; set; } = string.Empty;
}
```

---

## Testowanie

```powershell
# Przetestuj wszystkie endpointy API:
.\test-order-invoice-api.ps1

# Test kontrolera Order
curl -X POST "https://localhost:7208/api/orders/update-status" `
  -H "X-API-Key: your-api-key" `
  -H "Content-Type: application/json" `
  -d '{"marketplace":"APTEKA_OLMED","orderNumber":"ORD-001","status":"1"}'

# Test kontrolera Products
curl -X POST "https://localhost:7208/api/products/update-product-stocks" `
  -H "X-API-Key: your-api-key" `
  -H "Content-Type: application/json" `
  -d '{"marketplace":"APTEKA_OLMED","skus":{"SKU001":{"stock":100,"average_purchase_price":25.50}}}'
```

---

## Instrukcja dołączenia biblioteki do projektu

### 1. Dodanie przez NuGet (zalecane, jeśli biblioteka jest publikowana)

W Menedżerze pakietów NuGet wyszukaj `OlmedDataBus.Webhooks.Client` i zainstaluj do swojego projektu.

**lub** przez konsolę:

```
dotnet add package OlmedDataBus.Webhooks.Client
```

### 2. Ręczne dołączenie pliku DLL

Jeśli posiadasz tylko plik `OlmedDataBus.Webhooks.Client.dll`:

1. Skopiuj plik DLL do katalogu swojego projektu (np. do folderu `libs`).
2. Kliknij prawym przyciskiem myszy na projekt w Visual Studio → **Dodaj** → **Odwołanie...**.
3. Wybierz **Przeglądaj** i wskaż plik DLL.
4. Zatwierdź dodanie odwołania.

### 3. Użycie klientów API w zewnętrznych projektach

Skopiuj kod źródłowy klientów API do swojego projektu:
- [OrdersApiClient.README.md](OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md) - kod i dokumentacja
- [ProductsApiClient.README.md](OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md) - kod i dokumentacja

Wymagane zależności:
- System.Text.Json >= 8.0.0
- Prospeo.DTOs (project reference)
- .NET 8.0+

---

## Konfiguracja kluczy

W pliku `appsettings.json` lub innym źródle konfiguracji należy umieścić klucze:

```json
{
  "OlmedDataBus:WebhookKeys": {
    "EncryptionKey": "mysecretkey1234567890123456mysecretkey12",
    "HmacKey": "mysecretkey1234567890123456mysecretkey12"
  }
}
```

Klucze powinny być przekazane jako tekst o długości 32 znaków (256 bitów - 32 bajty).

---

## 📚 Dokumentacja

### Bezpieczeństwo i Konfiguracja
- [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md) - Szczegółowa konfiguracja bezpieczeństwa
- [QUICK-START.md](QUICK-START.md) - Szybki start dla deweloperów

### API i Integracja
- [README_ORDER_INVOICE_API.md](Prosepo.Webhooks/README_ORDER_INVOICE_API.md) - Dokumentacja API
- [ORDERS_API_CLIENT.md](ORDERS_API_CLIENT.md) - Klient API dla zamówień
- [PRODUCTS_API_CLIENT.md](PRODUCTS_API_CLIENT.md) - Klient API dla produktów

### Szczegółowe Dokumentacje Klientów
- [OrdersApiClient.README.md](OlmedDataBus.Webhooks.Client/OrdersApiClient.README.md) - Kod źródłowy i przykłady
- [ProductsApiClient.README.md](OlmedDataBus.Webhooks.Client/ProductsApiClient.README.md) - Kod źródłowy i przykłady

### Integracja z Kolejką
- [README_QUEUE_INTEGRATION.md](Prosepo.Webhooks/README_QUEUE_INTEGRATION.md) - Integracja z systemem kolejek

### Synchronizacja Zamówień
- [README_ORDER_SYNC.md](Prosepo.Webhooks/README_ORDER_SYNC.md) - Konfiguracja synchronizacji
- [ORDERSYNC_SUMMARY.md](Prosepo.Webhooks/ORDERSYNC_SUMMARY.md) - Podsumowanie implementacji

### Deployment
- [IIS-DEPLOYMENT.md](IIS-DEPLOYMENT.md) - Wdrożenie na IIS
- [CI-CD-README.md](CI-CD-README.md) - Automatyzacja CI/CD

---

## Wymagania

- .NET Standard 2.0 (kompatybilność z .NET Core 2.0+, .NET Framework 4.6.1+, .NET 5/6/7/8)
- Do testowania: ASP.NET Core Web API (np. .NET 8/9)
- Dla klientów API: .NET 8.0+ (zalecane)

---

## Licencja

Biblioteka przeznaczona do użytku partnerów Grupy OLMED.