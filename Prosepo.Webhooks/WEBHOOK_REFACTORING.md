# Refaktoryzacja WebhookController - Dokumentacja

## Przegl¹d

WebhookController zosta³ zrefaktoryzowany z wykorzystaniem wzorców projektowych **Strategy Pattern** oraz **Chain of Responsibility** w celu poprawy czytelnoœci, ³atwoœci utrzymania i rozszerzalnoœci kodu.

## Problemy w oryginalnym kodzie

1. **Naruszenie Single Responsibility Principle** - Controller robi³ zbyt du¿o:
   - Odbiera³ webhooki
   - Deszyfrowa³ dane
   - Parsowa³ JSON
   - Decydowa³ o routingu
   - Zarz¹dza³ kolejk¹
   - Logowa³ do plików

2. **Duplikacja kodu** - Metody dodawania do kolejki mia³y bardzo podobn¹ strukturê
3. **Niska testowalnoœæ** - Trudne testowanie ze wzglêdu na z³o¿onoœæ
4. **Trudnoœæ w dodawaniu nowych typów webhooków

## Rozwi¹zanie - Zastosowane wzorce

### 1. Strategy Pattern

Ka¿dy typ webhook ma dedykowan¹ strategiê przetwarzania:

- **ProductWebhookStrategy** - obs³uguje dane produktów
- **OrderWebhookStrategy** - obs³uguje dane zamówieñ  
- **UnknownWebhookStrategy** - obs³uguje nierozpoznane dane

#### Interfejs strategii

```csharp
public interface IWebhookProcessingStrategy
{
    bool CanProcess(WebhookProcessingContext context);
    Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context);
    string StrategyName { get; }
}
```

### 2. Chain of Responsibility

**WebhookDataParser** implementuje ³añcuch odpowiedzialnoœci do parsowania danych:

1. SprawdŸ czy zawiera zagnie¿d¿one `productData`
2. SprawdŸ czy zawiera zagnie¿d¿one `orderData`
3. Spróbuj deserializowaæ jako ProductDto na podstawie `webhookType`
4. Spróbuj deserializowaæ jako OrderDto na podstawie `webhookType`
5. Fallback - spróbuj jako ProductDto
6. Fallback - spróbuj jako OrderDto

### 3. Orchestrator Pattern

**WebhookProcessingOrchestrator** koordynuje proces przetwarzania:

```csharp
public interface IWebhookProcessingOrchestrator
{
    Task<WebhookProcessingResult> ProcessWebhookAsync(WebhookProcessingContext context);
}
```

## Struktura katalogów

```
Prosepo.Webhooks/
??? Services/
?   ??? Webhook/
?       ??? IWebhookDataParser.cs
?       ??? WebhookDataParser.cs
?       ??? IWebhookProcessingStrategy.cs
?       ??? WebhookProcessingOrchestrator.cs
?       ??? Strategies/
?           ??? ProductWebhookStrategy.cs
?           ??? OrderWebhookStrategy.cs
?           ??? UnknownWebhookStrategy.cs
??? Controllers/
    ??? WebhookController.cs (zrefaktoryzowany)
```

## Przep³yw przetwarzania

```
1. WebhookController.Receive()
   ?
2. Deszyfracja i weryfikacja (SecureWebhookHelper)
   ?
3. ProcessWebhookDataAsync() - zwraca bool (success/failure)
   ?
4. WebhookDataParser.ParseAsync() - identyfikuje typ danych
   ?
5. Tworzenie WebhookProcessingContext
   ?
6. WebhookProcessingOrchestrator.ProcessWebhookAsync()
   ?
7. Wybór odpowiedniej strategii (ProductWebhookStrategy/OrderWebhookStrategy/UnknownWebhookStrategy)
   ?
8. Strategia przetwarza dane i dodaje do Queue
   ?
9. Zwracany jest WebhookProcessingResult (Success: true/false)
   ?
10. Kontroler zwraca odpowiedni kod HTTP:
    - 200 OK - przetwarzanie zakoñczone sukcesem
    - 400 Bad Request - b³¹d podczas przetwarzania
```

## Kody odpowiedzi HTTP

