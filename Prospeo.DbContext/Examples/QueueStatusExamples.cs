using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Prospeo.DbContext.Extensions;
using Prospeo.DbContext.Services;
using Prospeo.DbContext.Models;
using Prospeo.DbContext.DTOs;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Definicja enum dla statusów kolejki (mo¿e byæ w osobnym pliku)
/// </summary>
public enum QueueItemStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// Przyk³ady u¿ycia modelu QueueStatus i serwisu QueueStatusService
/// </summary>
public static class QueueStatusExamples
{
    /// <summary>
    /// Przyk³ad podstawowego u¿ycia serwisu statusów kolejki
    /// </summary>
    public static async Task BasicUsageExample(IQueueStatusService queueStatusService)
    {
        // Dodawanie nowych statusów kolejki
        var statusPending = new QueueStatus
        {
            Name = "Pending",
            Value = 0,
            Description = "Zadanie oczekuje na przetworzenie"
        };

        var statusProcessing = new QueueStatus
        {
            Name = "Processing",
            Value = 1,
            Description = "Zadanie jest aktualnie przetwarzane"
        };

        var statusCompleted = new QueueStatus
        {
            Name = "Completed",
            Value = 2,
            Description = "Zadanie zosta³o pomyœlnie zakoñczone"
        };

        var statusFailed = new QueueStatus
        {
            Name = "Failed",
            Value = 3,
            Description = "Zadanie zakoñczy³o siê niepowodzeniem"
        };

        try
        {
            var dodanyPending = await queueStatusService.AddAsync(statusPending);
            var dodanyProcessing = await queueStatusService.AddAsync(statusProcessing);
            var dodanyCompleted = await queueStatusService.AddAsync(statusCompleted);
            var dodanyFailed = await queueStatusService.AddAsync(statusFailed);

            Console.WriteLine($"Dodano status: {dodanyPending.Name} (ID: {dodanyPending.Id})");
            Console.WriteLine($"Dodano status: {dodanyProcessing.Name} (ID: {dodanyProcessing.Id})");
            Console.WriteLine($"Dodano status: {dodanyCompleted.Name} (ID: {dodanyCompleted.Id})");
            Console.WriteLine($"Dodano status: {dodanyFailed.Name} (ID: {dodanyFailed.Id})");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"B³¹d dodawania statusu: {ex.Message}");
        }

        // Pobieranie statusu po wartoœci
        var statusPoWartosci = await queueStatusService.GetByValueAsync(1);
        Console.WriteLine($"Status o wartoœci 1: {statusPoWartosci?.Name}");

        // Pobieranie statusu po nazwie
        var statusPoNazwie = await queueStatusService.GetByNameAsync("Completed");
        Console.WriteLine($"Status 'Completed': {statusPoNazwie?.Description}");

