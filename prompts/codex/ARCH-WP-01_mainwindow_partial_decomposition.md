# ARCH-WP-01 - MainWindow Behavior-Invariant Partial Decomposition

You are the Implementation/Test Engineer. Execute exactly this one Work Package from current `origin/main` after the PR that introduced this prompt is merged.

## 0. Required Reading And Skill Bridge

Read in order:

1. `AGENTS.md`
2. `SKILLS.md`
3. `CLAUDE.md`
4. `docs/38_v1_Master_Roadmap.md`
5. `docs/39_Work_Package_Backlog.md` - ARCH-WP-01 section
6. `docs/40_ADR_Architecture_Evolution.md`
7. `docs/41_Approval_and_Pilot_Gates.md`
8. `docs/28_Security_Review_Checklist.md`
9. `docs/proposals/FABLE5_REPO_ASSESSMENT_PROPOSAL_20260706.md` - WP-D
10. `.claude/skills/risk-arch-refactor/SKILL.md` and support files
11. `.claude/skills/risk-ui-ux-review/SKILL.md` and support files
12. `.claude/skills/risk-smoke-governance/SKILL.md` and support files
13. `.claude/skills/risk-security-guard/SKILL.md`
14. `.claude/skills/risk-branch-governance/SKILL.md`
15. `src/RiskManagementAI.App/MainWindow.xaml.cs`
16. `tests/RiskManagementAI.SmokeTests/UiContractTests.cs`

Applied Skill Checklists must be reported: `risk-arch-refactor`, `risk-ui-ux-review`, `risk-smoke-governance`, `risk-security-guard`, `risk-branch-governance`.

## 1. Goal

Reduce the 1,614-line `MainWindow.xaml.cs` to 600 lines or fewer by moving existing methods verbatim into role-based `MainWindow.*.cs` partial-class files. Preserve every user-visible behavior, XAML binding, method signature, output contract, and audit result.

This is structural decomposition stage 1 only. It is not a feature WP or an MVVM migration.

## 2. Branch And Baseline

```powershell
git fetch origin main
git switch -c feature/arch-wp-01-mainwindow-partial-decomposition origin/main
git status --short --branch
```

Before editing, confirm:

- `VERSION` is `0.7.1`.
- Product code-test baseline documented in `docs/39` is `4efb8e6`.
- Local baseline build is 0 warnings / 0 errors.
- Local baseline SmokeTest is `Total=907 PASS=907 FAIL=0`, Unclassified 0.
- `MainWindow.xaml.cs` is 1,614 lines at the documented baseline. If current `origin/main` has an intentional later change, report the actual pre-edit line count and inspect it; do not discard it.

If the worktree is dirty or contains unrelated user changes, stop and create a clean worktree. Never overwrite or stage unrelated changes.

## 3. Exact Scope

Keep `MainWindow.xaml.cs` for fields, constructor, layout lifecycle, nested display DTOs, and `MainTabKey`. Move existing methods without logic edits into these partial files:

- `MainWindow.CompletionAssist.cs`
- `MainWindow.Navigation.cs`
- `MainWindow.SafetyDraftExcel.cs`
- `MainWindow.KnowledgeAudit.cs`
- `MainWindow.DataRiskReport.cs`
- `MainWindow.Presentation.cs`

Use the current namespace and `public partial class MainWindow : Window`. Move required `using` directives to the narrowest file that compiles. Do not change accessibility or signatures to make the split easier.

Update `UiContractTests.cs` so every existing source-text assertion reads one deterministic contract surface made by:

1. enumerating `src/RiskManagementAI.App/MainWindow*.cs`,
2. ordering paths with `StringComparer.Ordinal`, and
3. concatenating their contents.

Preserve every existing assertion and assertion message. Add structural guards for:

- `MainWindow.xaml.cs <= 600` lines,
- the required partial-file set,
- aggregation coverage of every `MainWindow*.cs` source file.

## 4. Prohibited Changes

- No XAML, tab, event wiring, handler signature, keyboard behavior, focus behavior, text, layout, or visual change.
- No method-body cleanup, renaming, reordering logic, abstraction extraction, DTO/enum relocation, controller/DI/MVVM migration, async/cancellation, or new feature.
- No Core, build, workflow, VERSION, release, package, policy, rules, templates, samples, or configuration changes.
- No test deletion, weakening, message rename, domain reclassification, or fixed-total reduction.
- No external NuGet/library, API, telemetry, auto-update, auto-execution, model/runtime, certificate/tool, real data, schema, regulation/NCR original, or secret.
- Do not start UI-WP-12 or any other WP.

If the split cannot compile without a behavior/public-contract change, STOP and report the exact dependency instead of widening scope.

## 5. Implementation Discipline

1. Create a method-to-file inventory before moving code.
2. Move complete methods in coherent blocks; do not rewrite their bodies.
3. Build after each coherent move so accessibility/using errors stay attributable.
4. Keep source ordering deterministic and ASCII unless an existing moved string requires its current encoding.
5. Compare pre/post handler names and signatures. XAML remains byte-unchanged.
6. Report each file's before/after line count and the final method-to-file mapping.

## 6. Local Gate

Run from the repository root:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
dotnet list package --format json
```

Required:

- build 0 warnings / 0 errors,
- SmokeTest `Total=N PASS=N FAIL=0`, `N >= 907`, Unclassified 0,
- every prior assertion/message preserved and structural assertions additive,
- external `PackageReference` 0,
- Gate A 0 under `docs/28`,
- `git diff -- src/RiskManagementAI.App/MainWindow.xaml` empty,
- no product behavior, release, workflow, or STOP-gated changes.

## 7. Self-Review And PR

Review the diff as a behavior-preserving refactor:

- method bodies moved rather than edited,
- no omitted/duplicated handler,
- XAML references still resolve,
- deterministic source aggregation covers all partials,
- line-cap test cannot pass by ignoring files,
- no audit/output/security drift,
- no unrelated format churn.

Commit subject:

```text
refactor: split MainWindow into behavior-invariant partials (ARCH-WP-01)
```

Push the feature branch and open a PR to `main`. The PR must include baseline/final line counts, method-to-file mapping, local build/SmokeTest/PackageReference/Gate A evidence, and changed files. Wait for exact-head hosted `test` and `wpf-build` success and review feedback.

Do not self-merge. Claude/user review and an explicit merge instruction remain required.
