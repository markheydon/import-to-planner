namespace ImportToPlanner.Application.Models;

/// <summary>
/// Outcome of automatic column mapping for an uploaded CSV header row.
/// </summary>
public enum ColumnMappingProposalStatus
{
    /// <summary>All required fields uniquely mapped using canonical header text only.</summary>
    Ready,

    /// <summary>Task Name is not assigned; user must map and confirm in the editor (FR-010).</summary>
    NeedsTaskName,

    /// <summary>Multiple source columns compete for one field; user must resolve in the editor (FR-010).</summary>
    Conflict,

    /// <summary>All required fields uniquely mapped via aliases; show summary and allow preview without a separate confirm (FR-011).</summary>
    NeedsConfirmation,
}
