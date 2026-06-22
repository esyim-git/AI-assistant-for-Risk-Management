param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot

# VERSION is the single source of truth (ADR-006). Resolve/validate before any version-derived path.
$VersionFile = Join-Path $Root "VERSION"
if (!(Test-Path $VersionFile)) { throw "VERSION file not found: $VersionFile" }
$FileVersion = (Get-Content $VersionFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($FileVersion)) { throw "VERSION file is empty: $VersionFile" }
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $FileVersion
} elseif ($Version -cne $FileVersion) {
    throw "Requested version '$Version' does not match VERSION file '$FileVersion'. VERSION is the single source of truth; update VERSION or omit -Version."
}
Write-Host "Version: $Version (source: VERSION file)"

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

# Build metadata for reproducibility (ADR-006). Native-command lookups are best-effort.
$BuildCommit = "unknown"
try { $c = (& git -C $Root rev-parse --short HEAD 2>$null) -join ""; if (-not [string]::IsNullOrWhiteSpace($c)) { $BuildCommit = $c.Trim() } } catch { $BuildCommit = "unknown" }
# Mark a dirty working tree: the recorded commit alone cannot reproduce the ZIP otherwise.
try { $dirty = (& git -C $Root status --porcelain 2>$null) -join "`n"; if ((-not [string]::IsNullOrWhiteSpace($dirty)) -and ($BuildCommit -ne "unknown")) { $BuildCommit = "$BuildCommit-dirty" } } catch { }
$SdkVersion = "unknown"
try { $s = (& dotnet --version 2>$null) -join ""; if (-not [string]::IsNullOrWhiteSpace($s)) { $SdkVersion = $s.Trim() } } catch { $SdkVersion = "unknown" }
$BuildDateUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")

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

## Build

- Version: $Version (VERSION single source)
- Build Commit: $BuildCommit
- .NET SDK: $SdkVersion
- Runtime: win-x64 self-contained (.NET 8)
- Build Date (UTC): $BuildDateUtc
- SmokeTest: not run by this packaging script — verify CI for this commit (authoritative total via STAB-WP-02)

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
