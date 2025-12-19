# ?? String Encryption Helper - Podsumowanie

## Dodane pliki

### 1. Core Components
- **`Prosepo.Webhooks\Helpers\StringEncryptionHelper.cs`** - Helper do szyfrowania/odszyfrowywania stringów (AES-256)
- **`Prosepo.Webhooks\Tools\EncryptionTool.cs`** - Narzêdzie CLI do zarz¹dzania szyfrowaniem

### 2. Documentation
- **`Prosepo.Webhooks\README_ENCRYPTION.md`** - Pe³na dokumentacja helpera
- **`ENCRYPTION_QUICK_START.md`** - Quick start guide z przyk³adami
- **`ENCRYPTION_INTEGRATION_EXAMPLES.md`** - Przyk³ady integracji z kodem

### 3. Utilities
- **`encrypt-config.ps1`** - PowerShell helper do ³atwego u¿ycia narzêdzia CLI

## Funkcjonalnoœci

### StringEncryptionHelper
? **GenerateKey()** - Generuje nowy klucz szyfrowania (Base64)
? **Encrypt(text, key)** - Szyfruje tekst u¿ywaj¹c AES-256
? **Decrypt(cipherText, key)** - Odszyfrowuje zaszyfrowany tekst
? **IsEncrypted(text)** - Sprawdza czy tekst jest w formacie zaszyfrowanym
? **DecryptIfEncrypted(text, key)** - Odszyfrowuje tylko jeœli zaszyfrowane (safe)

### EncryptionTool CLI
? Generowanie kluczy szyfrowania
? Szyfrowanie tekstów z linii poleceñ
? Odszyfrowywanie tekstów z linii poleceñ
? Kolorowe formatowanie output

### PowerShell Helper
? Uproszczone wywo³ywanie narzêdzia CLI
? Automatyczne parsowanie outputu
? Mo¿liwoœæ u¿ycia w skryptach automatyzacji

## Quick Start

### 1. Wygeneruj klucz
```powershell
.\encrypt-config.ps1 -GenerateKey
```

### 2. Zapisz klucz w User Secrets
```powershell
dotnet user-secrets set "Encryption:Key" "WYGENEROWANY_KLUCZ" --project Prosepo.Webhooks
```

### 3. Zaszyfruj has³o
```powershell
.\encrypt-config.ps1 -Encrypt "moje_haslo" -Key "KLUCZ"
```

### 4. U¿yj w konfiguracji
```json
{
  "OlmedApi": {
    "Password": "ZASZYFROWANE_HASLO"
  }
}
```

### 5. U¿yj w kodzie
```csharp
using Prosepo.Webhooks.Helpers;

string encrypted = _configuration["OlmedApi:Password"];
string key = _configuration["Encryption:Key"];
string password = StringEncryptionHelper.Decrypt(encrypted, key);
```

## U¿ycie CLI

### Bezpoœrednio przez dotnet
```bash
# Generuj klucz
dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key

# Szyfruj
dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt "text" --key "KEY"

# Odszyfrowuj
dotnet run --project Prosepo.Webhooks -- encrypt-tool --decrypt "ENCRYPTED" --key "KEY"
```

### Przez PowerShell Helper
```powershell
# Generuj klucz
$key = .\encrypt-config.ps1 -GenerateKey

# Szyfruj
$encrypted = .\encrypt-config.ps1 -Encrypt "text" -Key $key

# Odszyfrowuj
$decrypted = .\encrypt-config.ps1 -Decrypt $encrypted -Key $key
```

## Bezpieczeñstwo

### ? Dobre praktyki
- Klucz szyfrowania w User Secrets (dev) lub zmiennych œrodowiskowych (prod)
- Ró¿ne klucze dla ró¿nych œrodowisk
- Regularna rotacja kluczy (co 6-12 miesiêcy)
- Nie logowaæ odszyfrowanych wartoœci

### ? Czego unikaæ
- Nie commitowaæ klucza do repozytorium
- Nie u¿ywaæ tego samego klucza wszêdzie
- Nie przechowywaæ klucza w kodzie Ÿród³owym
- Nie wysy³aæ klucza przez niezabezpieczone kana³y

## Przyk³ady u¿ycia

### Przyk³ad 1: Zaszyfruj has³o API
```powershell
$key = .\encrypt-config.ps1 -GenerateKey
$encrypted = .\encrypt-config.ps1 -Encrypt "api_password_123" -Key $key
# Zapisz $encrypted w appsettings.json
```

### Przyk³ad 2: Zaszyfruj connection string
```powershell
$dbPassword = .\encrypt-config.ps1 -Encrypt "db_password_456" -Key $key
# U¿yj $dbPassword w konfiguracji bazy danych
```

### Przyk³ad 3: U¿ycie w OlmedApiService
```csharp
private string GetPassword()
{
    string encrypted = _configuration["OlmedApi:Password"];
    string key = _configuration["Encryption:Key"];
    return StringEncryptionHelper.Decrypt(encrypted, key);
}
```

## Integracja z istniej¹cym kodem

### OlmedApiService.cs
```csharp
using Prosepo.Webhooks.Helpers;

public class OlmedApiService
{
    private string GetDecryptedPassword()
    {
        string encrypted = _configuration["OlmedApi:Password"];
        string key = _configuration["Encryption:Key"];
        
        // Automatycznie wykrywa czy zaszyfrowane
        return StringEncryptionHelper.DecryptIfEncrypted(encrypted, key);
    }
}
```

## Konfiguracja œrodowisk

### Development (User Secrets)
```powershell
dotnet user-secrets set "Encryption:Key" "KEY_HERE" --project Prosepo.Webhooks
```

### Production (Windows Server)
```powershell
[Environment]::SetEnvironmentVariable("Encryption__Key", "KEY_HERE", "Machine")
```

### Production (Linux)
```bash
export Encryption__Key="KEY_HERE"
```

## Testowanie

### Test kompletnego workflow
```powershell
# Wygeneruj, zaszyfruj, odszyfruj
$key = .\encrypt-config.ps1 -GenerateKey
$encrypted = .\encrypt-config.ps1 -Encrypt "test" -Key $key
$decrypted = .\encrypt-config.ps1 -Decrypt $encrypted -Key $key
Write-Host "Test: $decrypted" # Powinno wyœwietliæ "test"
```

## Troubleshooting

### Problem: Brak klucza szyfrowania
**Rozwi¹zanie:** Ustaw klucz w User Secrets lub zmiennych œrodowiskowych

### Problem: FormatException
**Rozwi¹zanie:** Skopiuj pe³ny zaszyfrowany tekst (z `==` na koñcu)

### Problem: CryptographicException
**Rozwi¹zanie:** U¿ywasz z³ego klucza - u¿yj tego samego co przy szyfrowaniu

## Nastêpne kroki

1. ? Wygeneruj klucz dla swojego œrodowiska
2. ? Zaszyfruj wszystkie wra¿liwe dane
3. ? Zaktualizuj kod aby u¿ywa³ `DecryptIfEncrypted`
4. ? Przetestuj lokalnie
5. ? Wdro¿ na produkcjê z w³aœciwym kluczem

## Dokumentacja

Szczegó³owa dokumentacja:
- **README_ENCRYPTION.md** - Pe³na dokumentacja API
- **ENCRYPTION_QUICK_START.md** - Quick start z przyk³adami
- **ENCRYPTION_INTEGRATION_EXAMPLES.md** - Przyk³ady integracji

## Build Status
? **Build successful** - Wszystkie pliki skompilowane poprawnie
? **Ready to use** - Narzêdzie gotowe do u¿ycia
