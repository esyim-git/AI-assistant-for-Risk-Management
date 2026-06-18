$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Artifacts = Join-Path $Root "artifacts"

if (Test-Path $Artifacts) {
    Remove-Item $Artifacts -Recurse -Force
    Write-Host "Removed artifacts directory."
} else {
    Write-Host "No artifacts directory found."
}
