using ImportToPlanner.Application.TenantContext.Abstractions;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

/// <summary>
/// Adds hosted commercial backend client registrations used by the web host.
/// </summary>
public static class CommercialBackendServiceCollectionExtensions
{
    /// <summary>
    /// Adds the lightweight commercial API client and hosted-mode metadata adapter registrations.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommercialBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var commercialModeEnabled = configuration.GetValue<bool>("Features:CommercialMode:Enabled");
        if (!commercialModeEnabled)
        {
            services.AddHttpServiceReference<CommercialApiServiceClient>("https+http://commercialapiservice");
            return services;
        }

        services.AddHttpServiceReference<CommercialApiServiceClient>(
            "https+http://commercialapiservice",
            healthRelativePath: "health");

        services.AddSingleton<ITenantOperationalMetadataStore, BackendTenantOperationalMetadataStore>();

        return services;
    }
}
