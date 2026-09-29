using ImportToPlanner.E2E.Tests.Infrastructure;

namespace ImportToPlanner.E2E.Tests;

public sealed class DemoCaptureHostDiagnosticsTests
{
    [Fact]
    public async Task Demo_capture_host_returns_synthetic_containers_from_gateway()
    {
        await using var factory = await ImportToPlannerWebApplicationFactory.StartForDemoScreenshotCaptureAsync(
            TestContext.Current.CancellationToken);

        var containers = await factory.GetAvailableContainersForDiagnosticsAsync(
            TestContext.Current.CancellationToken);

        Assert.NotEmpty(containers);
        Assert.Contains(containers, container => container.DisplayName.Contains("Contoso", StringComparison.OrdinalIgnoreCase));
    }
}
