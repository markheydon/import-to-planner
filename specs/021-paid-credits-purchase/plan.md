# Implementation Plan: Paid Credits Purchase

**Branch**: `021-paid-credits-purchase` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/021-paid-credits-purchase/spec.md`, product contract `docs/credits-billing-usage-model.md`, GitHub issue [#125](https://github.com/markheydon/import-to-planner/issues/125). Planning constraints from the command: Stripe adapter placement, webhook on the web host, idempotency and event markers, pack catalogue in configuration, paid-lot expiry hosting choice, ledger extensions, account-deletion isolation, privacy copy, and tests including Docker [stripe-mock](https://github.com/stripe/stripe-mock).

## Summary

Hosted commercial mode gains one-time Stripe Checkout for a configured credit-pack catalogue. Credits are granted only from signature-verified webhooks on the web host, as append-only `PaidPurchase` lots on the existing tenant ledger, with idempotent refund and chargeback adjustments. Stripe.net stays in a new `ImportToPlanner.Infrastructure.Stripe` project. Paid-lot expiry runs as a commercial-only Azure Functions timer, referenced from AppHost and pointed at the existing `tables` resource, because the web app is not a 24/7 process. Account deletion and purge stay off `ICreditLedgerStore` and off tenant billing metadata. Self-hosted and commercial-off builds do not register Stripe, the function, or purchase UI. Payment testing uses Stripe Sandboxes and Docker stripe-mock only; live Stripe keys are out of scope for this feature’s test plan.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`)

**Primary Dependencies**: Existing Blazor Interactive Server and MudBlazor (pack choice on preview and `/profile`), Commercial credit ledger and profile use cases, Aspire `tables` already wired when commercial mode is on. New: Stripe.net **53.x** (latest at planning; pin the package in `Directory.Packages.props`), `StripeClient` instance (not the deprecated static API key), API version **2026-09-30.endive** unless a newer Stripe.net default is current at implementation. Stripe-hosted Checkout redirect (no Stripe.js in the Blazor app).

**Storage**: Existing AppHost `tables` (Azurite locally), shared by `web` and the expiry function via `WithReference(tables)`. Same `CommercialCreditLedger` table for paid lots, purchase and expiry transactions, and idempotency markers, all in the tenant partition. New tenant-scoped billing table `Storage:CommercialBillingTable` (default `CommercialBilling`) for the Stripe Customer id and receipt references. No second credit-data account, SQL, Redis, or queue. The Functions host may use Aspire host storage for its own runtime; that account is not the ledger.

**Testing**: xUnit v3, NSubstitute, built-in Assert, in `ImportToPlanner.Tests` and bUnit in `ImportToPlanner.Web.Tests`. Mandatory scenarios: webhook grant, duplicate-event idempotency, free-then-oldest-paid FIFO, refund unused quantity, delete-account ledger persistence. The expiry use case is tested with a clock; the timer function is a thin delegate test. No live Stripe environment. Human and staging payment checks use Stripe Sandboxes only (separate local and staging sandboxes, never `sk_live_` / `rk_live_`). Opt-in Docker contract check against `stripe/stripe-mock` (HTTP port 12111) for request shape only. No `Aspire.Hosting.Testing`. No Go toolchain and no vendored stripe-mock source. Playwright only if a later journey is explicitly requested.

**Target Platform**: Linux ASP.NET Core on Azure Container Apps via the existing `web` resource, which may scale to zero. Paid expiry on Azure Functions timer trigger so the sweep runs while `web` has no replica. Local Aspire plus Azurite. Stripe Sandboxes for every payment test that leaves the process.

**Project Type**: Layered Blazor web app, a Stripe adapter library, and a commercial-only Azure Functions timer project

**Performance Goals**: Checkout creation is one Stripe call plus one tenant billing read/write. Webhook handling is one tenant-partition batch (marker + lot + transaction). Expiry is a daily cross-partition lot scan acceptable at current commercial volume; per-tenant failures are logged and do not rewrite other tenants.