### 200 OK
Webhook zosta³ pomyœlnie odebrany, zdeszyfrowany i przetworzony. Dane zosta³y dodane do kolejki.

**Przyk³adowa odpowiedŸ:**
```json
{
  "success": true,
  "message": "Webhook przetworzony pomyœlnie",
  "guid": "672644b8d5983dd121a053ff0a3adbfa4da67a676772b37edec29d4e825f5d66"
}
```

### 400 Bad Request
Wyst¹pi³ b³¹d podczas przetwarzania webhook. Mo¿liwe przyczyny:
- Brak nag³ówka `X-OLMED-ERP-API-SIGNATURE`
- Nieprawid³owy podpis HMAC
- B³¹d deszyfracji danych
- **B³¹d podczas parsowania danych**
- **B³¹d podczas dodawania do kolejki**
- **Wyj¹tek podczas przetwarzania strategii**

**Przyk³adowe odpowiedzi b³êdów:**

Brak nag³ówka:
```json
"Brak nag³ówka X-OLMED-ERP-API-SIGNATURE"
```

B³¹d deszyfracji:
```json
"Nie uda³o siê zweryfikowaæ lub odszyfrowaæ webhook"
```

B³¹d przetwarzania (nowy):
```json
{
  "success": false,
  "error": "B³¹d podczas przetwarzania webhook",
  "message": "Nie uda³o siê przetworzyæ danych webhook",
  "guid": "672644b8d5983dd121a053ff0a3adbfa4da67a676772b37edec29d4e825f5d66"
}
```

B³¹d wyj¹tku (nowy):
```json
{
  "success": false,
  "error": "B³¹d podczas przetwarzania webhook",
  "message": "Reflection-based serialization has been disabled...",
  "guid": "672644b8d5983dd121a053ff0a3adbfa4da67a676772b37edec29d4e825f5d66"
}
```

## Obs³uga b³êdów

### Poziom kontrolera
WebhookController teraz **zawsze zwraca odpowiedni kod HTTP**:
- ? Sukces ? `200 OK` z informacj¹ o sukcesie
- ? B³¹d ? `400 Bad Request` z szczegó³ami b³êdu

### Poziom orchestratora
`ProcessWebhookDataAsync()` zwraca `bool`:
- `true` - przetwarzanie zakoñczone sukcesem
- `false` - przetwarzanie zakoñczone niepowodzeniem

### Poziom strategii
Ka¿da strategia zwraca `WebhookProcessingResult`:
```csharp
public class WebhookProcessingResult
{
    public bool Success { get; set; }
    public List<Queue> CreatedQueueItems { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Przep³yw b³êdów

```
B³¹d w strategii
  ?
WebhookProcessingResult.Success = false
  ?
ProcessWebhookDataAsync() zwraca false
  ?
Receive() zwraca BadRequest(400)
  ?
Klient otrzymuje kod 400 z opisem b³êdu
```

## .NET 9 Kompatybilnoœæ - JSON Serialization

### Problem
W .NET 9 reflection-based JSON serialization jest domyœlnie wy³¹czona ze wzglêdów wydajnoœciowych i bezpieczeñstwa.

### Rozwi¹zanie
Wszystkie serwisy u¿ywaj¹ce `JsonSerializer` zosta³y zaktualizowane aby u¿ywaæ `DefaultJsonTypeInfoResolver`:

```csharp
private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(), // W³¹cz reflection
    Converters = { new CustomDateTimeConverter() }
};
```

**Lokalizacje konfiguracji:**
- `Program.cs` - globalna konfiguracja kontrolerów
- `WebhookDataParser` - parsowanie przychodz¹cych danych
- `ProductWebhookStrategy` - serializacja ProductDto
- `OrderWebhookStrategy` - serializacja OrderDto

## Korzyœci refaktoryzacji

### 1. **Single Responsibility**
Ka¿da klasa ma jedn¹, dobrze zdefiniowan¹ odpowiedzialnoœæ:
- `WebhookController` - odbiera HTTP requesty
- `WebhookDataParser` - parsuje JSON
- `Strategia` - przetwarza konkretny typ danych
- `Orchestrator` - koordynuje proces

### 2. **Open/Closed Principle**
Dodanie nowego typu webhook wymaga tylko:
- Stworzenia nowej strategii implementuj¹cej `IWebhookProcessingStrategy`
- Zarejestrowania jej w DI

**Przyk³ad dodania nowego typu:**

```csharp
// 1. Stwórz now¹ strategiê
public class InvoiceWebhookStrategy : IWebhookProcessingStrategy
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    public InvoiceWebhookStrategy(...)
    {
        // Konfiguracja dla .NET 9
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    public bool CanProcess(WebhookProcessingContext context)
    {
        return context.ParseResult.InvoiceData != null;
    }

    public async Task<WebhookProcessingResult> ProcessAsync(WebhookProcessingContext context)
    {
        // Implementacja z u¿yciem _jsonOptions
    }

    public string StrategyName => "InvoiceWebhook";
}

