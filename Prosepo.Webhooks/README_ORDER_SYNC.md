# OrderSyncConfigurationService - Dokumentacja

## Opis

`OrderSyncConfigurationService` to serwis zarz¹dzaj¹cy konfiguracj¹ synchronizacji zamówieñ z API Olmed. Zosta³ stworzony na wzór `ProductSyncConfigurationService` i automatycznie zarz¹dza dynamicznymi zakresami dat (dateFrom/dateTo) dla zapytañ o zamówienia.

## G³ówne funkcjonalnoœci

### 1. Dynamiczne generowanie zakresów dat
Serwis automatycznie oblicza zakres dat dla zapytañ:
- **dateTo**: Domyœlnie dzisiejsza data (DateTime.Now.Date) lub wczorajsza data
- **dateFrom**: Automatycznie obliczana jako dateTo minus okreœlona liczba dni (domyœlnie 2 dni)
- **Format daty**: Konfigurowalny (domyœlnie "yyyy-MM-dd")

### 2. Zarz¹dzanie konfiguracjami
- £adowanie konfiguracji z pliku JSON
- Cache z automatycznym odœwie¿aniem co 5 minut
- CRUD operacje na konfiguracjach (Create, Read, Update, Delete)
- Walidacja i logowanie b³êdów

### 3. Integracja z CronScheduler
- Tworzenie obiektów `CronJobRequest` z dynamicznie wygenerowanym body
- Automatyczne dodawanie autoryzacji Olmed (jeœli UseOlmedAuth = true)
- Podgl¹d ¿¹dañ przed wykonaniem (bez wysy³ania)

## Struktura pliku konfiguracyjnego

Plik: `Configuration/order-sync-config.json`

```json
{
  "version": "1.0",
  "lastModified": "2025-01-20T10:00:00Z",
  "configurations": [
    {
      "id": "olmed-sync-orders",
      "name": "Synchronizacja zamówieñ Olmed",
      "description": "Pobieranie zamówieñ z API Olmed co 2 godziny",
      "isActive": true,
      "intervalSeconds": 7200,
      "method": "POST",
      "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
      "useOlmedAuth": true,
      "headers": {
        "accept": "application/json",
        "Content-Type": "application/json",
        "X-CSRF-TOKEN": ""
      },
      "marketplace": "APTEKA_OLMED",
      "dateRangeDays": 2,
      "useCurrentDateAsEndDate": true,
      "dateFormat": "yyyy-MM-dd",
      "additionalParameters": {}
    }
  ]
}
```

## Parametry konfiguracji

| Parametr | Typ | Domyœlna wartoœæ | Opis |
|----------|-----|------------------|------|
| `id` | string | - | Unikalny identyfikator konfiguracji (u¿ywany jako JobId) |
| `name` | string | - | Nazwa opisowa konfiguracji |
| `description` | string | - | Szczegó³owy opis konfiguracji |
| `isActive` | bool | true | Czy konfiguracja jest aktywna |
| `intervalSeconds` | int | 7200 | Interwa³ wykonywania w sekundach (7200 = 2 godziny) |
| `method` | string | "POST" | Metoda HTTP ¿¹dania |
| `url` | string | - | URL endpointu API |
| `useOlmedAuth` | bool | true | Czy u¿ywaæ automatycznej autoryzacji Olmed |
| `headers` | Dictionary | - | Nag³ówki HTTP ¿¹dania |
| `marketplace` | string | - | Marketplace (np. "APTEKA_OLMED") |
| `dateRangeDays` | int | 2 | Liczba dni zakresu (dateFrom = dateTo - dateRangeDays) |
| `useCurrentDateAsEndDate` | bool | true | Czy u¿yæ dzisiejszej daty jako dateTo (false = wczorajsza data) |
| `dateFormat` | string | "yyyy-MM-dd" | Format daty w ¿¹daniu |
| `additionalParameters` | Dictionary | - | Dodatkowe parametry dla ¿¹dania |

## Przyk³ady u¿ycia

### 1. Pobranie wszystkich aktywnych konfiguracji

```csharp
public class OrderSyncService
{
    private readonly OrderSyncConfigurationService _configService;
    
    public OrderSyncService(OrderSyncConfigurationService configService)
    {
        _configService = configService;
    }
    
    public async Task SyncAllOrders()
    {
        var configurations = await _configService.GetActiveConfigurationsAsync();
        
        foreach (var config in configurations)
        {
            var request = _configService.CreateCronJobRequest(config);
            // Wykonaj ¿¹danie...
        }
    }
}
```

### 2. Generowanie body ¿¹dania

```csharp
var config = await _configService.GetConfigurationByIdAsync("olmed-sync-orders");
if (config != null)
{
    var body = _configService.GenerateRequestBody(config);
    // Body zawiera: {"marketplace":"APTEKA_OLMED","dateFrom":"2025-01-18","dateTo":"2025-01-20"}
}
```

### 3. Podgl¹d ¿¹dania przed wykonaniem

```csharp
var preview = await _configService.GetRequestPreviewAsync("olmed-sync-orders");
if (preview != null)
{
    // Wyœwietl szczegó³y ¿¹dania bez wysy³ania go
    Console.WriteLine($"URL: {preview.Url}");
    Console.WriteLine($"Body: {preview.Body}");
}
```

