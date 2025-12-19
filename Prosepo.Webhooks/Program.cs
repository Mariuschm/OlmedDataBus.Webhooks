using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Tools;
using Prospeo.DbContext.Extensions;
using Prospeo.DbContext.Data;
using Microsoft.EntityFrameworkCore;
using Prosepo.Webhooks.Helpers;
// Sprawdź czy uruchomiono narzędzie szyfrowania
if (args.Length > 0 && args[0] == "encrypt-tool")
{
    EncryptionTool.Run(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja logowania - zachowanie domyślnego logowania ASP.NET Core
builder.Logging.AddConsole();

// Dodanie logowania do pliku
var logsDirectory = builder.Configuration["Logging:File:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs");
Directory.CreateDirectory(logsDirectory);

// Dodaj ładowanie appsettings.Local.json jeśli istnieje (dla jawności)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddHttpClient();

// Rejestracja OlmedApiService
builder.Services.AddScoped<OlmedApiService>();

// Dodaj Prospeo DbContext i serwisy (z domyślnym connection stringiem)
// Sprawdź czy connection string istnieje w konfiguracji
var encryptionKey = Environment.GetEnvironmentVariable("PROSPEO_KEY") ?? "CPNFWqXE3TMY925xMgUPlUnWkjSyo9182PpYM69HM44=";
var connectionString = StringEncryptionHelper.DecryptIfEncrypted(builder.Configuration.GetConnectionString("DefaultConnection"), encryptionKey);
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddProspeoServices(builder.Configuration);
}
else
{
    // Fallback - jeśli nie ma connection stringa, dodaj tylko podstawowe serwisy bez DbContext
    // Pozwoli to aplikacji działać bez bazy danych ale ograniczy funkcjonalność Queue
    Console.WriteLine("Warning: No DefaultConnection found. Queue functionality will be disabled.");
}

// Rejestracja FileLoggingService
builder.Services.AddSingleton<FileLoggingService>();

// Rejestracja CronSchedulerService jako Hosted Service
builder.Services.AddSingleton<CronSchedulerService>();
builder.Services.AddHostedService<CronSchedulerService>(provider => provider.GetService<CronSchedulerService>()!);

// Rejestracja ProductSyncConfigurationService
builder.Services.AddScoped<ProductSyncConfigurationService>();

// Rejestracja OrderSyncConfigurationService
builder.Services.AddScoped<OrderSyncConfigurationService>();

// Rejestracja GracefulShutdownService jako Hosted Service
builder.Services.AddHostedService<GracefulShutdownService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Testowanie połączenia z bazą danych przy starcie aplikacji
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProspeoDataContext>();    
        // Sprawdź czy można połączyć się z bazą danych
        await context.Database.CanConnectAsync();
        // Opcjonalnie: sprawdź czy tabele istnieją (bez tworzenia ich)
        var firmsExist = await context.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ProRWS' AND TABLE_NAME = 'Firmy'") >= 0;

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database connection established successfully to {Database}", "PROSWB");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd połączenia z bazą danych: {ex.Message}");
        // Aplikacja może kontynuować działanie, ale funkcjonalność Queue będzie ograniczona
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to establish database connection to {Database}", "PROSWB");
    }
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
Console.WriteLine("🚀 Prosepo Webhooks is running...");

app.Run();