// 2. Zarejestruj w Program.cs
builder.Services.AddScoped<IWebhookProcessingStrategy, InvoiceWebhookStrategy>();
```

### 3. **Testowalnoœæ**
Ka¿dy komponent mo¿na testowaæ niezale¿nie:

```csharp
// Test parsera
var parser = new WebhookDataParser(loggerMock);
var result = await parser.ParseAsync(jsonData, "product.updated");
Assert.NotNull(result.ProductData);

// Test strategii
var strategy = new ProductWebhookStrategy(queueServiceMock, ...);
var context = new WebhookProcessingContext { ... };
var result = await strategy.ProcessAsync(context);
Assert.True(result.Success);
```

### 4. **Redukcja duplikacji**
Wspólna logika (pobieranie nazwy firmy, tworzenie Queue items) jest scentralizowana w strategiach.

### 5. **Lepsze logowanie**
Ka¿dy krok procesu jest logowany z odpowiednim kontekstem.

## Konfiguracja w Program.cs

```csharp
// Rejestracja webhook processing services
builder.Services.AddScoped<IWebhookDataParser, WebhookDataParser>();
builder.Services.AddScoped<IWebhookProcessingOrchestrator, WebhookProcessingOrchestrator>();

// Rejestracja strategii przetwarzania webhooków
builder.Services.AddScoped<IWebhookProcessingStrategy, ProductWebhookStrategy>();
builder.Services.AddScoped<IWebhookProcessingStrategy, OrderWebhookStrategy>();
builder.Services.AddScoped<IWebhookProcessingStrategy, UnknownWebhookStrategy>();

