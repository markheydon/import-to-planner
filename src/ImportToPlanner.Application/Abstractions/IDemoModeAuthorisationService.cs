namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Determines whether the signed-in operator may view or use demonstration controls.
/// </summary>
public interface IDemoModeAuthorisationService
{
    /// <summary>Gets a value indicating whether demo controls may be shown.</summary>
    bool CanShowDemoControls();

    /// <summary>Gets a value indicating whether demo mode may be activated.</summary>
    bool CanActivateDemo();
}
