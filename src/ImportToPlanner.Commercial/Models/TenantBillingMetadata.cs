namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Stripe billing metadata for a commercial tenant (not stored on the account row or ledger).
/// </summary>
/// <param name="TenantId">Entra tenant partition key.</param>
/// <param name="StripeCustomerId">Opaque Stripe customer identifier.</param>
/// <param name="LastInvoiceOrReceiptId">Optional latest invoice or receipt reference for support.</param>
/// <param name="UpdatedUtc">Last write instant.</param>
public sealed record TenantBillingMetadata(
    string TenantId,
    string StripeCustomerId,
    string? LastInvoiceOrReceiptId,
    DateTimeOffset UpdatedUtc);
