using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.CommercialService;
using ImportToPlanner.CommercialService.CommercialAccounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Tests;

public sealed class CommercialApiServiceTests
{
    [Fact]
    public void AddCommercialApiServices_RegistersCommercialOperationUseCases()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
            })
            .Build();

        services.AddCommercialApiServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accessUseCase = serviceProvider.GetRequiredService<ICommercialAccessUseCase>();
        var profileUseCase = serviceProvider.GetRequiredService<ICommercialProfileUseCase>();
        var tenantMetadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();

        Assert.Equal("CommercialAccessUseCase", accessUseCase.GetType().Name);
        Assert.Equal("GetCommercialProfileUseCase", profileUseCase.GetType().Name);
        Assert.Equal("TableTenantOperationalMetadataStore", tenantMetadataStore.GetType().Name);
    }

    [Fact]
    public void AddCommercialApiServices_RegistersRetentionHostedService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
            })
            .Build();

        services.AddCommercialApiServices(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && string.Equals(descriptor.ImplementationType?.Name, "CommercialAccountRetentionHostedService", StringComparison.Ordinal));
    }

    [Fact]
    public void StartupConfigurationValidator_WhenTablesConnectionStringMissing_ThrowsFriendlyError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Contains("ConnectionStrings:tables", exception.Message, StringComparison.Ordinal);
    }
}
