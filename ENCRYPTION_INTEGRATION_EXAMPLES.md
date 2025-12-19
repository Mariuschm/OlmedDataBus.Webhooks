# ?? Przyk³ad integracji szyfrowania z OlmedApiService

## Przed integracj¹ (niezabezpieczone)

**appsettings.json:**
```json
{
  "OlmedApi": {
    "BaseUrl": "https://api.olmed.pl",
    "Username": "olmed_user",
    "Password": "moje_jawne_haslo"
  }
}
```

**OlmedApiService.cs:**
```csharp
public class OlmedApiService
{
    private readonly IConfiguration _configuration;
    
    public OlmedApiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    private string GetPassword()
    {
        // ? Has³o w jawnej formie!
        return _configuration["OlmedApi:Password"];
    }
}
```

## Po integracji (zabezpieczone)

### Krok 1: Zaszyfruj has³o

```powershell
# Wygeneruj klucz
$key = .\encrypt-config.ps1 -GenerateKey

# Zapisz klucz w User Secrets
dotnet user-secrets set "Encryption:Key" $key --project Prosepo.Webhooks

# Zaszyfruj has³o
$encryptedPassword = .\encrypt-config.ps1 -Encrypt "moje_jawne_haslo" -Key $key
```

### Krok 2: Zaktualizuj konfiguracjê

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

### Krok 3: Zaktualizuj OlmedApiService

**OlmedApiService.cs:**
```csharp
using Prosepo.Webhooks.Helpers;

public class OlmedApiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OlmedApiService> _logger;
    
    public OlmedApiService(
        IConfiguration configuration,
        ILogger<OlmedApiService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    private string GetDecryptedPassword()
    {
        try
        {
            string encryptedPassword = _configuration["OlmedApi:Password"];
            string encryptionKey = _configuration["Encryption:Key"];
            
            if (string.IsNullOrEmpty(encryptionKey))
            {
                _logger.LogWarning("Encryption key not found. Using password as-is (not recommended).");
                return encryptedPassword;
            }
            
            // ? Automatycznie odszyfruje jeœli zaszyfrowane
            return StringEncryptionHelper.DecryptIfEncrypted(encryptedPassword, encryptionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt API password");
            throw;
        }
    }
    
    public async Task<string> AuthenticateAsync()
    {
        string username = _configuration["OlmedApi:Username"];
        string password = GetDecryptedPassword();
        
        // ? U¿yj odszyfrowanego has³a
        // ...
    }
}
```

## Dodatkowe przyk³ady

### Connection String z zaszyfrowanym has³em

**appsettings.json:**
```json
{
  "DatabaseConnection": {
    "Server": "192.168.88.210",
    "Database": "PROSWB",
    "UserId": "sa",
    "PasswordEncrypted": "dH8mK3xY9pL4jW2nF7bV5cR1sA0gE+zX/yW=="
  }
}
```

**DatabaseService.cs:**
```csharp
using Prosepo.Webhooks.Helpers;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    
    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    private string GetConnectionString()
    {
        var config = _configuration.GetSection("DatabaseConnection");
        
        string server = config["Server"];
        string database = config["Database"];
        string userId = config["UserId"];
        string encryptedPassword = config["PasswordEncrypted"];
        string encryptionKey = _configuration["Encryption:Key"];
        
        string password = StringEncryptionHelper.Decrypt(encryptedPassword, encryptionKey);
        
        return $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=true;";
    }
}
```

### Multiple API Keys

**appsettings.json:**
```json
{
  "ExternalApis": {
    "Olmed": {
      "ApiKeyEncrypted": "xxx..."
    },
    "Allegro": {
      "ClientIdEncrypted": "yyy...",
      "ClientSecretEncrypted": "zzz..."
    }
  }
}
```

**ExternalApiService.cs:**
```csharp
using Prosepo.Webhooks.Helpers;

public class ExternalApiService
{
    private readonly IConfiguration _configuration;
    
    public ExternalApiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    private string DecryptApiKey(string configPath)
    {
        string encrypted = _configuration[configPath];
        string encryptionKey = _configuration["Encryption:Key"];
        
        return StringEncryptionHelper.Decrypt(encrypted, encryptionKey);
    }
    
    public string GetOlmedApiKey()
    {
        return DecryptApiKey("ExternalApis:Olmed:ApiKeyEncrypted");
    }
    
    public (string clientId, string clientSecret) GetAllegroCredentials()
    {
        return (
            DecryptApiKey("ExternalApis:Allegro:ClientIdEncrypted"),
            DecryptApiKey("ExternalApis:Allegro:ClientSecretEncrypted")
        );
    }
}
```

