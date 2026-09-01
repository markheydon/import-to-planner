# Quickstart: Import Wizard Stepper Layout

Validation guide for spec 010. Implementation details live in `tasks.md` after `/speckit-tasks`.

## Prerequisites

- .NET SDK from `global.json`
- Solution restores successfully
- Optional: Aspire AppHost running if you want a visual pass (`web` resource)

## Automated checks

From the repository root:

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --verbosity minimal
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

If other projects were untouched, Web.Tests is sufficient for this feature. Before merge, run the full solution test set used by CI.

Expected:

- `HomePageSmokeTests` — five step titles present (including **Preview and confirm** and **Report**); no `Step 1` / `Step 5` in headings; **Preview import** and **Confirm import** action labels on step 4; **not** five `.step-card` nodes.
- `HomePageWorkflowTests` — step progression, preview/execute enablement, stale preview still hold; current/complete/upcoming asserted via stepper/step state or copy, not card CSS.
- `HomePageIdentityContextTests` — email, tenant name, Profile `/profile` when commercial and signed in.
- `HomePageCommercialAccessTests` — signed-out commercial gate; no “Select Planner location”; first-sign-in alert then wizard.
- `HomePageCommercialRetentionTests` — paused access, Restore, Open profile; no wizard.
- `ProfilePageTests` — unchanged pass.

Add or extend tests for:

- No stepper on commercial login and deleted-account gates.
- Self-host (`commercialModeEnabled: false`) — no Profile, no commercial gates, wizard visible when signed in.
- After Story 3: summary labels for location/plan/file; Open in Planner when a plan id exists.
- After Story 3: completed setup not showing full location/plan/CSV forms while `viewedStep` is 4 or 5 unless expanded.

## Manual visual pass (optional but recommended)

1. Run AppHost with commercial mode **off**. Sign in. Complete a sample CSV through preview, confirm import, and report. Confirm only one step’s form is large; header still has theme and Sign out; no Profile.
2. Repeat with commercial mode **on**. Confirm Profile in the header, first-sign-in alert once, login gate when signed out, and retention gate if you can use a deleted test account.
3. After Story 3, on a wide viewport, confirm the summary rail stays visible while scrolling the centre pane.
4. Theme: auto / light / dark still apply.

If AppHost is already running and `web` was rebuilt:

```bash
aspire resource web rebuild
```

## Out of scope for this guide

- Changing Graph or CSV fixtures
- Restyling `/profile`
- Measuring exact pixel scroll length (qualitative “materially shorter” is enough)
