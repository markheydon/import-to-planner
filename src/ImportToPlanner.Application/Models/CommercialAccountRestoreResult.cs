namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents the result of a commercial account restore request.
/// </summary>
public enum CommercialAccountRestoreResult
{
    Restored,
    AccountNotFound,
    AccountNotDeleted,
    RetentionExpired,
}