using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Services;
using System.Security.Cryptography;
using System.Text;

namespace Prospeo.DbContext.Extensions;

/// <summary>
/// Rozszerzenia dla konfiguracji serwisów DbContext
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Dodaje ProspeoDataContext z konfiguracj¹ z appsettings
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="configuration">Konfiguracja aplikacji</param>
    /// <param name="connectionStringName">Nazwa connection stringa (domyœlnie "DefaultConnection")</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoDbContext(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        // Deszyfruj connection string jeœli zawiera zaszyfrowane has³o
        connectionString = DecryptConnectionString(connectionString);

        return services.AddProspeoDbContext(connectionString);
    }

    /// <summary>
    /// Dodaje ProspeoDataContext z okreœlonym connection stringiem
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProspeoDataContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        });

        return services;
    }

    /// <summary>
    /// Dodaje ProspeoDataContext z niestandardow¹ konfiguracj¹
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="optionsAction">Akcja konfiguracji DbContext</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<ProspeoDataContext>(optionsAction);
        return services;
    }

    /// <summary>
    /// Dodaje ProspeoDataContext z bezpoœrednim connection stringiem (u¿ywa konstruktora)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoDbContextDirect(this IServiceCollection services, string connectionString)
    {
        // Deszyfruj connection string jeœli zawiera zaszyfrowane has³o
        connectionString = DecryptConnectionString(connectionString);
        
        services.AddScoped<ProspeoDataContext>(provider => new ProspeoDataContext(connectionString));
        return services;
    }

    /// <summary>
    /// Dodaje wszystkie serwisy Prospeo (DbContext + serwisy biznesowe)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="configuration">Konfiguracja aplikacji</param>
    /// <param name="connectionStringName">Nazwa connection stringa</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoServices(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        // Dodaj DbContext
        services.AddProspeoDbContext(configuration, connectionStringName);

        // Dodaj serwisy biznesowe
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();

        return services;
    }

    /// <summary>
    /// Dodaje wszystkie serwisy Prospeo z okreœlonym connection stringiem
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoServices(this IServiceCollection services, string connectionString)
    {
        // Dodaj DbContext
        services.AddProspeoDbContext(connectionString);

        // Dodaj serwisy biznesowe
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();

        return services;
    }

    /// <summary>
    /// Dodaje wszystkie serwisy Prospeo z bezpoœrednim connection stringiem (u¿ywa konstruktora)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddProspeoServicesDirect(this IServiceCollection services, string connectionString)
    {
        // Dodaj DbContext z bezpoœrednim connection stringiem
        services.AddProspeoDbContextDirect(connectionString);

        // Dodaj serwisy biznesowe
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();

        return services;
    }

    #region Encryption Helper Methods

    private const string EncryptedPrefix = "ENC:";
    
    private static byte[] GetEncryptionKey()
    {
        var machineKey = Environment.MachineName + Environment.UserName;
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
    }
    
    private static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;
            
        if (!encryptedText.StartsWith(EncryptedPrefix))
            return encryptedText;
        
        try
        {
            var cipherText = encryptedText.Substring(EncryptedPrefix.Length);
            var buffer = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();
            
            var iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new CryptographicException(
                $"Nie mo¿na odszyfrowaæ wartoœci. Upewnij siê, ¿e zaszyfrowano j¹ na tej samej maszynie i koncie u¿ytkownika. B³¹d: {ex.Message}",
                ex);
        }
    }
    
    private static bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptedPrefix);
    }
    
    private static string DecryptConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;
        
        var passwordPattern = "Password=";
        var passwordIndex = connectionString.IndexOf(passwordPattern, StringComparison.OrdinalIgnoreCase);
        
        if (passwordIndex == -1)
            return connectionString;
        
        var startIndex = passwordIndex + passwordPattern.Length;
        var endIndex = connectionString.IndexOf(';', startIndex);
        
        if (endIndex == -1)
            endIndex = connectionString.Length;
        
        var passwordValue = connectionString.Substring(startIndex, endIndex - startIndex);
        
        if (!IsEncrypted(passwordValue))
            return connectionString;
        
        var decryptedPassword = Decrypt(passwordValue);
        
        return connectionString.Substring(0, startIndex) + 
               decryptedPassword + 
               connectionString.Substring(endIndex);
    }

    #endregion
}