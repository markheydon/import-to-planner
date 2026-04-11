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
  - Goal name create/reuse
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

This repository now has a strong open-source baseline: the core governance files are in place, the declared repository license aligns with the checked-in `LICENSE`, weekly Dependabot updates are configured, and CodeQL analysis is running successfully on the active pull request. The remaining gaps are mostly in repository security hardening and settings verification rather than missing release paperwork.

### 📄 File Compliance

| File | Status | Notes |
|------|--------|-------|
| `LICENSE` | ✅ Present | Root `LICENSE` is present and materially matches the repository's declared MIT license. |
| `README.md` | ✅ Present | Meaningful project overview, local run instructions, CSV example, and roadmap are present. A short contributing link/section would still improve release readiness. |
| `CODEOWNERS` | ✅ Present | `.github/CODEOWNERS` assigns all files to `@markheydon`. |
| `CONTRIBUTING.md` | ✅ Present | Tailored contribution guide covers issues, pull requests, code style, tests, and CLA/DCO stance. |
| `SUPPORT.md` | ✅ Present | Clear support path for bugs, questions, security issues, and platform-level escalation. |
| `CODE_OF_CONDUCT.md` | ✅ Present | Recognized Contributor Covenant code of conduct is adopted. |
| `SECURITY.md` | ✅ Present | Security disclosure process is documented in the conventional `.github/SECURITY.md` location. |

### 🔒 Security Configuration

| Setting | Status | Notes |
|--------|--------|-------|
| Secret scanning | ❌ Not enabled | GitHub secret-scanning check returned `Repository does not have GitHub Advanced Security enabled`, so repo-level secret scanning is not active through that surface. |
| Dependabot alerts / security updates | ⚠️ Partially verified | `.github/dependabot.yml` configures weekly NuGet update PRs. I could not directly verify the repository-level alerts/security-updates toggles from the available API surface. |
| Code scanning (CodeQL) analyses | ✅ Present | The active PR shows successful `CodeQL` and `Analyze` check runs, so code scanning analyses are present. |
| Branch protection on default branch | ⚠️ Partially verified | The active PR is blocked pending normal merge requirements, which is consistent with branch/ruleset enforcement, but I could not directly query whether required reviews, required status checks, and signed commits are all configured. |

### ⚖️ License Analysis

- **Declared repository license (metadata):** `MIT`
- **`LICENSE` file match:** `Yes` - the checked-in license text materially matches the MIT license declared in repository metadata.
- **Dependency manifests found:** `Directory.Packages.props` and 5 `*.csproj` files.
- **Direct dependency review (manual, preliminary):** `CsvHelper`, `Microsoft.Graph`, `Microsoft.Identity.Web`, `Microsoft.FluentUI.AspNetCore.Components`, `coverlet.collector`, `Microsoft.NET.Test.Sdk`, and `xunit` are all commonly distributed under permissive licenses.
- **Copyleft flags:** No direct GPL, AGPL, LGPL, or other strong-copyleft dependency was identified from the declared package set.
- **Release note:** This is still a manual review. A formal SBOM or license scan in CI would provide stronger evidence before a public release.

### 📊 Risk Assessment

| Category | Level | Details |
|----------|-------|---------|
| **Business Risk** | 🟢 Low | No hardcoded secrets or proprietary/internal-only references were found in the repository content reviewed here. The main remaining issue is that automated secret-scanning coverage is not enabled through GitHub Advanced Security. |
| **Legal Risk** | 🟢 Low | Repository metadata declares MIT, the `LICENSE` file aligns, and no strong-copyleft direct dependencies were identified in the declared package set. |
| **Open Source Risk** | 🟢 Low | The expected governance files are present and meaningful, with a clear maintainer, contribution guide, support guidance, code of conduct, and security policy already established. |

### 📋 Recommendations

**Must Fix (blocking release)**

1. Enable GitHub secret scanning or add an equivalent automated secret-detection control in CI before a broader public release.

**Should Address**

1. Verify the default branch ruleset explicitly in GitHub Settings and confirm whether required reviews, required checks, and signed-commit requirements are all enabled.
2. Verify Dependabot alerts and security-update settings in GitHub Settings so the repository settings match the checked-in `dependabot.yml` automation.
3. Add a brief `Contributing` link or section to the README so the contributor path is visible from the landing page.

**Nice to Have**

1. Add an automated SBOM or license scan in CI to continuously flag unknown or copyleft dependency changes.
2. Clean up the minor casing typos in the MIT `LICENSE` text for presentation polish, even though the license intent is clear.

**Helpful references**

- GitHub Open Source guides: https://opensource.guide/
- About repository security settings: https://docs.github.com/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches
- Secure coding and disclosure process guidance: https://docs.github.com/code-security/getting-started/adding-a-security-policy-to-your-repository
