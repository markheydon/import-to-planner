# Quickstart: Paid Credits Purchase

Validation guide for spec 021. Contracts: [paid-credits-purchase-contracts.md](./contracts/paid-credits-purchase-contracts.md). Data: [data-model.md](./data-model.md).

## Prerequisites

- .NET SDK from `global.json`
- Docker, only for the optional stripe-mock check
- A Stripe Sandbox for every manual or staging payment journey. Create Prices, the restricted key, and the webhook secret in that Sandbox. Do not use a live Stripe account, live Prices, or live keys.
- Commercial mode configuration when exercising the hosted path

Default automated tests do not need Stripe keys or Docker. They must not call live Stripe.

## 1. Default tests

From the repository root:

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~PaidCredit|FullyQualifiedName~CreditLedger|FullyQualifiedName~DeleteCommercial"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected:

- Webhook grant creates one paid lot and does not grant on replay.
- FIFO consumes free monthly credits before the older paid lot.
- Refund removes only the unused remainder once.
- Account delete and purge leave ledger transactions and the billing customer id unchanged.
- Format check exits 0.

There is no AppHost test project. Do not add `Aspire.Hosting.Testing`.

## 2. Optional stripe-mock contract check

Stripe’s mock server is a Docker image. It checks request shape against the OpenAPI spec. It does not store charges or send webhooks.

```bash
docker run --rm -p 12111:12111 stripe/stripe-mock:latest
```

In another shell:

```bash
export STRIPE_MOCK_BASE_URL=http://localhost:12111
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~StripeMock"
```

Expected: the opt-in test creates a Checkout Session against the mock and gets a fixture response. Unset `STRIPE_MOCK_BASE_URL` and the same test skips, so a machine without Docker still passes the default suite.

Sample probe (not a substitute for the test):

```bash
curl -i http://localhost:12111/v1/checkout/sessions \
  -u sk_test_123: \
  -d mode=payment \
  -d "line_items[0][price]=price_123" \
  -d "line_items[0][quantity]=1" \
  -d success_url="https://example.com/success" \
  -d cancel_url="https://example.com/cancel"
```

The mock may accept this and return a fixture. That does not mean a Sandbox payment would grant credits. The mock is not a Stripe Sandbox and it is not live mode.

## 3. Stripe Sandbox journey (local or staging)

Use a Sandbox for this whole section. Refuse to continue if the API key or webhook secret is a live-mode key.

1. In the Stripe Dashboard, open a Sandbox (a separate one for staging if this is not your local sandbox). Create the pack Prices there.
2. Set that Sandbox’s restricted API key and webhook signing secret on the web app (Aspire parameters or user secrets). Do not commit them. Do not copy live keys into these slots.
3. Configure at least two published packs with those Sandbox Price ids.
4. Leave `Stripe:AutomaticTaxEnabled` false unless this Sandbox has an active tax registration.
5. Sign in as a non-allowlisted commercial user whose remaining credits are below a preview would-create count.
6. Confirm the shortfall warning, the recommended pack, and that Confirm import stays disabled.
7. Start checkout, abandon it, return to preview, and confirm the balance is unchanged.
8. Complete payment with a Sandbox test card. Forward webhooks with the Stripe CLI logged into that Sandbox if the app is on localhost (`stripe listen --forward-to localhost:<port>/webhooks/stripe`).
9. Refresh preview. Remaining credits increase by the pack size once. Confirm enables when would-create fits.
10. Replay the event from the Sandbox. Balance does not increase again.
11. Open `/profile` and confirm the purchase appears without a Stripe customer id, and the catalogue has no shortfall pre-selection.
12. Delete the commercial account. Ledger balance and the billing customer row remain. Restore within retention does not duplicate credits.

Refund and dispute behaviour against the Sandbox is optional extra evidence. Twelve-month expiry is proved by the use-case test with a clock, then by the Functions timer in a commercial-mode Aspire run (invoke the timer or wait for the schedule). Do not rely on the web app staying scaled up. There is no live-mode step in this guide.

## 4. Commercial-off

Run the import preview with commercial mode disabled. Buy-credits controls, `/webhooks/stripe`, and the expiry function are absent. Startup does not require Stripe settings or the Functions resource.
