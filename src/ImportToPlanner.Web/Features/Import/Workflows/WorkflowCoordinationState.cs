using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.CsvImport.Models;
using ImportToPlanner.Application.ImportPlanning.Models;
using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Features.Import.Presenters;

namespace ImportToPlanner.Web.Features.Import.Workflows;

/// <summary>
/// Represents neutral status levels for workflow state.
/// </summary>
public enum WorkflowStatusLevel
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>
/// Represents UI workflow state for the import page.
/// </summary>
public sealed class WorkflowCoordinationState
{
    public const string NoFileSelectedText = "No file selected";

    public List<PlannerContainer> Containers { get; } = [];

    public List<PlannerPlan> Plans { get; } = [];

    public List<ImportValidationError> ParseErrors { get; } = [];

    public PlannerContainer? SelectedContainer { get; set; }

    public PlannerPlan? SelectedPlan { get; set; }

    public string CsvContent { get; set; } = string.Empty;

    public string SelectedFileName { get; set; } = NoFileSelectedText;

    public string? StatusMessage { get; set; }

    public string? StatusReferenceId { get; set; }

    public WorkflowStatusLevel StatusLevel { get; set; } = WorkflowStatusLevel.Info;

    public bool IsBusy { get; set; }

    public bool IgnoreExtraColumns { get; set; } = true;

    public bool NoGroupsFound { get; set; }

    public bool IsPreviewStale { get; set; }

    public TenantContext? ActiveTenantContext { get; set; }

    public ConsentResolution? ConsentResolution { get; set; }

    public bool IsUnsupportedAccount { get; set; }

    public bool IsAdminConsentRequired { get; set; }

    public bool IsTenantContextMismatch { get; set; }

    public ImportPlanningRequest? CurrentPlanningRequest { get; set; }

    public ImportPlanningViewModel? PlanningViewModel { get; set; }

    public ImportExecutionReportViewModel? ExecutionReport { get; set; }
}
