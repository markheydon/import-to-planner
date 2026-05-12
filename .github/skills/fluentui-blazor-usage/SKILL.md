---
name: fluentui-blazor-usage
description: >
  Provides accurate coding patterns for building Blazor applications with
  Microsoft.FluentUI.AspNetCore.Components v5. Covers setup, theming,
  navigation, dialogs, data grid, forms, icons, and common migration pitfalls.
---

# Fluent UI Blazor v5 - Usage Skill

Use this skill when generating or reviewing UI code for Fluent UI Blazor v5.

## Applicability Gate

- Apply this skill for implementation only when the project references `Microsoft.FluentUI.AspNetCore.Components` v5.
- If the project is on v4, use this skill for migration planning and known-issue research only.

## Critical Notes

- v5 is a major rewrite and differs from v4 in multiple APIs and component names.
- v5 targets .NET 9+.
- Prefer official v5 patterns over ad-hoc CSS tweaks for layout or behaviour fixes.
- Prefer Fluent UI components and built-in parameters over raw HTML/CSS overrides.
- Treat custom CSS or raw HTML workarounds as a last resort and only after checking Fluent UI MCP guidance and component docs.

## Quick Setup Checklist

```csharp
// Program.cs
builder.Services.AddFluentUIComponents();
```

```razor
@* MainLayout.razor / App.razor *@
<FluentProviders />
```

```html
<link href="_content/Microsoft.FluentUI.AspNetCore.Components/css/default-fuib.css" rel="stylesheet" />
```

## Key v4 to v5 Differences

- `FluentDesignTheme` removed in favour of CSS custom properties and JS theme helpers.
- Navigation uses `FluentNav`, `FluentNavItem`, `FluentNavCategory`.
- `IToastService` and `IMessageService` are removed.
- List controls now use `TOption, TValue` and `SelectedItems`.
- Prefer `ButtonAppearance.Primary` over v4 `Appearance.Accent` patterns.
- Dialog usage is simplified through `IDialogService.ShowDialogAsync<TDialog>(DialogOptions)`.

## Common Pitfalls

1. Missing `<FluentProviders />` in root layout.
2. Mixing v4 and v5 component names.
3. Using v4 list-binding properties (`SelectedOptions`).
4. Relying on v4-specific enums and service types.

## References

- [SETUP.md](references/SETUP.md)
- [DATAGRID.md](references/DATAGRID.md)
- [THEMING.md](references/THEMING.md)
