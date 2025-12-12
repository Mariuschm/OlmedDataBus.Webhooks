# Prospeo.DbContext

Biblioteka Entity Framework Core dla systemu Prospeo zawieraj¹ca modele, konteksty bazy danych i serwisy dla obs³ugi danych.

## Funkcje

- ? **Model Firmy** - kompletny model dla tabeli `ProRWS.Firmy`
- ? **Model QueueStatus** - model dla tabeli `ProRWS.QueueStatus`
- ? **Model Queue** - model dla tabeli `ProRWS.Queue` z relacj¹ do Firmy
- ? **Entity Framework Core** - pe³ne wsparcie dla SQL Server
- ? **Serwisy biznesowe** - `FirmyService`, `QueueStatusService` i `QueueService` z pe³nym API CRUD
- ? **Relacje miêdzy tabelami** - Foreign Keys i nawigacyjne w³aœciwoœci
- ? **DTOs** - gotowe DTOs dla API i transferu danych
- ? **Walidacja** - automatyczna walidacja unikalnoœci kluczy
- ? **Logowanie** - kompleksowe logowanie operacji
- ? **Rozszerzenia DI** - ³atwa konfiguracja w aplikacji

## Instalacja

Dodaj referencjê do projektu lub skompiluj jako bibliotekê DLL.

## Konfiguracja

### 1. Connection String

W `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ProspeoDb;Schema=ProRWS;Trusted_Connection=true;"
  }
}
```

### 2. Rejestracja serwisów

W `Program.cs`:

```csharp
using Prospeo.DbContext.Extensions;

// Zalecany sposób - wszystkie serwisy
builder.Services.AddProspeoServices(builder.Configuration);

// Lub tylko DbContext
builder.Services.AddProspeoDbContext(builder.Configuration);

// Z niestandardowym connection stringiem
builder.Services.AddProspeoServices("connection-string");
```

## U¿ycie

### Podstawowe operacje CRUD dla Firm

```csharp
public class FirmyController : ControllerBase
{
    private readonly IFirmyService _firmyService;

    public FirmyController(IFirmyService firmyService)
    {
        _firmyService = firmyService;
    }

    [HttpGet]
    public async Task<IEnumerable<Firmy>> GetAll()
    {
        return await _firmyService.GetAllAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Firmy>> GetById(int id)
    {
        var firma = await _firmyService.GetByIdAsync(id);
        return firma == null ? NotFound() : Ok(firma);
    }
}
```

### Operacje CRUD dla StatusówKolejki

