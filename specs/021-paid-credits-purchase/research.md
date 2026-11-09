# Research: Paid Credits Purchase

## Decision 1: Stripe adapter project

- **Decision**: Add `src/ImportToPlanner.Infrastructure.Stripe`. It references `ImportToPlanner.Commercial` (ports and neutral records only) and Stripe.net. `ImportToPlanner.Web` references the Stripe project and registers it only when commercial mode is on. Commercial, Application, and Domain do not reference Stripe.net.
- **Rationale**: Clean Architecture keeps vendor SDKs in an outer adapter. `ImportToPlanner.Infrastructure.Graph` is the existing precedent. Spec 011 kept Stripe out of Commercial so ledger policy stays replaceable. Checkout creation policy (which pack, which tenant, which actor) stays a Commercial use case; the adapter only creates a Checkout Session and verifies webhooks.
- **Alternatives considered**: Stripe types inside `ImportToPlanner.Commercial`. Rejected because a ledger change would compile against Stripe.net. Stripe types only in Web. Rejected because the Blazor host would own SDK mapping that a later worker or function would need to duplicate.

## Decision 2: Stripe-hosted Checkout, not embedded payments

- **Decision**: Create a Checkout Session in `mode=payment` and redirect the browser to Stripe-hosted Checkout. Success and cancel URLs return to preview or profile. The Blazor app does not load Stripe.js.
- **Rationale**: The product contract limits Stripe to checkout, receipts, invoicing, and tax. Hosted Checkout avoids PCI surface and a Content Security Policy change for Stripe.js. One-time packs are the Checkout Sessions case in Stripe’s payments guidance, not PaymentIntents and not Billing subscriptions.
- **Alternatives considered**: Payment Element embedded in Blazor. Rejected as extra UI and CSP scope. Payment Links. Rejected because pack choice and tenant binding must be created per signed-in session. Charges API. Rejected as deprecated.

## Decision 3: Webhook on the web host

- **Decision**: `POST /webhooks/stripe` on `ImportToPlanner.Web`, registered only when commercial mode is on. Anonymous, raw body, signature verified with the webhook signing secret before any ledger write. Commercial-off and self-host do not map the route and do not require Stripe settings at startup.
- **Rationale**: The web app is already the only compute process. Fulfilment must not depend on the browser return URL. Stripe guidance treats webhooks as required for granting access, including asynchronous payment methods.
- **Alternatives considered**: Grant on the success page. Rejected by FR-006 and by Stripe fulfilment guidance. A separate public API host. Rejected because it would be a new AppHost resource for one route.

## Decision 4: Idempotency markers in the tenant ledger partition

- **Decision**: Extend `TableCreditLedgerStore` with unique marker rows in the same tenant partition as lots and transactions:
  - `pay|{externalPaymentId}` — at most one paid lot per confirmed payment
  - `evt|{stripeEventId}` — at most one handling of a Stripe event
  - `adj|{externalAdjustmentId}` — at most one compensating adjustment per refund or dispute
- **Rationale**: Month grants already use a unique row (`grant|{utcYearMonth}`) as the idempotency lock. The same `AddEntity` conflict (HTTP 409) is the duplicate-delivery lock. Partition key remains the Entra tenant id. `ActorUserId` is stored on the transaction for audit and is not part of the key.
- **Alternatives considered**: A global event table outside the tenant partition. Rejected because the command requires tenant partition only, and the signed event carries the server-written tenant id. Relying on Stripe’s retry behaviour without a marker. Rejected because FR-008 requires at most one lot even if two deliveries overlap.

## Decision 5: Tenant binding

- **Decision**: Checkout creation reads tenant and user only from the authenticated commercial session. The adapter writes `tenant_id`, `pack_id`, and `actor_user_id` into Checkout Session metadata and sets `client_reference_id` to the tenant id. The webhook reads tenant id only from that metadata after signature verification. If metadata is missing or the pack id is not in the published catalogue, the handler does not grant and returns a failure status so the event is visible in operations logs.
- **Rationale**: FR-002 forbids client-supplied tenant override. Metadata is trustworthy only because this server set it and Stripe signed the event.
- **Alternatives considered**: A cross-partition checkout index keyed by session id. Rejected as unnecessary if metadata is mandatory and signed. Trusting a return-URL query parameter. Rejected.

## Decision 6: Pack catalogue in configuration

- **Decision**: Published packs live in configuration (`Stripe:CreditPacks`), not in code and not as the system of record inside Stripe Product metadata. Each pack has `PackId`, `CreditQuantity`, `StripePriceId`, `DisplayName`, `AmountMinor`, `Currency`, and `Published`. Checkout rejects unknown, blank, or unpublished pack ids. Preview pre-selects the published pack with the smallest `CreditQuantity` greater than or equal to the shortfall. Profile lists published packs with no pre-selection.
- **Rationale**: FR-004 asks planning to fix sizes and prices as deployment configuration. Credit quantity must be authoritative in the app so a Stripe price change cannot silently grant a different number of credits. Display amounts live beside the price id so the UI can render without a price lookup; operators must keep them aligned with the Stripe Price.
- **Alternatives considered**: Hard-coded packs. Rejected because prices change without a release. Reading credit quantity from Stripe Price metadata at grant time. Rejected because webhook grants would depend on a second Stripe read and could drift from the catalogue the user saw.

