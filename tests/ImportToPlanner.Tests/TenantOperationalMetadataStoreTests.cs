using ImportToPlanner.Application.Models;
using ImportToPlanner.Tests.TestDoubles;

namespace ImportToPlanner.Tests;

public sealed class TenantOperationalMetadataStoreTests
{
    [Fact]
    public async Task BoundaryStore_WhenMultipleTenantsAreStored_ReturnsTenantScopedRecordsOnly()
    {
        var store = new TenantOperationalMetadataStoreStub();

        await store.UpsertAsync(
            new TenantOperationalMetadata(
                "tenant-a",
                ConsentResolutionStatus.Granted,
                "configured",
                DateTimeOffset.UtcNow,
                "A1",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        await store.UpsertAsync(
            new TenantOperationalMetadata(
                "tenant-b",
                ConsentResolutionStatus.AdminConsentRequired,
                "configured",
                DateTimeOffset.UtcNow,
                "B1",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var tenantA = await store.GetAsync("tenant-a", CancellationToken.None);
        var tenantB = await store.GetAsync("tenant-b", CancellationToken.None);

        Assert.NotNull(tenantA);
        Assert.NotNull(tenantB);
        Assert.Equal("tenant-a", tenantA!.TenantId);
        Assert.Equal(ConsentResolutionStatus.Granted, tenantA.ConsentStatus);
        Assert.Equal("tenant-b", tenantB!.TenantId);
        Assert.Equal(ConsentResolutionStatus.AdminConsentRequired, tenantB.ConsentStatus);
    }
}
