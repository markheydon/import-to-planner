# CI Notes

CI validates both the application and the AppHost.

## Validation Scope

- Solution restore, format verification, build, and unit/integration tests on
  `ImportToPlanner.slnx` (excluding `ImportToPlanner.E2E.Tests`)
- Playwright browser journeys in `tests/ImportToPlanner.E2E.Tests/` via
  `.github/workflows/ci-e2e.yml`
- AppHost validation as part of solution-level restore/build on `ImportToPlanner.slnx`
- JavaScript syntax validation for tracked `*.js` files via `node --check`

See `.github/workflows/ci.yml` for the core .NET pipeline and
`.github/workflows/ci-e2e.yml` for Playwright E2E validation.

## Practical Notes

- Keep the AppHost included in the solution build path and avoid changes that require separate CI-only steps unless they are explicitly added to `.github/workflows/ci.yml`.
- When planner-facing behaviour changes, keep validation in place for both authority paths (`AzureAd:HomeTenantId=multiple` and tenant-specific authority) unless the change is explicitly scoped and documented.
- Use the CI baseline as a minimum bar before enabling hosted rollout.
- Run JavaScript syntax checks locally before pushing UI shell changes:

```bash
git ls-files '*.js' | xargs -n1 node --check
```
