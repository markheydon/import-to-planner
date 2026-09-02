# Feature Specification: Commercial Credit Ledger

**Feature Branch**: `011-commercial-credit-ledger`

**Created**: 2026-09-02

**Status**: Draft

**Input**: User description: "Add commercial credit accounting for hosted Import To Planner (commercial mode on only). Product contract: docs-internal/credits-billing-usage-model.md and GitHub issue #126. Immutable per-tenant credit ledger; derive balance from transactions; free monthly grants, import usage, and free-credit expiry; design lots/types so paid purchases can be added later. Grant 25 free credits lazily on first successful commercial login each calendar month; no prorate; timestamp at that login. Expire leftover free credits at calendar month end with an explicit ledger transaction (lazy on next balance-needed login). No rollover. No job that grants credits to dormant tenants. 1 credit = 1 successfully created Planner task. Preview and validation never consume credits. On preview, if would-create exceeds remaining balance, show a prominent UK English warning (N, M, shortfall) and disable Confirm import. No overage. During execute, consume only created tasks; if balance hits zero, stop starting new creates. Show created count, credits used, and remaining credits on the import summary. Self-hosted and commercial mode off unchanged."

**Traceability**: GitHub issue [#126](https://github.com/markheydon/import-to-planner/issues/126) (parent epic [#124](https://github.com/markheydon/import-to-planner/issues/124)). Product contract: `docs-internal/credits-billing-usage-model.md`. Paid purchases are a later feature ([#125](https://github.com/markheydon/import-to-planner/issues/125)) and MUST NOT ship in this increment.

## Clarifications

### Session 2026-09-02

- Q: If two people in the same organisation confirm imports at the same time, how should the shared credit balance be protected so created tasks never exceed remaining credits? → A: Re-check live remaining credits at Confirm import; disable or refuse confirm if would-create now exceeds remaining; each running import still stops new creates at zero.
- Q: If someone stays signed in across the end of a calendar month, when should leftover free credits expire and the new month’s 25 be granted? → A: On sign-in and whenever a current balance is needed (preview, confirm, execute): expire leftovers for the new month, then grant 25 if not already granted this month.
- Q: If the product cannot apply a due grant or expiry, or cannot read the live remaining balance, what should happen to sign-in, preview, confirm, and execute? → A: Fail the balance-needed action (preview, confirm, execute) with a friendly error; do not import, do not record a grant, do not pretend remaining is zero. Sign-in may succeed as a session.
- Q: If a Planner task is created successfully but the product then cannot record the credit usage, what should happen? → A: Keep the created task, retry recording its usage before the next create, then stop the run with an error if recording still fails; do not delete the Planner task.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tenant Receives a Fair Monthly Free Allowance (Priority: P1)

A person using the hosted commercial product signs in with their organisation. The first time that organisation needs a current balance in a calendar month (successful sign-in, or preview, confirm, or execute if they stayed signed in), they receive 25 free import credits. Repeating those actions in the same month does not add another 25. Unused free credits do not carry into the next month, even if the user never signs out.

**Why this priority**: Without a trustworthy allowance and an auditable balance, the hosted product cannot meter usage fairly and cannot later sell extra credits. This is the foundation of commercial accounting.

**Independent Test**: Sign in a commercial tenant for the first time in a month, confirm the organisation has 25 free credits dated at that instant, repeat sign-in the same month with no second grant, then keep a session across month-end, request a balance (preview or confirm), and confirm leftovers expired and a fresh 25 granted at that instant.

**Acceptance Scenarios**:

1. **Given** a commercial organisation has not received a free grant in the current calendar month, **When** a user in that organisation completes a successful sign-in, **Then** the organisation is granted 25 free credits and the grant is recorded at the actual grant instant (not the first day of the month).
2. **Given** a commercial organisation already received its free grant this calendar month, **When** any user in that organisation signs in again or needs a balance again in the same month, **Then** no additional free grant is recorded and the remaining balance is unchanged by granting.
3. **Given** a commercial organisation’s first grant of the month occurs after the first day of the month, **When** the grant is applied, **Then** the organisation still receives the full 25 credits (no prorating).
4. **Given** a commercial organisation has unused free credits at the end of a calendar month, **When** sign-in or any later action that needs a current balance occurs (preview, confirm, or execute), **Then** leftover free credits are expired with an explicit ledger record first, they do not roll over, and a new 25-credit free grant is applied only if the organisation has not already received this month’s grant.
5. **Given** a commercial organisation has no sign-in and no balance-needed action during a calendar month, **When** that month ends, **Then** no grant is created for that dormant organisation.
6. **Given** a user stays signed in across UTC month-end, **When** they next preview, confirm, or execute, **Then** last month’s leftover free credits are expired and this month’s grant is applied if due, at that instant, without requiring a new sign-in.

---

### User Story 2 - Preview Warns and Blocks When the Import Would Exceed Credits (Priority: P1)

A commercial user prepares an import as today: they upload a file, validate, and preview. Preview always runs and never uses credits. If the preview shows more tasks that would be created than the organisation has remaining, they see a prominent warning with the would-create count, remaining credits, and shortfall, and they cannot confirm the import. If the would-create count is within the remaining balance, confirm stays available (subject to existing import rules).

**Why this priority**: Users must not start an import they cannot finish within their allowance. Blocking at confirm, after an honest preview, is the main fairness and trust control.

**Independent Test**: With a known remaining balance, run a preview whose would-create count exceeds that balance and confirm the warning, figures, and disabled confirm; run a preview at or below the balance and confirm that confirm remains enabled and the balance is unchanged after preview.

**Acceptance Scenarios**:

1. **Given** a commercial user has remaining credits and no due expiry or grant, **When** they run preview, **Then** preview completes using the existing would-create count (tasks that would be created, not file row count and not reused or skipped rows) and the credit balance does not change because of preview (preview never consumes credits).
2. **Given** preview reports a would-create count greater than remaining credits, **When** the preview step is shown, **Then** a prominent UK English warning states how many tasks would be created, how many credits remain, and the shortfall, and Confirm import is disabled.
3. **Given** preview reports a would-create count less than or equal to remaining credits, **When** the preview step is shown, **Then** Confirm import is not disabled by this credit rule (existing non-credit confirm rules still apply).
4. **Given** Confirm import is disabled because of insufficient credits, **When** the user has not obtained more credits, **Then** they cannot start execution from that preview, and the product does not offer a fake purchase path in this increment.
5. **Given** a user is validating a file or reviewing mapping without executing, **When** those steps complete, **Then** no credits are consumed.
6. **Given** a preview previously fitted within remaining credits, **When** another person in the same organisation has since used credits so that live remaining is now below that preview’s would-create count, **Then** Confirm import is disabled or refused with the same insufficient-credits warning (would-create N, remaining M, shortfall), not started from the stale preview.
7. **Given** the organisation’s credit ledger cannot be read or a due grant or expiry cannot be applied, **When** the user tries preview, confirm, or execute, **Then** that action fails with a friendly UK English error, Confirm import does not start a run, remaining is not shown as zero, and no grant is recorded.

---

### User Story 3 - Execution Charges Only Created Tasks and Never Goes Negative (Priority: P1)

A commercial user confirms an import that was allowed by the preview gate. Credits are used only for Planner tasks that are actually created. If the run creates fewer tasks than preview expected, unused credits remain. If remaining credits reach zero during the run, the product stops starting new task creates, already-created tasks stay created, and the report explains that further creates stopped because credits ran out. The balance never goes below zero.

**Why this priority**: Charging for work not done, or silently dropping remaining rows, would break the “no hidden deductions” promise. Mid-run exhaustion must be visible and bounded.

**Independent Test**: Execute an import that creates fewer tasks than previewed and confirm usage equals created count; execute until remaining credits would be exhausted mid-run and confirm no further creates start, no negative balance, and the report names credit exhaustion for rows not created.

**Acceptance Scenarios**:

1. **Given** a commercial user confirms an import, **When** tasks are successfully created, **Then** the organisation is charged one credit per successfully created task and not for reused, skipped, or failed rows.
2. **Given** an import creates fewer tasks than the preview would-create count, **When** execution finishes, **Then** unused credits remain on the organisation’s balance.
3. **Given** remaining credits reach zero while an import is still creating tasks, **When** the product would start the next create, **Then** it does not start that create or any further creates, the balance stays at zero (never negative), and tasks already created remain created.
4. **Given** creates stopped because credits ran out, **When** the user views the import summary, **Then** rows not created for that reason are shown as not created with a clear credit-exhausted explanation (they are not silently omitted).
5. **Given** a Planner task was created but recording its credit usage fails, **When** the product would start the next create, **Then** it retries recording that usage first; if recording still fails, it does not start further creates, does not remove the created Planner task, and the summary shows a UK English error for the stopped run with remaining not-started rows visible.

---

### User Story 4 - Import Summary Makes Usage Obvious (Priority: P2)

After a commercial import run, the existing import summary shows how many tasks were created, how many credits were used, and how many credits remain. In this increment usage is from the free monthly allowance. The breakdown is ready to distinguish free versus paid credits later without changing what “used” and “remaining” mean.

**Why this priority**: Repeated, visible accounting teaches the model without a separate wallet page.

**Independent Test**: Complete a commercial import and confirm the summary shows created count, credits used (free monthly), and remaining credits matching the ledger-derived balance.

**Acceptance Scenarios**:

1. **Given** a commercial import has finished (including a partial run stopped for credit exhaustion), **When** the user views the import summary, **Then** it shows tasks created, credits used, and remaining credits after the run.
2. **Given** credits used in this increment come only from the free monthly allowance, **When** the summary shows usage, **Then** it presents that usage honestly (no hidden extra deductions and no overage line).
3. **Given** the organisation’s remaining balance after the run, **When** the user later needs a balance (for example on the next preview), **Then** that remaining figure matches the summary.

---

### User Story 5 - Self-Hosted and Commercial-Off Stay Unchanged (Priority: P1)

An organisation running the product for themselves, or a hosted deployment with commercial capabilities off, continues the current import journey with no credit ledger, no credit warnings, no confirm block for credits, and no billing copy on Home.

**Why this priority**: Self-hosted viability is a standing product constraint. Commercial accounting must be additive.

**Independent Test**: Run the full import journey with commercial capabilities off and confirm no credit records, no credit wording, and confirm behaviour identical to today aside from unrelated existing rules.

**Acceptance Scenarios**:

1. **Given** commercial capabilities are off, **When** a user previews and confirms an import of any size, **Then** Confirm import is not disabled for credit reasons and no credit warning appears.
2. **Given** commercial capabilities are off, **When** an import executes, **Then** no credit ledger entries are written and the import summary does not show credit usage or remaining credits.
3. **Given** commercial capabilities are off, **When** the user views Home, **Then** there is no billing or credit copy.

---

### Edge Cases

- Two users in the same organisation sign in around the same time at the start of a month: the organisation still receives exactly one free grant of 25 for that month.
- Preview would-create equals remaining credits exactly: Confirm import is not blocked by the credit rule.
- Preview would-create is zero: Confirm import is not blocked by insufficient credits; no credits are consumed on preview.
- File validation fails: no credits are consumed.
- Execution fails before any task is created: no credits are consumed.
- Execution creates some tasks then hits a non-credit error: only successfully created tasks consume credits.
- A user signs in or needs a balance in a new month after unused free credits remained: expiry is recorded first, then the new grant; history shows both events.
- An organisation that never signed in and never needed a balance during a month receives neither a grant nor an expiry row for that idle month.
- File validation alone does not need a credit balance and MUST NOT apply expiry or grant.
- Restored commercial access in the same calendar month as an earlier grant does not receive a second grant that month.
- Existing import confirm rules (for example stale preview) continue to apply in addition to the new credit rule.
- Two people confirm at the same moment after both seeing a sufficient live balance: both imports may start; each still stops starting new creates when the shared remaining balance hits zero; the combined created tasks never drive the balance below zero.
- Confirm is not a reservation: unused would-create from a preview is not held aside for that user.
- Grant, expiry, or live-balance read fails: preview, confirm, and execute do not proceed; the user sees a UK English error, not an insufficient-credits warning with remaining zero, and no grant row is written.
- Sign-in can still establish a commercial session when grant or expiry cannot be applied; the next preview, confirm, or execute retries until the ledger succeeds.
- A task is created in Planner but usage cannot be recorded: the task remains; the product retries that usage record before starting another create; if it still fails, the run stops with an error and does not delete the Planner task; remaining not-started rows stay visible.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Credit accounting MUST apply only when the hosted commercial product is on. Self-hosted use and commercial-off MUST NOT create a ledger, show credit UI, or block confirm for credits.
- **FR-002**: Credits MUST be accounted per commercial organisation (tenant), shared by users of that organisation, not as a separate per-user wallet.
- **FR-003**: The product MUST keep an immutable, append-only credit ledger per tenant. Balances MUST be derived from ledger transactions. Past transactions MUST NOT be edited; corrections MUST be additional compensating transactions.
- **FR-004**: The ledger model MUST support free monthly grants, import usage, and free-credit expiry in this increment, and MUST be shaped so paid purchase lots and paid expiry can be added later without changing consumption order or how remaining balance is derived.
- **FR-005**: Credits MUST be held as lots with a type (free monthly now; paid later). Consumption MUST use free lots first, then paid lots oldest-first, even though this increment has no paid lots.
- **FR-006**: Each commercial tenant MUST be entitled to 25 free credits per calendar month. That quantity is the published V1 allowance (it may be changed later as a single published figure; this feature does not introduce a second account type or a “one free import” gift).
- **FR-007**: The free grant MUST be applied lazily the first time that tenant needs a current balance in a calendar month: on successful commercial sign-in, and also on preview, confirm, or execute if the grant is not yet recorded for that month. The product MUST NOT run a job that grants or expires credits for tenants who neither sign in nor need a balance.
- **FR-008**: The free grant MUST NOT be prorated. A first grant on any day of the month still receives 25 credits.
- **FR-009**: The free-grant ledger record MUST be timestamped at the actual grant instant (that sign-in or later balance-needed action), not backdated to the first day of the month.
- **FR-010**: A tenant MUST receive at most one free grant per calendar month.
- **FR-011**: Unused free credits MUST expire at the end of the same calendar month. They MUST NOT roll over. Expiry MUST appear as an explicit ledger transaction when it is applied.
- **FR-012**: Expiry and the new-month grant MUST be applied lazily whenever a current balance is needed after month-end (sign-in, preview, confirm, or execute), including a session that stayed signed in. Leftover free credits from the previous month MUST be expired before a new grant is recorded. File validation MUST NOT apply expiry or grant.
- **FR-013**: Calendar month boundaries for grant uniqueness and free-credit expiry MUST use Coordinated Universal Time (UTC).
- **FR-014**: One credit MUST equal one Planner task successfully created. Validation and preview MUST never consume credits.
- **FR-015**: Preview MUST always be allowed to run for commercial users regardless of remaining credits.
- **FR-016**: The confirm gate MUST compare the preview’s would-create count (tasks that would be created) with **live** remaining credits, not the file’s row count and not reused or skipped rows.
- **FR-017**: If would-create is greater than remaining credits, the preview step MUST show a prominent UK English warning that includes would-create count N, remaining credits M, and the shortfall (N minus M), and MUST disable Confirm import.
- **FR-018**: If would-create is less than or equal to the **live** remaining credits at confirm time, this feature MUST NOT disable Confirm import for credit reasons. Existing non-credit confirm rules remain.
- **FR-019**: There MUST be no overage, overdraft, or percentage slack. Users MUST NOT start execution when would-create exceeds **live** remaining credits.
- **FR-029**: Confirm import MUST re-check the tenant’s live remaining credits against that preview’s would-create count at confirm time (not rely only on the balance from when preview ran). If live remaining is now less than would-create, Confirm import MUST be disabled or refused with the same UK English warning as FR-017 (N, M, and shortfall). This increment MUST NOT reserve credits at preview or confirm.
- **FR-020**: This increment MUST NOT implement paid purchase, checkout, or payment confirmation. Insufficient-credit messaging MAY tell the user they need more credits; it MUST NOT pretend a purchase completed.
- **FR-021**: During execution, the product MUST consume credits only for tasks actually created. Reused, skipped, and failed creates MUST NOT consume credits. Usage MUST be recorded before the next create starts.
- **FR-031**: If a task is created but recording its usage fails, the product MUST keep that Planner task, MUST retry recording that usage before starting another create, and MUST NOT start further creates if recording still fails. It MUST NOT delete the Planner task. Remaining not-started work MUST be visible on the summary with a UK English error (not treated as a successful full import).
- **FR-022**: If remaining credits reach zero during execution, the product MUST stop starting new task creates, MUST NOT allow a negative balance, and MUST keep already-created tasks.
- **FR-023**: When creates stop because credits ran out, the import summary MUST list remaining not-created work with a clear credit-exhausted outcome rather than omitting those rows.
- **FR-024**: After execution, the import summary MUST show tasks created, credits used, and remaining credits. Credits used in this increment are free monthly credits.
- **FR-025**: Remaining credits shown after execution MUST match the ledger-derived tenant balance.
- **FR-026**: Credit checks and deductions MUST be scoped to the signed-in tenant. A tenant MUST NOT read or change another tenant’s ledger.
- **FR-027**: Failures while granting, expiring, checking, or consuming credits MUST be presented in UK English as human-friendly, actionable messages. They MUST NOT be treated as a successful import or a successful grant.
- **FR-030**: If a due grant or expiry cannot be applied, or live remaining credits cannot be read, preview, confirm, and execute MUST fail closed: they MUST NOT start or continue an import, MUST NOT write a grant, MUST NOT invent a remaining balance of zero, and MUST NOT show the insufficient-credits warning as a substitute. Sign-in MAY still establish a session.
- **FR-028**: This feature MUST NOT add a persistent header “wallet” beyond what preview and the import summary need.

### Key Entities

- **Commercial tenant**: The organisation that owns a shared credit balance. Identified by the existing commercial tenant identity. All users of the tenant share the same ledger.
- **Credit lot**: A dated allocation of credits of a type (free monthly in this increment; paid in a later increment), with remaining quantity derived from later usage and expiry. Free lots expire at the end of the calendar month of the grant. Paid lots will expire on their own later rules without changing “free first, then oldest paid”.
- **Credit ledger transaction**: An immutable record of a balance-changing event for a tenant (free grant, import usage, free expiry; later also paid purchase and paid expiry), with time, amount, type, and enough reference to the import or lot to support support and a future history view.
- **Derived balance**: The tenant’s remaining credits, always calculated from the ledger (and lots), never as an independently editable running total that is the system of record.
- **Would-create count**: The preview figure for how many Planner tasks would be created if the user confirms, used only for the confirm gate.
- **Import usage record**: The execution-time consumption of credits equal to successfully created tasks, reflected on the ledger and summarised on the import report.
- **Credit-exhausted outcome**: A visible execution result for work not started because remaining credits reached zero.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of tested first successful commercial sign-ins of a calendar month (when no grant yet exists), the tenant receives exactly 25 free credits timestamped at that instant.
- **SC-002**: In 100% of tested additional sign-ins or balance-needed actions in the same calendar month, the tenant does not receive a second free grant.
- **SC-003**: In 100% of tested mid-month first grants, the tenant still receives 25 free credits (no prorating).
- **SC-004**: In 100% of tested month-boundary balance needs (new sign-in or a session that lasted past month-end) with leftover free credits, leftovers do not appear in the new month’s remaining balance, and an expiry record exists when expiry is applied.
- **SC-005**: 100% of tested preview and validation runs do not consume credits. Preview may change the ledger only by applying due expiry and/or the month’s free grant before reporting remaining credits. Validation never changes the ledger.
- **SC-014**: In 100% of tested sessions that remain signed in across UTC month-end, the next preview, confirm, or execute expires leftover free credits and grants this month’s 25 if due, without requiring a new sign-in.
- **SC-015**: In 100% of tested ledger-unavailable cases, preview, confirm, and execute do not proceed, no grant is recorded, remaining is not treated as zero, and the user sees an error rather than a successful import or a successful grant.
- **SC-016**: In 100% of tested runs where a task is created but usage cannot be recorded after retry, no further creates start, the created task remains, and the summary reports the failure rather than a fully successful import.
- **SC-006**: 100% of tested previews where would-create exceeds remaining credits show N, remaining M, and shortfall, and leave Confirm import disabled.
- **SC-007**: 100% of tested previews where would-create is at or below remaining credits leave Confirm import enabled with respect to this credit rule, unless a later live re-check at confirm shows the remaining balance no longer covers would-create.
- **SC-013**: In 100% of tested cases where remaining credits fall below a prior preview’s would-create count before confirm, Confirm import is disabled or refused with N, M, and shortfall, and execution does not start from that stale sufficient preview.
- **SC-008**: 100% of tested executions charge exactly one credit per successfully created task and never produce a negative balance.
- **SC-009**: When a test run exhausts credits mid-import, 100% of remaining not-yet-started creates are left uncreated, already-created tasks remain, and the summary states credit exhaustion.
- **SC-010**: After every tested commercial import, the summary shows created count, credits used, and remaining credits that match the ledger-derived balance.
- **SC-011**: 100% of tested commercial-off / self-hosted import journeys show no credit UI, write no ledger, and do not block confirm for credits.
- **SC-012**: A commercial user who is allowed to confirm can complete preview review and understand remaining credits from the summary without a separate accounting page.

## Assumptions

- Hosted commercial identity and tenant context already exist; this feature accounts credits against that tenant, not a new account type.
- “Successful commercial login” means a completed sign-in that grants normal commercial access (not a blocked deleted-account visit).
- UTC calendar months are an acceptable hosted default; the product does not ask each tenant for a billing timezone in this increment.
- Lazy expiry and grant on sign-in or the next preview, confirm, or execute meets the product rule that expiry is explicit and that unused free credits do not roll over; a dedicated month-end job for free credits is not required in this increment.
- A copy-only “you need more credits” message (or omitting a buy action) is acceptable until paid purchases ship; this increment must not simulate payment.
- Existing import confirm rules stay in force; this feature only adds the insufficient-credit disable.
- Preview already produces a reliable would-create count distinct from file rows; this feature uses that figure rather than inventing a new size metric.
- No header wallet, no transaction-history page, and no operator-facing ledger browser are required for this increment.
- Paid lots, paid expiry jobs, checkout, and invoices are specified only as extension points so consumption rules need not be redesigned later.
- Dormant tenants accrue no ledger noise: no monthly grant rows and no expiry rows until a later sign-in or other balance-needed action.
- Support can reconstruct a tenant’s remaining credits from the ledger alone.

## Out of Scope

- Paid credit purchases, checkout, payment webhooks, invoices, receipts, and tax handling.
- Subscriptions, automatic recurring charges, and scheduled imports.
- A free-account SKU or a “one free import” allowance.
- Credit overage or overdraft.
- A background job that grants free credits to dormant tenants.
- A background job required solely to expire free credits (lazy expiry is in scope).
- Public website pricing or credits copy.
- Persistent account-header wallet chrome beyond preview and import summary.
- Changing self-hosted import behaviour.

## Dependencies

- Relies on existing commercial tenant identity and commercial-on versus commercial-off behaviour.
- Relies on the existing import preview would-create count and import summary surface.
- Blocks the later paid-credits feature, which must add lots and purchases onto this ledger rather than replace it.
