using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Builds and confirms CSV column mapping proposals.
/// </summary>
public interface ICsvColumnMappingService
{
    /// <summary>
    /// Builds a mapping proposal from raw header labels and optional saved layout memory.
    /// </summary>
    ColumnMappingProposal BuildProposal(IReadOnlyList<string> rawHeaders, SavedLayoutMapping? savedForLayout);

    /// <summary>
    /// Converts a proposal and user overrides into a confirmed mapping. Requires Task Name.
    /// </summary>
    /// <exception cref="InvalidOperationException">When Task Name is not assigned.</exception>
    CsvColumnMapping ToConfirmedMapping(
        ColumnMappingProposal proposal,
        IReadOnlyDictionary<string, string?> userAssignments);
}