### 4. Dodawanie/aktualizacja konfiguracji

```csharp
var newConfig = new OrderSyncConfiguration
{
    Id = "custom-sync-orders",
    Name = "Niestandardowa synchronizacja",
    IsActive = true,
    IntervalSeconds = 3600, // 1 godzina
    Url = "https://api.example.com/orders",
    Marketplace = "CUSTOM_MARKETPLACE",
    DateRangeDays = 7, // 7 dni wstecz
    UseCurrentDateAsEndDate = false // U¿yj wczorajszej daty
};

var success = await _configService.SaveConfigurationAsync(newConfig);
```

### 5. Usuwanie konfiguracji

```csharp
var deleted = await _configService.DeleteConfigurationAsync("custom-sync-orders");
if (deleted)
{
    Console.WriteLine("Konfiguracja zosta³a usuniêta");
}
```

## Przyk³adowe ¿¹danie generowane przez serwis

Dla dzisiejszej daty **2025-01-20** i konfiguracji z `dateRangeDays = 2`:

### ¯¹danie HTTP:
```http
POST https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders
Content-Type: application/json
Authorization: Bearer {token}
accept: application/json
X-CSRF-TOKEN: 

{
  "marketplace": "APTEKA_OLMED",
  "dateFrom": "2025-01-18",
  "dateTo": "2025-01-20"
}
```

## Konfiguracja w appsettings.json

Mo¿na nadpisaæ domyœln¹ œcie¿kê pliku konfiguracyjnego:

```json
{
  "OrderSync": {
    "ConfigurationFile": "C:\\CustomPath\\order-sync-config.json"
  }
}
```

Jeœli nie podano œcie¿ki, domyœlnie u¿ywana jest: `{CurrentDirectory}/Configuration/order-sync-config.json`

## Logowanie

Serwis loguje nastêpuj¹ce zdarzenia:
- ? Za³adowanie konfiguracji (INFO)
- ?? B³êdy deserializacji (WARNING)
- ? B³êdy I/O i inne wyj¹tki (ERROR)
- ?? Szczegó³y generowania body ¿¹dania (DEBUG)

## Integracja z CronController

Przyk³ad u¿ycia w kontrolerze:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderSyncController : ControllerBase
{
    private readonly OrderSyncConfigurationService _configService;
    private readonly CronSchedulerService _schedulerService;
    
    [HttpGet("preview/{configId}")]
    public async Task<IActionResult> GetRequestPreview(string configId)
    {
        var preview = await _configService.GetRequestPreviewAsync(configId);
        return preview != null ? Ok(preview) : NotFound();
    }
    
    [HttpPost("execute/{configId}")]
    public async Task<IActionResult> ExecuteSync(string configId)
    {
        var config = await _configService.GetConfigurationByIdAsync(configId);
        if (config == null) return NotFound();
        
        var request = _configService.CreateCronJobRequest(config);
        var response = await _schedulerService.ExecuteJobAsync(request);
        
        return Ok(response);
    }
}
```

## Ró¿nice wzglêdem ProductSyncConfigurationService

| Aspekt | ProductSync | OrderSync |
|--------|-------------|-----------|
| Body ¿¹dania | Statyczne (tylko marketplace) | Dynamiczne (marketplace + dateFrom + dateTo) |
| Zakres dat | Nie dotyczy | Automatycznie obliczany |
| Format daty | Nie dotyczy | Konfigurowalny |
| Endpoint | /products/get-products | /orders/get-orders |
| Plik konfiguracji | product-sync-config.json | order-sync-config.json |

## Najlepsze praktyki

1. **Interwa³ synchronizacji**: Zalecane 1-2 godziny (3600-7200 sekund)
2. **Zakres dat**: 2-7 dni dla optymalnej wydajnoœci
3. **Format daty**: U¿ywaj "yyyy-MM-dd" dla API Olmed
4. **Monitoring**: Regularnie sprawdzaj logi pod k¹tem b³êdów
5. **Cache**: Odœwie¿aj cache po zmianach w pliku konfiguracyjnym

## Rozwi¹zywanie problemów

### Problem: Brak zamówieñ w odpowiedzi
**Rozwi¹zanie**: SprawdŸ czy zakres dat jest poprawny. Zwiêksz `dateRangeDays` lub zmieñ `useCurrentDateAsEndDate` na false.

### Problem: B³¹d autoryzacji
**Rozwi¹zanie**: Upewnij siê ¿e `useOlmedAuth = true` i token jest aktualny w CronSchedulerService.

### Problem: Nieprawid³owy format daty
**Rozwi¹zanie**: SprawdŸ parametr `dateFormat`. API Olmed oczekuje formatu "yyyy-MM-dd".

### Problem: Cache nie odœwie¿a siê
**Rozwi¹zanie**: Wywo³aj `RefreshCache()` lub poczekaj 5 minut (automatyczne odœwie¿enie).

## Wsparcie

W przypadku pytañ lub problemów:
- SprawdŸ logi aplikacji w katalogu `Logs`
- U¿yj endpointu `/preview/{configId}` do weryfikacji ¿¹dania
- Skonsultuj siê z dokumentacj¹ API Olmed
