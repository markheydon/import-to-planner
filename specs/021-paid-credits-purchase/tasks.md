---

description: "Task list for Paid Credits Purchase (spec 021)"
---

# Tasks: Paid Credits Purchase

**Input**: Design documents from `/specs/021-paid-credits-purchase/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/paid-credits-purchase-contracts.md, quickstart.md

**Tests**: Mandatory per spec contracts §10 and plan.md (xUnit v3, NSubstitute, bUnit; opt-in stripe-mock only).

**Organization**: Tasks grouped by user story for independent delivery. **End-to-end preview purchase (US1) requires grant/webhook work (US2)** — complete Phase 3 (US2) before validating Phase 4 (US1) acceptance scenarios 3–5.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US7) on story-phase tasks only

## Path Conventions

Repository layout per plan.md: `src/ImportToPlanner.*`, `tests/ImportToPlanner.Tests`, `tests/ImportToPlanner.Web.Tests`, `website/content/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New projects, packages, and solution wiring without business logic.

- [ ] T001 Add `ImportToPlanner.Infrastructure.Stripe` and `ImportToPlanner.Functions` to `ImportToPlanner.slnx`
- [ ] T002 Create `src/ImportToPlanner.Infrastructure.Stripe/ImportToPlanner.Infrastructure.Stripe.csproj` referencing `ImportToPlanner.Commercial` and central-package-managed Stripe.net 53.x in `Directory.Packages.props`
- [ ] T003 Create `src/ImportToPlanner.Functions/ImportToPlanner.Functions.csproj` referencing `ImportToPlanner.Commercial` only (no Stripe.net)
- [ ] T004 [P] Add `Aspire.Hosting.Azure.Functions` package version to `Directory.Packages.props` and reference from `src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj`
- [ ] T005 Add project reference from `src/ImportToPlanner.Web/ImportToPlanner.Web.csproj` to `ImportToPlanner.Infrastructure.Stripe`
- [ ] T006 [P] Add `Stripe:CreditPacks`, `Stripe:ApiKey`, `Stripe:WebhookSecret`, `Stripe:AutomaticTaxEnabled`, and `Storage:CommercialBillingTable` sample keys to `src/ImportToPlanner.Web/appsettings.Development.json` (placeholder values only, no secrets in source)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Configuration models, billing port, ledger extensions, and commercial registration skeleton. **No user story work until this phase completes.**

**⚠️ CRITICAL**: Blocks all user story phases.

