using System.Security.Claims;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;

namespace ImportToPlanner.Web.Tests;

public sealed class MicrosoftIdentityAccessTokenProviderTests
{
    private static readonly IReadOnlyCollection<string> GraphScopes = ["User.Read"];
    private static readonly DeploymentModeConfiguration SelfHostedDeploymentModeConfiguration = new(
        DeploymentMode.SelfHostedSingleTenant,
        "tenant-self-hosted",
        true,
        false,
        "SingleActiveReplica",
        ["User.Read"],
        null);

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenUserIsUnauthenticated_ThrowsInvalidOperationException()
    {
        var tokenAcquisition = new FakeTokenAcquisition();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me")));

        Assert.Contains("authenticated user context", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenUserHasOidTid_AddsMappedAndUniqueAccountClaims()
    {
        var tokenAcquisition = new FakeTokenAcquisition();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "object-id"),
            new Claim("tid", "tenant-id"),
        ], authenticationType: "test-auth"));
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration);

        _ = await provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me"));

        Assert.NotNull(tokenAcquisition.CapturedUser);
        Assert.Equal("object-id", tokenAcquisition.CapturedUser!.FindFirst("uid")?.Value);
        Assert.Equal("tenant-id", tokenAcquisition.CapturedUser.FindFirst("utid")?.Value);
        Assert.Equal("object-id", tokenAcquisition.CapturedUser.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value);
        Assert.Equal("tenant-id", tokenAcquisition.CapturedUser.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, tokenAcquisition.CapturedAuthenticationScheme);
    }

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenSelfHostedAndTenantClaimMissing_UsesAuthorityTenantFallback()
    {
        var tokenAcquisition = new FakeTokenAcquisition();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "object-id"),
        ], authenticationType: "test-auth"));
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration);

        _ = await provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me"));

        Assert.NotNull(tokenAcquisition.CapturedUser);
        Assert.Equal("tenant-self-hosted", tokenAcquisition.CapturedUser!.FindFirst("tid")?.Value);
        Assert.Equal("tenant-self-hosted", tokenAcquisition.CapturedUser.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value);
    }

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenPreferredUsernameMissing_AddsLoginHintClaims()
    {
        var tokenAcquisition = new FakeTokenAcquisition();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "object-id"),
            new Claim("tid", "tenant-id"),
            new Claim("upn", "person@contoso.com"),
        ], authenticationType: "test-auth"));
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration);

        _ = await provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me"));

        Assert.NotNull(tokenAcquisition.CapturedUser);
        Assert.Equal("person@contoso.com", tokenAcquisition.CapturedUser!.FindFirst("preferred_username")?.Value);
        Assert.Equal("person@contoso.com", tokenAcquisition.CapturedUser.FindFirst("login_hint")?.Value);
    }

    private static IAccessTokenProvider CreateProvider(
        ITokenAcquisition tokenAcquisition,
        ClaimsPrincipal user,
        DeploymentModeConfiguration deploymentModeConfiguration)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };

        var providerType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.MicrosoftIdentityAccessTokenProvider", throwOnError: true)!;
        return (IAccessTokenProvider)Activator.CreateInstance(providerType, tokenAcquisition, httpContextAccessor, deploymentModeConfiguration, GraphScopes)!;
    }

    private sealed class FakeTokenAcquisition : ITokenAcquisition
    {
        public ClaimsPrincipal? CapturedUser { get; private set; }
        public string? CapturedAuthenticationScheme { get; private set; }

        public Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            CapturedUser = user;
            CapturedAuthenticationScheme = authenticationScheme;
            return Task.FromResult("token");
        }

        public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => Task.FromException<AuthenticationResult>(new NotSupportedException());

        public Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => Task.FromResult("app-token");

        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => Task.FromException<AuthenticationResult>(new NotSupportedException());

        public void ReplyForbiddenWithWwwAuthenticateHeader(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalUiRequiredException,
            string? authenticationScheme = null,
            HttpResponse? httpResponse = null)
        {
        }

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
            => authenticationScheme ?? string.Empty;

        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalUiRequiredException,
            HttpResponse? httpResponse = null)
            => Task.CompletedTask;
    }
}
