# CI Notes

CI validates both the application and the AppHost.

## Validation Scope

- Solution restore, format verification, build, and test on `ImportToPlanner.slnx`
- Aspire AppHost restore and build via `dotnet restore src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj` and `dotnet build src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj --no-restore`
- JavaScript syntax validation for tracked `*.js` files via `node --check`

See `.github/workflows/ci.yml` for the full pipeline.

## Practical Notes

- Keep the AppHost build path healthy alongside the main solution build.
- When planner-facing behaviour changes, keep validation in place for both authority paths (`AzureAd:TenantId=organizations` and tenant-specific authority) unless the change is explicitly scoped and documented.
- Use the CI baseline as a minimum bar before enabling hosted rollout.
- Run JavaScript syntax checks locally before pushing UI shell changes:

```bash
git ls-files '*.js' | xargs -n1 node --check
```
