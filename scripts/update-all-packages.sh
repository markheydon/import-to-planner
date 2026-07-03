#!/usr/bin/env bash
set -euo pipefail

# Updates NuGet packages for all projects in a solution by calling
# `dotnet package update --project` for each project path.
#
# Usage:
#   ./scripts/update-all-packages.sh [solution-file]
#
# Example:
#   ./scripts/update-all-packages.sh ImportToPlanner.slnx

SOLUTION_FILE="${1:-ImportToPlanner.slnx}"

if [[ ! -f "$SOLUTION_FILE" ]]; then
  echo "Error: solution file not found: $SOLUTION_FILE" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: dotnet CLI not found in PATH" >&2
  exit 1
fi

echo "Updating outdated packages in solution: $SOLUTION_FILE"

overall_status=0

while IFS= read -r project; do
  [[ -z "$project" ]] && continue

  echo
  echo "==> $project"
  if dotnet package update --project "$project"; then
    continue
  else
    exit_code=$?
  fi

  # dotnet package update exit code notes:
  # 0 = packages updated successfully
  # 2 = nothing to update (treated as success)
  if [[ "$exit_code" -eq 2 ]]; then
    continue
  fi

  overall_status=1
  echo "Error: update failed for $project (exit code: $exit_code)" >&2

done < <(dotnet sln "$SOLUTION_FILE" list | tail -n +3)

if [[ "$overall_status" -eq 0 ]]; then
  echo
  echo "Done: package update sweep completed."
else
  echo
  echo "Completed with one or more project update failures." >&2
fi

exit "$overall_status"