- [ ] T007 Add `CreditEntryType.PaidAdjustment = 6` to `src/ImportToPlanner.Commercial/Models/CreditLedgerEnums.cs`
- [ ] T008 [P] Add `CreditPackOptions` and `StripeOptions` (including `CreditPacks` list with PackId, CreditQuantity, StripePriceId, DisplayName, AmountMinor, Currency, Published) under `src/ImportToPlanner.Commercial/Configuration/` or `Billing/`
- [ ] T009 [P] Define neutral checkout/grant/refund records and outcome types in `src/ImportToPlanner.Commercial/Credits/` (no Stripe types)
- [ ] T010 Add `ITenantBillingStore` in `src/ImportToPlanner.Commercial/Abstractions/ITenantBillingStore.cs` (tenant partition row `billing`: StripeCustomerId, LastInvoiceOrReceiptId, UpdatedUtc)
- [ ] T011 Implement `TableTenantBillingStore` in `src/ImportToPlanner.Commercial/Billing/TableTenantBillingStore.cs` using `Storage:CommercialBillingTable` (default `CommercialBilling`)
- [ ] T012 Extend `ICreditLedgerStore` in `src/ImportToPlanner.Commercial/Abstractions/ICreditLedgerStore.cs` with paid grant, paid expiry, paid adjustment, and paid-lot lookup by `ExternalPaymentId` methods per data-model.md
- [ ] T013 Implement paid lot rows (`lot|paid|{externalPaymentId}`), markers (`pay|`, `evt|`, `adj|`), and `PaidPurchase` / `PaidExpiry` / `PaidAdjustment` transactions in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs`
- [ ] T014 [P] Add `CommercialBillingTableClientKey` and keyed `TableClient` registration in `src/ImportToPlanner.Commercial/DependencyInjection.cs` when commercial mode is enabled
- [ ] T015 Register `ITenantBillingStore`, pack options, and paid use case stubs in `src/ImportToPlanner.Commercial/DependencyInjection.cs` (commercial-on only)
- [ ] T016 Extend `src/ImportToPlanner.Web/Infrastructure/StartupConfigurationValidator.cs` to require Stripe API key, webhook secret, billing table name, and at least one published pack when `Features:CommercialMode:Enabled` is true; reject live-mode key prefixes (`sk_live_`, `rk_live_`) outside Production
- [ ] T017 [P] Extend `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` for billing table client registration when commercial mode is on
- [ ] T018 [P] Extend `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` to assert Stripe.net references exist only in `ImportToPlanner.Infrastructure.Stripe` (source and csproj)

**Checkpoint**: Ledger and billing ports compile; commercial-off startup unchanged.

---

## Phase 3: User Story 2 — Paid Lots Granted Safely After Confirmed Payment (Priority: P1)

**Goal**: Authoritative webhook grant, idempotent markers, refund/chargeback adjustment, no grant on abandoned or async-failed payments.

**Independent Test**: One verified payment → one lot; replay event/payment → no second lot; failed/abandoned → ledger unchanged.

### Tests for User Story 2

- [ ] T028 [P] [US2] Add grant and duplicate idempotency tests in `tests/ImportToPlanner.Tests/Credits/GrantPaidCreditsUseCaseTests.cs`
- [ ] T029 [P] [US2] Add abandoned checkout and `async_payment_failed` no-grant tests in `tests/ImportToPlanner.Tests/Credits/StripeWebhookGrantTests.cs`
- [ ] T030 [P] [US2] Add refund partial-use, empty lot, and duplicate adjustment tests in `tests/ImportToPlanner.Tests/Credits/AdjustPaidCreditsForRefundUseCaseTests.cs`
- [ ] T031 [P] [US2] Add unknown pack rejection test in `tests/ImportToPlanner.Tests/Credits/GrantPaidCreditsUseCaseTests.cs`

### Implementation for User Story 2

- [ ] T032 [US2] Implement `GrantPaidCreditsUseCase` in `src/ImportToPlanner.Commercial/Credits/GrantPaidCreditsUseCase.cs` (published pack check; `evt|` + `pay|` + lot + `PaidPurchase`; expiry `GrantedAtUtc` plus twelve calendar months; `AlreadyGranted` on replay; ledger remains sole balance authority per FR-013)
- [ ] T033 [US2] Implement `AdjustPaidCreditsForRefundUseCase` in `src/ImportToPlanner.Commercial/Credits/AdjustPaidCreditsForRefundUseCase.cs` (unused quantity only; `adj|` marker; never negative balance)
- [ ] T034 [US2] Implement webhook signature verification and event → neutral confirmation mapping in `src/ImportToPlanner.Infrastructure.Stripe/` (handle `checkout.session.completed` and `checkout.session.async_payment_succeeded` only when `payment_status` is `paid`; `async_payment_failed` no grant; `refund.updated` succeeded; `charge.dispute.closed` lost)
- [ ] T035 [US2] Add `POST /webhooks/stripe` in `src/ImportToPlanner.Web/Features/Billing/StripeWebhookEndpoint.cs` (anonymous, raw body, 400 on bad signature, 500 on ledger failure, 200 on success/ignored)
- [ ] T036 [US2] Map webhook endpoint only when commercial mode is enabled in `src/ImportToPlanner.Web/Program.cs`
- [ ] T037 [US2] Register grant and adjustment use cases in `src/ImportToPlanner.Commercial/DependencyInjection.cs`

**Checkpoint**: Paid grant path ready for preview end-to-end validation in Phase 4.

---

## Phase 4: User Story 1 — Buy Credits When Preview Shows a Shortfall (Priority: P1) 🎯 MVP

**Goal**: Insufficient-credits preview shows UK English shortfall warning, full pack catalogue with recommended smallest pack ≥ shortfall, checkout redirect, and confirm enabled after ledger balance refreshes post-grant.

**Independent Test**: Tenant with remaining &lt; would-create → preview warning → start checkout → (after Phase 3 grant) return → balance refresh → Confirm import enables when N ≤ remaining.

### Tests for User Story 1

- [ ] T019 [P] [US1] Add bUnit tests for preview shortfall pack list, recommended pre-selection, disabled Confirm, and no buy UI for complimentary tenants in `tests/ImportToPlanner.Web.Tests/Features/Import/HomeCreditPurchaseTests.cs`
- [ ] T020 [P] [US1] Add bUnit test that opening checkout alone does not change displayed remaining in `tests/ImportToPlanner.Web.Tests/Features/Import/HomeCreditPurchaseTests.cs`
- [ ] T067 [P] [US1] Add bUnit fail-closed tests when ledger ensure or balance read fails on preview (and after checkout return refresh): UK English error, no invented remaining, insufficient-credits warning not shown as substitute (FR-020, spec 011 FR-030) in `tests/ImportToPlanner.Web.Tests/Features/Import/HomeCreditPurchaseTests.cs`

### Implementation for User Story 1

- [ ] T021 [US1] Implement `CreateCreditCheckoutUseCase` in `src/ImportToPlanner.Commercial/Credits/CreateCreditCheckoutUseCase.cs` (session tenant and actor only; reject unknown/unpublished PackId; complimentary tenants rejected; no ledger write)
- [ ] T022 [US1] Add `ICreditCheckoutPaymentPort` (or equivalent) in `src/ImportToPlanner.Commercial/Abstractions/` and Stripe implementation in `src/ImportToPlanner.Infrastructure.Stripe/` creating Checkout Session per data-model.md (`mode=payment`, metadata `tenant_id`/`pack_id`/`actor_user_id`, no `payment_method_types`, `automatic_tax` gated by `Stripe:AutomaticTaxEnabled`)
- [ ] T023 [US1] Add authenticated checkout endpoint or minimal API in `src/ImportToPlanner.Web/Features/Billing/` mapping session → `CreateCreditCheckoutUseCase` and returning redirect URL
- [ ] T024 [US1] Extend `src/ImportToPlanner.Web/Features/Import/Presenters/ImportCreditPreviewPresenter.cs` with pack catalogue labels, shortfall warning (N, M, shortfall), and recommended pack selection (smallest `CreditQuantity` ≥ shortfall; no pre-selection when none covers shortfall)
- [ ] T025 [US1] Update preview step UI in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor` and `Home.razor.cs` (MudBlazor pack chooser, buy action, keep Confirm disabled while would-create &gt; remaining; hide mandatory buy for allowlisted tenants)
- [ ] T026 [US1] Wire `ImportWorkflowCoordinator` in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs` to refresh credit snapshot after return from checkout (Ensure balance on preview focus/refresh per FR-010; fail closed per FR-020)
- [ ] T027 [US1] Register Stripe adapter and checkout use case in `src/ImportToPlanner.Web/Program.cs` or commercial DI extension (commercial-on only)

**Checkpoint**: US1 acceptance scenarios 1–5 can be validated end-to-end with Sandbox or test doubles.

---

## Phase 5: User Story 3 — Usage Consumes Free Then Oldest Paid; Summary Shows the Split (Priority: P1)

**Goal**: FIFO consumption and import summary free/paid breakdown when both pools used.

**Independent Test**: Seed free + two paid lots; execute import spanning both; ledger order and summary lines match.

### Tests for User Story 3

- [ ] T038 [P] [US3] Add FIFO consumption test (free monthly first, then oldest `GrantedAtUtc`, `LotId` tie-break) in `tests/ImportToPlanner.Tests/Credits/PaidCreditFifoConsumptionTests.cs`
- [ ] T039 [P] [US3] Extend import summary assertions for free-only and free+paid breakdown in `tests/ImportToPlanner.Tests/Credits/CreditLedgerExecutionIntegrationTests.cs` or dedicated summary test file

### Implementation for User Story 3

- [ ] T040 [US3] Verify `CreditLotSelector` in `src/ImportToPlanner.Commercial/Credits/` orders `LotType.FreeMonthly` before paid lots by `GrantedAtUtc` then `LotId`; adjust only if paid lots are skipped incorrectly
- [ ] T041 [US3] Ensure import summary presenter/mapper in `src/ImportToPlanner.Web/Features/Import/` exposes separate free and paid credit-used quantities when both apply (FR-011)

**Checkpoint**: US3 acceptance scenarios pass without negative balance or overage.

---

## Phase 6: User Story 4 — Paid Credits Expire on Schedule With Explicit Ledger Records (Priority: P1)

**Goal**: Daily scheduled expiry via Azure Functions calling `ExpirePaidCreditLotsUseCase`; explicit `PaidExpiry` transactions.

**Independent Test**: Paid lot with `ExpiresAtUtc` in the past → sweep → one expiry transaction; unused-only portion expired.

### Tests for User Story 4

- [ ] T042 [P] [US4] Add `ExpirePaidCreditLotsUseCase` tests with injectable clock in `tests/ImportToPlanner.Tests/Credits/ExpirePaidCreditLotsUseCaseTests.cs` (due lot, not-yet-due lot, partial consumption, zero-unused skip)
- [ ] T043 [P] [US4] Add timer function delegate test with NSubstitute use case in `tests/ImportToPlanner.Tests/Credits/PaidCreditExpiryFunctionTests.cs`

### Implementation for User Story 4

- [ ] T044 [US4] Implement `ExpirePaidCreditLotsUseCase` in `src/ImportToPlanner.Commercial/Credits/ExpirePaidCreditLotsUseCase.cs` (cross-partition paid lot query; per-lot `PaidExpiry`; log per-tenant failures; ETag retries)
- [ ] T045 [US4] Add timer-triggered function in `src/ImportToPlanner.Functions/` invoking `ExpirePaidCreditLotsUseCase` (≥ daily schedule; no HTTP surface)
- [ ] T046 [US4] Register Functions host DI for commercial table clients and expiry use case in `src/ImportToPlanner.Functions/Program.cs`
- [ ] T047 [US4] Wire `AddAzureFunctionsProject` for `ImportToPlanner.Functions` with `WithReference(tables)` when commercial mode is on in `src/ImportToPlanner.AppHost/AppHost.cs`
- [ ] T048 [US4] Pass `Storage:CommercialCreditLedgerTable` and billing/ledger settings to the Functions resource in `src/ImportToPlanner.AppHost/AppHost.cs` (no Stripe secrets on Functions)

**Checkpoint**: Expiry runs without a web replica; no silent balance changes.

---

## Phase 7: User Story 5 — Tenant Ledger Survives Commercial Account Deletion (Priority: P1)

**Goal**: Delete and purge account rows do not touch ledger or billing metadata.

**Independent Test**: Seed ledger + billing row → delete account → purge → assert transaction count, paid remaining, and `StripeCustomerId` unchanged.

### Tests for User Story 5

- [ ] T049 [P] [US5] Add delete, restore-within-retention, then purge ledger and billing persistence tests in `tests/ImportToPlanner.Tests/Credits/DeleteCommercialAccountLedgerIsolationTests.cs` (assert derived balance unchanged across delete and restore per US5 AC2; ledger and billing row unchanged after purge per SC-005)
- [ ] T050 [P] [US5] Add architecture test that `DeleteCommercialAccountUseCase` and `PurgeExpiredCommercialAccountsUseCase` do not reference `ICreditLedgerStore` or `ITenantBillingStore` in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`

