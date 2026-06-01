using ImportToPlanner.Application.CsvImport.Abstractions;
using ImportToPlanner.Application.CsvImport.Services;
using ImportToPlanner.Application.ImportExecution.Abstractions;
using ImportToPlanner.Application.ImportExecution.Services;
using ImportToPlanner.Application.ImportPlanning.Abstractions;
using ImportToPlanner.Application.ImportPlanning.Services;
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
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICsvImportParser, CsvImportParser>();
        services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();

        return services;
    }
}
