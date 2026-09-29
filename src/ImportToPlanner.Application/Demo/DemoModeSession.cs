using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.Application.Demo;

/// <summary>
/// Scoped session store for demonstration mode.
/// This type does not enforce the operator allowlist. The web host registers an
/// <see cref="IDemoModeSession"/> wrapper that checks <see cref="IDemoModeAuthorisationService"/>
/// before activation.
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
