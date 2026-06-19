param(
    [string]$EventPath = $env:GITHUB_EVENT_PATH
)

$ErrorActionPreference = "Stop"

if ($env:GITHUB_EVENT_NAME -ne "push" -or $env:GITHUB_REF -ne "refs/heads/main") {
    Write-Host "Main soft guard skipped: event is not a push to main."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($EventPath) -or !(Test-Path $EventPath)) {
    throw "GitHub event payload not found. Cannot verify main push provenance."
}

$eventPayload = Get-Content -Path $EventPath -Raw | ConvertFrom-Json
$message = [string]$eventPayload.head_commit.message

$looksLikeGitHubPullRequestMerge =
    ($message -match "\(#\d+\)") -or
    ($message -match "^Merge pull request #\d+ ")

if ($looksLikeGitHubPullRequestMerge) {
    Write-Host "Main soft guard passed: head commit appears to come from a GitHub PR merge."
    exit 0
}

Write-Host "MAIN SOFT GUARD FAILED"
Write-Host "Pushes to main must go through PR review and squash merge."
Write-Host "This repository is private on a plan where branch protection is unavailable, so this workflow is an advisory guard."
Write-Host "Head commit message:"
Write-Host $message
exit 1
