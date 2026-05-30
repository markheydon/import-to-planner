using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Import.Presenters;
using ImportToPlanner.Web.Features.Import.Workflows;
using Microsoft.AspNetCore.WebUtilities;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    protected override async Task OnInitializedAsync()
    {
        showCommercialLoginGate = false;
        showFirstCommercialSignInGuidance = false;

        var hasAuthErrorFromQuery = false;
        var hasTokenReauthenticationQuery = false;
        var query = new Uri(NavigationManager.Uri).Query;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var parsedQuery = QueryHelpers.ParseQuery(query);
            var authReferenceId = parsedQuery.TryGetValue(AuthenticationReferenceQueryKey, out var authReferenceValues)
                && !string.IsNullOrWhiteSpace(authReferenceValues)
                    ? authReferenceValues.ToString()
                    : null;
            if (parsedQuery.TryGetValue("authError", out var authErrorValues)
                && !string.IsNullOrWhiteSpace(authErrorValues))
            {
                hasAuthErrorFromQuery = true;
                var authFailure = PlannerFailureMessageMapper.FromDiagnosticSignal(
                    PlannerFailureMessageMapper.ResolveDiagnosticCode(authErrorValues.ToString()),
                    authErrorValues.ToString(),
                    WorkflowState.ConsentResolution?.Status ?? ConsentResolutionStatus.Unknown,
                    allowRawMessage: true);
                SetStatus(
                    authFailure.UserMessage,
                    WorkflowStatusLevel.Warning,
                    authReferenceId,
                    preserveGuidanceFlags: true,
                    failureSignals: authFailure);
            }

            if (parsedQuery.TryGetValue(TokenReauthenticationQueryKey, out var tokenReauthenticationValues)
                && string.Equals(tokenReauthenticationValues.ToString(), "1", StringComparison.Ordinal))
            {
                hasTokenReauthenticationQuery = true;
            }
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        IdentityPresentation = SessionIdentityPresenter.Present(SessionIdentityContextAccessor.TryGetCurrent());

        if (CommercialModeOptions.Enabled)
        {
            if (!isAuthenticated)
            {
                showCommercialLoginGate = true;
                return;
            }

            var decision = await ResolveCommercialAccessDecisionAsync();
            if (decision is null)
            {
                return;
            }
        }

        if (!isAuthenticated)
        {
            if (hasAuthErrorFromQuery)
            {
                // Keep the user on the page with the auth error message instead of re-triggering sign-in.
                return;
            }

            NavigationManager.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
            return;
        }

        try
        {
            await WorkflowCoordinator.LoadContainersAsync(WorkflowState, CancellationToken.None);

            if (hasTokenReauthenticationQuery)
            {
                RemoveTokenReauthenticationQueryFlag();
            }
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            HandleUserSafeFailure(ex, "workflow.initialise.load_containers", WorkflowStatusLevel.Error);
        }
    }
}
