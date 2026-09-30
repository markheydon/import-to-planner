#!/usr/bin/env bash
# Builds the Hugo site (unless --skip-build) and checks that the landing page and
# import-workflow guide expose the same releaseVersion as the site footer.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLIC="${ROOT}/website/public"
SKIP_BUILD=0

usage() {
  cat <<'EOF'
Usage: verify-guide-release-applicability.sh [--skip-build]

  --skip-build  Assert against website/public/ without invoking invoke-hugo-site.sh
                (for CI after a native Hugo build).
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-build)
      SKIP_BUILD=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown argument '$1'" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  "$ROOT/scripts/invoke-hugo-site.sh" build
fi

extract_guide_release_version() {
  local file="$1"
  local -a tags=()
  local tag version

  while IFS= read -r tag; do
    [[ -n "$tag" ]] && tags+=("$tag")
  done < <(grep -oE 'guide-release-applicability[^>]+' "$file" || true)

  if (( ${#tags[@]} != 1 )); then
    echo "error: expected exactly one guide-release-applicability callout in ${file}, found ${#tags[@]}" >&2
    return 1
  fi

  tag="${tags[0]}"
  if [[ "$tag" == *'data-release-version="'* ]]; then
    version="$(printf '%s' "$tag" | sed -E 's/.*data-release-version="([^"]+)".*/\1/')"
  else
    version="$(printf '%s' "$tag" | sed -E 's/.*data-release-version=([^ >]+).*/\1/')"
  fi

  if [[ "$version" == "$tag" || -z "$version" ]]; then
    echo "error: could not read data-release-version from guide callout in ${file}" >&2
    return 1
  fi

  printf '%s' "$version"
}

extract_footer_release_version() {
  local file="$1"
  if grep -q 'Documentation build: unreleased' "$file"; then
    echo "unreleased"
    return 0
  fi

  local version
  version="$(grep -oE 'Documentation for release [^<]+' "$file" | head -n 1 | sed 's/Documentation for release //')"
  if [[ -z "$version" ]]; then
    echo "error: could not read footer release label from ${file}" >&2
    return 1
  fi

  printf '%s\n' "$version"
}

page_release_version() {
  local file="$1"
  local guide footer

  if [[ ! -f "$file" ]]; then
    echo "error: missing built page ${file}" >&2
    return 1
  fi

  guide="$(extract_guide_release_version "$file")"
  footer="$(extract_footer_release_version "$file")"

  if [[ "$guide" != "$footer" ]]; then
    echo "error: guide callout (${guide}) does not match footer (${footer}) in ${file}" >&2
    return 1
  fi

  printf '%s' "$guide"
}

LANDING="${PUBLIC}/index.html"
WORKFLOW="${PUBLIC}/import-workflow/index.html"

landing_version="$(page_release_version "$LANDING")"
workflow_version="$(page_release_version "$WORKFLOW")"

echo "landing: releaseVersion=${landing_version} (guide and footer match)"
echo "import-workflow: releaseVersion=${workflow_version} (guide and footer match)"

if [[ "$landing_version" != "$workflow_version" ]]; then
  echo "error: landing (${landing_version}) and import-workflow (${workflow_version}) differ" >&2
  exit 1
fi

echo "Guide release applicability is consistent across landing, import-workflow, and footers."
