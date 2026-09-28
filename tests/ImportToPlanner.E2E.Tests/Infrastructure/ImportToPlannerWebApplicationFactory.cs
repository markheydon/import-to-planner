using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.E2E.Tests.TestDoubles;
using ImportToPlanner.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImportToPlanner.E2E.Tests.Infrastructure;

internal sealed class ImportToPlannerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool commercialModeEnabled;

    public ImportToPlannerWebApplicationFactory(bool commercialModeEnabled = false)
    {
        this.commercialModeEnabled = commercialModeEnabled;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(E2ETestingHostEnvironment.EnvironmentName);

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
}
