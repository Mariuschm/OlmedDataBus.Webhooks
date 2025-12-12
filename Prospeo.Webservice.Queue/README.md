# Prospeo.Webservice.Queue

Biblioteka Entity Framework do obs³ugi kolejek zadañ asynchronicznych z persystencj¹ w bazie danych.

## Funkcje

- ? Entity Framework Core dla persystencji w bazie danych
- ? Obs³uga kolejek asynchronicznych
- ? Automatyczny mechanizm ponawiania z wyk³adniczym opóŸnieniem
- ? Przetwarzanie oparte na priorytecie
- ? Wsparcie dla Correlation ID do œledzenia
- ? Kompleksowe logowanie
- ? Œledzenie statusu i monitorowanie
- ? Konfigurowalne próby ponowienia
- ? Automatyczne czyszczenie zakoñczonych elementów

## Instalacja

Dodaj bibliotekê do swojego projektu:

```xml
<PackageReference Include="Prospeo.Webservice.Queue" Version="1.0.0" />
```

## Szybki start

### 1. Konfiguracja bazy danych

Uruchom migracje Entity Framework, aby utworzyæ schemat bazy danych:

```bash
dotnet ef migrations add InitialCreate --project YourProject --context QueueDbContext
dotnet ef database update --project YourProject --context QueueDbContext
```

### 2. Konfiguracja

W `appsettings.json` dodaj connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ProspeoQueueDb;Trusted_Connection=true;"
  }
}
```

### 3. Rejestracja us³ug

W `Program.cs` lub `Startup.cs`:

```csharp
using Prospeo.Webservice.Queue.Extensions;

// Rejestracja podstawowych us³ug kolejki
services.AddQueueServices(configuration);

// Lub z niestandardowym procesorem
services.AddQueueServices<MyCustomProcessor>(configuration);

// Lub z bezpoœrednim connection stringiem
services.AddQueueServices("your-connection-string");
```

### 4. Podstawowe u¿ycie

```csharp
public class MyService
{
    private readonly IQueueService _queueService;

    public MyService(IQueueService queueService)
    {
        _queueService = queueService;
    }

    public async Task<long> SendEmailAsync(string email, string subject, string body)
    {
        var emailData = new { Email = email, Subject = subject, Body = body };
        
        return await _queueService.EnqueueAsync("email", emailData, 
            correlationId: Guid.NewGuid().ToString(),
            priority: 1);
    }

    public async Task<int> CheckPendingItemsAsync()
    {
        return await _queueService.GetQueueCountAsync(QueueItemStatus.Pending);
    }
}
```

## Implementacja niestandardowego procesora

Utwórz niestandardowy procesor implementuj¹c `IQueueProcessor`:

```csharp
using Prospeo.Webservice.Queue.Services;
using Prospeo.Webservice.Queue.Models;

public class MyCustomProcessor : IQueueProcessor
{
    private readonly ILogger<MyCustomProcessor> _logger;
    private readonly IEmailService _emailService;

    public MyCustomProcessor(ILogger<MyCustomProcessor> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<bool> ProcessAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (item.Type.ToLowerInvariant())
            {
                case "email":
                    return await ProcessEmailAsync(item, cancellationToken);
                
                case "webhook":
                    return await ProcessWebhookAsync(item, cancellationToken);
                
                default:
                    _logger.LogWarning("Nieznany typ elementu kolejki: {Type}", item.Type);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B³¹d podczas przetwarzania elementu {Id}", item.Id);
            return false;
        }
    }

    private async Task<bool> ProcessEmailAsync(QueueItem item, CancellationToken cancellationToken)
    {
        var emailData = JsonSerializer.Deserialize<EmailData>(item.Payload);
        return await _emailService.SendAsync(emailData.Email, emailData.Subject, emailData.Body);
    }

