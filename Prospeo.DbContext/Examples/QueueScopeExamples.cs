using Prospeo.DbContext.Data;
using Prospeo.DbContext.Enums;
using Microsoft.EntityFrameworkCore;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Przyk³ady u¿ycia QueueScope i QueueStatus enum
/// </summary>
public static class QueueScopeExamples
{
    /// <summary>
    /// Przyk³ad 1: Filtrowanie kolejki po typie Scope i statusie
    /// </summary>
    public static async Task Example1_FilterByScopeAndStatus()
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz wszystkie zadania towarowe oczekuj¹ce na przetworzenie
        var pendingTowarTasks = await context.Queues
            .Where(q => q.Scope == (int)QueueScope.Towar 
                     && q.Flg == (int)QueueStatus.Pending)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {pendingTowarTasks.Count} zadañ typu Towar oczekuj¹cych na przetworzenie");

        // Wyœwietl szczegó³y
        foreach (var task in pendingTowarTasks.Take(10))
        {
            Console.WriteLine($"ID: {task.Id}, Scope: {task.ScopeEnum}, Status: {task.FlgEnum}, TargetID: {task.TargetID}");
        }
    }

    /// <summary>
    /// Przyk³ad 2: Filtrowanie po firmie, typie i statusie
    /// </summary>
    public static async Task Example2_FilterByCompanyScopeAndStatus(int companyId)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz zadania towarowe oczekuj¹ce dla konkretnej firmy
        var towarTasksForCompany = await context.Queues
            .Where(q => q.FirmaId == companyId 
                     && q.Scope == (int)QueueScope.Towar
                     && q.Flg == (int)QueueStatus.Pending)
            .OrderBy(q => q.DateAdd)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {towarTasksForCompany.Count} zadañ oczekuj¹cych dla firmy {companyId}");
    }

    /// <summary>
    /// Przyk³ad 3: Tworzenie nowego zadania z enumami
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
            FlgEnum = QueueStatus.Pending,
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
    /// Przyk³ad 4: Przetwarzanie zadania ze zmian¹ statusu
    /// </summary>
    public static async Task Example4_ProcessTaskWithStatusChange(int queueId)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var queueItem = await context.Queues.FindAsync(queueId);
        if (queueItem == null)
        {
            Console.WriteLine($"Zadanie o ID {queueId} nie zosta³o znalezione");
            return;
        }

        try
        {
            // Ustaw status "w trakcie"
            queueItem.FlgEnum = QueueStatus.Processing;
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Rozpoczêto przetwarzanie zadania {queueId}");

            // Symulacja przetwarzania
            await Task.Delay(1000);

            // Ustaw status "zakoñczone"
            queueItem.FlgEnum = QueueStatus.Completed;
            queueItem.Description = "Zadanie zakoñczone pomyœlnie";
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Zadanie {queueId} zakoñczone pomyœlnie");
        }
        catch (Exception ex)
        {
            // Ustaw status "b³¹d"
            queueItem.FlgEnum = QueueStatus.Error;
            queueItem.Description = $"B³¹d: {ex.Message}";
            queueItem.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await context.SaveChangesAsync();

            Console.WriteLine($"Zadanie {queueId} zakoñczone z b³êdem: {ex.Message}");
        }
    }

    /// <summary>
    /// Przyk³ad 5: Grupowanie zadañ wed³ug statusu
    /// </summary>
    public static async Task Example5_GroupByStatus()
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Grupowanie zadañ wed³ug statusu
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

        Console.WriteLine("Statystyki zadañ wed³ug statusu:");
        foreach (var group in groupedTasks)
        {
            var statusName = Enum.IsDefined(typeof(QueueStatus), group.Status)
                ? ((QueueStatus)group.Status).ToString()
                : $"Unknown ({group.Status})";

            Console.WriteLine($"Status: {statusName}, Liczba: {group.Count}");
        }
    }

    /// <summary>
    /// Przyk³ad 6: Pobieranie zadañ do przetworzenia (jak w Worker)
    /// </summary>
    public static async Task<List<Prospeo.DbContext.Models.Queue>> Example6_GetTasksForProcessing(
        int companyId, 
        int count = 10)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        // Pobierz zadania towarowe oczekuj¹ce na przetworzenie
        return await context.Queues
            .Where(q => q.FirmaId == companyId 
                     && q.Scope == (int)QueueScope.Towar
                     && q.Flg == (int)QueueStatus.Pending)
            .OrderBy(q => q.DateAdd)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Przyk³ad 7: Ponowne przetwarzanie zadañ z b³êdami
    /// </summary>
    public static async Task Example7_RetryErrorTasks(int companyId, int olderThanHours = 1)
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        using var context = new ProspeoDataContext(connectionString);

        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-olderThanHours).ToUnixTimeSeconds();

        // ZnajdŸ zadania z b³êdami starsze ni¿ okreœlony czas
        var errorTasks = await context.Queues
            .Where(q => q.FirmaId == companyId
                     && q.FlgEnum == QueueStatus.Error
                     && q.DateMod < cutoffTime)
            .ToListAsync();

        Console.WriteLine($"Znaleziono {errorTasks.Count} zadañ z b³êdami do ponowienia");

        foreach (var task in errorTasks)
        {
            // Reset statusu do Pending
            task.FlgEnum = QueueStatus.Pending;
            task.Description = $"Ponowienie próby (poprzedni b³¹d: {task.Description})";
            task.DateMod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Zresetowano {errorTasks.Count} zadañ do statusu Pending");
    }

    /// <summary>
    /// Przyk³ad 8: Raport statusów zadañ
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

        Console.WriteLine($"Raport zadañ dla firmy {companyId}:");
        Console.WriteLine("?????????????????????????????????????");

        foreach (var item in report)
        {
            var scopeName = Enum.IsDefined(typeof(QueueScope), item.Scope)
                ? ((QueueScope)item.Scope).ToString()
                : $"Unknown({item.Scope})";

            var statusName = Enum.IsDefined(typeof(QueueStatus), item.Status)
                ? ((QueueStatus)item.Status).ToString()
                : $"Unknown({item.Status})";

            Console.WriteLine($"{scopeName,-15} | {statusName,-12} | {item.Count,5} zadañ");
        }
    }
}
