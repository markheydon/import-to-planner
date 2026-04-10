# Import To Planner

Single-use Blazor utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting.

## Current Implementation Status

This repository now includes:

- Clean architecture solution structure:
  - `src/ImportToPlanner.Web`
  - `src/ImportToPlanner.Application`
  - `src/ImportToPlanner.Domain`
  - `src/ImportToPlanner.Infrastructure.Graph`
  - `tests/ImportToPlanner.Tests`
- CSV parser and validation pipeline for columns:
  - `Task Name` (required)
  - `Description`
  - `Priority`
  - `Bucket`
  - `Goal`
- Priority validation/mapping:
  - Accepts numeric `0-10`
  - Accepts `Urgent`, `Important`, `Medium`, `Low`
- Dry-run planning with idempotency rules:
  - Plan create/reuse
  - Bucket create/reuse
  - Goal label create/reuse
  - Task create/skip
  - Duplicate task names in CSV are skipped after the first occurrence
- Confirmation + execution reporting UI
- Unit tests for parser and orchestrator behavior

## Important Note

The infrastructure currently uses an in-memory `IPlannerGateway` implementation to enable end-to-end UI and orchestration development.

The next implementation step is replacing this with real Microsoft Graph delegated calls.

## Microsoft Graph Constraints Accounted For

- Planner plans are container-scoped (group-backed plan container expected).
- User context is delegated.
- Tasks are idempotent by app logic (title-based check in selected plan).
- Goal is mapped to Planner category/label behavior in the app model.

## Run Locally

```bash
dotnet restore
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

## CSV Example

```csv
Task Name,Description,Priority,Bucket,Goal
Create user stories,Draft sprint stories,Important,Backlog,Delivery
Review architecture,Validate boundaries,5,Architecture,Quality
Prepare release notes,,Low,,Communication
```

## Next Steps

1. Implement real Graph gateway in `src/ImportToPlanner.Infrastructure.Graph`.
2. Add Entra ID delegated auth in the web app and token acquisition.
3. Replace in-memory groups with real user-accessible group lookup.
4. Add integration tests for Graph error handling and permission failures.

## Open Source Compliance Report

**Timestamp:** 2026-04-10
**Overall Status:** NEEDS WORK ⚠️

This repository already has a solid technical baseline (clear README, active issues, testable .NET solution). The main open-source-release gaps are legal and community-governance files, plus incomplete visible evidence for repository security controls.

### 📄 File Compliance

| File | Status | Notes |
|------|--------|-------|
| `LICENSE` | ❌ Missing | No root `LICENSE` file found. This is a release blocker because downstream users need explicit usage rights. |
| `README.md` | ⚠️ Present but thin | Present and meaningful, but currently under the recommended depth (>100 lines) and missing explicit install/contributing sections. |
| `CODEOWNERS` | ❌ Missing | No `CODEOWNERS` found at root or `.github/CODEOWNERS`. |
| `CONTRIBUTING.md` | ❌ Missing | No contribution process guidance (issues/PR workflow, standards, CLA/DCO policy). |
| `SUPPORT.md` | ❌ Missing | No user support path documented. |
| `CODE_OF_CONDUCT.md` | ❌ Missing | No recognized community conduct policy found. |
| `SECURITY.md` | ❌ Missing | No documented vulnerability disclosure process found. |

### 🔒 Security Configuration

| Setting | Status | Notes |
|--------|--------|-------|
| Secret scanning | ⚠️ Unknown | No explicit repository-level setting data was available through the accessible API surface in this session. Verify in GitHub Settings > Security. |
| Dependabot alerts / security updates | ⚠️ Unknown | Could not confirm enabled status from accessible API metadata; no `.github/dependabot.yml` file detected. |
| Code scanning (CodeQL) analyses | ❌ Not observed | No workflow under `.github/workflows/` and no check-run evidence of code scanning analyses on the current PR. |
| Branch protection on default branch | ⚠️ Unknown | Could not confirm protection rules (required reviews/checks/signed commits) from available API responses in this session. |

### ⚖️ License Analysis

- **Declared repository license (metadata):** `NONE` observed (no SPDX license reported in repository metadata available during this check).
- **`LICENSE` file match:** **Mismatch / not verifiable** (file missing).
- **Dependency manifests found:** `Directory.Packages.props`, 5 `*.csproj` files.
- **NuGet dependency license review (attempted):**
  - `CsvHelper`, `Microsoft.Graph`, `Microsoft.Identity.Web`, `Microsoft.FluentUI.AspNetCore.Components`, `coverlet.collector`, `xunit`, and related test/runtime packages appear to use permissive licenses in typical upstream usage.
  - **No GPL/AGPL/LGPL strong-copyleft package was identified from the declared direct package set.**
  - Treat this as a preliminary check; confirm with a formal SBOM/license scanner before release.

### 📊 Risk Assessment

| Category | Level | Details |
|----------|-------|---------|
| **Business Risk** | 🟡 Medium | No obvious hardcoded secrets were observed in this pass, but repository security controls (secret scanning/branch protection) are not yet confirmed and should be verified before broader release. |
| **Legal Risk** | 🔴 High | Missing `LICENSE` and no declared SPDX metadata creates immediate legal ambiguity for open-source distribution. |
| **Open Source Risk** | 🔴 High | Most community governance files are currently missing (`CODEOWNERS`, `CONTRIBUTING.md`, `SUPPORT.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`). |

### 📋 Recommendations

**Must Fix (blocking release)**

1. Add a root `LICENSE` file and ensure it matches repository metadata SPDX declaration.
2. Add `SECURITY.md` with a clear vulnerability reporting process and response expectations.
3. Add `CODE_OF_CONDUCT.md` (for example, Contributor Covenant 2.1).

**Should Address**

1. Add `.github/CODEOWNERS` with at least one maintainer/team.
2. Add `CONTRIBUTING.md` covering issue flow, PR process, coding/testing expectations, and CLA/DCO stance.
3. Add `SUPPORT.md` describing where users should request help.
4. Enable and verify branch protection (required reviews + required checks) on the default branch.
5. Enable Dependabot security updates and code scanning (CodeQL or equivalent).

**Nice to Have**

1. Expand README beyond the current implementation snapshot into full user/developer install and contribution sections.
2. Add an automated license/SBOM check in CI to continuously flag copyleft or unknown-license dependencies.

**Helpful references**

- GitHub Open Source guides: https://opensource.guide/
- About repository security settings: https://docs.github.com/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches
- Secure coding and disclosure process guidance: https://docs.github.com/code-security/getting-started/adding-a-security-policy-to-your-repository
