using System.Text;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Tests;

public sealed class ExecutionReportCsvExporterTests
{
    private static readonly string[] ExpectedHeaders =
    [
        "Record type",
        "Source row number",
        "Task name",
        "Outcome",
        "Task identifier",
        "Reason or details",
        "Action type",
        "Goal name",
    ];

    [Fact]
    public void Export_WithMixedOutcomes_ProducesContractShapedCsv()
    {
        var exporter = new ExecutionReportCsvExporter();
        var generatedAt = new DateTimeOffset(2026, 9, 29, 14, 30, 45, TimeSpan.Zero);
        var rows = new List<ExecutionReportCsvRow>
        {
            new(
                ExecutionReportCsvRecordType.SourceRow,
                2,
                "Alpha, task",
                "Created",
                "task-created-001",
                null,
                null,
                null),
            new(
                ExecutionReportCsvRecordType.SourceRow,
                3,
                "Existing task",
                "Reused or skipped",
                null,
                "Task already exists in the destination plan.",
                null,
                null),
            new(
                ExecutionReportCsvRecordType.SourceRow,
                4,
                "Broken task",
                "Failed",
                null,
                "Planner provider is unavailable.",
                null,
                null),
            new(
                ExecutionReportCsvRecordType.ManualFollowUp,
                null,
                "Alpha, task",
                "Manual follow-up",
                null,
                "Link this task to the goal manually in Planner.",
                "Link Task To Goal",
                "Sprint 1"),
        };

        var file = exporter.Export(rows, generatedAt);

        Assert.Equal("import-execution-report-20260929-143045Z.csv", file.FileName);
        Assert.Equal("text/csv", file.MediaType);
        Assert.StartsWith(ExpectedHeaders[0], Encoding.UTF8.GetString(file.Content), StringComparison.Ordinal);

        var text = Encoding.UTF8.GetString(file.Content);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(string.Join(',', ExpectedHeaders), lines[0]);
        Assert.Contains("\"Alpha, task\"", text, StringComparison.Ordinal);
        Assert.Contains("Created", text, StringComparison.Ordinal);
        Assert.Contains("Reused or skipped", text, StringComparison.Ordinal);
        Assert.Contains("Failed", text, StringComparison.Ordinal);
        Assert.Contains("Manual follow-up", text, StringComparison.Ordinal);
        Assert.Contains("Link Task To Goal", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WhenReasonContainsLineBreak_QuotesFieldPerRfc4180()
    {
        var exporter = new ExecutionReportCsvExporter();
        var rows = new List<ExecutionReportCsvRow>
        {
            new(
                ExecutionReportCsvRecordType.SourceRow,
                2,
                "Task A",
                "Failed",
                null,
                "First line\nSecond line",
                null,
                null),
        };

        var file = exporter.Export(rows, new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
        var text = Encoding.UTF8.GetString(file.Content);
        Assert.Contains("\"First line\nSecond line\"", text, StringComparison.Ordinal);
    }
}
