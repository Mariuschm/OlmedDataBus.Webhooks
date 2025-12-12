using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Services;

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
}