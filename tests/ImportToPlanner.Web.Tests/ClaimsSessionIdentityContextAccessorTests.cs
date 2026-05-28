using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.Web.Tests;

public sealed class ClaimsSessionIdentityContextAccessorTests
{
    [Fact]
    public void TryGetCurrent_WhenAuthenticatedWithPreferredUsernameAndTenantDisplayName_ReturnsDisplayIdentityContext()
    {
        var accessor = CreateAccessor(
            CreatePrincipal(
                [
                    new Claim("tid", "tenant-001"),
                    new Claim("oid", "user-001"),
                    new Claim("preferred_username", "user@contoso.com"),
                    new Claim("tenant_display_name", "Contoso Ltd"),
                ]));

        var identity = accessor.TryGetCurrent();

        Assert.NotNull(identity);
        Assert.Equal("tenant-001", identity!.TenantId);
        Assert.Equal("user-001", identity.UserId);
        Assert.Equal("user@contoso.com", identity.EmailAddress);
        Assert.Equal("Contoso Ltd", identity.TenantName);
    }

    [Fact]
    public void TryGetCurrent_WhenPreferredClaimsMissing_UsesEmailAndLegacyTenantNameFallbackClaims()
    {
        var accessor = CreateAccessor(
            CreatePrincipal(
                [
                    new Claim("tid", "tenant-001"),
                    new Claim("oid", "user-001"),
                    new Claim("email", "fallback@contoso.com"),
                    new Claim("http://schemas.microsoft.com/identity/claims/tenantdisplayname", "Contoso Legacy"),
                ]));

        var identity = accessor.TryGetCurrent();

        Assert.NotNull(identity);
        Assert.Equal("fallback@contoso.com", identity!.EmailAddress);
        Assert.Equal("Contoso Legacy", identity.TenantName);
    }

    private static ClaimsSessionIdentityContextAccessor CreateAccessor(ClaimsPrincipal principal)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "organizations",
                ["AzureAd:HomeTenantId"] = "multiple",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:CallbackPath"] = "/signin-oidc",
                ["DownstreamApis:MicrosoftGraph:Scopes:0"] = "User.Read",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();

        var authorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(configuration);
        return new ClaimsSessionIdentityContextAccessor(httpContextAccessor, authorityConfiguration);
    }

    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
        => new(new ClaimsIdentity(claims, "test-auth"));
}
