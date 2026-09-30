# App External Links Contract Amendment (018)

## Scope

Extends `specs/017-public-hugo-website/contracts/app-external-links-contract.md` without
replacing it. Adds internal About navigation requirements from FR-002.

## Additional placement rules

- In addition to external documentation, terms, privacy, and support links, the application
  chrome MUST expose an internal **About** route link from:
  - The persistent footer link row (when present).
  - The **Help** menu (or equivalent help entry) on import workflow pages.
- About is an in-app route, not an external URL; it MUST NOT be modelled as `{DocsBaseUrl}/about`.

## Consistency

- External URLs on About MUST remain identical to footer external links for the same
  `DocsBaseUrl`.
- Two-click reachability for terms, privacy, and support (SC-002 from spec 017) MUST remain
  satisfied after adding About links.
