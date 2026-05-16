# Deployment Strategy (Alternative): Multi-Tenant Hosted Rollout

## 1) Objective

Define a practical deployment approach that supports:

- **Self-hosted single-tenant** operation (customer-managed)
- **Shared hosted multi-tenant** operation (service-managed)

without changing the current functional scope of Import To Planner.

---

## 2) Dual-track model

| Track | Intended use | Tenant boundary | Ownership |
|---|---|---|---|
| Self-hosted single-tenant | Organisations running their own instance | One configured Entra tenant | Customer/implementer |
| Shared hosted multi-tenant | Centrally hosted service for multiple organisations | Per-tenant runtime isolation in one hosted app | Service team |

---

## 3) Baseline hosted architecture (low-cost first)

- Keep Aspire AppHost focused on **one web resource only**.
- Deploy that web resource to **Azure Container Apps**.
- Persist only minimal hosted tenant metadata in low-cost storage (Azure Table Storage).
- Do not add extra infrastructure tiers unless evidence justifies it.

### Mandatory baseline choices

1. **Compute/ingress**: Azure Container Apps.
2. **Topology**: Aspire-managed single `web` resource.
3. **Configuration & secrets**: platform-managed Container Apps configuration/secrets by default.
4. **Key Vault**: introduced only when certificate-backed handling requires it.
5. **Telemetry transport**: reuse existing **OTLP** path first.

---

## 4) Configuration, secrets, and telemetry rules

- Environment values are managed per environment in platform configuration.
- Secrets must not be committed to source control.
- Key Vault is optional at baseline and becomes required only for certificate lifecycle/security needs.
- Telemetry must use tenant-safe correlation (pseudonymous where possible), not broad exposure of raw tenant identifiers.
- OTLP reuse is the first option; new paid monitoring components need explicit approval.

---

## 5) Environment model and promotion

Two environments only:

- **beta** (non-production)
- **production**

### Promotion policy

- Deploy to **beta** after CI success.
- Promote to **production** only via **manual approval**.
- Keep beta and production isolated for:
  - settings/secrets
  - identity registrations and redirect URIs
  - telemetry routes
  - operational access controls

---

## 6) CI/CD strategy

### Existing CI gate (must remain)

- `dotnet restore ImportToPlanner.slnx`
- `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- `dotnet build ImportToPlanner.slnx --no-restore`
- `dotnet restore apphost.cs`
- `dotnet build apphost.cs --no-restore`
- `dotnet test ImportToPlanner.slnx --no-build`

### Delivery flow

1. Run CI checks above.
2. Package and deploy hosted build to **beta**.
3. Execute hosted smoke/health checks.
4. Require manual approval for **production** promotion.
5. Support controlled rollback to prior known-good revision in each environment.

### Configuration ownership

| Area | Accountable owner |
|---|---|
| CI workflow definitions | Repository maintainers |
| beta config/secrets | Non-prod deployment owner |
| production config/secrets | Production service owner |
| Entra app registrations/consent setup | Identity owner |
| Certificate lifecycle | Security/deployment owner |
| Telemetry endpoint and retention path | Operations owner |

---

## 7) Hosted operations runbook expectations

### Smoke/health checks (beta and production)

- Endpoint responds and app loads.
- Sign-in and callback flow succeed.
- Planner data access path works.
- Validate/preview/import journey behaves correctly.
- Consent guidance appears correctly when permissions are missing.
- OTLP telemetry arrives with tenant-safe correlation.

### Tenant-aware incident investigation

- Triage by stage: sign-in, consent, validation, preview, import, reporting.
- Use tenant correlation identifiers for diagnosis.
- Resolve to tenant identity only through controlled support procedures.

### Consent/admin-consent troubleshooting

- Distinguish user-actionable errors from administrator consent requirements.
- Provide clear admin-consent guidance without exposing internal secrets or implementation detail.

### Support/privacy boundaries

- No operational need to retain imported CSV/task content.
- Restrict access to tenant-identifying data.
- Do not log secrets, tokens, or certificate material.

---

## 8) AppHost evolution guardrails

### Current constraint (mandatory)

- AppHost remains a **single `web` resource** during initial hosted rollout.

### Deferred resources (not added by default)

- Front Door / Application Gateway
- Dedicated cache
- Queue/worker services
- Always-on relational database
- Additional paid observability stack beyond current OTLP path

### Allowed placeholders (future, approved only)

- Explicit tenant-metadata storage resource mapping
- Edge/ingress resource when justified
- Extended telemetry resources when OTLP baseline is proven insufficient

---

## 9) Cost controls (explicit)

- Prefer consumption-oriented Azure services.
- Keep environment count to **beta + production** only.
- Store only minimal tenant metadata.
- Reuse OTLP pipeline before purchasing new telemetry services.
- Introduce Key Vault only when certificate handling/security requires it.
- No additional always-on components without measured demand and approval.

---

## 10) Documentation deliverables

- Deployment architecture overview (dual-track model)
- Environment and promotion procedure
- CI/CD workflow and rollback guidance
- Configuration/secrets ownership matrix
- Tenant-aware operations and support boundaries
- Consent/admin-consent troubleshooting guide
- Telemetry and privacy handling notes
- Cost assumptions and scale trigger criteria
- AppHost evolution policy (constraints, deferred items, placeholders)

---

## 11) Decision gates before scope expansion

Implementation may not expand beyond this baseline until all gates are approved:

1. **Architecture gate**: hosted baseline (Container Apps + single-web Aspire model) accepted.
2. **Environment gate**: beta/production model and manual promotion accepted.
3. **Consent + tenant metadata gate**: consent handling approach and minimal tenant metadata model accepted.
4. **Telemetry/privacy gate**: tenant correlation, privacy controls, and support access boundaries accepted.

Only after these approvals can additional resources, environments, or platform services be introduced.
