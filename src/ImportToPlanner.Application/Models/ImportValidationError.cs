namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a validation issue found in a CSV payload.
/// </summary>
/// <param name="RowNumber">The row number where the issue occurred, or 0 for file-level errors.</param>
/// <param name="Field">The field name associated with the issue.</param>
/// <param name="Message">The validation message.</param>
public sealed record ImportValidationError(int RowNumber, string Field, string Message);
