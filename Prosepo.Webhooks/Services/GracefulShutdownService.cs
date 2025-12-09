using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Services
{
    public class GracefulShutdownService : IHostedService
    {
        private readonly ILogger<GracefulShutdownService> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _serviceProvider;

        public GracefulShutdownService(
            ILogger<GracefulShutdownService> logger,
            IHostApplicationLifetime lifetime,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _lifetime = lifetime;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStopping.Register(OnApplicationStopping);
            _logger.LogInformation("Graceful Shutdown Service uruchomiony");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Graceful Shutdown Service zatrzymywany");
            return Task.CompletedTask;
        }

        private void OnApplicationStopping()
        {
            _logger.LogInformation("Aplikacja siê zamyka - wykonywanie procedur zamykania...");
            
            try
            {
                // Wykonaj logout z Olmed API
                _ = Task.Run(async () => await PerformOlmedLogout());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas procedur zamykania aplikacji");
            }
        }

        private async Task PerformOlmedLogout()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<GracefulShutdownService>>();

                using var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10); // Krótki timeout dla shutdown

                // SprawdŸ czy jest zapisany token
                var currentToken = GetCurrentOlmedToken();
                if (string.IsNullOrEmpty(currentToken))
                {
                    logger.LogInformation("Brak aktywnego tokena Olmed - pomijanie logout");
                    return;
                }

                var baseUrl = configuration["OlmedAuth:BaseUrl"] ?? "https://draft-csm-connector.grupaolmed.pl";
                var logoutUrl = $"{baseUrl}/erp-api/auth/logout";

                logger.LogInformation("Wykonywanie logout z Olmed API: {Url}", logoutUrl);

                var request = new HttpRequestMessage(HttpMethod.Post, logoutUrl);
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {currentToken}");
                request.Headers.Add("X-CSRF-TOKEN", "");
                request.Content = new StringContent("", System.Text.Encoding.UTF8);

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Logout z Olmed API wykonany pomyœlnie");
                }
                else
                {
                    logger.LogWarning("Logout z Olmed API nieudany: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas logout z Olmed API");
            }
        }

        private string? GetCurrentOlmedToken()
        {
            // Dostêp do statycznego tokena z CronController
            // W prawdziwej aplikacji lepiej by³oby u¿yæ dependency injection lub centralnego cache
            try
            {
                var tokenStorageField = typeof(Controllers.CronController)
                    .GetField("_tokenStorage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (tokenStorageField?.GetValue(null) is Dictionary<string, TokenInfo> tokenStorage)
                {
                    if (tokenStorage.TryGetValue("olmed", out var tokenInfo) && tokenInfo.ExpiresAt > DateTime.UtcNow)
                    {
                        return tokenInfo.Token;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie mo¿na pobraæ tokena Olmed ze storage");
            }

            return null;
        }
    }
}