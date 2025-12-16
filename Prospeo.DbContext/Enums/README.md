# QueueScope i QueueStatus Enums

Enumy reprezentuj¹ce typy zakresu operacji i statusy zadañ w kolejce.

## QueueScope - Typ operacji

```csharp
namespace Prospeo.DbContext.Enums;

public enum QueueScope
{
    /// <summary>
    /// Operacja na towarze
    /// </summary>
    Towar = 16
}
```

## QueueStatus - Status zadania

```csharp
public enum QueueStatus
{
    /// <summary>
    /// Zadanie oczekuje na przetworzenie
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Zadanie w trakcie przetwarzania
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Zadanie zakoñczone pomyœlnie
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Zadanie zakoñczone z b³êdem
    /// </summary>
    Error = 3
}
```

## U¿ycie w modelu Queue

Model `Queue` zosta³ rozszerzony o w³aœciwoœci enum:

```csharp
public class Queue
{
    // Zakres operacji
    public int Scope { get; set; }
    public QueueScope ScopeEnum { get; set; }
    
    // Status zadania
    public int Flg { get; set; }
    public QueueStatus FlgEnum { get; set; }
}
```

## Przyk³ady u¿ycia

### 1. Pobieranie zadañ oczekuj¹cych na przetworzenie

```csharp
// Pobierz zadania towarowe oczekuj¹ce na przetworzenie
var pendingTowarTasks = await context.Queues
    .Where(q => q.FirmaId == companyId 
             && q.Scope == (int)QueueScope.Towar
             && q.Flg == (int)QueueStatus.Pending)
    .OrderBy(q => q.DateAdd)
    .ToListAsync();
```

### 2. U¿ycie w Worker Service

```csharp
public class Worker : BackgroundService
{
    private async Task PerformDatabaseOperations(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProspeoDataContext>();
        
        // Pobierz zadania towarowe oczekuj¹ce na przetworzenie
        var towarQueueItems = await context.Queues
            .Where(q => q.FirmaId == _configuration.CompanyId 
                     && q.Scope == (int)QueueScope.Towar
                     && q.Flg == (int)QueueStatus.Pending)
            .OrderBy(q => q.DateAdd)
            .Take(10)
            .ToListAsync(cancellationToken);
        
        foreach (var queueItem in towarQueueItems)
        {
            _logger.LogInformation(
                "Przetwarzanie zadania: ID={id}, Scope={scope}, Status={status}, TargetID={targetId}",
                queueItem.Id,
                queueItem.ScopeEnum,
                queueItem.FlgEnum,
                queueItem.TargetID);
            
            try
            {
                // Ustaw status "w trakcie przetwarzania"
                queueItem.FlgEnum = QueueStatus.Processing;
                queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await context.SaveChangesAsync(cancellationToken);
                
                // Przetwórz zadanie
                await ProcessTowarTask(queueItem, cancellationToken);
                
                // Ustaw status "zakoñczone"
                queueItem.FlgEnum = QueueStatus.Completed;
                queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d przetwarzania zadania {id}", queueItem.Id);
                
                // Ustaw status "b³¹d"
                queueItem.FlgEnum = QueueStatus.Error;
                queueItem.Description = $"B³¹d: {ex.Message}";
                queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
```

### 3. Tworzenie nowego zadania

```csharp
var newTask = new Queue
{
    FirmaId = 1,
    ScopeEnum = QueueScope.Towar,
    FlgEnum = QueueStatus.Pending,  // Nowe zadanie oczekuje
    Request = "{...}",
    DateAdd = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    Description = "Synchronizacja towaru",
    TargetID = 123
};

context.Queues.Add(newTask);
await context.SaveChangesAsync();
```

### 4. Filtrowanie po statusie

```csharp
// Pobierz wszystkie zadania z b³êdami
var errorTasks = await context.Queues
    .Where(q => q.Flg == (int)QueueStatus.Error)
    .ToListAsync();

// Pobierz zadania w trakcie przetwarzania
var processingTasks = await context.Queues
    .Where(q => q.FlgEnum == QueueStatus.Processing)
    .ToListAsync();
```

