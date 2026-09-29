using ImportToPlanner.Application.Demo;

namespace ImportToPlanner.Tests;

public sealed class DemoModeTests
{
    [Fact]
    public void Session_ResetsToInactiveByDefault()
    {
        var session = new DemoModeSession();
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Session_RemainsActiveUntilReset()
    {
        var session = new DemoModeSession();
        session.Activate();
        Assert.True(session.IsActive);
        session.Deactivate();
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Session_ResetForcesInactiveAfterActivate()
    {
        var session = new DemoModeSession();
        session.Activate();
        session.Reset();
        Assert.False(session.IsActive);
    }
}
