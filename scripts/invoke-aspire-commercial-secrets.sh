#!/usr/bin/env bash
# Configure AppHost parameters for commercial mode and Stripe via the Aspire CLI secret store.
# Usage: ./scripts/invoke-aspire-commercial-secrets.sh <command> [args]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_HOST_PROJECT="$REPO_ROOT/src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj"

usage() {
    cat <<'EOF'
Usage: invoke-aspire-commercial-secrets.sh <command> [arguments]

Commands:
  enable-commercial              Set Parameters:enableCommercialMode to true
  disable-commercial             Set Parameters:enableCommercialMode to false
  set-stripe-api-key <value>     Set Parameters:stripeApiKey (restricted Sandbox key)
  set-stripe-webhook-secret <v>  Set Parameters:stripeWebhookSecret (from stripe listen or Dashboard)
  list                           List AppHost secrets (keys only; values are not printed by default)
  path                           Show the AppHost user-secrets file path

Secrets are stored in the AppHost user-secrets file (same backing store as `aspire secret`).
When commercial mode is enabled at AppHost startup, stripeApiKey and stripeWebhookSecret
are forwarded to the web resource as Stripe__* environment variables.

You still need Azure AD and certificate parameters for Graph sign-in; set those with
`aspire secret set Parameters:azureAdTenantId ...` (and related keys) or use Aspire prompts on first run.

Credit pack Price ids remain in appsettings.Development.json unless you override them via configuration.

Examples:
  ./scripts/invoke-aspire-commercial-secrets.sh enable-commercial
  ./scripts/invoke-aspire-commercial-secrets.sh set-stripe-api-key rk_test_...
  ./scripts/invoke-aspire-commercial-secrets.sh set-stripe-webhook-secret whsec_...
  aspire run
EOF
}

require_aspire() {
    if ! command -v aspire >/dev/null 2>&1; then
        echo "error: Aspire CLI not found. Install from https://aspire.dev/docs/get-started/install-cli" >&2
        exit 1
    fi
}

aspire_secret_set() {
    local key="$1"
    local value="$2"
    aspire secret set "$key" "$value" --apphost "$APP_HOST_PROJECT" --non-interactive
}

if [[ $# -lt 1 ]]; then
    usage >&2
    exit 1
fi

require_aspire

COMMAND="$1"
shift

case "$COMMAND" in
    enable-commercial)
        aspire_secret_set "Parameters:enableCommercialMode" "true"
        echo "Commercial mode enabled for AppHost. Restart Aspire so tables and paid-credit-expiry are modelled."
        ;;
    disable-commercial)
        aspire_secret_set "Parameters:enableCommercialMode" "false"
        echo "Commercial mode disabled for AppHost. Restart Aspire."
        ;;
    set-stripe-api-key)
        if [[ $# -lt 1 ]]; then
            echo "error: missing API key value" >&2
            exit 1
        fi
        aspire_secret_set "Parameters:stripeApiKey" "$1"
        echo "Set Parameters:stripeApiKey. Requires commercial mode on and an Aspire restart if already running."
        ;;
    set-stripe-webhook-secret)
        if [[ $# -lt 1 ]]; then
            echo "error: missing webhook secret value" >&2
            exit 1
        fi
        aspire_secret_set "Parameters:stripeWebhookSecret" "$1"
        echo "Set Parameters:stripeWebhookSecret. Restart or rebuild web if the app is already running."
        ;;
    list)
        aspire secret list --apphost "$APP_HOST_PROJECT"
        ;;
    path)
        aspire secret path --apphost "$APP_HOST_PROJECT"
        ;;
    -h|--help|help)
        usage
        ;;
    *)
        echo "error: unknown command '$COMMAND'" >&2
        usage >&2
        exit 1
        ;;
esac
