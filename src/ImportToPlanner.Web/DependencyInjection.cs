using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Workflows;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Kiota.Abstractions.Authentication;
using MudBlazor.Services;

namespace ImportToPlanner.Web;

/// <summary>
/// Extension methods for registering web-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds web UI, authentication, and Graph client services.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">Application configuration used for authentication and Graph scopes.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWebHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddMudServices();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();

        var graphScopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>() ?? ["User.Read"];

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi(graphScopes)
            .AddInMemoryTokenCaches();

        services.AddScoped<GraphServiceClient>(serviceProvider =>
        {
            var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var accessTokenProvider = new MicrosoftIdentityAccessTokenProvider(tokenAcquisition, httpContextAccessor, graphScopes);
            var authenticationProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
            return new GraphServiceClient(authenticationProvider);
        });

        services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Adds workflow and presenter services used by the Blazor import experience.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddImportWorkflow(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ImportPlanningPresenter>();
        services.AddScoped<ImportExecutionPresenter>();
        services.AddScoped<WorkflowCoordinationState>();
        services.AddScoped<ImportWorkflowCoordinator>();

        return services;
    }
}
