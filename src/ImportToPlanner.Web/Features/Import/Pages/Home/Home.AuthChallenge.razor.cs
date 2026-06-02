using ImportToPlanner.Application.Common.Models;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Web.Features.Import.Workflows;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Identity.Web;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private async Task<bool> TryHandleAuthenticationChallengeAsync(Exception exception)
    {
        if (exception is not MicrosoftIdentityWebChallengeUserException)
        {
            return false;
        }

        var failurePresentation = FailureDiagnostics.RecordHandledFailure(
            Logger,
            exception,
            "workflow.authentication.challenge",
            PlannerFailureCategory.Authentication.ToString(),
            InteractiveTokenAcquisitionRequiredMessage,
            LogLevel.Warning,
            tenantContext: WorkflowState.ActiveTenantContext,
            consentStatus: WorkflowState.ConsentResolution?.Status ?? ConsentResolutionStatus.Unknown,
            failureCode: "graph.token.challenge_required");

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            NavigationManager.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
            return true;
        }

        if (!HasTokenReauthenticationQueryFlag())
        {
            NavigationManager.NavigateTo(BuildInteractiveChallengeUri(authState.User), forceLoad: true);
            return true;
        }

        SetStatus(InteractiveTokenAcquisitionRequiredMessage, WorkflowStatusLevel.Warning, failurePresentation.ReferenceId);
        return true;
    }

    private bool HasTokenReauthenticationQueryFlag()
    {
        var query = new Uri(NavigationManager.Uri).Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var parsedQuery = QueryHelpers.ParseQuery(query);
        return parsedQuery.TryGetValue(TokenReauthenticationQueryKey, out var tokenReauthenticationValue)
               && string.Equals(tokenReauthenticationValue.ToString(), "1", StringComparison.Ordinal);
    }

    private void RemoveTokenReauthenticationQueryFlag()
    {
        var currentUri = new Uri(NavigationManager.Uri);
        var parsedQuery = QueryHelpers.ParseQuery(currentUri.Query);
        if (!parsedQuery.Remove(TokenReauthenticationQueryKey))
        {
            return;
        }

        var remainingQuery = parsedQuery
            .SelectMany(
                static pair => pair.Value,
                static (pair, value) => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value ?? string.Empty)}");

        var queryString = string.Join("&", remainingQuery);
        var targetUri = string.IsNullOrEmpty(queryString)
            ? currentUri.AbsolutePath
            : $"{currentUri.AbsolutePath}?{queryString}";

        if (!string.IsNullOrWhiteSpace(currentUri.Fragment))
        {
            targetUri += currentUri.Fragment;
        }

        NavigationManager.NavigateTo(targetUri, forceLoad: false, replace: true);
    }

    private string BuildInteractiveChallengeUri(System.Security.Claims.ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var scopes = Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>() ?? ["User.Read"];
        var challengeScope = string.Join(' ', scopes);
        var loginHint = user.FindFirst("preferred_username")?.Value ?? user.Identity?.Name ?? string.Empty;
        var redirectUri = $"/?{TokenReauthenticationQueryKey}=1";

        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);
        var encodedScope = Uri.EscapeDataString(challengeScope);
        var encodedLoginHint = Uri.EscapeDataString(loginHint);

        return $"MicrosoftIdentity/Account/Challenge?redirectUri={encodedRedirectUri}&scope={encodedScope}&loginHint={encodedLoginHint}";
    }
}
