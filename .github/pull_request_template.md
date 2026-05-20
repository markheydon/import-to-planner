## Summary

<!-- Describe what changed and why. -->

## Related Issue Or Discussion

<!-- Link the related GitHub issue, discussion, or write N/A. -->

## Change Type

- [ ] Bug fix
- [ ] Feature
- [ ] Refactor
- [ ] Documentation
- [ ] Tests only
- [ ] Build, CI, or tooling

## Verification

<!-- List the checks you ran and any manual validation you performed. -->

- [ ] `dotnet restore ImportToPlanner.slnx`
- [ ] `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [ ] `dotnet build ImportToPlanner.slnx`
- [ ] `dotnet test ImportToPlanner.slnx`
- [ ] `git ls-files '*.js' | xargs -n1 node --check`
- [ ] `dotnet restore apphost.cs` and `dotnet build apphost.cs --no-restore` when AppHost or hosted-path changes are affected
- [ ] Additional manual verification completed where needed

Verification notes:

## Planner Gateway And Runtime-Mode Impact

- [ ] Not applicable
- [ ] I changed planner behaviour and verified both `PlannerGateway:UseGraph=false` and `PlannerGateway:UseGraph=true`
- [ ] I changed planner behaviour but scope is intentionally limited, and I have documented the limitation below

Runtime-mode notes:

## UX And Accessibility Impact

- [ ] Not applicable
- [ ] I changed UI behaviour and checked the affected workflow on desktop
- [ ] I changed UI behaviour and checked the affected workflow on mobile or a narrow layout
- [ ] I reviewed wording and it uses UK English
- [ ] I considered accessibility impact for the affected workflow

UX notes:

## Operational And Performance Impact

- [ ] Not applicable
- [ ] I reviewed operational safety, diagnostics, and secret handling impact
- [ ] I reviewed remote-call or integration impact
- [ ] I reviewed performance impact for changed hot paths or validation/import behaviour

Operational or performance notes:

## Documentation Impact

- [ ] Not applicable
- [ ] I updated `README.md`
- [ ] I updated `CONTRIBUTING.md`
- [ ] I updated relevant docs under `docs/` or `docs-internal/`
- [ ] No documentation update was needed because the change does not affect behaviour, setup, or contributor workflow

## Review Readiness

- [ ] This PR is focused on one logical change
- [ ] All relevant CI checks are expected to pass
- [ ] I addressed Copilot review findings before requesting human review
- [ ] I am prepared to reply to review comments in-thread