```csharp
public class QueueStatusController : ControllerBase
{
    private readonly IQueueStatusService _queueStatusService;

    public QueueStatusController(IQueueStatusService queueStatusService)
    {
        _queueStatusService = queueStatusService;
    }

    [HttpGet]
    public async Task<IEnumerable<QueueStatus>> GetAll()
    {
        return await _queueStatusService.GetAllAsync();
    }

    [HttpGet("by-value/{value}")]
    public async Task<ActionResult<QueueStatus>> GetByValue(int value)
    {
        var status = await _queueStatusService.GetByValueAsync(value);
        return status == null ? NotFound() : Ok(status);
    }

    [HttpPost]
    public async Task<ActionResult<QueueStatus>> Create(CreateQueueStatusDto dto)
    {
        var queueStatus = new QueueStatus
        {
            Name = dto.Name,
            Value = dto.Value,
            Description = dto.Description
        };

        try
        {
            var result = await _queueStatusService.AddAsync(queueStatus);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

### Operacje CRUD dla Kolejki

```csharp
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly IFirmyService _firmyService;

    public QueueController(IQueueService queueService, IFirmyService firmyService)
    {
        _queueService = queueService;
        _firmyService = firmyService;
    }

    [HttpGet]
    public async Task<IEnumerable<Queue>> GetAll()
    {
        return await _queueService.GetAllAsync();
    }

    [HttpGet("by-firma/{firmaId}")]
    public async Task<IEnumerable<Queue>> GetByFirma(int firmaId)
    {
        return await _queueService.GetByFirmaIdAsync(firmaId);
    }

    [HttpPost]
    public async Task<ActionResult<Queue>> Create(CreateQueueDto dto)
    {
        // SprawdŸ czy firma istnieje
        var firma = await _firmyService.GetByIdAsync(dto.FirmaId);
        if (firma == null)
            return BadRequest("Firma not found");

        var queue = new Queue
        {
            FirmaId = dto.FirmaId,
            Scope = dto.Scope,
            Request = dto.Request,
            Flg = dto.Flg,
            Description = dto.Description,
            TargetID = dto.TargetID
        };

        var result = await _queueService.AddAsync(queue);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

### Praca z relacjami

```csharp
// Pobierz firmê wraz z jej zadaniami w kolejce
var firma = await _firmyService.GetByIdAsync(1);
if (firma != null)
{
    var zadaniaFirmy = await _queueService.GetByFirmaIdAsync(firma.Id);
    Console.WriteLine($"Firma {firma.NazwaFirmy} ma {zadaniaFirmy.Count()} zadañ");
}

// Dodaj zadanie do kolejki dla konkretnej firmy
var webhookTask = new Queue
{
    FirmaId = firma.Id,
    Scope = 1, // Webhook
    Request = JsonSerializer.Serialize(webhookData),
    Description = "Process webhook notification",
    TargetID = orderId
};
await _queueService.AddAsync(webhookTask);
```

### Praca z timestampami Unix

```csharp
var queue = new Queue();

// Ustaw datê przez w³aœciwoœæ DateTime (automatycznie konwertuje na Unix)
queue.DateAddDateTime = DateTime.UtcNow;
queue.DateModDateTime = DateTime.UtcNow;

// Lub ustaw bezpoœrednio Unix timestamp
queue.DateAdd = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

// Odczytaj jako DateTime
Console.WriteLine($"Data utworzenia: {queue.DateAddDateTime}");
```

## Modele

### Model Firmy

Tabela `ProRWS.Firmy` zawiera:

| Kolumna | Typ | Opis |
|---------|-----|------|
| `Id` | int (PK, Identity) | Identyfikator firmy |
| `RowID` | uniqueidentifier | Unikalny GUID (auto-generowany) |
| `NazwaFirmy` | nvarchar(255) | Nazwa firmy |
| `NazwaBazyERP` | nvarchar(255) | Nazwa bazy ERP (unikalna) |
| `CzyTestowa` | bit | Czy firma jest testowa |
| `ApiKey` | nvarchar(255) | Klucz API (opcjonalny, unikalny) |
| `AuthorizeAllEndpoints` | bit | Czy autoryzowaæ wszystkie endpointy |

### Model QueueStatus

Tabela `ProRWS.QueueStatus` zawiera:

| Kolumna | Typ | Opis |
|---------|-----|------|
| `Id` | int (PK, Identity) | Identyfikator statusu |
| `RowID` | uniqueidentifier | Unikalny GUID (auto-generowany) |
| `Name` | varchar(16) | Nazwa statusu (unikalna) |
| `Value` | int | Wartoœæ numeryczna statusu (unikalna) |
| `Description` | varchar(1024) | Opis statusu |

### Model Queue

Tabela `ProRWS.Queue` zawiera:

| Kolumna | Typ | Opis |
|---------|-----|------|
| `Id` | int (PK, Identity) | Identyfikator zadania |
| `RowID` | uniqueidentifier | Unikalny GUID (auto-generowany) |
| `Firma` | int (FK) | Identyfikator firmy (klucz obcy) |
| `Scope` | int | Zakres operacji |
| `Request` | varchar(max) | ¯¹danie w formacie JSON/XML |
| `DateAdd` | int | Data dodania (Unix timestamp) |
| `DateMod` | int | Data modyfikacji (Unix timestamp) |
| `Flg` | int | Flagi statusu/opcji |
| `Description` | varchar(1024) | Opis zadania |
| `TargetID` | int | Identyfikator docelowy |

#### Relacje
- `Queue.Firma` ? `Firmy.Id` (Many-to-One)
- `Firmy.QueueItems` ? Kolekcja zadañ dla firmy (One-to-Many)

## DTOs

### Dla Firm
- **FirmaDto** - Wyœwietlanie danych firmy
- **CreateFirmaDto** - Tworzenie nowej firmy
- **UpdateFirmaDto** - Aktualizacja firmy
- **FirmaSearchDto** - Wyszukiwanie i filtrowanie
- **PagedFirmyDto** - Stronicowana lista

### Dla StatusówKolejki
- **QueueStatusDto** - Wyœwietlanie danych statusu
- **CreateQueueStatusDto** - Tworzenie nowego statusu
- **UpdateQueueStatusDto** - Aktualizacja statusu
- **QueueStatusSearchDto** - Wyszukiwanie i filtrowanie
- **PagedQueueStatusDto** - Stronicowana lista
- **QueueStatusLookupDto** - Lista wyboru statusów

### Dla Kolejki
- **QueueDto** - Wyœwietlanie danych zadania (z preview Request)
- **QueueDetailDto** - Szczegó³owe dane zadania (z pe³nym Request)
- **CreateQueueDto** - Tworzenie nowego zadania
- **UpdateQueueDto** - Aktualizacja zadania
- **QueueSearchDto** - Wyszukiwanie i filtrowanie
- **PagedQueueDto** - Stronicowana lista
- **QueueStatsDto** - Statystyki kolejki

## API Serwisów

### IFirmyService

```csharp
Task<IEnumerable<Firmy>> GetAllAsync();
Task<Firmy?> GetByIdAsync(int id);
Task<Firmy?> GetByApiKeyAsync(string apiKey);
Task<Firmy> AddAsync(Firmy firma);
Task<bool> UpdateAsync(Firmy firma);
Task<bool> DeleteAsync(int id);
// ... i wiêcej
```

### IQueueStatusService

```csharp
Task<IEnumerable<QueueStatus>> GetAllAsync();
Task<QueueStatus?> GetByIdAsync(int id);
Task<QueueStatus?> GetByNameAsync(string name);
Task<QueueStatus?> GetByValueAsync(int value);
Task<QueueStatus> AddAsync(QueueStatus queueStatus);
Task<bool> UpdateAsync(QueueStatus queueStatus);
Task<bool> DeleteAsync(int id);
Task<bool> IsValueUniqueAsync(int value, int? excludeId = null);
Task<bool> IsNameUniqueAsync(string name, int? excludeId = null);
// ... i wiêcej
```

### IQueueService

```csharp
Task<IEnumerable<Queue>> GetAllAsync();
Task<Queue?> GetByIdAsync(int id);
Task<IEnumerable<Queue>> GetByFirmaIdAsync(int firmaId);
Task<IEnumerable<Queue>> GetByScopeAsync(int scope);
Task<IEnumerable<Queue>> GetByFlagAsync(int flg);
Task<IEnumerable<Queue>> GetByDateRangeAsync(DateTime dateFrom, DateTime dateTo);
Task<Queue> AddAsync(Queue queue);
Task<bool> UpdateAsync(Queue queue);
Task<bool> DeleteAsync(int id);
Task<int> GetCountAsync();
Task<int> DeleteOldItemsAsync(int olderThanDays);
// ... i wiêcej
```

## Migracje Entity Framework

Tworzenie migracji:

```bash
# Z poziomu katalogu rozwi¹zania
dotnet ef migrations add AddQueue --project Prospeo.DbContext --context ProspeoDataContext

# Aktualizacja bazy danych
dotnet ef database update --project Prospeo.DbContext --context ProspeoDataContext
```

## Integracja z innymi projektami

### Prosepo.Webhooks

```csharp
// Program.cs w Prosepo.Webhooks
builder.Services.AddProspeoServices(builder.Configuration);

// U¿ycie w kontrolerze
public class WebhookController : ControllerBase
{
    private readonly IFirmyService _firmyService;
    private readonly IQueueService _queueService;
    private readonly IQueueStatusService _queueStatusService;

    public async Task<IActionResult> ProcessWebhook([FromHeader] string apiKey, [FromBody] object payload)
    {
        // ZnajdŸ firmê po API key
        var firma = await _firmyService.GetByApiKeyAsync(apiKey);
        if (firma == null)
        {
            return Unauthorized("Invalid API key");
        }

        // Dodaj zadanie do kolejki
        var queueTask = new Queue
        {
            FirmaId = firma.Id,
            Scope = 100, // Webhook processing
            Request = JsonSerializer.Serialize(payload),
            Description = "Incoming webhook processing",
            TargetID = 0
        };

        var addedTask = await _queueService.AddAsync(queueTask);
        
        return Ok(new { TaskId = addedTask.Id, Status = "Queued" });
    }
}
```

## Zale¿noœci

- Microsoft.EntityFrameworkCore (8.0.8)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.8)
- Microsoft.EntityFrameworkCore.Design (8.0.8)
- Microsoft.Extensions.Configuration.Abstractions (8.0.0)

