using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Controller do zarz¹dzania zadaniami cyklicznymi (cron jobs) i autoryzacji Olmed API.
    /// Umo¿liwia planowanie, wykonywanie i monitorowanie zadañ HTTP oraz zarz¹dzanie tokenami autoryzacji.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CronController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CronController> _logger;
        private readonly IConfiguration _configuration;
        private readonly CronSchedulerService _schedulerService;
        
        /// <summary>
        /// Statyczny storage dla tokenów autoryzacji. 
        /// W œrodowisku produkcyjnym warto rozwa¿yæ u¿ycie Redis lub innego cache'a.
        /// </summary>
        private static readonly Dictionary<string, TokenInfo> _tokenStorage = new();

        /// <summary>
        /// Inicjalizuje now¹ instancjê CronController.
        /// </summary>
        /// <param name="httpClient">Klient HTTP do wykonywania ¿¹dañ</param>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <param name="schedulerService">Serwis zarz¹dzaj¹cy zadaniami cyklicznymi</param>
        public CronController(HttpClient httpClient, ILogger<CronController> logger, IConfiguration configuration, CronSchedulerService schedulerService)
        {   
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _schedulerService = schedulerService;
        }

        #region Scheduled Jobs Management - Zarz¹dzanie zadaniami cyklicznymi

        /// <summary>
        /// Dodaje lub aktualizuje zadanie cykliczne w schedulerze.
        /// </summary>
        /// <param name="request">Dane zadania cyklicznego do zaplanowania</param>
        /// <returns>Potwierdzenie dodania zadania z informacj¹ o nastêpnym wykonaniu</returns>
        /// <response code="200">Zadanie zosta³o pomyœlnie zaplanowane</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas planowania zadania</response>
        [HttpPost("schedule")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult ScheduleJob([FromBody] ScheduleJobRequest request)
        {
            try
            {
                _logger.LogInformation("Dodawanie zadania cyklicznego: {JobId}", request.JobId);

                // Dodanie zadania do schedulera - scheduler automatycznie oblicza czas nastêpnego wykonania
                _schedulerService.AddOrUpdateJob(request.JobId, request.Schedule);

                return Ok(new
                {
                    Success = true,
                    JobId = request.JobId,
                    Message = "Zadanie zosta³o zaplanowane pomyœlnie",
                    NextExecution = _schedulerService.GetJob(request.JobId)?.NextExecution
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas planowania zadania: {JobId}", request.JobId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Pobiera listê wszystkich zaplanowanych zadañ cyklicznych.
        /// </summary>
        /// <returns>Lista zadañ z podstawowymi informacjami o ka¿dym zadaniu</returns>
        /// <response code="200">Lista zadañ zosta³a pomyœlnie pobrana</response>
        [HttpGet("schedule")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetAllScheduledJobs()
        {
            // Tworzenie uproszczonej listy zadañ z najwa¿niejszymi informacjami
            var jobs = _schedulerService.GetAllJobs().Select(job => new
            {
                job.Id,
                job.Schedule.Type,
                job.NextExecution,
                job.LastExecution,
                job.ExecutionCount,
                job.IsActive,
                job.CreatedAt,
                RequestUrl = job.Schedule.Request.Url,
                RequestMethod = job.Schedule.Request.Method
            });

            return Ok(new { Success = true, Jobs = jobs });
        }

        /// <summary>
        /// Pobiera szczegó³owe informacje o konkretnym zadaniu cyklicznym.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania</param>
        /// <returns>Pe³ne informacje o zadaniu lub b³¹d jeœli zadanie nie istnieje</returns>
        /// <response code="200">Zadanie zosta³o znalezione</response>
        /// <response code="404">Zadanie o podanym ID nie zosta³o znalezione</response>
        [HttpGet("schedule/{jobId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public IActionResult GetScheduledJob(string jobId)
        {
            var job = _schedulerService.GetJob(jobId);
            if (job == null)
                return NotFound(new { Success = false, Message = "Zadanie nie zosta³o znalezione" });

            return Ok(new { Success = true, Job = job });
        }

        /// <summary>
        /// Usuwa zadanie cykliczne ze schedulera.
        /// </summary>
        /// <param name="jobId">Identyfikator zadania do usuniêcia</param>
        /// <returns>Potwierdzenie usuniêcia lub b³¹d jeœli zadanie nie istnieje</returns>
        /// <response code="200">Zadanie zosta³o pomyœlnie usuniête</response>
        /// <response code="404">Zadanie o podanym ID nie zosta³o znalezione</response>
        [HttpDelete("schedule/{jobId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public IActionResult RemoveScheduledJob(string jobId)
        {
            var removed = _schedulerService.RemoveJob(jobId);
            if (!removed)
                return NotFound(new { Success = false, Message = "Zadanie nie zosta³o znalezione" });

            return Ok(new { Success = true, Message = "Zadanie zosta³o usuniête" });
        }

        /// <summary>
        /// Tworzy zestaw przyk³adowych zadañ cyklicznych do demonstracji funkcjonalnoœci.
        /// Zawiera przyk³ady dla ró¿nych typów harmonogramów (interwa³y, dziennie, tygodniowo).
        /// </summary>
        /// <returns>Lista utworzonych przyk³adowych zadañ</returns>
        /// <response code="200">Przyk³adowe zadania zosta³y utworzone</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas tworzenia zadañ</response>
        [HttpPost("schedule/examples")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult CreateExampleJobs()
        {
            try
            {
                // Przyk³ad 1: Zadanie wykonywane co 30 sekund - przydatne do monitorowania
                _schedulerService.AddOrUpdateJob("test-interval", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 30,
                    Request = new CronJobRequest
                    {
                        Method = "GET",
                        Url = "https://httpbin.org/get",
                        Headers = new Dictionary<string, string> { { "User-Agent", "Prosepo-Scheduler" } }
                    }
                });

                // Przyk³ad 2: Zadanie codzienne o 09:00 - typowe dla raportów dziennych
                _schedulerService.AddOrUpdateJob("daily-report", new CronJobSchedule
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
                _schedulerService.AddOrUpdateJob("weekly-cleanup", new CronJobSchedule
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
                _schedulerService.AddOrUpdateJob("olmed-auth-refresh", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 2700, // 45 minut - bezpieczny margin przed wygaœniêciem tokena (60 min)
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = "http://localhost:5251/api/cron/auth/olmed-login"
                    }
                });

                return Ok(new
                {
                    Success = true,
                    Message = "Utworzono przyk³adowe zadania cykliczne",
                    Jobs = new[] { "test-interval", "daily-report", "weekly-cleanup", "olmed-auth-refresh" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia przyk³adowych zadañ");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Authentication Methods - Metody autoryzacji Olmed API

        /// <summary>
        /// Wykonuje logowanie do API Olmed i przechowuje otrzymany token.
        /// Implementuje dok³adnie ten sam curl command jak w dokumentacji API.
        /// </summary>
        /// <returns>Informacje o otrzymanym tokenie wraz z czasem wygaœniêcia</returns>
        /// <response code="200">Logowanie zakoñczone sukcesem</response>
        /// <response code="400">B³¹d logowania - niepoprawne dane lub odpowiedŸ API</response>
        /// <response code="500">B³¹d serwera podczas procesu logowania</response>
        [HttpPost("auth/olmed-login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedLogin()
        {
            try
            {
                // Pobieranie danych autoryzacji z konfiguracji
                var username = _configuration["OlmedAuth:Username"] ?? "test_prospeo";
                var password = _configuration["OlmedAuth:Password"] ?? "pvRGowxF%266J%2AM%24";
                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                
                // Budowanie URL z parametrami query - zgodnie z dokumentacj¹ API Olmed
                var loginUrl = $"{baseUrl}/erp-api/auth/login?username={username}&password={Uri.EscapeDataString(password)}";

                _logger.LogInformation("Wykonywanie logowania Olmed: {Url}", baseUrl + "/erp-api/auth/login");

                // Przygotowanie ¿¹dania HTTP - dok³adna replika curl command
                var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("X-CSRF-TOKEN", ""); // Wymagany pusty nag³ówek CSRF
                request.Content = new StringContent("", Encoding.UTF8);

                // Wykonanie ¿¹dania logowania
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parsowanie odpowiedzi JSON - oczekiwany format: { "access_token": "...", "expires_in": 3600, "token_type": "bearer" }
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

                        // Przechowywanie tokena w statycznym storage
                        // TODO: W œrodowisku produkcyjnym rozwa¿yæ u¿ycie Redis lub innego cache'a
                        _tokenStorage["olmed"] = tokenInfo;

                        _logger.LogInformation("Token Olmed zosta³ zapisany. Wygasa: {ExpiresAt}", tokenInfo.ExpiresAt);

                        return Ok(new AuthResponse
                        {
                            Success = true,
                            Token = authResponse.Token,
                            ExpiresAt = tokenInfo.ExpiresAt,
                            ExpiresIn = authResponse.ExpiresIn ?? 3600,
                            Message = "Token zosta³ pomyœlnie pobrany i zapisany"
                        });
                    }
                }

                // Logowanie nieudanego logowania z szczegó³ami odpowiedzi
                _logger.LogWarning("Nieudane logowanie Olmed: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = $"B³¹d logowania: {response.StatusCode} - {responseContent}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania Olmed");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Pobiera informacje o aktualnie przechowywanym tokenie Olmed.
        /// Sprawdza czy token jest jeszcze wa¿ny i zwraca informacje o jego stanie.
        /// </summary>
        /// <returns>Informacje o tokenie lub b³¹d jeœli token nie istnieje lub wygas³</returns>
        /// <response code="200">Token jest aktywny</response>
        /// <response code="400">Token wygas³</response>
        /// <response code="404">Brak zapisanego tokena</response>
        [HttpGet("auth/olmed-token")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 404)]
        public IActionResult GetOlmedToken()
        {
            if (_tokenStorage.TryGetValue("olmed", out var tokenInfo))
            {
                // Sprawdzenie czy token jeszcze nie wygas³
                if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                {
                    // Obliczanie pozosta³ego czasu wa¿noœci tokena
                    var remainingSeconds = (int)(tokenInfo.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                    
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Token = tokenInfo.Token,
                        ExpiresAt = tokenInfo.ExpiresAt,
                        ExpiresIn = remainingSeconds,
                        Message = "Token jest aktywny"
                    });
                }
                else
                {
                    // Token wygas³ - automatyczne usuniêcie ze storage
                    _tokenStorage.Remove("olmed");
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Token wygas³"
                    });
                }
            }

            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "Brak zapisanego tokena"
            });
        }

        /// <summary>
        /// Inteligentne odœwie¿anie tokena - sprawdza czy token wymaga odœwie¿enia i wykonuje odpowiedni¹ akcjê.
        /// Logika dzia³ania:
        /// 1. Jeœli token jest wa¿ny przez wiêcej ni¿ 5 minut - zwraca aktualny token
        /// 2. Jeœli token wygasa wkrótce - próbuje refresh przez /auth/refresh
        /// 3. Jeœli refresh siê nie powiedzie - wykonuje pe³ne logowanie
        /// </summary>
        /// <returns>Aktualny lub odœwie¿ony token</returns>
        /// <response code="200">Token jest aktywny lub zosta³ pomyœlnie odœwie¿ony</response>
        /// <response code="400">B³¹d podczas odœwie¿ania tokena</response>
        /// <response code="500">B³¹d serwera podczas procesu odœwie¿ania</response>
        [HttpPost("auth/refresh-if-needed")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> RefreshTokenIfNeeded()
        {
            if (_tokenStorage.TryGetValue("olmed", out var tokenInfo))
            {
                // Sprawdzenie czy token wygasa w ci¹gu 5 minut - proaktywne odœwie¿anie
                if (tokenInfo.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Token = tokenInfo.Token,
                        ExpiresAt = tokenInfo.ExpiresAt,
                        Message = "Token jest jeszcze aktywny"
                    });
                }

                // Token wygasa wkrótce lub ju¿ wygas³ - próba odœwie¿enia
                _logger.LogInformation("Token wygasa wkrótce - próba odœwie¿enia");
                var refreshResult = await TryRefreshToken(tokenInfo.Token);
                if (refreshResult != null)
                {
                    return Ok(refreshResult);
                }
            }

            // Brak tokena lub refresh siê nie powiód³ - pe³ne logowanie jako fallback
            _logger.LogInformation("Refresh tokena nieudany lub brak tokena - wykonywanie pe³nego logowania");
            return await OlmedLogin();
        }

        /// <summary>
        /// Prywatna metoda próbuj¹ca odœwie¿yæ token przez endpoint /auth/refresh.
        /// Implementuje dok³adnie ten sam curl command jak w dokumentacji API.
        /// </summary>
        /// <param name="currentToken">Aktualny token do odœwie¿enia</param>
        /// <returns>Nowy token jeœli operacja siê powiod³a, null w przeciwnym przypadku</returns>
        private async Task<AuthResponse?> TryRefreshToken(string currentToken)
        {
            try
            {
                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                var refreshUrl = $"{baseUrl}/erp-api/auth/refresh";

                _logger.LogInformation("Wykonywanie refresh tokena Olmed: {Url}", refreshUrl);

                // Przygotowanie ¿¹dania refresh - dok³adna replika curl command
                var request = new HttpRequestMessage(HttpMethod.Post, refreshUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {currentToken}"); // U¿ycie aktualnego tokena
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", Encoding.UTF8);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parsowanie odpowiedzi - oczekiwany ten sam format co przy logowaniu
                    var authResponse = JsonSerializer.Deserialize<OlmedAuthResponse>(responseContent);
                    
                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        // Tworzenie nowego obiektu tokena
                        var tokenInfo = new TokenInfo
                        {
                            Token = authResponse.Token,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 3600),
                            CreatedAt = DateTime.UtcNow
                        };

                        // Zast¹pienie starego tokena nowym w storage
                        _tokenStorage["olmed"] = tokenInfo;

                        _logger.LogInformation("Token Olmed zosta³ odœwie¿ony. Nowy token wygasa: {ExpiresAt}", tokenInfo.ExpiresAt);

                        return new AuthResponse
                        {
                            Success = true,
                            Token = authResponse.Token,
                            ExpiresAt = tokenInfo.ExpiresAt,
                            ExpiresIn = authResponse.ExpiresIn ?? 3600,
                            Message = "Token zosta³ pomyœlnie odœwie¿ony"
                        };
                    }
                }

                // Logowanie nieudanego refresh z szczegó³ami
                _logger.LogWarning("Nieudany refresh tokena Olmed: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                
                return null; // Refresh siê nie powiód³ - bêdzie wykonane pe³ne logowanie
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas refresh tokena Olmed");
                return null; // Refresh siê nie powiód³
            }
        }

        #endregion

        #region One-time Execution - Jednorazowe wykonanie zadañ

        /// <summary>
        /// Wykonuje jednorazowe zadanie HTTP na podstawie podanych parametrów.
        /// Obs³uguje automatyczne dodawanie tokena Olmed dla ¿¹dañ do API grupaolmed.pl.
        /// </summary>
        /// <param name="request">Parametry ¿¹dania HTTP do wykonania</param>
        /// <returns>OdpowiedŸ z wynikami wykonania zadania</returns>
        /// <response code="200">Zadanie zosta³o wykonane (niezale¿nie od statusu HTTP odpowiedzi)</response>
        /// <response code="500">B³¹d serwera podczas wykonywania zadania</response>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(CronJobResponse), 200)]
        [ProducesResponseType(typeof(CronJobResponse), 500)]
        public async Task<IActionResult> ExecuteCronJob([FromBody] CronJobRequest request)
        {
            try
            {
                _logger.LogInformation("Wykonywanie zadania cron: {Method} {Url}", request.Method, request.Url);

                // Tworzenie ¿¹dania HTTP na podstawie parametrów
                var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);

                // Dodanie nag³ówków z wyj¹tkiem Content-Type (bêdzie ustawiony automatycznie)
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            // Content-Type bêdzie ustawiony wraz z zawartoœci¹
                            continue;
                        }
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Automatyczne dodanie tokena Olmed dla ¿¹dañ do API Olmed
                if (request.Url.Contains("grupaolmed.pl") && request.UseOlmedAuth == true)
                {
                    if (_tokenStorage.TryGetValue("olmed", out var tokenInfo) && tokenInfo.ExpiresAt > DateTime.UtcNow)
                    {
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {tokenInfo.Token}");
                        _logger.LogInformation("Dodano token Olmed do ¿¹dania");
                    }
                    else
                    {
                        _logger.LogWarning("Brak wa¿nego tokena Olmed dla ¿¹dania do API Olmed");
                    }
                }

                // Dodanie zawartoœci dla metod POST/PUT
                if (!string.IsNullOrEmpty(request.Body) && 
                    (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                     request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)))
                {
                    var contentType = request.Headers?.GetValueOrDefault("Content-Type", "application/json") ?? "application/json";
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                }

                // Wykonanie ¿¹dania HTTP
                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Logowanie odpowiedzi (skrócone dla d³ugich odpowiedzi)
                var logContent = responseContent.Length > 1000 ? responseContent[..1000] + "..." : responseContent;
                _logger.LogInformation("OdpowiedŸ cron job: {StatusCode} - {Content}", response.StatusCode, logContent);

                return Ok(new CronJobResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent,
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas wykonywania zadania cron");
                return StatusCode(500, new CronJobResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Response = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Wykonuje test webhook Olmed - pomocnicze zadanie do testowania po³¹czenia.
        /// Wysy³a ¿¹danie POST do lokalnego endpointu testowego.
        /// </summary>
        /// <returns>OdpowiedŸ z wynikami testu webhook</returns>
        /// <response code="200">Test zosta³ wykonany</response>
        /// <response code="500">B³¹d podczas wykonywania testu</response>
        [HttpPost("execute-olmed-webhook")]
        [ProducesResponseType(typeof(CronJobResponse), 200)]
        [ProducesResponseType(typeof(CronJobResponse), 500)]
        public async Task<IActionResult> ExecuteOlmedWebhookTest()
        {
            try
            {
                // Pobieranie URL-a z konfiguracji
                var olmedBaseUrl = _configuration["OlmedDataBus:BaseUrl"] ?? "http://localhost:5000";
                var testEndpoint = $"{olmedBaseUrl}/api/webhook/test";

                _logger.LogInformation("Wykonywanie testu webhook Olmed: {Url}", testEndpoint);

                // Wys³anie prostego ¿¹dania testowego
                var response = await _httpClient.PostAsync(testEndpoint, 
                    new StringContent("{\"test\": \"cron job\"}", Encoding.UTF8, "application/json"));
                
                var responseContent = await response.Content.ReadAsStringAsync();

                return Ok(new CronJobResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent,
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas wykonywania testu webhook Olmed");
                return StatusCode(500, new CronJobResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Response = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });
            }
        }

        #endregion

        /// <summary>
        /// Endpoint sprawdzaj¹cy stan serwera - health check.
        /// U¿ywany przez systemy monitorowania do weryfikacji dostêpnoœci serwisu.
        /// </summary>
        /// <returns>Status "Healthy" z aktualnym czasem</returns>
        /// <response code="200">Serwer jest sprawny</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }

        #region Logout Methods - Metody wylogowania

        /// <summary>
        /// Wykonuje wylogowanie z API Olmed i usuwa token ze storage.
        /// Implementuje dok³adnie ten sam curl command jak w dokumentacji API.
        /// Token jest usuwany ze storage niezale¿nie od wyniku operacji logout.
        /// </summary>
        /// <returns>Potwierdzenie wylogowania</returns>
        /// <response code="200">Wylogowanie zakoñczone sukcesem lub brak aktywnego tokena</response>
        /// <response code="400">B³¹d podczas wylogowania</response>
        /// <response code="500">B³¹d serwera podczas wylogowania</response>
        [HttpPost("auth/olmed-logout")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedLogout()
        {
            try
            {
                // Sprawdzenie czy istnieje aktywny token
                if (!_tokenStorage.TryGetValue("olmed", out var tokenInfo) || tokenInfo.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Brak aktywnego tokena Olmed - pomijanie logout");
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Message = "Brak aktywnego tokena do wylogowania"
                    });
                }

                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                var logoutUrl = $"{baseUrl}/erp-api/auth/logout";

                _logger.LogInformation("Wykonywanie logout z Olmed API: {Url}", logoutUrl);

                // Przygotowanie ¿¹dania logout - dok³adna replika curl command
                var request = new HttpRequestMessage(HttpMethod.Post, logoutUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {tokenInfo.Token}"); // U¿ycie aktualnego tokena
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", Encoding.UTF8);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                // WA¯NE: Token jest usuwany niezale¿nie od wyniku logout
                // Zapobiega to problemom z "nieœmiertelnymi" tokenami w storage
                _tokenStorage.Remove("olmed");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Logout z Olmed API wykonany pomyœlnie");
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Message = "Logout wykonany pomyœlnie"
                    });
                }
                else
                {
                    _logger.LogWarning("Logout z Olmed API nieudany: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = $"B³¹d logout: {response.StatusCode} - {responseContent}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logout z Olmed API");
                // Usuñ token ze storage nawet przy b³êdzie - cleanup na wszelki wypadek
                _tokenStorage.Remove("olmed");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Manualnie wymusza odœwie¿enie tokena przez endpoint /auth/refresh.
        /// Jeœli refresh siê nie powiedzie, automatycznie wykonuje pe³ne logowanie.
        /// </summary>
        /// <returns>Nowy lub aktualny token</returns>
        /// <response code="200">Token zosta³ pomyœlnie odœwie¿ony lub wykonane pe³ne logowanie</response>
        /// <response code="400">Brak tokena do odœwie¿enia</response>
        /// <response code="500">B³¹d serwera podczas odœwie¿ania</response>
        [HttpPost("auth/olmed-refresh")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedRefreshToken()
        {
            try
            {
                // Sprawdzenie czy istnieje token do odœwie¿enia
                if (!_tokenStorage.TryGetValue("olmed", out var tokenInfo))
                {
                    _logger.LogInformation("Brak tokena do odœwie¿enia");
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Brak tokena do odœwie¿enia. Wykonaj najpierw logowanie."
                    });
                }

                // Próba odœwie¿enia z aktualnym tokenem
                var refreshResult = await TryRefreshToken(tokenInfo.Token);
                if (refreshResult != null)
                {
                    return Ok(refreshResult);
                }

                // Fallback: jeœli refresh siê nie powiód³, wykonaj pe³ne logowanie
                _logger.LogInformation("Refresh tokena nieudany - wykonywanie pe³nego logowania");
                return await OlmedLogin();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas refresh tokena Olmed");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        #endregion
    }
}