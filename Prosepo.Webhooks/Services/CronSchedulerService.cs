using System.Collections.Concurrent;
using System.Text.Json;
using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Services
{
    /// <summary>
    /// Serwis zarz¹dzaj¹cy zadaniami cyklicznymi (cron jobs).
    /// Automatycznie uruchamia zadania zgodnie z ich harmonogramem i zarz¹dza ich cyklem ¿ycia.
    /// Implementuje IHostedService co oznacza, ¿e uruchamia siê wraz z aplikacj¹.
    /// </summary>
    public class CronSchedulerService : IHostedService, IDisposable
    {
        private readonly ILogger<CronSchedulerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        /// <summary>
        /// Thread-safe kolekcja przechowuj¹ca wszystkie zaplanowane zadania.
        /// Klucz: ID zadania, Wartoœæ: obiekt ScheduledJob z pe³nymi informacjami.
        /// </summary>
        private readonly ConcurrentDictionary<string, ScheduledJob> _scheduledJobs = new();
        
        /// <summary>
        /// Timer wykonuj¹cy sprawdzanie zadañ do wykonania co 10 sekund.
        /// Null podczas gdy serwis jest zatrzymany.
        /// </summary>
        private Timer? _timer;

        /// <summary>
        /// Inicjalizuje now¹ instancjê CronSchedulerService.
        /// </summary>
        /// <param name="logger">Logger do rejestrowania zdarzeñ serwisu</param>
        /// <param name="serviceProvider">Provider do tworzenia scope'ów dla dependency injection</param>
        public CronSchedulerService(ILogger<CronSchedulerService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Uruchamia serwis schedulera - wywo³ywane automatycznie przy starcie aplikacji.
        /// Inicjalizuje timer sprawdzaj¹cy zadania co 10 sekund.
        /// </summary>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cron Scheduler Service uruchomiony");
            
            // Timer sprawdza co 10 sekund czy s¹ zadania do wykonania
            // Pierwsze sprawdzenie natychmiast (TimeSpan.Zero), potem co 10 sekund
            _timer = new Timer(CheckAndExecuteJobs, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Zatrzymuje serwis schedulera - wywo³ywane automatycznie przy zatrzymaniu aplikacji.
        /// Zatrzymuje timer i zapobiega dalszemu wykonywaniu zadañ.
        /// </summary>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cron Scheduler Service zatrzymany");
            
            // Zatrzymanie timera - brak dalszych sprawdzeñ
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dodaje nowe zadanie lub aktualizuje istniej¹ce w schedulerze.
        /// Automatycznie oblicza czas nastêpnego wykonania na podstawie harmonogramu.
        /// </summary>
        /// <param name="jobId">Unikalny identyfikator zadania</param>
        /// <param name="schedule">Harmonogram zadania z parametrami HTTP</param>
        public void AddOrUpdateJob(string jobId, CronJobSchedule schedule)
        {
            // Tworzenie nowego obiektu zadania z obliczonym czasem nastêpnego wykonania
            var job = new ScheduledJob
            {
                Id = jobId,
                Schedule = schedule,
                NextExecution = CalculateNextExecution(schedule), // Obliczenie nastêpnego wykonania
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Thread-safe dodanie/aktualizacja zadania
            // Jeœli zadanie ju¿ istnieje, zachowuje statystyki wykonañ
            _scheduledJobs.AddOrUpdate(jobId, job, (key, old) =>
            {
                job.ExecutionCount = old.ExecutionCount; // Zachowanie liczby wykonañ
                job.LastExecution = old.LastExecution;   // Zachowanie czasu ostatniego wykonania
                job.CreatedAt = old.CreatedAt;           // Zachowanie oryginalnego czasu utworzenia
                return job;
            });

            _logger.LogInformation("Dodano/zaktualizowano zadanie cykliczne: {JobId}, nastêpne wykonanie: {NextExecution}", 
                jobId, job.NextExecution);
        }

        /// <summary>
        /// Usuwa zadanie ze schedulera.
        /// Po usuniêciu zadanie nie bêdzie ju¿ wykonywane.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania do usuniêcia</param>
        /// <returns>True jeœli zadanie zosta³o usuniête, false jeœli nie istnia³o</returns>
        public bool RemoveJob(string jobId)
        {
            if (_scheduledJobs.TryRemove(jobId, out var job))
            {
                _logger.LogInformation("Usuniêto zadanie cykliczne: {JobId}", jobId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pobiera wszystkie zadania z schedulera.
        /// Zwraca kopiê kolekcji - bezpieczne do iteracji.
        /// </summary>
        /// <returns>Lista wszystkich zaplanowanych zadañ</returns>
        public IEnumerable<ScheduledJob> GetAllJobs()
        {
            return _scheduledJobs.Values.ToList(); // Kopia - thread-safe
        }

        /// <summary>
        /// Pobiera konkretne zadanie po jego identyfikatorze.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania</param>
        /// <returns>Zadanie jeœli istnieje, null w przeciwnym przypadku</returns>
        public ScheduledJob? GetJob(string jobId)
        {
            _scheduledJobs.TryGetValue(jobId, out var job);
            return job;
        }

        /// <summary>
        /// Metoda wywo³ywana przez timer co 10 sekund.
        /// Sprawdza wszystkie zadania i wykonuje te, których czas nadszed³.
        /// Obs³uguje b³êdy wykonania bez zatrzymywania ca³ego schedulera.
        /// </summary>
        /// <param name="state">Parametr z timera (nie u¿ywany)</param>
        private async void CheckAndExecuteJobs(object? state)
        {
            var now = DateTime.UtcNow;
            
            // Znalezienie wszystkich aktywnych zadañ gotowych do wykonania
            var jobsToExecute = _scheduledJobs.Values
                .Where(job => job.IsActive && job.NextExecution <= now)
                .ToList();

            // Wykonanie ka¿dego zadania w pêtli
            foreach (var job in jobsToExecute)
            {
                try
                {
                    _logger.LogInformation("Wykonywanie zadania cyklicznego: {JobId}", job.Id);
                    
                    // Utworzenie scope'a dla dependency injection
                    // Ka¿de zadanie ma swój w³asny scope - izolacja zale¿noœci
                    using var scope = _serviceProvider.CreateScope();
                    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    
                    // Wykonanie w³aœciwego zadania HTTP
                    await ExecuteJob(job, httpClient, configuration);
                    
                    // Aktualizacja statystyk zadania - niezale¿nie od wyniku wykonania
                    job.LastExecution = now;
                    job.ExecutionCount++;
                    job.NextExecution = CalculateNextExecution(job.Schedule); // Obliczenie nastêpnego wykonania
                    
                    _logger.LogInformation("Zadanie {JobId} wykonane pomyœlnie. Nastêpne wykonanie: {NextExecution}", 
                        job.Id, job.NextExecution);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "B³¹d podczas wykonywania zadania cyklicznego: {JobId}", job.Id);
                    
                    // WA¯NE: Mimo b³êdu, planuj nastêpne wykonanie
                    // Scheduler jest odporny na b³êdy - jeden b³¹d nie zatrzymuje ca³ego systemu
                    job.LastExecution = now;
                    job.ExecutionCount++;
                    job.NextExecution = CalculateNextExecution(job.Schedule);
                }
            }
        }

        /// <summary>
        /// Wykonuje pojedyncze zadanie HTTP.
        /// Buduje ¿¹danie HTTP na podstawie parametrów w harmonogramie i wysy³a je.
        /// </summary>
        /// <param name="job">Zadanie do wykonania</param>
        /// <param name="httpClient">Klient HTTP do wykonania ¿¹dania</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <returns>Task reprezentuj¹cy wykonanie zadania</returns>
        private async Task ExecuteJob(ScheduledJob job, HttpClient httpClient, IConfiguration configuration)
        {
            // Tworzenie ¿¹dania HTTP na podstawie parametrów zadania
            var request = new HttpRequestMessage(new HttpMethod(job.Schedule.Request.Method), job.Schedule.Request.Url);

            // Dodanie nag³ówków HTTP (pomijaj¹c Content-Type - bêdzie ustawiony automatycznie)
            if (job.Schedule.Request.Headers != null)
            {
                foreach (var header in job.Schedule.Request.Headers)
                {
                    if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            // Automatyczna autoryzacja Olmed dla odpowiednich URL-i
            if (job.Schedule.Request.Url.Contains("grupaolmed.pl") && job.Schedule.Request.UseOlmedAuth)
            {
                // TODO: Integracja z tokenem Olmed z CronController
                // W przysz³oœci mo¿na rozwa¿yæ przeniesienie zarz¹dzania tokenami do osobnego serwisu
                _logger.LogInformation("Zadanie {JobId} wymaga autoryzacji Olmed", job.Id);
            }

            // Dodanie zawartoœci dla metod POST/PUT
            if (!string.IsNullOrEmpty(job.Schedule.Request.Body) && 
                (job.Schedule.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                 job.Schedule.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)))
            {
                var contentType = job.Schedule.Request.Headers?.GetValueOrDefault("Content-Type", "application/json") ?? "application/json";
                request.Content = new StringContent(job.Schedule.Request.Body, System.Text.Encoding.UTF8, contentType);
            }

            // Wykonanie ¿¹dania HTTP
            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Logowanie odpowiedzi (skrócone dla d³ugich odpowiedzi)
            var logContent = responseContent.Length > 1000 ? responseContent[..1000] + "..." : responseContent;
            _logger.LogInformation("OdpowiedŸ zadania {JobId}: {StatusCode} - {Content}", 
                job.Id, response.StatusCode, logContent);
        }

        /// <summary>
        /// Oblicza czas nastêpnego wykonania zadania na podstawie jego harmonogramu.
        /// Obs³uguje wszystkie typy harmonogramów: Interval, Daily, Weekly, Monthly, Cron.
        /// </summary>
        /// <param name="schedule">Harmonogram zadania</param>
        /// <returns>Czas nastêpnego wykonania (UTC)</returns>
        private DateTime CalculateNextExecution(CronJobSchedule schedule)
        {
            var now = DateTime.UtcNow;
            
            return schedule.Type switch
            {
                // Interwa³: dodaj X sekund do aktualnego czasu
                ScheduleType.Interval => now.AddSeconds(schedule.IntervalSeconds ?? 60),
                
                // Codziennie: nastêpny dzieñ o okreœlonej godzinie
                ScheduleType.Daily => now.Date.AddDays(1).AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0),
                
                // Tygodniowo: nastêpny okreœlony dzieñ tygodnia o okreœlonej godzinie
                ScheduleType.Weekly => CalculateNextWeekly(now, schedule),
                
                // Miesiêcznie: nastêpny miesi¹c w okreœlony dzieñ o okreœlonej godzinie
                ScheduleType.Monthly => CalculateNextMonthly(now, schedule),
                
                // Cron: parsowanie wyra¿enia cron (obecnie placeholder)
                ScheduleType.Cron => CalculateNextFromCron(now, schedule.CronExpression ?? "0 * * * *"),
                
                // Fallback: domyœlnie co 5 minut
                _ => now.AddMinutes(5)
            };
        }

        /// <summary>
        /// Oblicza nastêpne wykonanie dla harmonogramu tygodniowego.
        /// Znajduje najbli¿szy okreœlony dzieñ tygodnia o okreœlonej godzinie.
        /// </summary>
        /// <param name="now">Aktualny czas</param>
        /// <param name="schedule">Harmonogram z ustawionym DayOfWeek, Hour i Minute</param>
        /// <returns>Czas nastêpnego wykonania</returns>
        private DateTime CalculateNextWeekly(DateTime now, CronJobSchedule schedule)
        {
            var targetDay = schedule.DayOfWeek ?? DayOfWeek.Monday;
            
            // Obliczenie ile dni do nastêpnego wyst¹pienia docelowego dnia tygodnia
            var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;
            
            // Jeœli to dzisiaj, ale godzina ju¿ minê³a, przesuñ na nastêpny tydzieñ
            if (daysUntilTarget == 0 && 
                now.TimeOfDay > TimeSpan.FromHours(schedule.Hour ?? 0).Add(TimeSpan.FromMinutes(schedule.Minute ?? 0)))
            {
                daysUntilTarget = 7;
            }
                
            return now.Date.AddDays(daysUntilTarget).AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0);
        }

        /// <summary>
        /// Oblicza nastêpne wykonanie dla harmonogramu miesiêcznego.
        /// Znajduje nastêpny miesi¹c w okreœlonym dniu o okreœlonej godzinie.
        /// Obs³uguje miesi¹ce z ró¿n¹ liczb¹ dni (28, 29, 30, 31).
        /// </summary>
        /// <param name="now">Aktualny czas</param>
        /// <param name="schedule">Harmonogram z ustawionym DayOfMonth, Hour i Minute</param>
        /// <returns>Czas nastêpnego wykonania</returns>
        private DateTime CalculateNextMonthly(DateTime now, CronJobSchedule schedule)
        {
            var targetDay = schedule.DayOfMonth ?? 1;
            
            // Obliczenie nastêpnego miesi¹ca
            var nextMonth = now.Month == 12 ? 
                new DateTime(now.Year + 1, 1, 1) : 
                new DateTime(now.Year, now.Month + 1, 1);
            
            // Jeœli docelowy dzieñ nie istnieje w miesi¹cu (np. 31 lutego), u¿yj ostatniego dnia
            var actualDay = Math.Min(targetDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
            var targetDate = new DateTime(nextMonth.Year, nextMonth.Month, actualDay);
            
            return targetDate.AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0);
        }

        /// <summary>
        /// Oblicza nastêpne wykonanie dla wyra¿enia cron.
        /// UWAGA: Obecnie jest to placeholder - zwraca czas za godzinê.
        /// W prawdziwej implementacji nale¿y u¿yæ biblioteki do parsowania cron (np. NCrontab).
        /// </summary>
        /// <param name="now">Aktualny czas</param>
        /// <param name="cronExpression">Wyra¿enie cron do sparsowania</param>
        /// <returns>Czas nastêpnego wykonania (obecnie placeholder)</returns>
        private DateTime CalculateNextFromCron(DateTime now, string cronExpression)
        {
            // TODO: Implementacja parsowania wyra¿eñ cron
            // Prosta implementacja dla podstawowych przypadków
            // W produkcji warto u¿yæ biblioteki jak NCrontab lub Quartz.NET
            
            _logger.LogWarning("Parsowanie wyra¿eñ cron nie jest w pe³ni zaimplementowane. U¿ywanie placeholder dla: {CronExpression}", cronExpression);
            return now.AddHours(1); // Placeholder - godzina do przodu
        }

        /// <summary>
        /// Zwalnia zasoby u¿ywane przez scheduler.
        /// Zatrzymuje timer i czyœci zadania.
        /// </summary>
        public void Dispose()
        {
            _logger.LogInformation("CronSchedulerService - zwalnianie zasobów");
            _timer?.Dispose();
        }
    }

    /// <summary>
    /// Reprezentuje zaplanowane zadanie cykliczne z pe³nymi informacjami o jego stanie.
    /// Zawiera harmonogram, statystyki wykonañ i metadane zadania.
    /// </summary>
    public class ScheduledJob
    {
        /// <summary>
        /// Unikalny identyfikator zadania.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Harmonogram zadania z parametrami HTTP.
        /// Zawiera wszystkie informacje potrzebne do wykonania zadania.
        /// </summary>
        public CronJobSchedule Schedule { get; set; } = new();

        /// <summary>
        /// Czas nastêpnego zaplanowanego wykonania (UTC).
        /// Automatycznie obliczany przez scheduler po ka¿dym wykonaniu.
        /// </summary>
        public DateTime NextExecution { get; set; }

        /// <summary>
        /// Czas ostatniego wykonania zadania (UTC).
        /// Null jeœli zadanie nigdy nie by³o wykonane.
        /// </summary>
        public DateTime? LastExecution { get; set; }

        /// <summary>
        /// Liczba wykonañ zadania od momentu utworzenia.
        /// Inkrementowana po ka¿dej próbie wykonania (niezale¿nie od wyniku).
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Czy zadanie jest aktywne.
        /// Nieaktywne zadania nie s¹ wykonywane przez scheduler.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Czas utworzenia zadania (UTC).
        /// U¿ywany do statystyk i debugowania.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}