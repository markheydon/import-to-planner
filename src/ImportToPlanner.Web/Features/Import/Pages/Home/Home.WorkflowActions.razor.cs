using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Features.Import.Workflows;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private async Task RefreshContainersAsync()
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        try
        {
            await WorkflowCoordinator.LoadContainersAsync(WorkflowState, CancellationToken.None);

            SetStatus("Location list refreshed.", WorkflowStatusLevel.Success);
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            HandleUserSafeFailure(ex, "workflow.refresh_containers", WorkflowStatusLevel.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task RefreshPlansAsync()
    {
        if (isBusy || selectedContainer is null)
        {
            return;
        }

        isBusy = true;
        try
        {
            await WorkflowCoordinator.LoadPlansAsync(WorkflowState, CancellationToken.None);
            SetStatus("Plan list refreshed.", WorkflowStatusLevel.Success);
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            HandleUserSafeFailure(ex, "workflow.refresh_plans", WorkflowStatusLevel.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task OnFileChangedAsync(IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        ResetFlowState();

        selectedFileName = file.Name;

        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var reader = new StreamReader(stream);
        csvContent = await reader.ReadToEndAsync();

        SetStatus("CSV file loaded. Click Preview import.", WorkflowStatusLevel.Info);
    }

    private async Task ClearSelectedCsv()
    {
        if (isBusy || !hasSelectedCsv)
        {
            return;
        }

        if (csvFileUpload is not null)
        {
            await csvFileUpload.ClearAsync();
        }

        csvContent = string.Empty;
        selectedFileName = WorkflowCoordinationState.NoFileSelectedText;
        ResetFlowState();
        SetStatus("CSV selection cleared.", WorkflowStatusLevel.Info);
    }

    private async Task OnSelectedContainerChangedAsync(PlannerContainer? container)
    {
        if (selectedContainer?.Id == container?.Id)
        {
            return;
        }

        var hadPreview = preview is not null;
        selectedContainer = container;
        selectedPlan = null;
        plans.Clear();
        parseErrors.Clear();
        ResetExecutionState();
        MarkPreviewAsStale("Preview is stale because your selected location changed. Generate a fresh preview before import.");

        if (selectedContainer is null)
        {
            if (!hadPreview)
            {
                SetStatus("Select a location to continue.", WorkflowStatusLevel.Info);
            }

            return;
        }

        isBusy = true;
        try
        {
            await WorkflowCoordinator.LoadPlansAsync(WorkflowState, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            HandleUserSafeFailure(ex, "workflow.select_container.load_plans", WorkflowStatusLevel.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private Task OnSelectedPlanChangedAsync(PlannerPlan? plan)
    {
        if (selectedPlan?.Id == plan?.Id)
        {
            return Task.CompletedTask;
        }

        var hadPreview = preview is not null;
        selectedPlan = plan;
        parseErrors.Clear();
        ResetExecutionState();
        MarkPreviewAsStale("Preview is stale because your selected plan changed. Generate a fresh preview before import.");
        if (!hadPreview)
        {
            SetStatus(selectedPlan is null ? "Select a plan to continue." : null, WorkflowStatusLevel.Info);
        }

        return Task.CompletedTask;
    }

    private Task OnIgnoreExtraColumnsChanged(bool value)
    {
        if (ignoreExtraColumns == value)
        {
            return Task.CompletedTask;
        }

        ignoreExtraColumns = value;
        parseErrors.Clear();
        ResetExecutionState();
        MarkPreviewAsStale("Preview is stale because CSV options changed. Generate a fresh preview before import.");
        return Task.CompletedTask;
    }

    private void ResetFlowState()
    {
        parseErrors.Clear();
        ResetPreviewState();
        ResetExecutionState();
        SetStatus(null, WorkflowStatusLevel.Info);
    }

    private void ResetPreviewState()
    {
        WorkflowState.PlanningViewModel = null;
        currentRequest = null;
        isPreviewStale = false;
    }

    private void ResetExecutionState()
    {
        WorkflowState.ExecutionReport = null;
    }

    private void InvalidatePreview(string message)
    {
        parseErrors.Clear();
        ResetPreviewState();
        ResetExecutionState();
        SetStatus(message, WorkflowStatusLevel.Warning);
    }

    private void MarkPreviewAsStale(string message)
    {
        if (preview is null || currentRequest is null)
        {
            return;
        }

        isPreviewStale = true;
        SetStatus(message, WorkflowStatusLevel.Warning);
    }

    private async Task BuildPreviewAsync()
    {
        if (isBusy)
        {
            return;
        }

        parseErrors.Clear();
        ResetPreviewState();
        ResetExecutionState();
        SetStatus(null, WorkflowStatusLevel.Info);

        isBusy = true;
        try
        {
            await WorkflowCoordinator.BuildPreviewAsync(WorkflowState, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            var failurePresentation = HandleUserSafeFailure(ex, "workflow.build_preview", WorkflowStatusLevel.Error);
            parseErrors.Add(new ImportValidationError(0, "System", failurePresentation.UserMessage));
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task ExecuteAsync()
    {
        if (!canExecute)
        {
            return;
        }

        isBusy = true;
        SetStatus(null, WorkflowStatusLevel.Info);
        try
        {
            await WorkflowCoordinator.ExecuteAsync(WorkflowState, CancellationToken.None);
        }
        catch (ImportToPlanner.Application.Exceptions.StaleImportPreviewException ex)
        {
            isPreviewStale = true;
            HandleUserSafeFailure(ex, "workflow.execute.stale_preview", WorkflowStatusLevel.Warning);
        }
        catch (InvalidOperationException ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            isPreviewStale = false;
            HandleUserSafeFailure(ex, "workflow.execute.invalid_operation", WorkflowStatusLevel.Warning);
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthenticationChallengeAsync(ex))
            {
                return;
            }

            HandleUserSafeFailure(ex, "workflow.execute", WorkflowStatusLevel.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private static string FormatContainer(PlannerContainer? container)
        => container is null ? string.Empty : $"{container.DisplayName} ({container.Type})";

    private static string FormatPlan(PlannerPlan? plan)
        => plan?.Title ?? string.Empty;

    private Task<IEnumerable<PlannerContainer>> SearchContainers(string value, CancellationToken cancellationToken)
        => SearchItems(
            value,
            containers,
            static (container, term) => container.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                        || container.Type.ToString().Contains(term, StringComparison.OrdinalIgnoreCase),
            cancellationToken);

    private Task<IEnumerable<PlannerPlan>> SearchPlans(string value, CancellationToken cancellationToken)
        => SearchItems(
            value,
            plans,
            static (plan, term) => plan.Title.Contains(term, StringComparison.OrdinalIgnoreCase),
            cancellationToken);

    private static Task<IEnumerable<TItem>> SearchItems<TItem>(
        string value,
        IEnumerable<TItem> source,
        Func<TItem, string, bool> match,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(source);
        }

        return Task.FromResult(source.Where(item => match(item, value)));
    }

    private MudFileUpload<IBrowserFile>? csvFileUpload;

    private async Task OpenCsvFilePickerAsync()
    {
        if (csvFileUpload is not null)
        {
            await csvFileUpload.OpenFilePickerAsync();
        }
    }
}
