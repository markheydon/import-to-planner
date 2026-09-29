namespace ImportToPlanner.Application.Models;

/// <summary>
/// Browser-persisted mapping for a normalised header layout.
/// </summary>
public sealed record SavedLayoutMapping
{
    public required string LayoutSignature { get; init; }

    /// <summary>
    /// Maps source header label to import field id, or <see langword="null"/> when the user chose not to import that column.
    /// </summary>
    public required IReadOnlyDictionary<string, string?> Assignments { get; init; }

    public required DateTimeOffset UpdatedUtc { get; init; }
}
