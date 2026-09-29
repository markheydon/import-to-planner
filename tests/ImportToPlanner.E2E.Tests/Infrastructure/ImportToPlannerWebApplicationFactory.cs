using System.Net;
using System.Net.Sockets;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Domain;
using ImportToPlanner.E2E.Tests.TestDoubles;
using ImportToPlanner.Infrastructure.Graph.Import;
using ImportToPlanner.Web;
using ImportToPlanner.Web.Features.Demo;
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

    internal async Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersForDiagnosticsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IPlannerGateway>();
        return await gateway.GetAvailableContainersAsync(cancellationToken).ConfigureAwait(false);
    }

    public static Task<ImportToPlannerWebApplicationFactory> StartAsync(
        bool commercialModeEnabled = false,
        CancellationToken cancellationToken = default)
        => StartAsync(
            commercialModeEnabled,
            enableDemoDocumentationCapture: false,
            cancellationToken);

    public static Task<ImportToPlannerWebApplicationFactory> StartForDemoScreenshotCaptureAsync(
        CancellationToken cancellationToken = default)
        => StartAsync(
            commercialModeEnabled: false,
            enableDemoDocumentationCapture: true,
            cancellationToken);

    public static async Task<ImportToPlannerWebApplicationFactory> StartAsync(
        bool commercialModeEnabled,
        bool enableDemoDocumentationCapture,
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

        var configurationValues = new Dictionary<string, string?>
        {
            ["Features:CommercialMode:Enabled"] = commercialModeEnabled.ToString(),
            ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
        };

        if (enableDemoDocumentationCapture)
        {
            configurationValues["DemoMode:DemoControlsEnabled"] = "true";
            configurationValues["DemoMode:OperatorAllowlist:0"] = "e2e-test-user@contoso.com";
        }

        builder.Configuration.AddInMemoryCollection(configurationValues);

        if (enableDemoDocumentationCapture)
        {
            builder.Services.RemoveAll<ICurrentTenantContextAccessor>();
            builder.Services.AddScoped<ICurrentTenantContextAccessor, E2EFixedTenantContextAccessor>();
            builder.Services.RemoveAll<IDemoModeSession>();
            builder.Services.AddSingleton<IDemoModeSession, AlwaysActiveDemoModeSession>();
            builder.Services.RemoveAll<IDemoModeAuthorisationService>();
            builder.Services.AddScoped<IDemoModeAuthorisationService, E2EDemoDocumentationAuthorisation>();

            builder.Services.RemoveAll<IPlannerGateway>();
            builder.Services.AddScoped<E2EPlannerGateway>();
            builder.Services.AddScoped<IPlannerGateway>(serviceProvider =>
                new DemoAwarePlannerGateway(
                    serviceProvider.GetRequiredService<E2EPlannerGateway>(),
                    serviceProvider.GetRequiredService<IDemoModeSession>()));

            builder.Services.RemoveAll<ICsvImportParser>();
            builder.Services.AddScoped<ICsvImportParser>(serviceProvider =>
                new DemoAwareCsvImportParser(
                    serviceProvider.GetRequiredService<CsvImportParser>(),
                    serviceProvider.GetRequiredService<IDemoModeSession>()));
        }
        else
        {
            builder.Services.RemoveAll<IPlannerGateway>();
            builder.Services.AddScoped<IPlannerGateway, E2EPlannerGateway>();
        }

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
