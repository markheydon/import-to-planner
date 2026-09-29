namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Per-session demonstration mode flag for synthetic import journeys.
/// </summary>
public interface IDemoModeSession
{
    /// <summary>Gets a value indicating whether demonstration mode is active.</summary>
    bool IsActive { get; }

    /// <summary>Activates demonstration mode for the current session.</summary>
    void Activate();

    /// <summary>Deactivates demonstration mode for the current session.</summary>
    void Deactivate();

    /// <summary>Resets demonstration mode (sign-in and sign-out).</summary>
    void Reset();
}
