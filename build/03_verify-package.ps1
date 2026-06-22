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

# Keep this mirror synchronized with RiskManagementAI.Core.Kb.KbRepositoryGuard.
# SmokeTest asserts that every guard token/allowlist value appears here so drift
# fails in CI before a release package can be cut.
$SourceTextScanDirs = @("kb", "config", "samples", "data_sources")
$SourceTextAllowlist = @(
    "kb/README.md",
    "kb/public_regulation_catalog.csv",
    "kb/ncr_placeholder.md"
)

function New-StringFromCodeUnits {
    param([int[]]$CodeUnits)

    $chars = New-Object char[] $CodeUnits.Length
    for ($i = 0; $i -lt $CodeUnits.Length; $i++) {
        $chars[$i] = [char]$CodeUnits[$i]
    }

    return -join $chars
}

$InternalRegulationOriginalText = New-StringFromCodeUnits @(0xB0B4, 0xBD80, 0xADDC, 0xC815, 0x0020, 0xC6D0, 0xBB38)
$NcrOfficialOriginalText = New-StringFromCodeUnits @(0x004E, 0x0043, 0x0052, 0x0020, 0xACF5, 0xC2DD, 0xBCF8, 0x0020, 0xC6D0, 0xBB38)

$SuspiciousNameTokens = @(
    "internal_rule_original",
    "internal_regulation_original",
    "ncr_official_original",
    "official_text",
    "full_text"
)
$SuspiciousContentTokens = @(
    $InternalRegulationOriginalText,
    $NcrOfficialOriginalText,
    "official text",
    "full text"
)
$SourceTextExtensions = @(".csv", ".json", ".jsonl", ".md", ".txt", ".sql")

function Test-ContainsOrdinalIgnoreCase {
    param(
        [string]$Text,
        [string]$Value
    )

    return $Text.IndexOf($Value, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Get-ZipRelativePath {
    param(
        [string]$Root,
        [string]$FullName
    )

    $rootFullPath = [System.IO.Path]::GetFullPath($Root)
    $fileFullPath = [System.IO.Path]::GetFullPath($FullName)
    $separator = [System.IO.Path]::DirectorySeparatorChar.ToString()
    if (-not $rootFullPath.EndsWith($separator, [System.StringComparison]::Ordinal)) {
        $rootFullPath = $rootFullPath + $separator
    }

    if (-not $fileFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Extracted file path is outside scan root: $fileFullPath"
    }

    return $fileFullPath.Substring($rootFullPath.Length).Replace('\', '/')
}

function Get-DecodedTextVariants {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $variants = New-Object System.Collections.Generic.List[string]
    $utf8Encoding = New-Object System.Text.UTF8Encoding -ArgumentList $false, $false

    foreach ($encoding in @(
        $utf8Encoding,
        [System.Text.Encoding]::Unicode,
        [System.Text.Encoding]::BigEndianUnicode,
        [System.Text.Encoding]::Default
    )) {
        try {
            $variants.Add($encoding.GetString($bytes))
        } catch {
            # Try the next decoder.
        }
    }

    try {
        Add-Type -AssemblyName System.Text.Encoding.CodePages -ErrorAction SilentlyContinue
        [System.Text.Encoding]::RegisterProvider([System.Text.CodePagesEncodingProvider]::Instance)
    } catch {
        # Windows PowerShell/.NET Framework may support code page 949 without this provider.
    }

    try {
        $variants.Add([System.Text.Encoding]::GetEncoding(949).GetString($bytes))
    } catch {
        # Code page 949 may be unavailable on a minimal host; UTF-8/ASCII tokens still run.
    }

    return $variants
}

function Test-SuspiciousFileContent {
    param([string]$Path)

    $extension = [System.IO.Path]::GetExtension($Path)
    if (-not ($SourceTextExtensions -contains $extension)) {
        return $false
    }

    $textVariants = Get-DecodedTextVariants -Path $Path
    foreach ($text in $textVariants) {
        foreach ($token in $SuspiciousContentTokens) {
            if (Test-ContainsOrdinalIgnoreCase -Text $text -Value $token) {
                return $true
            }
        }
    }

    return $false
}

function Find-ForbiddenSourceText {
    param([string]$ExtractedRoot)

    $problems = @()
    foreach ($relativeDir in $SourceTextScanDirs) {
        $scanRoot = Join-Path $ExtractedRoot $relativeDir
        if (!(Test-Path $scanRoot)) {
            continue
        }

        $files = Get-ChildItem -LiteralPath $scanRoot -Recurse -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            $relativePath = Get-ZipRelativePath -Root $ExtractedRoot -FullName $file.FullName
            if ($SourceTextAllowlist -contains $relativePath) {
                continue
            }

            foreach ($token in $SuspiciousNameTokens) {
                if (Test-ContainsOrdinalIgnoreCase -Text $relativePath -Value $token) {
                    $problems += "FORBIDDEN source-text filename token '$token' in ZIP: $relativePath"
                    break
                }
            }

            if (Test-SuspiciousFileContent -Path $file.FullName) {
                $problems += "FORBIDDEN source-text content token in ZIP: $relativePath"
            }
        }
    }

    return $problems
}

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

# --- 3) Extract ZIP and scan source-text assets by name and content ---
$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("RiskManagementAI_package_scan_" + [Guid]::NewGuid().ToString("N"))
try {
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractRoot -Force
    $sourceTextProblems = Find-ForbiddenSourceText -ExtractedRoot $extractRoot
    if ($sourceTextProblems.Count -gt 0) {
        Write-Host "PACKAGE SOURCE-TEXT VERIFICATION FAILED:"
        $sourceTextProblems | ForEach-Object { Write-Host " - $_" }
        exit 1
    }
} finally {
    if (Test-Path $extractRoot) {
        Remove-Item $extractRoot -Recurse -Force
    }
}

Write-Host "Package source-text OK: no internal-regulation/NCR original text detected."
Write-Host "Verify completed for v$Version."
