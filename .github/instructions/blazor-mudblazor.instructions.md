description: 'Blazor and Fluent UI guidance for Razor UI work, designed to coexist with task-specific skills.'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

# Blazor + Fluent UI Instructions

Use this file as baseline guidance for Blazor UI work in Razor components.

## Intent

- Keep behaviour predictable, maintainable, and aligned with repository conventions.
- Prefer minimal, focused changes for the requested outcome.
- Avoid guidance that conflicts with specialized skills; defer to them when active.

## Scope and Change Discipline

- Follow repository conventions first.
- Keep changes small and scoped to the user request.
- For `*.razor.cs` files, treat this instruction as primary for component-facing concerns (UI state, lifecycle usage, event flow, parameter interactions).
- For `*.razor.cs` internals that are pure domain or application policy, align with the C# clean architecture instruction while preserving component boundaries.

## Blazor Component Patterns

- Keep components focused and small.
- Keep UI logic lightweight in `.razor`; move complex logic to `.razor.cs` or services.
- Use `EventCallback` or `EventCallback<T>` for child-to-parent communication.
- Prefer explicit parameters over hidden coupling.
- Use lifecycle methods intentionally (`OnInitializedAsync`, `OnParametersSetAsync`) and avoid fire-and-forget tasks.

## Fluent UI Usage

- Prefer Fluent UI Blazor components and parameters before custom CSS.
- Prefer composition with Fluent UI layout primitives and utility classes.
- Keep custom `.razor.css` minimal and use it only when component options are insufficient.
- Ensure required providers are present in the main layout when using dialogs/popovers/snackbars.

## Decision Priority

When guidance conflicts, apply this order:

1. User request
2. Repository conventions
3. Active task-specific skill (for example: Fluent UI, async, docs, or test-framework skills)
4. This instruction file
5. General Blazor/.NET defaults

For `*.razor.cs`, Blazor component-facing concerns are governed first by `blazor-mudblazor.instructions.md`, while Clean Architecture governs non-UI policy boundaries and dependency direction.
