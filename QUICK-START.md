# ?? Quick Start - Prosepo Webhooks

## Pierwsze Uruchomienie (Nowy Deweloper)

### 1?? Sklonuj Repozytorium

```bash
git clone https://192.168.88.204/Prospeo/26161_Webbhook
cd OlmedDataBus
```

### 2?? Skonfiguruj User Secrets (WYMAGANE!)

?? **BEZ TEGO KROKU APLIKACJA NIE ZADZIA£A!**

```powershell
# Uruchom skrypt automatycznej konfiguracji
.\setup-user-secrets.ps1
```

Skrypt skonfiguruje wszystkie wymagane dane:
- ? Connection String do bazy danych
- ? Klucze szyfrowania webhook
- ? Dane uwierzytelniania OlmedAuth

### 3?? Zbuduj Projekt

```powershell
dotnet build
```

### 4?? Uruchom Aplikacjê

```powershell
cd Prosepo.Webhooks
dotnet run
```

Aplikacja powinna uruchomiæ siê na: `https://localhost:5001`

### 5?? Weryfikacja

SprawdŸ logi startowe - powinny pokazaæ:
```
? Po³¹czenie z baz¹ danych SQL Server (192.168.88.210/PROSWB) zosta³o pomyœlnie nawi¹zane!
```

---

## ?? Alternatywna Metoda (Rêczna)

Jeœli wolisz rêczn¹ konfiguracjê:

```powershell
cd Prosepo.Webhooks

# Skopiuj template i uzupe³nij danymi
copy appsettings.Local.template.json appsettings.Local.json
notepad appsettings.Local.json
```

Lub u¿yj User Secrets bezpoœrednio:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "TWÓJ_CONNECTION_STRING"
dotnet user-secrets set "OlmedDataBus:WebhookKeys:EncryptionKey" "TWÓJ_KEY"
# ... etc.
```

---

## ?? Problemy?

### "No connection string configured"
**Rozwi¹zanie:** Uruchom `.\setup-user-secrets.ps1`

### "Database connection failed"
**Rozwi¹zanie:** SprawdŸ czy masz dostêp do serwera `192.168.88.210`

### "User secrets not found"
**Rozwi¹zanie:** 
```powershell
cd Prosepo.Webhooks
dotnet user-secrets init --id olmedatabus-webhooks-2024
.\setup-user-secrets.ps1
```

---

## ?? Wiêcej Informacji

- **Pe³na dokumentacja bezpieczeñstwa:** [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md)
- **Deployment do IIS:** [IIS-DEPLOYMENT.md](IIS-DEPLOYMENT.md)
- **CI/CD:** [CI-CD-README.md](CI-CD-README.md)

---

## ? Przydatne Komendy

```powershell
# Wyœwietl skonfigurowane User Secrets
cd Prosepo.Webhooks
dotnet user-secrets list

# Usuñ wszystkie User Secrets
dotnet user-secrets clear

# Uruchom z okreœlonym œrodowiskiem
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run

# Build i publikacja
dotnet publish -c Release
```

---

**Gotowe! Mo¿esz zacz¹æ pracê nad projektem! ??**
