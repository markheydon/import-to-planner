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
    public async Task AddWebHostServices_WhenHostedAuthenticationFailsForUnsupportedAccount_RedirectsWithFriendlyError()
    {
        var serviceProvider = BuildHostedServiceProvider();
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
        var serviceProvider = BuildHostedServiceProvider();
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

    private static ServiceProvider BuildHostedServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "organizations",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:CallbackPath"] = "/signin-oidc",
                ["DownstreamApis:MicrosoftGraph:Scopes:0"] = "User.Read",
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(new DeploymentModeConfiguration(
            DeploymentMode.HostedSharedMultiTenant,
            "organizations",
            true,
            false,
            "SingleActiveReplica",
            ["User.Read"],
            new Uri("https://login.microsoftonline.com/organizations/v2.0/adminconsent?client_id=test")));

        services.AddWebHostServices(configuration);

        return services.BuildServiceProvider();
    }

    private static AuthenticationScheme CreateOpenIdConnectScheme()
        => new(OpenIdConnectDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, typeof(OpenIdConnectHandler));
}
