# FileLoggingService - Konfiguracja IsDebug

## Opis

Parametr `IsDebug` w konfiguracji `Logging:File` kontroluje poziomy logowania zapisywane do plików. Pozwala to na elastyczne zarz¹dzanie iloœci¹ generowanych logów w zale¿noœci od œrodowiska.

## Konfiguracja

### Parametr IsDebug

| Wartoœæ | Zachowanie | Zalecane œrodowisko |
|---------|-----------|---------------------|
| `true` | Loguje **wszystkie** poziomy (Trace, Debug, Information, Warning, Error, Critical) | Development, Test |
| `false` | Loguje **tylko b³êdy** (Error, Critical) | Production |

### appsettings.json (Domyœlna konfiguracja)

```json
{
  "Logging": {
    "File": {
      "Directory": "Logs",
      "IsDebug": false,
      "MinLevel": "Information",
      "MaxFileSizeMB": 10,
      "RetainedFileCountLimit": 30,
      "RetentionDays": 30
    }
  }
}
```

### appsettings.Development.json (Œrodowisko deweloperskie)

```json
{
  "Logging": {
    "File": {
      "IsDebug": true
    }
  }
}
```

W œrodowisku deweloperskim `IsDebug` jest ustawione na `true`, co oznacza ¿e wszystkie poziomy logowania bêd¹ zapisywane do plików.

### appsettings.Production.json (Œrodowisko produkcyjne)

```json
{
  "Logging": {
    "File": {
      "Directory": "Logs",
      "IsDebug": false,
      "MinLevel": "Warning",
      "MaxFileSizeMB": 50,
      "RetainedFileCountLimit": 90,
      "RetentionDays": 90
    }
  }
}
```

W œrodowisku produkcyjnym `IsDebug` jest ustawione na `false`, co oznacza ¿e tylko b³êdy (Error i Critical) bêd¹ zapisywane do plików.

## Dzia³anie

### Tryb Debug (IsDebug = true)

Wszystkie poziomy logowania s¹ zapisywane:
- ? **Trace** - Szczegó³owe informacje diagnostyczne
- ? **Debug** - Informacje debugowania
- ? **Information** - Informacyjne komunikaty
- ? **Warning** - Ostrze¿enia
- ? **Error** - B³êdy
- ? **Critical** - Krytyczne b³êdy

**Przyk³ad logów:**
```
[2025-01-20 10:15:30.123] [Information] [webhook] Otrzymano webhook - GUID: abc123...
[2025-01-20 10:15:30.145] [Debug] [webhook] Rozpoczêto deszyfracjê danych...
[2025-01-20 10:15:30.167] [Information] [webhook] Pomyœlnie odszyfrowano webhook
[2025-01-20 10:15:30.189] [Warning] [queue] Kolejka jest pe³na, próba ponowienia za 5s
```

### Tryb Produkcyjny (IsDebug = false)

Tylko b³êdy s¹ zapisywane:
- ? Trace - **NIE ZAPISYWANE**
- ? Debug - **NIE ZAPISYWANE**
- ? Information - **NIE ZAPISYWANE**
- ? Warning - **NIE ZAPISYWANE**
- ? **Error** - B³êdy
- ? **Critical** - Krytyczne b³êdy

**Przyk³ad logów:**
```
[2025-01-20 10:15:30.234] [Error] [webhook] Nie uda³o siê zweryfikowaæ podpisu webhook
Exception: InvalidSignatureException: Signature mismatch
[2025-01-20 10:16:45.567] [Critical] [database] Po³¹czenie z baz¹ danych zosta³o utracone
```

## Implementacja

### FileLoggingService.cs

```csharp
public class FileLoggingService
{
    private readonly bool _isDebug;

    public FileLoggingService(IConfiguration configuration)
    {
        _isDebug = _configuration.GetValue<bool>("Logging:File:IsDebug", false);
    }

    private bool ShouldLog(LogLevel level)
    {
        // W trybie debug logujemy wszystkie poziomy
        if (_isDebug)
        {
            return true;
        }

        // W trybie produkcyjnym logujemy tylko Error i Critical
        return level >= LogLevel.Error;
    }

    public async Task LogAsync(string category, LogLevel level, string message, ...)
    {
        // SprawdŸ czy log powinien byæ zapisany
        if (!ShouldLog(level))
        {
            return;
        }

        // Zapisz log...
    }
}
```

## Przyk³ady u¿ycia

### Kontroler z logowaniem

```csharp
public class WebhookController : ControllerBase
{
    private readonly FileLoggingService _fileLoggingService;

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] WebhookPayload payload)
    {
        // ? Development (IsDebug=true): Zapisze log
        // ? Production (IsDebug=false): Nie zapisze (Information < Error)
        await _fileLoggingService.LogAsync("webhook", LogLevel.Information, 
            "Otrzymano webhook", null, new { Guid = payload.guid });

        try
        {
            // Przetwarzanie...
        }
        catch (Exception ex)
        {
            // ? Development: Zapisze log
            // ? Production: Zapisze log (Error >= Error)
            await _fileLoggingService.LogAsync("webhook", LogLevel.Error, 
                "B³¹d podczas przetwarzania webhook", ex, new { Guid = payload.guid });
        }

        return Ok();
    }
}
```

## Zalety rozwi¹zania

### 1. **Zmniejszenie rozmiaru logów w Production**
W œrodowisku produkcyjnym tylko b³êdy s¹ zapisywane, co znacz¹co zmniejsza rozmiar plików logów.

**Szacunki:**
- Development (IsDebug=true): ~500 MB logów/dzieñ
- Production (IsDebug=false): ~10-50 MB logów/dzieñ (zale¿nie od iloœci b³êdów)

