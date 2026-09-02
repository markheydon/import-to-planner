namespace ImportToPlanner.Web.Features.Import.Workflows;

/// <summary>
/// Credit balance snapshot for preview and confirm gating.
/// </summary>
public sealed class WorkflowCreditBalanceSnapshot
{
    /// <summary>
    /// Gets or sets the would-create task count from the preview.
    /// </summary>
    public int WouldCreateCount { get; set; }

    /// <summary>
    /// Gets or sets live remaining credits after ensure.
    /// </summary>
    public int? RemainingCredits { get; set; }

    /// <summary>
    /// Gets or sets the shortfall when would-create exceeds remaining.
    /// </summary>
    public int Shortfall { get; set; }

    /// <summary>
    /// Gets or sets whether confirm should be blocked for insufficient credits.
    /// </summary>
    public bool InsufficientCredits { get; set; }

    /// <summary>
    /// Gets or sets whether the ledger could not be read or updated.
    /// </summary>
    public bool LedgerUnavailable { get; set; }

    /// <summary>
    /// Gets or sets the structured ledger failure code when unavailable.
    /// </summary>
    public string? LedgerFailureCode { get; set; }
}
