param(
    [string]$Version = "",
    [string]$Configuration = "Release"
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

$Project = Join-Path $Root "src\RiskManagementAI.App\RiskManagementAI.App.csproj"
$PublishDir = Join-Path $Root "artifacts\publish\RiskManagementAI-v$Version-win-x64"

if (!(Test-Path $Project)) {
    throw "Project file not found: $Project"
}

Write-Host "Cleaning publish directory..."
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

Write-Host "Publishing RiskManagementAI self-contained win-x64..."
dotnet publish $Project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:EnableUnsafeBinaryFormatterSerialization=false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Copying offline assets..."

# Required offline assets - fail the build if any is missing (Req 7).
$RequiredAssetFolders = @("config", "rules", "kb", "templates", "samples", "deploy")
foreach ($folder in $RequiredAssetFolders) {
    $src = Join-Path $Root $folder
    $dst = Join-Path $PublishDir $folder
    if (!(Test-Path $src)) {
        throw "Required offline asset folder missing: '$folder'. Cannot produce a complete offline package."
    }
    Copy-Item $src $dst -Recurse -Force
}

# Optional offline assets - SQL/VBA templates for offline reference (copied if present).
$OptionalAssetFolders = @("sql", "vba")
foreach ($folder in $OptionalAssetFolders) {
    $src = Join-Path $Root $folder
    $dst = Join-Path $PublishDir $folder
    if (Test-Path $src) {
        Copy-Item $src $dst -Recurse -Force
    } else {
        Write-Warning "Optional asset folder not found, skipping: '$folder'"
    }
}

# Release packages must not include development/test/debug configuration.
Get-ChildItem -LiteralPath $PublishDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    ($_.Name -like "*.Development.json") -or ($_.Name -like "*.Test.json") -or ($_.Name -like "*.Debug.json")
} | Remove-Item -Force

New-Item -ItemType Directory -Path (Join-Path $PublishDir "logs") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $PublishDir "reports") -Force | Out-Null
"" | Set-Content -Path (Join-Path $PublishDir "logs\.keep") -Encoding ASCII
"" | Set-Content -Path (Join-Path $PublishDir "reports\.keep") -Encoding ASCII

@"
@echo off
cd /d %~dp0
start RiskManagementAI.exe
"@ | Set-Content -Path (Join-Path $PublishDir "run.bat") -Encoding ASCII

# Safety guard (Req 10/11): no model weights / secrets / real data may leak into the publish output.
$ForbiddenExt = @(".gguf", ".safetensors", ".onnx", ".pt", ".pem", ".key", ".pfx", ".env")
$ForbiddenPathNames = @("real_data", "secrets", "credentials", "exports")
$Forbidden = Get-ChildItem -Path $PublishDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $pathParts = $_.FullName -split "[\\/]+"
    ($ForbiddenExt -contains $_.Extension) -or
    ($pathParts | Where-Object { ($ForbiddenPathNames -contains $_) -or ($_ -like "internal_*") })
}
if ($Forbidden) {
    $Forbidden | ForEach-Object { Write-Host "FORBIDDEN IN PUBLISH: $($_.FullName)" }
    throw "Publish output contains forbidden files (model/secret/real-data). Aborting before packaging."
}

# --- Integrity manifest (STAB-WP-03a, ADR-008) ---
# Integrity-critical core files. Runtime tamper detection (fail-closed) is STAB-WP-03b.
# The CP949 UHC map is embedded in RiskManagementAI.Core.dll (not a loose file) -> covered by the Core DLL hash.
# WinPS 5.1-safe: no Path.GetRelativePath (compute via Substring), no BOM-dependent literals.
Write-Host "Generating integrity manifest..."
$ManifestPath = Join-Path $PublishDir "approved_manifest.json"
if (Test-Path $ManifestPath) { Remove-Item $ManifestPath -Force }
$publishFull = [System.IO.Path]::GetFullPath($PublishDir)
$manifestEntries = New-Object System.Collections.Generic.List[object]

# Explicit core files (apphost + managed app DLL + Core DLL + policy/mapping).
foreach ($spec in @(
    @{ p = "RiskManagementAI.exe";        c = "App";     r = $true },
    @{ p = "RiskManagementAI.dll";        c = "App";     r = $true },
    @{ p = "RiskManagementAI.Core.dll";   c = "App";     r = $true },
    @{ p = "config/security_policy.json"; c = "Policy";  r = $true },
    @{ p = "config/column_mapping.json";  c = "Mapping"; r = $true }
)) {
    $full = Join-Path $PublishDir $spec.p
    if (!(Test-Path $full)) {
        if ($spec.r) { throw "Integrity manifest: required file missing in publish output: $($spec.p)" }
        continue
    }
    $item = Get-Item -LiteralPath $full
    $manifestEntries.Add([pscustomobject]@{
        path = $spec.p; size = $item.Length
        sha256 = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
        class = $spec.c; required = $spec.r
    })
}

# Globbed core asset folders (each file integrity-critical for its domain).
foreach ($glob in @(
    @{ dir = "rules";      c = "Rules";    pattern = "*"     },
    @{ dir = "templates";  c = "Template"; pattern = "*"     },
    @{ dir = "config/ncr"; c = "Ncr";      pattern = "*.json"},
    @{ dir = "kb";         c = "Kb";       pattern = "*.csv" },
    @{ dir = "kb";         c = "Kb";       pattern = "*.md"  }
)) {
    $globRoot = Join-Path $PublishDir $glob.dir
    if (-not (Test-Path $globRoot)) { continue }
    Get-ChildItem -LiteralPath $globRoot -Recurse -File -Filter $glob.pattern -ErrorAction SilentlyContinue | ForEach-Object {
        $rel = $_.FullName.Substring($publishFull.Length).TrimStart([char]92, [char]47).Replace('\', '/')
        $manifestEntries.Add([pscustomobject]@{
            path = $rel; size = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            class = $glob.c; required = $true
        })
    }
}

$manifest = [pscustomobject]@{
    version        = $Version
    generatedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
    files          = $manifestEntries.ToArray()
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $ManifestPath -Encoding UTF8
$ManifestHash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash
Write-Host "Integrity manifest written: $($manifestEntries.Count) entries, sha256=$ManifestHash"
Write-Host "  (Runtime verifies this manifest at startup; independent trust anchor is deferred to STAB-WP-05 code signing.)"

Write-Host "Publish completed: $PublishDir"
