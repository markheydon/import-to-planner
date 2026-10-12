# Feature Specification: Complimentary Unlimited Credits for Allowlisted Tenants

**Feature Branch**: `019-complimentary-tenant-credits`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Add complimentary unlimited credits for a deploy-time allowlist of Entra tenant IDs on hosted commercial Import To Planner. Parent epic #124. Depends on the immutable per-tenant ledger, free allowance, and preview/execute gate (#126). Allowlisted tenants must never be blocked by the credit confirm gate and must not be asked to buy credits. Keep the existing ledger and import pipeline. Still record usage (1 credit per successfully created task) for reporting. Do not show remaining 0 as if they were exhausted; surface Unlimited/complimentary in the UI. Specify an auditable transaction pattern that never goes negative (e.g. matching complimentary grants, or meter-only usage rows). Update the credits-billing-usage-model doc."

**Traceability**: GitHub issue [#129](https://github.com/markheydon/import-to-planner/issues/129) (parent epic [#124](https://github.com/markheydon/import-to-planner/issues/124)). **Depends on** [#126](https://github.com/markheydon/import-to-planner/issues/126) (commercial credit ledger, free allowance, preview/execute gate). Product contract: `docs/credits-billing-usage-model.md` (must be updated for complimentary tenants). Paid checkout ([#125](https://github.com/markheydon/import-to-planner/issues/125)) must not be the only path for allowlisted tenants.

## Clarifications

### Session 2026-10-12

- Q: For an allowlisted organisation, should import execution ever stop starting new task creates solely because the shared credit balance reached zero? → A: Never stop for credit exhaustion; only non-credit failures may halt the run.
- Q: When the allowlist contains a token that is not a valid tenant GUID, how should the product treat the rest of the configuration? → A: Ignore invalid tokens only; log diagnostic; valid GUIDs in the same list still apply.
- Q: When should the product decide whether the signed-in organisation is on the complimentary allowlist? → A: On every credit-related action when commercial tenant context is resolved.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Operator-Designated Tenants Import Without Credit Walls (Priority: P1)

A person signs in to the hosted commercial product with an organisation whose tenant identifier appears on a small, operator-maintained allowlist configured at deployment. They run preview and confirm imports of any size supported by the product’s existing import rules. They are never blocked at Confirm import for insufficient credits, never see messaging that implies they have run out of credits, and are never steered to buy credits as the only way to proceed.

**Why this priority**: The operator and designated testers must use the hosted product without purchasing credits or fighting the 25-credit monthly cap. This is the core product promise of the story.

**Independent Test**: Configure one allowlisted tenant identifier, sign in as that organisation, preview an import whose would-create count exceeds 25 (and exceeds any prior usage), confirm import is enabled, execute successfully, and verify usage is still reported on the summary while remaining is presented as unlimited/complimentary.

**Acceptance Scenarios**:

1. **Given** the hosted commercial product is on and the signed-in organisation’s tenant identifier is on the allowlist, **When** preview reports a would-create count greater than 25 or greater than any normal monthly allowance, **Then** Confirm import is not disabled for credit reasons and no insufficient-credits warning is shown.
2. **Given** an allowlisted organisation, **When** the user confirms and executes an import, **Then** tasks are created through the same import pipeline as other commercial tenants, one credit of usage is recorded per successfully created task, and the run does not stop starting new creates solely because credits reached zero.
3. **Given** an allowlisted organisation after a successful import, **When** the user views the import summary, **Then** it shows tasks created and credits used for that run, and remaining credits are shown as unlimited or clearly complimentary (not numeric zero with Confirm disabled elsewhere).
4. **Given** an allowlisted organisation, **When** the user navigates credit-related UI in the import journey, **Then** there is no buy-credits call to action presented as required to continue importing.

---

### User Story 2 - Normal Commercial Tenants Are Unchanged (Priority: P1)

A person signs in with an organisation that is not on the allowlist. They receive the standard monthly free allowance, preview warnings, confirm gate, execution charging, and (when available) paid-credit purchase paths exactly as defined for the credit ledger feature ([#126](https://github.com/markheydon/import-to-planner/issues/126)).

**Why this priority**: Complimentary access is a narrow operator control. Default commercial behaviour must not regress.

**Independent Test**: With an empty allowlist or a list that excludes the test tenant, repeat insufficient-credit preview and confirm-disable scenarios from the ledger feature and confirm outcomes match [#126](https://github.com/markheydon/import-to-planner/issues/126).

**Acceptance Scenarios**:

1. **Given** the allowlist is empty or does not include the signed-in organisation’s tenant identifier, **When** preview would-create exceeds remaining credits, **Then** the insufficient-credits warning and disabled Confirm import behave as in [#126](https://github.com/markheydon/import-to-planner/issues/126).
2. **Given** a non-allowlisted commercial organisation, **When** credits are exhausted during execution, **Then** the product stops starting new creates and reports credit exhaustion as in [#126](https://github.com/markheydon/import-to-planner/issues/126).
3. **Given** a non-allowlisted organisation, **When** paid credit purchase is available in the product, **Then** buy-credits paths remain available where the ledger feature and paid-credits feature define them.

---

### User Story 3 - Operator Configures the Allowlist Safely (Priority: P2)

A platform operator deploys or updates the hosted commercial environment. They supply a comma-separated list of organisation tenant identifiers (GUIDs) without embedding production identifiers in source control. An empty list means no complimentary tenants. Malformed entries do not partially match real tenants.

**Why this priority**: Complimentary access must be deploy-time policy, auditable and safe, not hard-coded identity in application source.

**Independent Test**: Deploy with empty list, whitespace-padded list, invalid tokens, and a single valid GUID; verify only exact GUID matches receive complimentary behaviour and invalid tokens are ignored with operator-visible diagnostics (not silent broad allow).

**Acceptance Scenarios**:

1. **Given** the allowlist configuration is empty, **When** any commercial tenant uses the product, **Then** no tenant receives complimentary unlimited treatment.
2. **Given** the allowlist contains trimmed valid tenant GUIDs separated by commas, **When** a user signs in whose organisation tenant identifier equals one of those GUIDs, **Then** that organisation is treated as complimentary for the session and credit flows.
3. **Given** the allowlist contains an entry that is not a valid GUID alongside at least one valid GUID, **When** the product loads configuration, **Then** the invalid entry is ignored with a logged diagnostic, valid entries still grant complimentary access, and no tenant is matched by substring or partial string comparison.
4. **Given** commercial capabilities are off (self-hosted or commercial disabled), **When** any user imports, **Then** the allowlist is unused and credit UI and ledger behaviour remain absent as today.

---

### User Story 4 - Usage Remains Auditable on the Ledger (Priority: P1)

An operator or support reviewer needs to see how much complimentary tenants actually imported. Each successful task creation during execution still produces explicit ledger usage equal to one credit per task, without maintaining a separate “no metering” import path.

**Why this priority**: Unlimited access must not erase operational visibility into real usage.

**Independent Test**: Execute two imports for an allowlisted tenant; verify ledger shows usage transactions totalling created tasks and that derived balances never go negative.

**Acceptance Scenarios**:

1. **Given** an allowlisted organisation completes an import that creates N tasks, **When** ledger history is inspected, **Then** N credits of import usage are recorded with the same semantics as non-allowlisted tenants (one per successful create).
2. **Given** any sequence of complimentary imports, **When** balances are derived from the ledger, **Then** the numeric derived balance never goes below zero.
3. **Given** complimentary usage is recorded, **When** matching complimentary grant transactions are applied per the chosen ledger pattern, **Then** each usage increment has a corresponding auditable complimentary grant so finite-lot arithmetic stays non-negative.

---

### Edge Cases

- Allowlist updated at deploy time: complimentary status is re-evaluated on the next credit-related action (sign-in balance refresh, preview, confirm, or execute) using the current configuration; users need not sign out solely for an allowlist change to take effect.
- Same organisation has users in multiple sessions: complimentary status is tenant-level; all users in that organisation share the same complimentary metering and UI.
- Allowlisted tenant with would-create of zero: Confirm import is not blocked for credits; no usage on execution if nothing is created.
- Preview still never consumes credits for allowlisted or normal tenants.
- Concurrent imports in one allowlisted organisation: confirm gate never blocks for credits; execution still records usage per create; no negative derived balance; no import stops solely because shared remaining credits reached zero (only non-credit failures halt creates).
- Allowlisted tenant when ledger read fails: preview, confirm, and execute fail closed with a friendly error as in [#126](https://github.com/markheydon/import-to-planner/issues/126); remaining MUST NOT be shown as numeric zero implying exhaustion.
- Duplicate GUIDs or extra commas in the allowlist: duplicates are harmless; empty segments are ignored after trim.
- Allowlist string contains both garbage tokens and valid GUIDs: valid GUIDs still apply; garbage is logged and ignored.
- A tenant removed from the allowlist on redeploy: on the next credit-related action they follow normal credit rules including confirm gate and monthly allowance, without requiring a new sign-in solely for that change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Complimentary unlimited treatment MUST apply only when hosted commercial capabilities are on. Self-hosted and commercial-off deployments MUST NOT read the allowlist for credit behaviour and MUST remain unchanged.
- **FR-002**: Operators MUST be able to configure a deploy-time allowlist of organisation tenant identifiers as a comma-separated list of GUIDs (trimmed entries; empty segments ignored). Default MUST be an empty list (no complimentary tenants).
- **FR-003**: Complimentary status MUST be determined by exact GUID equality between the signed-in commercial organisation tenant identifier and an allowlist entry. Matching MUST NOT use substring, contains, or case folding that could widen access. The allowlist MUST be evaluated against the current configuration on every credit-related action when commercial tenant context is resolved (sign-in balance refresh, preview, confirm, and execute), not only at process start and not only at sign-in.
- **FR-004**: Production tenant identifiers MUST NOT be committed in application source; only the configuration mechanism and empty default are documented for operators.
- **FR-005**: For allowlisted organisations, the credit confirm gate MUST NOT disable or refuse Confirm import for insufficient credits, including when would-create exceeds 25 or exceeds any monthly free allowance.
- **FR-006**: For allowlisted organisations, preview MUST still run and MUST NOT consume credits. Insufficient-credit warnings and shortfall messaging MUST NOT appear.
- **FR-007**: For allowlisted organisations, execution MUST use the existing import pipeline. Credits MUST still be recorded as one usage per successfully created task, with the same rules for skipped, reused, and failed rows as [#126](https://github.com/markheydon/import-to-planner/issues/126). Execution MUST NOT stop starting new task creates solely because remaining credits reached zero; only non-credit failures (for example Planner errors or unrecoverable ledger write failures per [#126](https://github.com/markheydon/import-to-planner/issues/126)) may halt further creates.
- **FR-008**: The product MUST NOT introduce a parallel import path that bypasses the ledger for complimentary tenants.
- **FR-009**: Ledger accounting for complimentary tenants MUST remain append-only and auditable. Derived balances MUST never be negative. The chosen pattern is: record import usage as today, and for each usage amount record a same-size **complimentary grant** transaction so finite-lot derived balance returns to zero while usage remains visible in history and summaries.
- **FR-010**: For allowlisted organisations, monthly free credit grants and free-credit expiry processing MUST be skipped. Complimentary status MUST take precedence over the 25-credit monthly cap and expiry rules.
- **FR-011**: Remaining credits shown in the import journey for allowlisted organisations MUST be presented as unlimited or clearly complimentary UK English copy (for example “Unlimited” with optional “complimentary” context). The UI MUST NOT present numeric zero remaining with Confirm disabled as if credits were exhausted.
- **FR-012**: Import summaries for allowlisted organisations MUST still show tasks created and credits used for that run. Credits used MUST reflect actual created tasks.
- **FR-013**: Buy-credits calls to action MUST be hidden or omitted for allowlisted organisations wherever the product would otherwise prompt purchase when credits are insufficient ([#125](https://github.com/markheydon/import-to-planner/issues/125) must not offer checkout as the only path for these tenants).
- **FR-014**: Non-allowlisted commercial organisations MUST retain all behaviours from [#126](https://github.com/markheydon/import-to-planner/issues/126) and later paid-credit features without change except where those features explicitly reference complimentary tenants.
- **FR-015**: Malformed allowlist entries MUST be ignored individually with operator-visible diagnostics (for example structured logs); valid GUIDs in the same configuration string MUST still apply. Malformed entries MUST NOT grant complimentary access and MUST NOT cause the entire allowlist to be discarded or block startup.
- **FR-016**: If grant, usage, complimentary grant, expiry, or balance read fails for any tenant, preview, confirm, and execute MUST fail closed with UK English errors as in [#126](https://github.com/markheydon/import-to-planner/issues/126); the product MUST NOT treat ledger failure as insufficient credits.
- **FR-017**: The product contract document `docs/credits-billing-usage-model.md` MUST be updated to describe complimentary allowlisted tenants, configuration expectations, ledger metering, UI presentation, and interaction with monthly free credits and paid credits.

### Key Entities

- **Complimentary allowlist**: Operator-supplied set of organisation tenant GUIDs effective at deployment; empty by default.
- **Complimentary organisation**: A commercial tenant whose identifier is on the allowlist; shares one ledger and complimentary status for all its users.
- **Complimentary grant transaction**: Ledger entry that offsets import usage for complimentary organisations so derived finite balance stays non-negative while preserving usage visibility.
- **Import usage record**: Same as [#126](https://github.com/markheydon/import-to-planner/issues/126)—one credit per successfully created task during execution.
- **Unlimited remaining presentation**: User-visible remaining balance state for complimentary organisations, distinct from numeric exhaustion at zero.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of tested allowlisted-tenant previews where would-create exceeds 25, Confirm import is not disabled for credit reasons and no insufficient-credits warning is shown.
- **SC-002**: In 100% of tested allowlisted-tenant executions, credits used on the import summary equals the count of successfully created tasks, and the run does not stop starting new creates solely because remaining credits reached zero.
- **SC-003**: In 100% of tested allowlisted-tenant import summaries after execution, remaining is presented as unlimited or complimentary, never as numeric zero with exhausted-credit framing.
- **SC-004**: In 100% of tested ledger inspections for allowlisted tenants, total import usage is visible and derived finite balance never goes below zero across arbitrary import sequences.
- **SC-005**: In 100% of tested non-allowlisted tenants (including empty allowlist), credit confirm gate, monthly allowance, and exhaustion behaviour match [#126](https://github.com/markheydon/import-to-planner/issues/126) acceptance outcomes.
- **SC-006**: In 100% of tested configurations mixing invalid tokens with valid GUIDs, only tenants whose identifiers exactly match a valid configured GUID receive complimentary access, invalid tokens are ignored with diagnostics, and valid entries remain effective.
- **SC-007**: In 100% of tested commercial-off journeys, allowlist configuration does not surface credit UI or complimentary behaviour.
- **SC-008**: Operators can configure the allowlist from documented deployment settings alone without changing application source.

## Assumptions

- The immutable per-tenant ledger, preview/execute gate, and monthly free allowance from [#126](https://github.com/markheydon/import-to-planner/issues/126) are implemented and stable before this feature ships.
- Organisation tenant identity for commercial sign-in is already available and stable for GUID comparison.
- A small allowlist (operator plus a few testers) is sufficient for V1; there is no per-user allowlist.
- Skipping monthly free grant and expiry for complimentary tenants reduces noise and avoids polluting balances; usage plus matching complimentary grants is enough for reporting.
- “Unlimited” is a presentation and gating policy; underlying ledger still records each task for audit.
- Paid credit lots ([#125](https://github.com/markheydon/import-to-planner/issues/125)) are irrelevant for allowlisted tenants in V1 because they never need to purchase; complimentary grants offset usage instead of consuming free or paid lots.
- Configuration reload follows existing deployment configuration practices; complimentary status follows the live configuration on each credit-related action. No in-app operator UI for editing the allowlist is required in this story.

## Out of Scope

- A public “unlimited plan” SKU or Stripe product for unlimited credits.
- Per-user allowlisting (tenant-level only).
- Hard-coding operator tenant identifiers in source.
- Changing self-hosted import behaviour or enabling credits when commercial is off.
- A second import pipeline without ledger usage.
- In-application UI for operators to edit the allowlist.
- Folding operator identity into application code as a special case beyond reading the configured GUID list.

## Dependencies

- **Blocked by / requires**: [#126](https://github.com/markheydon/import-to-planner/issues/126) credit ledger, free monthly allowance, preview and execute gate.
- **Parent**: [#124](https://github.com/markheydon/import-to-planner/issues/124) commercial credits and billing epic.
- **Related**: [#125](https://github.com/markheydon/import-to-planner/issues/125) paid credits must hide or skip buy CTAs for allowlisted tenants.
- **Documentation**: `docs/credits-billing-usage-model.md` update is part of this feature’s delivery.
