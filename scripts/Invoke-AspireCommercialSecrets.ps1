# Configure AppHost parameters for commercial mode and Stripe via aspire secret.
# Usage: ./scripts/Invoke-AspireCommercialSecrets.ps1 -Command enable-commercial

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('enable-commercial', 'disable-commercial', 'set-stripe-api-key', 'set-stripe-webhook-secret', 'list', 'path')]
    [string] $Command,

    [Parameter(Position = 1)]
    [string] $Value = ''
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$AppHostProject = Join-Path $RepoRoot 'src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj'

function Require-Aspire {
    if (-not (Get-Command aspire -ErrorAction SilentlyContinue)) {
        throw 'Aspire CLI not found. Install from https://aspire.dev/docs/get-started/install-cli'
    }
}

function Set-AspireSecret([string] $Key, [string] $SecretValue) {
    & aspire secret set $Key $SecretValue --apphost $AppHostProject --non-interactive
}

Require-Aspire

switch ($Command) {
    'enable-commercial' {
        Set-AspireSecret 'Parameters:enableCommercialMode' 'true'
        Write-Host 'Commercial mode enabled for AppHost. Restart Aspire so tables and paid-credit-expiry are modelled.'
    }
    'disable-commercial' {
        Set-AspireSecret 'Parameters:enableCommercialMode' 'false'
        Write-Host 'Commercial mode disabled for AppHost. Restart Aspire.'
    }
    'set-stripe-api-key' {
        if (-not $Value) { throw 'Missing API key value.' }
        Set-AspireSecret 'Parameters:stripeApiKey' $Value
        Write-Host 'Set Parameters:stripeApiKey.'
    }
    'set-stripe-webhook-secret' {
        if (-not $Value) { throw 'Missing webhook secret value.' }
        Set-AspireSecret 'Parameters:stripeWebhookSecret' $Value
        Write-Host 'Set Parameters:stripeWebhookSecret.'
    }
    'list' {
        & aspire secret list --apphost $AppHostProject
    }
    'path' {
        & aspire secret path --apphost $AppHostProject
    }
}