// Globalna konfiguracja JSON dla kontrolerów (.NET 9)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
```

## Modele danych

### WebhookParseResult
Wynik parsowania danych webhook.

```csharp
public class WebhookParseResult
{
    public ProductDto? ProductData { get; set; }
    public OrderDto? OrderData { get; set; }
    public string? ChangeType { get; set; }
    public bool IsRecognized => ProductData != null || OrderData != null;
}
```

### WebhookProcessingContext
Kontekst zawieraj¹cy wszystkie dane potrzebne do przetworzenia webhook.

```csharp
public class WebhookProcessingContext
{
    public string Guid { get; set; }
    public string WebhookType { get; set; }
    public string? ChangeType { get; set; }
    public string DecryptedJson { get; set; }
    public int DefaultFirmaId { get; set; }
    public int SecondFirmaId { get; set; }
    public WebhookParseResult ParseResult { get; set; }
}
```

### WebhookProcessingResult
Wynik przetwarzania webhook.

```csharp
public class WebhookProcessingResult
{
    public bool Success { get; set; }
    public List<Queue> CreatedQueueItems { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## Routing logic

### Produkty
Dodawane s¹ do **obu** firm:
- `DefaultFirmaId` (Olmed)
- `SecondFirmaId` (Zawisza)

### Zamówienia
Routing na podstawie `marketplace`:
- Jeœli `marketplace` zawiera "ZAWISZA" ? `SecondFirmaId`
- W przeciwnym razie ? `DefaultFirmaId`

### Nieznane dane
Dodawane do `DefaultFirmaId` z:
- `Scope = -1`
- `Flg = -1`
- `Request = string.Empty`

## Rozszerzalnoœæ

### Dodanie nowego typu danych

1. **Rozszerz WebhookParseResult:**
```csharp
public class WebhookParseResult
{
    // ...existing properties...
    public InvoiceDto? InvoiceData { get; set; }
    public bool IsRecognized => ProductData != null || OrderData != null || InvoiceData != null;
}
```

2. **Dodaj parsowanie w WebhookDataParser:**
```csharp
if (root.TryGetProperty("invoiceData", out var invoiceDataElement))
{
    result.InvoiceData = JsonSerializer.Deserialize<InvoiceDto>(invoiceDataElement.GetRawText(), _jsonOptions);
    return result;
}
```

3. **Stwórz now¹ strategiê** (patrz przyk³ad wy¿ej - pamiêtaj o `JsonSerializerOptions`)

4. **Zarejestruj w DI**

### Dodanie nowej logiki routingu

Wystarczy zmodyfikowaæ metodê w odpowiedniej strategii:

```csharp
private int DetermineTargetCompanyId(OrderDto orderData, WebhookProcessingContext context)
{
    // Mo¿esz dodaæ bardziej z³o¿on¹ logikê
    if (orderData.Type == 3) // Premium orders
        return context.DefaultFirmaId;
    
    if (orderData.Marketplace.Contains("ZAWISZA"))
        return context.SecondFirmaId;
    
    return context.DefaultFirmaId;
}
```

## Migracja z poprzedniej wersji

Refaktoryzacja jest **w pe³ni kompatybilna wstecz**:
- API endpoints pozostaj¹ niezmienione
- Logika biznesowa pozostaje taka sama
- Format danych w Queue pozostaje taki sam
- Tylko wewnêtrzna implementacja zosta³a zmieniona

## Troubleshooting

### B³¹d: "Reflection-based serialization has been disabled"

**Przyczyna:** Brak konfiguracji `TypeInfoResolver` w `JsonSerializerOptions`.

**Rozwi¹zanie:** Upewnij siê ¿e wszystkie miejsca u¿ywaj¹ce `JsonSerializer` maj¹:
```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};
```

### B³¹d deserializacji DateTime

**Przyczyna:** Niestandardowy format daty z API Olmed.

**Rozwi¹zanie:** U¿ywany jest `CustomDateTimeConverter` w `WebhookDataParser`:
```csharp
Converters = { new CustomDateTimeConverter() }
```

## Testy jednostkowe (przyk³ad)

```csharp
public class ProductWebhookStrategyTests
{
    [Fact]
    public async Task ProcessAsync_ShouldAddProductToTwoCompanies()
    {
        // Arrange
        var queueServiceMock = new Mock<IQueueService>();
        var strategy = new ProductWebhookStrategy(queueServiceMock.Object, ...);
        var context = new WebhookProcessingContext
        {
            ParseResult = new WebhookParseResult
            {
                ProductData = new ProductDto { Sku = "TEST123" }
            },
            DefaultFirmaId = 1,
            SecondFirmaId = 2
        };

        // Act
        var result = await strategy.ProcessAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.CreatedQueueItems.Count);
        queueServiceMock.Verify(x => x.AddAsync(It.IsAny<Queue>()), Times.Exactly(2));
    }
}
```

## Podsumowanie

Refaktoryzacja WebhookController znacz¹co poprawi³a:
- ? **Czytelnoœæ kodu** - ka¿da klasa ma jasno okreœlon¹ odpowiedzialnoœæ
- ? **Testowalnoœæ** - komponenty mo¿na testowaæ niezale¿nie
- ? **Rozszerzalnoœæ** - ³atwe dodawanie nowych typów webhooków
- ? **Utrzymanie** - ³atwiejsze wprowadzanie zmian
- ? **Zgodnoœæ z SOLID** - kod przestrzega zasad SOLID
- ? **.NET 9 Kompatybilnoœæ** - prawid³owa konfiguracja JSON serialization

Kod jest teraz gotowy na przysz³e rozszerzenia bez koniecznoœci modyfikowania istniej¹cych klas.
