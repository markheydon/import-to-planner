using ImportToPlanner.Commercial;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Infrastructure.Stripe;
using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.Tests.Infrastructure;

public sealed class CommercialOffStripeRegistrationTests
{
    [Fact]
    public void AddCommercial_DoesNotRegisterStripeWebhookVerifier()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
                ["Storage:CommercialCreditLedgerTable"] = "CommercialCreditLedger",
                ["Storage:CommercialBillingTable"] = "CommercialBilling",
            })
            .Build();

        services.AddCommercial(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.Null(serviceProvider.GetService<IPaymentWebhookVerifier>());
    }

    [Fact]
    public void AddInfrastructure_WhenCommercialOff_DoesNotRegisterStripeServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.Null(serviceProvider.GetService<IPaymentWebhookVerifier>());
    }

    [Fact]
    public void AddStripeInfrastructure_RegistersWebhookVerifierOnlyWhenExplicitlyAdded()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:ApiKey"] = "rk_test_placeholder",
                ["Stripe:WebhookSecret"] = "whsec_placeholder",
            })
            .Build();

        services.AddStripeInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IPaymentWebhookVerifier>());
    }
}
