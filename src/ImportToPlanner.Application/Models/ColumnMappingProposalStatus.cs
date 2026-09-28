namespace ImportToPlanner.Application.Models;

/// <summary>
/// Outcome of automatic column mapping for an uploaded CSV header row.
/// </summary>
public enum ColumnMappingProposalStatus
{
    Ready,
    NeedsTaskName,
    Conflict,
    NeedsConfirmation,
}
