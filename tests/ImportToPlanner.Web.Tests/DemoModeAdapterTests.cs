using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Web.Features.Demo;

namespace ImportToPlanner.Web.Tests;

public sealed class DemoModeAdapterTests
{
    [Fact]
    public async Task DemoAwarePlannerGateway_WhenActive_DoesNotInvokeLiveGateway()
    {
        var live = Substitute.For<IPlannerGateway>();
        var session = new DemoModeSession();
        session.Activate();
        var gateway = new DemoAwarePlannerGateway(live, session);

        var containers = await gateway.GetAvailableContainersAsync(CancellationToken.None);

        Assert.Single(containers);
        await live.DidNotReceive().GetAvailableContainersAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DemoAwareCsvImportParser_WhenActive_DoesNotInvokeLiveParser()
    {
        var live = Substitute.For<ICsvImportParser>();
        var session = new DemoModeSession();
        session.Activate();
        var parser = new DemoAwareCsvImportParser(live, session);

        var result = await parser.ParseAsync("ignored", CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        await live.DidNotReceive().ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }
}
