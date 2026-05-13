using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Presenters;

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
}
