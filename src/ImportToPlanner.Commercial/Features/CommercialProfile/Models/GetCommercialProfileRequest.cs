using ImportToPlanner.Commercial.Common.Models;

namespace ImportToPlanner.Commercial.Features.CommercialProfile.Models;

/// <summary>
/// Gets the hosted commercial account profile for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
public sealed record GetCommercialProfileRequest(SessionIdentityContext SessionIdentity);