### Implementation for User Story 5

- [ ] T051 [US5] Confirm `DeleteCommercialAccountUseCase` and `PurgeExpiredCommercialAccountsUseCase` under `src/ImportToPlanner.Commercial/` retain account+audit dependencies only; document in code comments if already compliant (no new ledger/billing injections)

**Checkpoint**: FR-014, FR-015, FR-016 enforced by tests.

---

## Phase 8: User Story 6 — Profile and In-App Purchase Entry (Priority: P2)

**Goal**: Full pack catalogue on `/profile` without shortfall pre-selection; activity shows purchases/refunds; no customer ids in UI.

**Independent Test**: Profile buy credits → checkout → activity line and balance match ledger.

### Tests for User Story 6

- [ ] T052 [P] [US6] Add bUnit profile catalogue without pre-selection, no `cus_` text, and fail-closed balance/ensure error handling (no buy-credits substitute per FR-020) in `tests/ImportToPlanner.Web.Tests/Features/CommercialAccounts/ProfileCreditPurchaseTests.cs`
- [ ] T053 [P] [US6] Add presenter test for `PaidAdjustment` activity labelling in `tests/ImportToPlanner.Tests/Credits/ProfileCreditActivityPresenterTests.cs`

### Implementation for User Story 6

- [ ] T054 [US6] Extend `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` and `Profile.razor.cs` with buy-credits pack list (all published packs, no shortfall highlight) and checkout action via `CreateCreditCheckoutUseCase`
- [ ] T055 [US6] Extend profile credit activity mapping in `src/ImportToPlanner.Commercial/Credits/` (or existing profile use case) for `PaidPurchase`, `PaidExpiry`, and `PaidAdjustment` with UK English labels
- [ ] T056 [US6] Extend `src/ImportToPlanner.Web/Features/CommercialAccounts/` presenter components for paid purchase and refund activity rows (no Stripe customer id in copy)