    private async Task<bool> ProcessWebhookAsync(QueueItem item, CancellationToken cancellationToken)
    {
        var webhookData = JsonSerializer.Deserialize<WebhookData>(item.Payload);
        // Implementuj logikê webhook
        return true;
    }
}
```

## Worker Service (opcjonalny)

Mo¿esz utworzyæ Worker Service do automatycznego przetwarzania kolejki:

```csharp
public class QueueWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueWorkerService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

    public QueueWorkerService(IServiceProvider serviceProvider, ILogger<QueueWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();
                var processor = scope.ServiceProvider.GetRequiredService<IQueueProcessor>();

                var item = await queueService.DequeueAsync();
                if (item != null)
                {
                    await queueService.MarkAsProcessingAsync(item.Id);
                    
                    var success = await processor.ProcessAsync(item, stoppingToken);
                    
                    if (success)
                        await queueService.MarkAsCompletedAsync(item.Id);
                    else
                        await queueService.MarkAsFailedAsync(item.Id, "Przetwarzanie zwróci³o false");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d w worker service");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }
}

// Rejestracja:
services.AddHostedService<QueueWorkerService>();
```

## API kolejki

Interfejs `IQueueService` udostêpnia nastêpuj¹ce metody:

```csharp
// Dodawanie do kolejki
Task<long> EnqueueAsync<T>(string type, T payload, string? correlationId = null, string? source = null, int priority = 0, int maxAttempts = 3);

// Pobieranie z kolejki
Task<QueueItem?> DequeueAsync();

// Zarz¹dzanie statusem
Task MarkAsProcessingAsync(long id);
Task MarkAsCompletedAsync(long id);
Task MarkAsFailedAsync(long id, string errorMessage, string? stackTrace = null);
Task ScheduleRetryAsync(long id, DateTime nextRetryAt);

// Zapytania
Task<QueueItem?> GetByIdAsync(long id);
Task<IEnumerable<QueueItem>> GetByCorrelationIdAsync(string correlationId);
Task<IEnumerable<QueueItem>> GetByStatusAsync(QueueItemStatus status, int skip = 0, int take = 100);
Task<int> GetQueueCountAsync(QueueItemStatus? status = null);

// Utrzymanie
Task DeleteCompletedItemsAsync(TimeSpan olderThan);
```

## Schemat bazy danych

Kolejka u¿ywa pojedynczej tabeli `QueueItems`:

- `Id` (bigint, klucz g³ówny)
- `Type` (nvarchar(500)) - typ elementu kolejki
- `Payload` (nvarchar(max)) - dane JSON
- `Status` (int) - status (Pending, Processing, Completed, Failed, Cancelled)
- `Attempts` (int) - liczba prób przetwarzania
- `MaxAttempts` (int) - maksymalna liczba prób
- `CreatedAt` (datetime2) - data utworzenia
- `ProcessedAt` (datetime2) - data zakoñczenia/b³êdu
- `NextRetryAt` (datetime2) - kiedy ponowiæ nieudane elementy
- `ErrorMessage` (nvarchar(max)) - komunikat b³êdu
- `ErrorStackTrace` (nvarchar(max)) - stos b³êdu
- `CorrelationId` (nvarchar(100)) - opcjonalny ID korelacji
- `Source` (nvarchar(200)) - opcjonalny identyfikator Ÿród³a
- `Priority` (int) - priorytet przetwarzania

## Zale¿noœci

- Microsoft.EntityFrameworkCore (8.0.8)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.8)
- Microsoft.Extensions.DependencyInjection.Abstractions (8.0.0)
- System.Text.Json (8.0.4)

## U¿ycie w Prospeo.Webhooks

```csharp
// W Program.cs projektu Prospeo.Webhooks
services.AddQueueServices<WebhookQueueProcessor>(configuration);

// Implementacja WebhookQueueProcessor
public class WebhookQueueProcessor : IQueueProcessor
{
    public async Task<bool> ProcessAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        // Logika przetwarzania webhook
        return true;
    }
}
```