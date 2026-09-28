using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.E2E.Tests.TestDoubles;
using ImportToPlanner.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.E2E.Tests.Infrastructure;

/// <summary>
/// Starts the web app on a real Kestrel port so Playwright can connect over HTTP.
/// </summary>
internal sealed class ImportToPlannerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool commercialModeEnabled;

    public ImportToPlannerWebApplicationFactory(bool commercialModeEnabled = false)
    {
        this.commercialModeEnabled = commercialModeEnabled;
    }

    public Uri ServerBaseAddress
    {
        get
        {
            var server = Services.GetRequiredService<IServer>();
            var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
                ?? throw new InvalidOperationException("The E2E host did not publish a listening address.");

            return new Uri(address);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(E2ETestingHostEnvironment.EnvironmentName);
        builder.UseKestrel();
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = commercialModeEnabled.ToString(),
                ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPlannerGateway>();
            services.AddScoped<IPlannerGateway, E2EPlannerGateway>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();
        return host;
    }
}
