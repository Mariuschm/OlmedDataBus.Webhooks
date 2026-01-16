# QueueRelations - Dokumentacja

## Przegl¹d

`QueueRelations` to model reprezentuj¹cy relacje miêdzy elementami kolejki w systemie ProRWS. Umo¿liwia œledzenie zale¿noœci miêdzy ró¿nymi zadaniami w kolejce, co jest kluczowe dla zachowania spójnoœci danych i zarz¹dzania przep³ywem procesów biznesowych.

## Struktura tabeli SQL

```sql
CREATE TABLE ProRWS.QueueRelations
(
    Id INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_QueueRelations PRIMARY KEY,
    SourceItemId INT NOT NULL,
    TargetItemId INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    
    CONSTRAINT UQ_QueueRelations_Source_Target
        UNIQUE (SourceItemId, TargetItemId),
    CONSTRAINT FK_QueueRelations_Source
        FOREIGN KEY (SourceItemId) REFERENCES ProRWS.Queue(Id),
    CONSTRAINT FK_QueueRelations_Target
        FOREIGN KEY (TargetItemId) REFERENCES ProRWS.Queue(Id)
);

-- Indeksy wspomagaj¹ce zapytania
CREATE INDEX IX_QueueRelations_SourceItemId ON ProRWS.QueueRelations (SourceItemId);
CREATE INDEX IX_QueueRelations_TargetItemId ON ProRWS.QueueRelations (TargetItemId);
```

## Pola modelu

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | `int` | Klucz g³ówny, auto-increment |
| `SourceItemId` | `int` | ID elementu Ÿród³owego (parent) - klucz obcy do `Queue.Id` |
| `TargetItemId` | `int` | ID elementu docelowego (child) - klucz obcy do `Queue.Id` |
| `CreatedAt` | `DateTime` | Data utworzenia relacji (automatycznie ustawiana) |
| `SourceItem` | `Queue?` | Relacja nawigacyjna do elementu Ÿród³owego |
| `TargetItem` | `Queue?` | Relacja nawigacyjna do elementu docelowego |

## Przypadki u¿ycia

### 1. Zamówienie ? Faktura
Gdy zamówienie generuje fakturê sprzeda¿y:
```csharp
// OrderQueueService tworzy zamówienie
var orderQueueItem = await queueService.AddAsync(new Queue { ... });

// InvoiceQueueService tworzy fakturê
var invoiceQueueItem = await queueService.AddAsync(new Queue { ... });

// Tworzymy relacjê
await queueRelationsService.CreateRelationAsync(
    orderQueueItem.Id,      // Source: zamówienie
    invoiceQueueItem.Id     // Target: faktura
);
```

### 2. Faktura ? Korekta
Gdy faktura wymaga korekty:
```csharp
// Oryginalna faktura ju¿ istnieje
var originalInvoiceId = 123;

// CorrectionQueueService tworzy korektê
var correctionQueueItem = await queueService.AddAsync(new Queue { ... });

// Tworzymy relacjê
await queueRelationsService.CreateRelationAsync(
    originalInvoiceId,          // Source: oryginalna faktura
    correctionQueueItem.Id      // Target: korekta
);
```

### 3. Zamówienie ? Wydanie magazynowe (MMW)
Gdy zamówienie generuje dokument wydania:
```csharp
// Zamówienie istnieje
var orderQueueId = 456;

// MmwQueueService tworzy wydanie
var mmwQueueItem = await queueService.AddAsync(new Queue { ... });

// Tworzymy relacjê
await queueRelationsService.CreateRelationAsync(
    orderQueueId,           // Source: zamówienie
    mmwQueueItem.Id         // Target: wydanie magazynowe
);
```

