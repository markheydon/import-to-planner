using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Loads rolling twelve-month credit activity for the commercial profile page.
/// </summary>
public interface IGetProfileCreditActivityUseCase
{
    /// <summary>
    /// Lists and aggregates ledger transactions for profile activity display.
    /// </summary>
    Task<GetProfileCreditActivityOutcome> GetAsync(
        ProfileCreditActivityRequest request,
        CancellationToken cancellationToken);
}