### 2. **£atwiejsze debugowanie w Development**
Wszystkie poziomy logowania pomagaj¹ w szybszym zlokalizowaniu problemów podczas rozwoju.

### 3. **Lepsza wydajnoœæ w Production**
Mniej operacji I/O na dysku = lepsza wydajnoœæ aplikacji.

### 4. **Zgodnoœæ z najlepszymi praktykami**
Rozwi¹zanie zgodne z zasadami:
- **Development**: Verbose logging dla debugowania
- **Production**: Minimal logging dla wydajnoœci i bezpieczeñstwa

## Testowanie

### Test w Development

1. Uruchom aplikacjê w trybie Development:
```bash
dotnet run --environment Development
```

2. SprawdŸ logi w katalogu `Logs/`:
```bash
cat Logs/app_webhook_20250120.log
```

Wszystkie poziomy logowania powinny byæ widoczne.

### Test w Production

1. Uruchom aplikacjê w trybie Production:
```bash
dotnet run --environment Production
```

2. SprawdŸ logi w katalogu `Logs/`:
```bash
cat Logs/app_webhook_20250120.log
```

Tylko b³êdy (Error i Critical) powinny byæ widoczne.

## Zmiana konfiguracji w runtime

### Przez appsettings.json

Edytuj odpowiedni plik konfiguracyjny i zrestartuj aplikacjê:

```json
{
  "Logging": {
    "File": {
      "IsDebug": true  // Zmieñ na false dla trybu produkcyjnego
    }
  }
}
```

### Przez zmienne œrodowiskowe

Ustaw zmienn¹ œrodowiskow¹ (nadpisuje appsettings.json):

**Windows:**
```powershell
$env:Logging__File__IsDebug = "true"
dotnet run
```

**Linux/Mac:**
```bash
export Logging__File__IsDebug=true
dotnet run
```

### Przez User Secrets (Development)

```bash
dotnet user-secrets set "Logging:File:IsDebug" "true"
```

## Monitoring i alerty

### Monitorowanie rozmiaru logów

```powershell
# PowerShell - sprawdŸ rozmiar logów z ostatnich 7 dni
$logs = Get-ChildItem -Path "Logs" -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-7) }

$totalSize = ($logs | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "£¹czny rozmiar logów: $([math]::Round($totalSize, 2)) MB"
```

### Alert na nadmierny rozmiar logów

Jeœli rozmiar logów przekracza 1 GB w trybie Production, mo¿e to wskazywaæ na:
- ? Parametr `IsDebug` ustawiony na `true` w Production
- ? Du¿¹ liczbê b³êdów w aplikacji (wymaga interwencji)

## Najlepsze praktyki

1. ? **Development/Test**: `IsDebug = true` - pe³ne logowanie dla debugowania
2. ? **Production**: `IsDebug = false` - tylko b³êdy dla minimalizacji logów
3. ? Regularnie sprawdzaj rozmiar plików logów
4. ? Skonfiguruj automatyczne czyszczenie starych logów (RetentionDays)
5. ? Monitoruj logi b³êdów w Production (alerty email/SMS)
6. ? NIE zostawiaj `IsDebug = true` w Production d³ugoterminowo

## Troubleshooting

### Problem: Brak logów w pliku

**SprawdŸ:**
1. Czy `IsDebug` jest ustawione prawid³owo dla œrodowiska
2. Czy poziom logowania jest >= Error w Production
3. Czy katalog `Logs/` istnieje i aplikacja ma prawa zapisu
4. Czy wywo³ujesz `FileLoggingService.LogAsync()` z odpowiednim poziomem

### Problem: Za du¿o logów w Production

**Rozwi¹zanie:**
1. Upewnij siê ¿e `IsDebug = false` w appsettings.Production.json
2. SprawdŸ czy zmienna œrodowiskowa nie nadpisuje konfiguracji
3. SprawdŸ czy `ASPNETCORE_ENVIRONMENT` jest ustawione na "Production"

### Problem: Za ma³o logów w Development

**Rozwi¹zanie:**
1. Upewnij siê ¿e `IsDebug = true` w appsettings.Development.json
2. SprawdŸ czy `ASPNETCORE_ENVIRONMENT` jest ustawione na "Development"
3. SprawdŸ czy wywo³ujesz `LogAsync()` z odpowiednimi parametrami

## Zwi¹zek z innymi parametrami

| Parametr | IsDebug = true | IsDebug = false |
|----------|----------------|-----------------|
| MinLevel | Ignorowany | Ignorowany (zawsze Error+) |
| LogLevel (ILogger) | Dzia³a normalnie | Dzia³a normalnie |
| File logging | Wszystkie poziomy | Tylko Error+ |
| Console logging | Bez zmian | Bez zmian |

**Uwaga:** `IsDebug` wp³ywa **TYLKO** na FileLoggingService, nie na standardowe logowanie ASP.NET Core (ILogger).

## Podsumowanie

Parametr `IsDebug` zapewnia:
- ?? **Elastycznoœæ** - ró¿ne poziomy logowania dla ró¿nych œrodowisk
- ?? **Wydajnoœæ** - mniej logów = mniej I/O w Production
- ?? **Debugowanie** - pe³ne logi w Development
- ?? **Oszczêdnoœæ miejsca** - mniejsze pliki logów w Production
- ? **Najlepsze praktyki** - zgodnoœæ ze standardami bran¿owymi

---

**Data:** 2025-01-20  
**Wersja:** 1.0.0  
**Status:** Gotowe do u¿ycia