### 4. Faktura zakupu ? Przyjêcie magazynowe (PW)
Gdy faktura zakupu generuje przyjêcie do magazynu:
```csharp
// PurchaseInvoiceQueueService tworzy fakturê zakupu
var purchaseInvoiceId = 789;

// PwQueueService tworzy przyjêcie magazynowe
var pwQueueItem = await queueService.AddAsync(new Queue { ... });

// Tworzymy relacjê
await queueRelationsService.CreateRelationAsync(
    purchaseInvoiceId,      // Source: faktura zakupu
    pwQueueItem.Id          // Target: przyjêcie magazynowe
);
```

## Serwis `IQueueRelationsService`

### Podstawowe operacje

#### Tworzenie relacji
```csharp
// Tworzy now¹ relacjê (rzuca wyj¹tek jeœli istnieje)
var relation = await queueRelationsService.CreateRelationAsync(sourceId, targetId);

// Tworzy lub pobiera istniej¹c¹ relacjê (bezpieczne)
var relation = await queueRelationsService.CreateOrGetRelationAsync(sourceId, targetId);
```

#### Pobieranie relacji
```csharp
// Po ID relacji
var relation = await queueRelationsService.GetByIdAsync(relationId);

// Miêdzy dwoma elementami
var relation = await queueRelationsService.GetRelationAsync(sourceId, targetId);

// Wszystkie relacje wychodz¹ce (dzieci)
var sourceRelations = await queueRelationsService.GetSourceRelationsAsync(sourceId);

// Wszystkie relacje przychodz¹ce (rodzice)
var targetRelations = await queueRelationsService.GetTargetRelationsAsync(targetId);

// Wszystkie relacje dla elementu (zarówno source jak i target)
var allRelations = await queueRelationsService.GetAllRelationsForItemAsync(itemId);
```

#### Pobieranie powi¹zanych elementów kolejki
```csharp
// Pobierz elementy docelowe (dzieci)
var targetItems = await queueRelationsService.GetTargetItemsAsync(sourceId);

// Pobierz elementy Ÿród³owe (rodzice)
var sourceItems = await queueRelationsService.GetSourceItemsAsync(targetId);
```

#### Usuwanie relacji
```csharp
// Usuñ relacjê po ID
bool deleted = await queueRelationsService.DeleteAsync(relationId);

// Usuñ relacjê miêdzy elementami
bool deleted = await queueRelationsService.DeleteRelationAsync(sourceId, targetId);

// Usuñ wszystkie relacje wychodz¹ce
int deletedCount = await queueRelationsService.DeleteSourceRelationsAsync(sourceId);

// Usuñ wszystkie relacje przychodz¹ce
int deletedCount = await queueRelationsService.DeleteTargetRelationsAsync(targetId);

// Usuñ wszystkie relacje dla elementu
int deletedCount = await queueRelationsService.DeleteAllRelationsForItemAsync(itemId);
```

#### Sprawdzanie istnienia i liczniki
```csharp
// SprawdŸ czy relacja istnieje
bool exists = await queueRelationsService.RelationExistsAsync(sourceId, targetId);

// Liczba relacji wychodz¹cych
int count = await queueRelationsService.GetSourceRelationsCountAsync(sourceId);

// Liczba relacji przychodz¹cych
int count = await queueRelationsService.GetTargetRelationsCountAsync(targetId);
```

## Integracja z Queue Service Processors

