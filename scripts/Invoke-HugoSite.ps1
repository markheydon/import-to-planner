# Build or preview the Hugo public product site via Docker/Podman.
# Usage: ./scripts/Invoke-HugoSite.ps1 [build|serve|preview] [-Runtime docker|podman] [-ServePort N] [-PreviewPort N]

param(
    [ValidateSet("build", "serve", "preview")]
    [string]$Command = "build",
    [string]$Runtime = "",
    [int]$ServePort = 1313,
    [int]$PreviewPort = 8080
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$SiteDir = Join-Path $RepoRoot "website"
$PublicPath = Join-Path $SiteDir "public"
$HugoImage = "docker.io/hugomods/hugo:latest"
$NginxImage = "docker.io/library/nginx:alpine"

function Resolve-ContainerRuntime {
    if ($Runtime) {
        if (-not (Get-Command $Runtime -ErrorAction SilentlyContinue)) {
            throw "Container runtime '$Runtime' not found on PATH."
        }
        return $Runtime
    }
    if (Get-Command docker -ErrorAction SilentlyContinue) { return "docker" }
    if (Get-Command podman -ErrorAction SilentlyContinue) { return "podman" }
    throw "No container runtime found. Install Docker or Podman, or pass -Runtime."
}

function Invoke-HugoBuild([string]$BaseUrl = "") {
    $hugoArgs = @("--minify")
    if ($BaseUrl) { $hugoArgs += @("--baseURL", $BaseUrl) }
    & $resolvedRuntime run --rm -v "${RepoRoot}:/src" -w /src/website $HugoImage hugo @hugoArgs
    if (-not (Test-Path (Join-Path $PublicPath "index.html"))) {
        throw "Hugo did not produce website/public/index.html"
    }
}

if (-not (Test-Path $SiteDir)) {
    throw "Hugo site directory not found at $SiteDir"
}

$resolvedRuntime = Resolve-ContainerRuntime

switch ($Command) {
    "build" {
        Write-Host "Building Hugo site to website/public..."
        Invoke-HugoBuild
        Write-Host "Done. Output: $PublicPath"
    }
    "serve" {
        Write-Host "Starting Hugo dev server at http://localhost:${ServePort} ..."
        & $resolvedRuntime run --rm -p "${ServePort}:1313" -v "${RepoRoot}:/src" -w /src/website $HugoImage `
            hugo server --bind 0.0.0.0 --baseURL "http://localhost:${ServePort}/"
    }
    "preview" {
        Write-Host "Building Hugo site..."
        Invoke-HugoBuild "http://localhost:${PreviewPort}/"
        Write-Host "Serving website/public at http://localhost:${PreviewPort} ..."
        & $resolvedRuntime run --rm -p "${PreviewPort}:80" -v "${PublicPath}:/usr/share/nginx/html:ro" $NginxImage
    }
}
