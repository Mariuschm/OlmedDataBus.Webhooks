using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Prospeo.Webservice.Queue.Data;
using Prospeo.Webservice.Queue.Services;

namespace Prospeo.Webservice.Queue.Extensions;

/// <summary>
/// Rozszerzenia dla konfiguracji us³ug kolejki
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Dodaje podstawowe us³ugi kolejki (bez procesora)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="configuration">Konfiguracja aplikacji</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        return AddQueueServices(services, connectionString);
    }

    /// <summary>
    /// Dodaje podstawowe us³ugi kolejki z okreœlonym connection stringiem (bez procesora)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueServices(this IServiceCollection services, string connectionString)
    {
        // Dodaj DbContext
        services.AddDbContext<QueueDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // Dodaj podstawowe us³ugi kolejki
        services.AddScoped<IQueueService, QueueService>();

        return services;
    }

    /// <summary>
    /// Dodaje us³ugi kolejki z niestandardowym procesorem
    /// </summary>
    /// <typeparam name="TProcessor">Typ procesora implementuj¹cego IQueueProcessor</typeparam>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="configuration">Konfiguracja aplikacji</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueServices<TProcessor>(this IServiceCollection services, IConfiguration configuration)
        where TProcessor : class, IQueueProcessor
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        return AddQueueServices<TProcessor>(services, connectionString);
    }

    /// <summary>
    /// Dodaje us³ugi kolejki z niestandardowym procesorem i connection stringiem
    /// </summary>
    /// <typeparam name="TProcessor">Typ procesora implementuj¹cego IQueueProcessor</typeparam>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueServices<TProcessor>(this IServiceCollection services, string connectionString)
        where TProcessor : class, IQueueProcessor
    {
        // Dodaj DbContext
        services.AddDbContext<QueueDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // Dodaj us³ugi z niestandardowym procesorem
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IQueueProcessor, TProcessor>();

        return services;
    }

    /// <summary>
    /// Dodaje tylko DbContext dla kolejki (do u¿ycia gdy chcesz samodzielnie zarz¹dzaæ serwisami)
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="connectionString">Connection string do bazy danych</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<QueueDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Dodaje DbContext z opcjami konfiguracji
    /// </summary>
    /// <param name="services">Kolekcja serwisów</param>
    /// <param name="optionsAction">Akcja konfiguracji DbContext</param>
    /// <returns>Kolekcja serwisów</returns>
    public static IServiceCollection AddQueueDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<QueueDbContext>(optionsAction);
        return services;
    }
}