namespace ImportToPlanner.CommercialService.CommercialAccounts.Models;

/// <summary>
/// Represents an audit event for account lifecycle changes and sign-in outcomes.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="OccurredUtc">The UTC timestamp when the event occurred.</param>
/// <param name="EventType">The audit event type.</param>
/// <param name="Outcome">A stable outcome code for diagnostics and policy tracking.</param>
/// <param name="RetentionExpiresUtc">The UTC timestamp when the event retention expires.</param>
public sealed record AccountAuditEvent(
    string TenantId,
    string UserId,
    DateTimeOffset OccurredUtc,
    AccountAuditEventType EventType,
    string Outcome,
    DateTimeOffset RetentionExpiresUtc);

/// <summary>
/// Represents supported account audit event categories.
/// </summary>
public enum AccountAuditEventType
{
    AccountCreated,
    AccountDeleted,
    AccountRestored,
    SignInOutcome,
}
