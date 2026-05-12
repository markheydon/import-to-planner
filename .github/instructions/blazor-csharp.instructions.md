---
description: 'Unified guidance for Blazor and C# work in Blazor projects, designed to coexist with task-specific skills.'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css, **/*.cs'
---

# Blazor + C# Unified Instructions

Use this file as baseline guidance for Blazor projects that include Razor UI and C# code.

## Intent

- Keep behaviour predictable, maintainable, and aligned with repository conventions.
- Prefer minimal, focused changes for the requested outcome.
- Avoid guidance that conflicts with specialized skills; defer to them when active.

## Scope and Change Discipline

- Follow repository conventions first.
- Keep changes small and scoped to the user request.
- Reuse existing abstractions before introducing new ones.
- Do not change SDK, target framework, project architecture, or generated files unless explicitly requested.

## C# Version and Language Features

- Use modern C# features that are supported by the repository's target framework and SDK.
- Do not force or set a newer language version than the project supports unless explicitly requested.
- If language-version support is unclear, prefer the project defaults.

## C# Design and Reliability

- Use clear naming and least required visibility.
- Validate inputs at boundaries and throw precise exceptions.
- Prefer async end-to-end for I/O work.
- Avoid blocking async calls such as `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()`.
- Propagate `CancellationToken` when long-running or cancellable work is involved.

## Comments and Documentation

- Prefer self-documenting code and comments that explain intent or trade-offs.
- Avoid boilerplate comments that restate obvious code.
- For public APIs, include XML docs (`<summary>`, and when relevant `<param>`, `<returns>`, `<exception>`, `<example>`).
- For internal/private methods, add comments only when behaviour is non-obvious.

## Blazor Component Patterns

- Keep components focused and small.
- Keep UI logic lightweight in `.razor`; move complex logic to `.razor.cs` or services.
- Use `EventCallback` or `EventCallback<T>` for child-to-parent communication.
- Prefer explicit parameters over hidden coupling.
- Use lifecycle methods intentionally (`OnInitializedAsync`, `OnParametersSetAsync`) and avoid fire-and-forget tasks.

## MudBlazor Usage (When MudBlazor Is Present)

- Prefer MudBlazor components and parameters before custom CSS.
- Prefer composition with MudBlazor layout primitives and utility classes.
- Keep custom `.razor.css` minimal and use it only when component options are insufficient.
- Ensure required providers are present in the main layout when using dialogs/popovers/snackbars.

## Fluent UI Blazor Usage (When Fluent UI Blazor Is Present)

- Prefer Fluent UI Blazor components and their parameters before custom CSS.
- Prefer composition with Fluent UI layout primitives such as `FluentStack`, `FluentGrid`, `FluentGridItem`, and `FluentSpacer`.
- Keep custom `.razor.css` minimal and use it only when component options are insufficient.
- Ensure required providers are present in the main layout: `FluentDesignTheme`, `FluentDialogProvider`, `FluentToastProvider`, `FluentTooltipProvider`, and `FluentMessageBarProvider`.

## Testing Guidance

- Use the testing framework already used in the repository.
- Add or update tests for changed behaviour, especially public-facing behaviour.
- Keep tests deterministic and independent.
- Follow existing naming conventions in the repository instead of imposing a global pattern.
- Mock only external dependencies when isolation is necessary.

## Security and Observability

- Treat external input as untrusted and validate/sanitise as needed.
- Avoid logging secrets or sensitive data.
- Preserve existing logging and telemetry patterns in the repository.

## Decision Priority

When guidance conflicts, apply this order:

1. User request
2. Repository conventions
3. Active task-specific skill (for example: MudBlazor, Fluent UI Blazor, async, docs, or test-framework skills)
4. This instruction file
5. General Blazor/.NET defaults
