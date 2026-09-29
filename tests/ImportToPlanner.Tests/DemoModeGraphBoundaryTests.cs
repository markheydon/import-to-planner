using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Infrastructure.Graph.Import;
using ImportToPlanner.Infrastructure.Graph.Planner;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;

namespace ImportToPlanner.Tests;

public sealed class DemoModeGraphBoundaryTests
{
    [Fact]
    public async Task GetAvailableContainersAsync_WhenDemonstrationModeIsActive_ThrowsBeforeGraph()
    {
        var adapter = Substitute.For<IRequestAdapter>();
        var session = Substitute.For<IDemoModeSession>();
        session.IsActive.Returns(true);
        var gateway = new GraphPlannerGateway(new GraphServiceClient(adapter), demoModeSession: session);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.GetAvailableContainersAsync(CancellationToken.None));

        Assert.Contains("demonstration mode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAsync_WhenDemonstrationModeIsActive_ThrowsBeforeReadingCsv()
    {
        var session = Substitute.For<IDemoModeSession>();
        session.IsActive.Returns(true);
        var parser = new CsvImportParser(new CsvColumnMappingService(), session);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parser.ParseAsync("Task name\nExample", CancellationToken.None));

        Assert.Contains("demonstration mode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
