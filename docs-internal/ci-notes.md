# CI Notes

CI validates both the application and the AppHost.

## Validation Scope

- Solution restore, format verification, build, and test on `ImportToPlanner.slnx`
- Aspire AppHost restore and build via `dotnet restore apphost.cs` and `dotnet build apphost.cs --no-restore`

See `.github/workflows/ci.yml` for the full pipeline.

## Practical Notes

- Keep the AppHost build path healthy alongside the main solution build.
- When planner gateway behaviour changes, keep validation in place for both runtime modes unless the change is explicitly scoped to one mode and documented as such.
- Use the CI baseline as a minimum bar before enabling hosted rollout.
