# Prosepo.Webhooks - Deployment do IIS

## Przegl¹d

Ten dokument opisuje jak wdro¿yæ aplikacjê Prosepo.Webhooks na serwerze IIS.

## Wymagania

### Opcja 1: Self-Contained Deployment (Zalecane)
- Windows Server z IIS 8.0 lub nowszym
- .NET Core Hosting Bundle **NIE jest wymagany**
- Wszystkie zale¿noœci s¹ zawarte w publikacji

### Opcja 2: Framework-Dependent Deployment
- Windows Server z IIS 8.0 lub nowszym
- **.NET 10 Runtime** musi byæ zainstalowany na serwerze
- .NET Core Hosting Bundle dla .NET 10

## Dostêpne Skrypty Publikacji

### 1. `publish-to-iis.ps1` (Self-Contained - Zalecane)
Publikuje aplikacjê jako self-contained, która zawiera wszystkie zale¿noœci .NET.

**Zalety:**
- Nie wymaga instalacji .NET Runtime na serwerze
- Pe³na kontrola nad wersj¹ .NET
- £atwiejsze zarz¹dzanie ró¿nymi wersjami

**Wady:**
- Wiêkszy rozmiar publikacji (~100MB)

**U¿ycie:**
```powershell
# Domyœlna œcie¿ka: C:\inetpub\wwwroot\ProspeoWebhooks
.\publish-to-iis.ps1

# Niestandardowa œcie¿ka
.\publish-to-iis.ps1 -OutputPath "D:\Apps\ProspeoWebhooks"

# Tryb Debug
.\publish-to-iis.ps1 -Configuration Debug
```

### 2. `publish-to-iis-framework-dependent.ps1` (Framework-Dependent)
Publikuje aplikacjê która wymaga zainstalowanego .NET Runtime na serwerze.

**Zalety:**
- Mniejszy rozmiar publikacji (~5-10MB)
- £atwiejsze aktualizacje security patches przez Microsoft

**Wady:**
- Wymaga zainstalowania .NET 10 Runtime na serwerze
- Potencjalne konflikty wersji

**U¿ycie:**
```powershell
.\publish-to-iis-framework-dependent.ps1
```

## Instalacja .NET 10 Runtime (dla Framework-Dependent)

Jeœli wybierasz deployment framework-dependent, musisz zainstalowaæ .NET 10 Runtime:

1. Pobierz .NET 10 Hosting Bundle:
   ```
   https://dotnet.microsoft.com/download/dotnet/10.0
   ```

2. Zainstaluj Hosting Bundle na serwerze IIS

3. Zrestartuj IIS:
   ```powershell
   iisreset
   ```

4. SprawdŸ zainstalowane runtime:
   ```powershell
   dotnet --list-runtimes
   ```

## Konfiguracja IIS

### Automatyczna Konfiguracja (przez skrypty)
Skrypty automatycznie:
- Tworz¹ pulê aplikacji `ProspeoWebhooksPool`
- Konfiguruj¹ pulê dla .NET Core (No Managed Code)
- Tworz¹ aplikacjê w Default Web Site pod œcie¿k¹ `/ProspeoWebhooks`
- Ustawiaj¹ odpowiednie uprawnienia dla IIS_IUSRS
- Tworz¹ katalogi robocze (Logs, WebhookData, CronJobLogs)

### Rêczna Konfiguracja

#### 1. Utwórz Pulê Aplikacji
```powershell
New-WebAppPool -Name "ProspeoWebhooksPool"
Set-ItemProperty "IIS:\AppPools\ProspeoWebhooksPool" -Name "managedRuntimeVersion" -Value ""
```

#### 2. Utwórz Aplikacjê
```powershell
New-WebApplication -Name "ProspeoWebhooks" `
    -Site "Default Web Site" `
    -PhysicalPath "C:\inetpub\wwwroot\ProspeoWebhooks" `
    -ApplicationPool "ProspeoWebhooksPool"
