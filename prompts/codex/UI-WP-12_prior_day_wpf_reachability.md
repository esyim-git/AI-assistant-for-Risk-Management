# UI-WP-12 - Prior-Day Analytics WPF And Report Reachability

You are the Local Codex Implementation/Test Engineer. Execute exactly this one Work Package from the latest `origin/main` after the planning PR that introduced this prompt is merged.

Do not implement any later reachability, runtime, signing, NCR Pack, or LLM work.

## 0. Required Reading And Automatic Skill Bridge

Read in order:

1. `AGENTS.md`
2. `SKILLS.md`
3. `CLAUDE.md`
4. `docs/38_v1_Master_Roadmap.md`
5. `docs/39_Work_Package_Backlog.md` - UI-WP-12 section and Current Resume Brief
6. `docs/40_ADR_Architecture_Evolution.md` - ADR-002 and ADR-011
7. `docs/41_Approval_and_Pilot_Gates.md`
8. `docs/28_Security_Review_Checklist.md`
9. `docs/53_Repository_Audit_and_v1_Execution_Plan.md` - sections 2 and 4 as the pre-ARCH audit basis
10. `.agents/skills/risk-local-codex-lifecycle/SKILL.md`
11. `.claude/skills/risk-ui-ux-review/SKILL.md` and support files
12. `.claude/skills/risk-data-limit-review/SKILL.md` and support files
13. `.claude/skills/risk-smoke-governance/SKILL.md` and support files
14. `.claude/skills/risk-security-guard/SKILL.md`
15. `.claude/skills/risk-branch-governance/SKILL.md`
16. `src/RiskManagementAI.Core/Risk/PriorDayAnalyzer.cs`
17. `src/RiskManagementAI.Core/Risk/PriorDayAnalysisResult.cs`
18. `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs`
19. `src/RiskManagementAI.App/MainWindow.xaml`
20. `src/RiskManagementAI.App/MainWindow.xaml.cs`
21. `src/RiskManagementAI.App/MainWindow.DataRiskReport.cs`
22. `tests/RiskManagementAI.SmokeTests/UiContractTests.cs`
23. `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`
24. `tests/RiskManagementAI.SmokeTests/ReportTests.cs`

Applied Skill Checklists must be reported: `risk-local-codex-lifecycle`, `risk-ui-ux-review`, `risk-data-limit-review`, `risk-smoke-governance`, `risk-security-guard`, `risk-branch-governance`.

## 1. Goal

Expose the existing, verified `PriorDayAnalyzer` through an explicit review-only Risk Dashboard workflow and export that exact result to a static Excel 2021 `PRIOR_DAY` worksheet.

This is reachability work. It must not create or duplicate an analysis engine.

## 2. Branch And Baseline

Use a clean non-temporary worktree from current live main:

```powershell
git fetch origin main --prune
git worktree add <clean-non-temp-path> -b feature/ui-wp-12-prior-day-wpf-reachability origin/main
git status --short --branch
```

Before editing, confirm and record:

- current `origin/main` full SHA,
- `VERSION` is `0.7.1`,
- product code-test baseline in `docs/39` is `0a3386f5de8209ced9443d371375d63ee0309343`,
- local Release build is 0 warnings / 0 errors,
- SmokeTest is `Total=910 PASS=910 FAIL=0`, Unclassified 0,
- `PriorDayAnalyzer` has no App/WPF call site,
- `MainWindow.xaml.cs` remains within the ARCH-WP-01 line cap and the six role partials are present.

The planning PR merge may advance current main without changing the product baseline. If product code or test totals moved intentionally, inspect and report the actual baseline; do not reset or overwrite it.

If any checkout is dirty, preserve it and use another clean worktree. Existing changes belong to the user.

## 3. Exact Scope

### 3.1 Risk Dashboard workflow

Reuse the existing inputs:

- `RiskExposurePathBox`,
- `RiskLimitPathBox`,
- `RiskBaseDateBox` as Current BASE_DT.

Add stable controls for:

