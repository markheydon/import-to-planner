using System.Net;

namespace ImportToPlanner.E2E.Tests.Infrastructure;

[Collection(nameof(BrowserE2ETests))]
public sealed class ImportToPlannerWebApplicationFactoryTests
{
    [Fact]
    public async Task ServerBaseAddress_UsesAssignedEphemeralPort()
    {
        await using var factory = await ImportToPlannerWebApplicationFactory.StartAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(factory.ServerBaseAddress.Port > 0);
        Assert.Equal("127.0.0.1", factory.ServerBaseAddress.Host);
    }

    [Fact]
    public async Task ServerBaseAddress_RespondsOverHttp()
    {
        await using var factory = await ImportToPlannerWebApplicationFactory.StartAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = factory.ServerBaseAddress };

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SignInRoute_WhenE2ETestingHostWithOptIn_RedirectsToHome()
    {
        await using var factory = await ImportToPlannerWebApplicationFactory.StartAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var client = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false },
            disposeHandler: true)
        {
            BaseAddress = factory.ServerBaseAddress,
        };

        using var response = await client.GetAsync("/e2e/sign-in", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }
}
