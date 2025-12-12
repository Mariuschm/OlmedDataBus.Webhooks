using Prospeo.DbContext.Data;
using Prospeo.DbContext.Extensions;
using Prospeo.DbContext.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Przyk³ady ró¿nych sposobów konfiguracji ProspeoDataContext
/// </summary>
public static class DbContextConfigurationExamples
{
    /// <summary>
    /// Przyk³ad u¿ycia konstruktora z connection stringiem
    /// </summary>
    public static async Task DirectConnectionStringExample()
    {
        var connectionString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        
        // Sposób 1: Bezpoœrednie u¿ycie konstruktora
        using var context = new ProspeoDataContext(connectionString);
        
        // SprawdŸ po³¹czenie
        var canConnect = await context.Database.CanConnectAsync();
        Console.WriteLine($"Po³¹czenie z baz¹ danych: {(canConnect ? "OK" : "B£¥D")}");
        
        // Przyk³ad zapytania
        var firmy = await context.Firmy.Take(5).ToListAsync();
        Console.WriteLine($"Znaleziono {firmy.Count} firm");
    }

    /// <summary>
    /// Przyk³ad konfiguracji w Dependency Injection
    /// </summary>
    public static void DependencyInjectionExamples(IServiceCollection services, IConfiguration configuration)
    {
        // Sposób 1: Z appsettings.json (standardowy)
        services.AddProspeoServices(configuration);
        
        // Sposób 2: Z bezpoœrednim connection stringiem (standardowy)
        services.AddProspeoServices("Server=localhost;Database=ProspeoDb;Trusted_Connection=true;");
        
        // Sposób 3: Z bezpoœrednim connection stringiem (u¿ywa konstruktora)
        services.AddProspeoServicesDirect("Server=localhost;Database=ProspeoDb;Trusted_Connection=true;");
        
        // Sposób 4: Tylko DbContext z konstruktorem
        services.AddProspeoDbContextDirect("Server=localhost;Database=ProspeoDb;Trusted_Connection=true;");
        
        // Sposób 5: Zaawansowana konfiguracja
        services.AddProspeoDbContext(options =>
        {
            options.UseSqlServer("connection-string", sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(5);
            });
            options.EnableSensitiveDataLogging(); // Tylko dla developmentu
        });
    }

    /// <summary>
    /// Przyk³ad u¿ycia w aplikacji konsolowej
    /// </summary>
    public static async Task ConsoleApplicationExample()
    {
        Console.WriteLine("=== Przyk³ad aplikacji konsolowej ===");
        
        var connectionString = "Server=localhost;Database=ProspeoDb;Integrated Security=true;TrustServerCertificate=true;";
        
        try
        {
            using var context = new ProspeoDataContext(connectionString);
            
            // SprawdŸ czy baza istnieje
            var exists = await context.Database.CanConnectAsync();
            Console.WriteLine($"Baza danych dostêpna: {exists}");
            
            if (!exists)
            {
                Console.WriteLine("Tworzenie bazy danych...");
                await context.Database.EnsureCreatedAsync();
            }
            
            // Przyk³ad dodawania danych
            var nowaFirma = new Models.Firmy
            {
                NazwaFirmy = "Test Firma Console",
                NazwaBazyERP = "TestConsoleDB",
                CzyTestowa = true
            };
            
            context.Firmy.Add(nowaFirma);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Dodano firmê z ID: {nowaFirma.Id}");
            
            // Pobierz wszystkie firmy
            var firmy = await context.Firmy.ToListAsync();
            Console.WriteLine($"£¹czna liczba firm: {firmy.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"B³¹d: {ex.Message}");
        }
    }

    /// <summary>
    /// Przyk³ad migracji bazy danych
    /// </summary>
    public static async Task MigrationExample()
    {
        Console.WriteLine("=== Przyk³ad migracji ===");
        
        var connectionString = "Server=localhost;Database=ProspeoDb;Integrated Security=true;TrustServerCertificate=true;";
        
        using var context = new ProspeoDataContext(connectionString);
        
        try
        {
            // SprawdŸ pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            Console.WriteLine($"Oczekuj¹ce migracje: {pendingMigrations.Count()}");
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine("Wykonywanie migracji...");
                await context.Database.MigrateAsync();
                Console.WriteLine("Migracje zakoñczone pomyœlnie");
            }
            
            // SprawdŸ applied migrations
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            Console.WriteLine($"Zastosowane migracje: {appliedMigrations.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"B³¹d migracji: {ex.Message}");
        }
    }

    /// <summary>
    /// Przyk³ad u¿ycia w ASP.NET Core
    /// </summary>
    public static void AspNetCoreConfigurationExample()
    {
        Console.WriteLine("=== Przyk³ad konfiguracji ASP.NET Core ===");
        Console.WriteLine("Ten kod powinien byæ u¿yty w Program.cs aplikacji ASP.NET Core:");
        Console.WriteLine();
        Console.WriteLine("var builder = WebApplication.CreateBuilder();");
        Console.WriteLine();
        Console.WriteLine("// Sposób 1: Z appsettings");
        Console.WriteLine("builder.Services.AddProspeoServices(builder.Configuration);");
        Console.WriteLine();
        Console.WriteLine("// Sposób 2: Z environment variable");
        Console.WriteLine("var connectionString = Environment.GetEnvironmentVariable(\"PROSPEO_CONNECTION_STRING\");");
        Console.WriteLine("if (!string.IsNullOrEmpty(connectionString))");
        Console.WriteLine("    builder.Services.AddProspeoServicesDirect(connectionString);");
        Console.WriteLine();
        Console.WriteLine("var app = builder.Build();");
    }

    /// <summary>
    /// Przyk³ad ró¿nych sposobów konfiguracji serwisów
    /// </summary>
    public static void ServiceConfigurationExamples()
    {
        var services = new ServiceCollection();
        
        // Tworzymy pust¹ konfiguracjê dla przyk³adu
        var configurationData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
        
        Console.WriteLine("=== Ró¿ne sposoby konfiguracji serwisów ===");
        
        // 1. Standardowa konfiguracja
        try
        {
            services.AddProspeoServices(configuration);
            Console.WriteLine("? Standardowa konfiguracja z IConfiguration");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? B³¹d standardowej konfiguracji: {ex.Message}");
        }
        
        // 2. Z bezpoœrednim connection stringiem
        var connString = "Server=localhost;Database=ProspeoDb;Trusted_Connection=true;";
        services.AddProspeoServices(connString);
        Console.WriteLine("? Z bezpoœrednim connection stringiem");
        
        // 3. Z konstruktorem (direct)
        services.AddProspeoServicesDirect(connString);
        Console.WriteLine("? Z konstruktorem bezpoœrednim");
        
        // 4. Tylko DbContext
        services.AddProspeoDbContext(connString);
        Console.WriteLine("? Tylko DbContext");
        
        // 5. DbContext z konstruktorem
        services.AddProspeoDbContextDirect(connString);
        Console.WriteLine("? DbContext z konstruktorem");
        
        Console.WriteLine($"Zarejestrowano {services.Count} serwisów");
    }

    /// <summary>
    /// Przyk³ad konfiguracji dla ró¿nych œrodowisk
    /// </summary>
    public static void EnvironmentSpecificConfiguration(IServiceCollection services, IConfiguration configuration, string environment)
    {
        var connectionString = environment switch
        {
            "Development" => "Server=localhost;Database=ProspeoDb_Dev;Trusted_Connection=true;",
            "Testing" => "Server=testserver;Database=ProspeoDb_Test;Trusted_Connection=true;",
            "Staging" => configuration.GetConnectionString("StagingConnection"),
            "Production" => configuration.GetConnectionString("ProductionConnection"),
            _ => configuration.GetConnectionString("DefaultConnection")
        };
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddProspeoServicesDirect(connectionString);
            Console.WriteLine($"Skonfigurowano dla œrodowiska: {environment}");
            Console.WriteLine($"Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
        }
        else
        {
            Console.WriteLine($"Brak connection stringa dla œrodowiska: {environment}");
        }
    }

    /// <summary>
    /// Przyk³ad walidacji connection stringa
    /// </summary>
    public static async Task<bool> ValidateConnectionString(string connectionString)
    {
        try
        {
            using var context = new ProspeoDataContext(connectionString);
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                Console.WriteLine("? Connection string jest poprawny");
                
                // SprawdŸ czy tabele istniej¹
                var tablesExist = await context.Firmy.AnyAsync();
                Console.WriteLine($"? Tabele s¹ dostêpne: {tablesExist}");
                
                return true;
            }
            else
            {
                Console.WriteLine("? Nie mo¿na po³¹czyæ siê z baz¹ danych");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? B³¹d connection stringa: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Przyk³ady ró¿nych connection stringów
/// </summary>
public static class ConnectionStringExamples
{
    // SQL Server z Windows Authentication
    public const string SqlServerWindows = "Server=localhost;Database=ProspeoDb;Integrated Security=true;TrustServerCertificate=true;";
    
    // SQL Server z SQL Authentication
    public const string SqlServerSql = "Server=localhost;Database=ProspeoDb;User Id=prospeo_user;Password=SecurePassword123;TrustServerCertificate=true;";
    
    // SQL Server z connection pooling
    public const string SqlServerPooled = "Server=localhost;Database=ProspeoDb;Integrated Security=true;Pooling=true;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;";
    
    // SQL Server Express LocalDB
    public const string LocalDb = @"Server=(localdb)\mssqllocaldb;Database=ProspeoDb;Trusted_Connection=true;";
    
    // SQL Server w Docker
    public const string SqlServerDocker = "Server=localhost,1433;Database=ProspeoDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;";
    
    // Azure SQL Database
    public const string AzureSql = "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ProspeoDb;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    
    /// <summary>
    /// Generuje connection string na podstawie parametrów
    /// </summary>
    public static string BuildConnectionString(string server, string database, string? userId = null, string? password = null, bool integratedSecurity = true)
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder();
        builder["Server"] = server;
        builder["Database"] = database;
        builder["TrustServerCertificate"] = "true";
        
        if (integratedSecurity && string.IsNullOrEmpty(userId))
        {
            builder["Integrated Security"] = "true";
        }
        else
        {
            builder["User Id"] = userId;
            builder["Password"] = password;
        }
        
        return builder.ConnectionString;
    }
}