- explicit Prior BASE_DT input, named `RiskPriorBaseDateBox`,
- explicit command button with visible text `전일 대비`, wired to `OnRunPriorDayAnalysis`,
- a read-only prior-day summary,
- a read-only comparison grid that exposes Current, Prior, deltas, movement, and status fields,
- a compact review panel that surfaces TopN movers, Methodology, UserValidation, DraftNotice, and HiddenRisk findings.

Keep Current and Prior-Day results easy to distinguish. Prefer a restrained nested tab or equivalent unframed workspace within the existing Risk tab; do not stack cards or shrink the existing grid into an unreadable area. All text must wrap or scroll without overlap at the existing minimum window size.

The command must call the existing `PriorDayAnalyzer` with the explicitly entered paths and dates. It must not infer a prior business day, search for another file, auto-run on text changes, or change `LimitMonitor` behavior.

### 3.2 Result lifetime and audit

Keep only the last successful `PriorDayAnalysisResult` in memory with an input signature derived from the resolved exposure path, resolved limit path, normalized Current date, and normalized Prior date.

- Clear the cached result and visible stale prior-day output when any of those inputs changes.
- Never write the result or raw inputs to local config.
- Append one existing hash-only `TaskLogWriter` audit event for an explicit prior-day run.
- Preserve existing non-fatal `TASK_LOG_WRITE_FAILED` behavior.

Show `PriorDayContract.HiddenRisk.Findings` through the existing structured findings panel. Do not flatten away finding codes or severity.

### 3.3 Excel report reachability

Add one source-compatible optional field at the end of `ExcelReportRequest`:

```csharp
PriorDayAnalysisResult? PriorDayAnalysis = null
```

Existing seven-argument call sites must compile and preserve their current output when the value is null.

Add `PRIOR_DAY` to `ExcelReportBuilder.ExpectedSheetNames`. When `PriorDayAnalysis` is present, write deterministic static values from that exact object:

- Current/Prior dates and KPI counts,
- full `ComparisonTable` in Core order,
- TopN movers in Core order,
- Methodology, UserValidation, DraftNotice, and HiddenRisk finding code/severity/message.

Do not recalculate movement, deltas, TopN, statuses, reconciliation, or limits in the report layer. Do not add formulas that are unnecessary for the static contract.

When no valid cached prior-day result matches the current Risk inputs, report generation must continue through the existing path and the `PRIOR_DAY` sheet must contain an explicit `NOT_RUN`/review-only marker rather than stale data or a fabricated comparison.

## 4. Public Contracts And Compatibility

Allowed additive public change:

- optional trailing `ExcelReportRequest.PriorDayAnalysis`.

Allowed report inventory change:

- `ExpectedSheetNames` gains `PRIOR_DAY`.

Everything else remains unchanged:

- `PriorDayAnalyzer` and all `PriorDay*` records,
- `LimitMonitor`, `LimitAnalysisResult`, seven states, and reconciliation codes,
- existing current-day Risk Dashboard workflow,
- existing report request call sites and workbook sheets,
- hash-only audit schemas,
- NoModelMode.

If implementation requires widening another public contract or changing Core classification logic, STOP and report the exact reason instead of expanding scope.

## 5. Prohibited Changes

- No new analysis engine, status, movement, join, aggregation, threshold, or date-normalization logic.
- No automatic prior-day/business-calendar logic or file/sheet/path inference.
- No streaming/progress/cancellation work; that belongs to `DATA-UI-WP-01`.
- No new charting, dashboard redesign, navigation redesign, MVVM migration, or unrelated ARCH cleanup.
- No LLM commentary, model/runtime, Vector/Embedding, signing, certificate/tool, real NCR/internal Pack, or approval-gated dependency.
- No external NuGet/library/API, telemetry, auto-update, or SQL/VBA execution.
- No VERSION, release, build, workflow, policy, rules, sample, or Gate B/C status change.
- No real company data, real schema/table/column names, internal regulation text, model file, credential, or secret.
- No existing assertion deletion, weakening, message rename, domain reclassification, or fixed-total reduction.

## 6. Error And UX Contract

