namespace ImportToPlanner.Web.Themes;

/// <summary>
/// Carries the current theme state and a callback for child components to request a theme change.
/// Passed down the component tree via <c>CascadingValue</c> from <c>MainLayout</c>.
/// </summary>
/// <param name="ThemeMode">The currently active theme selection (Auto, Light, or Dark).</param>
/// <param name="IsDarkMode">Whether the dark palette is currently applied.</param>
/// <param name="SetThemeMode">Callback that child components invoke to change the active theme.</param>
internal sealed record ThemeState(
    ThemeMode ThemeMode,
    bool IsDarkMode,
    Func<ThemeMode, Task> SetThemeMode);
