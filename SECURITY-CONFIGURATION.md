# ?? BezpieczeÒstwo Wraøliwych Danych - Dokumentacja

## ?? Spis Treúci
1. [Problem](#problem)
2. [Rozwiπzanie](#rozwiπzanie)
3. [Konfiguracja årodowiska Deweloperskiego](#konfiguracja-úrodowiska-deweloperskiego)
4. [Konfiguracja årodowiska Produkcyjnego](#konfiguracja-úrodowiska-produkcyjnego)
5. [Hierarchia Konfiguracji](#hierarchia-konfiguracji)
6. [Najlepsze Praktyki](#najlepsze-praktyki)

---

## ? Problem

Wraøliwe dane by≥y przechowywane w jawnym tekúcie w pliku `appsettings.json`:
- **Connection String** do bazy danych SQL Server (zawierajπcy has≥o)
- **EncryptionKey** i **HmacKey** dla webhook
- **Nazwa uøytkownika** i **has≥o** do OlmedAuth

To stanowi **powaøne zagroøenie bezpieczeÒstwa**:
- ? Dane sπ widoczne dla kaødego z dostÍpem do repozytorium
- ? Dane sπ commitowane do Git (historia pozostaje)
- ? Brak separacji miÍdzy úrodowiskami (DEV/PROD)
- ? £atwy wyciek danych przy udostÍpnianiu kodu

---

## ? Rozwiπzanie

Implementacja wielopoziomowego systemu zabezpieczeÒ:

### ?? årodowisko Deweloperskie (Development)
**User Secrets** - dane przechowywane lokalnie poza kodem ürÛd≥owym

### ?? årodowisko Produkcyjne (Production)  
**Zmienne årodowiskowe IIS** - dane w konfiguracji Application Pool

---

## ??? Konfiguracja årodowiska Deweloperskiego

### Krok 1: Automatyczna Konfiguracja (Zalecane)

Uruchom skrypt automatycznej konfiguracji:

```powershell
.\setup-user-secrets.ps1
```

**Co robi ten skrypt:**
- ? Inicjalizuje User Secrets dla projektu
- ? Konfiguruje wszystkie wraøliwe dane
- ? Pokazuje lokalizacjÍ pliku secrets.json
- ? Wyúwietla listÍ skonfigurowanych sekretÛw

### Krok 2: RÍczna Konfiguracja (Opcjonalnie)

Jeúli wolisz rÍcznπ konfiguracjÍ:

```powershell
cd Prosepo.Webhooks

# Inicjalizacja User Secrets
dotnet user-secrets init

# Konfiguracja Connection String
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=192.168.88.210;Database=PROSWB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"

# Konfiguracja OlmedDataBus Keys
dotnet user-secrets set "OlmedDataBus:WebhookKeys:EncryptionKey" "YOUR_ENCRYPTION_KEY"
dotnet user-secrets set "OlmedDataBus:WebhookKeys:HmacKey" "YOUR_HMAC_KEY"

# Konfiguracja OlmedAuth
dotnet user-secrets set "OlmedAuth:Username" "YOUR_USERNAME"
dotnet user-secrets set "OlmedAuth:Password" "YOUR_PASSWORD"
```

### Weryfikacja Konfiguracji

```powershell
cd Prosepo.Webhooks
dotnet user-secrets list
```

### Lokalizacja User Secrets

User Secrets sπ przechowywane **poza projektem**:

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\olmedatabus-webhooks-2024\secrets.json
```

**Linux/macOS:**
```
~/.microsoft/usersecrets/olmedatabus-webhooks-2024/secrets.json
```

---

## ?? Konfiguracja årodowiska Produkcyjnego

### Metoda 1: Automatyczna Konfiguracja IIS (Zalecane)

Uruchom skrypt jako **Administrator**:

```powershell
.\setup-production-env.ps1
```

**Co robi ten skrypt:**
- ? Konfiguruje zmienne úrodowiskowe w IIS Application Pool
- ? Ustawia ASPNETCORE_ENVIRONMENT=Production
- ? Restartuje Application Pool
- ? Weryfikuje konfiguracjÍ

### Metoda 2: RÍczna Konfiguracja IIS

1. OtwÛrz **IIS Manager**
2. Przejdü do **Application Pools** ? **ProsepoWebhooks**
3. Kliknij prawym przyciskiem ? **Advanced Settings**
4. Znajdü sekcjÍ **Environment Variables**
5. Dodaj zmienne:

| Nazwa Zmiennej | Przyk≥adowa WartoúÊ |
|----------------|---------------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=...;Database=...;User Id=...;Password=...` |
| `OlmedDataBus__WebhookKeys__EncryptionKey` | `YOUR_ENCRYPTION_KEY` |
| `OlmedDataBus__WebhookKeys__HmacKey` | `YOUR_HMAC_KEY` |
| `OlmedAuth__Username` | `YOUR_USERNAME` |
| `OlmedAuth__Password` | `YOUR_PASSWORD` |

?? **UWAGA:** Uøywaj podwÛjnego podkreúlenia `__` jako separatora poziomÛw konfiguracji!

6. Restartuj Application Pool

### Metoda 3: Zmienne Systemowe Windows

```powershell
# Uruchom jako Administrator
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "YOUR_CONNECTION_STRING", "Machine")
[Environment]::SetEnvironmentVariable("OlmedDataBus__WebhookKeys__EncryptionKey", "YOUR_KEY", "Machine")
# ... pozosta≥e zmienne
```

---

## ?? Hierarchia Konfiguracji

ASP.NET Core wczytuje konfiguracjÍ w nastÍpujπcej kolejnoúci (pÛüniejsze nadpisujπ wczeúniejsze):

```
1. appsettings.json                           ? Wartoúci domyúlne (placeholder)
2. appsettings.{Environment}.json             ? Specyficzne dla úrodowiska
3. User Secrets (tylko Development)           ? årodowisko deweloperskie
4. Environment Variables                      ? Produkcja
5. Command Line Arguments                     ? Nadpisanie w razie potrzeby
```

### Przyk≥ad

```json
// appsettings.json (commitowany do Git)
{
  "ConnectionStrings": {
    "DefaultConnection": "CONFIGURE_VIA_USER_SECRETS_OR_ENVIRONMENT_VARIABLES"
  }
}

// User Secrets - Development (lokalnie)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.88.210;Database=PROSWB;..."
  }
}

// Environment Variables - Production (IIS)
ConnectionStrings__DefaultConnection=Server=PROD_SERVER;Database=PROD_DB;...
```

---

## ?? Najlepsze Praktyki

### ? DO (RÛb to)

1. **Uøywaj User Secrets w Development**
   ```powershell
   dotnet user-secrets set "Key" "Value"
   ```

2. **Uøywaj zmiennych úrodowiskowych w Production**
   - IIS Application Pool Environment Variables
   - Azure App Settings
   - Docker Secrets

3. **RÛøne dane dla rÛønych úrodowisk**
   - DEV: dane testowe
   - PROD: dane produkcyjne

4. **Regularnie rotuj klucze i has≥a**
   - Co 90 dni dla hase≥
   - Co 6 miesiÍcy dla kluczy

5. **Ogranicz dostÍp**
   - Tylko niezbÍdny personel
   - RÛøne has≥a dla DEV/PROD

### ? DON'T (Nie rÛb tego)

1. **NIE commituj wraøliwych danych do Git**
   ```json
   // ? èLE
   "Password": "MyPassword123"
   
   // ? DOBRZE
   "Password": "CONFIGURE_VIA_USER_SECRETS_OR_ENVIRONMENT_VARIABLES"
   ```

2. **NIE udostÍpniaj `secrets.json` innym**
   - Kaødy deweloper konfiguruje w≥asne User Secrets

3. **NIE uøywaj tych samych hase≥ w DEV i PROD**

4. **NIE loguj wraøliwych danych**
   ```csharp
   // ? èLE
   _logger.LogInformation($"Password: {password}");
   
   // ? DOBRZE
   _logger.LogInformation("Authentication configured");
   ```

---

## ?? Weryfikacja Konfiguracji

### Development

```powershell
cd Prosepo.Webhooks
dotnet user-secrets list
dotnet run
```

Sprawdü logi startowe - aplikacja powinna po≥πczyÊ siÍ z bazπ danych.

### Production

1. Sprawdü zmienne úrodowiskowe w IIS:
   - IIS Manager ? Application Pools ? ProsepoWebhooks ? Advanced Settings ? Environment Variables

2. Sprawdü logi aplikacji:
   - `Logs/` katalog w aplikacji

3. Zweryfikuj po≥πczenie z bazπ danych

---

## ?? Troubleshooting

### Problem: "No connection string configured"

**Rozwiπzanie:**
```powershell
# Development
.\setup-user-secrets.ps1

# Production
.\setup-production-env.ps1
```

### Problem: "User Secrets not found"

**Rozwiπzanie:**
```powershell
cd Prosepo.Webhooks
dotnet user-secrets init --id olmedatabus-webhooks-2024
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_VALUE"
```

### Problem: "Application Pool nie widzi zmiennych úrodowiskowych"

**Rozwiπzanie:**
```powershell
# Restart Application Pool
Restart-WebAppPool -Name "ProsepoWebhooks"

# Lub recykling w IIS Manager
```

---

## ?? Dodatkowe Zasoby

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Safe Storage of App Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [IIS Environment Variables](https://learn.microsoft.com/en-us/iis/configuration/system.applicationhost/applicationpools/add/environmentvariables/)

---

## ?? Podsumowanie

### Przed:
? Wraøliwe dane w `appsettings.json` (Git)

### Po:
? **Development:** User Secrets (lokalnie, poza Git)  
? **Production:** Environment Variables (IIS, bezpieczne)  
? **Git:** Tylko placeholdery  
? **BezpieczeÒstwo:** Oddzielne dane DEV/PROD

---

**Autor:** Security Update  
**Data:** 2024  
**Wersja:** 1.0
