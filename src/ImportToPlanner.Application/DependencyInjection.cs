using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Application;

/// <summary>
/// Extension methods for registering application-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application use cases to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">Application configuration used to bind release label policy.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ReleaseLabelPolicy>()
            .Bind(configuration.GetSection(ReleaseLabelPolicy.ConfigurationSectionName));
        services.AddSingleton<ReleaseLabelFormatter>();

        services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();
        services.AddScoped<IImportTaskCreationQuota, NoOpImportTaskCreationQuota>();
        services.AddScoped<ICsvColumnMappingService, CsvColumnMappingService>();
        services.AddScoped<ExecutionReportCsvExporter>();
        services.AddScoped<DemoModeSession>();
        services.AddScoped<IDemoModeSession, DemoModeSession>();
        return services;
    }

    /// <summary>
    /// Adds application use cases to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => AddApplication(services, new ConfigurationBuilder().Build());
}