### 5. Ponowne przetwarzanie zadañ z b³êdami

```csharp
// ZnajdŸ zadania z b³êdami, które mo¿na ponowiæ
var retryTasks = await context.Queues
    .Where(q => q.FirmaId == companyId 
             && q.FlgEnum == QueueStatus.Error
             && q.DateMod < oldTimestamp)  // Starsze ni¿ X
    .ToListAsync();

foreach (var task in retryTasks)
{
    // Reset statusu do Pending
    task.FlgEnum = QueueStatus.Pending;
    task.Description = "Ponowienie próby";
    task.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

await context.SaveChangesAsync();
```

### 6. Statystyki zadañ

```csharp
var stats = await context.Queues
    .Where(q => q.FirmaId == companyId)
    .GroupBy(q => q.Flg)
    .Select(g => new
    {
        Status = g.Key,
        Count = g.Count()
    })
    .ToListAsync();

foreach (var stat in stats)
{
    var statusName = Enum.IsDefined(typeof(QueueStatus), stat.Status)
        ? ((QueueStatus)stat.Status).ToString()
        : $"Unknown ({stat.Status})";
    
    Console.WriteLine($"{statusName}: {stat.Count} zadañ");
}
```

## Wartoœci enumów

### QueueScope

| Wartoœæ | Nazwa | Opis |
|---------|-------|------|
| 16 | Towar | Operacja na towarze (insert/update) |

### QueueStatus (Flg)

| Wartoœæ | Nazwa | Opis |
|---------|-------|------|
| 0 | Pending | Zadanie oczekuje na przetworzenie |
| 1 | Processing | Zadanie w trakcie przetwarzania |
| 2 | Completed | Zadanie zakoñczone pomyœlnie |
| 3 | Error | Zadanie zakoñczone z b³êdem |

## Przep³yw statusów zadania

```
???????????
? Pending ? (0) - Nowe zadanie
???????????
     ?
     v
??????????????
? Processing ? (1) - Worker rozpocz¹³ przetwarzanie
??????????????
      ?
      ???????????????
      ?             ?
      v             v
?????????????   ?????????
? Completed ?   ? Error ?
?    (2)    ?   ?  (3)  ?
?????????????   ?????????
                    ?
                    ???> Mo¿e wróciæ do Pending (retry)
```

## Najlepsze praktyki

1. **Zawsze ustawiaj status Processing przed rozpoczêciem przetwarzania**
   - Zapobiega przetwarzaniu tego samego zadania przez wiele workerów

2. **U¿ywaj transakcji przy zmianie statusu**
   ```csharp
   using var transaction = await context.Database.BeginTransactionAsync();
   try
   {
       queueItem.FlgEnum = QueueStatus.Processing;
       await context.SaveChangesAsync();
       
       await ProcessTask(queueItem);
       
       queueItem.FlgEnum = QueueStatus.Completed;
       await context.SaveChangesAsync();
       await transaction.CommitAsync();
   }
   catch
   {
       await transaction.RollbackAsync();
       throw;
   }
   ```

3. **Zapisuj szczegó³y b³êdu w Description**
   ```csharp
   catch (Exception ex)
   {
       queueItem.FlgEnum = QueueStatus.Error;
       queueItem.Description = $"B³¹d: {ex.Message} | StackTrace: {ex.StackTrace}";
       await context.SaveChangesAsync();
   }
   ```

4. **Implementuj mechanizm retry dla zadañ z b³êdami**
   - Dodaj licznik prób w Description
   - Resetuj do Pending po okreœlonym czasie
   - Maksymalna liczba prób

## Uwagi

- W³aœciwoœci `ScopeEnum` i `FlgEnum` s¹ `[NotMapped]` - nie s¹ zapisywane bezpoœrednio w bazie
- Konwersja miêdzy `int` a `enum` odbywa siê automatycznie
- Worker powinien przetwarzaæ tylko zadania ze statusem `Pending` (Flg = 0)
- Status `Processing` zapobiega wielokrotnemu przetwarzaniu tego samego zadania
