namespace ImportToPlanner.Web.Themes;

/// <summary>Represents the user's preferred colour scheme for the application.</summary>
internal enum ThemeMode
{
    /// <summary>Follows the operating-system or browser preference.</summary>
    Auto,

    /// <summary>Forces the light palette.</summary>
    Light,

    /// <summary>Forces the dark palette.</summary>
    Dark,
}
