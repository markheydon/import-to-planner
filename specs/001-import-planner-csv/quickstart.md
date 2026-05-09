# Quickstart: CSV To Planner Import Workflow

## Prerequisites
- .NET 10 SDK installed
- Repository restored successfully
- For Graph mode: tenant/app registration and user secrets configured

## 1. Restore, build, and test
```bash
dotnet restore ImportToPlanner.slnx
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
```

## 2. Run in in-memory mode (default)
```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected:
- Home page loads import workflow UI.
- No tenant credentials required.

## 3. Run in Graph mode (optional)
Configure required user secrets first, then:
```bash
dotnet user-secrets set "PlannerGateway:UseGraph" "true" --project src/ImportToPlanner.Web
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected:
- Auth challenge if not signed in.
- Container/plan list available for current user context.

## 4. Validate clarified behaviour
Use a CSV with mixed valid and invalid rows, plus duplicate task names in the same target plan.

Checkpoints:
1. Validation errors appear for malformed rows and block execution.
2. Preview shows planned actions without writes.
3. Existing task name matches are skipped and reported as `already exists`.
4. Execution continues on independent row failures and reports partial outcomes.
5. Transient row failures retry once then fail if retry also fails.
6. If planner state changes after preview, execution is blocked and requires fresh preview.

## 5. AppHost/CI parity checks
```bash
dotnet restore apphost.cs
dotnet build apphost.cs --no-restore
```

Expected:
- Solution and AppHost paths both remain healthy.

## 6. Optional Aspire run
```bash
aspire start --isolated
aspire describe
aspire logs web
aspire stop
```
