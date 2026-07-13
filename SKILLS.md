# SKILLS.md - Project Skills Index

## 1. Purpose And Baseline

Project Skills make Local Codex repeat fragile repository workflows consistently without copying large checklists into every prompt.

- Current main and product code-test baseline: `0a3386f` (PR #139, ARCH-WP-01). The v0.7.1 release Build Commit remains `fa755256` (PR #136); docs-only merges may advance current main without changing either product or release provenance.
- VERSION `0.7.1`, current-main SmokeTest `Total=910 PASS=910 FAIL=0` (reproduced 2026-07-13 at `0a3386f`).
- Latest published release: unsigned `v0.7.1`, ZIP SHA256 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`.
- GOV-WP-02: `VERIFIED` by 2026-07-11 REST readback (Phase A protection + secret scanning/push protection).
- NEXT UP: `BLOCKED pending status/truth-sync`. ARCH-WP-01 is already merged as PR #139; run `risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner` before selecting the next implementation WP. User-driven Gate B/C runs independently on the published v0.7.1 ZIP using `docs/54`.

Current truth: `docs/53_Repository_Audit_and_v1_Execution_Plan.md` and `docs/39` Resume Brief.

## 2. Location And Use

The native lifecycle entry lives at `.agents/skills/risk-local-codex-lifecycle/SKILL.md` and is auto-discoverable by Codex. The 19 existing `.claude/skills/<skill-name>/SKILL.md` packages remain a transitional domain-checklist catalog; Local Codex selects and reads only those relevant to the task. Legacy actor wording inside that catalog does not override `AGENTS.md`, the native lifecycle Skill, or `docs/32`.

Applying a Skill is not a Gate PASS. Evidence and approvals remain mandatory.

## 3. Native Codex Skill (1)

| Skill | Purpose | Class |
|---|---|---|
| `risk-local-codex-lifecycle` | Plan, isolate, implement, verify, publish, independently review, merge, and clean up Risk repo work | Auto-discovered lifecycle router |

## 4. Transitional Domain Catalog (19)

| # | Skill | Purpose | Class |
|---|---|---|---|
| 1 | `risk-repo-audit` | End-to-end goal/status/bug/reachability/release/governance/roadmap audit | Preflight |
| 2 | `risk-status-sync` | VERSION, SHA, SmokeTest, Gate, NEXT UP diagnosis | Preflight/read-only |
| 3 | `risk-doc-truth-sync` | Docs vs implementation/release/Gate truth alignment | Preflight |
| 4 | `risk-wp-planner` | One 14-field WP + matching Codex prompt | Preflight |
| 5 | `risk-codex-review` | Scope/security/test/docs/reachability PR review | Preflight |
| 6 | `risk-smoke-governance` | Assertion preservation, domains, Unclassified=0 | Path-scoped |
| 7 | `risk-release-verify` | Portable ZIP/SHA/manifest/forbidden-file verification | Evidence Gate |
| 8 | `risk-gate-bc` | Offline Test-PC evidence preparation and recording | Evidence Gate |
| 9 | `risk-security-guard` | Gate A: secret/data/original/model/dependency/automation scan | Path-scoped |
| 10 | `risk-data-limit-review` | Input/mapping/join/reconciliation/dashboard=report review | Path-scoped |
| 11 | `risk-analytics-design` | Prior-day/visualization/TopN/heatmap design | Preflight |
| 12 | `risk-rag-ncr-governance` | Public/internal KB, clause, NCR, source-text governance | Path-scoped |
| 13 | `risk-llm-approval` | Local model/runtime approval package and STOP boundary | STOP Gate |
| 14 | `risk-feedback-learning` | Approved example curation/retrieval/reflection, not training | Preflight |
| 15 | `risk-ui-ux-review` | WPF editor/assist/layout/user-flow review | Path-scoped |
| 16 | `risk-branch-governance` | PR/squash/check/protection/tag rules | Preflight |
| 17 | `risk-release-cut` | Lockstep version cut and artifact provenance | Preflight |
| 18 | `risk-arch-refactor` | Behavior-preserving structural decomposition | Preflight |
| 19 | `risk-team-pilot` | Pilot Go/No-Go, kit, KPI, rollback, closure | Evidence Gate |

## 5. Standard Chains

### Repository audit / next direction

```text
$risk-local-codex-lifecycle
-> risk-repo-audit
-> risk-status-sync
-> risk-doc-truth-sync
-> risk-wp-planner (exactly one NEXT UP)
```

### Implementation review

```text
risk-codex-review
+ risk-security-guard
+ domain Skill
+ risk-smoke-governance when tests change
+ risk-branch-governance before merge
```

### Release

```text
risk-status-sync
-> risk-release-cut
-> risk-smoke-governance
-> risk-security-guard
-> risk-release-verify
-> risk-gate-bc (real PC)
-> risk-doc-truth-sync
```

### Team Pilot

```text
risk-gate-bc formal closure
-> risk-team-pilot
-> risk-feedback-learning
-> risk-doc-truth-sync
```

### Local LLM / model / runtime

```text
risk-llm-approval -> STOP until explicit approval
```

## 6. Classification

- **Path-scoped**: `risk-security-guard`, `risk-smoke-governance`, `risk-data-limit-review`, `risk-rag-ncr-governance`, `risk-ui-ux-review`.
- **Preflight**: `risk-repo-audit`, `risk-status-sync`, `risk-doc-truth-sync`, `risk-wp-planner`, `risk-codex-review`, `risk-analytics-design`, `risk-feedback-learning`, `risk-branch-governance`, `risk-release-cut`, `risk-arch-refactor`.
- **Evidence/STOP Gate**: `risk-release-verify`, `risk-gate-bc`, `risk-llm-approval`, `risk-team-pilot`.

## 7. Common Guardrails

- External NuGet/API/telemetry/auto-update/auto-execution 0.
- Real data/schema, internal/NCR original, secret/key/certificate, model/runtime repository inclusion 0.
- Existing tests are not weakened.
- A Core capability is not a WPF capability without a call site.
- A package candidate is not a published Release without remote tag/Release evidence.
- User-reported Test-PC success is not formal Gate evidence.
- Current main SHA, code-test baseline SHA, published tag SHA, and package Build Commit are reported separately.
- STOP items never become implementation WPs without approval.

## 8. Current Sequence

1. ARCH-WP-01 is complete at PR #139; truth-sync the roadmap and select exactly one new NEXT UP WP.
2. Candidate sequence remains UI reachability WPs for Prior-Day, streaming/XLSX profile, Clause search, and reviewed Example reflection, but order is not authorized until truth-sync.
3. .NET 10 migration remains required before Pilot.
4. Formal Gate B/C evidence runs independently from published v0.7.1; Team Pilot follows formal closure on a supported LTS build.

Approval tracks (signing, real NCR/internal Pack, Local LLM/runtime/model) remain independent and STOP-governed.
