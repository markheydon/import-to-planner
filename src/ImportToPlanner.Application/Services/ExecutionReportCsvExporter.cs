using System.Globalization;
using System.Text;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Serialises execution report export rows to a UTF-8 CSV file for download.
/// </summary>
public sealed class ExecutionReportCsvExporter
{
    private static readonly string[] Headers =
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

    /// <summary>
    /// Builds a CSV file from export rows sorted with source rows first (by row number) then manual follow-up rows.
    /// </summary>
    /// <param name="rows">The export projection rows.</param>
    /// <param name="generatedAtUtc">The UTC timestamp used in the file name.</param>
    /// <returns>The in-memory CSV file payload.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Registered in DI for replaceability and testing.")]
    public ExecutionReportCsvFile Export(IReadOnlyList<ExecutionReportCsvRow> rows, DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var orderedRows = rows
            .OrderBy(row => row.RecordType == ExecutionReportCsvRecordType.SourceRow ? 0 : 1)
            .ThenBy(row => row.SourceRowNumber ?? int.MaxValue)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Headers.Select(EscapeField)));

        foreach (var row in orderedRows)
        {
            var cells = new[]
            {
                EscapeField(MapRecordType(row.RecordType)),
                EscapeField(row.SourceRowNumber?.ToString(CultureInfo.InvariantCulture)),
                EscapeField(row.TaskName),
                EscapeField(row.Outcome),
                EscapeField(row.TaskIdentifier),
                EscapeField(row.ReasonOrDetails),
                EscapeField(row.ActionType),
                EscapeField(row.GoalName),
            };

            builder.AppendLine(string.Join(',', cells));
        }

        var fileName = FormattableString.Invariant(
            $"import-execution-report-{generatedAtUtc.UtcDateTime:yyyyMMdd-HHmmss}Z.csv");

        var content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(builder.ToString());
        return new ExecutionReportCsvFile(fileName, content, "text/csv");
    }

    private static string MapRecordType(ExecutionReportCsvRecordType recordType)
        => recordType switch
        {
            ExecutionReportCsvRecordType.SourceRow => "Source row",
            ExecutionReportCsvRecordType.ManualFollowUp => "Manual follow-up",
            _ => throw new ArgumentOutOfRangeException(nameof(recordType), recordType, "Unknown record type."),
        };

    internal static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var requiresQuotes = value.Contains(',')
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!requiresQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