```

#### 3. Ustaw Uprawnienia
```powershell
icacls "C:\inetpub\wwwroot\ProspeoWebhooks" /grant "IIS_IUSRS:(OI)(CI)F" /T
```

## Weryfikacja Instalacji

Po publikacji, sprawdŸ czy aplikacja dzia³a:

### 1. Health Check
```
http://localhost/ProspeoWebhooks/api/webhook/health
```

Oczekiwana odpowiedŸ:
```json
{
  "status": "Healthy",
  "timestamp": "2024-12-12T...",
  "webhookDirectory": "...",
  "directoryExists": true
}
```

### 2. Swagger UI
```
http://localhost/ProspeoWebhooks/swagger
```

### 3. Test Database Connection
```
http://localhost/ProspeoWebhooks/api/webhook/test/database
```

## Struktura Katalogów

Po publikacji, struktura katalogów bêdzie wygl¹daæ nastêpuj¹co:

```
C:\inetpub\wwwroot\ProspeoWebhooks\
??? Prosepo.Webhooks.exe              # Self-contained executable
??? Prosepo.Webhooks.dll              # Main application DLL
??? web.config                        # IIS configuration
??? appsettings.json                  # Application configuration
??? appsettings.Production.json       # Production overrides
??? Logs\                             # Application logs
??? WebhookData\                      # Webhook payloads
??? CronJobLogs\                      # Cron job logs
??? [inne pliki i zale¿noœci]
```

## Konfiguracja Aplikacji

### appsettings.json

G³ówne sekcje konfiguracji:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  },
  "OlmedDataBus": {
    "BaseUrl": "http://localhost:53000",
    "WebhookKeys": {
      "EncryptionKey": "...",
      "HmacKey": "..."
    }
  },
  "OlmedAuth": {
    "BaseUrl": "https://...",
    "Username": "...",
    "Password": "..."
  }
}
```

### appsettings.Production.json (Opcjonalne)

Utwórz ten plik aby nadpisaæ ustawienia dla œrodowiska produkcyjnego:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Troubleshooting

### Problem: "Unable to locate application dependencies"

**Przyczyna:** .NET Runtime nie jest zainstalowany lub aplikacja nie mo¿e go znaleŸæ.

**Rozwi¹zanie:**
1. U¿yj skryptu `publish-to-iis.ps1` (self-contained)
2. LUB zainstaluj .NET 10 Runtime i u¿yj `publish-to-iis-framework-dependent.ps1`

### Problem: "500.19 - Internal Server Error"

**Przyczyna:** Brak ASP.NET Core Module w IIS.

**Rozwi¹zanie:**
- Zainstaluj .NET Hosting Bundle
- Zrestartuj IIS: `iisreset`

### Problem: Brak dostêpu do katalogów (Logs, WebhookData)

**Przyczyna:** Nieprawid³owe uprawnienia.

**Rozwi¹zanie:**
```powershell
icacls "C:\inetpub\wwwroot\ProspeoWebhooks\Logs" /grant "IIS_IUSRS:(OI)(CI)F"
icacls "C:\inetpub\wwwroot\ProspeoWebhooks\WebhookData" /grant "IIS_IUSRS:(OI)(CI)F"
icacls "C:\inetpub\wwwroot\ProspeoWebhooks\CronJobLogs" /grant "IIS_IUSRS:(OI)(CI)F"
```

### Problem: Aplikacja nie startuje

**Diagnostyka:**
1. SprawdŸ Event Viewer: `Windows Logs > Application`
2. W³¹cz szczegó³owe logowanie w `web.config`:
   ```xml
   <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
   ```
3. SprawdŸ logi w katalogu `logs\`

## Aktualizacja Aplikacji

Aby zaktualizowaæ aplikacjê:

```powershell
# 1. Zatrzymaj aplikacjê
Stop-WebAppPool -Name "ProspeoWebhooksPool"

