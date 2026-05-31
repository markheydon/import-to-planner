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
    /// <param name="includeCommercialUseCases">
    /// Indicates whether commercial account use cases should be registered.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, bool includeCommercialUseCases = true)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();

        if (includeCommercialUseCases)
        {
            services.AddCommercialApplicationUseCases();
        }

        return services;
    }

    /// <summary>
    /// Adds commercial account use cases to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommercialApplicationUseCases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICommercialAccessUseCase, CommercialAccessUseCase>();
        services.AddScoped<GetCommercialProfileUseCase>();
        services.AddScoped<DeleteCommercialAccountUseCase>();
        services.AddScoped<RestoreCommercialAccountUseCase>();
        services.AddScoped<PurgeExpiredCommercialAccountsUseCase>();
        services.AddScoped<ICommercialProfileUseCase, GetCommercialProfileUseCase>();

        return services;
    }
}
