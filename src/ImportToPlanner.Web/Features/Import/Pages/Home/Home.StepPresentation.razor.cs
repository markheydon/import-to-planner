using System.Globalization;
using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private bool IsStepLocked(int step)
        => step switch
        {
            1 => false,
            2 => selectedContainer is null,
            3 => selectedContainer is null || selectedPlan is null,
            4 => selectedContainer is null || selectedPlan is null || string.IsNullOrWhiteSpace(csvContent),
            5 => executionResult is null && !canExecute,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private bool IsStepComplete(int step)
        => step switch
        {
            1 => selectedContainer is not null,
            2 => selectedPlan is not null,
            3 => !string.IsNullOrWhiteSpace(csvContent),
            4 => preview is not null && parseErrors.Count == 0 && !isPreviewStale && IsCurrentSelectionInSyncWithRequest(),
            5 => executionResult is not null,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private bool IsStepActive(int step) => ActiveStep.HasValue && ActiveStep.Value == step;

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
            4 => new HomeWorkflowStepPresentation(4, "Preview import", HomeWorkflowStepState.Upcoming, "4", null, "Preview import"),
            5 => new HomeWorkflowStepPresentation(5, "Confirm and import", HomeWorkflowStepState.Upcoming, "5", null, "Confirm and import"),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step."),
        };

    private int GetStepElevation(int step)
        => GetWorkflowStepState(step) switch
        {
            HomeWorkflowStepState.Current => 2,
            HomeWorkflowStepState.Completed => 1,
            HomeWorkflowStepState.Upcoming => 0,
            _ => 0,
        };

    private string GetStepClass(int step)
    {
        return GetWorkflowStepState(step) switch
        {
            HomeWorkflowStepState.Current => "step-card step-card--current pa-4",
            HomeWorkflowStepState.Completed => "step-card step-card--completed pa-4",
            HomeWorkflowStepState.Upcoming => "step-card step-card--upcoming pa-4",
            _ => "step-card step-card--upcoming pa-4",
        };
    }

    private Color GetStepAvatarColour(int step)
    {
        if (IsStepComplete(step))
        {
            return Color.Success;
        }

        return IsStepActive(step) ? Color.Primary : Color.Default;
    }

    private Color GetStepSummaryColour(int step)
        => step == 5 && executionResult?.Errors.Count > 0
            ? Color.Warning
            : Color.Success;

    private string GetStepSummary(int step)
        => step switch
        {
            1 => $"Location: {FormatContainer(selectedContainer)}",
            2 => $"Plan: {FormatPlan(selectedPlan)}",
            3 => $"CSV: {selectedFileName}",
            4 => "Preview generated and ready to execute.",
            5 => executionResult?.Errors.Count > 0
                ? "Execution finished with warnings."
                : "Execution completed successfully.",
            _ => string.Empty,
        };
}
