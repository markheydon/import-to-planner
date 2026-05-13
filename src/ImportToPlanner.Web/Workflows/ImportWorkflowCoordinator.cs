using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Presenters;
using MudBlazor;

namespace ImportToPlanner.Web.Workflows;

/// <summary>
/// Coordinates import workflow actions for the home page.
/// </summary>
public sealed class ImportWorkflowCoordinator(
    ICsvImportParser csvImportParser,
    IPlannerGateway plannerGateway,
    IImportPlanningUseCase planningUseCase,
    IImportExecutionUseCase executionUseCase,
    ImportPlanningPresenter planningPresenter,
    ImportExecutionPresenter executionPresenter)
{
    public async Task LoadContainersAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.Containers.Clear();
        var containers = await plannerGateway.GetAvailableContainersAsync(cancellationToken);
        state.Containers.AddRange(containers);
        state.NoGroupsFound = state.Containers.All(container => container.Type != ContainerType.Group);

        if (state.SelectedContainer is not null)
        {
            state.SelectedContainer = state.Containers.FirstOrDefault(container =>
                string.Equals(container.Id, state.SelectedContainer.Id, StringComparison.OrdinalIgnoreCase));
        }

        if (state.SelectedContainer is null)
        {
            state.SelectedPlan = null;
            state.Plans.Clear();
            return;
        }

        await LoadPlansAsync(state, cancellationToken);
    }

    public async Task LoadPlansAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.Plans.Clear();
        if (state.SelectedContainer is null)
        {
            state.SelectedPlan = null;
            return;
        }

        var plans = await plannerGateway.GetPlansAsync(state.SelectedContainer.Id, state.SelectedContainer.Type, cancellationToken);
        state.Plans.AddRange(plans.OrderBy(plan => plan.Title, StringComparer.OrdinalIgnoreCase));

        if (state.SelectedPlan is not null)
        {
            state.SelectedPlan = state.Plans.FirstOrDefault(plan =>
                string.Equals(plan.Id, state.SelectedPlan.Id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task BuildPreviewAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.ParseErrors.Clear();
        state.PlanningViewModel = null;
        state.ExecutionReport = null;
        state.CurrentPlanningRequest = null;
        state.IsPreviewStale = false;

        if (state.SelectedContainer is null)
        {
            state.ParseErrors.Add(new ImportValidationError(0, "Container", "A container must be selected."));
            return;
        }

        if (state.SelectedPlan is null)
        {
            state.ParseErrors.Add(new ImportValidationError(0, "Plan", "Select an existing plan."));
            return;
        }

        if (string.IsNullOrWhiteSpace(state.CsvContent))
        {
            state.ParseErrors.Add(new ImportValidationError(0, "File", "Upload a CSV file first."));
            return;
        }

        var parseResult = await csvImportParser.ParseAsync(state.CsvContent, cancellationToken, state.IgnoreExtraColumns);
        state.ParseErrors.AddRange(parseResult.ValidationErrors);

        if (parseResult.HasErrors)
        {
            state.StatusMessage = "Validation failed. Fix the reported issues and retry.";
            state.StatusSeverity = Severity.Error;
            return;
        }

        var request = new ImportPlanningRequest(
            state.SelectedContainer.Id,
            state.SelectedContainer.Type,
            state.SelectedPlan.Id,
            state.SelectedPlan.Title,
            parseResult.Rows);

        await planningUseCase.HandleAsync(request, planningPresenter, cancellationToken);
        state.CurrentPlanningRequest = request;
        state.PlanningViewModel = planningPresenter.ViewModel;
        state.StatusMessage = "Preview generated. Review actions, then confirm execution.";
        state.StatusSeverity = Severity.Success;
    }

    public async Task ExecuteAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.CurrentPlanningRequest is null || state.PlanningViewModel is null)
        {
            return;
        }

        var request = new ImportExecutionRequest(state.CurrentPlanningRequest, state.PlanningViewModel.Preview);
        await executionUseCase.HandleAsync(request, executionPresenter, cancellationToken);
        state.ExecutionReport = executionPresenter.ViewModel;
        state.IsPreviewStale = false;
        state.StatusMessage = state.ExecutionReport is null || state.ExecutionReport.Errors.Count == 0
            ? "Execution completed successfully."
            : "Execution completed with errors.";
        state.StatusSeverity = state.ExecutionReport is null || state.ExecutionReport.Errors.Count == 0
            ? Severity.Success
            : Severity.Warning;
    }

    public static bool TryGetFailure(Exception exception, out PlannerOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is PlannerOperationException plannerException)
        {
            failure = plannerException.Failure;
            return true;
        }

        if (exception is InvalidOperationException invalidOperationException &&
            invalidOperationException.Message.Contains(
                "authenticated user context is required to acquire a graph access token",
                StringComparison.OrdinalIgnoreCase))
        {
            failure = new PlannerOperationFailure(
                PlannerFailureCategory.Authentication,
                PlannerFailureTarget.Workflow,
                null,
                invalidOperationException.Message,
                false,
                "Authentication");
            return true;
        }

        failure = new PlannerOperationFailure(
            PlannerFailureCategory.Unknown,
            PlannerFailureTarget.Workflow,
            null,
            exception.Message,
            false,
            "Unhandled");
        return false;
    }
}
