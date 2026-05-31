using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class DisabledCommercialAccessUseCase : ICommercialAccessUseCase
{
    public Task<CommercialAccessDecision> ResolveAccessAsync(
        SessionIdentityContext sessionIdentity,
        bool commercialModeEnabled,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        var decision = new CommercialAccessDecision(
            CommercialAccessDecisionType.SelfHostedBypass,
            AccountStatus: null,
            RetentionExpiresUtc: null,
            ShouldSignOut: false);

        return Task.FromResult(decision);
    }
}
