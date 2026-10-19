#!/usr/bin/env bash
# Run Stripe's stripe-mock HTTP server in Docker/Podman for opt-in contract tests.
# Usage: ./scripts/invoke-stripe-mock.sh [start|stop|status|contract-test] [--runtime docker|podman] [--port N]

set -euo pipefail

COMMAND="start"
RUNTIME=""
MOCK_PORT=12111
CONTAINER_NAME="import-to-planner-stripe-mock"
MOCK_IMAGE="docker.io/stripe/stripe-mock:latest"

usage() {
    cat <<'EOF'
Usage: invoke-stripe-mock.sh [start|stop|status|contract-test] [options]

Commands:
  start          Start stripe-mock in the background (default)
  stop           Stop and remove the stripe-mock container
  status         Show whether the container is running
  contract-test  Ensure stripe-mock is running, then run StripeMockCheckoutContractTests

Options:
  --runtime RUNTIME   Container runtime: docker or podman (auto-detected if omitted)
  --port PORT         Host port for stripe-mock (default: 12111)
  -h, --help          Show this help

Environment:
  STRIPE_MOCK_BASE_URL  Used by contract-test (default: http://localhost:<port>)

Examples:
  ./scripts/invoke-stripe-mock.sh start
  ./scripts/invoke-stripe-mock.sh contract-test
  ./scripts/invoke-stripe-mock.sh stop
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        start|stop|status|contract-test)
            COMMAND="$1"
            shift
            ;;
        --runtime)
            RUNTIME="${2:-}"
            shift 2
            ;;
        --port)
            MOCK_PORT="${2:-}"
            shift 2
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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

resolve_runtime() {
    if [[ -n "$RUNTIME" ]]; then
        if ! command -v "$RUNTIME" >/dev/null 2>&1; then
            echo "error: container runtime '$RUNTIME' not found on PATH" >&2
            exit 1
        fi
        return
    fi

    if command -v docker >/dev/null 2>&1; then
        RUNTIME="docker"
    elif command -v podman >/dev/null 2>&1; then
        RUNTIME="podman"
    else
        echo "error: no container runtime found. Install Docker or Podman, or pass --runtime." >&2
        exit 1
    fi
}

container_running() {
    "$RUNTIME" ps --filter "name=^${CONTAINER_NAME}$" --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"
}

start_mock() {
    if container_running; then
        echo "stripe-mock already running as container '$CONTAINER_NAME' on port ${MOCK_PORT}."
        return 0
    fi

    if "$RUNTIME" ps -a --filter "name=^${CONTAINER_NAME}$" --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
        "$RUNTIME" rm -f "$CONTAINER_NAME" >/dev/null
    fi

    echo "Starting stripe-mock on http://localhost:${MOCK_PORT} ..."
    "$RUNTIME" run -d --name "$CONTAINER_NAME" -p "${MOCK_PORT}:12111" "$MOCK_IMAGE" >/dev/null
    echo "Container '$CONTAINER_NAME' is running."
}

stop_mock() {
    if container_running || "$RUNTIME" ps -a --filter "name=^${CONTAINER_NAME}$" --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
        "$RUNTIME" rm -f "$CONTAINER_NAME" >/dev/null
        echo "Stopped stripe-mock container '$CONTAINER_NAME'."
    else
        echo "No stripe-mock container named '$CONTAINER_NAME'."
    fi
}

status_mock() {
    if container_running; then
        echo "stripe-mock is running (container '$CONTAINER_NAME', port ${MOCK_PORT})."
        exit 0
    fi

    echo "stripe-mock is not running."
    exit 1
}

run_contract_test() {
    start_mock
    export STRIPE_MOCK_BASE_URL="${STRIPE_MOCK_BASE_URL:-http://localhost:${MOCK_PORT}}"
    echo "Running contract tests with STRIPE_MOCK_BASE_URL=${STRIPE_MOCK_BASE_URL}"
    dotnet test "$REPO_ROOT/ImportToPlanner.slnx" --filter "FullyQualifiedName~StripeMock"
}

resolve_runtime

case "$COMMAND" in
    start)
        start_mock
        ;;
    stop)
        stop_mock
        ;;
    status)
        status_mock
        ;;
    contract-test)
        run_contract_test
        ;;
esac