**Checkpoint**: Profile path matches FR-005 profile rules distinct from preview.

---

## Phase 9: User Story 7 — Self-Hosted and Commercial-Off Have No Paid-Credit Surface (Priority: P1)

**Goal**: No buy UI, webhook route, billing table requirement, or Functions resource when commercial mode is off.

**Independent Test**: Commercial off → preview and profile lack purchase affordances; no paid ledger writes on import.

### Tests for User Story 7

- [ ] T057 [P] [US7] Add commercial-off bUnit tests (no buy controls) in `tests/ImportToPlanner.Web.Tests/Features/Import/HomeCreditPurchaseTests.cs` and `ProfileCreditPurchaseTests.cs`
- [ ] T058 [P] [US7] Assert webhook endpoint not mapped when commercial off in `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` or dedicated `CommercialOffStripeRegistrationTests.cs`

### Implementation for User Story 7

- [ ] T059 [US7] Guard all Stripe, billing table, webhook, and pack UI registration behind `Features:CommercialMode:Enabled` in `src/ImportToPlanner.Web/Program.cs` and `src/ImportToPlanner.Commercial/DependencyInjection.cs`
- [ ] T060 [US7] Ensure `src/ImportToPlanner.AppHost/AppHost.cs` does not add Functions or pass Stripe parameters when `enableCommercialMode` is false

