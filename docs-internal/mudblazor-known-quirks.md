# MudBlazor Known Quirks

This note captures framework and component-library behaviours that can look incorrect in code review if checked from markup alone.

## Blazor runtime error banner (`#blazor-error-ui`)

- The `#blazor-error-ui` element may be rendered in page markup even during normal operation.
- In this repository, default hidden styling for that element is provided by MudBlazor CSS from `_content/MudBlazor/MudBlazor.min.css`.
- The stylesheet is referenced in `src/ImportToPlanner.Web/Components/App.razor`.
- This means local `app.css` rules for `#blazor-error-ui` are not required while MudBlazor is present and correctly referenced.

## Review checklist for this area

- Confirm the MudBlazor stylesheet link remains present in `App.razor`.
- Confirm the banner is not visibly shown during normal load.
- If proposing CSS changes, verify runtime behaviour first rather than relying only on static HTML.

## When a local fallback may be needed

Add local fallback CSS only if one of the following is true:

- MudBlazor is removed.
- The `_content/MudBlazor/MudBlazor.min.css` reference is removed or broken.
- MudBlazor changes its bundled behaviour and no longer hides `#blazor-error-ui` by default.
