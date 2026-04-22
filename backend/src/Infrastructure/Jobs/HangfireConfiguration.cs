using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Infrastructure.Jobs;

public static class HangfireConfiguration
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Hangfire:Enabled", true))
        {
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=tibia_webstore;Username=postgres;Password=postgres";

        Log.Information("Configuring Hangfire with PostgreSQL storage");

        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
        {
            Attempts = 5,
            DelaysInSeconds = [60, 300, 900, 3600, 86400]
        });

        var storageOptions = new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            AllowDegradedModeWithoutStorage = true,
            QueuePollInterval = TimeSpan.FromSeconds(15)
        };

        services.AddHangfire((sp, configure) =>
        {
            configure.UseSerilogLogProvider();
            configure.UsePostgreSqlStorage(connectionString, storageOptions);
        });

        var workerCount = configuration.GetValue<int>("Hangfire:WorkerCount", Environment.ProcessorCount);
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount;
        });

        Log.Information("Hangfire configured with {WorkerCount} workers and exponential retry (5 attempts)", workerCount);

        return services;
    }
}
