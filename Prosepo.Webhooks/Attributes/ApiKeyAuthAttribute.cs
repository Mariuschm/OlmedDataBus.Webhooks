using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Prospeo.DbContext.Data;
using Microsoft.EntityFrameworkCore;

namespace Prosepo.Webhooks.Attributes
{
    /// <summary>
    /// Atrybut autoryzacji API Key
    /// Sprawdza token w nag³ówku X-API-Key z kluczami zapisanymi w bazie danych (tabela Firmy)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string API_KEY_HEADER_NAME = "X-API-Key";

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // SprawdŸ czy nag³ówek X-API-Key jest obecny
            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Brak nag³ówka X-API-Key",
                    message = "API Key jest wymagany do autoryzacji tego endpointa"
                });
                return;
            }

            var apiKey = extractedApiKey.ToString();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Pusty X-API-Key",
                    message = "API Key nie mo¿e byæ pusty"
                });
                return;
            }

            // Pobierz DbContext z DI
            var dbContext = context.HttpContext.RequestServices.GetService<ProspeoDataContext>();
            if (dbContext == null)
            {
                context.Result = new ObjectResult(new
                {
                    error = "B³¹d konfiguracji serwera",
                    message = "Brak po³¹czenia z baz¹ danych"
                })
                {
                    StatusCode = 500
                };
                return;
            }

            try
            {
                // SprawdŸ czy API Key istnieje w bazie danych
                var firma = await dbContext.Firmy
                    .Where(f => f.ApiKey == apiKey)
                    .FirstOrDefaultAsync();

                if (firma == null)
                {
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        error = "Nieprawid³owy API Key",
                        message = "Podany API Key nie zosta³ znaleziony w systemie"
                    });
                    return;
                }

                // Opcjonalnie: mo¿na zapisaæ informacjê o firmie do HttpContext
                context.HttpContext.Items["AuthenticatedFirma"] = firma;
                context.HttpContext.Items["FirmaId"] = firma.Id;
                context.HttpContext.Items["FirmaNazwa"] = firma.NazwaFirmy;
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<ApiKeyAuthAttribute>>();
                logger?.LogError(ex, "B³¹d podczas weryfikacji API Key");

                context.Result = new ObjectResult(new
                {
                    error = "B³¹d autoryzacji",
                    message = "Wyst¹pi³ b³¹d podczas weryfikacji API Key"
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