### W OrderQueueService
```csharp
public class OrderQueueService : IOrderQueueService
{
    private readonly IQueueService _queueService;
    private readonly IQueueRelationsService _queueRelationsService;
    
    public async Task ProcessOrderAsync(OrderDto order)
    {
        // 1. Utwórz element kolejki dla zamówienia
        var orderQueue = await _queueService.AddAsync(new Queue
        {
            Scope = (int)QueueScope.Order,
            Request = JsonSerializer.Serialize(order),
            // ... inne pola
        });
        
        // 2. Przetwórz zamówienie w ERP
        var erpOrderId = await CreateOrderInErp(order);
        orderQueue.TargetID = erpOrderId;
        await _queueService.UpdateAsync(orderQueue);
        
        // 3. Jeœli zamówienie generuje fakturê
        if (shouldGenerateInvoice)
        {
            var invoiceQueue = await CreateInvoiceForOrder(order, erpOrderId);
            
            // 4. Utwórz relacjê zamówienie ? faktura
            await _queueRelationsService.CreateRelationAsync(
                orderQueue.Id,
                invoiceQueue.Id
            );
        }
        
        // 5. Jeœli zamówienie generuje wydanie magazynowe
        if (shouldGenerateIssue)
        {
            var issueQueue = await CreateIssueForOrder(order, erpOrderId);
            
            // 6. Utwórz relacjê zamówienie ? wydanie
            await _queueRelationsService.CreateRelationAsync(
                orderQueue.Id,
                issueQueue.Id
            );
        }
    }
}
```

### W InvoiceCorrectionQueueService
```csharp
public class InvoiceCorrectionQueueService : IInvoiceCorrectionQueueService
{
    private readonly IQueueService _queueService;
    private readonly IQueueRelationsService _queueRelationsService;
    
    public async Task ProcessCorrectionAsync(CorrectionDto correction)
    {
        // 1. ZnajdŸ oryginaln¹ fakturê w kolejce
        var originalInvoices = await _queueService.GetByTargetIdAsync(correction.OriginalInvoiceId);
        var originalInvoiceQueue = originalInvoices.FirstOrDefault();
        
        if (originalInvoiceQueue == null)
        {
            throw new InvalidOperationException("Nie znaleziono oryginalnej faktury w kolejce");
        }
        
        // 2. Utwórz element kolejki dla korekty
        var correctionQueue = await _queueService.AddAsync(new Queue
        {
            Scope = (int)QueueScope.InvoiceCorrection,
            Request = JsonSerializer.Serialize(correction),
            // ... inne pola
        });
        
        // 3. Przetwórz korektê w ERP
        var erpCorrectionId = await CreateCorrectionInErp(correction);
        correctionQueue.TargetID = erpCorrectionId;
        await _queueService.UpdateAsync(correctionQueue);
        
        // 4. Utwórz relacjê faktura ? korekta
        await _queueRelationsService.CreateRelationAsync(
            originalInvoiceQueue.Id,
            correctionQueue.Id
        );
    }
    
    public async Task<IEnumerable<Queue>> GetCorrectionsForInvoiceAsync(int invoiceQueueId)
    {
        // Pobierz wszystkie korekty dla danej faktury
        return await _queueRelationsService.GetTargetItemsAsync(invoiceQueueId);
    }
}
```

## Migracje Entity Framework

### Tworzenie migracji
```bash
# Z poziomu katalogu projektu Prospeo.DbContext
dotnet ef migrations add AddQueueRelations --project ../OlmedDataBus/Prospeo.DbContext

# Aktualizacja bazy danych
dotnet ef database update --project ../OlmedDataBus/Prospeo.DbContext
```

### Rêczne tworzenie tabeli
Jeœli nie u¿ywasz migracji EF, wykonaj skrypt SQL z sekcji "Struktura tabeli SQL" powy¿ej.

## Konfiguracja Entity Framework

Model jest ju¿ skonfigurowany w `ProspeoDataContext.OnModelCreating()`:
- Klucz g³ówny: `PK_QueueRelations`
- Unikalne ograniczenie: `UQ_QueueRelations_Source_Target` dla pary (SourceItemId, TargetItemId)
- Klucze obce: `FK_QueueRelations_Source` i `FK_QueueRelations_Target`
- Indeksy: `IX_QueueRelations_SourceItemId` i `IX_QueueRelations_TargetItemId`
- Relacje nawigacyjne: dwukierunkowe miêdzy `Queue` a `QueueRelations`
- DeleteBehavior: `Restrict` (zapobiega kaskadowym usuniêciom i potencjalnym pêtlom)

## Rejestracja serwisu

Serwis jest automatycznie rejestrowany przy u¿yciu metod rozszerzaj¹cych:

