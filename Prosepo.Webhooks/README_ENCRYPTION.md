# ?? Narzêdzie do szyfrowania wra¿liwych danych konfiguracyjnych

## Przegl¹d

Helper do szyfrowania i odszyfrowywania stringów przy u¿yciu AES-256. Pozwala na bezpieczne przechowywanie wra¿liwych danych konfiguracyjnych takich jak:
- Has³a do API
- Connection stringi
- Klucze API
- Tokeny dostêpu

## Struktura

### 1. `StringEncryptionHelper.cs`
Helper zawieraj¹cy metody szyfrowania:
- `GenerateKey()` - generuje nowy klucz szyfrowania
- `Encrypt(plainText, key)` - szyfruje tekst
- `Decrypt(cipherText, key)` - odszyfrowuje tekst
- `IsEncrypted(text)` - sprawdza czy tekst jest zaszyfrowany
- `DecryptIfEncrypted(text, key)` - odszyfrowuje tylko jeœli tekst jest zaszyfrowany

### 2. `EncryptionTool.cs`
Narzêdzie CLI do zarz¹dzania szyfrowaniem

## U¿ycie

### 1. Generowanie klucza szyfrowania

```bash
dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key
```

Przyk³adowy wynik:
```
?? Wygenerowany klucz szyfrowania:
ABC123XYZ789QWERTY...==

??  WA¯NE: Zapisz ten klucz w bezpiecznym miejscu!
   Dodaj go do User Secrets lub zmiennych œrodowiskowych jako 'Encryption:Key'
```

### 2. Szyfrowanie tekstu

```bash
dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt "moje_haslo_api" --key "ABC123XYZ789..."
```

Przyk³adowy wynik:
```
?? Zaszyfrowany tekst:
XYZ789ABC123ENCRYPTED...==
```

### 3. Odszyfrowywanie tekstu

```bash
dotnet run --project Prosepo.Webhooks -- encrypt-tool --decrypt "XYZ789ABC123ENCRYPTED...==" --key "ABC123XYZ789..."
```

Przyk³adowy wynik:
```
?? Odszyfrowany tekst:
moje_haslo_api
```

## Konfiguracja klucza szyfrowania

### Option 1: User Secrets (Rekomendowane dla Development)

```bash
dotnet user-secrets set "Encryption:Key" "ABC123XYZ789..." --project Prosepo.Webhooks
```

### Option 2: Zmienne œrodowiskowe (Rekomendowane dla Production)

**Windows (PowerShell):**
```powershell
[Environment]::SetEnvironmentVariable("Encryption__Key", "ABC123XYZ789...", "Machine")
```

**Linux/Mac:**
```bash
export Encryption__Key="ABC123XYZ789..."
```

### Option 3: appsettings.json (NIE REKOMENDOWANE - tylko dla testów)

```json
{
  "Encryption": {
    "Key": "ABC123XYZ789..."
  }
}
```

?? **UWAGA**: Nie commituj klucza szyfrowania do repozytorium!

## Przyk³ad u¿ycia w kodzie

### Podstawowe szyfrowanie/odszyfrowywanie

```csharp
using Prosepo.Webhooks.Helpers;

// Wygeneruj klucz (tylko raz)
string key = StringEncryptionHelper.GenerateKey();

// Zaszyfruj has³o
string password = "moje_super_tajne_haslo";
string encrypted = StringEncryptionHelper.Encrypt(password, key);

// Zapisz zaszyfrowane has³o do konfiguracji
// encrypted = "XYZ789ABC123..."

// PóŸniej, odczytaj i odszyfruj
string decrypted = StringEncryptionHelper.Decrypt(encrypted, key);
// decrypted = "moje_super_tajne_haslo"
```

### Integracja z IConfiguration

```csharp
public class MyService
{
    private readonly IConfiguration _configuration;
    
    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetDecryptedApiPassword()
    {
        string encryptedPassword = _configuration["OlmedApi:Password"];
        string encryptionKey = _configuration["Encryption:Key"];
        
        return StringEncryptionHelper.Decrypt(encryptedPassword, encryptionKey);
    }
}
```

