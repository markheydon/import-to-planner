# App External Links Contract

## Scope

This contract governs outward links from the Import To Planner web application to the public
product site and v1.0 support path. It satisfies FR-006 and SC-002.

## Required destinations

The hosted application MUST expose links that resolve to:

| User need | Target | Notes |
| --- | --- | --- |
| User documentation home | `{DocsBaseUrl}/` | Default `https://docs.importplanner.app/` |
| Terms of use | `{DocsBaseUrl}/terms` | Same director-approved terms page as public site |
| Privacy and security | `{DocsBaseUrl}/privacy-and-security` | Expanded privacy page |
| Support / feedback | `{DocsBaseUrl}/support` | Page MUST forward users to GitHub Issues |

`DocsBaseUrl` MUST be configurable for self-hosted deployments but MUST default to the
production custom domain.

## Placement

- Links MUST appear in at least one always-visible chrome region (for example application
  footer or persistent help menu) on authenticated import workflow pages, not only on error
  pages.
- A reviewer starting from the app home or main import view MUST reach terms, privacy, and
  support within two clicks (SC-002).

## Wording constraints

- Troubleshooting and FAQ content inside the app MUST NOT instruct users to “contact your
  support team” without linking to the published support page or GitHub Issues path.
- Copy MUST NOT imply SLA-backed or email support in v1.0.

## Consistency

URLs and titles MUST match the `LegalAndSupportBundle` defined in `data-model.md` and the
public pages described in `public-site-contract.md`.

## Security

- Links MUST use `https` in production.
- Links MUST NOT embed secrets, tenant identifiers, or diagnostic tokens.
