using System.Security.Claims;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenUserNullChallengeOccurs_LogsClaimPresenceWithoutClaimValues()
    {
        var challengeException = CreateChallengeException("user_null");
        var tokenAcquisition = new FakeTokenAcquisition(challengeException);
        var logger = new TestLogger<MicrosoftIdentityAccessTokenProvider>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "object-id-1234567890"),
            new Claim("tid", "tenant-id-0987654321"),
            new Claim("preferred_username", "person@contoso.com"),
            new Claim("upn", "principal@contoso.com"),
            new Claim("email", "mailbox@contoso.com"),
            new Claim(ClaimTypes.Name, "Example Person"),
            new Claim(ClaimTypes.NameIdentifier, "name-identifier-123456"),
        ], authenticationType: "test-auth"));
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration, logger);

        var exception = await Assert.ThrowsAsync<MicrosoftIdentityWebChallengeUserException>(() =>
            provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me")));

        var entry = Assert.Single(logger.Entries);
        Assert.Same(challengeException, exception);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, entry.LogLevel);
        Assert.Same(challengeException, entry.Exception);
        Assert.Equal(true, entry.State["UidPresent"]);
        Assert.Equal(true, entry.State["UtidPresent"]);
        Assert.Equal(true, entry.State["OidPresent"]);
        Assert.Equal(true, entry.State["TidPresent"]);
        Assert.Equal(true, entry.State["PreferredUsernamePresent"]);
        Assert.Equal(true, entry.State["UpnPresent"]);
        Assert.Equal(true, entry.State["EmailPresent"]);
        Assert.Equal(true, entry.State["NamePresent"]);
        Assert.Equal(true, entry.State["NameIdentifierPresent"]);
        Assert.Equal(1, entry.State["IdentityCount"]);
        Assert.Equal(1, entry.State["AuthenticatedIdentityCount"]);
        Assert.All(
            entry.State.Where(pair => !string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal)),
            pair => Assert.True(
                pair.Value is bool or int,
                $"Unexpected diagnostic value type for {pair.Key}: {pair.Value?.GetType().Name ?? "<null>"}"));
        Assert.DoesNotContain("person@contoso.com", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("principal@contoso.com", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("mailbox@contoso.com", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("object-id-1234567890", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("tenant-id-0987654321", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Example Person", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("name-identifier-123456", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAuthorizationTokenAsync_WhenUserNullChallengeOccurs_LogsMissingClaimsAsFalse()
    {
        var tokenAcquisition = new FakeTokenAcquisition(CreateChallengeException("user_null"));
        var logger = new TestLogger<MicrosoftIdentityAccessTokenProvider>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "object-id"),
            new Claim("tid", "tenant-id"),
        ], authenticationType: "test-auth"));
        var provider = CreateProvider(tokenAcquisition, user, SelfHostedDeploymentModeConfiguration, logger);

        _ = await Assert.ThrowsAsync<MicrosoftIdentityWebChallengeUserException>(() =>
            provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me")));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(false, entry.State["PreferredUsernamePresent"]);
        Assert.Equal(false, entry.State["UpnPresent"]);
        Assert.Equal(false, entry.State["EmailPresent"]);
        Assert.Equal(false, entry.State["NamePresent"]);
        Assert.Equal(false, entry.State["NameIdentifierPresent"]);
        Assert.All(
            entry.State.Where(pair => !string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal)),
            pair => Assert.True(
                pair.Value is bool or int,
                $"Unexpected diagnostic value type for {pair.Key}: {pair.Value?.GetType().Name ?? "<null>"}"));
        Assert.DoesNotContain("<missing>", entry.Message, StringComparison.Ordinal);
    }

    private static IAccessTokenProvider CreateProvider(
        ITokenAcquisition tokenAcquisition,
        ClaimsPrincipal user,
        DeploymentModeConfiguration deploymentModeConfiguration)
        => CreateProvider(
            tokenAcquisition,
            user,
            deploymentModeConfiguration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MicrosoftIdentityAccessTokenProvider>.Instance);

    private static IAccessTokenProvider CreateProvider(
        ITokenAcquisition tokenAcquisition,
        ClaimsPrincipal user,
        DeploymentModeConfiguration deploymentModeConfiguration,
        ILogger<MicrosoftIdentityAccessTokenProvider> logger)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };

        var providerType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.MicrosoftIdentityAccessTokenProvider", throwOnError: true)!;
        return (IAccessTokenProvider)Activator.CreateInstance(providerType, tokenAcquisition, httpContextAccessor, deploymentModeConfiguration, logger, GraphScopes)!;
    }

    private static MicrosoftIdentityWebChallengeUserException CreateChallengeException(string errorCode)
        => new(
            new MsalUiRequiredException(errorCode, "Interactive sign-in is required to acquire the downstream Graph token."),
            ["Tasks.ReadWrite"],
            userflow: string.Empty);

    private sealed class FakeTokenAcquisition : ITokenAcquisition
    {
        private readonly Exception? exceptionToThrow;

        public FakeTokenAcquisition(Exception? exceptionToThrow = null)
        {
            this.exceptionToThrow = exceptionToThrow;
        }

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

            if (exceptionToThrow is not null)
            {
                return Task.FromException<string>(exceptionToThrow);
            }

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

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
            => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var structuredState = state as IEnumerable<KeyValuePair<string, object?>>;
            var capturedState = structuredState?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, object?>();
            Entries.Add(new LogEntry(logLevel, eventId, exception, formatter(state, exception), capturedState));
        }
    }

    private sealed record LogEntry(
        Microsoft.Extensions.Logging.LogLevel LogLevel,
        EventId EventId,
        Exception? Exception,
        string Message,
        IReadOnlyDictionary<string, object?> State);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
