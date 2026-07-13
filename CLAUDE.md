# CLAUDE.md

Compatibility guide retained for existing links and historical records. Claude-specific active actor assignments are retired: Local Codex owns current planning, implementation, verification, Draft PR authoring, and review coordination. Root `AGENTS.md`, the native `risk-local-codex-lifecycle` Skill, and `docs/32` govern current actor and merge rules.

## 0. Current Truth

- Current main and product code-test baseline: `0a3386f` (PR #139, ARCH-WP-01). The v0.7.1 release Build Commit remains `fa755256` (PR #136); later code/test merges advance the product baseline without changing published release provenance.
- VERSION `0.7.1`; authoritative current-main SmokeTest `Total=910 PASS=910 FAIL=0` (reproduced 2026-07-13 at `0a3386f`).
- Latest published release is unsigned `v0.7.1` at `fa755256`; ZIP SHA256 is `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`.
- Formal Gate B/C is `BLOCKED` for v0.7.1 (`docs/54`); v0.7.0 user-reported history in `docs/48` does not carry forward.
- **GOV-WP-02 = VERIFIED** by 2026-07-11 REST readback: `main` Phase A protection is active on strict `test`/`wpf-build`, secret scanning and push protection are enabled, and approvals 0/Code Owner OFF preserve the single-account workflow.
- **NEXT UP = BLOCKED pending status/truth-sync**. ARCH-WP-01 is already merged as PR #139; do not reuse the stale `docs/38`/`docs/39` NEXT UP entry. Select the next WP only through `risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner`. User-driven Gate B/C proceeds independently against the published v0.7.1 assets (`docs/54`); `docs/48` remains v0.7.0 historical evidence.
- Full evidence and roadmap: `docs/53_Repository_Audit_and_v1_Execution_Plan.md`.

Do not redesign completed MVP-1~3, R1, R2, R3, STAB-WP-01~04, UX-WP-01~11, KB-WP-01/02, FEEDBACK-WP-01/02, QA-WP-01~09, REL-WP-071 published release, CORR-WP-01, GOV-WP-02, or ARCH-WP-01.

## 1. Project Identity

Build a Windows offline/local, human-reviewed risk-management Copilot for:

- Golden6 SQL/VBA authoring support and safety checks.
- CSV/XLSX profile, mapping, limit monitoring, reconciliation, and prior-day analysis.
- Excel 2021 dashboard/report generation.
- Public regulation/approved knowledge retrieval and NCR structure guidance.
- Approved feedback example curation/retrieval.
- Hash-only audit history.

It never auto-executes SQL/VBA, makes official legal/NCR decisions, or silently trains model weights.

## 2. Environment Split

```text
GitHub / Dev PC       = Dev
Local validation PC  = Test
Company network PC   = Prod
```

Prod is execution-only. A design that requires SDK, NuGet restore, Git, Python, or external download in Prod fails the deployment contract.

## 3. Absolute Rules

- External NuGet `PackageReference` 0.
- External API/cloud API/telemetry/auto-update 0.
- SQL/VBA/Golden6 automatic execution 0.
- Real company data/schema, internal regulation/NCR original, credential/key/certificate, model/runtime repo inclusion 0.
- Audit source text 0; user ID hashes only.
- No automatic model-weight training.
- Existing tests are not deleted or weakened.
- NoModelMode remains a complete safe mode.
- Prod receives only the portable Release ZIP.

Any external library, Vector/Embedding, Local LLM runtime/model, signing credential/tool, real NCR coefficient, or internal source document triggers STOP and `docs/41`/`docs/51` approval.

## 4. SQL Output Contract

1. Purpose.
2. Table/column assumptions.
3. SQL.
4. Conditions.
5. Validation SQL.
6. Result interpretation.
7. Operational cautions.
8. Hidden Risk.

Read-only is default. Block INSERT, UPDATE, DELETE, MERGE, CREATE, ALTER, DROP, TRUNCATE, GRANT, REVOKE, EXEC, CALL, COMMIT, and ROLLBACK.

## 5. VBA Contract

Excel 2021 only. Require `Option Explicit`, explicit variables, error handling, source protection, Application state restoration, and array-first processing. Block or warn on Shell, WScript.Shell, Kill, destructive FileSystemObject operations, WinAPI/PtrSafe, Outlook auto-send, and external URLs.

## 6. Excel 2021 Contract

Block Microsoft 365-only functions listed by the active ruleset, including VSTACK/HSTACK/TEXTSPLIT/GROUPBY/MAP families. Prefer XLOOKUP/XMATCH/FILTER/SORT/SORTBY/UNIQUE/SEQUENCE/LET, SUMIFS/COUNTIFS/INDEX/MATCH, PivotTable, helper columns, SQL aggregation, or reviewed VBA.

## 7. Regulation/NCR Answer Contract

1. Question summary.
2. As-of date.
3. Document/version.
4. Clause/internal criterion.
5. Review-draft application judgment.
6. Required data.
7. Read-only SQL/VBA/Excel validation.
8. Risk-management cautions.
9. Compliance/risk owner confirmation.
10. Source.

Only public catalog or approved external Pack metadata is repository-safe. Internal originals and official NCR coefficients remain outside the repository and require document-owner approval. Every answer is a review draft, not an official interpretation.

## 8. Status And Evidence

Use only `VERIFIED`, `PARTIAL`, `SCAFFOLD_ONLY`, `PLACEHOLDER`, `BLOCKED`, `NOT_IMPLEMENTED`, `APPROVAL_REQUIRED`.

- Core test success does not prove WPF reachability.
- A local package candidate does not prove a published release.
- User-reported Test-PC success does not replace formal evidence.
- `CheckCount == 0` is `NOT_RUN`, never PASS.
- Current main SHA and code-test baseline SHA are separate concepts; any code/test release-cut change advances the baseline.

## 9. Local Codex Planning And Review Responsibilities

1. Run `risk-repo-audit` for broad repository/status/roadmap requests.
2. Keep `docs/38`, `docs/39`, `docs/40`, `docs/41`, the active Gate evidence doc (`docs/54`; `docs/48` historical), release docs, README, AGENTS, CLAUDE, and SKILLS aligned.
3. Define exactly one NEXT UP WP and a matching implementation prompt.
4. Coordinate review of the diff for scope, security, tests, docs, user reachability, and release provenance. Important PRs use a separate Local Codex task/context for the final exact-head verdict.
5. Maintain Capability -> WP -> Test -> Gate traceability.
6. Keep approval-gated work STOP until the owner records approval.
7. Do not directly edit/merge main; use `planning/*` for planning/truth-sync.

## 10. Local Codex Implementation Responsibilities

- Implement one WP on `feature/<WP-ID>-*`.
- Run build, SmokeTest, relevant package checks, Gate A, and self-review.
- Report exact total and evidence.
- Wait for local evidence, hosted required checks, independent review, and explicit or standing user authorization before merge.
- Preserve unrelated user changes and use a clean worktree.

## 11. Development Loop

```text
risk-repo-audit/status-sync
-> risk-doc-truth-sync
-> risk-wp-planner (one WP)
-> Local Codex implementation + local gate
-> Draft PR
-> separate Local Codex exact-head review + domain Skill + Gate A
-> squash PR
-> truth-sync
```

Local gate remains mandatory:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
```

Hosted PR CI (`test`, `wpf-build`) is the independent second gate. GOV-WP-02 verified the exact checks and enabled Phase A protection plus secret scanning/push protection. If Actions are unavailable or quota-blocked, record the closest local equivalent as `local-fallback-only`; merge remains blocked until protected-branch checks succeed or the operator explicitly approves a documented governance change.

## 12. Skill Operation

The native Local Codex lifecycle Skill lives at `.agents/skills/risk-local-codex-lifecycle/SKILL.md`. The existing `.claude/skills/<name>/SKILL.md` set is a transitional domain-checklist catalog indexed by `SKILLS.md`; legacy actor wording does not override current root governance.

- Broad audit: `risk-repo-audit`.
- State/docs/planning: `risk-status-sync`, `risk-doc-truth-sync`, `risk-wp-planner`.
- Review/security/tests/branch: `risk-codex-review`, `risk-security-guard`, `risk-smoke-governance`, `risk-branch-governance`.
- Release: `risk-release-cut`, `risk-release-verify`.
- Gate/Pilot: `risk-gate-bc`, `risk-team-pilot`.
- Architecture/UI: `risk-arch-refactor`, `risk-ui-ux-review`.
- Data/analytics: `risk-data-limit-review`, `risk-analytics-design`.
- KB/NCR: `risk-rag-ncr-governance`.
- Feedback: `risk-feedback-learning`.
- LLM: `risk-llm-approval` then STOP.

Automatic Preflight means the checklist is selected and read; it does not mean a Gate passed.

## 13. Automatic Preflight

| Work | Chain |
|---|---|
| Full repo/status/roadmap audit | `risk-repo-audit -> risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner` |
| PR/diff review | `risk-codex-review -> risk-security-guard -> domain Skill -> risk-smoke-governance` |
| UI/WPF | `risk-arch-refactor(preflight) -> risk-ui-ux-review -> risk-smoke-governance -> risk-security-guard` |
| Data/limit/report | `risk-data-limit-review -> risk-smoke-governance -> risk-security-guard` |
| KB/NCR | `risk-rag-ncr-governance -> risk-security-guard` |
| Release | `risk-release-cut -> risk-release-verify -> risk-security-guard` |
| Gate B/C | `risk-release-verify -> risk-gate-bc -> risk-doc-truth-sync` |
| Team Pilot | `risk-gate-bc -> risk-team-pilot -> risk-feedback-learning -> truth-sync` |
| LLM/model/runtime | `risk-llm-approval -> STOP` |

## 14. Git And Release

- PR required, squash only, subject contains `(#PR)`.
- Force push, hard reset, and main direct push are prohibited.
- Recheck live head SHA immediately before merge.
- Important PRs require a separate Local Codex exact-head review recorded as `independent_review_verdict` plus `reviewed_head`; numeric approvals stay 0 until a distinct approval-capable GitHub actor exists.
- Rebuild release assets from the exact tag target after every merge; earlier hashes become non-canonical.
- Attach only approved portable artifacts. State unsigned status explicitly.
- Public repository hard protection is active with actual check names and a profile compatible with the current reviewer model; changes follow `docs/32` and require a fresh REST readback.

## 15. Documentation Standard

Write Korean-first with necessary English terms. Each durable design/work document states purpose, scope, exclusions, implementation direction, security, tests, evidence, and future work. Historical documents keep their date/SHA and point to current truth instead of being silently rewritten as current.