- Normalize and reject same-day Current/Prior through the existing Core guard.
- Missing files, missing Prior rows, malformed dates, duplicate keys, corrupt CSV/XLSX inputs, and access failures must remain graceful and reviewable.
- Catch only the established user-facing exception set: `ArgumentException`, `IOException`, `InvalidDataException`, `UnauthorizedAccessException`.
- On failure, clear stale prior-day rows/summary and surface a structured High finding; do not crash the app.
- Keep Current-day controls usable even when Prior-Day fails.
- Use existing WPF controls and styles. Add no decorative cards, oversized type, or layout that overlaps at 1180x720.

## 7. Tests

Use the existing SmokeTest harness only. Preserve all 910 assertions and messages, then add focused tests.

### UiContract

- stable Prior input/summary/grid/review control names,
- `전일 대비` button and `OnRunPriorDayAnalysis` wiring,
- existing `한도 점검` path remains present,
- UI handler calls `PriorDayAnalyzer` rather than duplicating movement/status logic,
- input-change invalidation prevents stale report reuse,
- layout remains bounded and scrollable.

### Limit/Reconciliation

- reuse existing current/prior/delta, six movement, TopN, 4-section, same-day, missing-row, and duplicate-key fixtures,
- add a test only where UI/report integration exposes an uncovered Core boundary,
- do not duplicate already sufficient Core assertions merely to increase Total.

### Report

- existing seven-argument request remains source compatible,
- null prior-day input preserves existing report data and emits deterministic `PRIOR_DAY=NOT_RUN`,
- valid prior-day input writes the same dates, counts, row values, movements, TopN order, and findings as the supplied Core object,
- `ExpectedSheetNames`, workbook relationships, content types, app properties, and final sheet indexing remain correct,
- formulas still pass `Excel2021FunctionChecker`,
- report path traversal guards and hash-only audit remain intact.

Required result:

- Release build 0 warnings / 0 errors,
- SmokeTest `Total=N PASS=N FAIL=0`, `N >= 910`, Unclassified 0,
- external `PackageReference` 0,
- Gate A 0,
- no test weakening.

Do not invent a target Total before implementation.

## 8. Local And Hosted Gates

Run from the clean worktree root:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
dotnet list package --format json
```

Run the repository Gate A procedure in `docs/28`. Record changed files, exact head SHA, totals by domain, PackageReference evidence, and any unexecuted manual checks.

After the Draft PR is pushed, hosted `test` and `wpf-build` must both succeed at the exact current PR head before merge. A quota/runner outage is `unavailable_quota_or_runner`, not green; follow the repository fallback rules without disabling protection.

Actual WPF rendering, focus/resize behavior, Excel 2021 manual opening, and offline Test-PC operation remain formal Gate B evidence in `docs/54`. Do not mark them PASS from source tests.

## 9. Docs And Truth Sync

In the implementation PR, update only living status affected by the completed WP:

- `docs/38` C-34/capability/NEXT UP,
- `docs/39` Current Resume Brief and UI-WP-12 result evidence.

Do not rewrite historical snapshots, release evidence, or `docs/54` operator results. Select the following NEXT UP only after implementation and independent review evidence supports that decision.

Skill files are read-only unless the user explicitly asks to update them.

## 10. Commit, Draft PR, And Independent Review

Commit subject:

```text
feat: expose prior-day analytics in WPF and report (UI-WP-12)
```

Push the feature branch and open a Draft PR to `main`. The PR body must include:

- classification: behavior/product change, no production activation,
- base/head full SHAs,
- changed files and public contract delta,
- current/prior UI behavior and stale-result invalidation,
- report single-source evidence,
- local build/SmokeTest/domain totals/PackageReference/Gate A evidence,
- hosted checks state,
- docs/Skill status,
- Gate B manual checks still BLOCKED,
- rollback and clean-worktree cleanup conditions.

Then request a separate Local Codex task/context that did not author the final diff to review the exact PR head. The PR evidence must include:

```text
independent_review_verdict: pass|changes_required
reviewed_head: <full SHA>
```

Any new push invalidates the prior verdict and requires exact-head re-review. Do not merge without clean independent review, required hosted checks, and an explicit user merge authorization. Merge is not deployment or Gate B/C closure.
