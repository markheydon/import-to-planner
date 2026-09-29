using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Demo;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Tests;

public sealed class DemoModeAuthorisationTests
{
    [Fact]
    public void Authorisation_WhenAllowlistMismatch_DoesNotShowControls()
    {
        var service = CreateAuthorisationService(
            demoControlsEnabled: true,
            allowlist: ["other-user@contoso.com"],
            identity: new SessionIdentityContext("tenant", "user-1", "alex@contoso.com", null));

        Assert.False(service.CanShowDemoControls());
    }

    [Fact]
    public void Authorisation_WhenAllowlistMatches_ShowsControls()
    {
        var service = CreateAuthorisationService(
            demoControlsEnabled: true,
            allowlist: ["alex@contoso.com"],
            identity: new SessionIdentityContext("tenant", "user-1", "alex@contoso.com", null));

        Assert.True(service.CanShowDemoControls());
    }

    private static DemoModeAuthorisationService CreateAuthorisationService(
        bool demoControlsEnabled,
        IReadOnlyList<string> allowlist,
        SessionIdentityContext? identity)
    {
        var policy = Options.Create(new DemoModeDeploymentPolicy
        {
            DemoControlsEnabled = demoControlsEnabled,
            OperatorAllowlist = allowlist,
        });
        var accessor = Substitute.For<IOperatorIdentityContextAccessor>();
        accessor.TryGetCurrent().Returns(identity);
        return new DemoModeAuthorisationService(policy, accessor);
    }
}