# 2. Opublikuj now¹ wersjê
.\publish-to-iis.ps1

# 3. Aplikacja zostanie automatycznie uruchomiona przez skrypt
```

## Monitoring i Logs

### Logi Aplikacji
- Lokalizacja: `C:\inetpub\wwwroot\ProspeoWebhooks\Logs\`
- Format: `application_YYYYMMDD.log`
- Rotacja: Dzienna

### Logi Webhooks
- Lokalizacja: `C:\inetpub\wwwroot\ProspeoWebhooks\WebhookData\`
- Zawieraj¹: Surowe i odszyfrowane dane webhook

### Logi Cron Jobs
- Lokalizacja: `C:\inetpub\wwwroot\ProspeoWebhooks\CronJobLogs\`

### Event Viewer
- `Windows Logs > Application`
- Filtruj po Ÿródle: `IIS AspNetCore Module V2`

## Bezpieczeñstwo

### SSL/TLS
Zalecane jest u¿ycie HTTPS w produkcji:

```powershell
# Dodaj binding HTTPS do site
New-WebBinding -Name "Default Web Site" -Protocol https -Port 443
```

### Firewall
Otwórz odpowiednie porty:
```powershell
New-NetFirewallRule -DisplayName "Allow HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### Connection Strings
**NIGDY** nie commituj connection stringów z has³ami do repozytorium!

U¿yj:
- Azure Key Vault
- Windows Credentials Manager
- Environment Variables
- User Secrets (development)

## Performance Tuning

### Pula Aplikacji
```powershell
# Zwiêksz limit pamiêci
Set-ItemProperty "IIS:\AppPools\ProspeoWebhooksPool" -Name "recycling.periodicRestart.memory" -Value 2097152

# Ustaw Idle Timeout
Set-ItemProperty "IIS:\AppPools\ProspeoWebhooksPool" -Name "processModel.idleTimeout" -Value "00:20:00"
```

### Compression
W³¹cz compression w `web.config` dla lepszej wydajnoœci.

## Support

Jeœli masz problemy:
1. SprawdŸ logi aplikacji
2. SprawdŸ Event Viewer
3. Zweryfikuj konfiguracjê IIS
4. SprawdŸ po³¹czenie z baz¹ danych: `/api/webhook/test/database`

---

**Wersja:** 1.0  
**Data:** 2024-12-12  
**Autor:** Prospeo Development Team

## CI/CD Integration

### Dostêpne Skrypty CI/CD

Projekt zawiera skrypty do automatyzacji procesów budowania i wdra¿ania:

#### 1. `ci-cd-build-commit.ps1` - Manual Build & Commit
Manualnie uruchamia build, testy i commituje zmiany.

**U¿ycie:**
```powershell
# Podstawowe u¿ycie
.\ci-cd-build-commit.ps1

# Z niestandardowym commit message
.\ci-cd-build-commit.ps1 -CommitMessage "Feature: Added new webhook endpoint"

# Bez testów
.\ci-cd-build-commit.ps1 -SkipTests

# Bez pushowania
.\ci-cd-build-commit.ps1 -SkipPush

# Debug build
.\ci-cd-build-commit.ps1 -Configuration Debug
```

**Co robi:**
- ? Czyœci poprzednie buildy
- ? Przywraca pakiety NuGet
- ? Buduje projekt
- ? Uruchamia testy (opcjonalne)
- ? Generuje statystyki projektu
- ? Commituje zmiany z detalami buildu
- ? Pushuje do zdalnych repozytoriów

#### 2. `setup-git-hooks.ps1` - Git Hooks Setup
Instaluje Git hooks do automatycznego sprawdzania kodu.

**U¿ycie:**
```powershell
.\setup-git-hooks.ps1
```

