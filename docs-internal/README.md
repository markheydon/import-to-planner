# Internal Documentation

This directory contains internal documentation for the project, including guidelines for agents and other internal processes.

It is intended for use by developers and maintainers of the project, and may include implementation notes, internal policies, and other information that is not meant for public consumption. Public-facing documentation should be placed in the `docs/` directory.

---

## Aspire Production-Readiness Checklist (Planning Only)

This section tracks deployment preparation only. It does **not** add new runtime resources or change current implementation behaviour.

- [ ] Keep `apphost.cs` scope to a single `web` resource unless scaling requirements are explicitly approved.
- [ ] Confirm hosted environment secrets/config handoff for `PlannerGateway:UseGraph`, `AzureAd:*`, certificate credential source, and `OTEL_EXPORTER_OTLP_ENDPOINT`.
- [ ] Keep `/health` and `/alive` unexposed in non-development environments unless an authenticated or private ingress policy is in place.
- [ ] Ensure CI keeps validating current scope: `dotnet build ImportToPlanner.slnx`, `dotnet test ImportToPlanner.slnx`, `dotnet restore apphost.cs`, and `dotnet build apphost.cs --no-restore`.
- [ ] Record deployment handoff ownership for AppHost settings and web app secrets before enabling hosted rollout.

### Runtime configuration by environment

| Key | Local development (default) | Hosted environment (production-ready expectation) |
| --- | --- | --- |
| `PlannerGateway:UseGraph` | `false` by default for in-memory mode; set `true` only when testing real tenant flows locally. | `true` when the hosted instance should call Microsoft Graph; keep `false` only for non-production smoke environments. |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Instance`, `AzureAd:CallbackPath` | Store in user secrets for local Graph-mode testing. | Provide via platform-managed app settings/secrets; never commit values. |
| `AzureAd:ClientCertificates:0:SourceType`, `AzureAd:ClientCertificates:0:CertificateDiskPath`, `AzureAd:ClientCertificates:0:CertificatePassword` | Use `SourceType: Path` with an absolute Linux/WSL/macOS-visible path to a local `.pfx`. | Prefer a cloud-native certificate source (for example Key Vault/certificate store integration) and avoid mounted disk-path certificates where possible. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Usually unset unless developing against a local collector. | Set to the hosted OTLP collector endpoint to export logs/metrics/traces; when unset, OTLP export remains disabled. |

### Health endpoint exposure policy

- Current code only maps `/health` and `/alive` in Development (`MapDefaultEndpoints`).
- For non-development environments, keep these endpoints disabled by default.
- If enabled later for operations tooling, expose them only through private networking or authenticated ingress and document the owner of that policy.

### Aspire AppHost deployment handoff checklist

1. Confirm `apphost.cs` still defines only the `web` app and required launch profile.
2. Hand off hosted configuration values (`PlannerGateway:UseGraph`, `AzureAd:*`, certificate source, OTLP endpoint) to the deployment owner as secret-backed settings.
3. Verify redirect URI and delegated Graph permissions remain aligned with the target hosted URL.
4. Run CI-equivalent validation before rollout: solution build/tests plus AppHost build.
5. Capture post-deployment smoke checks (sign-in, preview, execute, telemetry flow) before enabling wider access.

### Aspire-compatible Azure resource options (cost-first)

| Stage | Recommended low-cost options | Notes |
| --- | --- | --- |
| As-is (single web resource) | Azure Container Apps (consumption/serverless profile) + optional Azure Key Vault for certificate material | Keeps baseline cost low and aligns with Aspire deployment tooling when only one web process is hosted. |
| Telemetry-enabled hosted runs | Existing OTLP collector endpoint first, or minimal Azure Monitor/OpenTelemetry pipeline | Prefer reusing an existing collector to avoid extra always-on cost. |
| Future scaling (if approved) | Container Apps scale rules, optional Front Door/App Gateway, and optional Key Vault references | Add only when measurable demand requires it; keep DB/cache/queue out of scope unless a separate issue approves them. |

## CI Notes

CI validates both application and AppHost:

- Solution restore/build/test on `ImportToPlanner.slnx`
- Aspire AppHost restore/build via `dotnet restore apphost.cs` and `dotnet build apphost.cs --no-restore`

See `.github/workflows/ci.yml` for the full pipeline.

## Known Limitations / Future Work

- Single-tenant configuration only; multi-tenant support is not yet implemented
- Hosted deployment implementation is not yet included
- Certificate loading uses local path-based secrets; Key Vault integration is future work
- Graph beta dependency may require adjustments if API contracts change

## Implementation Roadmap (GitHub Issues)

Tracked in [issue #1](https://github.com/markheydon/import-to-planner/issues/1) with child issues:

1. [#2](https://github.com/markheydon/import-to-planner/issues/2): Implement Graph gateway with beta support
2. [#3](https://github.com/markheydon/import-to-planner/issues/3): Add Entra delegated auth wiring
3. [#4](https://github.com/markheydon/import-to-planner/issues/4): Use real user-accessible containers and plans
4. [#5](https://github.com/markheydon/import-to-planner/issues/5): Add Graph error handling and retries
5. [#6](https://github.com/markheydon/import-to-planner/issues/6): Add Graph gateway test coverage
6. [#7](https://github.com/markheydon/import-to-planner/issues/7): Improve README for new-developer setup
7. [#8](https://github.com/markheydon/import-to-planner/issues/8): Entra app registration and permissions (manual task)
8. [#9](https://github.com/markheydon/import-to-planner/issues/9): Local secret configuration for tenant values (manual task)
