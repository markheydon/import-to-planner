using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Registers demonstration mode decorators and authentication hooks.
/// </summary>
public static class DemoModeServiceCollectionExtensions
{
    /// <summary>
    /// Adds demonstration mode routing and public documentation link configuration.
    /// </summary>
    public static IServiceCollection AddDemoMode(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DemoModeDeploymentPolicy>()
            .Bind(configuration.GetSection(DemoModeDeploymentPolicy.ConfigurationSectionName));

        services
            .AddOptions<DocsExternalLinksOptions>()
            .Bind(configuration.GetSection(DocsExternalLinksOptions.ConfigurationSectionName));

        services.TryAddScoped<OperatorIdentityContextAccessor>();
        services.TryAddScoped<IOperatorIdentityContextAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<OperatorIdentityContextAccessor>());

        services.AddScoped<IDemoModeAuthorisationService, DemoModeAuthorisationService>();

        services.RemoveAll<IDemoModeSession>();
        services.AddScoped<DemoModeSession>();
        services.AddScoped<IDemoModeSession>(serviceProvider =>
            new AuthorisedDemoModeSession(
                serviceProvider.GetRequiredService<DemoModeSession>(),
                serviceProvider.GetRequiredService<IDemoModeAuthorisationService>()));

        services.RemoveAll<IPlannerGateway>();
        services.AddScoped<IPlannerGateway>(serviceProvider =>
            new DemoAwarePlannerGateway(
                serviceProvider.GetRequiredService<ImportToPlanner.Infrastructure.Graph.Planner.GraphPlannerGateway>(),
                serviceProvider.GetRequiredService<IDemoModeSession>()));

        services.RemoveAll<ICsvImportParser>();
        services.AddScoped<ICsvImportParser>(serviceProvider =>
            new DemoAwareCsvImportParser(
                serviceProvider.GetRequiredService<ImportToPlanner.Infrastructure.Graph.Import.CsvImportParser>(),
                serviceProvider.GetRequiredService<IDemoModeSession>()));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, DemoModeOpenIdConnectPostConfigurer>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, DemoModeCookieSignOutPostConfigurer>());

        return services;
    }
}

internal sealed class OperatorIdentityContextAccessor(
    ISessionIdentityContextAccessor inner) : IOperatorIdentityContextAccessor
{
    public SessionIdentityContext? TryGetCurrent() => inner.TryGetCurrent();
}
