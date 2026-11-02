# Run Stripe's stripe-mock in Docker/Podman for opt-in contract tests.
# Usage: ./scripts/Invoke-StripeMock.ps1 [-Command start|stop|status|contract-test] [-Runtime docker|podman] [-Port 12111]

[CmdletBinding()]
param(
    [ValidateSet('start', 'stop', 'status', 'contract-test')]
    [string] $Command = 'start',

    [string] $Runtime = '',

    [int] $Port = 12111
)

$ErrorActionPreference = 'Stop'

$ContainerName = 'import-to-planner-stripe-mock'
$MockImage = 'docker.io/stripe/stripe-mock:latest'
$RepoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-ContainerRuntime {
    if ($Runtime) {
        if (-not (Get-Command $Runtime -ErrorAction SilentlyContinue)) {
            throw "Container runtime '$Runtime' not found on PATH."
        }
        return $Runtime
    }

    if (Get-Command docker -ErrorAction SilentlyContinue) { return 'docker' }
    if (Get-Command podman -ErrorAction SilentlyContinue) { return 'podman' }
    throw 'No container runtime found. Install Docker or Podman, or pass -Runtime.'
}

function Test-ContainerRunning([string] $rt) {
    $names = & $rt ps --filter "name=^$ContainerName$" --format '{{.Names}}' 2>$null
    return ($names -contains $ContainerName)
}

function Start-StripeMock([string] $rt) {
    if (Test-ContainerRunning $rt) {
        Write-Host "stripe-mock already running as container '$ContainerName' on port $Port."
        return
    }

    $existing = & $rt ps -a --filter "name=^$ContainerName$" --format '{{.Names}}' 2>$null
    if ($existing -contains $ContainerName) {
        & $rt rm -f $ContainerName | Out-Null
    }

    Write-Host "Starting stripe-mock on http://localhost:$Port ..."
    & $rt run -d --name $ContainerName -p "${Port}:12111" $MockImage | Out-Null
    Write-Host "Container '$ContainerName' is running."
}

function Stop-StripeMock([string] $rt) {
    $existing = & $rt ps -a --filter "name=^$ContainerName$" --format '{{.Names}}' 2>$null
    if ($existing -contains $ContainerName) {
        & $rt rm -f $ContainerName | Out-Null
        Write-Host "Stopped stripe-mock container '$ContainerName'."
    } else {
        Write-Host "No stripe-mock container named '$ContainerName'."
    }
}

function Invoke-ContractTest([string] $rt) {
    Start-StripeMock $rt
    if (-not $env:STRIPE_MOCK_BASE_URL) {
        $env:STRIPE_MOCK_BASE_URL = "http://localhost:$Port"
    }
    Write-Host "Running contract tests with STRIPE_MOCK_BASE_URL=$($env:STRIPE_MOCK_BASE_URL)"
    dotnet test (Join-Path $RepoRoot 'ImportToPlanner.slnx') --filter 'FullyQualifiedName~StripeMock'
}

$rt = Resolve-ContainerRuntime

switch ($Command) {
    'start' { Start-StripeMock $rt }
    'stop' { Stop-StripeMock $rt }
    'status' {
        if (Test-ContainerRunning $rt) {
            Write-Host "stripe-mock is running (container '$ContainerName', port $Port)."
        } else {
            Write-Host 'stripe-mock is not running.'
            exit 1
        }
    }
    'contract-test' { Invoke-ContractTest $rt }
}
