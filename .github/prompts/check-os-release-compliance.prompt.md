---
agent: 'agent'
description: Checks a GitHub repository for compliance with Open Source Release Requirements: file presence, security settings, license analysis, and risk assessment. A compliance report is then appended to the repository README.
tools: [read, edit, search, 'github/*']
---

## File Compliance Check

For the current repository, check whether each of the following files exists at
the repository root (or in `.github/` where conventional). For each file that
exists, also assess whether it has meaningful content.

| File | What to look for |
|------|-----------------|
| `LICENSE` | Must be present. Contents must match the license declared in the repo metadata. |
| `README.md` | Must be present and substantial (>100 lines recommended). Should contain sections for usage, install, and contributing. |
| `CODEOWNERS` | Must list at least one maintainer or team. |
| `CONTRIBUTING.md` | Must describe how to contribute (issues, PRs, CLA/DCO, code style). |
| `SUPPORT.md` | Must explain how users can get help. |
| `CODE_OF_CONDUCT.md` | Must adopt a recognized code of conduct. |
| `SECURITY.md` | Must describe the security vulnerability disclosure process. |

## Security Configuration Check

Using the GitHub API, check the following security settings on the current repository:

- **Secret scanning** — Is secret scanning enabled?
- **Dependabot** — Are Dependabot alerts and/or security updates enabled?
- **Code scanning (CodeQL)** — Are any code scanning analyses present?
- **Branch protection** — Is the default branch protected? Are required reviews,
  status checks, or signed commits configured?

Handle `404` or `403` responses gracefully — they typically mean the feature is
not enabled or you lack permission to check it.

## License & Legal Analysis

- Compare the contents of the `LICENSE` file against the license declared in
  the repository metadata (`license.spdx_id` from the repo API response).
  Flag any mismatch.
- Look for dependency manifests (`package.json`, `requirements.txt`, `go.mod`,
  `Cargo.toml`, `pom.xml`, `Gemfile`, `*.csproj`, etc.) in the repository.
- For each manifest found, attempt to identify declared dependency licenses.
  Specifically flag any **GPL**, **AGPL**, **LGPL**, or other strong-copyleft
  licenses that would require legal review before an open source release.

## Risk Assessment

Based on your findings, assign a risk level (**Low**, **Medium**, or **High**)
to each of the following categories:

| Category | Low 🟢 | Medium 🟡 | High 🔴 |
|----------|--------|-----------|---------|
| **Business Risk** | No secrets, no proprietary code patterns | Some internal references found | Secrets detected, proprietary code |
| **Legal Risk** | Permissive license, no copyleft deps | Minor license inconsistencies | GPL/AGPL deps, license mismatch |
| **Open Source Risk** | All files present, active maintainers | Some files missing or thin | No README, no CODEOWNERS |

## Generate Compliance Report

Update the repo README with a new section (if one doesn't already exist) regarding the current Open Source Release Requirements compliance.

Append the compliance report as a new top-level ## Open Source Compliance Report section at the end of README.md. If README.md does not exist, create it. Do not modify any other content in the file.

1. **Header** — timestamp, overall status (PASS ✅ / NEEDS WORK ⚠️ / BLOCKED 🚫)
2. **📄 File Compliance** — table of 7 files with ✅/❌ status and notes
3. **🔒 Security Configuration** — table of 4 settings with status
4. **⚖️ License Analysis** — declared license, LICENSE file match, copyleft flags
5. **📊 Risk Assessment** — Business/Legal/Open Source risk levels (🟢/🟡/🔴) with details
6. **📋 Recommendations** — prioritized as Must Fix (blocking), Should Address, Nice to Have

### Tone Guidelines

- Be **constructive** — help teams succeed, don't gatekeep.
- Explain *why* missing items matter and link to guidance.
- Celebrate what the team has already done well.