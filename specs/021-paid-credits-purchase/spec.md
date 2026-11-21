# Feature Specification: Paid Credits Purchase

**Feature Branch**: `021-paid-credits-purchase`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Add paid credits for hosted Import To Planner. Depends on the immutable per-tenant credit ledger, free monthly allowance, and preview/execute gate already implemented (#126). Product contract: docs/credits-billing-usage-model.md and GitHub issue #125. Related completed free-tier work: #124."

**Traceability**: GitHub issue [#125](https://github.com/markheydon/import-to-planner/issues/125) (parent epic [#124](https://github.com/markheydon/import-to-planner/issues/124)). **Builds on** [#126](https://github.com/markheydon/import-to-planner/issues/126) (commercial credit ledger, free allowance, preview/execute gate). **Coordinates with** [#138](https://github.com/markheydon/import-to-planner/issues/138) / spec `020-profile-credits-ledger` (profile balance and activity) and [#129](https://github.com/markheydon/import-to-planner/issues/129) (complimentary unlimited tenants). Product contract: `docs/credits-billing-usage-model.md`.

## Clarifications

### Session 2026-10-13

- Q: When a user deletes their commercial account, where should payment-provider customer and receipt metadata be retained so receipts, disputes, and tax stay supportable without implying the tenant’s paid credit balance is forfeited? → A: Tenant-scoped commercial storage; individual account deletion does not remove tenant billing metadata or ledger balance.
- Q: When a payment is refunded or charged back after credits were granted, how should the tenant ledger be updated in V1? → A: On confirmed refund or chargeback, compensating ledger transaction for unused quantity in the originating paid lot only (idempotent; never below zero).
- Q: On the preview step when credits are insufficient, how should users choose which credit pack to buy? → A: Show all published packs; highlight or pre-select the smallest pack whose size is at least the shortfall; user may choose another pack.
- Q: On `/profile` buy-credits (no active import shortfall), how should pack choice be presented compared with the preview insufficient-credits path? → A: Preview recommends smallest pack ≥ shortfall; profile shows full catalogue with no shortfall-based highlight or pre-selection.
- Q: When checkout is created and when paid credits are granted or adjusted, which organisation must receive the credits? → A: Server derives organisation solely from the authenticated commercial session at checkout creation and on every grant or refund adjustment; no client-supplied tenant override.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buy Credits When Preview Shows a Shortfall (Priority: P1)

An organisation administrator or import user in hosted commercial mode prepares an import. Preview runs as today and never consumes credits. When the would-create count exceeds the organisation’s remaining balance, they see a prominent UK English warning (N would be created, M remain, shortfall) and Confirm import stays disabled. The warning offers a real path to buy additional credits through external checkout. After payment is confirmed on the server, the organisation’s balance increases, the preview step refreshes remaining credits, and Confirm import becomes available when N is less than or equal to the new balance.

**Why this priority**: Paid credits are the revenue path; without a working purchase journey from the blocked preview state, users cannot complete large imports after the free allowance.

**Independent Test**: With a tenant whose remaining balance is below a preview’s would-create count, open preview, start checkout from the warning, complete payment, return to the app, confirm balance refresh, and verify Confirm import enables when would-create fits the updated balance.

**Acceptance Scenarios**:

1. **Given** preview reports would-create N greater than remaining M, **When** the preview step is shown, **Then** a prominent UK English warning states N, M, and the shortfall, Confirm import is disabled, all published credit packs are offered for purchase, the smallest pack whose size is at least the shortfall is highlighted or pre-selected as the recommended choice, and the user may select a different pack before starting checkout.
2. **Given** the user has opened checkout but payment is not yet confirmed server-side, **When** they return to preview, **Then** remaining balance is unchanged by checkout alone and Confirm import stays disabled if N still exceeds M.
3. **Given** payment is confirmed server-side for a pack of C credits, **When** the user returns to or refreshes the preview step, **Then** remaining balance increases by C (ledger-derived), and Confirm import is enabled when N is less than or equal to the updated remaining balance (subject to existing non-credit confirm rules).
4. **Given** preview would-create is less than or equal to remaining credits, **When** the preview step is shown, **Then** Confirm import is not disabled for credit reasons and no insufficient-credits purchase prompt is required to continue.
5. **Given** a user on the complimentary unlimited allowlist, **When** they preview any import size, **Then** Confirm import is not blocked for insufficient credits, buy-credits is not presented as required to continue, and remaining is shown as unlimited per the complimentary-tenant product rules.

---

### User Story 2 - Paid Lots Are Granted Safely After Confirmed Payment (Priority: P1)

Each successful purchase creates a distinct paid credit lot on the organisation’s ledger. Grants happen only after the product receives an authoritative server-side payment confirmation (not when checkout opens and not from browser-side success alone). Duplicate confirmations for the same payment must not double-grant. Confirmed refunds or chargebacks reduce only the unused quantity still in the originating paid lot via a compensating ledger transaction (idempotent; no negative balance). Each lot expires twelve months after purchase; that expiry is recorded later as its own ledger transaction.

**Why this priority**: Incorrect or duplicate grants break trust and revenue accounting; the ledger is the system of record.

**Independent Test**: Simulate confirmed payment once and verify one lot and balance increase; replay the same confirmation and verify no second lot; abandon checkout and verify no grant.

**Acceptance Scenarios**:

1. **Given** a commercial tenant completes checkout, **When** payment is not yet confirmed server-side, **Then** no paid purchase transaction or paid lot appears on the tenant ledger.
2. **Given** a confirmed payment for a pack of C credits, **When** the grant is applied, **Then** the ledger records a paid purchase for C credits as a distinct lot with purchase time T and expiry at T plus twelve months.
3. **Given** the same payment confirmation is delivered more than once, **When** the product processes it, **Then** at most one paid lot is granted for that payment and the balance increases only once.
4. **Given** a failed or abandoned payment, **When** no successful confirmation is received, **Then** the ledger is unchanged by that attempt.
5. **Given** a paid lot with U credits still unused from an earlier confirmed purchase, **When** an authoritative server-side refund or chargeback is confirmed for that payment, **Then** the ledger records a compensating transaction that removes U credits from that lot (not more than U), derived balance decreases by U, and already-consumed credits from that lot are not clawed back.
6. **Given** a paid lot fully consumed (zero unused), **When** a refund or chargeback is confirmed for that payment, **Then** no compensating credit removal is applied beyond ledger rules (balance does not go negative).
7. **Given** the same refund or chargeback confirmation is delivered more than once, **When** the product processes it, **Then** at most one compensating adjustment applies for that refund identifier and balance decreases only once for the unused portion.

---

### User Story 3 - Usage Consumes Free Then Oldest Paid; Summary Shows the Split (Priority: P1)

When an import executes, credits are still charged only for successfully created tasks. Free monthly credits are consumed first; any remainder is taken from paid lots in oldest-purchase-first order. After the run, the import summary shows tasks created, credits used with a free versus paid breakdown when both were used, and remaining credits after execution.

**Why this priority**: Paying customers must not lose paid credits while free allowance remains, and must see honest accounting when both pools are used.

**Independent Test**: Seed a tenant with partial free lot and two paid lots of different ages; execute an import that spans free and paid; confirm ledger consumption order and summary lines match.

**Acceptance Scenarios**:

1. **Given** an organisation has free and paid credits available, **When** an import consumes credits, **Then** free credits are reduced first and paid lots are reduced starting with the oldest purchase still unexpired.
2. **Given** an import uses only free credits, **When** the user views the import summary, **Then** credits used show only the free monthly portion (no paid line).
3. **Given** an import uses both free and paid credits, **When** the user views the import summary, **Then** credits used show separate free and paid quantities that sum to total credits used for created tasks.
4. **Given** execution behaviour from the ledger feature (partial run, mid-run exhaustion, usage recording failures), **When** paid credits exist, **Then** those rules still apply; paid lots do not introduce overage or negative balance.

---

### User Story 4 - Paid Credits Expire on Schedule With Explicit Ledger Records (Priority: P1)

Paid credit lots expire twelve months after purchase. Expiry is enforced by a scheduled process (not only when someone signs in). Each expiry writes an explicit ledger transaction; balances never change silently.

**Why this priority**: Predictable expiry is part of the commercial contract; dormant organisations must not keep paid balance indefinitely without recorded expiry.

**Independent Test**: Create a paid lot whose expiry time is in the past relative to a test clock or seeded data; run the scheduled expiry enforcement; confirm an expiry transaction and reduced balance.

**Acceptance Scenarios**:

1. **Given** a paid lot has passed its twelve-month expiry instant, **When** scheduled expiry enforcement runs, **Then** an explicit paid-credit expiry transaction is recorded and remaining quantity in that lot goes to zero without editing prior transactions.
2. **Given** a paid lot is not yet expired, **When** scheduled expiry enforcement runs, **Then** that lot is unchanged.
3. **Given** partial consumption of a paid lot before expiry, **When** expiry runs, **Then** only the unused portion of that lot is expired on the ledger.
4. **Given** expiry enforcement cannot complete for a tenant, **When** the failure is detected, **Then** it is logged for operations; the product does not silently adjust balances outside the ledger.

---

### User Story 5 - Tenant Ledger Survives Commercial Account Deletion (Priority: P1)

Credit balances, lots, grants, usage, purchases, and expiry are scoped to the Entra organisation (tenant), not to an individual user’s commercial profile record. When a user deletes their commercial account, signed-in access ends immediately and the existing approximately six-month retention window applies to that account row and restore—but the tenant credit ledger is not deleted, reset, or zeroed. Restoring the account during retention reactivates the same account row; it does not recreate or duplicate credits. A new commercial sign-in for the same organisation after account-row purge still sees the existing tenant ledger balance (including anti-abuse behaviour for monthly free grants already defined on the ledger).

**Why this priority**: Prevents “delete account → fresh free grant” abuse while keeping paid balance attached to the organisation.

**Independent Test**: Grant paid and free credits, delete one user’s commercial account, verify ledger unchanged and access blocked; restore within retention and verify ledger still matches; confirm purge of account row does not purge ledger partition.

**Acceptance Scenarios**:

1. **Given** a tenant has ledger history including paid purchases, **When** a user deletes their commercial account, **Then** that user loses commercial access immediately, the account row enters deleted retention, and all ledger transactions and derived balance for the tenant remain intact.
2. **Given** a deleted account within the retention window, **When** the user restores the account, **Then** the same account row is reactivated and the tenant ledger is unchanged from before deletion.
3. **Given** account-row purge after retention, **When** another user from the same organisation signs in commercially, **Then** they see the same tenant ledger balance and history (not a blank ledger), subject to existing monthly free-grant rules on the ledger.
4. **Given** account deletion, **When** payment-provider customer and receipt metadata is needed for receipts, disputes, or tax, **Then** that metadata remains in tenant-scoped commercial storage (not tied to the deleting user’s account-row lifecycle), the tenant credit ledger and derived paid balance are unchanged, and deleting or purging the user’s commercial account row does not remove tenant billing metadata.

---

### User Story 6 - Profile and In-App Purchase Entry (Priority: P2)

Commercial users can discover and buy credit packs from the profile credits section as well as from the preview shortfall path. Profile activity shows paid purchases alongside grants, expiries, and grouped import usage (per profile spec). Payment-provider customer identifiers are not shown in the user interface.

**Why this priority**: Users need a stable place to top up between imports; profile was designed to include purchases when this feature exists.

**Independent Test**: Open `/profile`, start checkout from buy credits, complete payment, confirm activity lists the purchase and balance matches preview.

**Acceptance Scenarios**:

1. **Given** hosted commercial mode and a non-allowlisted tenant, **When** the user opens `/profile`, **Then** they can start checkout for any published credit pack from the full catalogue with no shortfall-based highlight or pre-selection (unlike preview, which recommends the smallest pack covering an active import shortfall).
2. **Given** a completed paid purchase or a confirmed refund that adjusted an originating lot, **When** the user views profile credit activity, **Then** the event appears with understandable UK English labelling and quantity matching the ledger.
3. **Given** any commercial user-facing surface, **When** payment or account details are shown, **Then** technical payment-provider customer identifiers are omitted.

---

### User Story 7 - Self-Hosted and Commercial-Off Have No Paid-Credit Surface (Priority: P1)

Self-hosted deployments and hosted deployments with commercial mode off do not show buy-credits UI, do not initiate checkout, and do not require payment confirmation endpoints for normal operation.

**Why this priority**: Self-hosted viability and clear commercial boundary.

**Independent Test**: Run import journey with commercial off; confirm absence of purchase affordances and no ledger paid-purchase entries.

**Acceptance Scenarios**:

1. **Given** commercial capabilities are off, **When** a user runs preview and opens profile, **Then** no buy-credits actions, checkout, or paid-credit copy appear.
2. **Given** commercial capabilities are off, **When** any import runs, **Then** no paid purchase or paid expiry ledger entries are written.

---

### Edge Cases

- User starts checkout, closes the browser, and pays later: grant only after server confirmation; preview balance updates on next refresh after grant.
- Two users in the same tenant: purchases increase shared balance; either may complete checkout; consumption remains tenant-scoped.
- Confirm re-check after purchase: live remaining must include new paid credits before confirm proceeds.
- Purchase completes while another user consumes credits: confirm still uses live remaining versus N.
- Paid lot fully consumed before expiry: expiry job records zero additional expiry or a no-op consistent with ledger rules.
- Multiple paid lots with the same purchase second: FIFO still uses stable oldest-first ordering.
- Webhook or confirmation arrives before user returns to preview: balance is correct on next balance-needed action.
- Allowlisted tenant: no purchase required; ledger may still record usage with complimentary pairing per complimentary-tenant rules.
- Account deleted user cannot confirm imports; another active user in the same tenant can still use remaining paid balance.
- User deletes commercial account after purchasing credits: tenant ledger and tenant-scoped payment-provider metadata remain; only that user’s access and account-row retention apply.
- Pack catalogue lists a fixed small set of packs; unknown or disabled pack identifiers from stale UI are rejected with friendly UK English errors.
- User selects a pack smaller than the shortfall on preview: checkout may still complete, but Confirm import remains disabled until remaining balance covers N after grant.
- Refund or chargeback after partial use of a paid lot: only unused quantity in the originating lot is removed; imports that already consumed credits are unchanged.
- Checkout or webhook payload cannot redirect paid grants to another organisation: tenant for checkout creation and ledger writes is always the authenticated session organisation (or the organisation bound server-side when the checkout session was created).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Paid credit purchase MUST be available only when hosted commercial mode is on. Self-hosted and commercial-off MUST NOT expose purchase UI, checkout initiation, or routine dependence on payment confirmation endpoints.
- **FR-002**: All paid credit balances, lots, purchases, usage, grants, and expiries MUST remain scoped to the commercial organisation (Entra tenant), shared by all users in that tenant, not stored as a per-user wallet on the commercial profile row. Checkout creation, paid grants, and refund or chargeback adjustments MUST target the organisation derived from the authenticated commercial session (or the organisation bound server-side when that checkout session was created). Client-supplied tenant identifiers MUST NOT override session organisation.
- **FR-003**: The immutable per-tenant credit ledger delivered in [#126](https://github.com/markheydon/import-to-planner/issues/126) MUST remain the system of record. Paid grants, paid usage, paid expiry, and paid refund or chargeback adjustments MUST be append-only ledger transactions; balances MUST remain derived from the ledger.
- **FR-004**: The product MUST offer a small, fixed catalogue of credit packs (exact sizes and prices are configuration decisions documented in planning; not subscriptions or recurring charges).
- **FR-005**: Users MUST be able to start external checkout for a chosen pack from the preview insufficient-credits state and from the commercial profile credits section (non-allowlisted tenants). On preview when would-create exceeds remaining, the product MUST list all published packs and MUST highlight or pre-select the smallest pack whose size is at least the shortfall, while allowing the user to choose another pack before checkout. On `/profile` buy-credits, the product MUST list all published packs and MUST NOT apply shortfall-based highlight or pre-selection.
- **FR-006**: Paid credits MUST be granted only after authoritative server-side payment confirmation. Opening checkout or client-reported payment success alone MUST NOT grant credits. Each grant MUST apply to the same organisation that was bound when checkout was initiated for that payment.
- **FR-007**: Each confirmed purchase MUST create exactly one distinct paid lot on the ledger for that payment, with quantity equal to the purchased pack and expiry twelve months after the recorded purchase instant.
- **FR-008**: Duplicate payment confirmations for the same payment MUST be processed idempotently so the tenant receives at most one grant per payment.
- **FR-009**: Consumption order MUST remain: free monthly credits first, then paid lots oldest purchase first (FIFO), consistent with the product contract.
- **FR-010**: Preview MUST never consume credits. Confirm import MUST stay disabled or refused while would-create exceeds live remaining balance. After a successful paid grant, the UI MUST refresh ledger-derived remaining so confirm can proceed when would-create is less than or equal to remaining.
- **FR-011**: Import execution MUST charge only successfully created tasks. Import summary MUST show tasks created, credits used (with free versus paid breakdown when both apply), and remaining credits after the run.
- **FR-012**: Paid lot expiry MUST be enforced on a schedule without manual intervention. Each expiry MUST be recorded as an explicit ledger transaction; unused paid balance MUST NOT disappear without a ledger entry.
- **FR-013**: External payment services MUST be used only for checkout, payment, receipts, invoicing, and tax—not for metering import usage or maintaining credit balances.
- **FR-014**: Deleting a commercial user account MUST end that user’s access immediately and follow the existing approximately six-month account-row retention and restore flow. Account deletion and account-row purge MUST NOT delete, reset, or zero the tenant credit ledger.
- **FR-015**: Restoring a deleted commercial account within retention MUST reactivate the same account row and MUST NOT recreate, duplicate, or reset tenant ledger credits.
- **FR-016**: Payment-provider customer and receipt metadata MUST be stored in tenant-scoped commercial storage (organisation key), not on the per-user commercial account row as the system of record. Deleting or purging a user’s commercial account MUST NOT delete, reset, or forfeit tenant billing metadata or tenant ledger paid balance. Metadata MUST remain available for receipts, disputes, and tax for the organisation independent of any single user’s account lifecycle.
- **FR-017**: Complimentary allowlisted tenants MUST NOT be required to buy credits to import; buy-credits entry points MUST NOT appear as mandatory for them; remaining credits MUST present as unlimited per complimentary-tenant rules.
- **FR-018**: User-facing copy MUST be UK English, friendly, and actionable. Errors MUST NOT expose stack traces or payment-provider customer identifiers in the UI.
- **FR-019**: There MUST be no subscriptions, automatic recurring charges, credit overage, overdraft, or separate free-account SKU in this feature.
- **FR-020**: Fail-closed rules from the ledger feature for unreadable ledger or failed grant/expiry on balance-needed actions MUST still apply; purchase flow MUST NOT bypass them by inventing balance.
- **FR-021**: On authoritative server-side refund or chargeback confirmation for a prior purchase, the product MUST append a compensating ledger transaction linked to the originating paid lot that removes at most the lot’s current unused quantity. Duplicate refund confirmations MUST be idempotent. The tenant balance MUST NOT go negative and MUST NOT claw back credits already consumed from that lot.

### Key Entities

- **Credit pack**: A published product offering of a fixed number of import credits at a price, selected at checkout. Not a subscription.
- **Paid credit lot**: A ledger-backed allocation from one purchase, with original quantity, remaining quantity, purchase instant, and expiry instant twelve months later.
- **Payment confirmation**: An authoritative server-side signal that a checkout payment succeeded, carrying a stable identifier used for idempotent grant.
- **Paid purchase transaction**: Ledger entry that opens or increases a paid lot when payment is confirmed.
- **Paid refund or chargeback transaction**: Compensating ledger entry that reduces unused quantity in the originating paid lot when a refund or chargeback is confirmed server-side.
- **Refund or chargeback confirmation**: Authoritative server-side signal that a prior checkout payment was refunded or charged back, carrying a stable identifier used for idempotent compensating adjustment.
- **Paid expiry transaction**: Ledger entry that removes unused quantity from an expired paid lot.
- **Tenant credit ledger**: Append-only organisation-scoped history from which remaining balance and lot state are derived (extends existing ledger entity).
- **Commercial account record**: Per-user commercial profile and access gate; deletion affects user access and retention, not tenant ledger partition or tenant-scoped payment-provider metadata.
- **Tenant billing metadata**: Organisation-scoped payment-provider customer and receipt references used for checkout, invoices, disputes, and tax; survives individual commercial account deletion and account-row purge.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least ninety-five percent of commercial users who start checkout from an insufficient-credits preview can complete purchase and see Confirm import enabled within five minutes when would-create fits the purchased pack (measured in staging or pilot with test payments). This is a pilot or staging observability target only; it is not a mandatory automated CI gate.
- **SC-002**: Zero duplicate-payment confirmations in testing produce more than one paid lot for the same payment identifier.
- **SC-003**: One hundred percent of paid lots in acceptance tests expire within one scheduled enforcement cycle after their twelve-month instant, with a matching expiry ledger entry and correct remaining balance.
- **SC-004**: In acceptance tests, one hundred percent of imports that consume both free and paid credits show a summary breakdown that matches ledger consumption order (free first, then oldest paid).
- **SC-005**: In acceptance tests, commercial account deletion never reduces tenant ledger transaction count or resets derived paid balance; restore within retention preserves the same balance figure as before deletion.
- **SC-006**: Commercial-off and self-hosted journeys show no buy-credits or checkout affordances in usability checks of preview and profile.
- **SC-007**: In acceptance tests, confirmed refunds or chargebacks with partial lot consumption remove only the unused portion once per refund identifier; fully consumed lots produce no negative balance.

## Assumptions

- The immutable ledger, twenty-five monthly free credits, preview/execute gate, and import summary from [#126](https://github.com/markheydon/import-to-planner/issues/126) are already live; this feature extends them rather than redefining free-tier rules.
- Credit pack sizes and prices are a small fixed catalogue maintained in deployment configuration; marketing pricing pages on the public site are out of scope for V1.
- External checkout and server-side payment confirmation follow the payment provider described in `docs/credits-billing-usage-model.md` (checkout, receipts, VAT); the specification treats that provider as an external boundary without prescribing implementation structure.
- Scheduled paid-lot expiry runs at least daily in production; exact hosting choice is a planning decision.
- Profile credits section from spec `020-profile-credits-ledger` is implemented or will ship alongside this feature so purchases appear in activity and buy entry is available.
- Organisation administrators and ordinary import users share the same tenant balance; separate “who may purchase” roles beyond signed-in commercial access are not introduced in V1 unless planning discovers an existing policy hook.
- Public privacy copy will state that the credit ledger and tenant billing metadata are organisation-scoped and are not removed when an individual user deletes their commercial account; the six-month window applies to the user’s account row only (aligned in the same delivery as this feature, documented in planning).
