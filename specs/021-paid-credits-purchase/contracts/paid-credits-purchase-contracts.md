# Contracts: Paid Credits Purchase

Behaviour contracts for spec 021. Models are in [data-model.md](../data-model.md).

## 1. Composition

When `Features:CommercialMode:Enabled` is false:

- Do not register Stripe services, the webhook route, pack UI, billing table client, or the expiry Functions resource.
- Do not require `Stripe:*` or `Storage:CommercialBillingTable`.
- Preview, profile, and import match today’s commercial-off behaviour.

When commercial mode is true:

- Register existing commercial table clients plus `ITenantBillingStore`.
- Register grant, refund, checkout, and expiry use cases.
- Register `ImportToPlanner.Infrastructure.Stripe` with `StripeClient` built from `Stripe:ApiKey`. Development, CI, and staging use a Sandbox restricted key, not a live-mode key.
- Map `POST /webhooks/stripe`.
- Reference `ImportToPlanner.Functions` from AppHost with the existing `tables` resource. The function process calls `ExpirePaidCreditLotsUseCase` on its timer. It does not reference Stripe.net.
- Startup validation fails if the API key, webhook secret, billing table name, or published pack list is missing.

`ImportToPlanner.Commercial` must not reference `Stripe` namespaces. An architecture test scans project references and source for `Stripe.net` outside `ImportToPlanner.Infrastructure.Stripe`.

## 2. Create checkout

`CreateCreditCheckoutUseCase` request:

- Tenant id and actor user id from the server session (required, non-blank)
- Pack id
- Return surface: preview or profile (chooses success and cancel URLs)
- Occurred UTC

Outcomes:

- `Ready` with a redirect URL
- `Rejected` with a diagnostic code: commercial off, complimentary tenant, unknown or unpublished pack, billing store unavailable, payment provider unavailable

Rules:

- Complimentary allowlisted tenants do not receive a checkout URL. Callers hide buy actions.
- The use case resolves the pack from configuration and the Stripe customer id from `ITenantBillingStore`, creating a customer through the payment port when absent.
- The use case does not write a ledger transaction.

Payment port (implemented in Infrastructure.Stripe): create a Checkout Session as in the data model. Each user attempt uses a new Stripe idempotency key so the same pack can be bought again later. Grant idempotency is the payment marker, not this key.

## 3. Webhook

`POST /webhooks/stripe`

- Authentication: none. Trust is the `Stripe-Signature` header verified with `Stripe:WebhookSecret` and the raw body.
- Invalid or missing signature: HTTP 400, no ledger write, no customer id in the response body.
- Verified event the product ignores: HTTP 200.
- Verified `checkout.session.async_payment_failed`: HTTP 200, no grant.
- Verified payment confirmation (`checkout.session.completed` or `checkout.session.async_payment_succeeded`) with `payment_status=paid`: call grant. HTTP 200 only after the ledger commit succeeds. HTTP 500 if the ledger write fails, so Stripe retries.
- Verified refund (`refund.updated` and status `succeeded`) or lost dispute (`charge.dispute.closed` and status `lost`): call adjustment with the same success rule.
- `payment_status=unpaid` on `checkout.session.completed`: do not grant (asynchronous methods). HTTP 200.

Grant request carries tenant, pack, external payment id, stripe event id, actor user id, and occurred UTC from the verified payload. The endpoint must not accept a tenant id from the query string or a JSON field supplied outside the signed event.

## 4. Grant paid credits

`GrantPaidCreditsUseCase`:

- Rejects a pack id that is not published.
- Rejects a tenant id that does not match the value on the grant request (the webhook is the only caller in V1).
- Inserts `evt|{eventId}` and `pay|{externalPaymentId}` with the lot and `PaidPurchase` in one logical commit (table batch where entity group constraints allow; the payment marker insert is the uniqueness lock).
- Lot expiry is granted-at plus twelve months.
- Duplicate event id or duplicate payment id returns success with `AlreadyGranted` and does not change remaining credits.
- Actor user id is copied onto the transaction only.

## 5. Refund and chargeback

`AdjustPaidCreditsForRefundUseCase`:

- Loads the paid lot by `ExternalPaymentId` in that tenant.
- Removes `min(unused, requested)` which in V1 is the current unused quantity (never more, never below zero).
- Inserts `adj|{externalAdjustmentId}`. Replay returns `AlreadyAdjusted`.
- Missing lot: structured failure, HTTP 500, so a reordered event can retry. Do not invent a negative lot.

## 6. Consumption and summary

Unchanged ports: `RecordUsageAsync` and import summary. With paid lots present, selection is free monthly then oldest paid. Summary still splits free versus paid quantities. Complimentary paired usage still does not consume paid lots.

## 7. Expiry sweep