```csharp
// W Program.cs lub Startup.cs
services.AddProspeoServices(configuration);
// lub
services.AddProspeoServices(connectionString);
// lub
services.AddProspeoServicesDirect(connectionString);

// Wszystkie powy¿sze metody rejestruj¹ IQueueRelationsService
```

## Best Practices

### 1. Twórz relacje natychmiast po utworzeniu target item
```csharp
// ? DOBRZE
var sourceQueue = await queueService.AddAsync(sourceItem);
var targetQueue = await queueService.AddAsync(targetItem);
await queueRelationsService.CreateRelationAsync(sourceQueue.Id, targetQueue.Id);
```

### 2. U¿ywaj CreateOrGetRelationAsync dla idempotentnoœci
```csharp
// ? DOBRZE - bezpieczne przy ponownym wywo³aniu
var relation = await queueRelationsService.CreateOrGetRelationAsync(sourceId, targetId);

// ? LE - mo¿e rzuciæ wyj¹tek przy ponownym wywo³aniu
var relation = await queueRelationsService.CreateRelationAsync(sourceId, targetId);
```

### 3. Czyœæ relacje przy usuwaniu elementów
```csharp
// ? DOBRZE
await queueRelationsService.DeleteAllRelationsForItemAsync(queueItemId);
await queueService.DeleteAsync(queueItemId);
```

### 4. U¿ywaj relacji do rollback'ów
```csharp
// Jeœli przetwarzanie target item nie powiedzie siê
var targetItems = await queueRelationsService.GetTargetItemsAsync(sourceId);
foreach (var targetItem in targetItems)
{
    targetItem.Flg = (int)QueueStatusEnum.Error;
    await queueService.UpdateAsync(targetItem);
}
```

### 5. Loguj relacje dla audytu
```csharp
_logger.LogInformation(
    "Utworzono relacjê: {SourceScope} (ID:{SourceId}) -> {TargetScope} (ID:{TargetId})",
    sourceQueue.ScopeEnum,
    sourceQueue.Id,
    targetQueue.ScopeEnum,
    targetQueue.Id
);
```

## Zapytania pomocnicze

### ZnajdŸ wszystkie faktury dla zamówienia
```csharp
var orderQueueId = 123;
var invoices = await queueRelationsService.GetTargetItemsAsync(orderQueueId);
var invoiceQueues = invoices.Where(q => q.ScopeEnum == QueueScope.Invoice);
```

### ZnajdŸ zamówienie dla faktury
```csharp
var invoiceQueueId = 456;
var orders = await queueRelationsService.GetSourceItemsAsync(invoiceQueueId);
var orderQueue = orders.FirstOrDefault(q => q.ScopeEnum == QueueScope.Order);
```

### SprawdŸ czy element ma jakiekolwiek zale¿noœci
```csharp
var hasRelations = 
    await queueRelationsService.GetSourceRelationsCountAsync(itemId) > 0 ||
    await queueRelationsService.GetTargetRelationsCountAsync(itemId) > 0;
```

## Monitorowanie i troubleshooting

### SprawdŸ status powi¹zanych zadañ
```csharp
public async Task<bool> AreAllRelatedTasksCompletedAsync(int sourceItemId)
{
    var targetItems = await queueRelationsService.GetTargetItemsAsync(sourceItemId);
    return targetItems.All(item => item.FlgEnum == QueueStatusEnum.Completed);
}
```

### ZnajdŸ nieukoñczone zale¿noœci
```csharp
public async Task<IEnumerable<Queue>> GetPendingDependenciesAsync(int sourceItemId)
{
    var targetItems = await queueRelationsService.GetTargetItemsAsync(sourceItemId);
    return targetItems.Where(item => 
        item.FlgEnum != QueueStatusEnum.Completed && 
        item.FlgEnum != QueueStatusEnum.Error
    );
}
```

## Bezpieczeñstwo

