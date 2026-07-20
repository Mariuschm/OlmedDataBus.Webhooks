using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prospeo.DbContext.Data;
using Prospeo.DbContext.Interfaces;
using Prospeo.DbContext.Security;
using Prospeo.DbContext.Services;

namespace Prospeo.DbContext.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProspeoDbContext(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        connectionString = ConnectionStringProtector.DecryptConnectionString(connectionString);
        return services.AddProspeoDbContext(connectionString);
    }

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

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    public static IServiceCollection AddProspeoDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<ProspeoDataContext>(optionsAction);
        return services;
    }

    public static IServiceCollection AddProspeoDbContextDirect(this IServiceCollection services, string connectionString)
    {
        connectionString = ConnectionStringProtector.DecryptConnectionString(connectionString);
        services.AddScoped<ProspeoDataContext>(_ => new ProspeoDataContext(connectionString));
        return services;
    }

    public static IServiceCollection AddProspeoServices(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        services.AddProspeoDbContext(configuration, connectionStringName);
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IQueueRelationsService, QueueRelationsService>();
        return services;
    }

    public static IServiceCollection AddProspeoServices(this IServiceCollection services, string connectionString)
    {
        services.AddProspeoDbContext(connectionString);
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IQueueRelationsService, QueueRelationsService>();
        return services;
    }

    public static IServiceCollection AddProspeoServicesDirect(this IServiceCollection services, string connectionString)
    {
        services.AddProspeoDbContextDirect(connectionString);
        services.AddScoped<IFirmyService, FirmyService>();
        services.AddScoped<IQueueStatusService, QueueStatusService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IQueueRelationsService, QueueRelationsService>();
        return services;
    }
}
