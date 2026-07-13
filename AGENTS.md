# AGENTS.md

Local Codex 및 구현 Agent는 이 파일을 반드시 따른다. 충돌 우선순위는 **AGENTS.md > 지정 Work Package(`docs/39`) > Codex Prompt**다.

## 0. Current Baseline

- Current main and product code-test baseline: `0a3386f5de8209ced9443d371375d63ee0309343` (PR #139, ARCH-WP-01). The v0.7.1 release Build Commit remains `fa7552567cb432ec6a4afe9900b3eca480fc5780` (PR #136); later code/test merges advance the product baseline without changing published release provenance.
- VERSION: `0.7.1`.
- Authoritative local gate at `0a3386f`: build warning 0/error 0, SmokeTest `Total=910 PASS=910 FAIL=0`, Unclassified 0 (reproduced 2026-07-13).
- Latest published release: `v0.7.1` (`fa755256`, unsigned), ZIP SHA256 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`.
- Formal Gate B/C: `BLOCKED` for v0.7.1 (`docs/54`). v0.7.0 user-reported results in `docs/48` are historical and do not carry forward.
- **GOV-WP-02 = VERIFIED** (2026-07-11 REST readback): `main` requires PR + strict `test`/`wpf-build`, conversation resolution, linear history, and admin enforcement; force push/deletion are OFF; approvals 0/Code Owner OFF avoid the current single-account deadlock; secret scanning and push protection are ON.
- **NEXT UP = BLOCKED pending status/truth-sync**. ARCH-WP-01 is already merged as PR #139; do not reuse the stale NEXT UP entry in `docs/38`/`docs/39`. Run `risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner` before new implementation. User-driven Gate B/C continues independently against the published v0.7.1 ZIP (`docs/54`).
- Full current assessment: `docs/53_Repository_Audit_and_v1_Execution_Plan.md`.

Completed MVP-1~3, R1, R2, R3, STAB-WP-01~04, UX-WP-01~11, KB-WP-01/02, FEEDBACK-WP-01/02, QA-WP-01~09, REL-WP-071 published release, CORR-WP-01, GOV-WP-02, and ARCH-WP-01 are not redesigned. Core-only capabilities must not be described as user-facing until an App/WPF call site exists.

## 1. Final Product Boundary

Build an offline, portable, human-reviewed risk-management Copilot for:

- SQL/VBA/Excel 2021 safety checking and drafting support.
- Golden6 manual Export CSV/XLSX profiling.
- Exposure-Limit analysis, reconciliation, prior-day analytics, dashboard, and report.
- Public regulation catalog/approved pack retrieval and review-draft citations.
- Approved feedback example curation/retrieval and hash-only audit.

It is not an automatic DB/VBA executor, official legal interpretation engine, official NCR calculator, or autonomous model trainer.

## 2. Required Reading

Before material work read, in order:

1. `AGENTS.md`, `SKILLS.md`, `CLAUDE.md`.
2. `docs/38_v1_Master_Roadmap.md`.
3. `docs/39_Work_Package_Backlog.md` Resume Brief and the one NEXT UP WP.
4. `docs/40_ADR_Architecture_Evolution.md` and `docs/41_Approval_and_Pilot_Gates.md`.
5. `docs/28_Security_Review_Checklist.md`.
6. The selected `prompts/codex/<WP-ID>_*.md`.
7. Native `.agents/skills/risk-local-codex-lifecycle/SKILL.md`.
8. Relevant transitional `.claude/skills/<skill>/SKILL.md` and its direct support files.

For repository-wide diagnosis, use `risk-repo-audit` before `risk-status-sync`/`risk-doc-truth-sync`/`risk-wp-planner`.

## 3. Role And Workflow

Local Codex owns planning, implementation, local verification, Draft PR authoring, and review coordination. Work one WP at a time on `feature/<WP-ID>-*` unless the user explicitly requests a planning/truth-sync branch.

```text
Local Codex plan -> implementation + local gate -> Draft PR
-> separate Local Codex exact-head review -> fix/re-review
-> squash PR -> truth-sync -> next WP
```

- Important PRs require a separate Local Codex task/context that did not author the final diff. Under the current single GitHub account, record `independent_review_verdict` and `reviewed_head` in PR evidence instead of requiring an impossible counted self-approval.
- Do not merge before local evidence, hosted required checks, independent review, and explicit or standing user authorization.
- Preserve unrelated user changes. Use a clean worktree when the root is dirty.
- Never use force push, hard reset, or main direct push.
- One WP has one measurable goal. Put unrelated findings in the backlog.

## 4. Non-Negotiables

- External NuGet `PackageReference` = 0.
- External API, cloud API, telemetry, auto-update = 0.
- SQL/VBA/Golden6 automatic execution = 0.
- Audit logs store hashes, not source text; user identifiers are hashes.
- No real company data, real schema dictionary, internal regulation original, NCR official original, credential, token, certificate/key, model file/runtime in the repository.
- No automatic model-weight training.
- Existing tests may not be deleted or weakened. Any total decrease requires explicit mapping and approval.
- Nullable remains enabled. Prefer deterministic Ordinal ordering.
- Mutable writes stay under `logs/`, `reports/`, or `config/` with containment guards.
- Prod runs the self-contained portable ZIP only.

## 5. STOP Rules

STOP immediately before adding any external library/NuGet, Vector DB, Embedding runtime, Local LLM runtime/model, signing credential/tool, real NCR coefficient, or internal source document.

The approval packet must state component, reason, license, size, security impact, offline operation, CPU/RAM/GPU, ingress method, alternatives, rollback, and owner approval. Governing documents: `docs/40`, `docs/41`, `docs/51`.

No approval means no dependency, model, credential, real Pack, or runtime change.

## 6. Engineering Standard

- Prefer existing Core contracts and fail-safe/fail-closed patterns.
- Use structured parsers for CSV/XLSX/JSON/XML; no fragile ad hoc replacements.
- Findings contain code, severity, message, and location when available.
- User-facing errors are actionable and do not expose sensitive material.
- Do not claim a Core-only method is available in WPF.
- Do not claim a local package candidate is a published release.
- `CheckCount == 0` or equivalent absence of validation must never be displayed as PASS.

For UI work, run architecture preflight first. ARCH-WP-01 split the former `MainWindow.xaml.cs` concentration point; broad UI work must preserve those behavior-invariant partial boundaries and prove user reachability.

## 7. Test And Security Gate

For code/tooling behavior changes:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
```

Required report:

- build warning/error count.
- exact `Total=N PASS=N FAIL=0` and Unclassified count.
- positive and negative regression cases.
- external PackageReference 0.
- Gate A result from `docs/28`/`risk-security-guard`.
- changed files and user-facing behavior.

Hosted CI (`test`, `wpf-build`) is an independent second gate. Local verification remains required for Windows/WPF/package evidence. If Actions are unavailable or quota-blocked, run and preserve the closest local equivalent as `local-fallback-only`; it is not hosted green and does not satisfy protected-branch checks.

## 8. Release And Gate Rules

Release sequence:

```text
build/00_check-prereqs.ps1
build/01_publish-win-x64.ps1
build/02_package-release.ps1
build/03_verify-package.ps1
```

- Rebuild from the exact commit that will be tagged. A later merge invalidates the previous candidate SHA/provenance.
- Attach only portable ZIP, `.sha256`, and ReleaseNote unless the release WP explicitly adds an approved artifact.
- Unsigned artifacts must say unsigned. Signing remains STAB-WP-05 approval-gated.
- Gate B/C PASS requires real Test-PC evidence. User-reported success is recorded as such and does not replace missing evidence/checklist rows.
- Team Pilot requires formal Gate B/C and real KPI/rollback evidence.

## 9. Status Vocabulary

Use only: `VERIFIED`, `PARTIAL`, `SCAFFOLD_ONLY`, `PLACEHOLDER`, `BLOCKED`, `NOT_IMPLEMENTED`, `APPROVAL_REQUIRED`.

Evidence qualifiers such as `local-gate`, `Core-only`, `user-reported`, and `published` must remain explicit. Do not use DONE/PASS without scope and evidence.

## 10. Native Skill Entry And Transitional Catalog

Codex auto-discovers `.agents/skills/risk-local-codex-lifecycle` as the repository lifecycle entry point. Existing `.claude/skills/*` remain transitional domain checklists until migrated; their technical controls remain applicable, but legacy actor/merge wording is superseded by this file, the native lifecycle Skill, and `docs/32`.

- Repository diagnosis: `risk-repo-audit`.
- State/docs/planning: `risk-status-sync`, `risk-doc-truth-sync`, `risk-wp-planner`.
- Every change: `risk-security-guard`.
- Tests: `risk-smoke-governance`.
- PR/review: `risk-codex-review`, `risk-branch-governance`.
- Release: `risk-release-cut`, `risk-release-verify`.
- UI/refactor: `risk-ui-ux-review`, `risk-arch-refactor`.
- Data/risk: `risk-data-limit-review`, `risk-analytics-design`.
- KB/NCR: `risk-rag-ncr-governance`.
- Feedback: `risk-feedback-learning`.
- Local model: `risk-llm-approval` then STOP.
- Test PC/Pilot: `risk-gate-bc`, `risk-team-pilot`.

Completion reports include `Applied Skills/Checklists: ...`. Skill files are changed only when the user explicitly asks for Skill improvement or the requested governance migration requires it.

## 11. Branch And PR

- Branch: `feature/<wp-id>-<slug>`, `planning/<slug>`, `release/vX.Y.Z`, or `hotfix/<slug>`.
- PR required, squash only, subject includes `(#PR)`.
- No force push, branch deletion bypass, or main direct push.
- Live head SHA must be rechecked before merge.
- Important PR evidence includes `independent_review_verdict: pass` and the exact `reviewed_head` from a separate Local Codex task/context.
- Public-repository hard protection is active with actual CI checks `test` and `wpf-build`; `docs/32` governs changes. Do not require a nonexistent check or one-account self-approval.

## 12. Completion Report

Report role, repo path, reviewed SHA, deployment class, tests, Gate A, formal Gate status, known blockers, next action owner, and guide/Skill update status. Keep Core implementation, WPF reachability, published artifact, Test-PC evidence, and approval state separate.
