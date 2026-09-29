using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;

namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Demonstration session that refuses activation unless deployment policy allows it.
/// </summary>
public sealed class AuthorisedDemoModeSession(
    DemoModeSession session,
    IDemoModeAuthorisationService authorisation) : IDemoModeSession
{
    /// <inheritdoc />
    public bool IsActive => session.IsActive;

    /// <inheritdoc />
    public void Activate()
    {
        if (!authorisation.CanActivateDemo())
        {
            throw new InvalidOperationException(
                "Demonstration mode can only be activated by an allowlisted operator when demo controls are enabled.");
        }

        session.Activate();
    }

    /// <inheritdoc />
    public void Deactivate() => session.Deactivate();

    /// <inheritdoc />
    public void Reset() => session.Reset();
}
