#!/usr/bin/env bash
# Rejects byte-identical import workflow screenshots.
# With no PNG files, exits 0 so capture can be deferred (see GitHub issue 164).
# Pass --require to fail when the five step images are missing.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIR="${ROOT}/website/static/import-workflow"
REQUIRE=0

if [[ "${1:-}" == "--require" ]]; then
  REQUIRE=1
fi

EXPECTED=(
  step-1-select-location.png
  step-2-select-plan.png
  step-3-upload-csv.png
  step-4-preview-and-confirm.png
  step-5-execution-report.png
)

if [[ ! -d "$DIR" ]]; then
  if [[ "$REQUIRE" -eq 1 ]]; then
    echo "Missing screenshot directory: ${DIR}" >&2
    exit 1
  fi

  echo "No workflow screenshots yet; distinctness check skipped."
  exit 0
fi

shopt -s nullglob
files=("${DIR}"/*.png)
if (( ${#files[@]} == 0 )); then
  if [[ "$REQUIRE" -eq 1 ]]; then
    echo "No workflow screenshots in ${DIR}" >&2
    exit 1
  fi

  echo "No workflow screenshots yet; distinctness check skipped."
  exit 0
fi

if [[ "$REQUIRE" -eq 1 ]]; then
  for name in "${EXPECTED[@]}"; do
    if [[ ! -f "${DIR}/${name}" ]]; then
      echo "Missing workflow screenshot: ${name}" >&2
      exit 1
    fi
  done
fi

declare -A seen=()
for file in "${files[@]}"; do
  hash="$(sha256sum "$file" | awk '{print $1}')"
  if [[ -n "${seen[$hash]:-}" ]]; then
    echo "Workflow screenshots are identical: ${seen[$hash]} and ${file}" >&2
    exit 1
  fi

  seen["$hash"]="$(basename "$file")"
done

echo "Workflow screenshots are distinct (${#files[@]} file(s))."
