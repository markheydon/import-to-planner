using ImportToPlanner.Web.Themes;
using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private Task SetThemeModeAsync(ThemeMode mode)
    {
        if (ThemeState is null)
        {
            Logger.LogWarning("Theme mode change ignored because no ThemeState cascade is available.");
            return Task.CompletedTask;
        }

        return ThemeState.SetThemeMode(mode);
    }

    private string GetThemeMenuIcon()
        => (ThemeState?.ThemeMode ?? ThemeMode.Auto) switch
        {
            ThemeMode.Auto => Icons.Material.Filled.BrightnessAuto,
            ThemeMode.Light => Icons.Material.Filled.LightMode,
            ThemeMode.Dark => Icons.Material.Filled.DarkMode,
            _ => Icons.Material.Filled.BrightnessAuto,
        };

    private string GetThemeOptionLabel(ThemeMode mode)
    {
        var label = GetThemeModeDisplayName(mode);
        var activeMode = ThemeState?.ThemeMode ?? ThemeMode.Auto;
        return activeMode == mode ? $"✓ {label}" : label;
    }

    private static string GetThemeModeDisplayName(ThemeMode mode)
        => mode switch
        {
            ThemeMode.Auto => "Auto",
            ThemeMode.Light => "Light",
            ThemeMode.Dark => "Dark",
            _ => "Auto",
        };
}
