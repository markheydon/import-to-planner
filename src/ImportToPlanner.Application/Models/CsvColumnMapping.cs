namespace ImportToPlanner.Application.Models;

/// <summary>
/// User-confirmed mapping from import fields to source CSV headers for one upload session.
/// </summary>
public sealed class CsvColumnMapping
{
    public required string LayoutSignature { get; init; }

    /// <summary>
    /// Maps import field id to the source header label from the file (trimmed display text).
    /// </summary>
    public required IReadOnlyDictionary<string, string> Assignments { get; init; }
}
