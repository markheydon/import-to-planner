using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Features.Import.Presenters;

namespace ImportToPlanner.Web.Features.Import.Workflows;

/// <summary>
/// Coordinates import workflow actions for the home page.
/// </summary>
public sealed class ImportWorkflowCoordinator(
    ICsvImportParser csvImportParser,
    IPlannerGateway plannerGateway,
    IImportPlanningUseCase planningUseCase,
    IImportExecutionUseCase executionUseCase,
    ICurrentTenantContextAccessor currentTenantContextAccessor,
    ImportPlanningPresenter planningPresenter,
    ImportExecutionPresenter executionPresenter,
    IServiceProvider serviceProvider)
{
    private IEnsureCurrentCreditBalanceUseCase? EnsureCreditBalanceUseCase =>
        serviceProvider.GetService<IEnsureCurrentCreditBalanceUseCase>();

    public async Task LoadContainersAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        ResetGuidanceFlags(state);

        try
        {
            ResolveTenantContext(state);

            var previousContainerId = state.SelectedContainer?.Id;

            state.Containers.Clear();
            var containers = await plannerGateway.GetAvailableContainersAsync(cancellationToken);
            state.Containers.AddRange(containers);
            state.NoGroupsFound = state.Containers.All(container => container.Type != ContainerType.Group);

            if (state.SelectedContainer is not null)
            {
                state.SelectedContainer = state.Containers.FirstOrDefault(container =>
                    string.Equals(container.Id, state.SelectedContainer.Id, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(previousContainerId, state.SelectedContainer?.Id, StringComparison.OrdinalIgnoreCase))
            {
                InvalidatePreviewAndExecutionState(state);
            }

            if (state.SelectedContainer is null)
            {
                state.SelectedPlan = null;
                state.Plans.Clear();
                return;
            }

            await LoadPlansAsync(state, cancellationToken);
        }
        catch (Exception exception)
        {
            ApplyFailureSignals(state, exception);
            throw;
        }
    }

    public async Task LoadPlansAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        ResetGuidanceFlags(state);

        try
        {
            ResolveTenantContext(state);

            var previousPlanId = state.SelectedPlan?.Id;

            state.Plans.Clear();
            if (state.SelectedContainer is null)
            {
                state.SelectedPlan = null;

                if (!string.Equals(previousPlanId, state.SelectedPlan?.Id, StringComparison.OrdinalIgnoreCase))
                {
                    InvalidatePreviewAndExecutionState(state);
                }

                return;
            }

            var plans = await plannerGateway.GetPlansAsync(state.SelectedContainer.Id, state.SelectedContainer.Type, cancellationToken);
            state.Plans.AddRange(plans.OrderBy(plan => plan.Title, StringComparer.OrdinalIgnoreCase));

            if (state.SelectedPlan is not null)
            {
                state.SelectedPlan = state.Plans.FirstOrDefault(plan =>
                    string.Equals(plan.Id, state.SelectedPlan.Id, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(previousPlanId, state.SelectedPlan?.Id, StringComparison.OrdinalIgnoreCase))
            {
                InvalidatePreviewAndExecutionState(state);
            }
        }
        catch (Exception exception)
        {
            ApplyFailureSignals(state, exception);
            throw;
        }
    }

    public async Task BuildPreviewAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        ResetGuidanceFlags(state);

        try
        {
            ResolveTenantContext(state);

            state.ParseErrors.Clear();
            state.PlanningViewModel = null;
            state.ExecutionReport = null;
            state.CurrentPlanningRequest = null;
            state.CreditBalanceSnapshot = null;
            state.IsPreviewStale = false;

            if (state.SelectedContainer is null)
            {
                state.ParseErrors.Add(new ImportValidationError(0, "Location", "A location must be selected."));
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
                state.StatusReferenceId = null;
                state.StatusLevel = WorkflowStatusLevel.Error;
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
            state.CreditBalanceSnapshot = await BuildCreditBalanceSnapshotAsync(
                state,
                planningPresenter.ViewModel!.Preview,
                EnsureBalanceReason.Preview,
                cancellationToken).ConfigureAwait(false);
            if (state.CreditBalanceSnapshot?.LedgerUnavailable == true)
            {
                state.StatusMessage = ImportCreditPreviewPresenter.BuildLedgerUnavailableMessage();
                state.StatusReferenceId = state.CreditBalanceSnapshot.LedgerFailureCode;
                state.StatusLevel = WorkflowStatusLevel.Error;
                return;
            }

            state.StatusMessage = "Preview generated. Review actions, then confirm execution.";
            state.StatusReferenceId = null;
            state.StatusLevel = WorkflowStatusLevel.Success;
        }
        catch (Exception exception)
        {
            ApplyFailureSignals(state, exception);
            throw;
        }
    }

    public async Task RefreshCreditBalanceSnapshotForConfirmAsync(
        WorkflowCoordinationState state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.PlanningViewModel?.Preview is null)
        {
            return;
        }

        ResolveTenantContext(state);
        state.CreditBalanceSnapshot = await BuildCreditBalanceSnapshotAsync(
            state,
            state.PlanningViewModel.Preview,
            EnsureBalanceReason.Confirm,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(WorkflowCoordinationState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        ResetGuidanceFlags(state);

        try
        {
            ResolveTenantContext(state);

            if (state.CurrentPlanningRequest is null || state.PlanningViewModel is null)
            {
                return;
            }

            var preview = state.PlanningViewModel.Preview;
            var creditSnapshot = await BuildCreditBalanceSnapshotAsync(
                state,
                preview,
                EnsureBalanceReason.Confirm,
                cancellationToken).ConfigureAwait(false);
            state.CreditBalanceSnapshot = creditSnapshot;
            if (creditSnapshot?.LedgerUnavailable == true)
            {
                state.StatusMessage = ImportCreditPreviewPresenter.BuildLedgerUnavailableMessage();
                state.StatusReferenceId = creditSnapshot.LedgerFailureCode;
                state.StatusLevel = WorkflowStatusLevel.Error;
                return;
            }

            if (creditSnapshot?.InsufficientCredits == true)
            {
                state.StatusMessage = ImportCreditPreviewPresenter.BuildInsufficientCreditsWarning(
                    creditSnapshot.WouldCreateCount,
                    creditSnapshot.RemainingCredits ?? 0,
                    creditSnapshot.Shortfall);
                state.StatusLevel = WorkflowStatusLevel.Warning;
                return;
            }

            ImportExecutionMeteringContext? metering = null;
            if (state.ActiveTenantContext is not null && EnsureCreditBalanceUseCase is not null)
            {
                metering = new ImportExecutionMeteringContext(
                    state.ActiveTenantContext.TenantId,
                    state.ActiveTenantContext.UserObjectId);
            }

            var request = new ImportExecutionRequest(state.CurrentPlanningRequest, preview, metering);
            await executionUseCase.HandleAsync(request, executionPresenter, cancellationToken);
            state.ExecutionReport = executionPresenter.ViewModel;
            state.IsPreviewStale = false;
            state.StatusMessage = state.ExecutionReport is null || state.ExecutionReport.Errors.Count == 0
                ? "Execution completed successfully."
                : "Execution completed with errors.";
            state.StatusReferenceId = null;
            state.StatusLevel = state.ExecutionReport is null || state.ExecutionReport.Errors.Count == 0
                ? WorkflowStatusLevel.Success
                : WorkflowStatusLevel.Warning;
        }
        catch (Exception exception)
        {
            ApplyFailureSignals(state, exception);
            throw;
        }
    }

    private static void InvalidatePreviewAndExecutionState(WorkflowCoordinationState state)
    {
        var hadPreviewState = state.PlanningViewModel is not null
            || state.CurrentPlanningRequest is not null
            || state.ExecutionReport is not null;

        state.PlanningViewModel = null;
        state.CurrentPlanningRequest = null;
        state.ExecutionReport = null;
        state.CreditBalanceSnapshot = null;
        state.IsPreviewStale = hadPreviewState;
    }

    private async Task<WorkflowCreditBalanceSnapshot?> BuildCreditBalanceSnapshotAsync(
        WorkflowCoordinationState state,
        ImportPlanPreview preview,
        EnsureBalanceReason reason,
        CancellationToken cancellationToken)
    {
        if (EnsureCreditBalanceUseCase is null || state.ActiveTenantContext is null)
        {
            return null;
        }

        var wouldCreateCount = ImportCreditPreviewPresenter.CountWouldCreateTasks(preview);
        var ensureOutcome = await EnsureCreditBalanceUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                state.ActiveTenantContext.TenantId,
                state.ActiveTenantContext.UserObjectId,
                DateTimeOffset.UtcNow,
                reason),
            cancellationToken).ConfigureAwait(false);

        if (ensureOutcome is EnsureCurrentCreditBalanceOutcome.Failed failure)
        {
            return new WorkflowCreditBalanceSnapshot
            {
                WouldCreateCount = wouldCreateCount,
                LedgerUnavailable = true,
                LedgerFailureCode = failure.Failure.FailureCode,
            };
        }

        var balance = ((EnsureCurrentCreditBalanceOutcome.Succeeded)ensureOutcome).Result;
        var shortfall = Math.Max(0, wouldCreateCount - balance.RemainingCredits);
        return new WorkflowCreditBalanceSnapshot
        {
            WouldCreateCount = wouldCreateCount,
            RemainingCredits = balance.RemainingCredits,
            Shortfall = shortfall,
            InsufficientCredits = wouldCreateCount > balance.RemainingCredits,
        };
    }

    private void ResolveTenantContext(WorkflowCoordinationState state)
    {
        var resolvedContext = currentTenantContextAccessor.GetRequiredContext();

        if (state.ActiveTenantContext is not null
            && !string.Equals(state.ActiveTenantContext.TenantId, resolvedContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            state.IsTenantContextMismatch = true;
            InvalidatePreviewAndExecutionState(state);
        }

        state.ActiveTenantContext = resolvedContext;
    }

    private static void ResetGuidanceFlags(WorkflowCoordinationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.IsUnsupportedAccount = false;
        state.IsAdminConsentRequired = false;
    }

    private static void ApplyFailureSignals(WorkflowCoordinationState state, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(exception);

        var mapping = PlannerFailureMessageMapper.FromException(exception, state.ConsentResolution);
        state.IsUnsupportedAccount = mapping.IsUnsupportedAccount;
        state.IsAdminConsentRequired = mapping.IsAdminConsentRequired;
    }
}
