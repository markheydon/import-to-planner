using System.Globalization;
using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private int viewedStep = 1;

    private bool viewedStepInitialized;

    private int StepperActiveIndex
    {
        get => viewedStep - 1;
        set
        {
            if (value < 0)
            {
                return;
            }

            var targetStep = value + 1;
            if (targetStep is < 1 or > 5)
            {
                return;
            }

            if (!IsStepLocked(targetStep))
            {
                var previousStep = viewedStep;
                viewedStep = targetStep;
                if (targetStep == 4 && previousStep != 4)
                {
                    _ = RefreshLiveCreditBalanceSnapshotWhenOnPreviewStepAsync();
                }
            }
        }
    }

    private bool IsStepLocked(int step)
        => step switch
        {
            1 => false,
            2 => selectedContainer is null,
            3 => selectedContainer is null || selectedPlan is null,
            4 => selectedContainer is null || selectedPlan is null || string.IsNullOrWhiteSpace(csvContent),
            5 => executionResult is null,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private bool IsStepComplete(int step)
        => step switch
        {
            1 => selectedContainer is not null,
            2 => selectedPlan is not null,
            3 => !string.IsNullOrWhiteSpace(csvContent),
            4 => executionResult is not null,
            5 => executionResult is not null,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private bool IsStepActive(int step) => ActiveStep.HasValue && ActiveStep.Value == step;

    private void InitialiseViewedStep()
    {
        viewedStep = ActiveStep ?? 5;
        viewedStepInitialized = true;
    }

    private void MaybeAdvanceViewedStep()
    {
        if (!viewedStepInitialized)
        {
            return;
        }

        if (!IsStepComplete(viewedStep))
        {
            return;
        }

        if (viewedStep == 5)
        {
            return;
        }

        if (viewedStep == 4)
        {
            if (executionResult is not null)
            {
                viewedStep = 5;
            }
            else
            {
                _ = RefreshLiveCreditBalanceSnapshotWhenOnPreviewStepAsync();
            }

            return;
        }

        viewedStep = ActiveStep ?? viewedStep;
    }

    private bool FocusSetupStepIfReviewingLaterSteps(int setupStep)
    {
        if (!viewedStepInitialized || viewedStep <= 3 || setupStep is < 1 or > 3)
        {
            return false;
        }

        viewedStep = setupStep;
        return true;
    }

    private void SyncViewedStepAfterWorkflowInvalidation()
    {
        if (!viewedStepInitialized)
        {
            return;
        }

        if (!IsStepLocked(viewedStep))
        {
            return;
        }

        viewedStep = ActiveStep ?? GetHighestReachableStep();
    }

    private int GetHighestReachableStep()
    {
        for (var step = 5; step >= 1; step--)
        {
            if (!IsStepLocked(step))
            {
                return step;
            }
        }

        return 1;
    }

    private bool GetMudStepCompleted(int step) => IsStepComplete(step);

    private bool GetMudStepDisabled(int step) => IsStepLocked(step);

    private int GetVisibleSetupPanelCount() => viewedStep <= 3 ? viewedStep : 3;

    private bool IsSetupPanelExpanded(int step) => viewedStep == step;

    private string GetSetupPanelTitle(int step)
    {
        var title = GetWorkflowStepPresentation(step).Title;
        if (IsStepComplete(step) && viewedStep != step)
        {
            return $"{title} — {GetStepSummary(step)}";
        }

        return title;
    }

    private Task OnStepperPreviewInteraction(StepperInteractionEventArgs args)
    {
        var targetStep = args.StepIndex + 1;
        if (targetStep is < 1 or > 5 || IsStepLocked(targetStep))
        {
            args.Cancel = true;
        }

        return Task.CompletedTask;
    }

    private HomeWorkflowStepPresentation GetWorkflowStepPresentation(int step)
    {
        var state = GetWorkflowStepState(step);
        var template = GetWorkflowStepTemplate(step);
        var badgeContent = state == HomeWorkflowStepState.Completed
            ? "✓"
            : step.ToString(CultureInfo.InvariantCulture);
        var summary = IsStepComplete(step) ? GetStepSummary(step) : null;

        return template with
        {
            State = state,
            BadgeContent = badgeContent,
            Summary = summary,
        };
    }

    private HomeWorkflowStepState GetWorkflowStepState(int step)
    {
        if (IsStepComplete(step))
        {
            return HomeWorkflowStepState.Completed;
        }

        if (IsStepActive(step))
        {
            return HomeWorkflowStepState.Current;
        }

        return HomeWorkflowStepState.Upcoming;
    }

    private static HomeWorkflowStepPresentation GetWorkflowStepTemplate(int step)
        => step switch
        {
            1 => new HomeWorkflowStepPresentation(1, "Select Planner location", HomeWorkflowStepState.Upcoming, "1", null, null),
            2 => new HomeWorkflowStepPresentation(2, "Select plan", HomeWorkflowStepState.Upcoming, "2", null, null),
            3 => new HomeWorkflowStepPresentation(3, "Upload CSV", HomeWorkflowStepState.Upcoming, "3", null, null),
            4 => new HomeWorkflowStepPresentation(4, "Preview and confirm", HomeWorkflowStepState.Upcoming, "4", null, "Preview import", "Confirm import"),
            5 => new HomeWorkflowStepPresentation(5, "Report", HomeWorkflowStepState.Upcoming, "5", null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private string? GetStepSecondaryText(int step)
    {
        if (step == 4 && canExecute && executionResult is null)
        {
            return "Preview ready — confirm to import.";
        }

        return IsStepComplete(step) ? GetStepSummary(step) : null;
    }

    private string GetStepSummary(int step)
        => step switch
        {
            1 => $"Location: {FormatContainer(selectedContainer)}",
            2 => $"Plan: {FormatPlan(selectedPlan)}",
            3 => $"CSV: {selectedFileName}",
            4 => executionResult?.Errors.Count > 0
                ? "Import finished with warnings."
                : "Import completed successfully.",
            5 => executionResult?.Errors.Count > 0
                ? "Report available with warnings."
                : "Report available.",
            _ => string.Empty,
        };

    private int CountPreviewActions(string action)
        => preview?.TaskActions.Count(task => string.Equals(task.Action, action, StringComparison.OrdinalIgnoreCase)) ?? 0;
}
