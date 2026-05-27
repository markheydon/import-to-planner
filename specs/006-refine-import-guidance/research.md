# Research: Refine Import Guidance

## Decision 1: Keep the existing five-card workflow and improve state visibility inside it

- Decision: Retain the current card-based five-step layout and distinguish step
  states through styling and completion cues inside each existing card rather
  than introducing a separate stepper or navigation bar.
- Rationale: The spec calls for a minor UX refinement, not a structural
  redesign. The current layout already matches the workflow sequence, so clearer
  state treatment is the lowest-risk way to improve progression visibility while
  staying ready for future navigation.
- Alternatives considered: Add a dedicated stepper or top navigation strip.
  Rejected because it widens scope, duplicates the existing sequence indicators,
  and creates new layout/accessibility work that the feature does not require.

## Decision 2: Standardise on concise action-oriented step titles with explicit Planner context

- Decision: Use the clarified title pattern: `Select Planner location`, `Select
  plan`, `Upload CSV`, `Preview import`, and `Confirm and import`.
- Rationale: These titles are easier to scan than internal-facing labels such as
  `Select Container`, remain clear when read out of context, and are suitable for
  later reuse in any future navigation affordance.
- Alternatives considered: Use question-style headings such as `Where should the
  tasks go?`, or keep the current titles and only remove `Step X ·`. Rejected
  because question-style headings are less reusable in compact UI elements and
  the current terminology preserves internal system language the spec wants to
  soften.

## Decision 3: Keep the introduction compact but make CSV expectations explicit

- Decision: Expand the page introduction with a brief structured summary that
  names the required field (`Task Name`), the accepted fields (`Task Name`,
  `Description`, `Priority`, `Bucket`, `Goal`), and a short expectation note
  about manual follow-up.
- Rationale: The parser already enforces this schema, and the spec wants users to
  understand it before upload without turning the page into full documentation.
- Alternatives considered: Leave schema guidance out of the page and rely solely
  on validation errors, or add a longer tutorial-style introduction. Rejected
  because the first option keeps the current expectation gap and the second would
  crowd the main workflow.

## Decision 4: Mention one concrete manual-follow-up example only

- Decision: Explain that some details may still need manual completion in
  Planner after import, using goals as the single concrete example.
- Rationale: The app already surfaces manual actions, and goals are already part
  of the supported CSV vocabulary. One example is enough to set expectations
  without implying a complete limitations catalogue.
- Alternatives considered: Keep the wording generic with no example, or list
  every current automation limitation. Rejected because the former is less
  helpful to users and the latter is too verbose for the page-level guidance the
  spec calls for.

## Decision 5: Align only the light-theme border token

- Decision: Update the light-theme `LinesDefault` and matching divider token to
  `#d1d1d1`, keeping the rest of the palette unchanged.
- Rationale: The reported mismatch is isolated to border warmth. Restricting the
  change to the shared neutral line token solves the design issue without
  creating a broader visual regression surface.
- Alternatives considered: Adjust multiple neutrals or revisit both light and
  dark palettes together. Rejected because the request identifies one small token
  mismatch rather than a broader theming problem.

## Decision 6: Verify through existing Home page and architecture tests

- Decision: Use focused updates to `HomePageSmokeTests`, `HomePageWorkflowTests`,
  and `ArchitectureComplianceTests`, plus a manual responsive review.
- Rationale: Those files already cover the home page structure, workflow
  semantics, and markup guardrails. Reusing them keeps validation local to the
  changed slice.
- Alternatives considered: Add a new end-to-end harness or rely only on manual
  QA. Rejected because the existing test surfaces are already close to the
  change, and the repository expects smallest-practical automated verification
  first.

## Repository Policy Checkpoints

- All new user-facing wording must remain in UK English and stay on the Web side
  of the architecture boundary.
- Preview, confirmation, and execution semantics must remain unchanged even when
  labels and headings are updated.
- Responsive behaviour must remain legible on smaller screens where cards stack
  vertically.
- The implementation should not introduce any new remote calls or runtime flags;
  this is a presentation-only refinement.
