using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.E2E.Tests.TestDoubles;

/// <summary>
/// Keeps demonstration mode active for the lifetime of a screenshot-capture host instance.
/// </summary>
internal sealed class AlwaysActiveDemoModeSession : IDemoModeSession
{
    public bool IsActive => true;

    public void Activate()
    {
    }

    public void Deactivate()
    {
    }

    public void Reset()
    {
    }
}
