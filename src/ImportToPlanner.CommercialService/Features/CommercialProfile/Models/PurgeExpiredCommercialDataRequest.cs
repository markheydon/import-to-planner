namespace ImportToPlanner.CommercialService.Features.CommercialProfile.Models;

/// <summary>
/// Purges expired hosted commercial account and audit data.
/// </summary>
/// <param name="AsOfUtc">The UTC cutoff for purge evaluation.</param>
/// <param name="BatchSize">The maximum records to purge in this invocation.</param>
public sealed record PurgeExpiredCommercialDataRequest(
    DateTimeOffset AsOfUtc,
    int BatchSize);
