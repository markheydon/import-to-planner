namespace ImportToPlanner.Application.Models;

/// <summary>
/// Two or more source headers competing for the same import field.
/// </summary>
public sealed record ColumnMappingConflict(string FieldId, IReadOnlyList<string> SourceHeaders);