**Constraints**: Inward dependencies only. No Stripe.net, Azure Tables, or MudBlazor in Application or Domain. Commercial must not reference Stripe.net. The Functions project references Commercial and table clients only, not Stripe.net. UK English UI. Sandbox restricted API key (`rk_`) and webhook signing secret via Aspire parameters / user secrets, never in source, and never a live-mode key for development, CI, or staging. No `payment_method_types`. `automatic_tax` stays off unless `Stripe:AutomaticTaxEnabled` is true and the operator has an active Stripe Tax registration in that sandbox. Self-host and commercial-off must not require Stripe keys, the billing table, or the Functions resource. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`. No AppHost integration tests.

**Scale/Scope**: One Stripe class library, one Functions project with a single daily timer, one anonymous webhook route, configuration-backed pack list, preview and profile purchase UI, ledger write methods for paid purchase, refund, and expiry. No subscriptions and no Worker. Account-retention sweep stays on the existing web hosted service in this feature.

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below — still passes.*

- **I. Dependency Direction**: Grant, refund, and expiry policy live in Commercial. `ImportToPlanner.Infrastructure.Stripe` references Commercial abstractions and Stripe.net only. `ImportToPlanner.Functions` references Commercial and calls `ExpirePaidCreditLotsUseCase`. Web maps HTTP and session identity. Application and Domain stay unchanged. Commercial does not reference the Stripe or Functions projects.
- **II. Technology-neutral core**: Ledger models use opaque `ExternalPaymentId` and `ExternalAdjustmentId`. Stripe event objects, Checkout Session types, and webhook signatures stay in the Stripe adapter.
- **III. Explicit boundaries**: Checkout, grant, refund, and expiry use request/response records. Presenters own pack labels, shortfall highlight, and UK English errors. The webhook adapter maps a verified event to those records.
- **IV. Replaceable frameworks**: Stripe.net is confined to one project. A different provider would replace that project and the webhook parser. Tables remain behind `ICreditLedgerStore` and a new billing-metadata port.
- **V. Traceability**: Work maps to spec 021 FR-001–FR-021, US1–US7, and issue #125. Profile activity extends spec 020. Privacy copy is FR-014/FR-016.
- **VI. Testable behaviour**: Use cases run against the in-memory ledger and NSubstitute doubles. Webhook tests feed a verified domain event without calling Stripe. The timer function is tested as a delegate. stripe-mock is an extra contract check, not the proof of grant or idempotency. Sandbox journeys are manual evidence, not the unit gate, and they are not live mode.
- **VII. Explicit failures**: Invalid signature, unknown pack, missing checkout metadata, ledger write failure, and expiry tenant failure are distinct outcomes. The webhook returns a non-success status when the grant did not commit, so Stripe retries. UI never shows raw Stripe errors or customer ids.
- **VIII. Security**: Webhook is anonymous and fails closed without a valid signature. Tenant id comes from server-written Checkout Session metadata after verification, never from the client. Secrets stay in parameters. Development, CI, and staging accept sandbox keys only. Commercial-off does not expose checkout or require the signing secret. The function has no anonymous HTTP surface. Actor user id is audit-only.
- **IX. Quality evidence**: Architecture scan must reject Stripe.net references outside `ImportToPlanner.Infrastructure.Stripe`. Format gate at implement. No AppHost test project.
- **X. Self-hosted viability**: Stripe registration, webhook route, pack UI, and the Functions expiry resource exist only when commercial mode is on. Import without commercial mode is unchanged.

No unjustified gate failure. Both new projects are recorded under Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/021-paid-credits-purchase/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── paid-credits-purchase-contracts.md
└── tasks.md             # /speckit-tasks — not created here
```

### Source Code (repository root)

```text
src/ImportToPlanner.Infrastructure.Stripe/     # NEW — StripeClient, signature verify, Checkout mapping
src/ImportToPlanner.Commercial/
├── Abstractions/ICreditLedgerStore.cs        # paid purchase, refund, expiry methods
├── Abstractions/ITenantBillingStore.cs        # NEW — customer id, not the account row
├── Credits/ExpirePaidCreditLotsUseCase.cs
├── Credits/GrantPaidCreditsUseCase.cs
├── Credits/AdjustPaidCreditsForRefundUseCase.cs
├── Credits/CreateCreditCheckoutUseCase.cs     # pack lookup + billing customer; no Stripe types
├── Credits/Storage/TableCreditLedgerStore.cs  # PaidPurchase, PaidExpiry, PaidAdjustment markers
└── Billing/TableTenantBillingStore.cs
src/ImportToPlanner.Functions/                   # NEW — timer only; calls ExpirePaidCreditLotsUseCase
src/ImportToPlanner.Web/
├── Features/Billing/StripeWebhookEndpoint.cs  # POST when commercial mode is on
├── Features/Import/Pages/Home/                # pack list + recommended pack
└── Features/CommercialProfile/                # catalogue, no shortfall pre-selection
src/ImportToPlanner.AppHost/AppHost.cs         # Functions project + tables reference when commercial on
website/content/privacy-and-security.md        # ledger vs Stripe customer retention
tests/ImportToPlanner.Tests/Credits/           # grant, idempotency, FIFO, refund, expiry, delete
tests/ImportToPlanner.Tests/Billing/           # stripe-mock opt-in; sandbox keys never live
tests/ImportToPlanner.Web.Tests/               # bUnit preview + profile pack UI
```

