using ImportToPlanner.CommercialService.CommercialAccounts.Services;
using ImportToPlanner.CommercialService.TenantMetadata.Services;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.CommercialService;

internal static class CommercialServiceComposition
{
    internal static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CommercialModeOptions>()
            .Bind(configuration.GetSection(CommercialModeOptions.ConfigurationSectionName))
            .ValidateOnStart();
        services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);
        services.AddSingleton<CommercialAccountsService>();
        services.AddSingleton<CommercialAuditService>();
        services.AddSingleton<TenantMetadataService>();
        services.AddSingleton<CommercialProfileService>();
        services.AddSingleton<CommercialAccessService>();
        services.AddHostedService<CommercialAccountRetentionHostedService>();
    }
}

