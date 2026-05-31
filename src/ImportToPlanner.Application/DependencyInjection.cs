using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Services;
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

        services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();

        return services;
    }
}
