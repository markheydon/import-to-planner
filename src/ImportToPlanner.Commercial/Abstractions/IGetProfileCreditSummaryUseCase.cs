using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Loads organisation credit summary for the commercial profile page.
/// </summary>
public interface IGetProfileCreditSummaryUseCase
{
    /// <summary>
    /// Ensures the current balance and maps summary fields for profile display.
    /// </summary>
    Task<GetProfileCreditSummaryOutcome> GetAsync(
        ProfileCreditSummaryRequest request,
        CancellationToken cancellationToken);
}
