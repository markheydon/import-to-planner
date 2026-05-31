using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class BackendCommercialAccessUseCase(CommercialApiServiceClient commercialApiServiceClient) : ICommercialAccessUseCase
{
    public Task<CommercialAccessDecision> ResolveAccessAsync(
        SessionIdentityContext sessionIdentity,
        bool commercialModeEnabled,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return commercialApiServiceClient.ResolveAccessAsync(
            new ResolveCommercialAccessRequest(sessionIdentity, commercialModeEnabled, occurredUtc),
            cancellationToken);
    }
}