- **Constraint**: `UQ_QueueRelations_Source_Target` zapobiega duplikatom relacji
- **Foreign Keys**: Gwarantuj¹ integralnoœæ referencyjn¹
- **DeleteBehavior.Restrict**: Zapobiega przypadkowemu usuniêciu elementów kolejki, które maj¹ aktywne relacje
- **Validation**: Serwis sprawdza istnienie elementów przed utworzeniem relacji

## Performance

- **Indeksy**: Utworzone na `SourceItemId` i `TargetItemId` dla szybkich wyszukiwañ
- **Eager Loading**: U¿yj `Include()` do za³adowania powi¹zanych elementów:
  ```csharp
  var relations = await context.QueueRelations
      .Include(r => r.SourceItem)
      .Include(r => r.TargetItem)
      .ToListAsync();
  ```
- **Paging**: Przy du¿ej liczbie relacji u¿ywaj `Skip()` i `Take()`

## Przyk³adowy workflow kompletny

```csharp
public class CompleteOrderWorkflow
{
    private readonly IQueueService _queueService;
    private readonly IQueueRelationsService _relationsService;
    
    public async Task<OrderProcessingResult> ProcessCompleteOrderAsync(OrderDto order)
    {
        var result = new OrderProcessingResult();
        
        try
        {
            // 1. Utwórz zamówienie w kolejce
            var orderQueue = await _queueService.AddAsync(new Queue
            {
                Scope = (int)QueueScope.Order,
                Request = JsonSerializer.Serialize(order),
                Flg = (int)QueueStatusEnum.Pending
            });
            
            // 2. Przetwórz zamówienie
            var erpOrderId = await ProcessOrderInErp(order);
            orderQueue.TargetID = erpOrderId;
            orderQueue.Flg = (int)QueueStatusEnum.Completed;
            await _queueService.UpdateAsync(orderQueue);
            
            // 3. Utwórz fakturê
            var invoiceQueue = await CreateInvoiceQueue(order, erpOrderId);
            await _relationsService.CreateRelationAsync(orderQueue.Id, invoiceQueue.Id);
            result.InvoiceQueueId = invoiceQueue.Id;
            
            // 4. Utwórz wydanie magazynowe
            var issueQueue = await CreateIssueQueue(order, erpOrderId);
            await _relationsService.CreateRelationAsync(orderQueue.Id, issueQueue.Id);
            result.IssueQueueId = issueQueue.Id;
            
            // 5. Jeœli jest p³atnoœæ
            if (order.Payment != null)
            {
                var paymentQueue = await CreatePaymentQueue(order.Payment, erpOrderId);
                await _relationsService.CreateRelationAsync(invoiceQueue.Id, paymentQueue.Id);
                result.PaymentQueueId = paymentQueue.Id;
            }
            
            result.Success = true;
            result.OrderQueueId = orderQueue.Id;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            
            // Rollback - oznacz wszystkie powi¹zane zadania jako b³êdne
            if (result.OrderQueueId > 0)
            {
                await MarkRelatedTasksAsErrorAsync(result.OrderQueueId);
            }
        }
        
        return result;
    }
    
    private async Task MarkRelatedTasksAsErrorAsync(int orderQueueId)
    {
        var relatedTasks = await _relationsService.GetTargetItemsAsync(orderQueueId);
        foreach (var task in relatedTasks)
        {
            task.Flg = (int)QueueStatusEnum.Error;
            await _queueService.UpdateAsync(task);
        }
    }
}
```

## Wsparcie

Dla pytañ i problemów zwi¹zanych z `QueueRelations`:
1. SprawdŸ logi - serwis loguje wszystkie operacje
2. Zweryfikuj klucze obce w bazie danych
3. Upewnij siê, ¿e elementy kolejki istniej¹ przed utworzeniem relacji
4. SprawdŸ czy nie ma duplikatów relacji (constraint `UQ_QueueRelations_Source_Target`)
