using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Tests;

public sealed class HostedAuthenticationEventTests
{
    [Fact]
    public void FromConfiguration_WhenHomeTenantIdIsMultiple_ResolvesSharedAuthority()
    {
        var configuration = CreateConfiguration("home-tenant", "multiple");

        AzureAdConfigurationNormalizer.Apply(configuration);
        var authority = TenantAuthorityConfiguration.FromConfiguration(configuration);

        Assert.Equal("organizations", authority.TenantId);
        Assert.Equal("home-tenant", authority.AppRegistrationTenantId);
        Assert.Equal("multiple", authority.HomeTenantId);
        Assert.Equal(TenantAuthorityKind.SharedOrganisations, authority.AuthorityKind);
    }

    [Fact]
    public void FromConfiguration_WhenHomeTenantIdIsSpecificTenant_ResolvesSpecificAuthority()
    {
        var configuration = CreateConfiguration("app-registration-tenant", "tenant-specific");

        AzureAdConfigurationNormalizer.Apply(configuration);
        var authority = TenantAuthorityConfiguration.FromConfiguration(configuration);

        Assert.Equal("tenant-specific", authority.TenantId);
        Assert.Equal("app-registration-tenant", authority.AppRegistrationTenantId);
        Assert.Equal("tenant-specific", authority.HomeTenantId);
        Assert.Equal(TenantAuthorityKind.SpecificTenant, authority.AuthorityKind);
    }

    [Fact]
    public void FromConfiguration_WhenHomeTenantIdIsMissing_FallsBackToLegacyTenantId()
    {
        var configuration = CreateConfiguration("tenant-specific", null);

        AzureAdConfigurationNormalizer.Apply(configuration);
        var authority = TenantAuthorityConfiguration.FromConfiguration(configuration);

        Assert.Equal("tenant-specific", authority.TenantId);
        Assert.Equal("tenant-specific", authority.AppRegistrationTenantId);
        Assert.Null(authority.HomeTenantId);
        Assert.Equal(TenantAuthorityKind.SpecificTenant, authority.AuthorityKind);
    }

    [Fact]
    public async Task AddWebHostServices_WhenHostedAuthenticationFailsForUnsupportedAccount_RedirectsWithFriendlyError()
    {
        var serviceProvider = BuildHostedServiceProvider("home-tenant", "multiple");
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        var context = new AuthenticationFailedContext(
            httpContext,
            CreateOpenIdConnectScheme(),
            options)
        {
            Exception = new InvalidOperationException("Unsupported account type. Sign in with a supported work or school account."),
        };

        await options.Events.AuthenticationFailed(context);

        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        var location = httpContext.Response.Headers.Location.ToString();
        Assert.StartsWith("/?authError=", location, StringComparison.Ordinal);
        Assert.Contains("Unsupported%20account%20type", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddWebHostServices_WhenHostedRemoteFailureIsAccessDenied_RedirectsWithFriendlyError()
    {
        var serviceProvider = BuildHostedServiceProvider("home-tenant", "multiple");
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        var context = new RemoteFailureContext(
            httpContext,
            CreateOpenIdConnectScheme(),
            options,
            new InvalidOperationException("access_denied"));

        await options.Events.RemoteFailure(context);

        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        var location = httpContext.Response.Headers.Location.ToString();
        Assert.StartsWith("/?authError=", location, StringComparison.Ordinal);
        Assert.Contains("Administrator%20consent", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddWebHostServices_WhenSpecificTenantAuthenticationFails_DoesNotRedirectWithHostedGuidance()
    {
        var serviceProvider = BuildHostedServiceProvider("tenant-specific", "tenant-specific");
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        var context = new AuthenticationFailedContext(
            httpContext,
            CreateOpenIdConnectScheme(),
            options)
        {
            Exception = new InvalidOperationException("Unsupported account type. Sign in with a supported work or school account."),
        };

        await options.Events.AuthenticationFailed(context);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.True(string.IsNullOrWhiteSpace(httpContext.Response.Headers.Location.ToString()));
    }

    private static ServiceProvider BuildHostedServiceProvider(string tenantId, string? homeTenantId)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(tenantId, homeTenantId);

        AzureAdConfigurationNormalizer.Apply(configuration);

        var authority = TenantAuthorityConfiguration.FromConfiguration(configuration);
        var storage = StorageConfiguration.FromConfiguration(configuration);

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(authority);
        services.AddSingleton(storage);
        services.AddSingleton(new ConsentResolutionDefaults(authority.RequiredScopes, authority.AdminConsentUri));

        services.AddWebHostServices(configuration);

        return services.BuildServiceProvider();
    }

    private static ConfigurationManager CreateConfiguration(string tenantId, string? homeTenantId)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = tenantId,
            ["AzureAd:HomeTenantId"] = homeTenantId,
            ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
            ["AzureAd:CallbackPath"] = "/signin-oidc",
            ["DownstreamApis:MicrosoftGraph:Scopes:0"] = "User.Read",
            ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
            ["Storage:DataProtectionContainer"] = "dataprotection",
            ["Storage:DataProtectionBlob"] = "keys.xml",
        });

        return configuration;
    }

    private static AuthenticationScheme CreateOpenIdConnectScheme()
        => new(OpenIdConnectDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, typeof(OpenIdConnectHandler));
}