**Checkpoint**: Self-hosted and commercial-off journeys unchanged aside from shared ledger code paths.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Docs, opt-in contract test, format gate, manual quickstart alignment.

- [ ] T061 [P] Add opt-in stripe-mock Checkout Session shape test gated on `STRIPE_MOCK_BASE_URL` in `tests/ImportToPlanner.Tests/Billing/StripeMockCheckoutContractTests.cs`
- [ ] T062 [P] Update `website/content/privacy-and-security.md` per research Decision 15 (organisation ledger and Stripe billing metadata survive individual account deletion; six-month window on account row only)
- [ ] T063 Extend `GetProfileCreditSummaryUseCase` / balance presenters if paid lots affect remaining display edge cases in `src/ImportToPlanner.Commercial/Credits/`
- [ ] T064 Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~PaidCredit|FullyQualifiedName~CreditLedger|FullyQualifiedName~DeleteCommercial|FullyQualifiedName~StripeMock"` and fix failures
- [ ] T065 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [ ] T066 Validate `specs/021-paid-credits-purchase/quickstart.md` steps 1–4 against local Aspire commercial-on configuration (document any gaps in PR notes only)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **User story phases (3–9)** → **Polish (Phase 10)**
- **US2 (Phase 3)** must complete before end-to-end validation of **US1 (Phase 4)** checkout completion (grant path).
- **US4 (Phase 6)** depends on paid lot writes from **US2**.
- **US6 (Phase 8)** depends on **US1** checkout use case and **US2** activity types.

