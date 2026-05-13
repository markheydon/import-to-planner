using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Presenters;
using MudBlazor;

namespace ImportToPlanner.Web.Workflows;

/// <summary>
/// Represents UI workflow state for the import page.
/// </summary>
public sealed class WorkflowCoordinationState
{
    public List<PlannerContainer> Containers { get; } = [];

    public List<PlannerPlan> Plans { get; } = [];

    public List<ImportValidationError> ParseErrors { get; } = [];

    public PlannerContainer? SelectedContainer { get; set; }

    public PlannerPlan? SelectedPlan { get; set; }

    public string CsvContent { get; set; } = string.Empty;

    public string SelectedFileName { get; set; } = "No file selected";

    public string? StatusMessage { get; set; }

    public Severity StatusSeverity { get; set; } = Severity.Info;

    public bool IsBusy { get; set; }

    public bool IgnoreExtraColumns { get; set; } = true;

    public bool IsUsingGraphGateway { get; set; }

    public bool NoGroupsFound { get; set; }

    public bool IsPreviewStale { get; set; }

    public ImportPlanningRequest? CurrentPlanningRequest { get; set; }

    public ImportPlanningViewModel? PlanningViewModel { get; set; }

    public ImportExecutionReportViewModel? ExecutionReport { get; set; }
}
