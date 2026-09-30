# PR notes — 018 in-app About and release version

- **T025**: Verified `About.razor.cs` mirrors `MainLayout.razor.cs` (`DocsBaseUrl.TrimEnd('/')`, then `/`, `/terms`, `/privacy-and-security`, `/support`); no `{DocsBaseUrl}/about` doc link.
- **T026**: Keyboard/accessibility checked via `AboutPageTests` (`AboutPage_ExposesProductTitleHeadingAndFocusableDocumentationAnchors` and external-link href coverage) per `contracts/about-page-ui-contract.md`; Help `MudMenu` on `/` uses the same `ImportWorkflowHelpMenu` patterns as documented in research (Documentation, Support, About).
- **T027**: Automated quickstart checks run locally — release/About unit tests, `verify-guide-release-applicability.sh`, `dotnet format --verify-no-changes`. Manual sign-in navigation (§3–§4) and SC-004 triage timing remain for human verification on a deployed environment.
