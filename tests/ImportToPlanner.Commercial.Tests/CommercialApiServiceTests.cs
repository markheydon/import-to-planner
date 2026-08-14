using Azure.Data.Tables;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;
using ImportToPlanner.Commercial.Features.TenantMetadata.Services;

namespace ImportToPlanner.Commercial.Tests;

public sealed class CommercialServiceRegistrationTests
{
    [Fact]
    public void AddCommercialServices_RegistersCommercialStoreInterfaces()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        services.AddCommercialServices(retentionSweepEnabled: false);
        using var serviceProvider = services.BuildServiceProvider();

        var accountService = serviceProvider.GetRequiredService<ICommercialAccountsService>();
        var auditService = serviceProvider.GetRequiredService<ICommercialAuditService>();
        var tenantMetadataService = serviceProvider.GetRequiredService<ITenantMetadataService>();

        Assert.Equal("CommercialAccountsService", accountService.GetType().Name);
        Assert.Equal("CommercialAuditService", auditService.GetType().Name);
        Assert.Equal("TenantMetadataService", tenantMetadataService.GetType().Name);
    }
}
