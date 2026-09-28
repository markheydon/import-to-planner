namespace ImportToPlanner.Application.Models;

/// <summary>
/// Catalog entry describing one importable CSV column.
/// </summary>
public sealed class ImportColumnFieldDefinition
{
    public required string FieldId { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<string> CanonicalHeaders { get; init; }

    public required IReadOnlyList<string> Aliases { get; init; }

    public required bool IsRequired { get; init; }

    public required bool IsEnabled { get; init; }
}
