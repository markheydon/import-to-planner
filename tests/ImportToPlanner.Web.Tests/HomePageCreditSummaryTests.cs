using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCreditSummaryTests
{
    [Fact]
    public async Task HomeExecutionReport_ShowsCreatedCountCreditsUsedAndRemaining()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            ["Task: Alpha Task"],
            [],
            [],
            [],
            new ImportExecutionOutcomeSummary(1, 0, 0, 0, false, false),
            TasksCreatedCount: 1,
            CreditsUsed: 1,
            RemainingCredits: 24);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Tasks created", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Credits used (free monthly)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Remaining credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">1<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(">24<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HomeExecutionReport_WhenNoTasksCreated_ShowsRemainingCredits()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            [],
            ["Plan: Self Test", "Task: Create user stories"],
            [],
            [],
            new ImportExecutionOutcomeSummary(0, 4, 0, 6, false, false),
            TasksCreatedCount: 0,
            CreditsUsed: 0,
            RemainingCredits: 25);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Credits used (free monthly)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Remaining credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">0<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(">25<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HomeExecutionReport_WhenNoTasksCreatedAndBalanceUnavailable_ShowsWarning()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            [],
            ["Plan: Self Test", "Task: Create user stories"],
            [],
            ["Remaining credits could not be loaded for this execution report."],
            new ImportExecutionOutcomeSummary(0, 4, 1, 6, true, false),
            TasksCreatedCount: 0,
            CreditsUsed: 0,
            RemainingCredits: null);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Credits used (free monthly)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">0<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Errors: 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Import finished with errors", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Import complete.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HomeExecutionReport_WhenCreditsExhausted_ShowsExhaustionCopy()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            ["Task: Alpha Task"],
            [],
            [],
            ["Credit exhausted: task 'Task B' was not created because your organisation has no credits remaining."],
            new ImportExecutionOutcomeSummary(1, 0, 1, 0, true, false),
            TasksCreatedCount: 1,
            CreditsUsed: 1,
            RemainingCredits: 0);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Errors: 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Credits used (free monthly)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">0<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Import finished with errors", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Import complete.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HomeExecutionReport_WhenUsageRecordFails_ShowsWarningBannerNotSuccess()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            ["Task: Alpha Task"],
            [],
            [],
            ["Credit usage could not be recorded for task 'Alpha Task'. The import stopped before remaining tasks were created."],
            new ImportExecutionOutcomeSummary(1, 0, 1, 0, false, true),
            TasksCreatedCount: 1,
            CreditsUsed: 0,
            RemainingCredits: 24);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Import finished with errors", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Import complete.", cut.Markup, StringComparison.Ordinal);
    }
}
