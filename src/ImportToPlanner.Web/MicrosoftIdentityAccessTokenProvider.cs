using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;

namespace ImportToPlanner.Web;

internal sealed class MicrosoftIdentityAccessTokenProvider : IAccessTokenProvider
{
    private readonly ITokenAcquisition tokenAcquisition;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IReadOnlyCollection<string> scopes;

    public MicrosoftIdentityAccessTokenProvider(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        IReadOnlyCollection<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(tokenAcquisition);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(scopes);

        this.tokenAcquisition = tokenAcquisition;
        this.httpContextAccessor = httpContextAccessor;
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

        return await tokenAcquisition.GetAccessTokenForUserAsync(
            scopes,
            authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
            user: user).ConfigureAwait(false);
    }
}
