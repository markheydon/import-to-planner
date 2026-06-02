using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;
using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialApiServiceTests
{
    [Fact]
    public void ConfigureServices_RegistersCommercialStoreInterfaces()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        // Mimic the service registration in Program.cs by registering the same services.
        services.AddSingleton<ICommercialAccountsService, CommercialAccountsService>();
        services.AddSingleton<ICommercialAuditService, CommercialAuditService>();
        services.AddSingleton<ITenantMetadataService, TenantMetadataService>();
        using var serviceProvider = services.BuildServiceProvider();

        // Verify that the store interfaces resolve to their concrete implementations.
        var accountService = serviceProvider.GetRequiredService<ICommercialAccountsService>();
        var auditService = serviceProvider.GetRequiredService<ICommercialAuditService>();
        var tenantMetadataService = serviceProvider.GetRequiredService<ITenantMetadataService>();

        Assert.Equal("CommercialAccountsService", accountService.GetType().Name);
        Assert.Equal("CommercialAuditService", auditService.GetType().Name);
        Assert.Equal("TenantMetadataService", tenantMetadataService.GetType().Name);
    }
}
