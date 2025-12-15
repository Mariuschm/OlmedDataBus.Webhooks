# OlmedDataBus.Webhooks.Client

## ⚠️ WAŻNE: Bezpieczeństwo Konfiguracji

**PRZED pierwszym uruchomieniem** musisz skonfigurować wrażliwe dane (hasła, klucze):

### 🚀 Szybki Start dla Deweloperów

```powershell
# 1. Sklonuj repozytorium
git clone https://192.168.88.204/Prospeo/26161_Webbhook
cd OlmedDataBus

# 2. Skonfiguruj User Secrets (WYMAGANE!)
.\setup-user-secrets.ps1

# 3. Uruchom aplikację
cd Prosepo.Webhooks
dotnet run
```

📖 **Szczegółowa dokumentacja bezpieczeństwa:** [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md)  
📖 **Przewodnik Quick Start:** [QUICK-START.md](QUICK-START.md)

---

## Opis

`OlmedDataBus.Webhooks.Client` to biblioteka .NET umożliwiająca bezpieczną obsługę webhooków pochodzących z szyny danych OLMED. Umożliwia ona:
- weryfikację podpisu HMAC SHA256 przesyłanych danych,
- odszyfrowanie zaszyfrowanego payloadu (AES-256-CBC),
- prostą integrację z aplikacjami .NET (np. ASP.NET Core, Windows Service, itp.).

Dzięki tej bibliotece partnerzy OLMED mogą w łatwy sposób odbierać, weryfikować i odszyfrowywać dane przesyłane przez webhooki.

---

## Przykładowe użycie (na podstawie projektu TestHost)

Poniżej znajduje się uproszczony przykład użycia biblioteki w projekcie ASP.NET Core Web API (`OlmedDataBus.Webhooks.TestHost`):

```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly SecureWebhookHelper _helper;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IConfiguration configuration, ILogger<WebhookController> logger)
    {
        var encryptionKey = configuration["OlmedDataBus:WebhookKeys:EncryptionKey"] ?? string.Empty;
        var hmacKey = configuration["OlmedDataBus:WebhookKeys:HmacKey"] ?? string.Empty;
        _helper = new SecureWebhookHelper(encryptionKey, hmacKey);
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Receive(
        [FromBody] WebhookPayload payload,
        [FromHeader(Name = "X-OLMED-ERP-API-SIGNATURE")] string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest("Brak nagłówka X-OLMED-ERP-API-SIGNATURE.");

        if (_helper.TryDecryptAndVerifyWithIvPrefix(payload.guid, payload.webhookType, payload.webhookData, signature, out var json))
        {
            // Zamiana odszyfrowanego JSON na obiekt
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            _logger.LogInformation("Odszyfrowana zawartość webhooka: {Decrypted}", json);
            return Ok(new { Success = true, Decrypted = obj });
        }
        return BadRequest("Invalid signature or decryption failed.");
    }
}

public class WebhookPayload
{
    public string guid { get; set; } = string.Empty;
    public string webhookType { get; set; } = string.Empty;
    public string webhookData { get; set; } = string.Empty;
}
```

---

## Instrukcja dołączenia biblioteki do projektu

### 1. Dodanie przez NuGet (zalecane, jeśli biblioteka jest publikowana)

W Menedżerze pakietów NuGet wyszukaj `OlmedDataBus.Webhooks.Client` i zainstaluj do swojego projektu.

**lub** przez konsolę:

```
dotnet add package OlmedDataBus.Webhooks.Client
```

### 2. Ręczne dołączenie pliku DLL

Jeśli posiadasz tylko plik `OlmedDataBus.Webhooks.Client.dll`:

1. Skopiuj plik DLL do katalogu swojego projektu (np. do folderu `libs`).
2. Kliknij prawym przyciskiem myszy na projekt w Visual Studio → **Dodaj** → **Odwołanie...**.
3. Wybierz **Przeglądaj** i wskaż plik DLL.
4. Zatwierdź dodanie odwołania.

---

## Konfiguracja kluczy

W pliku `appsettings.json` lub innym źródle konfiguracji należy umieścić klucze:

```json
{
  "OlmedDataBus:WebhookKeys": {
    "EncryptionKey": "mysecretkey1234567890123456mysecretkey12",
    "HmacKey": "mysecretkey1234567890123456mysecretkey12"
  }
}
```

Klucze powinny być przekazane jako tekst o długości 32 znaków (256 bitów - 32 bajty).

---

## Wymagania

- .NET Standard 2.0 (kompatybilność z .NET Core 2.0+, .NET Framework 4.6.1+, .NET 5/6/7/8)
- Do testowania: ASP.NET Core Web API (np. .NET 8)

---

## Licencja

Biblioteka przeznaczona do użytku partnerów Grupy OLMED.