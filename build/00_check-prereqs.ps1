$ErrorActionPreference = "Stop"

Write-Host "Checking prerequisites for Dev environment..."

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "dotnet SDK not found. Install .NET SDK in Dev environment."
}

Write-Host "dotnet path: $($dotnet.Source)"
$sdks = @(dotnet --list-sdks)
if ($LASTEXITCODE -ne 0) {
    throw "dotnet SDK check failed. Verify .NET SDK installation in Dev environment."
}

if ($sdks.Count -eq 0) {
    throw ".NET SDK not found. dotnet runtime is present, but release publishing requires .NET 8 SDK in Dev/Test environment."
}

$net8Sdk = $sdks | Where-Object { $_ -match "^8\." }
if ($null -eq $net8Sdk -or $net8Sdk.Count -eq 0) {
    throw ".NET 8 SDK not found. Install .NET 8 SDK before running release packaging."
}

Write-Host ".NET SDKs installed:"
$sdks | ForEach-Object { Write-Host "  $_" }
dotnet --info

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Write-Warning "git not found. Git is recommended in Dev environment."
} else {
    Write-Host "git path: $($git.Source)"
    git --version
}

Write-Host "Prerequisite check completed."
