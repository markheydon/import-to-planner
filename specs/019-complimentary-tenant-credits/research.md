# Research: Complimentary Tenant Credits

Decisions for spec 019 / GitHub issue [#129](https://github.com/markheydon/import-to-planner/issues/129). Builds on the shipped [#126](https://github.com/markheydon/import-to-planner/issues/126) ledger (`specs/011-commercial-credit-ledger`).

## 1. Allowlist configuration and wiring

**Decision**: Add an AppHost parameter `complimentaryTenantIds` (default empty string) mapped to `Features:CommercialMode:ComplimentaryTenantIds` on the web resource (same pattern as `enableCommercialMode` → `Features__CommercialMode__Enabled` in `AppHost.cs`). Bind the key in **`ImportToPlanner.Commercial`** via `IOptionsMonitor<CommercialCreditModeOptions>` (or equivalent Commercial-owned options type) registered in `AddCommercial` — not via `ImportToPlanner.Web.CommercialModeOptions`, so dependency direction stays inward (constitution I).

**Rationale**: Matches issue #129 operator workflow; keeps production tenant GUIDs out of source; empty default means no behavioural change. Commercial policy parses the allowlist on each credit-related action from live configuration without referencing Web types.

**Alternatives considered**:

- Hard-coded operator tenant in Application — rejected (FR-004).
- Separate environment variable not surfaced in AppHost — rejected; operators already use Aspire parameters for commercial mode.
- Per-user allowlist — out of scope.

## 2. Allowlist parsing and membership

**Decision**: `IComplimentaryTenantPolicy` in `ImportToPlanner.Commercial` parses the configured string on each `IsComplimentary(tenantId)` call: split on comma, trim, skip empties, `Guid.TryParse` per segment; invalid segments log a structured warning once per distinct invalid token per process (or per configuration change) and are ignored; membership uses `Guid.TryParse` on the session `TenantId` and `guid.Equals(parsedEntry)` (canonical GUID equality, not substring match).

**Rationale**: Satisfies FR-003, FR-015, and clarification B (valid GUIDs still apply when garbage is present). Re-evaluating on each credit action satisfies clarification A without session stickiness.

**Alternatives considered**:

- Case-sensitive string equality on raw config — rejected; risks operator formatting mistakes while still using exact GUID parse, not contains.
- Fail-closed entire list on one bad token — rejected in clarify session (option B).

## 3. Ledger pattern for unlimited tenants

**Decision**: Extend the existing ledger with `CreditEntryType.ComplimentaryGrant` and `LotType.Complimentary`. For complimentary tenants:

- `EnsureCurrentCreditBalance` skips free monthly grant and free expiry; returns success with `ComplimentaryUnlimitedAccess = true` and numeric `RemainingCredits = 0` for storage math only (not shown as exhaustion in UI).
- `RecordUsage` for complimentary tenants writes a paired **Usage** (quantity 1, references import run / task) and **ComplimentaryGrant** (quantity 1, same complimentary lot) in one storage transaction, without consuming `FreeMonthly` or `Paid` lots.
- Derived balance from lots remains ≥ 0; usage rows remain visible for reporting.

**Rationale**: Implements FR-009 without a second import pipeline; avoids polluting free monthly lots (issue alternative “giant complimentary lot”); keeps append-only audit trail.

**Alternatives considered**:

- Meter-only usage rows without grants — rejected; risks negative derived balance unless a separate balance model is introduced.
- Skip ledger for complimentary tenants — rejected (FR-008, operator reporting).
- Grant 25 monthly and ignore cap in UI only — rejected (FR-010).

## 4. Confirm gate, quota, and mid-run execution

**Decision**:

- Web coordinator and `canExecute` treat complimentary balance snapshots as never `InsufficientCredits`.
- `ImportTaskCreationCreditQuota.BeforeCreateAsync` returns `Allow` immediately when `IComplimentaryTenantPolicy` is true (still fail-closed on ledger unavailable).
- `RecordSuccessfulCreateAsync` uses complimentary paired usage recording; never returns `Exhausted` for complimentary tenants.

**Rationale**: Clarification A — execution must not stop solely because credits reached zero; complimentary grants keep the quota path identical while bypassing exhaustion.

**Alternatives considered**:

- Rely on matching grants leaving remaining > 0 — rejected; concurrent imports and numeric remaining checks are fragile.

## 5. UI and summary presentation

**Decision**: Extend balance/snapshot models with `ComplimentaryUnlimitedAccess` (or equivalent neutral flag). Presenters map to UK English **Unlimited** (optional “complimentary” hint) for preview remaining and execution summary; import summary still shows integer **credits used** for the run. Hide buy-credits CTAs when the flag is true (including future #125 hooks guarded by the same flag).

**Rationale**: FR-011, FR-012, FR-013; avoids showing `0` remaining with disabled confirm.

**Alternatives considered**:

- Display `int.MaxValue` as remaining — rejected; misleading in summaries and tests.

## 6. Documentation

**Decision**: Update `docs/credits-billing-usage-model.md` (repo path; issue #129 references `docs-internal/` which is not present in this repository) with a **Complimentary allowlisted tenants** section: configuration key, empty default, ledger behaviour, UI, and interaction with monthly free and paid credits.

**Rationale**: FR-017 and operator discoverability without committing tenant GUIDs.

## 7. Testing strategy

**Decision**: Unit tests in `ImportToPlanner.Tests` (policy parser, ensure skip grant/expiry, complimentary record usage pairs, quota never exhausted). Web bUnit tests (no insufficient warning, confirm enabled, unlimited copy, commercial off unchanged). No AppHost tests (issue #129). Re-run existing 011 credit tests unchanged for non-allowlisted paths.

**Rationale**: `docs/engineering-policies.md` and spec FR-014 regression guard.

**Alternatives considered**:

- Playwright-only validation — rejected for this increment size.
