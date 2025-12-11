using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
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
        private readonly IConfiguration _configuration;
        private readonly string _logDirectory;
        private FileLoggingService? _fileLoggingService;
        
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
        /// <param name="configuration">Konfiguracja aplikacji</param>
        public CronSchedulerService(ILogger<CronSchedulerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            // Konfiguracja katalogu dla logów zadañ cyklicznych
            _logDirectory = _configuration["CronJobLogging:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "CronJobLogs");
            EnsureLogDirectoryExists();
        }

        /// <summary>
        /// Uruchamia serwis schedulera - wywo³ywane automatycznie przy starcie aplikacji.
        /// Inicjalizuje timer sprawdzaj¹cy zadania co 10 sekund i wykonuje logowanie Olmed.
        /// </summary>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cron Scheduler Service uruchomiony");
            
            // Inicjalizacja FileLoggingService
            try
            {
                using var scope = _serviceProvider.CreateScope();
                _fileLoggingService = scope.ServiceProvider.GetService<FileLoggingService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie mo¿na zainicjalizowaæ FileLoggingService w CronSchedulerService");
            }

            await LogSchedulerEvent("SERVICE_STARTED", "Cron Scheduler Service uruchomiony", null);
            
            // Wykonanie logowania Olmed przy starcie
            await PerformOlmedLoginOnStartup();
            
            // Automatyczne ³adowanie zadañ przyk³adowych przy starcie
            LoadExampleJobs();
            
            // Timer sprawdza co 10 sekund czy s¹ zadania do wykonania
            // Pierwsze sprawdzenie natychmiast (TimeSpan.Zero), potem co 10 sekund
            _timer = new Timer(CheckAndExecuteJobs, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Wykonuje logowanie do API Olmed przy starcie serwisu.
        /// Wykorzystuje istniej¹cy endpoint z CronController zamiast duplikowania logiki.
        /// </summary>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task PerformOlmedLoginOnStartup()
        {
            try
            {
                _logger.LogInformation("Wykonywanie logowania Olmed przy starcie CronSchedulerService...");
                
                using var scope = _serviceProvider.CreateScope();
                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                // Pobieranie URL z konfiguracji lub u¿ycie localhost
                var baseUrl = _configuration["CronController:BaseUrl"] ?? "http://localhost:5251";
                var loginUrl = $"{baseUrl}/api/cron/auth/olmed-login";

                _logger.LogInformation("Wywo³anie endpointu logowania Olmed z CronController: {LoginUrl}", loginUrl);

                // Wywo³anie istniej¹cego endpointu logowania z CronController
                var response = await httpClient.PostAsync(loginUrl, new StringContent("", Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Pomyœlne logowanie Olmed przez CronController przy starcie CronSchedulerService");

                    // Logowanie sukcesu do pliku
                    await LogSchedulerEvent("OLMED_LOGIN_SUCCESS", 
                        "Pomyœlne logowanie Olmed przy starcie serwisu przez CronController", 
                        new { 
                            LoginUrl = loginUrl,
                            StatusCode = response.StatusCode,
                            LoginTime = DateTime.UtcNow
                        });
                }
                else
                {
                    _logger.LogWarning("Nieudane logowanie Olmed przez CronController przy starcie: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);

                    await LogSchedulerEvent("OLMED_LOGIN_FAILED", 
                        "Nieudane logowanie Olmed przy starcie serwisu przez CronController", 
                        new { 
                            LoginUrl = loginUrl,
                            StatusCode = response.StatusCode,
                            Response = responseContent
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania Olmed przy starcie CronSchedulerService przez CronController");
                
                await LogSchedulerEvent("OLMED_LOGIN_ERROR", 
                    "B³¹d podczas logowania Olmed przy starcie serwisu przez CronController", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace
                    });
            }
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
            _ = Task.Run(async () => await LogSchedulerEvent("SERVICE_STOPPED", "Cron Scheduler Service zatrzymany", null));
            
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

            var isUpdate = _scheduledJobs.ContainsKey(jobId);

            // Thread-safe dodanie/aktualizacja zadania
            // Jeœli zadanie ju¿ istnieje, zachowuje statystyki wykonañ
            _scheduledJobs.AddOrUpdate(jobId, job, (key, old) =>
            {
                job.ExecutionCount = old.ExecutionCount; // Zachowanie liczby wykonañ
                job.LastExecution = old.LastExecution;   // Zachowanie czasu ostatniego wykonania
                job.CreatedAt = old.CreatedAt;           // Zachowanie oryginalnego czasu utworzenia
                return job;
            });

            var action = isUpdate ? "UPDATED" : "ADDED";
            _logger.LogInformation("Dodano/zaktualizowano zadanie cykliczne: {JobId}, nastêpne wykonanie: {NextExecution}", 
                jobId, job.NextExecution);

            // Logowanie do pliku
            _ = Task.Run(async () => await LogJobEvent(jobId, $"JOB_{action}", 
                $"Zadanie zosta³o {(isUpdate ? "zaktualizowane" : "dodane")}", 
                new { 
                    ScheduleType = schedule.Type.ToString(),
                    NextExecution = job.NextExecution,
                    RequestUrl = schedule.Request.Url,
                    RequestMethod = schedule.Request.Method
                }));
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
                
                // Logowanie do pliku
                _ = Task.Run(async () => await LogJobEvent(jobId, "JOB_REMOVED", "Zadanie zosta³o usuniête", 
                    new { 
                        ExecutionCount = job.ExecutionCount,
                        LastExecution = job.LastExecution,
                        CreatedAt = job.CreatedAt 
                    }));
                
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

            if (jobsToExecute.Any())
            {
                _logger.LogInformation("Znaleziono {Count} zadañ do wykonania", jobsToExecute.Count);
            }

            // Wykonanie ka¿dego zadania w pêtli
            foreach (var job in jobsToExecute)
            {
                var executionStartTime = DateTime.UtcNow;
                var success = false;
                string? errorMessage = null;
                string? responseData = null;
                int? statusCode = null;

                try
                {
                    _logger.LogInformation("Wykonywanie zadania cyklicznego: {JobId}", job.Id);
                    
                    // Utworzenie scope'a dla dependency injection
                    // Ka¿de zadanie ma swój w³asny scope - izolacja zale¿noœci
                    using var scope = _serviceProvider.CreateScope();
                    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    
                    // Wykonanie w³aœciwego zadania HTTP
                    var (httpSuccess, httpStatusCode, httpResponse) = await ExecuteJob(job, httpClient, configuration);
                    
                    success = httpSuccess;
                    statusCode = httpStatusCode;
                    responseData = httpResponse;
                    
                    _logger.LogInformation("Zadanie {JobId} wykonane pomyœlnie. Nastêpne wykonanie: {NextExecution}", 
                        job.Id, job.NextExecution);
                }
                catch (Exception ex)
                {
                    success = false;
                    errorMessage = ex.Message;
                    _logger.LogError(ex, "B³¹d podczas wykonywania zadania cyklicznego: {JobId}", job.Id);
                }
                finally
                {
                    // Aktualizacja statystyk zadania - niezale¿nie od wyniku wykonania
                    job.LastExecution = now;
                    job.ExecutionCount++;
                    job.NextExecution = CalculateNextExecution(job.Schedule); // Obliczenie nastêpnego wykonania
                    
                    var executionDuration = DateTime.UtcNow - executionStartTime;
                    
                    // Logowanie wykonania do pliku
                    _ = Task.Run(async () => await LogJobExecution(job.Id, success, executionStartTime, executionDuration, statusCode, responseData, errorMessage));
                }
            }
        }

        /// <summary>
        /// Wykonuje pojedyncze zadanie HTTP.
        /// Buduje ¿¹danie HTTP na podstawie parametrów w harmonogramie i wysy³a je.
        /// Token management jest obs³ugiwany przez CronController.
        /// </summary>
        /// <param name="job">Zadanie do wykonania</param>
        /// <param name="httpClient">Klient HTTP do wykonania ¿¹dania</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <returns>Tuple z informacjami o wyniku (success, statusCode, response)</returns>
        private async Task<(bool Success, int StatusCode, string Response)> ExecuteJob(ScheduledJob job, HttpClient httpClient, IConfiguration configuration)
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

            // Informacja o autoryzacji Olmed - rzeczywiste token management jest w CronController
            if (job.Schedule.Request.Url.Contains("grupaolmed.pl") && job.Schedule.Request.UseOlmedAuth)
            {
                _logger.LogInformation("Zadanie {JobId} wymaga autoryzacji Olmed - token management jest obs³ugiwany przez CronController", job.Id);
                
                // UWAGA: Token nie jest dodawany tutaj, poniewa¿:
                // 1. Token storage jest w CronController
                // 2. Zadania z UseOlmedAuth=true powinny byæ wykonywane przez CronController endpoint
                // 3. CronSchedulerService powinien g³ównie obs³ugiwaæ zadania bez autoryzacji lub delegowaæ do CronController
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

            return (response.IsSuccessStatusCode, (int)response.StatusCode, responseContent);
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

        #region File Logging Methods - Metody logowania do pliku

        /// <summary>
        /// Zapewnia istnienie katalogu do przechowywania logów zadañ cyklicznych.
        /// Tworzy katalog jeœli nie istnieje.
        /// </summary>
        private void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                    _logger.LogInformation("Utworzono katalog logów cron jobs: {Directory}", _logDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia katalogu logów cron jobs: {Directory}", _logDirectory);
            }
        }

        /// <summary>
        /// Loguje zdarzenie zwi¹zane z zadaniem do pliku.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania</param>
        /// <param name="eventType">Typ zdarzenia (JOB_ADDED, JOB_REMOVED, itp.)</param>
        /// <param name="message">Opis zdarzenia</param>
        /// <param name="additionalData">Dodatkowe dane do zapisania</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task LogJobEvent(string jobId, string eventType, string message, object? additionalData = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = eventType,
                    JobId = jobId,
                    Message = message,
                    AdditionalData = additionalData
                };

                await WriteLogEntry("job_events", logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania zdarzenia zadania {JobId}: {EventType}", jobId, eventType);
            }
        }

        /// <summary>
        /// Loguje wykonanie zadania do pliku.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania</param>
        /// <param name="success">Czy wykonanie zakoñczy³o siê sukcesem</param>
        /// <param name="startTime">Czas rozpoczêcia wykonania</param>
        /// <param name="duration">Czas trwania wykonania</param>
        /// <param name="statusCode">Kod statusu HTTP</param>
        /// <param name="responseData">OdpowiedŸ HTTP (skrócona)</param>
        /// <param name="errorMessage">Komunikat b³êdu jeœli wyst¹pi³</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task LogJobExecution(string jobId, bool success, DateTime startTime, TimeSpan duration, int? statusCode, string? responseData, string? errorMessage)
        {
            try
            {
                // Skrócenie odpowiedzi dla logowania
                var truncatedResponse = responseData?.Length > 500 ? responseData[..500] + "..." : responseData;

                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    JobId = jobId,
                    Success = success,
                    StartTime = startTime,
                    Duration = duration.TotalMilliseconds,
                    StatusCode = statusCode,
                    ResponseData = truncatedResponse,
                    ErrorMessage = errorMessage,
                    ExecutionId = Guid.NewGuid().ToString("N")[..8] // Krótki ID wykonania
                };

                await WriteLogEntry("job_executions", logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania wykonania zadania {JobId}", jobId);
            }
        }

        /// <summary>
        /// Loguje zdarzenie schedulera do pliku.
        /// </summary>
        /// <param name="eventType">Typ zdarzenia (SERVICE_STARTED, SERVICE_STOPPED, itp.)</param>
        /// <param name="message">Opis zdarzenia</param>
        /// <param name="additionalData">Dodatkowe dane do zapisania</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task LogSchedulerEvent(string eventType, string message, object? additionalData = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = eventType,
                    Message = message,
                    AdditionalData = additionalData,
                    ActiveJobsCount = _scheduledJobs.Count
                };

                await WriteLogEntry("scheduler_events", logEntry);

                // Dodatkowe logowanie przez FileLoggingService jeœli dostêpny
                if (_fileLoggingService != null)
                {
                    await _fileLoggingService.LogStructuredAsync("scheduler", LogLevel.Information, message, logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania zdarzenia schedulera: {EventType}", eventType);
            }
        }

        /// <summary>
        /// Zapisuje wpis do pliku log z rotacj¹ dzienn¹.
        /// </summary>
        /// <param name="logType">Typ logu (job_events, job_executions, scheduler_events)</param>
        /// <param name="logEntry">Wpis do zapisania</param>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task WriteLogEntry(string logType, object logEntry)
        {
            try
            {
                var date = DateTime.UtcNow.ToString("yyyyMMdd");
                var filename = $"cronjobs_{logType}_{date}.log";
                var filePath = Path.Combine(_logDirectory, filename);

                var jsonLine = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                }) + Environment.NewLine;

                // Thread-safe append do pliku
                using var semaphore = new SemaphoreSlim(1, 1);
                await semaphore.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(filePath, jsonLine);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania do pliku log: {LogType}", logType);
            }
        }

        #endregion

        /// <summary>
        /// Zwalnia zasoby u¿ywane przez scheduler.
        /// Zatrzymuje timer i czyœci zadania.
        /// </summary>
        public void Dispose()
        {
            _logger.LogInformation("CronSchedulerService - zwalnianie zasobów");
            _ = Task.Run(async () => await LogSchedulerEvent("SERVICE_DISPOSING", "Zwalnianie zasobów CronSchedulerService", 
                new { TotalJobs = _scheduledJobs.Count }));
            
            _timer?.Dispose();
        }

        /// <summary>
        /// £aduje przyk³adowe zadania cykliczne przy starcie aplikacji.
        /// Sprawdza konfiguracjê czy automatyczne ³adowanie jest w³¹czone.
        /// </summary>
        private void LoadExampleJobs()
        {
            try
            {
                // Sprawdzenie czy automatyczne ³adowanie przyk³adowych zadañ jest w³¹czone
                var autoLoadExamples = _configuration.GetValue<bool>("CronJobLogging:AutoLoadExampleJobs", true);
                if (!autoLoadExamples)
                {
                    _logger.LogInformation("Automatyczne ³adowanie przyk³adowych zadañ jest wy³¹czone w konfiguracji");
                    return;
                }

                _logger.LogInformation("£adowanie przyk³adowych zadañ cyklicznych...");

                // Przyk³ad 1: Zadanie wykonywane co 60 sekund - przydatne do monitorowania
                AddOrUpdateJob("test-interval", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 60, // Zwiêkszone do 60 sekund dla mniejszej czêstotliwoœci testów
                    Request = new CronJobRequest
                    {
                        Method = "GET",
                        Url = "https://httpbin.org/get",
                        Headers = new Dictionary<string, string> { { "User-Agent", "Prosepo-Scheduler" } }
                    }
                });

                // Przyk³ad 2: Zadanie codzienne o 09:00 - typowe dla raportów dziennych
                AddOrUpdateJob("daily-report", new CronJobSchedule
                {
                    Type = ScheduleType.Daily,
                    Hour = 9,
                    Minute = 0,
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = "https://httpbin.org/post",
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                        Body = "{\"report\": \"daily\", \"timestamp\": \"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"}"
                    }
                });

                // Przyk³ad 3: Zadanie tygodniowe (poniedzia³ki o 10:00) - typowe dla zadañ konserwacyjnych
                AddOrUpdateJob("weekly-cleanup", new CronJobSchedule
                {
                    Type = ScheduleType.Weekly,
                    DayOfWeek = DayOfWeek.Monday,
                    Hour = 10,
                    Minute = 0,
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = "https://httpbin.org/post",
                        Body = "{\"task\": \"weekly cleanup\"}"
                    }
                });

                // Przyk³ad 4: Automatyczne odœwie¿anie tokena Olmed co 45 minut
                // Zapewnia ci¹g³¹ autoryzacjê bez koniecznoœci ponownego logowania
                AddOrUpdateJob("olmed-auth-refresh", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 2700, // 45 minut - bezpieczny margin przed wygaœniêciem tokena (60 min)
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = "http://localhost:5251/api/cron/auth/refresh-if-needed"
                    }
                });

                // £adowanie zadañ synchronizacji produktów z konfiguracji
                _ = Task.Run(async () => await LoadProductSyncJobs());

                var loadedJobs = new[] { "test-interval", "daily-report", "weekly-cleanup", "olmed-auth-refresh" };
                
                _logger.LogInformation("Za³adowano {Count} przyk³adowych zadañ cyklicznych: {Jobs}", 
                    loadedJobs.Length, string.Join(", ", loadedJobs));

                // Logowanie do pliku
                _ = Task.Run(async () => await LogSchedulerEvent("EXAMPLE_JOBS_LOADED", 
                    $"Za³adowano {loadedJobs.Length} przyk³adowych zadañ cyklicznych", 
                    new { Jobs = loadedJobs, LoadedAt = DateTime.UtcNow }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania przyk³adowych zadañ cyklicznych");
                
                // Logowanie b³êdu do pliku
                _ = Task.Run(async () => await LogSchedulerEvent("EXAMPLE_JOBS_LOAD_ERROR", 
                    "B³¹d podczas ³adowania przyk³adowych zadañ", 
                    new { Error = ex.Message, StackTrace = ex.StackTrace }));
            }
        }

        /// <summary>
        /// £aduje zadania synchronizacji produktów z konfiguracji JSON.
        /// U¿ywa ProductSyncConfigurationService do pobrania aktywnych konfiguracji i tworzy odpowiednie zadania cykliczne.
        /// </summary>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        public async Task LoadProductSyncJobs()
        {
            try
            {
                _logger.LogInformation("£adowanie zadañ synchronizacji produktów z konfiguracji JSON...");

                // Utworzenie scope'a dla dependency injection
                using var scope = _serviceProvider.CreateScope();
                var productSyncConfigService = scope.ServiceProvider.GetService<ProductSyncConfigurationService>();

                if (productSyncConfigService == null)
                {
                    _logger.LogWarning("ProductSyncConfigurationService nie jest zarejestrowany. Pomijanie ³adowania zadañ synchronizacji produktów.");
                    return;
                }

                // Pobranie aktywnych konfiguracji synchronizacji
                var configurations = await productSyncConfigService.GetActiveConfigurationsAsync();
                var loadedJobs = new List<string>();

                if (!configurations.Any())
                {
                    _logger.LogInformation("Brak aktywnych konfiguracji synchronizacji produktów do za³adowania");
                    return;
                }

                _logger.LogInformation("Znaleziono {Count} aktywnych konfiguracji synchronizacji produktów", configurations.Count);

                // Tworzenie zadañ cyklicznych na podstawie konfiguracji
                foreach (var config in configurations)
                {
                    try
                    {
                        _logger.LogInformation("£adowanie zadania synchronizacji: {JobId} - {Name}", config.Id, config.Name);

                        // Tworzenie harmonogramu zadania na podstawie konfiguracji
                        var schedule = new CronJobSchedule
                        {
                            Type = ScheduleType.Interval,
                            IntervalSeconds = config.IntervalSeconds,
                            Request = new CronJobRequest
                            {
                                Method = config.Method,
                                Url = config.Url,
                                UseOlmedAuth = config.UseOlmedAuth,
                                Headers = config.Headers ?? new Dictionary<string, string>(),
                                Body = config.Body
                            }
                        };

                        // Dodanie zadania do schedulera
                        AddOrUpdateJob(config.Id, schedule);
                        loadedJobs.Add(config.Id);

                        _logger.LogInformation("Za³adowano zadanie synchronizacji: {JobId} - {Name} (interwa³: {Interval}s, URL: {Url})", 
                            config.Id, config.Name, config.IntervalSeconds, config.Url);

                        // Logowanie szczegó³ów konfiguracji do pliku
                        await LogJobEvent(config.Id, "PRODUCT_SYNC_JOB_LOADED", 
                            $"Za³adowano zadanie synchronizacji produktów: {config.Name}", 
                            new {
                                Name = config.Name,
                                Description = config.Description,
                                Marketplace = config.Marketplace,
                                IntervalSeconds = config.IntervalSeconds,
                                Method = config.Method,
                                Url = config.Url,
                                UseOlmedAuth = config.UseOlmedAuth,
                                HeadersCount = config.Headers?.Count ?? 0,
                                HasBody = !string.IsNullOrEmpty(config.Body),
                                AdditionalParametersCount = config.AdditionalParameters?.Count ?? 0
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "B³¹d podczas ³adowania zadania synchronizacji: {JobId} - {Name}", config.Id, config.Name);
                        
                        // Logowanie b³êdu do pliku
                        await LogJobEvent(config.Id, "PRODUCT_SYNC_JOB_LOAD_ERROR", 
                            $"B³¹d podczas ³adowania zadania synchronizacji: {config.Name}", 
                            new { 
                                Error = ex.Message,
                                ConfigurationId = config.Id,
                                ConfigurationName = config.Name
                            });
                    }
                }

                // Podsumowanie ³adowania
                if (loadedJobs.Any())
                {
                    _logger.LogInformation("Pomyœlnie za³adowano {LoadedCount} z {TotalCount} zadañ synchronizacji produktów: {Jobs}", 
                        loadedJobs.Count, configurations.Count, string.Join(", ", loadedJobs));

                    // Logowanie podsumowania do pliku
                    await LogSchedulerEvent("PRODUCT_SYNC_JOBS_LOADED", 
                        $"Za³adowano {loadedJobs.Count} zadañ synchronizacji produktów z konfiguracji JSON", 
                        new { 
                            LoadedJobs = loadedJobs,
                            TotalConfigurations = configurations.Count,
                            LoadedCount = loadedJobs.Count,
                            ConfigurationFile = _configuration["ProductSync:ConfigurationFile"] ?? "Configuration/product-sync-config.json",
                            LoadedAt = DateTime.UtcNow
                        });
                }
                else
                {
                    _logger.LogWarning("Nie uda³o siê za³adowaæ ¿adnego zadania synchronizacji produktów");
                    
                    await LogSchedulerEvent("PRODUCT_SYNC_JOBS_LOAD_WARNING", 
                        "Nie uda³o siê za³adowaæ ¿adnego zadania synchronizacji produktów", 
                        new { 
                            TotalConfigurations = configurations.Count,
                            ConfigurationsDetails = configurations.Select(c => new { c.Id, c.Name, c.IsActive }).ToList()
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania zadañ synchronizacji produktów z konfiguracji");
                
                // Logowanie b³êdu do pliku
                await LogSchedulerEvent("PRODUCT_SYNC_JOBS_LOAD_CRITICAL_ERROR", 
                    "Krytyczny b³¹d podczas ³adowania zadañ synchronizacji produktów", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        ConfigurationFile = _configuration["ProductSync:ConfigurationFile"] ?? "Configuration/product-sync-config.json"
                    });
            }
        }

        /// <summary>
        /// Ponownie ³aduje wszystkie zadania synchronizacji produktów z konfiguracji.
        /// Przydatne po zmianach w pliku konfiguracji.
        /// </summary>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        public async Task ReloadProductSyncJobs()
        {
            try
            {
                _logger.LogInformation("Ponowne ³adowanie zadañ synchronizacji produktów...");

                // Najpierw usuñ wszystkie istniej¹ce zadania synchronizacji produktów
                var existingJobs = _scheduledJobs.Keys
                    .Where(jobId => jobId.StartsWith("olmed-") || jobId.Contains("-sync-") || jobId.EndsWith("-sync"))
                    .ToList();

                foreach (var jobId in existingJobs)
                {
                    if (RemoveJob(jobId))
                    {
                        _logger.LogInformation("Usuniêto istniej¹ce zadanie synchronizacji: {JobId}", jobId);
                    }
                }

                // Za³aduj zadania ponownie z aktualnej konfiguracji
                await LoadProductSyncJobs();

                await LogSchedulerEvent("PRODUCT_SYNC_JOBS_RELOADED", 
                    "Zadania synchronizacji produktów zosta³y prze³adowane", 
                    new { 
                        RemovedJobs = existingJobs,
                        RemovedCount = existingJobs.Count,
                        ReloadedAt = DateTime.UtcNow 
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ponownego ³adowania zadañ synchronizacji produktów");
                
                await LogSchedulerEvent("PRODUCT_SYNC_JOBS_RELOAD_ERROR", 
                    "B³¹d podczas ponownego ³adowania zadañ synchronizacji produktów", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace
                    });
                
                throw; // Przeka¿ b³¹d wy¿ej dla w³aœciwej obs³ugi
            }
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