using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private sealed record ImportContextSummary(
        string LocationLabel,
        string PlanLabel,
        string CsvFileName,
        string PreviewStatus,
        string ExecutionStatus,
        string? PlannerUrl);

    private bool IsCurrentSelectionInSyncWithRequest()
        => selectedContainer is not null
           && selectedPlan is not null
           && currentRequest is not null
           && string.Equals(selectedContainer.Id, currentRequest.ContainerId, StringComparison.Ordinal)
           && string.Equals(selectedPlan.Id, currentRequest.PlanId, StringComparison.Ordinal);

    private ImportContextSummary GetImportContextSummary()
    {
        var locationLabel = selectedContainer is null ? "Not yet chosen" : FormatContainer(selectedContainer);
        var planLabel = selectedPlan is null ? "Not yet chosen" : FormatPlan(selectedPlan);
        var csvFileName = hasSelectedCsv ? selectedFileName : "Not yet chosen";

        var previewStatus = parseErrors.Count > 0
            ? "Validation errors"
            : isPreviewStale
                ? "Stale — regenerate preview"
                : preview is not null
                    ? "Ready"
                    : "Not generated";

        var executionStatus = executionResult switch
        {
            null => "Not run",
            { Errors.Count: > 0 } => "Completed with warnings",
            _ => "Succeeded",
        };

        string? plannerUrl = null;
        if (preview?.Preview.PlanId is { } previewPlanId)
        {
            plannerUrl = $"https://planner.cloud.microsoft/webui/plan/{previewPlanId}";
        }
        else if (executionResult?.PlanId is { } executionPlanId)
        {
            plannerUrl = $"https://planner.cloud.microsoft/webui/plan/{executionPlanId}";
        }

        return new ImportContextSummary(
            locationLabel,
            planLabel,
            csvFileName,
            previewStatus,
            executionStatus,
            plannerUrl);
    }

    private Color GetSummaryPreviewChipColour()
        => parseErrors.Count > 0 || isPreviewStale
            ? Color.Warning
            : preview is not null
                ? Color.Success
                : Color.Default;

    private Color GetSummaryExecutionChipColour()
        => executionResult switch
        {
            null => Color.Default,
            { Errors.Count: > 0 } => Color.Warning,
            _ => Color.Success,
        };
}
