using ImportToPlanner.Web.Features.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Infrastructure;

internal static class WebApplicationOptionsExtensions
{
    public static IServiceCollection AddWebApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.ConfigurationSectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DataProtectionContainer),
                "Set 'Storage:DataProtectionContainer'.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DataProtectionBlob),
                "Set 'Storage:DataProtectionBlob'.")
            .ValidateOnStart();

        services.TryAddSingleton<StorageConfiguration>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value.ToConfiguration());

        var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(configuration);
        services.TryAddSingleton<IOptions<TenantAuthorityConfiguration>>(
            Options.Create(tenantAuthorityConfiguration));
        services.TryAddSingleton(_ => tenantAuthorityConfiguration);

        return services;
    }
}
