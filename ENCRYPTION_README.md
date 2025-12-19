# String Encryption Helper

Helper do bezpiecznego szyfrowania wra¿liwych danych konfiguracyjnych (has³a API, connection stringi, tokeny).

## Szybki start

```powershell
# 1. Wygeneruj klucz
dotnet build Prosepo.Webhooks
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --generate-key

# 2. Zapisz klucz w User Secrets  
dotnet user-secrets set "Encryption:Key" "WYGENEROWANY_KLUCZ" --project Prosepo.Webhooks

# 3. Zaszyfruj has³o
dotnet run --project Prosepo.Webhooks --no-build -- encrypt-tool --encrypt "moje_haslo" --key "KLUCZ"

# 4. U¿yj w appsettings.json
# { "OlmedApi": { "Password": "ZASZYFROWANE_HASLO" } }

# 5. U¿yj w kodzie
using Prosepo.Webhooks.Helpers;
string password = StringEncryptionHelper.DecryptIfEncrypted(
    _configuration["OlmedApi:Password"],
    _configuration["Encryption:Key"]
);
```

## PowerShell Helper

```powershell
.\encrypt-config.ps1 -GenerateKey
.\encrypt-config.ps1 -Encrypt "tekst" -Key "klucz"
.\encrypt-config.ps1 -Decrypt "zaszyfrowane" -Key "klucz"
```

## Dokumentacja

- **ENCRYPTION_READY.md** - Quick start i status
- **Prosepo.Webhooks\README_ENCRYPTION.md** - Pe³na dokumentacja API
- **ENCRYPTION_QUICK_START.md** - Szczegó³owe przyk³ady
- **ENCRYPTION_INTEGRATION_EXAMPLES.md** - Przyk³ady integracji z kodem

## Pliki

- `Prosepo.Webhooks\Helpers\StringEncryptionHelper.cs` - Core helper (AES-256)
- `Prosepo.Webhooks\Tools\EncryptionTool.cs` - CLI tool
- `encrypt-config.ps1` - PowerShell helper script

## Bezpieczeñstwo

? AES-256 encryption  
? Losowy IV dla ka¿dego szyfrowania  
? Klucz oddzielony od danych (User Secrets / Environment Variables)  
? Base64 encoding dla ³atwego przechowywania  

? NIE commituj klucza szyfrowania do repozytorium!  
? Mo¿esz commitowaæ zaszyfrowane dane (s¹ bezpieczne bez klucza)
