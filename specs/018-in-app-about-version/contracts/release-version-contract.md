# Release Version Contract

## Scope

Single authoritative SemVer discipline shared by the Blazor application, GitHub Release tags, and
the public Hugo site (FR-001, FR-005, FR-006, FR-007, FR-009, FR-010).

## Tag naming

- Production releases MUST use annotated git tags matching `vMAJOR.MINOR.PATCH` (for example
  `v1.0.0`).
- GitHub Release title/name MUST match the tag string used for deploy verification.

## Application builds

| Build context | Expected user-facing label |
| --- | --- |
| Built from annotated `vX.Y.Z` tag | Label MUST match tag (including `v` prefix if present on tag). |
| Local dev, PR, main-branch CI, staging | Full SemVer with pre-release identifiers from git/build metadata; MUST NOT show unqualified shipping tag (for example bare `v1.0.0`) unless `ReleaseLabelPolicy.AllowUnqualifiedShippingLabel` is explicitly enabled for that environment in documented configuration. |
| Self-hosted build without tag | Honest pre-release/local identifier; MUST NOT invent a shipping version. |

## Build-time source of truth

- Version strings MUST be computed at build time (MSBuild/MinVer or equivalent) and embedded in
  assembly informational version / custom attributes.
- Manual duplication of version numbers in unrelated source files MUST NOT be required for normal
  releases.

## Optional build metadata

When provided at build time:

| Field | Display on About |
| --- | --- |
| UTC build timestamp | Show in ISO-8601 or documented UK-friendly format with “UTC” indicated. |
| Source revision | Show short SHA; optional link when repository URL template is configured. |

When not provided, About omits the corresponding row.

## Public Hugo site alignment

| Surface | Tagged deploy | Non-tag build |
| --- | --- | --- |
| Footer (`releaseVersion`) | `Documentation for release {tag}` | Unreleased wording (existing 017 partial) |
| Guide applicability (FR-007) | Above-fold callout on landing and import workflow (minimum) showing the same tag | Unreleased / non-production wording |

Tagged deploy MUST set `HUGO_PARAMS_RELEASEVERSION` to `github.ref_name` (existing workflow).

## Three-way verification (tagged production)

For each tagged production release after this feature ships:

```text
GitHub Release tag name = public site release label = in-app About release label
```

Recorded in `docs/release-runbook.md` checklist (Pass/Fail before release complete).

## Automated tests

Non-trivial normalisation or formatting logic MUST have unit tests covering:

- Official tag input
- Pre-release / non-tag git height
- Missing tag / shallow clone fallback behaviour
- Policy flag allowing unqualified label (if implemented)
