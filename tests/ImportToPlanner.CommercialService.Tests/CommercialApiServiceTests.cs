using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;
using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialApiServiceTests
{
    [Fact]
    public void ConfigureServices_RegistersCommercialOperationUseCases()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        using var serviceProvider = services.BuildServiceProvider();
        var accessService = serviceProvider.GetRequiredService<CommercialAccessService>();
        var profileService = serviceProvider.GetRequiredService<CommercialProfileService>();
        var tenantMetadataService = serviceProvider.GetRequiredService<TenantMetadataService>();

        Assert.Equal("CommercialAccessService", accessService.GetType().Name);
        Assert.Equal("CommercialProfileService", profileService.GetType().Name);
        Assert.Equal("TenantMetadataService", tenantMetadataService.GetType().Name);
    }
}
