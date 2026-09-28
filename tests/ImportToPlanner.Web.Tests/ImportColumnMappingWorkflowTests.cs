using ImportToPlanner.Application.Import;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Web.Tests;

public sealed class ImportColumnMappingWorkflowTests
{
    [Fact]
    public async Task ProcessCsvUploadAsync_WithAliasHeaders_AutoConfirmsMappingForPreview()
    {
        await using var ctx = CreateContextWithLayoutStore();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = CreateReadyState(ctx, "Title,Notes\nTask A,Body");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);

        Assert.True(state.IsColumnMappingConfirmed);
        Assert.Equal(ColumnMappingProposalStatus.NeedsConfirmation, state.ColumnMappingProposal!.Status);
        Assert.NotNull(state.ConfirmedColumnMapping);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenNeedsTaskName_BlocksUntilMappingConfirmed()
    {
        await using var ctx = CreateContextWithLayoutStore();
        ctx.CsvParser.UseRealParser = true;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = CreateReadyState(ctx, "Work item\nDo something");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);

        Assert.False(state.IsColumnMappingConfirmed);
        Assert.Equal(ColumnMappingProposalStatus.NeedsTaskName, state.ColumnMappingProposal!.Status);

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.Contains(state.ParseErrors, error => error.Field == "Mapping");

        var assignments = state.ColumnMappingProposal.SuggestedAssignments
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        assignments[ImportColumnFieldIds.TaskName] = "Work item";

        await coordinator.ConfirmColumnMappingAsync(state, assignments, CancellationToken.None);

        Assert.True(state.IsColumnMappingConfirmed);
        state.ParseErrors.Clear();
        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.DoesNotContain(state.ParseErrors, error => error.Field == "Mapping");
    }

    [Fact]
    public async Task ProcessCsvUploadAsync_PersistedLayout_ContainsHeadersAndFieldIdsOnly()
    {
        await using var ctx = CreateContextWithLayoutStore();
        var layoutStore = (ImportColumnMappingLayoutStoreStub)ctx.Services.GetRequiredService<IImportColumnMappingLayoutStore>();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        const string secretTaskTitle = "Secret task title from row";
        const string secretBody = "Secret body text from row";
        var state = CreateReadyState(ctx, $"Title,Notes\n{secretTaskTitle},{secretBody}");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);

        Assert.Single(layoutStore.Layouts);
        var saved = layoutStore.Layouts.Values.Single();
        var serialised = string.Join(
            ' ',
            saved.Assignments.Select(pair => $"{pair.Key}={pair.Value ?? "skip"}"));

        Assert.DoesNotContain(secretTaskTitle, serialised, StringComparison.Ordinal);
        Assert.DoesNotContain(secretBody, serialised, StringComparison.Ordinal);
        Assert.Equal(2, saved.Assignments.Count);
        Assert.True(saved.Assignments.ContainsKey("Title"));
        Assert.True(saved.Assignments.ContainsKey("Notes"));
        Assert.Equal(ImportColumnFieldIds.TaskName, saved.Assignments["Title"]);
        Assert.Equal(ImportColumnFieldIds.Description, saved.Assignments["Notes"]);
    }

    [Fact]
    public async Task ProcessCsvUploadAsync_WithCompetingTaskNameHeaders_OpensEditorAndBlocksPreview()
    {
        await using var ctx = CreateContextWithLayoutStore();
        ctx.CsvParser.UseRealParser = true;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = CreateReadyState(ctx, "Title,Task\nDo something,Also a task");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);

        Assert.True(state.ShowMappingEditor);
        Assert.False(state.IsColumnMappingConfirmed);
        Assert.Equal(ColumnMappingProposalStatus.Conflict, state.ColumnMappingProposal!.Status);

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.Contains(state.ParseErrors, error => error.Field == "Mapping");
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenMappingEditorOpen_BlocksUntilMappingConfirmed()
    {
        await using var ctx = CreateContextWithLayoutStore();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = CreateReadyState(ctx, "Title,Notes\nTask A,Body");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);
        Assert.True(state.IsColumnMappingConfirmed);

        state.ShowMappingEditor = true;
        state.IsColumnMappingConfirmed = false;

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        Assert.Contains(state.ParseErrors, error => error.Field == "Mapping");
    }

    [Fact]
    public async Task ProcessCsvUploadAsync_WithSavedLayout_ReappliesMappingWithoutManualConfirm()
    {
        await using var ctx = CreateContextWithLayoutStore();
        var layoutStore = (ImportColumnMappingLayoutStoreStub)ctx.Services.GetRequiredService<IImportColumnMappingLayoutStore>();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = CreateReadyState(ctx, "Title,Notes\nFirst,Body");

        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);
        Assert.True(state.IsColumnMappingConfirmed);

        state.CsvContent = "Title,Notes\nSecond,Other";
        await coordinator.ProcessCsvUploadAsync(state, CancellationToken.None);

        Assert.True(state.IsColumnMappingConfirmed);
        Assert.Equal("Title", state.ConfirmedColumnMapping!.Assignments[ImportColumnFieldIds.TaskName]);
        Assert.NotEmpty(layoutStore.Layouts);
    }

    private static HomePageTestContext CreateContextWithLayoutStore()
    {
        var layoutStore = new ImportColumnMappingLayoutStoreStub();
        return new HomePageTestContext(layoutStore: layoutStore);
    }

    private static WorkflowCoordinationState CreateReadyState(HomePageTestContext ctx, string csvContent)
    {
        return new WorkflowCoordinationState
        {
            SelectedContainer = ctx.Gateway.Containers[0],
            SelectedPlan = ctx.Gateway.Plans[0],
            CsvContent = csvContent,
            SelectedFileName = "import.csv",
        };
    }
}
