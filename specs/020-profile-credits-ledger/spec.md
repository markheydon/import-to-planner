# Feature Specification: Profile Credits and Ledger Activity

**Feature Branch**: `020-profile-credits-ledger`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Add a credits section on the commercial profile page (/profile) for hosted Import To Planner. Parent epic #124. Depends on the immutable per-tenant credit ledger (#126). Product contract: docs/credits-billing-usage-model.md. Show remaining credits after Ensure, current free-lot remaining and month-end expiry, and a ledger activity report. Group usage by import run (not one row per created task). Include grants and expiries now; paid purchases when #125 exists. Do not add Home header wallet chrome. Do not use Stripe for usage metering or as the user-facing statement. Fail closed with friendly UK English if the ledger cannot be read; do not show remaining 0. Commercial mode only. Self-host unchanged. Coordinate remaining copy with unlimited allowlisted tenants (#129) so they show Unlimited, not exhausted 0. Hide buy-credits for those tenants when #125 ships."

**Traceability**: GitHub issue [#138](https://github.com/markheydon/import-to-planner/issues/138) (parent epic [#124](https://github.com/markheydon/import-to-planner/issues/124)). **Depends on** [#126](https://github.com/markheydon/import-to-planner/issues/126) (commercial credit ledger, free allowance, preview/execute gate). **Coordinates with** [#129](https://github.com/markheydon/import-to-planner/issues/129) (complimentary unlimited tenants) and [#125](https://github.com/markheydon/import-to-planner/issues/125) (paid credits and checkout). Product contract: `docs/credits-billing-usage-model.md`.

## Clarifications

### Session 2026-10-12

- Q: For organisations on the complimentary unlimited allowlist, what should the profile credits section show besides remaining credits and activity? → A: Show only Unlimited (or equivalent) and activity; no free-monthly or paid lot breakdown.
- Q: How much credit activity history should `/profile` show by default? → A: Rolling 12 months of activity, newest first, with visible copy when older history is omitted.
- Q: For complimentary allowlisted organisations, which event types should appear in the profile activity list (within the twelve-month window)? → A: Grouped import usage only (no grant, expiry, purchase, or complimentary-grant ledger lines on profile).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Commercial User Checks Remaining Credits on Profile (Priority: P1)

A person signed in to the hosted commercial product opens the account profile screen (profile icon, `/profile`). They see how many import credits their organisation has left after the same balance rules used elsewhere in the product (including any due monthly grant or free-credit expiry). They do not need to start or finish an import to learn their balance.

**Why this priority**: Without a stable account surface for balance, users only discover credits during import warnings or summaries. Profile is the natural place to check organisation entitlement between imports.

**Independent Test**: Sign in as a commercial user with a known ledger state, open `/profile`, and confirm remaining credits match the ledger-derived balance after balance ensure. Repeat after another user in the same organisation consumes credits and confirm the profile reflects the shared tenant balance.

**Acceptance Scenarios**:

1. **Given** hosted commercial mode is on and the user is signed in, **When** they open `/profile`, **Then** the credits section shows the organisation’s remaining credits after applying the same ensure rules as preview (due expiry and monthly grant if applicable).
2. **Given** two users belong to the same commercial organisation, **When** either opens `/profile`, **Then** both see the same remaining figure for that organisation.
3. **Given** the ledger cannot be read or ensure cannot complete, **When** the user opens `/profile`, **Then** the credits section shows a friendly UK English error and does not show remaining credits as zero or as if the organisation were exhausted.
4. **Given** hosted commercial mode is off or the deployment is self-hosted, **When** the user opens `/profile`, **Then** no credits section or credit copy appears (existing profile content otherwise unchanged).

---

### User Story 2 - User Understands the Current Free Monthly Lot (Priority: P1)

On `/profile`, a commercial user who is not on the complimentary allowlist sees details of the organisation’s current free monthly credit lot: how many were granted this month, how many remain in that lot, and when unused free credits expire (end of the current calendar month in UTC, consistent with the product contract).

**Why this priority**: Remaining total alone does not explain monthly allowance, partial use, or expiry. The free lot is the primary entitlement most tenants rely on before paid credits exist.

**Independent Test**: With a tenant that received this month’s grant and partially used it, open `/profile` and confirm granted quantity, lot remaining, and expiry align with ledger lots. Cross month-end with a balance-needed action and confirm the profile shows the new month’s lot after ensure.

**Acceptance Scenarios**:

1. **Given** the organisation has an active free monthly lot for the current UTC month, **When** the user views `/profile`, **Then** they see granted quantity, remaining in that lot, and month-end expiry for free credits.
2. **Given** the organisation has not yet received this month’s grant, **When** the user opens `/profile`, **Then** ensure runs and the profile reflects the new grant and lot details consistent with the ledger.
3. **Given** leftover free credits from the prior month, **When** ensure applies expiry on profile load, **Then** the profile shows updated remaining and lot details that include the expiry in activity (see User Story 3) without implying credits rolled over.

---

### User Story 3 - User Reviews Organisation Credit Activity (Priority: P2)

On `/profile`, a commercial user sees a readable activity report for their organisation’s credit ledger: monthly grants, free-credit expiries, and import usage. Usage is grouped by import run (one line per run with total credits used), not one line per created task. Entries are labelled in UK English. Payment-provider customer identifiers and technical diagnostics are not shown.

**Why this priority**: Transparency and trust require a statement-style history, not only a headline balance. Grouping by import run keeps the report usable when a single run creates many tasks.

**Independent Test**: Seed a tenant with a grant, an expiry across month boundary, and two import runs with different created counts; open `/profile` and confirm activity order, types, quantities, and per-run usage totals match the ledger.

**Acceptance Scenarios**:

1. **Given** the organisation has ledger grants and expiries, **When** the user views credit activity on `/profile`, **Then** those events appear with understandable labels and quantities that match the ledger.
2. **Given** the organisation completed one or more imports that consumed credits, **When** the user views activity, **Then** each import run appears as a single usage entry with the total credits used for that run, not one entry per task.
3. **Given** multiple activity types exist, **When** the user reviews the list, **Then** entries are ordered newest-first so recent usage is easy to find.
4. **Given** the organisation has ledger events older than twelve months, **When** the user views activity, **Then** only events from the rolling last twelve months are listed and UK English copy explains that older history is not shown on this page.
5. **Given** activity cannot be loaded from the ledger, **When** the user opens `/profile`, **Then** they see a friendly UK English error for activity (and balance handling follows User Story 1 fail-closed rules).

---

### User Story 4 - Complimentary Tenants See Unlimited, Not Exhaustion (Priority: P1)

A user whose organisation is on the operator’s complimentary unlimited allowlist opens `/profile`. Remaining credits are presented as unlimited or clearly complimentary, never as numeric zero with messaging that implies they must buy credits to continue importing.

**Why this priority**: Allowlisted tenants still record usage on the ledger; the profile must not undermine the unlimited import promise from the complimentary-credits feature.

**Independent Test**: Configure an allowlisted tenant, run imports that record usage, open `/profile`, and confirm remaining shows unlimited/complimentary while activity still reflects usage grouped by run.

**Acceptance Scenarios**:

1. **Given** the signed-in organisation is on the complimentary allowlist, **When** the user opens `/profile`, **Then** remaining credits are shown as unlimited or equivalent complimentary wording, not as 0 with exhausted semantics.
2. **Given** an allowlisted organisation with recorded import usage, **When** the user views activity on `/profile`, **Then** only grouped import usage rows appear (no grant, expiry, purchase, or complimentary-grant lines) and per-run totals match ledger usage for that run.
3. **Given** an allowlisted organisation, **When** paid credit purchase is available in the product ([#125](https://github.com/markheydon/import-to-planner/issues/125)), **Then** `/profile` does not show buy-credits as required for that organisation.
4. **Given** an allowlisted organisation, **When** the user views `/profile`, **Then** the credits section shows unlimited or complimentary remaining and activity only, with no free-monthly lot breakdown and no paid lot breakdown.

---

### User Story 5 - Paid Credit Lots and Purchases Extend the Same Profile Section (Priority: P3)

After paid credits ship ([#125](https://github.com/markheydon/import-to-planner/issues/125)), a commercial user who is not on the complimentary allowlist opens `/profile`. They see paid credit lots with remaining quantity and expiry (twelve months from purchase per product contract), purchase events in activity, and an optional buy-credits entry on profile. Allowlisted tenants do not see buy-credits on profile.

**Why this priority**: Profile is the long-term account surface; paid lots must extend the same section without a separate wallet page or payment-provider statement.

**Independent Test**: With paid lots and a completed purchase on the ledger, open `/profile` for a non-allowlisted tenant and confirm lot breakdown, purchase activity, and buy entry; repeat for allowlisted tenant and confirm buy is hidden.

**Acceptance Scenarios**:

1. **Given** the organisation holds paid credit lots, **When** a non-allowlisted user views `/profile`, **Then** each paid lot with remaining balance shows remaining quantity and expiry date consistent with the ledger.
2. **Given** a successful paid credit purchase was recorded on the ledger, **When** the user views activity, **Then** the purchase appears with a user-facing label (not a payment-provider customer id).
3. **Given** paid checkout is available and the organisation is not allowlisted, **When** the user views `/profile`, **Then** they may open buy-credits from the credits section without a persistent header wallet on Home.
4. **Given** paid checkout is available and the organisation is allowlisted, **When** the user views `/profile`, **Then** buy-credits is not shown.

---

### Edge Cases

- User opens `/profile` at UTC month-end boundary: ensure applies expiry and grant before display; profile figures match a subsequent preview balance.
- Organisation has only paid lots (no free lot remaining) after [#125]: profile still shows correct total remaining and paid lot rows; free lot section reflects zero remaining or absence honestly.
- Organisation has never imported (non-complimentary): activity shows grants (and expiries if applicable) but no usage rows; allowlisted organisations with no imports show an empty activity list (no grant or complimentary-grant lines).
- Organisation with more than twelve months of ledger history: activity lists the rolling last twelve months only; UK English copy states that older events are omitted from this view (not silently dropped).
- Very large history within the twelve-month window: activity remains readable (grouped usage per run); pagination within the window is acceptable if needed, with clear indication when more pages exist.
- Concurrent imports while viewing profile: balance on load reflects ensure at load time; a refresh or revisit shows updated remaining (no requirement for live websocket updates in this story).
- Ledger read succeeds for balance but activity list fails: balance and activity fail independently with friendly errors; balance does not default to zero when only activity fails.
- Sign-in succeeds when ensure fails elsewhere: opening profile retries ensure; same fail-closed rules as import balance-needed actions.
- Self-hosted or commercial off: profile never queries or displays credits even if legacy rows existed in a misconfigured environment (commercial off must not surface credit UI).
- Complimentary allowlisted organisation: profile shows unlimited remaining and activity only; lot breakdown sections are omitted even if paid lots exist on the ledger later ([#125](https://github.com/markheydon/import-to-planner/issues/125)).
- Allowlisted organisation with complimentary-grant ledger rows: profile activity lists grouped import usage only; complimentary-grant (and other non-usage) ledger lines are not shown as separate activity rows.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A credits section MUST appear on `/profile` only when hosted commercial mode is on. Self-hosted and commercial-off MUST NOT show credit copy on `/profile` or add credit chrome elsewhere (including no persistent credit wallet on Home).
- **FR-002**: Profile credit data MUST be scoped to the signed-in commercial organisation (shared tenant ledger). Users MUST NOT see another organisation’s balance or activity.
- **FR-003**: Remaining credits on `/profile` MUST be derived from the immutable per-tenant ledger after the same ensure behaviour used when a current balance is needed for import (due free-credit expiry and monthly grant per [#126](https://github.com/markheydon/import-to-planner/issues/126)). The profile MUST NOT maintain a separate mutable balance counter as the source of truth.
- **FR-004**: If ensure or live remaining balance cannot be determined, `/profile` MUST fail closed: show a friendly UK English error, MUST NOT display remaining as zero, and MUST NOT present insufficient-credits or buy-credits messaging as a substitute for a read failure.
- **FR-005**: For organisations that are not on the complimentary allowlist, `/profile` MUST show the organisation’s current free monthly lot: granted quantity for the active month, remaining in that lot, and expiry at UTC calendar month end, consistent with `docs/credits-billing-usage-model.md`. Allowlisted organisations MUST NOT see a free-monthly or paid lot breakdown (only unlimited or complimentary remaining plus activity).
- **FR-006**: `/profile` MUST list organisation credit activity sourced from the ledger for the **rolling last twelve months**, newest first. For organisations not on the complimentary allowlist, activity MUST include free monthly grants, free-credit expiries, import usage grouped by import run with per-run credit totals, and (when [#125](https://github.com/markheydon/import-to-planner/issues/125) exists) purchase events and paid credit lot expiries recorded on the ledger. For allowlisted organisations, activity MUST include **only** import usage grouped by import run (no grant, expiry, purchase, paid lot expiry, or complimentary-grant lines). When events older than twelve months exist, the UI MUST include UK English copy that older history is not shown on this page. Labels MUST be UK English and suitable for end users.
- **FR-007**: Activity MUST NOT list one row per successfully created task for import usage; one import run MUST map to one usage summary row (or equivalent grouped presentation) with the total credits consumed for that run.
- **FR-008**: Activity MUST NOT expose payment-provider customer identifiers, internal tenant diagnostics, or stack traces in the user interface.
- **FR-009**: Credit activity and balances on `/profile` MUST NOT use payment-provider usage metering or the payment provider’s customer portal as the user-facing statement; the application ledger remains the source of truth for display.
- **FR-010**: For organisations on the complimentary unlimited allowlist ([#129](https://github.com/markheydon/import-to-planner/issues/129)), `/profile` MUST show remaining as unlimited or clearly complimentary, MUST NOT show numeric zero as if credits were exhausted, and MUST NOT imply purchase is required to import.
- **FR-011**: When paid credits ([#125](https://github.com/markheydon/import-to-planner/issues/125)) are available, `/profile` MUST show paid lots with remaining quantity and twelve-month expiry for organisations that are not on the complimentary allowlist, MUST include purchase and paid lot expiry events in activity for non-allowlisted organisations only, and MUST offer buy-credits only for non-allowlisted organisations. Allowlisted organisations MUST NOT see paid lot breakdown rows, purchase lines, or paid lot expiry lines on profile activity.
- **FR-012**: This feature MUST NOT change preview, confirm, or execute credit gate behaviour defined in [#126](https://github.com/markheydon/import-to-planner/issues/126).
- **FR-013**: This feature MUST NOT add operator-only ledger browser functionality; `/profile` is the end-user account surface.
- **FR-014**: The product MUST provide a commercial read path to load ledger balance (after ensure) and list activity suitable for profile display, without editing historical ledger transactions.

### Key Entities

- **Organisation credit balance (derived)**: Remaining import credits for the signed-in commercial tenant, calculated from ledger transactions and lots after ensure.
- **Free monthly credit lot**: The current month’s free allocation with granted amount, remaining quantity, and UTC month-end expiry.
- **Paid credit lot** (when [#125](https://github.com/markheydon/import-to-planner/issues/125) exists): A purchased allocation with remaining quantity and purchase-based expiry.
- **Profile activity entry**: A user-facing row derived from one or more ledger transactions with date, type label, and quantity. Non-complimentary tenants may see grants, expiries, purchases (when [#125](https://github.com/markheydon/import-to-planner/issues/125) exists), and grouped import usage per run. Complimentary allowlisted tenants see **only** grouped import usage rows (FR-006).
- **Import run usage summary**: The total credits consumed by one import execution, aggregated for activity display.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of tested cases, a signed-in commercial user can open `/profile` and see remaining credits matching the ledger-derived balance after ensure without running an import.
- **SC-002**: In 100% of tested ledger-read or ensure failures on profile, remaining is not shown as zero and the user sees a friendly UK English error.
- **SC-003**: In 100% of tested tenants with a current free lot, `/profile` shows granted amount, lot remaining, and month-end expiry consistent with the ledger.
- **SC-004**: In 100% of tested organisations with import history in the last twelve months, every import run in that window that consumed credits appears exactly once in profile activity with a total matching ledger usage for that run; when older history exists, profile copy indicates the twelve-month limit.
- **SC-005**: In 100% of tested commercial-off and self-hosted journeys, `/profile` shows no credits section and Home shows no new persistent credit wallet.
- **SC-006**: In 100% of tested allowlisted organisations, `/profile` shows unlimited or complimentary remaining, grouped import usage as the only activity rows, and no free-monthly or paid lot breakdown sections.
- **SC-007**: After paid credits are available, in 100% of tested non-allowlisted tenants with purchases, profile activity includes purchase entries and paid lot expiry details; allowlisted tenants never see buy-credits on profile in tests.

## Assumptions

- The commercial credit ledger, ensure rules, and import usage recording from [#126](https://github.com/markheydon/import-to-planner/issues/126) are implemented and stable before this story ships.
- `/profile` already exists for account metadata (tenant/user identifiers, retention, delete); this story adds a credits section without relocating those controls.
- Complimentary allowlist behaviour is defined in [#129](https://github.com/markheydon/import-to-planner/issues/129) and the product contract; profile copy aligns with import journey “Unlimited” presentation.
- Paid credit lots and checkout ([#125](https://github.com/markheydon/import-to-planner/issues/125)) may ship after this story; requirements FR-011 and User Story 5 apply when that feature is present, without blocking grants/expiry/usage activity for the initial profile release.
- Import runs are already identifiable in the ledger or can be correlated for grouping; if a run identifier is missing for legacy rows, the product presents the best available user-facing grouping without per-task rows.
- UTC month boundaries for free lot expiry match [#126](https://github.com/markheydon/import-to-planner/issues/126) and the billing usage model.

## Out of Scope

- Payment-provider usage metering or using invoices as the credits statement.
- Persistent credit wallet or balance chip on Home (FR-028 from [#126](https://github.com/markheydon/import-to-planner/issues/126) remains out of scope).
- Operator-facing ledger browser or support tools substituting for user profile.
- Changing preview, confirm, execute, or import summary credit behaviour.
- Credits, billing copy, or ledger writes for self-hosted deployments.
- Subscriptions, automatic top-up, or scheduled imports.

## Dependencies

- **Depends on**: [#126](https://github.com/markheydon/import-to-planner/issues/126) (ledger, ensure, preview/execute gate).
- **Parent**: [#124](https://github.com/markheydon/import-to-planner/issues/124) (commercial credits and billing).
- **Coordinates with**: [#129](https://github.com/markheydon/import-to-planner/issues/129) (unlimited/complimentary presentation and buy-credits hiding).
- **Extends when available**: [#125](https://github.com/markheydon/import-to-planner/issues/125) (paid lots, purchases, buy-credits entry on profile).