        // Pobieranie wszystkich statusów
        var wszystkieStatusy = await queueStatusService.GetAllAsync();
        Console.WriteLine($"Liczba wszystkich statusów: {wszystkieStatusy.Count()}");
    }

    /// <summary>
    /// Przyk³ad walidacji i obs³ugi b³êdów
    /// </summary>
    public static async Task ValidationExample(IQueueStatusService queueStatusService)
    {
        try
        {
            // Próba dodania statusu z duplikatem wartoœci
            var status1 = new QueueStatus
            {
                Name = "Status1",
                Value = 100,
                Description = "Pierwszy status"
            };

            var status2 = new QueueStatus
            {
                Name = "Status2",
                Value = 100, // Duplikat wartoœci!
                Description = "Drugi status"
            };

            await queueStatusService.AddAsync(status1);
            await queueStatusService.AddAsync(status2); // To rzuci wyj¹tek
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"B³¹d walidacji wartoœci: {ex.Message}");
        }

        try
        {
            // Próba dodania statusu z duplikatem nazwy
            var status3 = new QueueStatus
            {
                Name = "Pending", // Duplikat nazwy!
                Value = 200,
                Description = "Inny opis"
            };

            await queueStatusService.AddAsync(status3); // To te¿ rzuci wyj¹tek
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"B³¹d walidacji nazwy: {ex.Message}");
        }
    }

    /// <summary>
    /// Przyk³ad wyszukiwania i sprawdzania unikalnoœci
    /// </summary>
    public static async Task SearchExample(IQueueStatusService queueStatusService)
    {
        // SprawdŸ czy wartoœæ jest unikalna
        var czyUnikatnaWartosc = await queueStatusService.IsValueUniqueAsync(999);
        Console.WriteLine($"Czy wartoœæ 999 jest unikalna: {czyUnikatnaWartosc}");

        // SprawdŸ czy nazwa jest unikalna
        var czyUnikatnaNazwa = await queueStatusService.IsNameUniqueAsync("NewStatus");
        Console.WriteLine($"Czy nazwa 'NewStatus' jest unikalna: {czyUnikatnaNazwa}");

        // Pobierz status po ID
        var statusPoId = await queueStatusService.GetByIdAsync(1);
        if (statusPoId != null)
        {
            Console.WriteLine($"Status ID 1: {statusPoId.Name} - {statusPoId.Description}");
        }

        // SprawdŸ czy status istnieje
        var czyIstnieje = await queueStatusService.ExistsAsync(1);
        Console.WriteLine($"Czy status o ID 1 istnieje: {czyIstnieje}");
    }

    /// <summary>
    /// Przyk³ad aktualizacji statusu
    /// </summary>
    public static async Task UpdateExample(IQueueStatusService queueStatusService)
    {
        // Pobierz status do aktualizacji
        var status = await queueStatusService.GetByNameAsync("Pending");
        if (status != null)
        {
            // Aktualizuj opis
            status.Description = "Zaktualizowany opis: zadanie oczekuje w kolejce na przetworzenie";

            try
            {
                var zaktualizowano = await queueStatusService.UpdateAsync(status);
                Console.WriteLine($"Status zaktualizowany: {zaktualizowano}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"B³¹d aktualizacji: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Przyk³ad u¿ycia z enum (mapowanie na statusy z bazy)
    /// </summary>
    public static async Task EnumMappingExample(IQueueStatusService queueStatusService)
    {
        // Pobierz status z bazy na podstawie enum
        var enumStatus = QueueItemStatus.Processing;
        var statusZBazy = await queueStatusService.GetByValueAsync((int)enumStatus);
        
        if (statusZBazy != null)
        {
            Console.WriteLine($"Status {enumStatus}: {statusZBazy.Description}");
        }

        // SprawdŸ czy wszystkie statusy enum istniej¹ w bazie
        var enumValues = Enum.GetValues<QueueItemStatus>();
        foreach (var enumValue in enumValues)
        {
            var istnieje = await queueStatusService.GetByValueAsync((int)enumValue) != null;
            Console.WriteLine($"Status {enumValue} ({(int)enumValue}) istnieje w bazie: {istnieje}");
        }
    }
}

/// <summary>
/// Przyk³ad mapowania miêdzy modelami a DTOs dla QueueStatus
/// </summary>
public static class QueueStatusMappingExamples
{
    /// <summary>
    /// Mapowanie z modelu do DTO
    /// </summary>
    public static QueueStatusDto MapToDto(QueueStatus queueStatus)
    {
        return new QueueStatusDto
        {
            Id = queueStatus.Id,
            RowID = queueStatus.RowID,
            Name = queueStatus.Name,
            Value = queueStatus.Value,
            Description = queueStatus.Description
        };
    }

    /// <summary>
    /// Mapowanie z CreateDTO do modelu
    /// </summary>
    public static QueueStatus MapFromCreateDto(CreateQueueStatusDto dto)
    {
        return new QueueStatus
        {
            Name = dto.Name,
            Value = dto.Value,
            Description = dto.Description
        };
    }

    /// <summary>
    /// Aktualizacja modelu z UpdateDTO
    /// </summary>
    public static void UpdateFromDto(QueueStatus queueStatus, UpdateQueueStatusDto dto)
    {
        queueStatus.Name = dto.Name;
        queueStatus.Value = dto.Value;
        queueStatus.Description = dto.Description;
    }

    /// <summary>
    /// Mapowanie do lookup DTO (dla list wyboru)
    /// </summary>
    public static QueueStatusLookupDto MapToLookupDto(QueueStatus queueStatus)
    {
        return new QueueStatusLookupDto
        {
            Value = queueStatus.Value,
            Name = queueStatus.Name,
            Description = queueStatus.Description
        };
    }
}