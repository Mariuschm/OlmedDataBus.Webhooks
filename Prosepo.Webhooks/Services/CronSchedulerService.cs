using Prosepo.Webhooks.Helpers;
using Prosepo.Webhooks.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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
        private readonly string secureKey = Environment.GetEnvironmentVariable("PROSPEO_KEY") ?? "CPNFWqXE3TMY925xMgUPlUnWkjSyo9182PpYM69HM44=";
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
        /// Statyczny storage dla tokenów autoryzacji.
        /// Wspó³dzielony miêdzy CronSchedulerService i CronController.
        /// W œrodowisku produkcyjnym warto rozwa¿yæ u¿ycie Redis lub innego cache'a.
        /// </summary>
        private static readonly Dictionary<string, TokenInfo> _tokenStorage = new();

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
            LoadJobs();
            
            // Timer sprawdza co 10 sekund czy s¹ zadania do wykonania
            // Pierwsze sprawdzenie natychmiast (TimeSpan.Zero), potem co 10 sekund
            _timer = new Timer(CheckAndExecuteJobs, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Wykonuje logowanie do API Olmed przy starcie serwisu.
        /// Implementuje bezpoœrednio logikê logowania bez HTTP calls.
        /// </summary>
        /// <returns>Task reprezentuj¹cy operacjê asynchroniczn¹</returns>
        private async Task PerformOlmedLoginOnStartup()
        {
            try
            {
                _logger.LogInformation("Wykonywanie logowania Olmed przy starcie CronSchedulerService...");
                
                using var scope = _serviceProvider.CreateScope();
                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                // Pobieranie danych autoryzacji z konfiguracji
                var username = StringEncryptionHelper.DecryptIfEncrypted(_configuration["OlmedAuth:Username"], secureKey) ?? "test_prospeo";
                var password = StringEncryptionHelper.DecryptIfEncrypted(_configuration["OlmedAuth:Password"], secureKey) ?? "pvRGowxF%266J%2AM%24";
                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                
                // Budowanie URL z parametrami query - zgodnie z dokumentacj¹ API Olmed
                var loginUrl = $"{baseUrl}/erp-api/auth/login?username={username}&password={Uri.EscapeDataString(password)}";

                _logger.LogInformation("Wykonywanie bezpoœredniego logowania Olmed z CronSchedulerService: {BaseUrl}", baseUrl);

                // Przygotowanie ¿¹dania HTTP - dok³adna replika curl command
                var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("X-CSRF-TOKEN", ""); // Wymagany pusty nag³ówek CSRF
                request.Content = new StringContent("", Encoding.UTF8);

                // Wykonanie ¿¹dania logowania
                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parsowanie odpowiedzi JSON
                    var authResponse = JsonSerializer.Deserialize<OlmedAuthResponse>(responseContent);
                    
                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        // Tworzenie obiektu z informacjami o tokenie
                        var tokenInfo = new TokenInfo
                        {
                            Token = authResponse.Token,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 3600), // Domyœlnie 1 godzina
                            CreatedAt = DateTime.UtcNow
                        };

                        // Przechowywanie tokena w statycznym storage - dostêp z innych serwisów
                        SetOlmedToken(tokenInfo);

                        _logger.LogInformation("Token Olmed zosta³ pobrany i zapisany podczas startu CronSchedulerService. Wygasa: {ExpiresAt}", tokenInfo.ExpiresAt);

                        // Logowanie sukcesu do pliku
                        await LogSchedulerEvent("OLMED_LOGIN_SUCCESS", 
                            "Pomyœlne bezpoœrednie logowanie Olmed przy starcie serwisu", 
                            new { 
                                ExpiresAt = tokenInfo.ExpiresAt,
                                ExpiresIn = authResponse.ExpiresIn ?? 3600,
                                BaseUrl = baseUrl,
                                Method = "Direct",
                                LoginTime = DateTime.UtcNow
                            });
                    }
                    else
                    {
                        _logger.LogWarning("Nieprawid³owa odpowiedŸ z API Olmed podczas logowania przy starcie: brak tokena w odpowiedzi");
                        
                        await LogSchedulerEvent("OLMED_LOGIN_INVALID_RESPONSE", 
                            "Nieprawid³owa odpowiedŸ z API Olmed - brak tokena", 
                            new { 
                                StatusCode = response.StatusCode,
                                Response = responseContent,
                                BaseUrl = baseUrl,
                                Method = "Direct"
                            });
                    }
                }
                else
                {
                    // Logowanie nieudanego logowania z szczegó³ami odpowiedzi
                    _logger.LogWarning("Nieudane logowanie Olmed przy starcie CronSchedulerService: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);

                    await LogSchedulerEvent("OLMED_LOGIN_FAILED", 
                        "Nieudane logowanie Olmed przy starcie serwisu", 
                        new { 
                            StatusCode = response.StatusCode,
                            Response = responseContent,
                            BaseUrl = baseUrl,
                            Method = "Direct",
                            Username = username
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas bezpoœredniego logowania Olmed przy starcie CronSchedulerService");
                
                await LogSchedulerEvent("OLMED_LOGIN_ERROR", 
                    "B³¹d podczas bezpoœredniego logowania Olmed przy starcie serwisu", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        BaseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl",
                        Method = "Direct"
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

        #region Token Management - Zarz¹dzanie tokenami

        /// <summary>
        /// Zapisuje token Olmed w statycznym storage.
        /// Metoda publiczna umo¿liwiaj¹ca wspó³dzielenie tokenów miêdzy serwisami.
        /// </summary>
        /// <param name="tokenInfo">Informacje o tokenie do zapisania</param>
        public static void SetOlmedToken(TokenInfo tokenInfo)
        {
            _tokenStorage["olmed"] = tokenInfo;
        }

        /// <summary>
        /// Pobiera aktualny token Olmed ze statycznego storage.
        /// Sprawdza czy token jest jeszcze wa¿ny.
        /// </summary>
        /// <returns>TokenInfo jeœli token jest wa¿ny, null w przeciwnym przypadku</returns>
        public static TokenInfo? GetOlmedToken()
        {
            if (_tokenStorage.TryGetValue("olmed", out var tokenInfo))
            {
                if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                {
                    return tokenInfo;
                }
                else
                {
                    // Token wygas³ - usuñ ze storage
                    _tokenStorage.Remove("olmed");
                }
            }
            return null;
        }

        /// <summary>
        /// Usuwa token Olmed ze statycznego storage.
        /// U¿ywane przy wylogowywaniu lub gdy token jest nieprawid³owy.
        /// </summary>
        public static void RemoveOlmedToken()
        {
            _tokenStorage.Remove("olmed");
        }

        /// <summary>
        /// Sprawdza czy istnieje wa¿ny token Olmed.
        /// </summary>
        /// <returns>True jeœli token istnieje i jest wa¿ny</returns>
        public static bool HasValidOlmedToken()
        {
            return GetOlmedToken() != null;
        }

        #endregion

        #region Job Management - Zarz¹dzanie zadaniami

        /// <summary>
        /// Dodaje nowe zadanie lub aktualizuje istniej¹ce w schedulerze.
        /// Automatycznie oblicza czas nastêpnego wykonania na podstawie harmonogramu.
        /// </summary>
        /// <param name="jobId">Unikalny identyfikator zadania</param>
        /// <param name="schedule">Harmonogram zadania z parametrami HTTP</param>
        public void AddOrUpdateJob(string jobId, CronJobSchedule schedule)
        {
            var job = new ScheduledJob
            {
                Id = jobId,
                Schedule = schedule,
                NextExecution = CalculateNextExecution(schedule),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var isUpdate = _scheduledJobs.ContainsKey(jobId);
            _scheduledJobs.AddOrUpdate(jobId, job, (key, old) =>
            {
                job.ExecutionCount = old.ExecutionCount;
                job.LastExecution = old.LastExecution;
                job.CreatedAt = old.CreatedAt;
                return job;
            });

            var action = isUpdate ? "UPDATED" : "ADDED";
            _logger.LogInformation("Dodano/zaktualizowano zadanie cykliczne: {JobId}, nastêpne wykonanie: {NextExecution}", 
                jobId, job.NextExecution);

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
        /// </summary>
        public bool RemoveJob(string jobId)
        {
            if (_scheduledJobs.TryRemove(jobId, out var job))
            {
                _logger.LogInformation("Usuniêto zadanie cykliczne: {JobId}", jobId);
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
        /// </summary>
        public IEnumerable<ScheduledJob> GetAllJobs()
        {
            return _scheduledJobs.Values.ToList();
        }

        /// <summary>
        /// Pobiera konkretne zadanie po jego identyfikatorze.
        /// </summary>
        public ScheduledJob? GetJob(string jobId)
        {
            _scheduledJobs.TryGetValue(jobId, out var job);
            return job;
        }

        #endregion

        #region Job Execution - Wykonywanie zadañ

        /// <summary>
        /// Metoda wywo³ywana przez timer co 10 sekund.
        /// Sprawdza wszystkie zadania i wykonuje te, których czas nadszed³.
        /// </summary>
        private async void CheckAndExecuteJobs(object? state)
        {
            var now = DateTime.UtcNow;
            var jobsToExecute = _scheduledJobs.Values
                .Where(job => job.IsActive && job.NextExecution <= now)
                .ToList();

            if (jobsToExecute.Any())
            {
                _logger.LogInformation("Znaleziono {Count} zadañ do wykonania", jobsToExecute.Count);
            }

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
                    
                    using var scope = _serviceProvider.CreateScope();
                    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    
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
                    job.LastExecution = now;
                    job.ExecutionCount++;
                    job.NextExecution = CalculateNextExecution(job.Schedule);
                    
                    var executionDuration = DateTime.UtcNow - executionStartTime;
                    _ = Task.Run(async () => await LogJobExecution(job.Id, success, executionStartTime, executionDuration, statusCode, responseData, errorMessage));
                }
            }
        }

        /// <summary>
        /// Wykonuje pojedyncze zadanie HTTP z automatyczn¹ autoryzacj¹ Olmed.
        /// </summary>
        private async Task<(bool Success, int StatusCode, string Response)> ExecuteJob(ScheduledJob job, HttpClient httpClient, IConfiguration configuration)
        {
            var request = new HttpRequestMessage(new HttpMethod(job.Schedule.Request.Method), job.Schedule.Request.Url);

            // Dodanie nag³ówków HTTP
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

            // Automatyczna autoryzacja Olmed
            if (job.Schedule.Request.Url.Contains("grupaolmed.pl") && job.Schedule.Request.UseOlmedAuth)
            {
                var tokenInfo = GetOlmedToken();
                if (tokenInfo != null)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {tokenInfo.Token}");
                    _logger.LogInformation("Dodano token Olmed do zadania {JobId} (token wygasa: {ExpiresAt})", 
                        job.Id, tokenInfo.ExpiresAt);
                }
                else
                {
                    _logger.LogWarning("Brak wa¿nego tokena Olmed dla zadania {JobId}", job.Id);
                }
            }

            // Dodanie zawartoœci dla metod POST/PUT
            if (!string.IsNullOrEmpty(job.Schedule.Request.Body) && 
                (job.Schedule.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                 job.Schedule.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)))
            {
                var contentType = job.Schedule.Request.Headers?.GetValueOrDefault("Content-Type", "application/json") ?? "application/json";
                request.Content = new StringContent(job.Schedule.Request.Body, Encoding.UTF8, contentType);
            }

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Parsowanie odpowiedzi JSON i logowanie specjalnych przypadków
            await ParseAndLogResponse(job.Id, responseContent, response.IsSuccessStatusCode);

            var logContent = responseContent.Length > 1000 ? responseContent[..1000] + "..." : responseContent;
            _logger.LogInformation("OdpowiedŸ zadania {JobId}: {StatusCode} - {Content}", 
                job.Id, response.StatusCode, logContent);

            return (response.IsSuccessStatusCode, (int)response.StatusCode, responseContent);
        }

        /// <summary>
        /// Parsuje odpowiedŸ JSON i loguje specjalne przypadki do pliku.
        /// Szczególnie obs³uguje komunikat "Request already completed! Use GUID below."
        /// </summary>
        private async Task ParseAndLogResponse(string jobId, string responseContent, bool isSuccess)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseContent))
                    return;

                // Próba parsowania JSON
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Sprawdzenie czy odpowiedŸ zawiera komunikat o ju¿ zakoñczonym ¿¹daniu
                if (root.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrEmpty(message) && 
                        message.Contains("Request already completed! Use GUID below", StringComparison.OrdinalIgnoreCase))
                    {
                        // Wyci¹gniêcie requestGUID z odpowiedzi
                        string? requestGuid = null;
                        if (root.TryGetProperty("requestGUID", out var guidElement))
                        {
                            requestGuid = guidElement.GetString();
                        }

                        // Logowanie strukturalne do pliku
                        var logData = new
                        {
                            JobId = jobId,
                            EventType = "REQUEST_ALREADY_COMPLETED",
                            Message = message,
                            RequestGUID = requestGuid,
                            FullResponse = responseContent,
                            Timestamp = DateTime.UtcNow,
                            IsSuccess = isSuccess
                        };

                        await LogJobEvent(jobId, "REQUEST_ALREADY_COMPLETED", 
                            $"¯¹danie ju¿ zakoñczone - otrzymano GUID: {requestGuid}", 
                            logData);

                        _logger.LogInformation("Zadanie {JobId}: ¯¹danie ju¿ zakoñczone. RequestGUID: {RequestGUID}", 
                            jobId, requestGuid);

                        // Dodatkowe logowanie przez FileLoggingService jeœli dostêpny
                        if (_fileLoggingService != null)
                        {
                            await _fileLoggingService.LogStructuredAsync("job_responses", LogLevel.Information, 
                                "Request already completed", logData);
                        }
                    }
                }

                // Logowanie innych strukturalnych odpowiedzi JSON
                if (root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Any())
                {
                    var responseData = new
                    {
                        JobId = jobId,
                        ParsedResponse = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = false }),
                        Timestamp = DateTime.UtcNow,
                        IsSuccess = isSuccess,
                        ResponseLength = responseContent.Length
                    };

                    await WriteLogEntry("parsed_responses", responseData);
                }
            }
            catch (JsonException ex)
            {
                // OdpowiedŸ nie jest prawid³owym JSON - logujemy jako zwyk³y tekst
                _logger.LogDebug("OdpowiedŸ zadania {JobId} nie jest prawid³owym JSON: {Error}", jobId, ex.Message);
                
                var textResponseData = new
                {
                    JobId = jobId,
                    EventType = "NON_JSON_RESPONSE",
                    Response = responseContent.Length > 500 ? responseContent[..500] + "..." : responseContent,
                    Timestamp = DateTime.UtcNow,
                    IsSuccess = isSuccess,
                    ParseError = ex.Message
                };

                await WriteLogEntry("text_responses", textResponseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas parsowania odpowiedzi zadania {JobId}", jobId);
            }
        }

        #endregion

        #region Schedule Calculation - Obliczenia harmonogramu

        private DateTime CalculateNextExecution(CronJobSchedule schedule)
        {
            var now = DateTime.UtcNow;
            
            return schedule.Type switch
            {
                ScheduleType.Interval => now.AddSeconds(schedule.IntervalSeconds ?? 60),
                ScheduleType.Daily => now.Date.AddDays(1).AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0),
                ScheduleType.Weekly => CalculateNextWeekly(now, schedule),
                ScheduleType.Monthly => CalculateNextMonthly(now, schedule),
                ScheduleType.Cron => CalculateNextFromCron(now, schedule.CronExpression ?? "0 * * * *"),
                _ => now.AddMinutes(5)
            };
        }

        private DateTime CalculateNextWeekly(DateTime now, CronJobSchedule schedule)
        {
            var targetDay = schedule.DayOfWeek ?? DayOfWeek.Monday;
            var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;
            
            if (daysUntilTarget == 0 && 
                now.TimeOfDay > TimeSpan.FromHours(schedule.Hour ?? 0).Add(TimeSpan.FromMinutes(schedule.Minute ?? 0)))
            {
                daysUntilTarget = 7;
            }
                
            return now.Date.AddDays(daysUntilTarget).AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0);
        }

        private DateTime CalculateNextMonthly(DateTime now, CronJobSchedule schedule)
        {
            var targetDay = schedule.DayOfMonth ?? 1;
            var nextMonth = now.Month == 12 ? 
                new DateTime(now.Year + 1, 1, 1) : 
                new DateTime(now.Year, now.Month + 1, 1);
            
            var actualDay = Math.Min(targetDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
            var targetDate = new DateTime(nextMonth.Year, nextMonth.Month, actualDay);
            
            return targetDate.AddHours(schedule.Hour ?? 0).AddMinutes(schedule.Minute ?? 0);
        }

        private DateTime CalculateNextFromCron(DateTime now, string cronExpression)
        {
            _logger.LogWarning("Parsowanie wyra¿eñ cron nie jest w pe³ni zaimplementowane. U¿ywanie placeholder dla: {CronExpression}", cronExpression);
            return now.AddHours(1);
        }

        #endregion

        #region Job Loading - £adowanie zadañ

        private void LoadJobs()
        {
            try
            {
                var autoLoadExamples = _configuration.GetValue<bool>("CronJobLogging:AutoLoadExampleJobs", true);
                if (!autoLoadExamples)
                {
                    _logger.LogInformation("Automatyczne ³adowanie przyk³adowych zadañ jest wy³¹czone w konfiguracji");
                    return;
                }

                _logger.LogInformation("£adowanie zadañ cyklicznych...");
                
                AddOrUpdateJob("olmed-auth-refresh", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 2700, // 45 minut
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = _configuration["CronController:BaseUrl"] + "/api/cron/auth/refresh-if-needed" ?? "http://localhost:5251/api/cron/auth/refresh-if-needed"
                    }
                });

                _ = Task.Run(async () => await LoadProductSyncJobs());
                _ = Task.Run(async () => await LoadOrderSyncJobs());

                var loadedJobs = new[] { "olmed-auth-refresh" };
                _logger.LogInformation("Za³adowano {Count} zadañ cyklicznych: {Jobs}", 
                    loadedJobs.Length, string.Join(", ", loadedJobs));

                _ = Task.Run(async () => await LogSchedulerEvent("JOBS_LOADED", 
                    $"Za³adowano {loadedJobs.Length} zadañ cyklicznych", 
                    new { Jobs = loadedJobs, LoadedAt = DateTime.UtcNow }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania zadañ cyklicznych");
                _ = Task.Run(async () => await LogSchedulerEvent("JOBS_LOAD_ERROR", 
                    "B³¹d podczas ³adowania zadañ", 
                    new { Error = ex.Message, StackTrace = ex.StackTrace }));
            }
        }

        public async Task LoadProductSyncJobs()
        {
            try
            {
                _logger.LogInformation("£adowanie zadañ synchronizacji produktów z konfiguracji JSON...");

                using var scope = _serviceProvider.CreateScope();
                var productSyncConfigService = scope.ServiceProvider.GetService<ProductSyncConfigurationService>();

                if (productSyncConfigService == null)
                {
                    _logger.LogWarning("ProductSyncConfigurationService nie jest zarejestrowany.");
                    return;
                }

                var configurations = await productSyncConfigService.GetActiveConfigurationsAsync();
                var loadedJobs = new List<string>();

                if (!configurations.Any())
                {
                    _logger.LogInformation("Brak aktywnych konfiguracji synchronizacji produktów do za³adowania");
                    return;
                }

                _logger.LogInformation("Znaleziono {Count} aktywnych konfiguracji synchronizacji produktów", configurations.Count);

                foreach (var config in configurations)
                {
                    try
                    {
                        _logger.LogInformation("£adowanie zadania synchronizacji: {JobId} - {Name}", config.Id, config.Name);

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

                        AddOrUpdateJob(config.Id, schedule);
                        loadedJobs.Add(config.Id);

                        _logger.LogInformation("Za³adowano zadanie synchronizacji: {JobId} - {Name} (interwa³: {Interval}s, URL: {Url})", 
                            config.Id, config.Name, config.IntervalSeconds, config.Url);

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
                        
                        await LogJobEvent(config.Id, "PRODUCT_SYNC_JOB_LOAD_ERROR", 
                            $"B³¹d podczas ³adowania zadania synchronizacji: {config.Name}", 
                            new { 
                                Error = ex.Message,
                                ConfigurationId = config.Id,
                                ConfigurationName = config.Name
                            });
                    }
                }

                if (loadedJobs.Any())
                {
                    _logger.LogInformation("Pomyœlnie za³adowano {LoadedCount} z {TotalCount} zadañ synchronizacji produktów: {Jobs}", 
                        loadedJobs.Count, configurations.Count, string.Join(", ", loadedJobs));

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
        /// Prze³adowuje zadania synchronizacji produktów.
        /// Usuwa istniej¹ce zadania produktów i ³aduje je ponownie z konfiguracji.
        /// </summary>
        public async Task ReloadProductSyncJobs()
        {
            try
            {
                _logger.LogInformation("Ponowne ³adowanie zadañ synchronizacji produktów...");

                // Identyfikacja zadañ produktów do usuniêcia
                var existingProductJobs = _scheduledJobs.Keys
                    .Where(jobId => jobId.Contains("-product") || jobId.Contains("products") || jobId.EndsWith("-sync-products") || (jobId.StartsWith("olmed-") && jobId.Contains("-sync-")))
                    .ToList();

                foreach (var jobId in existingProductJobs)
                {
                    if (RemoveJob(jobId))
                    {
                        _logger.LogInformation("Usuniêto istniej¹ce zadanie synchronizacji produktów: {JobId}", jobId);
                    }
                }

                await LoadProductSyncJobs();

                await LogSchedulerEvent("PRODUCT_SYNC_JOBS_RELOADED", 
                    "Zadania synchronizacji produktów zosta³y prze³adowane", 
                    new { 
                        RemovedJobs = existingProductJobs,
                        RemovedCount = existingProductJobs.Count,
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
                
                throw;
            }
        }

        /// <summary>
        /// £aduje zadania synchronizacji zamówieñ z konfiguracji JSON.
        /// Wykorzystuje OrderSyncConfigurationService do pobrania aktywnych konfiguracji.
        /// </summary>
        public async Task LoadOrderSyncJobs()
        {
            try
            {
                _logger.LogInformation("£adowanie zadañ synchronizacji zamówieñ z konfiguracji JSON...");

                using var scope = _serviceProvider.CreateScope();
                var orderSyncConfigService = scope.ServiceProvider.GetService<OrderSyncConfigurationService>();

                if (orderSyncConfigService == null)
                {
                    _logger.LogWarning("OrderSyncConfigurationService nie jest zarejestrowany.");
                    return;
                }

                var configurations = await orderSyncConfigService.GetActiveConfigurationsAsync();
                var loadedJobs = new List<string>();

                if (!configurations.Any())
                {
                    _logger.LogInformation("Brak aktywnych konfiguracji synchronizacji zamówieñ do za³adowania");
                    return;
                }

                _logger.LogInformation("Znaleziono {Count} aktywnych konfiguracji synchronizacji zamówieñ", configurations.Count);

                foreach (var config in configurations)
                {
                    try
                    {
                        _logger.LogInformation("£adowanie zadania synchronizacji zamówieñ: {JobId} - {Name}", config.Id, config.Name);

                        // Generowanie body z dynamicznymi datami
                        var body = orderSyncConfigService.GenerateRequestBody(config);

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
                                Body = body // Dynamicznie wygenerowane body z datami
                            }
                        };

                        AddOrUpdateJob(config.Id, schedule);
                        loadedJobs.Add(config.Id);

                        _logger.LogInformation("Za³adowano zadanie synchronizacji zamówieñ: {JobId} - {Name} (interwa³: {Interval}s, URL: {Url})", 
                            config.Id, config.Name, config.IntervalSeconds, config.Url);

                        await LogJobEvent(config.Id, "ORDER_SYNC_JOB_LOADED", 
                            $"Za³adowano zadanie synchronizacji zamówieñ: {config.Name}", 
                            new {
                                Name = config.Name,
                                Description = config.Description,
                                Marketplace = config.Marketplace,
                                IntervalSeconds = config.IntervalSeconds,
                                Method = config.Method,
                                Url = config.Url,
                                UseOlmedAuth = config.UseOlmedAuth,
                                HeadersCount = config.Headers?.Count ?? 0,
                                HasBody = !string.IsNullOrEmpty(body),
                                BodyPreview = body.Length > 100 ? body[..100] + "..." : body,
                                DateRangeDays = config.DateRangeDays,
                                UseCurrentDateAsEndDate = config.UseCurrentDateAsEndDate,
                                DateFormat = config.DateFormat,
                                AdditionalParametersCount = config.AdditionalParameters?.Count ?? 0
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "B³¹d podczas ³adowania zadania synchronizacji zamówieñ: {JobId} - {Name}", config.Id, config.Name);
                        
                        await LogJobEvent(config.Id, "ORDER_SYNC_JOB_LOAD_ERROR", 
                            $"B³¹d podczas ³adowania zadania synchronizacji zamówieñ: {config.Name}", 
                            new { 
                                Error = ex.Message,
                                ConfigurationId = config.Id,
                                ConfigurationName = config.Name
                            });
                    }
                }

                if (loadedJobs.Any())
                {
                    _logger.LogInformation("Pomyœlnie za³adowano {LoadedCount} z {TotalCount} zadañ synchronizacji zamówieñ: {Jobs}", 
                        loadedJobs.Count, configurations.Count, string.Join(", ", loadedJobs));

                    await LogSchedulerEvent("ORDER_SYNC_JOBS_LOADED", 
                        $"Za³adowano {loadedJobs.Count} zadañ synchronizacji zamówieñ z konfiguracji JSON", 
                        new { 
                            LoadedJobs = loadedJobs,
                            TotalConfigurations = configurations.Count,
                            LoadedCount = loadedJobs.Count,
                            ConfigurationFile = _configuration["OrderSync:ConfigurationFile"] ?? "Configuration/order-sync-config.json",
                            LoadedAt = DateTime.UtcNow
                        });
                }
                else
                {
                    _logger.LogWarning("Nie uda³o siê za³adowaæ ¿adnego zadania synchronizacji zamówieñ");
                    
                    await LogSchedulerEvent("ORDER_SYNC_JOBS_LOAD_WARNING", 
                        "Nie uda³o siê za³adowaæ ¿adnego zadania synchronizacji zamówieñ", 
                        new { 
                            TotalConfigurations = configurations.Count,
                            ConfigurationsDetails = configurations.Select(c => new { c.Id, c.Name, c.IsActive }).ToList()
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania zadañ synchronizacji zamówieñ z konfiguracji");
                
                await LogSchedulerEvent("ORDER_SYNC_JOBS_LOAD_CRITICAL_ERROR", 
                    "Krytyczny b³¹d podczas ³adowania zadañ synchronizacji zamówieñ", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        ConfigurationFile = _configuration["OrderSync:ConfigurationFile"] ?? "Configuration/order-sync-config.json"
                    });
            }
        }

        /// <summary>
        /// Prze³adowuje zadania synchronizacji zamówieñ.
        /// Usuwa istniej¹ce zadania zamówieñ i ³aduje je ponownie z konfiguracji.
        /// </summary>
        public async Task ReloadOrderSyncJobs()
        {
            try
            {
                _logger.LogInformation("Ponowne ³adowanie zadañ synchronizacji zamówieñ...");

                // Identyfikacja zadañ zamówieñ do usuniêcia
                var existingOrderJobs = _scheduledJobs.Keys
                    .Where(jobId => jobId.Contains("-order") || jobId.Contains("orders") || jobId.EndsWith("-sync-orders"))
                    .ToList();

                foreach (var jobId in existingOrderJobs)
                {
                    if (RemoveJob(jobId))
                    {
                        _logger.LogInformation("Usuniêto istniej¹ce zadanie synchronizacji zamówieñ: {JobId}", jobId);
                    }
                }

                await LoadOrderSyncJobs();

                await LogSchedulerEvent("ORDER_SYNC_JOBS_RELOADED", 
                    "Zadania synchronizacji zamówieñ zosta³y prze³adowane", 
                    new { 
                        RemovedJobs = existingOrderJobs,
                        RemovedCount = existingOrderJobs.Count,
                        ReloadedAt = DateTime.UtcNow 
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ponownego ³adowania zadañ synchronizacji zamówieñ");
                
                await LogSchedulerEvent("ORDER_SYNC_JOBS_RELOAD_ERROR", 
                    "B³¹d podczas ponownego ³adowania zadañ synchronizacji zamówieñ", 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace
                    });
                
                throw;
            }
        }

        #endregion

        #region File Logging Methods

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

        private async Task LogJobExecution(string jobId, bool success, DateTime startTime, TimeSpan duration, int? statusCode, string? responseData, string? errorMessage)
        {
            try
            {
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
                    ExecutionId = Guid.NewGuid().ToString("N")[..8]
                };

                await WriteLogEntry("job_executions", logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania wykonania zadania {JobId}", jobId);
            }
        }

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

        private async Task WriteLogEntry(string logType, object logEntry)
        {
            try
            {
                var date = DateTime.UtcNow.ToString("yyyyMMdd");
                var filename = $"cronjobs_{logType}_{date}.log";
                var filePath = Path.Combine(_logDirectory, filename);

                // Konfiguracja JSON serialization dla .NET 9
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
                };

                var jsonLine = JsonSerializer.Serialize(logEntry, jsonOptions) + Environment.NewLine;

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

        public void Dispose()
        {
            _logger.LogInformation("CronSchedulerService - zwalnianie zasobów");
            _ = Task.Run(async () => await LogSchedulerEvent("SERVICE_DISPOSING", "Zwalnianie zasobów CronSchedulerService", 
                new { TotalJobs = _scheduledJobs.Count }));
            
            _timer?.Dispose();
        }
    }

    /// <summary>
    /// Reprezentuje zaplanowane zadanie cykliczne z pe³nymi informacjami o jego stanie.
    /// </summary>
    public class ScheduledJob
    {
        public string Id { get; set; } = string.Empty;
        public CronJobSchedule Schedule { get; set; } = new();
        public DateTime NextExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public int ExecutionCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}