## Decision 7: Paid-lot expiry as an Azure Functions timer

- **Decision**: Add `src/ImportToPlanner.Functions`, a single timer-triggered function that calls `ExpirePaidCreditLotsUseCase`. AppHost references it with `AddAzureFunctionsProject` only when commercial mode is on, and wires `WithReference(tables)` to the same `tables` resource `web` already uses. No AppHost integration tests. The sweep stays idempotent: a lot that is already expired writes nothing further.
- **Rationale**: A hosted service runs only while a web replica is up. This app is not a 24/7 process in the short term. AppHost already sets non-production `MinReplicas` to 0, and spec 004’s hosted baseline is scale-to-zero (`minReplicas=0`). Production currently sets `MinReplicas` to 1, but that only keeps a replica for site availability; it does not match idle-site operation, and dropping it back to zero would silently stop expiry. FR-012 and US4 require a schedule that does not wait for someone to sign in. Spec 008 kept Azure Functions as the AppHost resource for scheduled work once credits needed it. The function shares ledger data through the existing tables reference. Aspire may still provision Functions host storage for the runtime; that store is not a second credit ledger.
- **Alternatives considered**:
  - **`PaidCreditExpiryHostedService` beside `CommercialAccountRetentionHostedService`**. Rejected. When the Container App is scaled to zero, or simply has no replica because nobody is using the site, the timer does not run and unused paid credits stay on the books past twelve months.
  - **.NET Worker**. Rejected. On Container Apps it has the same scale-to-zero problem unless it is pinned to a minimum replica, which is a always-on cost for a daily job. It would still need its own AppHost resource and the `tables` reference.
  - **Lazy expiry only on sign-in**. Rejected by FR-012 and US4.

Account purge stays on `CommercialAccountRetentionHostedService` in this feature. It has the same idle-replica gap; moving it is a later change, not part of paid credits.

The function has no HTTP route. Commercial-off and self-host do not register the resource. Unit tests call the use case with a test clock. One test substitutes the use case and asserts the timer function delegates to it. Neither test starts AppHost.

## Decision 8: Ledger extensions, not a new store

- **Decision**: Implement paid writes on `TableCreditLedgerStore` / `ICreditLedgerStore`. Add `CreditEntryType.PaidAdjustment` for refunds and chargebacks. `PaidPurchase` opens a `LotType.Paid` lot with `ExpiresAtUtc = GrantedAtUtc + 12 months`. `PaidExpiry` zeros unused quantity. Consumption keeps `CreditLotSelector`: free monthly first, then oldest `GrantedAtUtc`. Stable tie-break by `LotId` when timestamps match.
- **Rationale**: FIFO and tenant partition already exist. Paid enum values were reserved in spec 011. Balances stay derived. `ActorUserId` remains an optional audit property on the transaction entity.
- **Alternatives considered**: A parallel paid-balance counter. Rejected by FR-003. Per-user ledger partition. Rejected by FR-002 and the command.

## Decision 9: Billing metadata separate from the account row and the ledger

- **Decision**: `ITenantBillingStore` persists Stripe Customer id and receipt or invoice references in a tenant-partition row (`billing`). `DeleteCommercialAccountUseCase` and `PurgeExpiredCommercialAccountsUseCase` keep their current dependencies (`ICommercialAccountStore`, `ICommercialAuditStore`) and must not gain `ICreditLedgerStore` or `ITenantBillingStore`. Confirmed in source: neither use case references the ledger today.
- **Rationale**: FR-014 and FR-016. The six-month window is the account row. Customer and receipt data must survive so Stripe receipts, disputes, and tax remain supportable. The ledger is a different store and must also survive. Privacy copy must say both facts, not imply billing metadata follows the account row.
- **Alternatives considered**: Store the Stripe customer id on the commercial account entity. Rejected because purge would delete it. Store it on the ledger. Rejected because the ledger is the credit system of record, not the payment-provider record.

## Decision 10: Refunds and chargebacks

- **Decision**: On `refund.updated` with status `succeeded`, or `charge.dispute.closed` with status `lost`, append `PaidAdjustment` for the originating lot’s current unused quantity only. Link the lot by `ExternalPaymentId` stored on the paid lot. Fully consumed lots write a zero-quantity marker so the adjustment id is still idempotent and the balance does not go negative. Already-consumed credits stay consumed.
- **Rationale**: FR-021 and the 2026-10-13 clarification. Stripe issues these events asynchronously; the success page cannot do this.
- **Alternatives considered**: Claw back consumed credits. Rejected by the spec. Ignore refunds in V1. Rejected by the clarified spec.

## Decision 11: Tax

- **Decision**: `Stripe:AutomaticTaxEnabled` defaults to false. Checkout sets `automatic_tax` only when that flag is true. Do not enable it until the Stripe account has an active registration in the customer’s jurisdiction.
- **Rationale**: Stripe Tax calculates nothing without a registration, which looks like tax is on when it is not. The product contract still assigns VAT to Stripe; the flag is how operators turn that on deliberately.
- **Alternatives considered**: Always enable `automatic_tax`. Rejected until registration is confirmed.

