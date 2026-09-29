using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Web.Features.Demo;

namespace ImportToPlanner.Web.Tests;

public sealed class AuthorisedDemoModeSessionTests
{
    [Fact]
    public void Activate_WhenOperatorIsNotAllowed_ThrowsAndStaysInactive()
    {
        var authorisation = Substitute.For<IDemoModeAuthorisationService>();
        authorisation.CanActivateDemo().Returns(false);
        var session = new AuthorisedDemoModeSession(new DemoModeSession(), authorisation);

        var exception = Assert.Throws<InvalidOperationException>(session.Activate);

        Assert.Contains("allowlisted", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Activate_WhenOperatorIsAllowed_ActivatesUnderlyingSession()
    {
        var authorisation = Substitute.For<IDemoModeAuthorisationService>();
        authorisation.CanActivateDemo().Returns(true);
        var session = new AuthorisedDemoModeSession(new DemoModeSession(), authorisation);

        session.Activate();

        Assert.True(session.IsActive);
    }

    [Fact]
    public void Reset_WhenActive_ClearsSessionWithoutAuthorisation()
    {
        var authorisation = Substitute.For<IDemoModeAuthorisationService>();
        authorisation.CanActivateDemo().Returns(true);
        var session = new AuthorisedDemoModeSession(new DemoModeSession(), authorisation);
        session.Activate();
        authorisation.CanActivateDemo().Returns(false);

        session.Reset();

        Assert.False(session.IsActive);
    }
}
