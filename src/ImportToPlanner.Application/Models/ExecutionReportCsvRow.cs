namespace ImportToPlanner.Application.Models;

/// <summary>
/// Distinguishes source-row outcomes from manual follow-up rows in the execution report CSV.
/// </summary>
public enum ExecutionReportCsvRecordType
{
    /// <summary>An automated per-source-row execution outcome.</summary>
    SourceRow,

    /// <summary>A post-import manual action from the report.</summary>
    ManualFollowUp,
}

/// <summary>
/// One row in the downloaded execution report CSV file.
/// </summary>
/// <param name="RecordType">Whether this row is a source outcome or manual follow-up.</param>
/// <param name="SourceRowNumber">The source row number for source rows; null for manual follow-up.</param>
/// <param name="TaskName">The task name when applicable.</param>
/// <param name="Outcome">The UK English outcome label for the contract.</param>
/// <param name="TaskIdentifier">The Planner task identifier when created.</param>
/// <param name="ReasonOrDetails">User-safe failure, skip, or manual details text.</param>
/// <param name="ActionType">The manual action type label when applicable.</param>
/// <param name="GoalName">The goal name for manual follow-up when shown on screen.</param>
public sealed record ExecutionReportCsvRow(
    ExecutionReportCsvRecordType RecordType,
    int? SourceRowNumber,
    string? TaskName,
    string Outcome,
    string? TaskIdentifier,
    string? ReasonOrDetails,
    string? ActionType,
    string? GoalName);

/// <summary>
/// Transient in-memory CSV payload for browser download.
/// </summary>
/// <param name="FileName">The suggested download file name.</param>
/// <param name="Content">The UTF-8 CSV bytes without a BOM.</param>
/// <param name="MediaType">The MIME type for the download.</param>
public sealed record ExecutionReportCsvFile(string FileName, byte[] Content, string MediaType);