`ExpirePaidCreditLotsUseCase.ExecuteAsync(asOfUtc, cancellationToken)`:

- Selects due paid lots with remaining quantity greater than zero.
- For each, appends `PaidExpiry` and sets remaining to zero without editing earlier transactions.
- Per-tenant or per-lot failure is logged and does not mark the whole sweep as a silent balance fix.
- Azure Functions timer: at least once every 24 hours. The function is a thin adapter over the use case. It reads the ledger through the shared `tables` reference. It is not registered when commercial mode is off.
- The timer runs when the web app has zero replicas. Do not implement this sweep as `BackgroundService` on `web`.
- Does not call account purge and does not delete billing rows.
- No AppHost integration test. Prove the use case with a test clock, and prove the function method invokes that use case with NSubstitute.

## 8. Account deletion

`DeleteCommercialAccountUseCase` and `PurgeExpiredCommercialAccountsUseCase` constructors stay limited to account and audit stores.

Required test: seed a tenant ledger (free and paid transactions) and a billing row; delete the account; restore is out of band; purge the account row; assert ledger transaction count, derived paid remaining, and billing customer id are unchanged.

A second test or architecture assertion fails if those two use cases gain an `ICreditLedgerStore` or `ITenantBillingStore` dependency.

## 9. UI

Commercial on, tenant not allowlisted:

- Preview shortfall: list every published pack; pre-select the smallest pack covering the shortfall; user can choose another; primary action starts checkout. Confirm stays disabled until live remaining covers would-create. Checkout creation alone does not change the warning figures.
- Profile: list every published pack with no pre-selection; action starts checkout.
- Neither surface shows `cus_` identifiers, raw Stripe errors, or stack traces.
- Commercial off or allowlisted: no buy-credits control required to continue.

MudBlazor components for the pack choice and alerts. Presenters build the UK English sentences.

## 10. Tests (default suite)

xUnit v3 and NSubstitute. No Docker.

| Scenario | Proves |
| --- | --- |
| Verified payment event grants one paid lot of pack size and expiry plus twelve months | US2, FR-006, FR-007 |
| Same event id and same payment id delivered again leave one lot | FR-008, SC-002 |
| Abandoned checkout / failed async payment writes nothing | US2 AC1, AC4 |
| Free lot plus two paid lots consume free first, then older `GrantedAtUtc` | US3, FR-009 |
| Refund after partial use removes only unused quantity once; second delivery is a no-op; empty lot does not go negative | FR-021, SC-007 |
| Delete then purge leaves ledger rows and billing customer id in place | US5, FR-014, FR-016, SC-005 |
| Unknown pack id is rejected | Edge case |
| Commercial-off registration does not map the webhook | FR-001, US7 |
| Ledger ensure or balance read unavailable on preview or profile balance-needed action | FR-020, spec 011 FR-030 (fail closed; no invented remaining; insufficient-credits warning not shown as substitute) |

bUnit: preview pre-selection and disabled confirm; profile catalogue without pre-selection; commercial-off has no buy action; preview and profile fail closed when ledger ensure/read fails (no buy-credits substitute for read failure).

## 11. stripe-mock (opt-in)

Not a default test. When `STRIPE_MOCK_BASE_URL` is set, one test constructs `StripeClient` against that base address and creates a Checkout Session for a fixture price. Expect a typed success response from the mock, which only checks that the SDK called a known URL with accepted parameter types.

Do not use stripe-mock to assert webhook signatures, ledger rows, idempotency, FIFO, or refunds. Those behaviours are outside stripe-mock’s contract ([stripe-mock limitations](https://github.com/stripe/stripe-mock)).

Image: `stripe/stripe-mock:latest`. HTTP port `12111` is sufficient. Do not add Go sources, a submodule, or a Homebrew dependency.

## 12. Stripe Sandboxes (required for any real Stripe call)

Development, CI, staging, and acceptance of this feature use Stripe Sandboxes only. Do not use the live Stripe environment. Do not put live-mode keys (`sk_live_`, `rk_live_`) in user secrets, Aspire parameters, or GitHub secrets for those environments.

- One sandbox for local development and a separate sandbox for staging.
- Restricted sandbox keys and that sandbox’s webhook signing secret. Local Aspire runs use AppHost parameters `stripeApiKey` and `stripeWebhookSecret` (`aspire secret set Parameters:stripeApiKey …`), forwarded to `web` as `Stripe__*` when commercial mode is on.
- Pack Price ids in configuration are objects created in the same sandbox.
- `Stripe:AutomaticTaxEnabled` stays false unless that sandbox has an active tax registration.
- stripe-mock does not replace a Sandbox. It never sees a live or sandbox secret.
- A Sandbox does not replace the default unit tests. Ledger, idempotency, FIFO, refund, and delete behaviour stay in-process.
