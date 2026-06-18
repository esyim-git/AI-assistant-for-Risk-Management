param(
    [string]$Version = "0.2.0"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $Root "artifacts\publish\RiskManagementAI-v$Version-win-x64"
$ReleaseDir = Join-Path $Root "artifacts\release"
$ZipPath = Join-Path $ReleaseDir "RiskManagementAI-v$Version-win-x64-portable.zip"
$HashPath = "$ZipPath.sha256"
$ReleaseNote = Join-Path $ReleaseDir "ReleaseNote-v$Version.md"
$DependencyList = Join-Path $ReleaseDir "DependencyList-v$Version.csv"

if (!(Test-Path $PublishDir)) {
    throw "Publish directory not found: $PublishDir"
}

New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

$Hash = Get-FileHash -Path $ZipPath -Algorithm SHA256
"$($Hash.Hash)  $(Split-Path $ZipPath -Leaf)" | Set-Content -Path $HashPath -Encoding ASCII

@"
# Release Note v$Version

## Target

- Windows 11 x64
- Excel 2021
- Offline execution
- Self-contained .NET portable ZIP

## Included

- RiskManagementAI executable
- Runtime dependencies
- Rules
- Templates
- Sample dummy data
- Offline run guide

## Excluded

- Real company data
- Internal regulation originals
- Credentials/passwords/tokens
- Large model files
- Golden6 automatic connection
- External API calls
- Telemetry

## SHA256

$($Hash.Hash)
"@ | Set-Content -Path $ReleaseNote -Encoding UTF8

@"
Component,Version,Source,Note
RiskManagementAI,$Version,Local Build,Application
.NET Runtime,self-contained,Microsoft,Included by publish
External NuGet,None,N/A,Starter avoids external package dependency
Local LLM Model,Not included,N/A,Use separate approved Model Pack
"@ | Set-Content -Path $DependencyList -Encoding UTF8

Write-Host "Release package created: $ZipPath"
Write-Host "SHA256: $($Hash.Hash)"