**Zainstalowane hooki:**
- **pre-commit**: Sprawdza build przed commitem
- **post-commit**: Loguje commity do pliku
- **pre-push**: Uruchamia testy przed pushem
- **post-merge**: Odbudowuje projekt po merge

**Pomijanie hooków:**
```powershell
# Pomiñ pre-commit hook
git commit --no-verify

# Pomiñ pre-push hook
git push --no-verify
```

#### 3. `ci-cd-watch.ps1` - Watch Mode
Automatycznie monitoruje zmiany i buduje projekt.

**U¿ycie:**
```powershell
# Podstawowy watch mode
.\ci-cd-watch.ps1

# Z auto-commitem
.\ci-cd-watch.ps1 -AutoCommit

# Z auto-commitem i pushem
.\ci-cd-watch.ps1 -AutoCommit -AutoPush

# Niestandardowy interval (domyœlnie 30s)
.\ci-cd-watch.ps1 -IntervalSeconds 60

# Debug build
.\ci-cd-watch.ps1 -Configuration Debug
```

**Co robi:**
- ?? Monitoruje zmiany w plikach projektu
- ?? Automatycznie buduje przy wykryciu zmian
- ?? Opcjonalnie commituje i pushuje zmiany
- ?? Loguje wszystkie operacje z timestampami

### Workflow CI/CD

#### Development Workflow
```powershell
# 1. Zainstaluj Git hooks (jednorazowo)
.\setup-git-hooks.ps1

# 2. Pracuj normalnie - hooki dzia³aj¹ automatycznie
git add .
git commit -m "My changes"  # pre-commit hook sprawdzi build
git push                     # pre-push hook uruchomi testy

# 3. Lub u¿yj skryptu do manualnego buildu
.\ci-cd-build-commit.ps1 -CommitMessage "Feature complete"
```

#### Continuous Development (Watch Mode)
```powershell
# Uruchom watch mode w osobnym terminalu
.\ci-cd-watch.ps1 -AutoCommit

# Pracuj normalnie - zmiany bêd¹ automatycznie buildowane i commitowane
```

#### Production Deployment
```powershell
# 1. Build i commit zmian
.\ci-cd-build-commit.ps1 -Configuration Release

# 2. Opublikuj do IIS
.\publish-to-iis.ps1
```

### Logi CI/CD

Wszystkie logi s¹ zapisywane w katalogu `build-logs/`:

```
build-logs/
??? commits.log           # Historia commitów
??? .gitignore           # Wykluczone z repo
```

### Best Practices

1. **Zawsze u¿ywaj Git hooks** - Zapobiegaj¹ commitowaniu niedzia³aj¹cego kodu
2. **Uruchamiaj testy przed pushem** - pre-push hook to zrobi automatycznie
3. **U¿ywaj watch mode podczas development** - Oszczêdza czas
4. **Commituj regularnie** - Ma³e, czêste commity s¹ lepsze ni¿ du¿e
5. **Sprawdzaj build logs** - Zawieraj¹ cenne informacje o buildzie

### Troubleshooting CI/CD

#### Problem: Hook nie dzia³a
**Rozwi¹zanie:**
```powershell
# Upewnij siê ¿e PowerShell mo¿e wykonywaæ skrypty
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

# Przeinstaluj hooki
.\setup-git-hooks.ps1
```

#### Problem: Watch mode nie wykrywa zmian
**Rozwi¹zanie:**
- Zwiêksz interval: `.\ci-cd-watch.ps1 -IntervalSeconds 60`
- SprawdŸ czy pliki nie s¹ w katalogu excluded (bin, obj)

#### Problem: Auto-commit tworzy za du¿o commitów
**Rozwi¹zanie:**
- U¿ywaj watch mode bez -AutoCommit
- Commituj manualnie: `.\ci-cd-build-commit.ps1`
- Zwiêksz interval sprawdzania

---

**Wersja:** 1.1  
**Data:** 2024-12-12  
**Autor:** Prospeo Development Team
