# Wizard Layout UI Contract

## Purpose

This contract describes the Home import surface after the stepper layout refactor. It is a UI contract, not an HTTP API. Implementers and bUnit tests should treat the regions, visibility rules, and labels below as the observable interface.

## Page regions

When the layout mode is `ImportWizard`:

| Region | Required content | Must not contain |
|--------|------------------|------------------|
| Header (full width) | Title “CSV to Planner Import”, compact CSV guidance (required **Task Name**; accepted Task Name, Description, Priority, Bucket, Goal; one manual-follow-up example), signed-in email and tenant name when present, icon actions for theme and commercial profile (`/profile`), Sign out | Step forms, stepper, summary rail |
| Step list | Five steps in order with titles below; completion and lock cues | Duplicate full forms for all five steps |
| Working pane | Detailed content for `viewedStep` only (plus collapsed setup panels from Story 3) | Always-expanded copies of inactive step forms |
| Summary rail (Story 3, wide viewport) | Location, plan, CSV file, preview/execution status; Open in Planner when a plan id exists | Sign-in or Profile (those stay in the header) |

When layout mode is `CommercialLoginGate`:

- Header title: **Sign in to Import To Planner**
- Existing first-sign-in / returning-user copy
- Sign in control
- **Must not** render the step list, working pane, or summary rail
- **Must not** contain “CSV to Planner Import” or “Select Planner location”

When layout mode is `CommercialDeletedAccountGate`:

- Header title: **Access is paused**
- Retention copy, optional restore status, restore-until date when present
- **Restore account**, **Open profile** (`/profile`), **Sign out**
- **Must not** render the step list, working pane, or summary rail

## Step list contract

Order and titles:

1. Select Planner location
2. Select plan
3. Upload CSV
4. Preview and confirm
5. Report

Rules:

- Visible titles must not include redundant `Step X` text.
- Completed steps show a completion cue and may show compact summary text (location, plan, file, import completed, report available).
- Step 4 completes after import runs; until then it may show interim summary text such as “Preview ready — confirm to import.” when a valid preview exists.
- Step 5 is locked until import has run; it shows the execution report only.
- Locked steps are not selectable.
- Signed-in commercial header order: email and tenant name, then icon actions (theme menu and profile control), then Sign out.
- The commercial profile control MUST link to `/profile` and MUST expose an accessible name of “Profile” (for example `aria-label`), whether rendered as text or an icon.
- Primary actions in the working pane remain **Preview import** and **Confirm import** on step 4 (sentence case). Built-in stepper Next/Previous/Reset must not appear as competing primary actions.

## Workflow gating contract (unchanged meaning)

- Preview remains unavailable until location, plan, and CSV content are present and the page is not busy (`canValidate`).
- Confirm remains unavailable until a current, non-stale preview matches the current selection (`canExecute`).
- Stale-preview warning copy remains: preview is stale because Planner state changed; generate a fresh preview before import.
- Tenant mismatch, admin consent, and unsupported-account alerts remain visible in the import-wizard branch, above the stepper.

## Identity and commercial chrome

| Control | Self-host | Commercial |
|---------|-----------|------------|
| Theme (auto / light / dark) | Yes | Yes |
| Email | Yes when signed in | Yes when signed in |
| Tenant display name | When available | When available |
| Profile → `/profile` | No | Yes when signed in with an active account — icon control with accessible name “Profile” (and **Open profile** on the retention gate) |
| Sign out / Sign in | Yes | Yes |
| Account-created success alert | No | Yes after first successful commercial sign-in, **once**, above the wizard |

`/profile` route behaviour is out of scope except that Home links must still resolve to it.

## Alert duplication

A given condition MUST appear in at most one `MudAlert` (or equivalent banner) at a time. Do not repeat the first-sign-in success message inside a step pane.

## Test observables

bUnit should be able to assert:

- Presence or absence of the five step titles.
- Presence of a single stepper host when `ImportWizard`; absence when either commercial gate is shown.
- Profile href `/profile` if and only if commercial mode is on and the signed-in wizard (or retention Open profile) is shown as specified today.
- Existing workflow progression still enables Preview then Confirm under the same stubs as `HomePageWorkflowTests`.

Do not require `.step-card` CSS class names after this feature.
