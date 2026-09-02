using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCreditWorkflowTests
{
    [Fact]
    public async Task BuildPreviewAsync_WhenCsvValidationFails_DoesNotInvokeEnsureBalance()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();

        state.SelectedContainer = ctx.Gateway.Containers[0];
        state.SelectedPlan = ctx.Gateway.Plans[0];
        state.CsvContent = string.Empty;

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.Equal(0, ctx.CreditEnsureUseCase.EnsureCallCount);
        Assert.Null(state.CreditBalanceSnapshot);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLedgerUnavailableAtConfirm_FailsClosedWithoutExecutionReport()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 25;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();

        state.SelectedContainer = ctx.Gateway.Containers[0];
        state.SelectedPlan = ctx.Gateway.Plans[0];
        state.CsvContent = "Task Name\nTask A";
        state.ActiveTenantContext = new TenantContext(
            "tenant-001",
            "tenant-key",
            "user-001",
            SupportedAccountType.WorkOrSchool,
            "Contoso");

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        ctx.CreditEnsureUseCase.FailClosed = true;

        await coordinator.ExecuteAsync(state, CancellationToken.None);

        Assert.Null(state.ExecutionReport);
        Assert.Contains("temporarily unavailable", state.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WorkflowStatusLevel.Error, state.StatusLevel);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenLedgerUnavailableAtPreview_ClearsPreviewState()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 25;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();

        state.SelectedContainer = ctx.Gateway.Containers[0];
        state.SelectedPlan = ctx.Gateway.Plans[0];
        state.CsvContent = "Task Name\nTask A";
        state.ActiveTenantContext = new TenantContext(
            "tenant-001",
            "tenant-key",
            "user-001",
            SupportedAccountType.WorkOrSchool,
            "Contoso");

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        ctx.CreditEnsureUseCase.FailClosed = true;

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.Null(state.PlanningViewModel);
        Assert.Null(state.CurrentPlanningRequest);
        Assert.True(state.CreditBalanceSnapshot?.LedgerUnavailable);
        Assert.Contains("temporarily unavailable", state.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLiveRemainingDropsBelowWouldCreate_BlocksExecution()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 25;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();

        state.SelectedContainer = ctx.Gateway.Containers[0];
        state.SelectedPlan = ctx.Gateway.Plans[0];
        state.CsvContent = "Task Name\nTask A";
        state.ActiveTenantContext = new TenantContext(
            "tenant-001",
            "tenant-key",
            "user-001",
            SupportedAccountType.WorkOrSchool,
            "Contoso");

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        ctx.CreditEnsureUseCase.RemainingCredits = 0;

        await coordinator.ExecuteAsync(state, CancellationToken.None);

        Assert.Null(state.ExecutionReport);
        Assert.Contains("would create", state.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
