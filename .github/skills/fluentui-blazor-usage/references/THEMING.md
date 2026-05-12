# Theming - Fluent UI Blazor v5

## Overview

v5 uses CSS custom properties and JS theme helpers. `FluentDesignTheme` is not used in v5.

## JS Theme Helpers

```csharp
await JSRuntime.InvokeVoidAsync("Blazor.theme.setLightTheme");
await JSRuntime.InvokeVoidAsync("Blazor.theme.setDarkTheme");
var isDark = await JSRuntime.InvokeAsync<bool>("Blazor.theme.switchTheme");
```

```csharp
var isDarkMode = await JSRuntime.InvokeAsync<bool>("Blazor.theme.isDarkMode");
var isSystemDark = await JSRuntime.InvokeAsync<bool>("Blazor.theme.isSystemDark");
```

## Theme-aware CSS

- `hidden-if-light`
- `hidden-if-dark`

## Token Usage

Use Fluent CSS variables directly:

- `var(--colorBrandBackground)`
- `var(--colorNeutralForeground1)`
- `var(--borderRadiusMedium)`

Or use typed C# constants (`StylesVariables`, `SystemColors`) where suitable.

## Spacing Utilities

All components support `Margin` and `Padding` parameters.

```razor
<FluentCard Margin="Margin.All4" Padding="Padding.Horizontal3 Padding.Vertical2">
    Content
</FluentCard>
```

Scale follows 4px steps (1 = 4px, 2 = 8px, etc.).
