using Prosepo.Webhooks.Helpers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prosepo.Webhooks.Services
{
    /// <summary>
    /// Serwis do komunikacji z API Olmed
    /// Obs³uguje autentykacjê i wysy³anie ¿¹dañ do systemu Olmed
    /// </summary>
    public class OlmedApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OlmedApiService> _logger;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private string? _cachedToken;
        private DateTime _tokenExpiration = DateTime.MinValue;
        private readonly string secureKey = Environment.GetEnvironmentVariable("PROSPEO_KEY") ?? "CPNFWqXE3TMY925xMgUPlUnWkjSyo9182PpYM69HM44=";

        public OlmedApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OlmedApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _baseUrl = configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
            _username = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedAuth:Username"], secureKey) ?? "test_prospeo";
            _password = StringEncryptionHelper.DecryptIfEncrypted(configuration["OlmedAuth:Password"], secureKey) ?? "pvRGowxF%266J%2AM%24";
        }

        /// <summary>
        /// Pobiera token autoryzacyjny z API Olmed
        /// Token jest cachowany przez 50 minut (wygasa po 60 minutach)
        /// </summary>
        private async Task<string?> GetAuthTokenAsync()
        {
            // SprawdŸ czy mamy wa¿ny token w cache
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedToken;
            }

            try
            {
                var loginUrl = $"{_baseUrl}/erp-api/auth/login";
                var loginData = new
                {
                    username = _username,
                    password = _password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(loginUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("B³¹d podczas logowania do Olmed API: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                if (tokenResponse != null && tokenResponse.TryGetValue("access_token", out var tokenElement))
                {
                    _cachedToken = tokenElement.GetString();
                    _tokenExpiration = DateTime.UtcNow.AddMinutes(50); // Cache na 50 minut (token wygasa po 60)
                    _logger.LogInformation("Pomyœlnie uzyskano token autoryzacyjny Olmed");
                    return _cachedToken;
                }

                _logger.LogError("Nie znaleziono tokena w odpowiedzi Olmed API");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyj¹tek podczas uzyskiwania tokena Olmed");
                return null;
            }
        }

        /// <summary>
        /// Wysy³a ¿¹danie POST do API Olmed z autoryzacj¹
        /// </summary>
        public async Task<(bool Success, string? Response, int StatusCode)> PostAsync(string endpoint, object requestBody)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Nie mo¿na uzyskaæ tokena autoryzacyjnego");
                    return (false, "B³¹d autoryzacji", 401);
                }

                var url = $"{_baseUrl}{endpoint}";
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Wysy³anie ¿¹dania POST do Olmed: {Url}", url);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("OdpowiedŸ Olmed API: Status={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);

                return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300,
                        responseContent,
                        (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas wysy³ania ¿¹dania do Olmed API: {Endpoint}", endpoint);
                return (false, ex.Message, 500);
            }
        }

        /// <summary>
        /// Wysy³a ¿¹danie GET do API Olmed z autoryzacj¹
        /// </summary>
        public async Task<(bool Success, string? Response, int StatusCode)> GetAsync(string endpoint)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Nie mo¿na uzyskaæ tokena autoryzacyjnego");
                    return (false, "B³¹d autoryzacji", 401);
                }

                var url = $"{_baseUrl}{endpoint}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Wysy³anie ¿¹dania GET do Olmed: {Url}", url);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("OdpowiedŸ Olmed API: Status={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);

                return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300,
                        responseContent,
                        (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas wysy³ania ¿¹dania do Olmed API: {Endpoint}", endpoint);
                return (false, ex.Message, 500);
            }
        }

        /// <summary>
        /// Resetuje cachowany token (przydatne gdy token wygas³)
        /// </summary>
        public void ResetToken()
        {
            _cachedToken = null;
            _tokenExpiration = DateTime.MinValue;
        }
    }
}