### Lazy Decryption (dla wydajnoœci)

```csharp
public class OlmedApiService
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<string> _password;
    
    public OlmedApiService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // ? Has³o bêdzie odszyfrowane tylko przy pierwszym u¿yciu
        _password = new Lazy<string>(() =>
        {
            string encrypted = _configuration["OlmedApi:Password"];
            string key = _configuration["Encryption:Key"];
            return StringEncryptionHelper.Decrypt(encrypted, key);
        });
    }
    
    public async Task<string> AuthenticateAsync()
    {
        string username = _configuration["OlmedApi:Username"];
        string password = _password.Value; // Odszyfrowane tylko raz
        
        // ...
    }
}
```

## Testing

### Unit Test z mockowaniem

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Prosepo.Webhooks.Helpers;

public class OlmedApiServiceTests
{
    [Fact]
    public void GetPassword_WithEncryptedPassword_ReturnsDecryptedValue()
    {
        // Arrange
        string key = StringEncryptionHelper.GenerateKey();
        string plainPassword = "test_password_123";
        string encryptedPassword = StringEncryptionHelper.Encrypt(plainPassword, key);
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["OlmedApi:Password"] = encryptedPassword,
                ["Encryption:Key"] = key
            })
            .Build();
        
        var service = new OlmedApiService(configuration, Mock.Of<ILogger<OlmedApiService>>());
        
        // Act
        var result = service.GetDecryptedPassword(); // Musisz uczyniæ tê metodê publiczn¹/internal dla testów
        
        // Assert
        Assert.Equal(plainPassword, result);
    }
}
```

### Integration Test

```csharp
[Fact]
public async Task Authenticate_WithEncryptedPassword_Succeeds()
{
    // Arrange
    var key = StringEncryptionHelper.GenerateKey();
    var encryptedPassword = StringEncryptionHelper.Encrypt("real_password", key);
    
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["OlmedApi:BaseUrl"] = "https://test-api.olmed.pl",
            ["OlmedApi:Username"] = "test_user",
            ["OlmedApi:Password"] = encryptedPassword,
            ["Encryption:Key"] = key
        })
        .Build();
    
    var service = new OlmedApiService(configuration, logger);
    
    // Act
    var token = await service.AuthenticateAsync();
    
    // Assert
    Assert.NotNull(token);
}
```

## Migracja stopniowa (Backward Compatibility)

Jeœli chcesz stopniowo migrowaæ z jawnych hase³ na zaszyfrowane:

```csharp
private string GetPassword()
{
    string password = _configuration["OlmedApi:Password"];
    string encryptionKey = _configuration["Encryption:Key"];
    
    // ? Dzia³a zarówno z jawnymi jak i zaszyfrowanymi has³ami
    if (!string.IsNullOrEmpty(encryptionKey) && 
        StringEncryptionHelper.IsEncrypted(password))
    {
        _logger.LogDebug("Decrypting password");
        return StringEncryptionHelper.Decrypt(password, encryptionKey);
    }
    
    _logger.LogWarning("Using plain-text password (migration needed)");
    return password;
}
```

Teraz mo¿esz stopniowo migrowaæ:
1. Dodaj klucz szyfrowania
2. Aplikacja dzia³a z jawnymi has³ami
3. Zaszyfruj has³a jedno po drugim
4. Aplikacja automatycznie wykrywa i u¿ywa zaszyfrowanych hase³
5. Po pe³nej migracji, usuñ fallback dla jawnych hase³

## Monitoring i Alerty

```csharp
public class OlmedApiService
{
    private string GetDecryptedPassword()
    {
        try
        {
            string encrypted = _configuration["OlmedApi:Password"];
            string key = _configuration["Encryption:Key"];
            
            if (string.IsNullOrEmpty(key))
            {
                // ?? Alert: brak klucza szyfrowania
                _logger.LogError("SECURITY: Encryption key not configured!");
                throw new InvalidOperationException("Encryption key is required");
            }
            
            if (!StringEncryptionHelper.IsEncrypted(encrypted))
            {
                // ?? Alert: has³o niezaszyfrowane
                _logger.LogWarning("SECURITY: Password is not encrypted!");
            }
            
            return StringEncryptionHelper.Decrypt(encrypted, key);
        }
        catch (CryptographicException ex)
        {
            // ?? Alert: nieprawid³owy klucz lub uszkodzone dane
            _logger.LogError(ex, "SECURITY: Failed to decrypt password - invalid key or corrupted data");
            throw;
        }
    }
}
```
