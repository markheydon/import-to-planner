using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests;

public sealed class ImportPresenterTests
{
    [Fact]
    public async Task ImportExecutionPresenter_PresentsUserFacingErrorsFromNeutralFailures()
    {
        var presenter = new ImportExecutionPresenter();
        var response = new ImportExecutionResult
        {
            PlanId = "plan-1",
            CreatedItems = [new ImportExecutionItem(PlannerFailureTarget.Task, "Task A")],
            ReusedOrSkippedItems = [],
            FailureItems =
            [
                new PlannerOperationFailure(
                    PlannerFailureCategory.Authentication,
                    PlannerFailureTarget.Workflow,
                    null,
                    "Auth failed.")
            ],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(1, 0, 1, 0, true, false),
        };

        await presenter.PresentAsync(response, CancellationToken.None);

        Assert.NotNull(presenter.ViewModel);
        var viewModel = presenter.ViewModel!;
        Assert.Contains(viewModel.Errors, error =>
            error.Contains("Sign in again", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportExecutionPresenter_PresentsCreditExhaustionCopy()
    {
        var presenter = new ImportExecutionPresenter();
        var response = new ImportExecutionResult
        {
            PlanId = "plan-1",
            CreatedItems = [],
            ReusedOrSkippedItems = [],
            FailureItems =
            [
                new PlannerOperationFailure(
                    PlannerFailureCategory.Validation,
                    PlannerFailureTarget.Task,
                    "Task B",
                    "Import stopped because your organisation has no credits remaining for new tasks.",
                    false,
                    "credits.exhausted"),
            ],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(0, 0, 1, 0, false, true),
            CreditsUsed = 0,
            RemainingCredits = 0,
        };

        await presenter.PresentAsync(response, CancellationToken.None);

        Assert.Contains(
            presenter.ViewModel!.Errors,
            error => error.Contains("Credit exhausted", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportExecutionPresenter_PresentsCreditBalanceReportUnavailableCopy()
    {
        var presenter = new ImportExecutionPresenter();
        var response = new ImportExecutionResult
        {
            PlanId = "plan-1",
            CreatedItems = [],
            ReusedOrSkippedItems = [new ImportExecutionItem(PlannerFailureTarget.Task, "Existing Task")],
            FailureItems =
            [
                new PlannerOperationFailure(
                    PlannerFailureCategory.Unavailable,
                    PlannerFailureTarget.Workflow,
                    null,
                    "Remaining credits could not be loaded for this execution report.",
                    false,
                    "credits.balance_report_unavailable"),
            ],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(0, 1, 1, 0, true, false),
            CreditsUsed = 0,
            RemainingCredits = null,
        };

        await presenter.PresentAsync(response, CancellationToken.None);

        Assert.Contains(
            presenter.ViewModel!.Errors,
            error => error.Contains("Remaining credits could not be loaded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlannerFailureMessageMapper_WhenAdminConsentIsRequired_PreservesConsentUri()
    {
        var consentUri = new Uri("https://contoso.example/admin-consent");
        var message = PlannerFailureMessageMapper.ToUserSafeMessage(
            new PlannerOperationFailure(
                PlannerFailureCategory.Authorisation,
                PlannerFailureTarget.Workflow,
                null,
                $"Administrator consent is required before this hosted tenant can continue. Ask your administrator to approve access: {consentUri}",
                false,
                "auth.admin_consent_required"));

        Assert.Contains("administrator", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approve", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(consentUri.AbsoluteUri, message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlannerFailureMessageMapper_WhenUnsupportedAccountFailure_ReturnsHostedAccountGuidance()
    {
        var message = PlannerFailureMessageMapper.ToUserSafeMessage(
            new PlannerOperationFailure(
                PlannerFailureCategory.Authentication,
                PlannerFailureTarget.Workflow,
                null,
                "Unsupported account type.",
                false,
                "UnsupportedAccount"));

        Assert.Contains("work or school", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlannerFailureMessageMapper_WhenTenantContextMismatch_ReturnsWorkflowRefreshGuidance()
    {
        var message = PlannerFailureMessageMapper.ToUserSafeMessage(
            new PlannerOperationFailure(
                PlannerFailureCategory.Conflict,
                PlannerFailureTarget.Workflow,
                null,
                "Tenant context changed.",
                false,
                "TenantMismatch"));

        Assert.Contains("fresh preview", message, StringComparison.OrdinalIgnoreCase);
    }
}
