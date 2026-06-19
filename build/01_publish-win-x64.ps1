param(
    [string]$Version = "0.2.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
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

Write-Host "Publish completed: $PublishDir"
