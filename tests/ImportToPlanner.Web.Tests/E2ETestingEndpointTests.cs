using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Web.Tests;

[CollectionDefinition(nameof(E2ETestingEndpointRouteTests), DisableParallelization = true)]
public sealed class E2ETestingEndpointRouteTests;

[Collection(nameof(E2ETestingEndpointRouteTests))]
public sealed class E2ETestingEndpointTests
{
    [Fact]
    public async Task SignInRoute_WhenEnvironmentIsDevelopment_ReturnsNotFound()
    {
        await using var host = await StartMinimalHostAsync(Environments.Development, e2eOptInEnabled: false);

        using var response = await host.Client.GetAsync(
            "/e2e/sign-in",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SignInRoute_WhenE2ETestingWithoutOptIn_ReturnsNotFound()
    {
        await using var host = await StartMinimalHostAsync(
            E2ETestingHostEnvironment.EnvironmentName,
            e2eOptInEnabled: false);

        using var response = await host.Client.GetAsync(
            "/e2e/sign-in",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<MinimalTestHost> StartMinimalHostAsync(
        string environmentName,
        bool e2eOptInEnabled)
    {
        var previousOptIn = Environment.GetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName);
        Environment.SetEnvironmentVariable(
            E2ETestingHostEnvironment.AllowEnvironmentVariableName,
            e2eOptInEnabled ? "true" : null);

        WebApplication? app = null;
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = environmentName,
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            app = builder.Build();
            app.MapE2ETestingEndpointsIfEnabled(app.Environment);
            await app.StartAsync(TestContext.Current.CancellationToken);

            var baseAddress = new Uri($"{app.Urls.First()}/");
            var client = new HttpClient { BaseAddress = baseAddress };
            return new MinimalTestHost(app, client, previousOptIn);
        }
        catch
        {
            if (app is not null)
            {
                await app.DisposeAsync();
            }

            Environment.SetEnvironmentVariable(
                E2ETestingHostEnvironment.AllowEnvironmentVariableName,
                previousOptIn);
            throw;
        }
    }

    private sealed class MinimalTestHost : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly string? previousOptIn;

        public MinimalTestHost(WebApplication app, HttpClient client, string? previousOptIn)
        {
            this.app = app;
            Client = client;
            this.previousOptIn = previousOptIn;
        }

        public HttpClient Client { get; }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            Environment.SetEnvironmentVariable(
                E2ETestingHostEnvironment.AllowEnvironmentVariableName,
                previousOptIn);
        }
    }
}