### Odszyfrowywanie tylko jeœli zaszyfrowane

```csharp
// Jeœli wartoœæ mo¿e byæ zaszyfrowana lub nie
string value = _configuration["SomeValue"];
string encryptionKey = _configuration["Encryption:Key"];

// Automatycznie odszyfruje jeœli zaszyfrowane, w przeciwnym razie zwróci oryginaln¹ wartoœæ
string actualValue = StringEncryptionHelper.DecryptIfEncrypted(value, encryptionKey);
```

## PowerShell Helper Script

Mo¿esz u¿yæ skryptu `encrypt-config.ps1`:

```powershell
# Wygeneruj klucz
.\encrypt-config.ps1 -GenerateKey

# Zaszyfruj wartoœæ
.\encrypt-config.ps1 -Encrypt "moje_haslo" -Key "ABC123..."

# Odszyfruj wartoœæ
.\encrypt-config.ps1 -Decrypt "XYZ789..." -Key "ABC123..."
```

## Bezpieczeñstwo

### ? Dobre praktyki:
1. **Klucz szyfrowania:**
   - Nigdy nie commituj do repozytorium
   - Przechowuj w User Secrets (dev) lub zmiennych œrodowiskowych (prod)
   - U¿ywaj ró¿nych kluczy dla ró¿nych œrodowisk
   
2. **Zaszyfrowane dane:**
   - Mo¿na commitowaæ do repozytorium (s¹ bezpieczne bez klucza)
   - Przechowuj w appsettings.json lub bazie danych
   
3. **Rotacja kluczy:**
   - Regularnie zmieniaj klucze szyfrowania
   - Odszyfruj stare dane i zaszyfruj ponownie nowym kluczem

### ? Czego NIE robiæ:
- Nie przechowuj klucza w kodzie Ÿród³owym
- Nie loguj odszyfrowanych wartoœci
- Nie wysy³aj klucza przez niezabezpieczone kana³y
- Nie u¿ywaj tego samego klucza na wszystkich œrodowiskach

## Przyk³ad workflow

### 1. Setup dla nowego projektu

```bash
# Wygeneruj klucz
dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key

# Zapisz klucz w User Secrets
dotnet user-secrets set "Encryption:Key" "WYGENEROWANY_KLUCZ" --project Prosepo.Webhooks
```

### 2. Zaszyfruj wra¿liwe dane

```bash
# Zaszyfruj has³o API
dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt "tajne_haslo_api" --key "KLUCZ_Z_SECRETS"

# Wynik: "XYZ789ABC123ENCRYPTED...=="
```

### 3. Zaktualizuj konfiguracjê

```json
{
  "OlmedApi": {
    "BaseUrl": "https://api.example.com",
    "Username": "user",
    "Password": "XYZ789ABC123ENCRYPTED...=="
  }
}
```

### 4. U¿yj w kodzie

```csharp
string encryptedPassword = _configuration["OlmedApi:Password"];
string encryptionKey = _configuration["Encryption:Key"];
string actualPassword = StringEncryptionHelper.Decrypt(encryptedPassword, encryptionKey);
```

## Troubleshooting

### Problem: "Klucz szyfrowania nie mo¿e byæ pusty"
**Rozwi¹zanie:** Upewnij siê, ¿e klucz jest ustawiony w User Secrets lub zmiennych œrodowiskowych.

### Problem: "FormatException: Invalid Base64"
**Rozwi¹zanie:** SprawdŸ czy kopiujesz pe³ny zaszyfrowany tekst (z wszystkimi znakami `=` na koñcu).

### Problem: "CryptographicException"
**Rozwi¹zanie:** U¿ywasz z³ego klucza do odszyfrowania. Upewnij siê, ¿e u¿ywasz tego samego klucza co przy szyfrowaniu.

## Testowanie

```bash
# Test kompletnego workflow
$KEY = dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key
$ENCRYPTED = dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt "test123" --key $KEY
$DECRYPTED = dotnet run --project Prosepo.Webhooks -- encrypt-tool --decrypt $ENCRYPTED --key $KEY
# $DECRYPTED powinno byæ "test123"
```
