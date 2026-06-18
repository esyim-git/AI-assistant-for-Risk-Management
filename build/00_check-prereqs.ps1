$ErrorActionPreference = "Stop"

Write-Host "Checking prerequisites for Dev environment..."

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "dotnet SDK not found. Install .NET SDK in Dev environment."
}

Write-Host "dotnet path: $($dotnet.Source)"
dotnet --info

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Write-Warning "git not found. Git is recommended in Dev environment."
} else {
    Write-Host "git path: $($git.Source)"
    git --version
}

Write-Host "Prerequisite check completed."
