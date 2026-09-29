#!/usr/bin/env bash
# Captures demonstration-mode import workflow screenshots into website/static/import-workflow/.
# Requires Docker-free local .NET SDK, built Web project, and Playwright Chromium (see tests/README.md).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

dotnet build tests/ImportToPlanner.E2E.Tests/ImportToPlanner.E2E.Tests.csproj --configuration Debug --verbosity minimal

PLAYWRIGHT_SCRIPT="tests/ImportToPlanner.E2E.Tests/bin/Debug/net10.0/playwright.ps1"
if [[ -f "$PLAYWRIGHT_SCRIPT" ]]; then
  pwsh "$PLAYWRIGHT_SCRIPT" install chromium
fi

export CAPTURE_DEMO_WORKFLOW_SCREENSHOTS=1
export IMPORT_TO_PLANNER_ALLOW_E2E_TESTING=true

dotnet test tests/ImportToPlanner.E2E.Tests/ImportToPlanner.E2E.Tests.csproj \
  --configuration Debug \
  --no-build \
  --filter "FullyQualifiedName~DemoWorkflowScreenshotCaptureTests" \
  --verbosity minimal

echo "Screenshots written to website/static/import-workflow/"
"${ROOT}/scripts/verify-import-workflow-screenshots.sh" --require
