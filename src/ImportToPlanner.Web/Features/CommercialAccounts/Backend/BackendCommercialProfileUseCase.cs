using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class BackendCommercialProfileUseCase(CommercialApiServiceClient commercialApiServiceClient) : ICommercialProfileUseCase
{
    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return commercialApiServiceClient.GetProfileAsync(
            new GetCommercialProfileRequest(sessionIdentity),
            cancellationToken);
    }

    public Task DeleteAccountAsync(SessionIdentityContext sessionIdentity, DateTimeOffset occurredUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return commercialApiServiceClient.DeleteAccountAsync(
            new DeleteCommercialAccountRequest(sessionIdentity, occurredUtc),
            cancellationToken);
    }

    public Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return commercialApiServiceClient.RestoreAccountAsync(
            new RestoreCommercialAccountRequest(sessionIdentity, occurredUtc),
            cancellationToken);
    }

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        return commercialApiServiceClient.PurgeExpiredAsync(
            new PurgeExpiredCommercialDataRequest(asOfUtc, batchSize),
            cancellationToken);
    }
}
