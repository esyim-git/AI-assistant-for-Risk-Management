param(
    [string]$Version = "0.2.0"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$ReleaseDir = Join-Path $Root "artifacts\release"
$ZipPath = Join-Path $ReleaseDir "RiskManagementAI-v$Version-win-x64-portable.zip"
$HashPath = "$ZipPath.sha256"

if (!(Test-Path $ZipPath)) { throw "ZIP not found: $ZipPath" }
if (!(Test-Path $HashPath)) { throw "SHA256 file not found: $HashPath" }

# --- 1) SHA256 integrity ---
$ExpectedLine = Get-Content $HashPath -Raw
$ExpectedHash = ($ExpectedLine -split "\s+")[0].Trim()
$ActualHash = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash

if ($ActualHash -ne $ExpectedHash) {
    Write-Host "SHA256 MISMATCH"
    Write-Host "Expected: $ExpectedHash"
    Write-Host "Actual  : $ActualHash"
    exit 1
}
Write-Host "SHA256 OK: $ActualHash"

# --- 2) Inspect ZIP contents without extracting ---
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $entries = $zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') }
} finally {
    $zip.Dispose()
}

$problems = @()

# 2a) Required contents present (Req 7/8/9).
$requiredFiles = @("RiskManagementAI.exe", "run.bat")
foreach ($f in $requiredFiles) {
    if (-not ($entries | Where-Object { $_ -ieq $f })) {
        $problems += "MISSING required file: $f"
    }
}

$requiredDirs = @("config/", "rules/", "kb/", "templates/", "samples/", "deploy/", "logs/", "reports/")
foreach ($d in $requiredDirs) {
    if (-not ($entries | Where-Object { $_ -ilike "$d*" })) {
        $problems += "MISSING required folder: $d"
    }
}

# 2b) Forbidden contents absent (Req 10/11).
$forbiddenExt = @(".gguf", ".safetensors", ".onnx", ".pt", ".pem", ".key", ".pfx", ".env")
$forbiddenPathNames = @("real_data", "secrets", "credentials", "exports")
foreach ($e in $entries) {
    $ext = [System.IO.Path]::GetExtension($e)
    if ($forbiddenExt -contains $ext) {
        $problems += "FORBIDDEN file (model/secret) in ZIP: $e"
    }
    $pathParts = $e -split "/+"
    if ($pathParts | Where-Object { ($forbiddenPathNames -contains $_) -or ($_ -like "internal_*") }) {
        $problems += "FORBIDDEN path (real-data/internal) in ZIP: $e"
    }
}

if ($problems.Count -gt 0) {
    Write-Host "PACKAGE CONTENT VERIFICATION FAILED:"
    $problems | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "Package content OK: required assets present, no model/secret/real-data files."
Write-Host "Verify completed for v$Version."
