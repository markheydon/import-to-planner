namespace ImportToPlanner.Application.Models;

/// <summary>
/// Suggested column mapping for an uploaded CSV before user confirmation.
/// </summary>
public sealed class ColumnMappingProposal
{
    public required string LayoutSignature { get; init; }

    public required IReadOnlyList<string> SourceHeaders { get; init; }

    /// <summary>
    /// Maps import field id to a source header label, or <see langword="null"/> when not mapped.
    /// </summary>
    public required IReadOnlyDictionary<string, string?> SuggestedAssignments { get; init; }

    public required ColumnMappingProposalStatus Status { get; init; }

    public required IReadOnlyList<ColumnMappingConflict> Conflicts { get; init; }
}
