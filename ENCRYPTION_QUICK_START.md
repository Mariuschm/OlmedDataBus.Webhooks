# ?? Quick Start - Szyfrowanie danych konfiguracyjnych

## Scenariusz: Zaszyfrowanie has³a do API Olmed

### Krok 1: Wygeneruj klucz szyfrowania

```powershell
# U¿yj PowerShell helper
$key = .\encrypt-config.ps1 -GenerateKey

# LUB bezpoœrednio przez dotnet CLI
dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key
```

**Przyk³adowy wynik:**
```
?? Wygenerowany klucz szyfrowania:
vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW==

??  WA¯NE: Zapisz ten klucz w bezpiecznym miejscu!
```

### Krok 2: Zapisz klucz w User Secrets (Development)

```powershell
dotnet user-secrets set "Encryption:Key" "vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW==" --project Prosepo.Webhooks
```

### Krok 3: Zaszyfruj has³o API

```powershell
# Zaszyfruj has³o do API Olmed
$encryptedPassword = .\encrypt-config.ps1 -Encrypt "moje_super_tajne_haslo_api" -Key "vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW=="
```

**Przyk³adowy wynik:**
```
?? Zaszyfrowany tekst:
qR9tL3xY8mN4pW2jF7bV5cH1kD0sA6gE+zX/yW==
```

### Krok 4: Zaktualizuj appsettings.json

**appsettings.json:**
```json
{
  "OlmedApi": {
    "BaseUrl": "https://api.olmed.pl",
    "Username": "olmed_user",
    "Password": "qR9tL3xY8mN4pW2jF7bV5cH1kD0sA6gE+zX/yW=="
  }
}
```

?? **Bezpieczeñstwo:** Zaszyfrowane has³o mo¿na teraz bezpiecznie commitowaæ do repozytorium!

### Krok 5: U¿yj w kodzie

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
        string encryptedPassword = _configuration["OlmedApi:Password"];
        string encryptionKey = _configuration["Encryption:Key"];
        
        // Automatycznie odszyfruje jeœli zaszyfrowane
        return StringEncryptionHelper.DecryptIfEncrypted(encryptedPassword, encryptionKey);
    }
    
    public async Task<string> AuthenticateAsync()
    {
        string username = _configuration["OlmedApi:Username"];
        string password = GetDecryptedPassword();
        
        // U¿yj odszyfrowanego has³a do autentykacji
        // ...
    }
}
```

## Scenariusz: Production Deployment

### Krok 1: Zapisz klucz w zmiennych œrodowiskowych (Windows Server)

```powershell
# Uruchom PowerShell jako Administrator
[Environment]::SetEnvironmentVariable("Encryption__Key", "vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW==", "Machine")

# Zrestartuj aplikacjê/IIS aby za³adowaæ now¹ zmienn¹
iisreset
```

### Krok 2: Zweryfikuj konfiguracjê

```powershell
# SprawdŸ czy zmienna jest ustawiona
[Environment]::GetEnvironmentVariable("Encryption__Key", "Machine")
```

## Scenariusz: Zaszyfrowanie Connection String

### Krok 1: Zaszyfruj wra¿liwe czêœci connection stringa

```powershell
# Zaszyfruj has³o do bazy danych
$encryptedDbPassword = .\encrypt-config.ps1 -Encrypt "zaq12wsX" -Key "vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW=="
```

### Krok 2: Zaktualizuj appsettings.json

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Server": "192.168.88.210",
    "Database": "PROSWB",
    "UserId": "sa",
    "Password": "dH8mK3xY9pL4jW2nF7bV5cR1sA0gE+zX/yW==",
    "TrustServerCertificate": "true"
  }
}
```

### Krok 3: Zbuduj connection string w kodzie

```csharp
private string GetConnectionString()
{
    string server = _configuration["ConnectionStrings:Server"];
    string database = _configuration["ConnectionStrings:Database"];
    string userId = _configuration["ConnectionStrings:UserId"];
    string encryptedPassword = _configuration["ConnectionStrings:Password"];
    string encryptionKey = _configuration["Encryption:Key"];
    
    string password = StringEncryptionHelper.Decrypt(encryptedPassword, encryptionKey);
    
    return $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=true;";
}
```

## Testowanie

### Test 1: Kompletny workflow

