# Setup Guide - Fluent UI Blazor v5

## Prerequisites

- .NET 9 SDK or later
- Visual Studio 2026 or VS Code with GitHub Copilot

## Package Installation

```bash
dotnet add package Microsoft.FluentUI.AspNetCore.Components --prerelease
```

## Service Registration

```csharp
builder.Services.AddFluentUIComponents();
```

Optional configuration:

```csharp
builder.Services.AddFluentUIComponents(config =>
{
    config.Tooltip.Delay = 500;
});
```

## Providers in Root Layout

```razor
<FluentProviders />
```

Without this, dialogs and tooltips may silently fail.

## Stylesheet

```html
<link href="_content/Microsoft.FluentUI.AspNetCore.Components/css/default-fuib.css" rel="stylesheet" />
```

## Imports

```razor
@using Microsoft.FluentUI.AspNetCore.Components
```

## Hosting Models

Use `AddFluentUIComponents()` in:

- Blazor Server: server `Program.cs`
- Blazor WebAssembly: client `Program.cs`
- Blazor Web App (Auto): both server and client `Program.cs`

## Troubleshooting

- Plain HTML rendering: confirm stylesheet and script assets.
- Dialog not shown: ensure `<FluentProviders />` exists.
- Missing service errors: add `AddFluentUIComponents()`.
- Compile errors for v4 enums/names: migrate to v5 symbols.
