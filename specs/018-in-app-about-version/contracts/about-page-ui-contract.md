# About Page UI Contract

## Scope

In-app About surface for signed-in users (FR-002–FR-004, FR-008). Satisfies user stories 1 and
4 and SC-001 / SC-004.

## Route and access

| Requirement | Rule |
| --- | --- |
| Route | Dedicated full-page Blazor route (recommended `/about`). MUST NOT be implemented as a modal dialog. |
| Authentication | Unsigned users MUST NOT see About content. They MUST be prompted to sign in (or redirected through sign-in) before content renders. |
| Sensitive data | About MUST NOT display tenant identifiers, tokens, or diagnostic dumps beyond optional public build metadata defined in `release-version-contract.md`. |

## Required content (signed-in)

| Element | Rule |
| --- | --- |
| Product name | Visible page title or heading (UK English). |
| Release version label | MUST match `release-version-contract.md` for the running deployment. |
| Build metadata | SHOULD show UTC build time and source revision when available; omit rows when unavailable. |

## External links

About MUST include links to the same destinations as footer/help for:

| Link | Path pattern |
| --- | --- |
| User documentation home | `{DocsBaseUrl}/` |
| Terms of use | `{DocsBaseUrl}/terms` |
| Privacy and security | `{DocsBaseUrl}/privacy-and-security` |
| Support | `{DocsBaseUrl}/support` |

`DocsBaseUrl` MUST remain configurable with production default per `app-external-links-contract.md`.

## Shell entry points

| Entry | Rule |
| --- | --- |
| Footer | Where `MainLayout` (or equivalent) footer links exist, add an **About** link to the internal route. |
| Help | A **Help** affordance MUST exist in application chrome on authenticated import workflow pages and MUST include a link to About alongside documentation/support paths. |
| Unsigned | About links MUST NOT bypass sign-in (hide, disable, or challenge — implementation choice recorded in tasks). |

## Accessibility

- About MUST be reachable and readable by keyboard and screen readers consistent with MudBlazor
  patterns used elsewhere.
- Page MUST expose a logical heading structure (`h1` for primary title).

## Wording

- UK English (for example “organisation”, “behaviour”).
- Do not imply SLA-backed support on About; point to published support page for issue reporting.
