# ?? String Encryption Helper - Gotowy do u¿ycia!

## ? Co zosta³o dodane

Utworzy³em kompletne narzêdzie do szyfrowania wra¿liwych danych konfiguracyjnych:

### 1. **StringEncryptionHelper** (`Prosepo.Webhooks\Helpers\StringEncryptionHelper.cs`)
   - Szyfrowanie/odszyfrowywanie AES-256
   - Automatyczna detekcja zaszyfrowanych danych
   - Bezpieczne generowanie kluczy

### 2. **EncryptionTool CLI** (`Prosepo.Webhooks\Tools\EncryptionTool.cs`)
   - Narzêdzie linii poleceñ
   - Kolorowe formatowanie
   - Integracja z `dotnet run`

### 3. **PowerShell Helper** (`encrypt-config.ps1`)
   - Uproszczone wywo³ywanie
   - Mo¿liwoœæ u¿ycia w skryptach

### 4. **Dokumentacja**
   - `README_ENCRYPTION.md` - pe³na dokumentacja
   - `ENCRYPTION_QUICK_START.md` - quick start
   - `ENCRYPTION_INTEGRATION_EXAMPLES.md` - przyk³ady integracji

## ?? Quick Start

### Sposób 1: Przez PowerShell (najprostszy)

```powershell
# 1. Wygeneruj klucz
.\encrypt-config.ps1 -GenerateKey

# 2. Zaszyfruj has³o (skopiuj klucz z kroku 1)
.\encrypt-config.ps1 -Encrypt "moje_haslo" -Key "KLUCZ_Z_KROKU_1"

# 3. Zapisz klucz w User Secrets
dotnet user-secrets set "Encryption:Key" "KLUCZ_Z_KROKU_1" --project Prosepo.Webhooks
```

### Sposób 2: Bezpoœrednio przez dotnet CLI

```powershell
# 1. Wygeneruj klucz (pomijaj¹c warningi architektury)
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --generate-key 2>$null

# 2. Zaszyfruj has³o
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --encrypt "moje_haslo" --key "KLUCZ" 2>$null

# 3. Odszyfruj (test)
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --decrypt "ZASZYFROWANE" --key "KLUCZ" 2>$null
```

### Sposób 3: U¿ycie w kodzie

```csharp
using Prosepo.Webhooks.Helpers;

// Odszyfrowywanie has³a API
string encrypted = _configuration["OlmedApi:Password"];
string key = _configuration["Encryption:Key"];
string password = StringEncryptionHelper.Decrypt(encrypted, key);

// LUB automatyczne (bezpieczniejsze):
string password = StringEncryptionHelper.DecryptIfEncrypted(
    _configuration["OlmedApi:Password"],
    _configuration["Encryption:Key"]
);
```

## ?? Pe³ny przyk³ad workflow

```powershell
# 1. Zbuduj projekt (jednorazowo)
dotnet build Prosepo.Webhooks

# 2. Wygeneruj klucz szyfrowania
$key = (dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --generate-key 2>$null) | Select-String -Pattern "^[A-Za-z0-9+/=]+$" | Select-Object -First 1

# 3. Zapisz klucz w User Secrets
dotnet user-secrets set "Encryption:Key" "$key" --project Prosepo.Webhooks

# 4. Zaszyfruj has³o API
$encryptedPassword = (dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --encrypt "moje_super_tajne_haslo" --key "$key" 2>$null) | Select-String -Pattern "^[A-Za-z0-9+/=]+$" | Select-Object -First 1

# 5. Wyœwietl zaszyfrowane has³o
Write-Host "Zaszyfrowane has³o: $encryptedPassword" -ForegroundColor Green

# 6. Zaktualizuj appsettings.json
# Wpisz $encryptedPassword jako wartoœæ OlmedApi:Password

# 7. Test deszyfrowania
$decrypted = (dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --decrypt "$encryptedPassword" --key "$key" 2>$null) | Select-Object -Last 1
Write-Host "Odszyfrowane has³o (test): $decrypted" -ForegroundColor Cyan
```

## ?? Przyk³ad konfiguracji

**appsettings.json** (PRZED - niezabezpieczone):
```json
{
  "OlmedApi": {
    "BaseUrl": "https://api.olmed.pl",
    "Username": "olmed_user",
    "Password": "moje_jawne_haslo"
  }
}
```

**appsettings.json** (PO - zabezpieczone):
```json
{
  "OlmedApi": {
    "BaseUrl": "https://api.olmed.pl",
    "Username": "olmed_user",
    "Password": "qR9tL3xY8mN4pW2jF7bV5cH1kD0sA6gE+zX/yW=="
  }
}
```

? Teraz mo¿esz bezpiecznie commitowaæ plik!

## ?? Integracja z istniej¹cym kodem

**OlmedApiService.cs:**
```csharp
using Prosepo.Webhooks.Helpers;

public class OlmedApiService
{
    private readonly IConfiguration _configuration;
    
    public OlmedApiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    private string GetDecryptedPassword()
    {
        string encrypted = _configuration["OlmedApi:Password"];
        string key = _configuration["Encryption:Key"];
        
        // Automatycznie wykrywa czy zaszyfrowane i odszyfrowuje
        return StringEncryptionHelper.DecryptIfEncrypted(encrypted, key);
    }
    
    public async Task<string> AuthenticateAsync()
    {
        string username = _configuration["OlmedApi:Username"];
        string password = GetDecryptedPassword();
        
        // U¿yj odszyfrowanego has³a...
    }
}
```

## ?? Bezpieczeñstwo

### ? Dobre praktyki (zaimplementowane):
- Klucz 256-bit AES (najwy¿szy standard)
- Losowy IV dla ka¿dego szyfrowania
- Klucz przechowywany oddzielnie od zaszyfrowanych danych
- Base64 encoding dla ³atwego przechowywania

### ?? Checklist wdro¿enia:

1. ? **Build successful** - wszystko skompilowane
2. ? Wygeneruj klucz szyfrowania dla swojego œrodowiska
3. ? Zapisz klucz w User Secrets (dev) lub zmiennych œrodowiskowych (prod)
4. ? Zaszyfruj wszystkie wra¿liwe dane w konfiguracji
5. ? Zaktualizuj kod aby u¿ywa³ `DecryptIfEncrypted`
6. ? Przetestuj lokalnie
7. ? Wdro¿ na produkcjê

## ?? Dokumentacja

Szczegó³owa dokumentacja dostêpna w:
- **`Prosepo.Webhooks\README_ENCRYPTION.md`** - API documentation
- **`ENCRYPTION_QUICK_START.md`** - Quick start guide
- **`ENCRYPTION_INTEGRATION_EXAMPLES.md`** - Integration examples

## ?? Znane problemy

### Warningi podczas kompilacji
Podczas uruchamiania narzêdzia mog¹ pojawiæ siê warningi o niezgodnoœci architektury procesora (x86 vs AMD64). S¹ one bezpieczne i mo¿na je zignorowaæ u¿ywaj¹c `2>$null` w PowerShell lub `2>/dev/null` w Bash.

### Rozwi¹zanie:
```powershell
# Dodaj --no-build i przekieruj stderr
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --generate-key 2>$null
```

## ? Status
- **Build**: ? Successful
- **Tests**: ? Passed (manual)
- **Documentation**: ? Complete
- **Ready for use**: ? Yes

Narzêdzie jest gotowe do u¿ycia! ??
