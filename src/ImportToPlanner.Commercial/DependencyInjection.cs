using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;
using ImportToPlanner.Commercial.Features.CommercialProfile.Services;
using ImportToPlanner.Commercial.Features.TenantMetadata;
using ImportToPlanner.Commercial.Features.TenantMetadata.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImportToPlanner.Commercial;

/// <summary>
/// Registers hosted commercial-mode services for in-process composition.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds commercial account, audit, metadata, and optional retention sweep services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="retentionSweepEnabled">Whether the retention sweep hosted service should run.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommercialServices(
        this IServiceCollection services,
        bool retentionSweepEnabled)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICommercialAccountsService, CommercialAccountsService>();
        services.AddSingleton<ICommercialAuditService, CommercialAuditService>();
        services.AddSingleton<ITenantMetadataService, TenantMetadataService>();
        services.AddSingleton<CommercialProfileService>();
        services.AddSingleton<CommercialAccessService>();

        services.RemoveAll<ITenantOperationalMetadataStore>();
        services.AddSingleton<ITenantOperationalMetadataStore, CommercialTenantOperationalMetadataStore>();

        if (retentionSweepEnabled)
        {
            services.AddHostedService<CommercialAccountRetentionHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Adds in-process commercial workflow services backed by no-op stores for self-host mode.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommercialServiceStubs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICommercialAccountsService, NoOpCommercialAccountsService>();
        services.AddSingleton<ICommercialAuditService, NoOpCommercialAuditService>();
        services.AddSingleton<ITenantMetadataService, NoOpTenantMetadataService>();
        services.AddSingleton<CommercialProfileService>();
        services.AddSingleton<CommercialAccessService>();

        return services;
    }
}