```powershell
# 1. Wygeneruj klucz
$key = .\encrypt-config.ps1 -GenerateKey

# 2. Zaszyfruj tekst testowy
$encrypted = .\encrypt-config.ps1 -Encrypt "test123" -Key $key

# 3. Odszyfruj z powrotem
$decrypted = .\encrypt-config.ps1 -Decrypt $encrypted -Key $key

# 4. SprawdŸ wynik
if ($decrypted -eq "test123") {
    Write-Host "? Test przeszed³ pomyœlnie!" -ForegroundColor Green
} else {
    Write-Host "? Test nie powiód³ siê!" -ForegroundColor Red
}
```

### Test 2: Integracja z konfiguracj¹

```powershell
# Zaszyfruj przyk³adowe dane
$key = "vK5YB3dP7wQ9xL2mN8hJ6fT4gR1sA0cE+zX/yW=="
$apiPassword = .\encrypt-config.ps1 -Encrypt "api_password_123" -Key $key
$dbPassword = .\encrypt-config.ps1 -Encrypt "db_password_456" -Key $key

Write-Host "Zaszyfrowane has³o API: $apiPassword"
Write-Host "Zaszyfrowane has³o DB: $dbPassword"
```

## Migracja istniej¹cych danych

### Scenariusz: Masz ju¿ has³o w appsettings.json

**Przed (niezabezpieczone):**
```json
{
  "OlmedApi": {
    "Password": "moje_jawne_haslo"
  }
}
```

**Kroki:**
1. Zaszyfruj has³o:
```powershell
$encrypted = .\encrypt-config.ps1 -Encrypt "moje_jawne_haslo" -Key $key
```

2. Zaktualizuj konfiguracjê:
```json
{
  "OlmedApi": {
    "Password": "qR9tL3xY8mN4pW2jF7bV5cH1kD0sA6gE+zX/yW=="
  }
}
```

3. Zaktualizuj kod aby u¿ywa³ `DecryptIfEncrypted`:
```csharp
string password = StringEncryptionHelper.DecryptIfEncrypted(
    _configuration["OlmedApi:Password"],
    _configuration["Encryption:Key"]
);
```

**Po (zabezpieczone):** ? Has³o jest zaszyfrowane!

## Troubleshooting

### Problem: Aplikacja nie mo¿e odszyfrowaæ has³a

**SprawdŸ:**
1. Czy klucz jest ustawiony w User Secrets lub zmiennych œrodowiskowych?
```powershell
dotnet user-secrets list --project Prosepo.Webhooks
```

2. Czy u¿ywasz tego samego klucza co przy szyfrowaniu?

3. Czy skopiowa³eœ pe³ny zaszyfrowany tekst (z `==` na koñcu)?

### Problem: "FormatException: Invalid Base64"

**Rozwi¹zanie:** Upewnij siê, ¿e kopiujesz pe³ny tekst z wszystkimi znakami specjalnymi:
```
qR9tL3xY8mN4pW2jF7bV5cH1kD0sA6gE+zX/yW==
                                    ^^^^^^^^ Wa¿ne!
```

## Rotacja kluczy (zaawansowane)

### Kiedy rotowaæ klucze?
- Co 6-12 miesiêcy (zalecane)
- Po opuszczeniu firmy przez cz³onka zespo³u z dostêpem
- Po podejrzeniu wycieku klucza

### Jak rotowaæ klucze?

1. **Wygeneruj nowy klucz:**
```powershell
$newKey = .\encrypt-config.ps1 -GenerateKey
```

2. **Odszyfruj wszystkie dane starym kluczem:**
```powershell
$oldKey = "stary_klucz..."
$password1 = .\encrypt-config.ps1 -Decrypt "stare_zaszyfrowane_haslo1" -Key $oldKey
$password2 = .\encrypt-config.ps1 -Decrypt "stare_zaszyfrowane_haslo2" -Key $oldKey
```

3. **Zaszyfruj ponownie nowym kluczem:**
```powershell
$newEncrypted1 = .\encrypt-config.ps1 -Encrypt $password1 -Key $newKey
$newEncrypted2 = .\encrypt-config.ps1 -Encrypt $password2 -Key $newKey
```

4. **Zaktualizuj konfiguracjê i zmienne œrodowiskowe z nowym kluczem**

## Najlepsze praktyki

? **DO:**
- U¿ywaj ró¿nych kluczy dla ka¿dego œrodowiska (dev, staging, prod)
- Przechowuj klucze w User Secrets (dev) lub zmiennych œrodowiskowych (prod)
- Regularnie rotuj klucze
- Loguj b³êdy deszyfrowania (ale NIE odszyfrowane wartoœci)

? **NIE:**
- Nie commituj kluczy do repozytorium
- Nie loguj odszyfrowanych wartoœci
- Nie wysy³aj kluczy przez email/chat
- Nie u¿ywaj tego samego klucza wszêdzie
