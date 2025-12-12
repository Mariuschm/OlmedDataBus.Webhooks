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
        private readonly ProductSyncConfigurationService _productSyncConfigService;
        private readonly IServiceProvider _serviceProvider;
        
        /// <summary>
        /// Inicjalizuje now¹ instancjê CronController.
        /// Token management jest delegowane do CronSchedulerService.
        /// </summary>
        /// <param name="httpClient">Klient HTTP do wykonywania ¿¹dañ</param>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <param name="schedulerService">Serwis zarz¹dzaj¹cy zadaniami cyklicznymi</param>
        /// <param name="productSyncConfigurationService">Serwis zarz¹dzaj¹cy konfiguracj¹ synchronizacji produktów</param>
        /// <param name="serviceProvider">Service provider do tworzenia scope'ów</param>
        public CronController(HttpClient httpClient, ILogger<CronController> logger, IConfiguration configuration, CronSchedulerService schedulerService, ProductSyncConfigurationService productSyncConfigurationService, IServiceProvider serviceProvider)
        {   
            _httpClient = httpClient;
            _logger = logger;            
            _configuration = configuration;
            _schedulerService = schedulerService;
            _productSyncConfigService = productSyncConfigurationService;
            _serviceProvider = serviceProvider;
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
                return NotFound(new { Success = false, Message = "Zadanie nie zosta³a znaleziona" });

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
                return NotFound(new { Success = false, Message = "Zadanie nie zosta³a znaleziona" });

            return Ok(new { Success = true, Message = "Zadanie zosta³o usuniête" });
        }

        /// <summary>
        /// Tworzy zestaw przyk³adowych zadañ cyklicznych do demonstracji funkcjonalnoœci.
        /// Zawiera przyk³ady dla ró¿nych typów harmonogramów (interwa³y, dziennie, tygodniowo).
        /// Zadania synchronizacji produktów s¹ ³adowane z konfiguracji zewnêtrznej.
        /// </summary>
        /// <returns>Lista utworzonych przyk³adowych zadañ</returns>
        /// <response code="200">Przyk³adowe zadania zosta³y utworzone</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas tworzenia zadañ</response>
        [HttpPost("schedule/examples")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateExampleJobs()
        {
            try
            {
                var createdJobs = new List<string>();

                // Automatyczne odœwie¿anie tokena Olmed co 45 minut
                // Zapewnia ci¹g³¹ autoryzacjê bez koniecznoœci ponownego logowania
                _schedulerService.AddOrUpdateJob("olmed-auth-refresh", new CronJobSchedule
                {
                    Type = ScheduleType.Interval,
                    IntervalSeconds = 2700, // 45 minut - bezpieczny margin przed wygaœniêciem tokena (60 min)
                    Request = new CronJobRequest
                    {
                        Method = "POST",
                        Url = "https://localhost:53000/api/cron/auth/refresh-if-needed"
                    }
                });
                createdJobs.Add("olmed-auth-refresh");

                // £adowanie zadañ synchronizacji produktów z konfiguracji JSON
                var productSyncConfigurations = await _productSyncConfigService.GetActiveConfigurationsAsync();
                
                foreach (var config in productSyncConfigurations)
                {
                    try
                    {
                        _schedulerService.AddOrUpdateJob(config.Id, new CronJobSchedule
                        {
                            Type = ScheduleType.Interval,
                            IntervalSeconds = config.IntervalSeconds,
                            Request = new CronJobRequest
                            {
                                Method = config.Method,
                                Url = config.Url,
                                UseOlmedAuth = config.UseOlmedAuth,
                                Headers = config.Headers,
                                Body = config.Body
                            }
                        });
                        
                        createdJobs.Add(config.Id);
                        _logger.LogInformation("Utworzono zadanie synchronizacji z konfiguracji: {JobId} - {Name}", config.Id, config.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "B³¹d podczas tworzenia zadania synchronizacji: {JobId} - {Name}", config.Id, config.Name);
                    }
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"Utworzono {createdJobs.Count} zadañ cyklicznych (w tym {productSyncConfigurations.Count} z konfiguracji JSON)",
                    Jobs = createdJobs,
                    ProductSyncJobs = productSyncConfigurations.Select(c => new 
                    { 
                        Id = c.Id, 
                        Name = c.Name, 
                        Marketplace = c.Marketplace,
                        IntervalSeconds = c.IntervalSeconds,
                        IsActive = c.IsActive,
                        Url = c.Url
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia przyk³adowych zadañ");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Authentication Methods - Metody autoryzacji Olmed API (Simplified Facades)

        /// <summary>
        /// Wykonuje logowanie do API Olmed - uproszczona facade u¿ywaj¹ca wspó³dzielonego storage.
        /// Minimalna implementacja bez duplikowania ca³ej logiki z CronSchedulerService.
        /// </summary>
        /// <returns>Informacje o tokenie</returns>
        /// <response code="200">Logowanie zakoñczone sukcesem</response>
        /// <response code="400">B³¹d logowania</response>
        /// <response code="500">B³¹d serwera</response>
        [HttpPost("auth/olmed-login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedLogin()
        {
            try
            {
                _logger.LogInformation("API wywo³anie logowania Olmed - sprawdzanie istniej¹cego tokena");

                // SprawdŸ czy ju¿ istnieje wa¿ny token
                var existingToken = CronSchedulerService.GetOlmedToken();
                if (existingToken != null)
                {
                    var remainingSeconds = (int)(existingToken.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                    _logger.LogInformation("Zwrócenie istniej¹cego wa¿nego tokena Olmed (wygasa za {Seconds}s)", remainingSeconds);
                    
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Token = existingToken.Token,
                        ExpiresAt = existingToken.ExpiresAt,
                        ExpiresIn = remainingSeconds,
                        Message = "Zwrócono istniej¹cy wa¿ny token"
                    });
                }

                // Minimalna implementacja logowania - u¿ywa tego samego storage co CronSchedulerService
                _logger.LogInformation("Brak wa¿nego tokena - wykonywanie nowego logowania przez CronController");
                
                var username = _configuration["OlmedAuth:Username"] ?? "test_prospeo";
                var password = _configuration["OlmedAuth:Password"] ?? "pvRGowxF%266J%2AM%24";
                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                
                var loginUrl = $"{baseUrl}/erp-api/auth/login?username={username}&password={Uri.EscapeDataString(password)}";

                var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", Encoding.UTF8);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<OlmedAuthResponse>(responseContent);
                    
                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        var tokenInfo = new TokenInfo
                        {
                            Token = authResponse.Token,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 3600),
                            CreatedAt = DateTime.UtcNow
                        };

                        // U¿yj wspólnego storage z CronSchedulerService
                        CronSchedulerService.SetOlmedToken(tokenInfo);

                        _logger.LogInformation("Token Olmed zapisany przez CronController API. Wygasa: {ExpiresAt}", tokenInfo.ExpiresAt);

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

                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = $"B³¹d logowania: {response.StatusCode} - {responseContent}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logowania Olmed przez CronController API");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Pobiera informacje o aktualnym tokenie Olmed - facade do CronSchedulerService.
        /// </summary>
        [HttpGet("auth/olmed-token")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 404)]
        public IActionResult GetOlmedToken()
        {
            var tokenInfo = CronSchedulerService.GetOlmedToken();
            
            if (tokenInfo != null)
            {
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

            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "Brak zapisanego tokena"
            });
        }

        /// <summary>
        /// Inteligentne odœwie¿anie tokena - facade wykorzystuj¹ce CronSchedulerService.
        /// </summary>
        [HttpPost("auth/refresh-if-needed")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> RefreshTokenIfNeeded()
        {
            var tokenInfo = CronSchedulerService.GetOlmedToken();
            
            if (tokenInfo != null)
            {
                // Sprawdzenie czy token wygasa w ci¹gu 5 minut
                if (tokenInfo.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    var remainingSeconds = (int)(tokenInfo.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Token = tokenInfo.Token,
                        ExpiresAt = tokenInfo.ExpiresAt,
                        ExpiresIn = remainingSeconds,
                        Message = "Token jest jeszcze aktywny"
                    });
                }

                _logger.LogInformation("Token wygasa wkrótce - próba odœwie¿enia przez uproszczony refresh");
                
                // Uproszczona próba refresh
                var refreshResult = await SimpleRefreshToken(tokenInfo.Token);
                if (refreshResult != null)
                {
                    return Ok(refreshResult);
                }
            }

            // Fallback - pe³ne logowanie
            _logger.LogInformation("Refresh nieudany lub brak tokena - wykonywanie pe³nego logowania");
            return await OlmedLogin();
        }

        /// <summary>
        /// Uproszczona metoda refresh tokena.
        /// </summary>
        private async Task<AuthResponse?> SimpleRefreshToken(string currentToken)
        {
            try
            {
                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                var refreshUrl = $"{baseUrl}/erp-api/auth/refresh";

                var request = new HttpRequestMessage(HttpMethod.Post, refreshUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {currentToken}");
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", Encoding.UTF8);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<OlmedAuthResponse>(responseContent);
                    
                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        var tokenInfo = new TokenInfo
                        {
                            Token = authResponse.Token,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 3600),
                            CreatedAt = DateTime.UtcNow
                        };

                        CronSchedulerService.SetOlmedToken(tokenInfo);

                        return new AuthResponse
                        {
                            Success = true,
                            Token = authResponse.Token,
                            ExpiresAt = tokenInfo.ExpiresAt,
                            ExpiresIn = authResponse.ExpiresIn ?? 3600,
                            Message = "Token zosta³ odœwie¿ony"
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas refresh tokena");
                return null;
            }
        }

        /// <summary>
        /// Wylogowanie z API Olmed - facade do CronSchedulerService.
        /// </summary>
        [HttpPost("auth/olmed-logout")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedLogout()
        {
            try
            {
                var tokenInfo = CronSchedulerService.GetOlmedToken();
                if (tokenInfo == null)
                {
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Message = "Brak aktywnego tokena do wylogowania"
                    });
                }

                var baseUrl = _configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                var logoutUrl = $"{baseUrl}/erp-api/auth/logout";

                var request = new HttpRequestMessage(HttpMethod.Post, logoutUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {tokenInfo.Token}");
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", Encoding.UTF8);

                var response = await _httpClient.SendAsync(request);

                // Zawsze usuñ token ze storage
                CronSchedulerService.RemoveOlmedToken();

                return Ok(new AuthResponse
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Logout wykonany pomyœlnie" : "Logout nieudany, ale token usuniêty ze storage"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logout");
                CronSchedulerService.RemoveOlmedToken(); // Cleanup
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Manualny refresh tokena - facade do uproszczonej logiki.
        /// </summary>
        [HttpPost("auth/olmed-refresh")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(typeof(AuthResponse), 500)]
        public async Task<IActionResult> OlmedRefreshToken()
        {
            try
            {
                var tokenInfo = CronSchedulerService.GetOlmedToken();
                if (tokenInfo == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Brak tokena do odœwie¿enia. Wykonaj najpierw logowanie."
                    });
                }

                var refreshResult = await SimpleRefreshToken(tokenInfo.Token);
                if (refreshResult != null)
                {
                    return Ok(refreshResult);
                }

                // Fallback - pe³ne logowanie
                return await OlmedLogin();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas refresh tokena");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
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
                    var tokenInfo = CronSchedulerService.GetOlmedToken();
                    if (tokenInfo != null)
                    {
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {tokenInfo.Token}");
                        _logger.LogInformation("Dodano token Olmed do ¿¹dania (wygasa: {ExpiresAt})", tokenInfo.ExpiresAt);
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
                var testEndpoint = $"{olmedBaseUrl}/api/webhook/health";

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
            var activeJobs = _schedulerService.GetAllJobs().Where(j => j.IsActive).ToList();
            
            return Ok(new 
            { 
                Status = "Healthy", 
                Timestamp = DateTime.UtcNow,
                ActiveJobs = activeJobs.Count,
                Jobs = activeJobs.Select(j => new
                {
                    j.Id,
                    j.Schedule.Type,
                    j.NextExecution,
                    j.ExecutionCount,
                    j.IsActive
                }).ToList()
            });
        }

        /// <summary>
        /// Pobiera logi wykonywania zadañ cyklicznych z plików.
        /// </summary>
        /// <param name="logType">Typ logu (job_executions, job_events, scheduler_events)</param>
        /// <param name="date">Data w formacie yyyyMMdd (opcjonalna, domyœlnie dzisiaj)</param>
        /// <param name="jobId">Filtrowanie po ID zadania (opcjonalne)</param>
        /// <returns>Lista wpisów z logów</returns>
        /// <response code="200">Logi zosta³y pobrane</response>
        /// <response code="404">Nie znalezione pliki logów</response>
        /// <response code="400">Nieprawid³owe parametry</response>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string logType = "job_executions",
            [FromQuery] string? date = null,
            [FromQuery] string? jobId = null)
        {
            try
            {
                // Walidacja typu logu
                var validLogTypes = new[] { "job_executions", "job_events", "scheduler_events" };
                if (!validLogTypes.Contains(logType))
                {
                    return BadRequest(new 
                    { 
                        Success = false, 
                        Message = $"Nieprawid³owy typ logu. Dostêpne: {string.Join(", ", validLogTypes)}" 
                    });
                }

                // Parsowanie daty lub u¿ycie dzisiejszej
                var targetDate = date ?? DateTime.UtcNow.ToString("yyyyMMdd");
                if (!DateTime.TryParseExact(targetDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    return BadRequest(new 
                    { 
                        Success = false, 
                        Message = "Nieprawid³owy format daty. U¿yj: yyyyMMdd" 
                    });
                }

                // Œcie¿ka do pliku logów
                var logDirectory = _configuration["CronJobLogging:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "CronJobLogs");
                var filename = $"cronjobs_{logType}_{targetDate}.log";
                var filePath = Path.Combine(logDirectory, filename);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new 
                    { 
                        Success = false, 
                        Message = $"Nie znaleziono pliku logów: {filename}" 
                    });
                }

                // Odczyt i parsowanie logów
                var lines = await System.IO.File.ReadAllLinesAsync(filePath);
                var logs = new List<object>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<JsonElement>(line);
                        
                        // Filtrowanie po jobId jeœli podano
                        if (!string.IsNullOrEmpty(jobId))
                        {
                            if (logEntry.TryGetProperty("JobId", out var jobIdProperty) && 
                                jobIdProperty.GetString() != jobId)
                            {
                                continue;
                            }
                        }

                        logs.Add(logEntry);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Nie mo¿na sparsowaæ linii logu: {Line}, B³¹d: {Error}", line, ex.Message);
                    }
                }

                return Ok(new 
                { 
                    Success = true, 
                    LogType = logType,
                    Date = targetDate,
                    JobId = jobId,
                    TotalEntries = logs.Count,
                    Logs = logs.TakeLast(100).ToList() // Ostatnie 100 wpisów
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania logów: {LogType}, Data: {Date}", logType, date);
                return StatusCode(500, new 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
        }

        /// <summary>
        /// Pobiera listê dostêpnych plików logów zadañ cyklicznych.
        /// </summary>
        /// <returns>Lista dostêpnych plików logów</returns>
        /// <response code="200">Lista plików zosta³a pobrana</response>
        [HttpGet("logs/files")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetLogFiles()
        {
            try
            {
                var logDirectory = _configuration["CronJobLogging:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "CronJobLogs");
                
                if (!Directory.Exists(logDirectory))
                {
                    return Ok(new 
                    { 
                        Success = true, 
                        LogDirectory = logDirectory,
                        Files = new object[0] 
                    });
                }

                var logFiles = Directory.GetFiles(logDirectory, "cronjobs_*.log")
                    .Select(filePath => new
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        Size = new FileInfo(filePath).Length,
                        CreatedAt = System.IO.File.GetCreationTime(filePath),
                        LastModified = System.IO.File.GetLastWriteTime(filePath)
                    })
                    .OrderByDescending(f => f.LastModified)
                    .ToList();

                return Ok(new 
                { 
                    Success = true, 
                    LogDirectory = logDirectory,
                    TotalFiles = logFiles.Count,
                    Files = logFiles 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania listy plików logów");
                return StatusCode(500, new 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
        }

        /// <summary>
        /// Pobiera szczegó³owe informacje o stanie schedulera.
        /// </summary>
        /// <returns>Informacje o schedulerze i aktywnych zadaniach</returns>
        /// <response code="200">Informacje o schedulerze zosta³y pobrane</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetSchedulerStatus()
        {
            try
            {
                var allJobs = _schedulerService.GetAllJobs().ToList();
                var activeJobs = allJobs.Where(j => j.IsActive).ToList();
                var inactiveJobs = allJobs.Where(j => !j.IsActive).ToList();

                // ZnajdŸ najbli¿sze zadanie do wykonania
                var nextJob = activeJobs
                    .Where(j => j.NextExecution > DateTime.UtcNow)
                    .OrderBy(j => j.NextExecution)
                    .FirstOrDefault();

                // Statystyki wykonañ
                var totalExecutions = allJobs.Sum(j => j.ExecutionCount);
                var jobsExecutedToday = allJobs.Where(j => j.LastExecution?.Date == DateTime.UtcNow.Date).ToList();

                return Ok(new
                {
                    Success = true,
                    Scheduler = new
                    {
                        IsRunning = true,
                        StartTime = DateTime.UtcNow, // Placeholder - mo¿na dodaæ rzeczywisty czas startu
                        CheckInterval = "10 seconds",
                        LogDirectory = _configuration["CronJobLogging:Directory"] ?? "CronJobLogs"
                    },
                    Jobs = new
                    {
                        Total = allJobs.Count,
                        Active = activeJobs.Count,
                        Inactive = inactiveJobs.Count,
                        TotalExecutions = totalExecutions,
                        ExecutedToday = jobsExecutedToday.Count
                    },
                    NextExecution = nextJob != null ? new
                    {
                        JobId = nextJob.Id,
                        ScheduledFor = nextJob.NextExecution,
                        TimeRemaining = (nextJob.NextExecution - DateTime.UtcNow).TotalSeconds
                    } : null,
                    RecentActivity = allJobs
                        .Where(j => j.LastExecution.HasValue)
                        .OrderByDescending(j => j.LastExecution)
                        .Take(5)
                        .Select(j => new
                        {
                            JobId = j.Id,
                            LastExecution = j.LastExecution,
                            ExecutionCount = j.ExecutionCount
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania statusu schedulera");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        #region Product Sync Configuration Management - Zarz¹dzanie konfiguracj¹ synchronizacji produktów

        /// <summary>
        /// Pobiera wszystkie konfiguracje synchronizacji produktów.
        /// </summary>
        /// <returns>Lista konfiguracji synchronizacji produktów</returns>
        /// <response code="200">Konfiguracje zosta³y pobrane</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas pobierania konfiguracji</response>
        [HttpGet("product-sync/configurations")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetProductSyncConfigurations()
        {
            try
            {
                var configurations = await _productSyncConfigService.GetAllConfigurationsAsync();
                return Ok(new
                {
                    Success = true,
                    Total = configurations.Count,
                    Active = configurations.Count(c => c.IsActive),
                    Configurations = configurations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji synchronizacji produktów");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Pobiera konkretn¹ konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configurationId">ID konfiguracji</param>
        /// <returns>Konfiguracja synchronizacji produktów</returns>
        /// <response code="200">Konfiguracja zosta³a znaleziona</response>
        /// <response code="404">Konfiguracja nie zosta³a znaleziona</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas pobierania konfiguracji</response>
        [HttpGet("product-sync/configurations/{configurationId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetProductSyncConfiguration(string configurationId)
        {
            try
            {
                var configuration = await _productSyncConfigService.GetConfigurationByIdAsync(configurationId);
                if (configuration == null)
                {
                    return NotFound(new { Success = false, Message = "Konfiguracja nie zosta³a znaleziona" });
                }

                return Ok(new
                {
                    Success = true,
                    Configuration = configuration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji synchronizacji produktów: {ConfigId}", configurationId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Dodaje lub aktualizuje konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configuration">Konfiguracja do dodania/aktualizacji</param>
        /// <returns>Potwierdzenie operacji</returns>
        /// <response code="200">Konfiguracja zosta³a zapisana</response>
        /// <response code="400">Nieprawid³owe dane konfiguracji</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas zapisywania</response>
        [HttpPost("product-sync/configurations")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> SaveProductSyncConfiguration([FromBody] ProductSyncConfiguration configuration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configuration.Id))
                {
                    return BadRequest(new { Success = false, Message = "ID konfiguracji jest wymagane" });
                }

                if (string.IsNullOrWhiteSpace(configuration.Url))
                {
                    return BadRequest(new { Success = false, Message = "URL jest wymagany" });
                }

                var saved = await _productSyncConfigService.SaveConfigurationAsync(configuration);
                if (saved)
                {
                    // Jeœli konfiguracja jest aktywna, utwórz/zaktualizuj odpowiadaj¹ce zadanie
                    if (configuration.IsActive)
                    {
                        _schedulerService.AddOrUpdateJob(configuration.Id, new CronJobSchedule
                        {
                            Type = ScheduleType.Interval,
                            IntervalSeconds = configuration.IntervalSeconds,
                            Request = new CronJobRequest
                            {
                                Method = configuration.Method,
                                Url = configuration.Url,
                                UseOlmedAuth = configuration.UseOlmedAuth,
                                Headers = configuration.Headers,
                                Body = configuration.Body
                            }
                        });
                        _logger.LogInformation("Utworzono/zaktualizowano zadanie dla konfiguracji: {ConfigId}", configuration.Id);
                    }
                    else
                    {
                        // Jeœli konfiguracja nieaktywna, usuñ zadanie ze schedulera
                        _schedulerService.RemoveJob(configuration.Id);
                        _logger.LogInformation("Usuniêto zadanie dla nieaktywnej konfiguracji: {ConfigId}", configuration.Id);
                    }

                    return Ok(new
                    {
                        Success = true,
                        Message = "Konfiguracja zosta³a zapisana pomyœlnie",
                        Configuration = configuration
                    });
                }
                else
                {
                    return StatusCode(500, new { Success = false, Message = "Nie uda³o siê zapisaæ konfiguracji" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania konfiguracji synchronizacji produktów: {ConfigId}", configuration?.Id);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Aktualizuje konkretn¹ konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configurationId">ID konfiguracji</param>
        /// <param name="configuration">Konfiguracja do aktualizacji</param>
        /// <returns>Potwierdzenie operacji</returns>
        /// <response code="200">Konfiguracja zosta³a zaktualizowana</response>
        /// <response code="400">Nieprawid³owe dane konfiguracji</response>
        /// <response code="404">Konfiguracja nie zosta³a znaleziona</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas aktualizacji</response>
        [HttpPut("product-sync/configurations/{configurationId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateProductSyncConfiguration(string configurationId, [FromBody] ProductSyncConfiguration configuration)
        {
            try
            {
                // SprawdŸ czy konfiguracja istnieje
                var existingConfig = await _productSyncConfigService.GetConfigurationByIdAsync(configurationId);
                if (existingConfig == null)
                {
                    return NotFound(new { Success = false, Message = "Konfiguracja nie zosta³a znaleziona" });
                }

                // Upewnij siê ¿e ID siê zgadza
                configuration.Id = configurationId;

                if (string.IsNullOrWhiteSpace(configuration.Url))
                {
                    return BadRequest(new { Success = false, Message = "URL jest wymagany" });
                }

                var saved = await _productSyncConfigService.SaveConfigurationAsync(configuration);
                if (saved)
                {
                    // Zarz¹dzaj zadaniami na podstawie statusu aktywnoœci
                    if (configuration.IsActive)
                    {
                        _schedulerService.AddOrUpdateJob(configuration.Id, new CronJobSchedule
                        {
                            Type = ScheduleType.Interval,
                            IntervalSeconds = configuration.IntervalSeconds,
                            Request = new CronJobRequest
                            {
                                Method = configuration.Method,
                                Url = configuration.Url,
                                UseOlmedAuth = configuration.UseOlmedAuth,
                                Headers = configuration.Headers,
                                Body = configuration.Body
                            }
                        });
                    }
                    else
                    {
                        _schedulerService.RemoveJob(configuration.Id);
                    }

                    return Ok(new
                    {
                        Success = true,
                        Message = "Konfiguracja zosta³a zaktualizowana pomyœlnie",
                        Configuration = configuration
                    });
                }
                else
                {
                    return StatusCode(500, new { Success = false, Message = "Nie uda³o siê zaktualizowaæ konfiguracji" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas aktualizacji configuracji synchronizacji produktów: {ConfigId}", configurationId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Usuwa konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configurationId">ID konfiguracji do usuniêcia</param>
        /// <returns>Potwierdzenie usuniêcia</returns>
        /// <response code="200">Konfiguracja zosta³a usuniêta</response>
        /// <response code="404">Konfiguracja nie zosta³a znaleziona</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas usuwania</response>
        [HttpDelete("product-sync/configurations/{configurationId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteProductSyncConfiguration(string configurationId)
        {
            try
            {
                var deleted = await _productSyncConfigService.DeleteConfigurationAsync(configurationId);
                if (deleted)
                {
                    // Usuñ równie¿ odpowiadaj¹ce zadanie ze schedulera jeœli istnieje
                    _schedulerService.RemoveJob(configurationId);
                    
                    return Ok(new
                    {
                        Success = true,
                        Message = "Konfiguracja i zwi¹zane zadanie zosta³y usuniête"
                    });
                }
                else
                {
                    return NotFound(new { Success = false, Message = "Konfiguracja nie zosta³a znaleziona" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas usuwania konfiguracji synchronizacji produktów: {ConfigId}", configurationId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Odœwie¿a cache konfiguracji synchronizacji produktów.
        /// </summary>
        /// <returns>Potwierdzenie odœwie¿enia</returns>
        /// <response code="200">Cache zosta³ odœwie¿ony</response>
        [HttpPost("product-sync/refresh-cache")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult RefreshProductSyncCache()
        {
            _productSyncConfigService.RefreshCache();
            return Ok(new
            {
                Success = true,
                Message = "Cache konfiguracji synchronizacji produktów zosta³ odœwie¿ony"
            });
        }

        /// <summary>
        /// £aduje zadania synchronizacji z aktualnej konfiguracji do schedulera.
        /// </summary>
        /// <returns>Potwierdzenie za³adowania zadañ</returns>
        /// <response code="200">Zadania zosta³y za³adowane</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas ³adowania zadañ</response>
        [HttpPost("product-sync/reload-jobs")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ReloadProductSyncJobs()
        {
            try
            {
                var configurations = await _productSyncConfigService.GetActiveConfigurationsAsync();
                var loadedJobs = new List<string>();

                foreach (var config in configurations)
                {
                    try
                    {
                        _schedulerService.AddOrUpdateJob(config.Id, new CronJobSchedule
                        {
                            Type = ScheduleType.Interval,
                            IntervalSeconds = config.IntervalSeconds,
                            Request = new CronJobRequest
                            {
                                Method = config.Method,
                                Url = config.Url,
                                UseOlmedAuth = config.UseOlmedAuth,
                                Headers = config.Headers,
                                Body = config.Body
                            }
                        });
                        
                        loadedJobs.Add(config.Id);
                        _logger.LogInformation("Za³adowano zadanie synchronizacji: {JobId} - {Name}", config.Id, config.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "B³¹d podczas ³adowania zadania synchronizacji: {JobId} - {Name}", config.Id, config.Name);
                    }
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"Za³adowano {loadedJobs.Count} zadañ synchronizacji produktów",
                    LoadedJobs = loadedJobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania zadañ synchronizacji produktów");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Prze³adowuje zadania synchronizacji produktów bezpoœrednio z CronSchedulerService.
        /// Usuwa istniej¹ce zadania i ³aduje je ponownie z aktualnej konfiguracji.
        /// </summary>
        /// <returns>Potwierdzenie prze³adowania zadañ</returns>
        /// <response code="200">Zadania zosta³y prze³adowane</response>
        /// <response code="500">Wyst¹pi³ b³¹d podczas prze³adowania zadañ</response>
        [HttpPost("product-sync/reload-from-scheduler")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ReloadProductSyncJobsFromScheduler()
        {
            try
            {
                await _schedulerService.ReloadProductSyncJobs();

                var activeJobs = _schedulerService.GetAllJobs()
                    .Where(j => j.IsActive && (j.Id.StartsWith("olmed-") || j.Id.Contains("-sync-")))
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = $"Prze³adowano zadania synchronizacji produktów z CronSchedulerService",
                    ActiveSyncJobs = activeJobs.Count,
                    Jobs = activeJobs.Select(j => new
                    {
                        j.Id,
                        j.NextExecution,
                        j.ExecutionCount,
                        Url = j.Schedule.Request.Url,
                        Method = j.Schedule.Request.Method,
                        IntervalSeconds = j.Schedule.IntervalSeconds
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas prze³adowania zadañ synchronizacji produktów z CronSchedulerService");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion
    }
}