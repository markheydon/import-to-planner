using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Import.Workflows;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private async Task<CommercialAccessDecision?> ResolveCommercialAccessDecisionAsync()
    {
        var sessionIdentity = SessionIdentityContextAccessor.TryGetCurrent();
        if (sessionIdentity is null)
        {
            SetStatus(
                "Unable to resolve your signed-in identity. Sign out and sign in again.",
                WorkflowStatusLevel.Warning);
            return null;
        }

        var accessDecision = await CommercialAccessUseCase.ResolveAccessAsync(
            sessionIdentity,
            CommercialModeOptions.Enabled,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        showFirstCommercialSignInGuidance = accessDecision.Decision == CommercialAccessDecisionType.CreateAccount;
        showCommercialDeletedAccountGate = false;
        deletedAccountRetentionExpiresUtc = null;
        restoreAccountStatusMessage = null;

        if (accessDecision.Decision is CommercialAccessDecisionType.BlockedDeleted or CommercialAccessDecisionType.OfferRestore)
        {
            showCommercialDeletedAccountGate = true;
            deletedAccountRetentionExpiresUtc = accessDecision.RetentionExpiresUtc;
            SetStatus(CommercialDeletedAccountMessage, WorkflowStatusLevel.Warning);
            if (accessDecision.ShouldSignOut)
            {
                OnSignOutClicked();
                return null;
            }
        }

        return accessDecision;
    }

    private async Task RestoreCommercialAccountAsync()
    {
        if (isRestoringCommercialAccount)
        {
            return;
        }

        var sessionIdentity = SessionIdentityContextAccessor.TryGetCurrent();
        if (sessionIdentity is null)
        {
            SetStatus(
                "Unable to resolve your signed-in identity. Sign out and sign in again.",
                WorkflowStatusLevel.Warning);
            return;
        }

        isRestoringCommercialAccount = true;
        try
        {
            var restoreResult = await CommercialProfileUseCase.RestoreAccountAsync(sessionIdentity, DateTimeOffset.UtcNow, CancellationToken.None);
            switch (restoreResult)
            {
                case CommercialAccountRestoreResult.Restored:
                    NavigationManager.NavigateTo("/", forceLoad: true);
                    return;
                case CommercialAccountRestoreResult.AccountNotFound:
                    restoreAccountStatusMessage = RestoreAccountNotFoundMessage;
                    break;
                case CommercialAccountRestoreResult.AccountNotDeleted:
                    restoreAccountStatusMessage = RestoreAccountNotDeletedMessage;
                    break;
                case CommercialAccountRestoreResult.RetentionExpired:
                    restoreAccountStatusMessage = RestoreAccountRetentionExpiredMessage;
                    break;
                default:
                    restoreAccountStatusMessage = "Your account could not be restored. Please sign out and sign in again.";
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleUserSafeFailure(ex, "workflow.commercial.restore_account", WorkflowStatusLevel.Error);
        }
        finally
        {
            isRestoringCommercialAccount = false;
        }
    }

    private void OnSignInClicked()
    {
        NavigationManager.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
    }

    private void OnSignOutClicked()
    {
        var returnUrl = Uri.EscapeDataString(new Uri(NavigationManager.BaseUri).PathAndQuery);
        NavigationManager.NavigateTo($"MicrosoftIdentity/Account/SignOut?returnUrl={returnUrl}", forceLoad: true);
    }
}
