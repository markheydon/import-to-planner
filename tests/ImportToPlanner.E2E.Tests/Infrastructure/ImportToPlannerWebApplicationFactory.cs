using System.Net;
using System.Net.Sockets;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.E2E.Tests.TestDoubles;
using ImportToPlanner.Web;
using ImportToPlanner.Web.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImportToPlanner.E2E.Tests.Infrastructure;

/// <summary>
/// Starts the web app on a real Kestrel listener so Playwright can connect over HTTP.
/// </summary>
internal sealed class ImportToPlannerWebApplicationFactory : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly bool ownsApp;

    private ImportToPlannerWebApplicationFactory(WebApplication app, Uri serverBaseAddress, bool ownsApp)
    {
        this.app = app;
        ServerBaseAddress = serverBaseAddress;
        this.ownsApp = ownsApp;
    }

    public Uri ServerBaseAddress { get; }

    public static async Task<ImportToPlannerWebApplicationFactory> StartAsync(
        bool commercialModeEnabled = false,
        CancellationToken cancellationToken = default)
    {
        Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, "true");

        var listenPort = AllocateLoopbackPort();
        var listenUrl = $"http://127.0.0.1:{listenPort}";

        var builder = ImportToPlannerWebHost.CreateWebApplicationBuilder(
        [
            "--environment", E2ETestingHostEnvironment.EnvironmentName,
            "--urls", listenUrl,
        ]);

        builder.WebHost.UseKestrel();
        builder.WebHost.UseContentRoot(Path.GetDirectoryName(typeof(ImportToPlannerWebHost).Assembly.Location)!);
        builder.WebHost.UseStaticWebAssets();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Features:CommercialMode:Enabled"] = commercialModeEnabled.ToString(),
            ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
        });

        builder.Services.RemoveAll<IPlannerGateway>();
        builder.Services.AddScoped<IPlannerGateway, E2EPlannerGateway>();

        var app = builder.Build();
        ImportToPlannerWebHost.ConfigureWebApplication(app);
        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        var baseAddress = new Uri($"{listenUrl}/");
        WaitForHttpReady(baseAddress);

        return new ImportToPlannerWebApplicationFactory(app, baseAddress, ownsApp: true);
    }

    public async ValueTask DisposeAsync()
    {
        if (ownsApp)
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }

        Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, null);
    }

    private static int AllocateLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void WaitForHttpReady(Uri baseAddress)
    {
        using var client = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(2) };
        for (var attempt = 0; attempt < 200; attempt++)
        {
            try
            {
                using var response = client.GetAsync("/").GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode || (int)response.StatusCode is >= 300 and < 400)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException)
            {
                Thread.Sleep(50);
            }
        }

        throw new InvalidOperationException($"The E2E host did not respond over HTTP at {baseAddress}.");
    }
}
