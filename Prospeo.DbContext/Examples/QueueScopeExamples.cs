using Prospeo.DbContext.Data;
using Prospeo.DbContext.Enums;
using Microsoft.EntityFrameworkCore;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Przykłady użycia QueueScope i QueueStatusEnum enum
/// </summary>
public static class QueueScopeExamples
{
    /// <summary>
    /// Przykład 1: Filtrowanie kolejki po typie Scope i statusie
    /// </summary>
    public static async Task Example1_FilterByScopeAndStatus()
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz wszystkie zadania towarowe oczekujące na przetworzenie
        var pendingTowarTasks = await context.Queues
            .Where(q => q.Scope == (int)QueueScope.Towar 
                     && q.Flg == (int)QueueStatusEnum.Pending)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {pendingTowarTasks.Count} zadań typu Towar oczekujących na przetworzenie");

        // Wyświetl szczegóły
        foreach (var task in pendingTowarTasks.Take(10))
        {
            Console.WriteLine($"ID: {task.Id}, Scope: {task.ScopeEnum}, Status: {task.FlgEnum}, TargetID: {task.TargetID}");
        }
    }

    /// <summary>
    /// Przykład 2: Filtrowanie po firmie, typie i statusie
    /// </summary>
    public static async Task Example2_FilterByCompanyScopeAndStatus(int companyId)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz zadania towarowe oczekujące dla konkretnej firmy
        var towarTasksForCompany = await context.Queues
            .Where(q => q.FirmaId == companyId 
                     && q.Scope == (int)QueueScope.Towar
                     && q.Flg == (int)QueueStatusEnum.Pending)
            .OrderBy(q => q.DateAdd)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {towarTasksForCompany.Count} zadań oczekujących dla firmy {companyId}");
    }

    /// <summary>
    /// Przykład 3: Tworzenie nowego zadania z enumami
    /// </summary>
    public static async Task Example3_CreateQueueItemWithEnums(int companyId, int targetId, string request)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var newQueueItem = new Prospeo.DbContext.Models.Queue
        {
            FirmaId = companyId,
            ScopeEnum = QueueScope.Towar,
            FlgEnum = QueueStatusEnum.Pending,
            Request = request,
            DateAdd = (int)now,
            DateMod = (int)now,
            Description = "Zadanie utworzone z enumami",
            TargetID = targetId
        };

        context.Queues.Add(newQueueItem);
        await context.SaveChangesAsync();

        Console.WriteLine($"Utworzono nowe zadanie: ID={newQueueItem.Id}, Scope={newQueueItem.ScopeEnum}, Status={newQueueItem.FlgEnum}");
    }

    /// <summary>
    /// Przykład 4: Przetwarzanie zadania ze zmianą statusu
    /// </summary>
    public static async Task Example4_ProcessTaskWithStatusChange(int queueId)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var queueItem = await context.Queues.FindAsync(queueId);
        if (queueItem == null)
        {
            Console.WriteLine($"Zadanie o ID {queueId} nie zostało znalezione");
            return;
        }

        try
        {
            // Ustaw status "w trakcie"
            queueItem.FlgEnum = QueueStatusEnum.Processing;
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Rozpoczęto przetwarzanie zadania {queueId}");

            // Symulacja przetwarzania
            await Task.Delay(1000);

            // Ustaw status "zakończone"
            queueItem.FlgEnum = QueueStatusEnum.Completed;
            queueItem.Description = "Zadanie zakończone pomyślnie";
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Zadanie {queueId} zakończone pomyślnie");
        }
        catch (Exception ex)
        {
            // Ustaw status "błąd"
            queueItem.FlgEnum = QueueStatusEnum.Error;
            queueItem.Description = $"Błąd: {ex.Message}";
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Zadanie {queueId} zakończone z błędem: {ex.Message}");
        }
    }

    /// <summary>
    /// Przykład 5: Grupowanie zadań według statusu
    /// </summary>
    public static async Task Example5_GroupByStatus()
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Grupowanie zadań według statusu
        var groupedTasks = await context.Queues
            .GroupBy(q => q.Flg)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                OldestDate = g.Min(q => q.DateAdd),
                NewestDate = g.Max(q => q.DateAdd)
            })
            .ToListAsync();

        Console.WriteLine("Statystyki zadań według statusu:");
        foreach (var group in groupedTasks)
        {
            var statusName = Enum.IsDefined(typeof(QueueStatusEnum), group.Status)
                ? ((QueueStatusEnum)group.Status).ToString()
                : $"Unknown ({group.Status})";

            Console.WriteLine($"Status: {statusName}, Liczba: {group.Count}");
        }
    }

    /// <summary>
    /// Przykład 6: Pobieranie zadań do przetworzenia (jak w Worker)
    /// </summary>
    public static async Task<List<Prospeo.DbContext.Models.Queue>> Example6_GetTasksForProcessing(
        int companyId, 
        int count = 10)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz zadania towarowe oczekujące na przetworzenie
        return await context.Queues
            .Where(q => q.FirmaId == companyId 
                     && q.Scope == (int)QueueScope.Towar
                     && q.Flg == (int)QueueStatusEnum.Pending)
            .OrderBy(q => q.DateAdd)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Przykład 7: Ponowne przetwarzanie zadań z błędami
    /// </summary>
    public static async Task Example7_RetryErrorTasks(int companyId, int olderThanHours = 1)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-olderThanHours).ToUnixTimeSeconds();

        // Znajdź zadania z błędami starsze niż określony czas
        var errorTasks = await context.Queues
            .Where(q => q.FirmaId == companyId
                     && q.FlgEnum == QueueStatusEnum.Error
                     && q.DateMod < cutoffTime)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {errorTasks.Count} zadań z błędami do ponowienia");

        foreach (var task in errorTasks)
        {
            // Reset statusu do Pending
            task.FlgEnum = QueueStatusEnum.Pending;
            task.Description = $"Ponowienie próby (poprzedni błąd: {task.Description})";
            task.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Zresetowano {errorTasks.Count} zadań do statusu Pending");
    }

    /// <summary>
    /// Przykład 8: Raport statusów zadań
    /// </summary>
    public static async Task Example8_StatusReport(int companyId)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var report = await context.Queues
            .Where(q => q.FirmaId == companyId)
            .GroupBy(q => new { q.Scope, q.Flg })
            .Select(g => new
            {
                Scope = g.Key.Scope,
                Status = g.Key.Flg,
                Count = g.Count()
            })
            .ToListAsync();

        Console.WriteLine($"Raport zadań dla firmy {companyId}:");
        Console.WriteLine("─────────────────────────────────────");

        foreach (var item in report)
        {
            var scopeName = Enum.IsDefined(typeof(QueueScope), item.Scope)
                ? ((QueueScope)item.Scope).ToString()
                : $"Unknown({item.Scope})";

            var statusName = Enum.IsDefined(typeof(QueueStatusEnum), item.Status)
                ? ((QueueStatusEnum)item.Status).ToString()
                : $"Unknown({item.Status})";

            Console.WriteLine($"{scopeName,-15} | {statusName,-12} | {item.Count,5} zadań");
        }
    }
}