## Decision 12: Testing and stripe-mock

- **Decision**: Behaviour tests do not call Stripe. They use xUnit v3, NSubstitute for thin ports, and the existing in-memory ledger for stateful cases. An opt-in test may point Stripe.net at Docker stripe-mock:

  `docker run --rm -p 12111:12111 stripe/stripe-mock:latest`

  The test runs only when `STRIPE_MOCK_BASE_URL` is set (for example `http://localhost:12111`). It checks that creating a Checkout Session is accepted by the mock (parameter names and types). It is not part of the default `dotnet test` gate.
- **Rationale**: [stripe-mock](https://github.com/stripe/stripe-mock) is Stripe’s OpenAPI mock. It is stateless, responses are fixtures, it does not reproduce API behaviour, and it does not deliver signed webhooks or honour idempotent business effects. Docker avoids installing Go or vendoring the repository. Webhook grant, idempotency, FIFO, and account deletion need deterministic doubles, which stripe-mock cannot provide.
- **Alternatives considered**: `go install` stripe-mock. Rejected to avoid a Go toolchain. Submodule of stripe-mock. Rejected as repository pollution. Testcontainers always-on in CI. Rejected because the default suite must pass without Docker, and the mock cannot assert the ledger. Stripe CLI webhook forwarding as the automated grant test. Rejected as an external network dependency; useful in the manual Sandbox quickstart. Live Stripe as the test environment. Rejected. See Decision 16.

## Decision 13: Cursor Stripe plugin skills

- **Decision**: Implementation uses the installed Cursor Stripe plugin skills `stripe-best-practices` and `stripe-docs`. Relevant references are payments (Checkout Sessions and webhook fulfilment), security (restricted keys, webhook signatures, no keys in source), and tax (do not enable `automatic_tax` without a registration).
- **Rationale**: Those skills match this integration. They already state Checkout Sessions for one-time payments, webhook fulfilment for both completed and async-success events, `StripeClient` instead of a global API key, restricted keys, and Stripe.net 53 against API version 2026-09-30.endive.
- **Alternatives considered**: Connect, Metronome, Stripe Apps, Stripe Pay, and Stripe Projects skills. Not applicable: this is not a marketplace, not usage-based billing, not a Dashboard app, and not a provisioning flow. The repo does not need those skills for V1.

## Decision 14: Secrets on the existing web resource

- **Decision**: When commercial mode is on, AppHost passes `Stripe__ApiKey` and `Stripe__WebhookSecret` from secret parameters into `web`, same style as other commercial secrets. Those values are Sandbox credentials (Decision 16). Startup validation requires the key, webhook secret, at least one published pack, and `Storage:CommercialBillingTable` only when commercial mode is on. The Functions project does not receive Stripe keys.
- **Rationale**: Spec 009 already gates table clients on commercial mode. Stripe configuration follows that gate so self-host startup stays free of Stripe. Expiry does not call Stripe.
- **Alternatives considered**: A Key Vault resource in AppHost. Deferred; Aspire parameters are the current secret path for this app. Add Key Vault only if operators move other secrets there.

## Decision 16: Stripe Sandboxes for every test that contacts Stripe

- **Decision**: Any test or manual session that calls Stripe uses a [Stripe Sandbox](https://docs.stripe.com/sandboxes.md), never the live environment. Use a separate sandbox for local development and another for staging. Use a restricted key (`rk_`) from that sandbox. Live-mode prefixes (`sk_live_`, `rk_live_`) are forbidden in Development, CI, staging, and this feature’s acceptance checks. stripe-mock remains the offline shape check and is not a Sandbox. The shared account-wide test mode is not the default; Sandboxes isolate settings and data from live mode.
- **Rationale**: The maintainer will develop and accept payments against Sandboxes. Stripe’s current guidance for new integrations is separate Sandboxes rather than the shared test-mode sandbox, and never live keys in source or in non-production hosts. Sandboxes are already the supported way to run Checkout, signed webhooks, refunds, and tax without moving money.
- **Alternatives considered**: Live mode with a tiny pack price. Rejected. It charges real cards, mixes production customers into the ledger design, and is unnecessary while Sandboxes exist. Shared test mode only. Rejected as the default because a Sandbox keeps this product’s test data away from any other test-mode work on the same Stripe account. stripe-mock as the only Stripe stand-in. Rejected because it cannot complete a signed webhook grant.

## Decision 15: Privacy wording

- **Decision**: Update `website/content/privacy-and-security.md` so the six-month window applies to the user’s commercial account row only. State that the organisation credit ledger is kept, and that Stripe customer and receipt metadata are stored for the organisation and are not removed when one user deletes their account. Remove the “we will update this page when that ships” deferral for Stripe, and stop describing billing metadata as following commercial account configuration in a way that sounds per-user.
- **Rationale**: FR-016 and the spec assumption on public privacy copy. The page already says the ledger survives deletion; it does not yet separate Stripe retention from that account row. End-user-docs skill applies when the page is edited.
