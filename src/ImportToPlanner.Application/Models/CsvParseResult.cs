namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents the output of CSV parsing and validation.
/// </summary>
/// <param name="Rows">The valid rows that can be processed.</param>
/// <param name="ValidationErrors">The validation issues found.</param>
public sealed record CsvParseResult(
    IReadOnlyList<CsvTaskRow> Rows,
    IReadOnlyList<ImportValidationError> ValidationErrors)
{
    /// <summary>
    /// Gets a value indicating whether parsing produced at least one validation error.
    /// </summary>
    public bool HasErrors => ValidationErrors.Count > 0;
}
