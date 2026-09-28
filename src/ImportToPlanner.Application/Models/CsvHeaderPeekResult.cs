namespace ImportToPlanner.Application.Models;

/// <summary>
/// Result of reading the header row from a CSV upload.
/// </summary>
public sealed class CsvHeaderPeekResult
{
    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<ImportValidationError> ValidationErrors { get; init; }

    public bool HasErrors => ValidationErrors.Count > 0;
}