### User Story Dependencies

| Story | Depends on | Independent test focus |
| --- | --- | --- |
| US2 | Phase 2 | Grant, idempotency, refund |
| US1 | Phase 2; grant from US2 (Phase 3) for full E2E | Preview UI, checkout start, confirm gating |
| US3 | Phase 2; paid lots from US2 | FIFO + summary |
| US4 | US2 paid lots | Scheduled expiry |
| US5 | Phase 2 ledger + billing seed | Delete/purge isolation |
| US6 | US1 checkout + US2 ledger entries | Profile catalogue |
| US7 | Registration guards | Commercial-off surfaces |

### Parallel Opportunities

- **Phase 1**: T004, T006 parallel with T002–T003 after T001
- **Phase 2**: T008, T009, T014, T017, T018 parallel after T007
- **Within US2**: T028–T031 parallel; T032–T033 parallel before T034–T035
- **Within US4**: T042–T043 parallel; T044 before T045–T048
- **Polish**: T061, T062 parallel

---

## Parallel Example: User Story 2

```bash
# Tests in parallel (different files):
tests/ImportToPlanner.Tests/Credits/GrantPaidCreditsUseCaseTests.cs
tests/ImportToPlanner.Tests/Credits/AdjustPaidCreditsForRefundUseCaseTests.cs
tests/ImportToPlanner.Tests/Credits/StripeWebhookGrantTests.cs

# Then wire webhook endpoint after use cases exist:
src/ImportToPlanner.Web/Features/Billing/StripeWebhookEndpoint.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 + grant path)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US2) grant/webhook — minimum for real balance increase.
3. Complete Phase 4 (US1) preview purchase UX.
4. **STOP and VALIDATE**: quickstart §1 filters + Sandbox journey steps 5–10.
5. Add US3–US7 and polish incrementally.

### Incremental Delivery

1. Foundation → US2 → US1 (revenue path live).
2. US3 (accounting trust) → US4 (contract expiry) → US5 (deletion safety).
3. US6 (profile top-up) → US7 (commercial boundary) → polish.

### Suggested task counts

| Phase | Tasks |
| --- | ---: |
| Setup | 6 |
| Foundational | 12 |
| US2 | 10 |
| US1 | 10 |
| US3 | 4 |
| US4 | 7 |
| US5 | 3 |
| US6 | 5 |
| US7 | 4 |
| Polish | 6 |
| **Total** | **67** |

---

## Notes

- **FR-018** (UK English, no stack traces or `cus_` in UI): presenter and MudBlazor tasks T024, T025, T052, T055, and T056.
- **FR-019** (no subscriptions or overdraft): one-time Checkout `mode=payment` only; no recurring billing APIs in scope (see plan.md Summary).
- MudBlazor skill applies to all pack UI in Home and Profile.
- Stripe plugin skills `stripe-best-practices` and `stripe-docs` apply during Infrastructure.Stripe implementation.
- After AppHost changes, run `aspire resource web rebuild` and rebuild the Functions resource when modified.
- Do not add AppHost integration tests or grant logic on the checkout success URL alone.
