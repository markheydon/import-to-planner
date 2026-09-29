# Demo Mode Contract

## Scope

Demo mode enables authorised service operators to walk the import journey with fully
synthetic data for documentation screenshots and marketing capture. It applies to MHCG-hosted
and self-hosted deployments (FR-015).

## Authorisation

- Demo toggle visibility requires ALL of:
  - Signed-in identity matches deployment-configured allowlist (Entra object ID and/or
    verified work-account UPN), AND
  - Deployment policy `DemoControlsEnabled` is true.
- Microsoft 365 tenant administrator roles MUST NOT grant demo access by themselves.
- When `DemoControlsEnabled` is false (default for production), no user sees the toggle.

## Session behaviour

- Demo mode MUST default to **off** at sign-in for every user.
- When an allowlisted operator enables demo mode, it remains **on** until they toggle off or
  sign out.
- Sign-out MUST force demo mode **off**; the next sign-in MUST start with demo off unless
  explicitly toggled on again.

## Import journey behaviour while active

While demo mode is active for an allowlisted session:

- UI MUST present only synthetic groups, plans, tasks, CSV samples, and reports labelled as
  demonstration data.
- The import workflow MUST NOT call Microsoft Graph for import-related steps.
- The import workflow MUST NOT parse or persist a real user-uploaded file for import steps.
- In-memory fixtures MUST be sufficient to traverse: group selection, plan naming, upload,
  validation, preview, execution, and report review.

When demo mode is inactive, behaviour MUST match the standard live import path (Graph and
real uploads as today).

## Configuration surface

Deployment configuration MUST support:

- `DemoMode:OperatorAllowlist` (or equivalent) — collection of object IDs and/or UPNs.
- `DemoMode:DemoControlsEnabled` — boolean; false by default on production.
- Documentation in `docs/` MUST describe allowlist maintenance and safe environments for
  screenshot capture (staging/non-production expected).

## UI requirements

- Toggle MUST be clearly labelled as demo/demonstration mode.
- When demo is active, a persistent indicator MUST warn that data is synthetic and Graph is
  not used for import steps.

## Testing obligations

Automated tests MUST cover at least:

- Allowlist match and non-match (toggle hidden).
- Demo off at sign-in; off after sign-out.
- Demo on persists across navigation until toggle off or sign-out.
- With demo on, Graph gateway / upload dependencies are not invoked for import steps
  (substitute or spy at adapter boundary).

Any Graph or real upload invocation during active demo mode is a **defect**.

## Documentation obligations

- Engineering docs MUST explain enablement for authors (allowlist, policy, environments).
- Public workflow guide SHOULD adopt screenshots captured under demo mode once available.