## Struktura projektu

```
Prospeo.DbContext/
??? Data/
?   ??? ProspeoDbContext.cs          # Kontekst EF (ProspeoDataContext)
??? Models/
?   ??? Firmy.cs                     # Model firmy
?   ??? QueueStatus.cs               # Model statusów kolejki
?   ??? Queue.cs                     # Model kolejki zadañ
?   ??? QueueModel.cs                # Model kolejki (legacy)
??? Services/
?   ??? IFirmyService.cs             # Interfejs serwisu firm
?   ??? FirmyService.cs              # Implementacja serwisu firm
?   ??? IQueueStatusService.cs       # Interfejs serwisu statusów
?   ??? QueueStatusService.cs        # Implementacja serwisu statusów
?   ??? IQueueService.cs             # Interfejs serwisu kolejki
?   ??? QueueService.cs              # Implementacja serwisu kolejki
??? DTOs/
?   ??? FirmyDtos.cs                 # DTOs dla firm
?   ??? QueueStatusDtos.cs           # DTOs dla statusów kolejki
?   ??? QueueDtos.cs                 # DTOs dla kolejki zadañ
??? Extensions/
?   ??? ServiceCollectionExtensions.cs # Rozszerzenia DI
??? Examples/
?   ??? FirmyExamples.cs             # Przyk³ady u¿ycia firm
?   ??? QueueStatusExamples.cs       # Przyk³ady u¿ycia statusów
?   ??? QueueExamples.cs             # Przyk³ady u¿ycia kolejki
??? README.md                        # Ta dokumentacja