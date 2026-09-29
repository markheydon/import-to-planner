using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.Application.Demo;

/// <summary>
/// Scoped session store for demonstration mode.
/// </summary>
public sealed class DemoModeSession : IDemoModeSession
{
    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public void Activate() => IsActive = true;

    /// <inheritdoc />
    public void Deactivate() => IsActive = false;

    /// <inheritdoc />
    public void Reset() => IsActive = false;
}