**Structure Decision**: Add `ImportToPlanner.Infrastructure.Stripe` on the same pattern as `ImportToPlanner.Infrastructure.Graph`: vendor SDK at the outer edge, referenced by `ImportToPlanner.Web` only. Add `ImportToPlanner.Functions` for the daily expiry timer, referenced from AppHost only when commercial mode is on, with `WithReference(tables)` on the existing tables resource. Do not put Stripe.net in Commercial or in the function. Do not run expiry inside `CommercialAccountRetentionHostedService` or a Worker. Do not add AppHost integration tests.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New project `ImportToPlanner.Infrastructure.Stripe` | Constitution II and IV require the Stripe SDK and event shapes to stay outside Commercial policy. Graph already uses a dedicated infrastructure project for the same reason. | Stripe.net inside Commercial would couple ledger policy to a vendor SDK (explicitly avoided in spec 011). Stripe.net only inside Web would mix HTTP hosting, Blazor, and the SDK, and would make the adapter harder to replace. |
| New project `ImportToPlanner.Functions` | Paid expiry must run on a schedule while the web app is scaled to zero or otherwise idle. Spec 008 left Functions as the AppHost resource for that scheduled work. | A hosted service or Worker inside or beside `web` only runs while a web replica is up. Production `MinReplicas` is 1 in AppHost today, but non-production is already `MinReplicas` 0, spec 004’s hosted baseline is scale-to-zero, and short-term traffic will not keep a replica alive overnight. That misses FR-012. |

## Phase 0 Research

See [research.md](./research.md). Technical Context has no NEEDS CLARIFICATION remaining.

## Phase 1 Design

- [data-model.md](./data-model.md) — packs, billing metadata, paid lots, markers, entry types.
- [contracts/paid-credits-purchase-contracts.md](./contracts/paid-credits-purchase-contracts.md) — checkout, webhook, ledger, expiry, deletion, UI, tests.
- [quickstart.md](./quickstart.md) — unit tests, Docker stripe-mock, and Stripe Sandbox journeys (no live mode).

### Constitution re-check (post-design)

Still passes. Application and Domain are untouched. Commercial owns grant, refund, expiry, and pack selection policy through neutral types. The Stripe project is the only Stripe.net reference. The Functions project is a replaceable timer around the same expiry use case. Account deletion does not gain a ledger or billing dependency. Self-host registration stays commercial-off. Webhook trust boundary, sandbox-only test keys, and secret handling are specified. Failures are structured. Both extra projects are justified above.

## Implementation notes (for `/speckit-tasks`)

- During implementation, follow `docs/engineering-policies.md`, the C# and Blazor rule, and the MudBlazor skill for pack UI. Public copy under `website/content/` follows the end-user-docs skill.
- Stripe implementation decisions follow the Cursor Stripe plugin skills `stripe-best-practices` (payments, security, and tax references) and `stripe-docs` for API lookups. Do not pull Connect, Metronome, Stripe Apps, or Stripe Projects into this feature.
- Fulfil from `checkout.session.completed` and `checkout.session.async_payment_succeeded` only when `payment_status` is `paid`. Also handle `checkout.session.async_payment_failed` as no grant. Refund and lost-dispute events call the compensating use case.
- Omit `payment_method_types`. Instantiate `StripeClient` per process from configuration. Prefer a restricted key.
- Leave `automatic_tax` disabled unless configuration explicitly enables it after an active tax registration exists in the sandbox in use.
- Development, CI, and staging configuration must use a Stripe Sandbox restricted key and that sandbox’s webhook secret. Reject live-mode key prefixes (`sk_live_`, `rk_live_`) outside a deliberate production deployment. Do not use the live Stripe environment to develop or accept this feature.
- After coding, if AppHost is running, rebuild `web` and the functions resource. AppHost gains the Functions project only; do not add an AppHost test.
- Add both projects to `ImportToPlanner.slnx` and package versions to `Directory.Packages.props`. Web references the Stripe project. The Functions project references Commercial. Commercial references neither. AppHost gains `Aspire.Hosting.Azure.Functions`.
