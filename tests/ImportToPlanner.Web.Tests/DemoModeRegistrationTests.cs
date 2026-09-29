using ImportToPlanner.Application;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web.Features.Demo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Web.Tests;

public sealed class DemoModeRegistrationTests
{
    [Fact]
    public void AddDemoMode_RegistersAuthorisedSessionAndImportDecoratorsLast()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
                ["DemoMode:DemoControlsEnabled"] = "false",
            })
            .Build();

        services.AddSingleton<ISessionIdentityContextAccessor, UnusedSessionIdentity>();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddDemoMode(configuration);

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPlannerGateway));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICsvImportParser));
        Assert.NotNull(services.Single(descriptor => descriptor.ServiceType == typeof(IPlannerGateway)).ImplementationFactory);
        Assert.NotNull(services.Single(descriptor => descriptor.ServiceType == typeof(ICsvImportParser)).ImplementationFactory);

        using var provider = services.BuildServiceProvider();
        var session = provider.GetRequiredService<IDemoModeSession>();
        Assert.IsType<AuthorisedDemoModeSession>(session);
    }

    private sealed class UnusedSessionIdentity : ISessionIdentityContextAccessor
    {
        public SessionIdentityContext? TryGetCurrent() => null;
    }
}
