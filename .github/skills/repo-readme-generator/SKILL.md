---
name: repo-readme-generator
description: 'Intelligent README.md generation prompt that analyses project documentation structure and creates comprehensive repository documentation. Scans repository documentation files and copilot-instructions.md to extract project information, technology stack, architecture, development workflow, coding standards, and testing approaches while generating well-structured markdown documentation with proper formatting, cross-references, and developer-focused content.'
---

# README Generator Prompt

Generate a comprehensive README.md for this repository by analysing repository-native sources of truth (Spec Kit artefacts and governance guidance). Follow these steps.

1. Scan the Spec Kit feature artefacts across all feature folders under `specs/` (for example, `001-*`, `002-*`, `003-*`), using each feature folder's standard files.
   - `spec.md`
   - `plan.md`
   - `tasks.md`
   - `research.md`
   - `data-model.md`
   - `quickstart.md`
   - `contracts/*`

2. Review repository governance and contributor guidance.
   - `.github/copilot-instructions.md`
   - `.specify/memory/constitution.md`
   - `AGENTS.md`
   - `tests/README.md`

3. Create a README.md with the following sections, grounding each section in the sources above (and broader repository files where relevant).

## Project Name and Description
- Extract the project name and primary purpose from the documentation.
- Include a concise description of what the project does.

## Technology Stack
- List the primary technologies, languages, and frameworks used.
- Include version information when available.
- Source this information primarily from `Directory.Packages.props`, `global.json`, `ImportToPlanner.slnx`, and feature `plan.md` files under `specs/`.

## Project Architecture
- Provide a high-level overview of the architecture.
- Consider including a simple diagram if described in the documentation.
- Source from feature `plan.md`, `data-model.md`, and `contracts/*` files under `specs/`, plus `src/` project boundaries.

## Getting Started
- Include installation instructions based on the technology stack.
- Add setup and configuration steps.
- Include any prerequisites.
- Source from `README.md`, feature `quickstart.md` files under `specs/`, and relevant app configuration examples.

## Project Structure
- Brief overview of the folder organisation.
- Source from the repository root layout (`src/`, `tests/`, `docs/`, `docs-internal/`, `specs/`, and AppHost files).

## Key Features
- List main functionality and features of the project.
- Extract from various documentation files.
- Source primarily from feature `spec.md` files under `specs/` and `contracts/*` files.

## Development Workflow
- Summarise the development process.
- Include information about branching strategy if available.
- Source from `CONTRIBUTING.md`, `AGENTS.md`, `.specify/memory/constitution.md`, and feature `tasks.md` files under `specs/`.

## Coding Standards
- Summarise key coding standards and conventions.
- Source from `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` files.

## Testing
- Explain testing approach and tools.
- Source from `tests/README.md`, test project files under `tests/`, and constitutional testing requirements.

## Contributing
- Guidelines for contributing to the project.
- Reference any code exemplars for guidance.
- Source from `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `AGENTS.md`, and `.github/copilot-instructions.md`.

## Licence
- Include license information if available.

Format the README with proper Markdown, including the following.
- Clear headings and subheadings.
- Code blocks where appropriate.
- Lists for better readability.
- Links to other documentation files.
- Badges for build status, version, etc. if information is available.

Keep the README concise yet informative, focusing on what new developers or users would need to know about the project.
