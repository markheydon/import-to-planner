using MudBlazor;

namespace ImportToPlanner.Web.Themes;

internal static class ImportToPlannerTheme
{
    internal static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0f6cbd",
            Secondary = "#038387",
            Black = "#201f1e",
            Surface = "#ffffff",
            Background = "#f3f2f1",
            BackgroundGray = "#edebe9",
            AppbarText = "#242424",
            AppbarBackground = "rgba(255,255,255,0.92)",
            DrawerBackground = "#ffffff",
            DrawerText = "#323130",
            DrawerIcon = "#605e5c",
            ActionDefault = "#5f6a79",
            TextPrimary = "#242424",
            TextSecondary = "#605e5c",
            GrayLight = "#edebe9",
            GrayLighter = "#faf9f8",
            Info = "#0f6cbd",
            Success = "#107c10",
            Warning = "#8a6200",
            Error = "#a4262c",
            LinesDefault = "#e1dfdd",
            TableLines = "#edebe9",
            Divider = "#e1dfdd",
            OverlayLight = "#ffffff80",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#60b0ff",
            Secondary = "#5bd1c5",
            Surface = "#1f232b",
            Background = "#161a20",
            BackgroundGray = "#1b2028",
            AppbarText = "#f3f6fb",
            AppbarBackground = "rgba(22,26,32,0.92)",
            DrawerBackground = "#161a20",
            ActionDefault = "#9cb2d5",
            ActionDisabled = "#9999994d",
            ActionDisabledBackground = "#605f6d4d",
            TextPrimary = "#f3f6fb",
            TextSecondary = "#c5cfde",
            TextDisabled = "#ffffff33",
            DrawerIcon = "#aab6cc",
            DrawerText = "#d3dbea",
            GrayLight = "#2a3342",
            GrayLighter = "#222a37",
            Info = "#7cc5ff",
            Success = "#59c36a",
            Warning = "#f7c35f",
            Error = "#f28b94",
            LinesDefault = "#344053",
            TableLines = "#2d3748",
            Divider = "#344053",
            OverlayLight = "#12161d99",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "Segoe UI Variable Text", "Noto Sans", "Helvetica Neue", "Arial", "sans-serif"],
            },
        },
        LayoutProperties = new LayoutProperties(),
    };
}
