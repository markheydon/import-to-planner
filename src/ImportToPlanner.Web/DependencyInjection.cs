using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Workflows;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Kiota.Abstractions.Authentication;
using MudBlazor.Services;

namespace ImportToPlanner.Web;

/// <summary>
/// Extension methods for registering web-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    private const string AuthenticationErrorQueryKey = "authError";
    private const string AuthenticationReferenceQueryKey = "authRef";
    private const string UnsupportedAccountErrorMessage = "Unsupported account type. Sign in with a supported work or school account.";
    private const string LegacyTenantIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
    private const string LegacyIdentityProviderClaimType = "http://schemas.microsoft.com/identity/claims/identityprovider";

    /// <summary>
    /// Adds Aspire-managed Azure storage service clients used by the web host.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddWebStorageClients(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAzureBlobServiceClient(connectionName: "blobs");

        return builder;
    }

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
        services.AddScoped<UserFacingFailureDiagnostics>();
        services.AddScoped<ImportToPlanner.Application.Abstractions.ICurrentTenantContextAccessor, ClaimsTenantContextAccessor>();

        var tenantAuthorityConfiguration = services
            .Where(descriptor => descriptor.ServiceType == typeof(TenantAuthorityConfiguration))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<TenantAuthorityConfiguration>()
            .LastOrDefault()
            ?? TenantAuthorityConfiguration.FromConfiguration(configuration);
        services.TryAddSingleton(tenantAuthorityConfiguration);

        var graphScopes = tenantAuthorityConfiguration.RequiredScopes;

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi(graphScopes)
            .AddInMemoryTokenCaches();

        services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<TenantAuthorityConfiguration>((options, configuredAuthority) =>
            {
                options.ResponseType = OpenIdConnectResponseType.Code;

                var existingOnTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    if (existingOnTokenValidated is not null)
                    {
                        await existingOnTokenValidated(context);
                    }

                    if (context.Result?.Handled == true)
                    {
                        return;
                    }

                    if (!configuredAuthority.IsSharedOrganisations)
                    {
                        return;
                    }

                    var tenantId = context.Principal?.FindFirst("tid")?.Value
                        ?? context.Principal?.FindFirst(LegacyTenantIdentifierClaimType)?.Value;
                    var identityProvider = context.Principal?.FindFirst("idp")?.Value
                        ?? context.Principal?.FindFirst(LegacyIdentityProviderClaimType)?.Value;
                    if (string.IsNullOrWhiteSpace(tenantId)
                        || string.Equals(tenantId, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(identityProvider) && identityProvider.Contains("live.com", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Fail(UnsupportedAccountErrorMessage);
                    }
                };

                var existingOnAuthenticationFailed = options.Events.OnAuthenticationFailed;
                options.Events.OnAuthenticationFailed = async context =>
                {
                    if (existingOnAuthenticationFailed is not null)
                    {
                        await existingOnAuthenticationFailed(context).ConfigureAwait(false);
                    }

                    if (context.Result?.Handled == true)
                    {
                        return;
                    }

                    if (!configuredAuthority.IsSharedOrganisations)
                    {
                        return;
                    }

                    if (TryMapHostedAuthenticationFailure(context.Exception?.Message, configuredAuthority, out var failure))
                    {
                        var failurePresentation = RecordHostedAuthenticationFailure(
                            context.HttpContext,
                            context.Exception,
                            failure,
                            "open_id_connect.authentication_failed");
                        RedirectToHomeWithAuthError(context, failurePresentation);
                    }
                };

                var existingOnRemoteFailure = options.Events.OnRemoteFailure;
                options.Events.OnRemoteFailure = async context =>
                {
                    if (existingOnRemoteFailure is not null)
                    {
                        await existingOnRemoteFailure(context).ConfigureAwait(false);
                    }

                    if (context.Result?.Handled == true)
                    {
                        return;
                    }

                    if (!configuredAuthority.IsSharedOrganisations)
                    {
                        return;
                    }

                    if (TryMapHostedAuthenticationFailure(context.Failure?.Message, configuredAuthority, out var failure))
                    {
                        var failurePresentation = RecordHostedAuthenticationFailure(
                            context.HttpContext,
                            context.Failure,
                            failure,
                            "open_id_connect.remote_failure");
                        RedirectToHomeWithAuthError(context, failurePresentation);
                    }
                };
            });

        services.AddScoped<GraphServiceClient>(serviceProvider =>
        {
            var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var configuredAuthority = serviceProvider.GetRequiredService<TenantAuthorityConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger<MicrosoftIdentityAccessTokenProvider>>();
            var failureDiagnostics = serviceProvider.GetRequiredService<UserFacingFailureDiagnostics>();
            var accessTokenProvider = new MicrosoftIdentityAccessTokenProvider(tokenAcquisition, httpContextAccessor, configuredAuthority, logger, graphScopes, failureDiagnostics);
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

    private static string BuildAdminConsentMessage(TenantAuthorityConfiguration tenantAuthorityConfiguration)
    {
        ArgumentNullException.ThrowIfNull(tenantAuthorityConfiguration);

        return tenantAuthorityConfiguration.AdminConsentUri is null
            ? "Administrator consent is required before this tenant can continue."
            : $"Administrator consent is required before this tenant can continue. Ask your administrator to approve access: {tenantAuthorityConfiguration.AdminConsentUri}";
    }

    private static bool TryMapHostedAuthenticationFailure(
        string? failureMessage,
        TenantAuthorityConfiguration tenantAuthorityConfiguration,
        out HostedAuthenticationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(tenantAuthorityConfiguration);

        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            failure = default;
            return false;
        }

        if (failureMessage.Contains("unsupported account", StringComparison.OrdinalIgnoreCase))
        {
            failure = new HostedAuthenticationFailure(
                UnsupportedAccountErrorMessage,
                PlannerFailureCategory.Authentication.ToString(),
                ConsentResolutionStatus.Unknown,
                "auth.unsupported_account");
            return true;
        }

        if (failureMessage.Contains("admin_consent", StringComparison.OrdinalIgnoreCase)
            || failureMessage.Contains("consent_required", StringComparison.OrdinalIgnoreCase)
            || failureMessage.Contains("access_denied", StringComparison.OrdinalIgnoreCase)
            || failureMessage.Contains("administrator consent", StringComparison.OrdinalIgnoreCase))
        {
            failure = new HostedAuthenticationFailure(
                BuildAdminConsentMessage(tenantAuthorityConfiguration),
                PlannerFailureCategory.Authorisation.ToString(),
                ConsentResolutionStatus.AdminConsentRequired,
                "auth.admin_consent_required");
            return true;
        }

        failure = default;
        return false;
    }

    private static FailurePresentation RecordHostedAuthenticationFailure(
        HttpContext httpContext,
        Exception? exception,
        HostedAuthenticationFailure failure,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(DependencyInjection).FullName!);
        var failureDiagnostics = httpContext.RequestServices.GetRequiredService<UserFacingFailureDiagnostics>();
        return failureDiagnostics.RecordHandledFailure(
            logger,
            exception,
            operation,
            failure.FailureCategory,
            failure.UserSafeMessage,
            LogLevel.Warning,
            consentStatus: failure.ConsentStatus,
            failureCode: failure.FailureCode);
    }

    private static void RedirectToHomeWithAuthError(AuthenticationFailedContext context, FailurePresentation failurePresentation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(failurePresentation.UserMessage);

        context.HandleResponse();
        context.Response.Redirect(BuildAuthenticationErrorRedirectUri(failurePresentation));
    }

    private static void RedirectToHomeWithAuthError(RemoteFailureContext context, FailurePresentation failurePresentation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(failurePresentation.UserMessage);

        context.HandleResponse();
        context.Response.Redirect(BuildAuthenticationErrorRedirectUri(failurePresentation));
    }

    private static string BuildAuthenticationErrorRedirectUri(FailurePresentation failurePresentation)
    {
        var encodedMessage = Uri.EscapeDataString(failurePresentation.UserMessage);
        if (string.IsNullOrWhiteSpace(failurePresentation.ReferenceId))
        {
            return $"/?{AuthenticationErrorQueryKey}={encodedMessage}";
        }

        var encodedReferenceId = Uri.EscapeDataString(failurePresentation.ReferenceId);
        return $"/?{AuthenticationErrorQueryKey}={encodedMessage}&{AuthenticationReferenceQueryKey}={encodedReferenceId}";
    }

    private readonly record struct HostedAuthenticationFailure(
        string UserSafeMessage,
        string FailureCategory,
        ConsentResolutionStatus ConsentStatus,
        string FailureCode);
}
