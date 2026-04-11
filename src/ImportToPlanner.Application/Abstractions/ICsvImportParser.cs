using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Parses and validates CSV uploads for Planner import.
/// </summary>
public interface ICsvImportParser
{
    /// <summary>
    /// Parses CSV content and returns normalized rows plus validation errors.
    /// </summary>
    /// <param name="csvContent">The raw CSV file content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="ignoreExtraColumns">If true, silently ignore columns not in the supported set. If false, report them as validation errors.</param>
    /// <returns>A parse result containing rows and validation issues.</returns>
    Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false);
}
