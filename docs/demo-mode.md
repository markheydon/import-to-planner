# Demonstration mode (operators)

Demonstration mode lets allowlisted operators walk the import workflow with **synthetic**
data for documentation screenshots. It is available on hosted and self-hosted deployments.

## Configuration

Set these keys in environment variables, .NET user secrets, or deployment-specific settings.
Committed `appsettings.json` ships **empty** allowlists and safe defaults only.

| Key | Purpose |
| --- | --- |
| `DemoMode:DemoControlsEnabled` | When `false` (production default), no user sees the toggle. |
| `DemoMode:OperatorAllowlist` | Entra object IDs and/or normalised operator UPNs permitted to use demo controls. |

Example (user secrets on a staging machine):

```json
{
  "DemoMode": {
    "DemoControlsEnabled": true,
    "OperatorAllowlist": [
      "00000000-0000-0000-0000-000000000099",
      "author@contoso.com"
    ]
  }
}
```

Microsoft 365 tenant administrator roles **do not** grant demo access by themselves.

## Behaviour

- Demo mode is **off** at sign-in for every user.
- Allowlisted operators may toggle demo on; it stays on until toggled off or sign-out.
- While active, import steps use in-memory fixtures only — no Microsoft Graph and no real CSV
  parsing for import steps.
- Use non-production environments for screenshot capture.

## UI

Authenticated operators who pass the allowlist see a **Demonstration mode** switch in the
application footer. A persistent warning banner appears while demo is active.

## Public site workflow screenshots

To regenerate demonstration-mode images for `website/content/import-workflow.md`:

```bash
./scripts/capture-demo-workflow-screenshots.sh
```

The script runs an opt-in Playwright capture test (`CAPTURE_DEMO_WORKFLOW_SCREENSHOTS=1`)
and writes PNG files to `website/static/import-workflow/`. Requires Chromium for Playwright
(see `tests/README.md`).
