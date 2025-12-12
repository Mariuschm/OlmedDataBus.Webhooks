using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prospeo.Webservice.Queue.Extensions;
using Prospeo.Webservice.Queue.Services;
using Prospeo.Webservice.Queue.Models;

namespace Prospeo.Webservice.Queue.Examples;

/// <summary>
/// Przyk³ad u¿ycia biblioteki Prospeo.Webservice.Queue
/// </summary>
public static class UsageExamples
{
    /// <summary>
    /// Przyk³ad konfiguracji w Program.cs lub Startup.cs
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Sposób 1: Podstawowe us³ugi kolejki
        services.AddQueueServices(configuration);
        
        // Sposób 2: Z bezpoœrednim connection stringiem
        // services.AddQueueServices("Server=localhost;Database=QueueDb;Trusted_Connection=true;");
        
        // Sposób 3: Z niestandardowym procesorem
        // services.AddQueueServices<MyCustomProcessor>(configuration);
        
        // Sposób 4: Tylko DbContext (gdy chcesz samodzielnie zarz¹dzaæ serwisami)
        // services.AddQueueDbContext("connection-string");
    }
    
    /// <summary>
    /// Przyk³ad podstawowego u¿ycia kolejki
    /// </summary>
    public static async Task BasicUsageExample(IQueueService queueService)
    {
        // Dodawanie zadañ do kolejki
        var emailTaskId = await queueService.EnqueueAsync("email", new
        {
            To = "user@example.com",
            Subject = "Witaj!",
            Body = "Dziêkujemy za rejestracjê."
        }, correlationId: Guid.NewGuid().ToString());

        var webhookTaskId = await queueService.EnqueueAsync("webhook", new
        {
            Url = "https://api.example.com/webhook",
            Method = "POST",
            Data = new { UserId = 123, Action = "registered" }
        }, priority: 1); // Wy¿szy priorytet

        // Sprawdzanie statusu
        var emailTask = await queueService.GetByIdAsync(emailTaskId);
        Console.WriteLine($"Email task status: {emailTask?.Status}");

        // Pobieranie statystyk
        var pendingCount = await queueService.GetQueueCountAsync(QueueItemStatus.Pending);
        Console.WriteLine($"Pending tasks: {pendingCount}");
    }

    /// <summary>
    /// Przyk³ad przetwarzania zadañ (u¿yj w worker service lub background service)
    /// </summary>
    public static async Task ProcessingExample(IQueueService queueService, IQueueProcessor processor)
    {
        // Pobierz nastêpne zadanie
        var task = await queueService.DequeueAsync();
        if (task == null)
            return; // Brak zadañ

        try
        {
            // Oznacz jako przetwarzane
            await queueService.MarkAsProcessingAsync(task.Id);

            // Przetwórz zadanie
            var success = await processor.ProcessAsync(task);

            if (success)
            {
                await queueService.MarkAsCompletedAsync(task.Id);
            }
            else
            {
                await queueService.MarkAsFailedAsync(task.Id, "Processing returned false");
            }
        }
        catch (Exception ex)
        {
            await queueService.MarkAsFailedAsync(task.Id, ex.Message, ex.StackTrace);
        }
    }
}

/// <summary>
/// Przyk³ad implementacji niestandardowego procesora
/// </summary>
public class MyCustomProcessor : IQueueProcessor
{
    private readonly ILogger<MyCustomProcessor> _logger;

    public MyCustomProcessor(ILogger<MyCustomProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Przetwarzanie zadania {Id} typu {Type}", item.Id, item.Type);

        try
        {
            switch (item.Type.ToLowerInvariant())
            {
                case "email":
                    return await ProcessEmailTask(item, cancellationToken);
                
                case "webhook":
                    return await ProcessWebhookTask(item, cancellationToken);
                
                default:
                    _logger.LogWarning("Nieznany typ zadania: {Type}", item.Type);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B³¹d podczas przetwarzania zadania {Id}", item.Id);
            return false;
        }
    }

    private async Task<bool> ProcessEmailTask(QueueItem item, CancellationToken cancellationToken)
    {
        // Implementacja wysy³ania email
        _logger.LogInformation("Wysy³anie emaila dla zadania {Id}", item.Id);
        
        // Symulacja pracy
        await Task.Delay(1000, cancellationToken);
        
        return true; // Zwróæ true jeœli wys³ano pomyœlnie
    }

    private async Task<bool> ProcessWebhookTask(QueueItem item, CancellationToken cancellationToken)
    {
        // Implementacja wywo³ania webhook
        _logger.LogInformation("Wywo³anie webhook dla zadania {Id}", item.Id);
        
        // Symulacja pracy
        await Task.Delay(500, cancellationToken);
        
        return true; // Zwróæ true jeœli wywo³ano pomyœlnie
    }
}