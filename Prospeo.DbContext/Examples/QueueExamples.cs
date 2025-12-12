using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Prospeo.DbContext.Extensions;
using Prospeo.DbContext.Services;
using Prospeo.DbContext.Models;
using Prospeo.DbContext.DTOs;
using System.Text.Json;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Przyk³ady u¿ycia modelu Queue i serwisu QueueService
/// </summary>
public static class QueueExamples
{
    /// <summary>
    /// Przyk³ad podstawowego u¿ycia serwisu kolejki
    /// </summary>
    public static async Task BasicUsageExample(IQueueService queueService, IFirmyService firmyService)
    {
        // Pobierz firmê do testów
        var firmy = await firmyService.GetAllAsync();
        var firma = firmy.FirstOrDefault();
        if (firma == null)
        {
            Console.WriteLine("Brak firm w bazie danych - dodaj najpierw firmê");
            return;
        }

        // Dodawanie nowych zadañ do kolejki
        var webhookRequest = new
        {
            Url = "https://api.example.com/webhook",
            Method = "POST",
            Headers = new { Authorization = "Bearer token123" },
            Payload = new { OrderId = 12345, Status = "completed" }
        };

        var queueTask1 = new Queue
        {
            FirmaId = firma.Id,
            Scope = 1, // Scope dla webhook
            Request = JsonSerializer.Serialize(webhookRequest),
            Flg = 0, // Status pocz¹tkowy
            Description = "Webhook notification for order completion",
            TargetID = 12345
        };

        var dataSync = new
        {
            Type = "ProductSync",
            ProductIds = new[] { 1, 2, 3, 4, 5 },
            Action = "Update",
            Timestamp = DateTime.UtcNow
        };

        var queueTask2 = new Queue
        {
            FirmaId = firma.Id,
            Scope = 2, // Scope dla synchronizacji danych
            Request = JsonSerializer.Serialize(dataSync),
            Flg = 0,
            Description = "Product data synchronization",
            TargetID = 0
        };

        try
        {
            var dodane1 = await queueService.AddAsync(queueTask1);
            var dodane2 = await queueService.AddAsync(queueTask2);

            Console.WriteLine($"Dodano zadanie webhook z ID: {dodane1.Id}");
            Console.WriteLine($"Dodano zadanie synchronizacji z ID: {dodane2.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"B³¹d dodawania zadania: {ex.Message}");
        }

        // Pobieranie zadañ z kolejki
        var wszystkieZadania = await queueService.GetAllAsync();
        Console.WriteLine($"Liczba wszystkich zadañ: {wszystkieZadania.Count()}");

        // Pobieranie zadañ dla konkretnej firmy
        var zadaniaFirmy = await queueService.GetByFirmaIdAsync(firma.Id);
        Console.WriteLine($"Liczba zadañ dla firmy {firma.NazwaFirmy}: {zadaniaFirmy.Count()}");

        // Pobieranie zadañ wed³ug zakresu
        var zadaniaWebhook = await queueService.GetByScopeAsync(1);
        Console.WriteLine($"Liczba zadañ webhook (scope=1): {zadaniaWebhook.Count()}");
    }

    /// <summary>
    /// Przyk³ad wyszukiwania i filtrowania
    /// </summary>
    public static async Task SearchExample(IQueueService queueService)
    {
        // Wyszukiwanie wed³ug flagi
        var zadaniaZFlaga0 = await queueService.GetByFlagAsync(0);
        Console.WriteLine($"Zadania z flag¹ 0: {zadaniaZFlaga0.Count()}");

        // Wyszukiwanie wed³ug TargetID
        var zadaniaTarget = await queueService.GetByTargetIdAsync(12345);
        Console.WriteLine($"Zadania z TargetID 12345: {zadaniaTarget.Count()}");

        // Wyszukiwanie wed³ug zakresu dat (ostatnie 24 godziny)
        var wczoraj = DateTime.UtcNow.AddDays(-1);
        var teraz = DateTime.UtcNow;
        var zadaniaOstatnichDni = await queueService.GetByDateRangeAsync(wczoraj, teraz);
        Console.WriteLine($"Zadania z ostatnich 24 godzin: {zadaniaOstatnichDni.Count()}");

        // Stronicowanie
        var pierwszaStrona = await queueService.GetPagedAsync(0, 10);
        Console.WriteLine($"Pierwsza strona (10 elementów): {pierwszaStrona.Count()}");

        // Liczba zadañ
        var liczbaCa³kowita = await queueService.GetCountAsync();
        Console.WriteLine($"Ca³kowita liczba zadañ w kolejce: {liczbaCa³kowita}");
    }

    /// <summary>
    /// Przyk³ad aktualizacji i zarz¹dzania zadaniami
    /// </summary>
    public static async Task ManagementExample(IQueueService queueService)
    {
        // Pobierz pierwsze zadanie do aktualizacji
        var zadania = await queueService.GetPagedAsync(0, 1);
        var zadanie = zadania.FirstOrDefault();
        
        if (zadanie != null)
        {
            Console.WriteLine($"Aktualizacja zadania ID: {zadanie.Id}");

            // Zmieñ flagê na "w trakcie przetwarzania"
            zadanie.Flg = 1;
            zadanie.Description = "Updated: " + zadanie.Description;

            var zaktualizowano = await queueService.UpdateAsync(zadanie);
            Console.WriteLine($"Zadanie zaktualizowane: {zaktualizowano}");

            // Pobierz szczegó³y zaktualizowanego zadania
            var szczegoly = await queueService.GetByIdAsync(zadanie.Id);
            if (szczegoly != null)
            {
                Console.WriteLine($"Zaktualizowany opis: {szczegoly.Description}");
                Console.WriteLine($"Data modyfikacji: {szczegoly.DateModDateTime}");
            }
        }

        // Przyk³ad usuwania starych zadañ (starszych ni¿ 30 dni)
        var usunieteStare = await queueService.DeleteOldItemsAsync(30);
        Console.WriteLine($"Usuniêto {usunieteStare} starych zadañ");
    }

    /// <summary>
    /// Przyk³ad pracy z timestampami Unix
    /// </summary>
    public static void TimestampExample()
    {
        Console.WriteLine("=== Przyk³ad pracy z timestampami Unix ===");

        var queue = new Queue
        {
            FirmaId = 1,
            Scope = 1,
            Request = "{}",
            Description = "Test task",
            TargetID = 1
        };

        // Ustawienie daty przez w³aœciwoœæ DateTime
        queue.DateAddDateTime = DateTime.UtcNow;
        queue.DateModDateTime = DateTime.UtcNow;

        Console.WriteLine($"Data dodania (DateTime): {queue.DateAddDateTime}");
        Console.WriteLine($"Data dodania (Unix): {queue.DateAdd}");
        Console.WriteLine($"Data modyfikacji (DateTime): {queue.DateModDateTime}");
        Console.WriteLine($"Data modyfikacji (Unix): {queue.DateMod}");

        // Konwersja z Unix timestamp
        var unixTimestamp = 1640995200; // 1 stycznia 2022 00:00:00 UTC
        queue.DateAdd = unixTimestamp;
        Console.WriteLine($"Unix {unixTimestamp} = {queue.DateAddDateTime}");
    }

    /// <summary>
    /// Przyk³ad mapowania do DTOs
    /// </summary>
    public static async Task DtoMappingExample(IQueueService queueService)
    {
        var zadania = await queueService.GetPagedAsync(0, 5);
        
        Console.WriteLine("=== Mapowanie do DTOs ===");
        
        foreach (var zadanie in zadania)
        {
            var dto = QueueMappingExamples.MapToDto(zadanie);
            Console.WriteLine($"Zadanie {dto.Id}: {dto.Description}");
            Console.WriteLine($"  Firma: {dto.Firma?.NazwaFirmy ?? "Nieznana"}");
            Console.WriteLine($"  Request preview: {dto.RequestPreview}");
            Console.WriteLine($"  Data: {dto.DateAdd}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Przyk³ad integracji z webhook systemem
    /// </summary>
    public static async Task WebhookIntegrationExample(IQueueService queueService, IFirmyService firmyService)
    {
        Console.WriteLine("=== Przyk³ad integracji z systemem webhook ===");

        // Symulacja odebrania webhook od klienta
        var webhookPayload = new
        {
            EventType = "order.completed",
            OrderId = 67890,
            CustomerId = 12345,
            Timestamp = DateTime.UtcNow,
            Data = new
            {
                OrderTotal = 299.99,
                Items = new[]
                {
                    new { ProductId = 1, Quantity = 2 },
                    new { ProductId = 5, Quantity = 1 }
                }
            }
        };

        // ZnajdŸ firmê po kluczu API (symulacja)
        var firmy = await firmyService.GetAllAsync();
        var firma = firmy.FirstOrDefault(f => f.ApiKey != null);
        
        if (firma != null)
        {
            // Dodaj zadanie do kolejki dla dalszego przetwarzania
            var queueTask = new Queue
            {
                FirmaId = firma.Id,
                Scope = 100, // Scope dla webhook events
                Request = JsonSerializer.Serialize(webhookPayload),
                Flg = 0, // Oczekuje na przetworzenie
                Description = $"Webhook event: {webhookPayload.EventType} for order {webhookPayload.OrderId}",
                TargetID = webhookPayload.OrderId
            };

            var dodane = await queueService.AddAsync(queueTask);
            Console.WriteLine($"Dodano zadanie webhook do kolejki: ID {dodane.Id}");
            
            // Symulacja przetwarzania
            await Task.Delay(100); // Symulacja czasu przetwarzania
            
            // Oznacz jako przetworzone
            dodane.Flg = 2; // Zakoñczone pomyœlnie
            await queueService.UpdateAsync(dodane);
            
            Console.WriteLine($"Zadanie {dodane.Id} zosta³o przetworzone");
        }
    }
}

/// <summary>
/// Przyk³ad mapowania miêdzy modelami a DTOs dla Queue
/// </summary>
public static class QueueMappingExamples
{
    /// <summary>
    /// Mapowanie z modelu do DTO
    /// </summary>
    public static QueueDto MapToDto(Queue queue)
    {
        return new QueueDto
        {
            Id = queue.Id,
            RowID = queue.RowID,
            FirmaId = queue.FirmaId,
            Firma = queue.Firma != null ? new FirmaDto
            {
                Id = queue.Firma.Id,
                RowID = queue.Firma.RowID,
                NazwaFirmy = queue.Firma.NazwaFirmy,
                NazwaBazyERP = queue.Firma.NazwaBazyERP,
                CzyTestowa = queue.Firma.CzyTestowa,
                MaApiKey = !string.IsNullOrWhiteSpace(queue.Firma.ApiKey),
                AuthorizeAllEndpoints = queue.Firma.AuthorizeAllEndpoints
            } : null,
            Scope = queue.Scope,
            RequestPreview = queue.Request.Length > 100 
                ? queue.Request.Substring(0, 100) + "..." 
                : queue.Request,
            DateAdd = queue.DateAddDateTime,
            DateMod = queue.DateModDateTime,
            Flg = queue.Flg,
            Description = queue.Description,
            TargetID = queue.TargetID
        };
    }

    /// <summary>
    /// Mapowanie z CreateDTO do modelu
    /// </summary>
    public static Queue MapFromCreateDto(CreateQueueDto dto)
    {
        return new Queue
        {
            FirmaId = dto.FirmaId,
            Scope = dto.Scope,
            Request = dto.Request,
            Flg = dto.Flg,
            Description = dto.Description,
            TargetID = dto.TargetID
        };
    }

    /// <summary>
    /// Aktualizacja modelu z UpdateDTO
    /// </summary>
    public static void UpdateFromDto(Queue queue, UpdateQueueDto dto)
    {
        queue.FirmaId = dto.FirmaId;
        queue.Scope = dto.Scope;
        queue.Request = dto.Request;
        queue.Flg = dto.Flg;
        queue.Description = dto.Description;
        queue.TargetID = dto.TargetID;
    }

    /// <summary>
    /// Mapowanie do szczegó³owego DTO (z pe³nym Request)
    /// </summary>
    public static QueueDetailDto MapToDetailDto(Queue queue)
    {
        var baseDto = MapToDto(queue);
        return new QueueDetailDto
        {
            Id = baseDto.Id,
            RowID = baseDto.RowID,
            FirmaId = baseDto.FirmaId,
            Firma = baseDto.Firma,
            Scope = baseDto.Scope,
            RequestPreview = baseDto.RequestPreview,
            DateAdd = baseDto.DateAdd,
            DateMod = baseDto.DateMod,
            Flg = baseDto.Flg,
            Description = baseDto.Description,
            TargetID = baseDto.TargetID,
            FullRequest = queue.Request
        };
    }
}