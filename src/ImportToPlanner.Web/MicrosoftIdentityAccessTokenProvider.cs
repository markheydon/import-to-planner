using System.Security.Claims;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;

namespace ImportToPlanner.Web;

internal sealed class MicrosoftIdentityAccessTokenProvider : IAccessTokenProvider
{
    private const string ObjectIdentifierClaimType = "oid";
    private const string TenantIdentifierClaimType = "tid";
    private const string LegacyObjectIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string LegacyTenantIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
    private const string UniqueObjectIdentifierClaimType = "uid";
    private const string UniqueTenantIdentifierClaimType = "utid";
    private const string PreferredUsernameClaimType = "preferred_username";
    private const string LoginHintClaimType = "login_hint";
    private const string UpnClaimType = "upn";
    private const string EmailClaimType = "email";
    private const string CommonAuthorityTenant = "common";
    private const string OrganizationsAuthorityTenant = "organizations";
    private readonly ITokenAcquisition tokenAcquisition;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly DeploymentModeConfiguration deploymentModeConfiguration;
    private readonly ILogger<MicrosoftIdentityAccessTokenProvider> logger;
    private readonly IReadOnlyCollection<string> scopes;

    public MicrosoftIdentityAccessTokenProvider(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        DeploymentModeConfiguration deploymentModeConfiguration,
        IReadOnlyCollection<string> scopes)
        : this(
            tokenAcquisition,
            httpContextAccessor,
            deploymentModeConfiguration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MicrosoftIdentityAccessTokenProvider>.Instance,
            scopes)
    {
    }

    public MicrosoftIdentityAccessTokenProvider(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        DeploymentModeConfiguration deploymentModeConfiguration,
        ILogger<MicrosoftIdentityAccessTokenProvider> logger,
        IReadOnlyCollection<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(tokenAcquisition);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(deploymentModeConfiguration);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopes);

        this.tokenAcquisition = tokenAcquisition;
        this.httpContextAccessor = httpContextAccessor;
        this.deploymentModeConfiguration = deploymentModeConfiguration;
        this.logger = logger;
        this.scopes = scopes;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator(["graph.microsoft.com"]);

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();

        if (!AllowedHostsValidator.IsUrlHostValid(uri))
        {
            return string.Empty;
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("An authenticated user context is required to acquire a Graph access token.");
        }

        user = EnsureMsalAccountIdentifiers(user, ResolveFallbackTenantIdentifier());

        try
        {
            return await tokenAcquisition.GetAccessTokenForUserAsync(
                scopes,
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                user: user).ConfigureAwait(false);
        }
        catch (MicrosoftIdentityWebChallengeUserException exception)
            when (exception.InnerException is MsalUiRequiredException msalUiRequiredException
                  && string.Equals(msalUiRequiredException.ErrorCode, "user_null", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                exception,
                "Graph token acquisition failed with user_null. Claim snapshot: uid={Uid}, utid={Utid}, oid={Oid}, tid={Tid}, objectidentifier={LegacyOid}, tenantid={LegacyTid}, preferred_username={PreferredUsername}, upn={Upn}, email={Email}, name={Name}, nameidentifier={NameIdentifier}, identities={IdentityCount}.",
                GetClaimForDiagnostics(user, UniqueObjectIdentifierClaimType),
                GetClaimForDiagnostics(user, UniqueTenantIdentifierClaimType),
                GetClaimForDiagnostics(user, ObjectIdentifierClaimType),
                GetClaimForDiagnostics(user, TenantIdentifierClaimType),
                GetClaimForDiagnostics(user, LegacyObjectIdentifierClaimType),
                GetClaimForDiagnostics(user, LegacyTenantIdentifierClaimType),
                GetClaimForDiagnostics(user, PreferredUsernameClaimType),
                GetClaimForDiagnostics(user, UpnClaimType, ClaimTypes.Upn),
                GetClaimForDiagnostics(user, EmailClaimType, ClaimTypes.Email),
                GetClaimForDiagnostics(user, "name", ClaimTypes.Name),
                GetClaimForDiagnostics(user, ClaimTypes.NameIdentifier),
                user.Identities.Count());

            throw;
        }
    }

    private static ClaimsPrincipal EnsureMsalAccountIdentifiers(ClaimsPrincipal user, string? fallbackTenantId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var objectId = FindFirstClaimValue(user, ObjectIdentifierClaimType, LegacyObjectIdentifierClaimType, UniqueObjectIdentifierClaimType);
        var tenantId = FindFirstClaimValue(user, TenantIdentifierClaimType, LegacyTenantIdentifierClaimType, UniqueTenantIdentifierClaimType);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            tenantId = fallbackTenantId;
        }

        var loginHint = FindFirstClaimValue(
            user,
            PreferredUsernameClaimType,
            UpnClaimType,
            EmailClaimType,
            ClaimTypes.Upn,
            ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(objectId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return user;
        }

        var principal = new ClaimsPrincipal();
        foreach (var identity in user.Identities)
        {
            principal.AddIdentity(CloneIdentityWithMsalAccountIdentifiers(identity, objectId, tenantId, loginHint));
        }

        return principal;
    }

    private string? ResolveFallbackTenantIdentifier()
    {
        if (deploymentModeConfiguration.Mode != DeploymentMode.SelfHostedSingleTenant)
        {
            return null;
        }

        var authorityTenant = deploymentModeConfiguration.AuthorityTenant;
        if (string.IsNullOrWhiteSpace(authorityTenant)
            || string.Equals(authorityTenant, CommonAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(authorityTenant, OrganizationsAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(authorityTenant, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorityTenant;
    }

    private static ClaimsIdentity CloneIdentityWithMsalAccountIdentifiers(
        ClaimsIdentity identity,
        string objectId,
        string tenantId,
        string? loginHint)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var clone = new ClaimsIdentity(identity);

        AddClaimIfMissing(clone, ObjectIdentifierClaimType, objectId);
        AddClaimIfMissing(clone, TenantIdentifierClaimType, tenantId);
        AddClaimIfMissing(clone, LegacyObjectIdentifierClaimType, objectId);
        AddClaimIfMissing(clone, LegacyTenantIdentifierClaimType, tenantId);
        AddClaimIfMissing(clone, UniqueObjectIdentifierClaimType, objectId);
        AddClaimIfMissing(clone, UniqueTenantIdentifierClaimType, tenantId);
        AddClaimIfMissing(clone, PreferredUsernameClaimType, loginHint);
        AddClaimIfMissing(clone, LoginHintClaimType, loginHint);

        return clone;
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string claimType, string? claimValue)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return;
        }

        if (identity.HasClaim(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal)))
        {
            return;
        }

        identity.AddClaim(new Claim(claimType, claimValue));
    }

    private static string? FindFirstClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(claimTypes);

        for (var index = 0; index < claimTypes.Length; index++)
        {
            var value = principal.FindFirst(claimTypes[index])?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string GetClaimForDiagnostics(ClaimsPrincipal principal, params string[] claimTypes)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(claimTypes);

        var value = FindFirstClaimValue(principal, claimTypes);
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<missing>";
        }

        if (value.Length <= 12)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, 6), "...", value.AsSpan(value.Length - 4));
    }
}
