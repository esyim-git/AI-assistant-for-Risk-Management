# Codex CORR-WP-01 - Report Reconciliation NOT_RUN Truth-State

> Authority: `docs/39` CORR-WP-01, `docs/38` C-31/RR-17, `docs/53 §3`, ADR-002/011 in `docs/40`.
>
> **NEXT UP is this one WP only.** Conflict order: `AGENTS.md` > `docs/39` > this prompt.

## Problem

When limit inputs are absent, `MainWindow.BuildEmptyLimitAnalysis` creates zero reconciliation checks with `Passed=true`. `ExcelReportBuilder` currently maps only the boolean to PASS/FAIL, so the SUMMARY can say `ReconciliationPassed=PASS` while the same workbook contains High `LIMIT_DATA_REQUIRED`. Zero validation is not a pass.

## Read First

`AGENTS.md`, `SKILLS.md`, `CLAUDE.md`, `docs/38`, `docs/39` CORR-WP-01, `docs/40` ADR-002/011, `docs/28`, `docs/53`, then:

- `.claude/skills/risk-data-limit-review/SKILL.md` and support.
- `.claude/skills/risk-smoke-governance/SKILL.md` and support.
- `.claude/skills/risk-security-guard/SKILL.md` and support.
- `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs`.
- `src/RiskManagementAI.App/MainWindow.xaml.cs`.
- `tests/RiskManagementAI.SmokeTests/ReportTests.cs` and `UiContractTests.cs`.

## Branch

```powershell
git fetch origin
git switch -c feature/corr-wp-01-reconciliation-not-run origin/main
```

No main direct push, no force push. One small squash PR.

## Scope

1. Use one deterministic display-state rule:
   - `CheckCount == 0` -> `NOT_RUN`.
   - `CheckCount > 0 && Passed` -> `PASS`.
   - otherwise -> `FAIL`.
2. Apply it to Excel Report SUMMARY without changing the `ReconciliationSummary` public record or `BuildReport` signature.
3. Align the empty UI analysis path so it is not treated as successful validation.
4. Add additive regression assertions for zero/pass/fail and retained `LIMIT_DATA_REQUIRED` visibility.

## Out Of Scope

- No new reconciliation engine/check/code/state.
- No Prior-Day/UI feature, MainWindow decomposition, VERSION bump, release tag, package publication, or docs truth-sync.
- No external library/NuGet/API/model/signing work.

## Security And Behavior

- Preserve 7-state LimitAnalysisResult, existing RECON codes/check count, Dashboard=Report source data, report paths, hash-only audit, and NoModelMode.
- Do not weaken existing PASS/FAIL cases. Only zero-check display semantics change.
- Keep real data/schema/originals/secrets/models out of tests and repo.
- STOP on any external dependency request.

## Tests

- zero checks with synthetic `Passed=true`: SUMMARY contains `NOT_RUN` and does not represent reconciliation as PASS.
- one or more passing checks: existing PASS behavior remains.
- one or more failed checks: existing FAIL behavior remains.
- missing-limit report still exposes `LIMIT_DATA_REQUIRED` in VALIDATION and EXCEPTION_LIST.
- Existing 900 assertions remain; new assertions use Report/UiContract domain tokens; Unclassified 0.

Run:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
```

Then run Gate A and confirm csproj `PackageReference` 0.

## Report

- Changed files and exact rule location.
- build warning/error count.
- exact `Total=N PASS=N FAIL=0`, domain counts, Unclassified 0.
- zero/pass/fail regression evidence.
- Gate A and PackageReference 0.
- Explicit statement that v0.7.1 assets were not tagged/published and must be rebuilt after merge.

## Review Checklist

False PASS removed / zero=`NOT_RUN` / nonzero PASS/FAIL unchanged / public contract unchanged / LIMIT_DATA_REQUIRED retained / tests additive / NuGet 0 / audit and paths unchanged / Gate A PASS.
