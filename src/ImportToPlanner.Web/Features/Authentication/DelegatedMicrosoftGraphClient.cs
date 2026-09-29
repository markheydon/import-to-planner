using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace ImportToPlanner.Web.Features.Authentication;

/// <summary>
/// Factory-managed Microsoft Graph client for delegated user access.
/// </summary>
internal sealed class DelegatedMicrosoftGraphClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatedMicrosoftGraphClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client supplied by <see cref="IHttpClientFactory"/>.</param>
    /// <param name="accessTokenProvider">The delegated access token provider.</param>
    public DelegatedMicrosoftGraphClient(
        HttpClient httpClient,
        MicrosoftIdentityAccessTokenProvider accessTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);

        var authenticationProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
        var requestAdapter = new HttpClientRequestAdapter(authenticationProvider, httpClient: httpClient);
        ServiceClient = new GraphServiceClient(requestAdapter);
    }

    /// <summary>
    /// Gets the Kiota Graph service client.
    /// </summary>
    public GraphServiceClient ServiceClient { get; }
}
