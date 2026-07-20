using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prospeo.DbContext.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Prosepo.Webhooks.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string ApiKeyHeaderName = "X-API-Key";

    private readonly ProspeoDataContext? _dbContext;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ProspeoDataContext? dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return AuthenticateResult.Fail("Brak nagłówka X-API-Key");
        }

        var apiKey = extractedApiKey.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.Fail("Pusty X-API-Key");
        }

        if (_dbContext == null)
        {
            return AuthenticateResult.Fail("Brak połączenia z bazą danych");
        }

        var firma = await _dbContext.Firmy.FirstOrDefaultAsync(f => f.ApiKey == apiKey);
        if (firma == null)
        {
            return AuthenticateResult.Fail("Nieprawidłowy API Key");
        }

        Context.Items["AuthenticatedFirma"] = firma;
        Context.Items["FirmaId"] = firma.Id;
        Context.Items["FirmaNazwa"] = firma.NazwaFirmy;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, firma.Id.ToString()),
            new Claim(ClaimTypes.Name, firma.NazwaFirmy)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}