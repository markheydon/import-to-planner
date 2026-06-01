using ImportToPlanner.CommercialService.Common.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialProfile.Models;

/// <summary>
/// Soft-deletes the hosted commercial account for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record DeleteCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);
