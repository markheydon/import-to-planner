using ImportToPlanner.Web.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace ImportToPlanner.Web.Components.Layout;

/// <summary>Code-behind for <c>MainLayout.razor</c>. Owns theme state for the entire application.</summary>
public sealed partial class MainLayout
{
    private const string ThemeModeSessionKey = "import-to-planner-theme-mode";

    private bool _isDarkMode;
    private bool _isThemeInitialised;
    private ThemeMode _themeMode = ThemeMode.Auto;
    private MudThemeProvider? _themeProvider;
    private readonly MudTheme _theme = ImportToPlannerTheme.Default;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<MainLayout> Logger { get; set; } = default!;

    /// <summary>The current theme state, cascaded to child components.</summary>
    private ThemeState ThemeContext => new(_themeMode, _isDarkMode, SetThemeModeAsync);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isThemeInitialised || _themeProvider is null)
        {
            return;
        }

        string? mode;
        try
        {
            mode = await JSRuntime.InvokeAsync<string?>("importToPlannerThemeStorage.getThemeMode", ThemeModeSessionKey);
        }
        catch (InvalidOperationException ex) when (IsDeferredJsInteropException(ex))
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogDebug(ex, "Unexpected invalid operation while initialising theme mode from session storage.");
            throw;
        }

        _themeMode = ParseThemeMode(mode);
        await ApplyThemeModeAsync(_themeMode);
        await _themeProvider.WatchSystemDarkModeAsync(OnSystemPreferenceChangedAsync);

        _isThemeInitialised = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetThemeModeAsync(ThemeMode mode)
    {
        await ApplyThemeModeAsync(mode);

        await JSRuntime.InvokeVoidAsync(
            "importToPlannerThemeStorage.setThemeMode",
            ThemeModeSessionKey,
            ToSessionValue(mode));

        await InvokeAsync(StateHasChanged);
    }

    private Task OnSystemPreferenceChangedAsync(bool systemPrefersDarkMode)
    {
        if (_themeMode is not ThemeMode.Auto)
        {
            return Task.CompletedTask;
        }

        _isDarkMode = systemPrefersDarkMode;
        return InvokeAsync(StateHasChanged);
    }

    private async Task ApplyThemeModeAsync(ThemeMode mode)
    {
        _themeMode = mode;
        if (mode is ThemeMode.Auto && _themeProvider is not null)
        {
            _isDarkMode = await _themeProvider.GetSystemDarkModeAsync();
            return;
        }

        _isDarkMode = mode is ThemeMode.Dark;
    }

    private static ThemeMode ParseThemeMode(string? mode)
        => mode?.ToLowerInvariant() switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            _ => ThemeMode.Auto,
        };

    private static string ToSessionValue(ThemeMode mode)
        => mode switch
        {
            ThemeMode.Light => "light",
            ThemeMode.Dark => "dark",
            _ => "auto",
        };

    private static bool IsDeferredJsInteropException(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("prerender", StringComparison.OrdinalIgnoreCase);
    }
}
