# 38. v1.0 Master Roadmap and Release Train

## 0. Current Baseline

- v0.7.1 release Build Commit: `fa755256` (PR #136, docs-only). Product code-test baseline remains `4efb8e6` (PR #135); docs/workflow-only merges may advance current main without changing either.
- VERSION `0.7.1`.
- Local gate: build warning 0/error 0, SmokeTest `Total=907 PASS=907 FAIL=0`, Unclassified 0.
- Latest published release: unsigned `v0.7.1` (`fa755256`), ZIP SHA256 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`.
- v0.7.1 code cut + CORR-WP-01 + final rebuild/tag/Release: `VERIFIED` (published release evidence attached).
- Pre-CORR candidate (`abab29b`, SHA256 `A70D0B37AD92344A2ECFBE0D4D96360F56CBAFFF94363249F0BD1A20ADC1ECDC`) is invalid after #135 and must not be published.
- Formal Gate B/C: `BLOCKED` (`docs/54`). v0.7.0 user-reported evidence remains historical in `docs/48`; v0.7.1 starts a release-specific round.
- **NEXT UP = GOV-WP-02** (hosted evidence + branch protection + secret-scanning alignment; package `docs/39`, prompt `prompts/codex/GOV-WP-02_branch_security_governance.md`). Published v0.7.1 Gate B/C is user-driven in parallel (`docs/54`).

Completed MVP-1~3, R1, R2, R3, STAB-WP-01~04, UX-WP-01~11, KB-WP-01/02, FEEDBACK-WP-01/02, QA-WP-01~09, REL-WP-071 published release, and CORR-WP-01 are not redesigned.

Current repository audit and detailed sequencing: `docs/53_Repository_Audit_and_v1_Execution_Plan.md`. WP history: `docs/39`.

## 1. Non-Negotiables

Offline · external NuGet/API/telemetry/auto-update 0 · SQL/VBA/Golden6 auto-execution 0 · hash-only audit · NoModelMode · real data/schema/internal/NCR original/model/certificate/secret repo inclusion 0 · automatic weight training 0 · test weakening 0 · Prod portable ZIP only.

External dependency, Vector/Embedding, LLM runtime/model, signing credential/tool, real NCR coefficient, or internal Pack requires STOP and approval (`docs/41`, `docs/51`).

## 2. Release Train

| Train | Target | Goal | Gate | Status |
|---|---|---|---|---|
| R1 | v0.5.0 | CSV/CP949/XLSX, mapping, limit join, reconciliation, dashboard=report | Data | VERIFIED |
| R3 | v0.6.0 | Public KB metadata/citation structure, NCR 8-field structure | RAG/NCR | VERIFIED / NCR SCAFFOLD_ONLY |
| STAB | v0.6.1 | version, package manifest, runtime Fail-Closed, test baseline/suites | Security/Release | PARTIAL: signing APPROVAL_REQUIRED |
| R2 | v0.7.0 | semantic hardening, streaming Core, prior-day Core, visualization/report | Data | VERIFIED(Core); WPF reachability PARTIAL |
| REL | v0.7.1 | post-v0.7.0 shipped-artifact parity | Local/Release | VERIFIED; published at `fa755256` (unsigned) |
| CORR | v0.7.1 pre-publish | zero-check reconciliation must be `NOT_RUN` | Data/Report | VERIFIED (#135, `4efb8e6`) |
| GOV | v0.7.x | public PR CI, actual checks, branch protection, secret scanning | Governance | PARTIAL: hosted `test`/`wpf-build` evidence observed; protection/security settings pending |
| v0.8 | v0.8.x | behavior-preserving UI decomposition and Core capability reachability | A/B | NOT_IMPLEMENTED |
| Runtime | v0.9.x | .NET 10 LTS migration before .NET 8 EOS | Release/B | NOT_IMPLEMENTED |
| KB/NCR | v0.9.x+ | approved external Pack tool/SOP; real content remains approval-gated | RAG/NCR | PARTIAL / APPROVAL_REQUIRED |
| R4 | post-approval | Local LLM adapter/runtime/model | Model | APPROVAL_REQUIRED / STOP |
| R6 | v1.0.0 | 2~5 user Team Pilot with real evidence/KPI/rollback | Pilot B/C | BLOCKED |

## 3. Product Capability State

| Capability | Core | WPF/User | Published | Note |
|---|---|---|---|---|
| Safety checkers | VERIFIED | VERIFIED | v0.7.0 | Auto-execution 0 |
| Audit logs/history | VERIFIED | VERIFIED | v0.7.0 | Hash only |
| CSV/CP949 profile | VERIFIED | PARTIAL | v0.7.0 | UI uses in-memory path |
| XLSX profile | VERIFIED | NOT_IMPLEMENTED | library shipped | No Data UI routing |
| Limit/7-state/reconciliation | VERIFIED | VERIFIED | v0.7.1 | zero-check `NOT_RUN` included |
| Prior-Day | VERIFIED | NOT_IMPLEMENTED | v0.7.1 | App call site 0 |
| Streaming profile | VERIFIED | NOT_IMPLEMENTED | Core shipped | App call site 0 |
| RISK_VISUAL/report | VERIFIED | VERIFIED | v0.7.0 | Formal B/C pending |
| KB catalog | VERIFIED | VERIFIED | v0.7.0 | Public metadata |
| Clause search/snippet gate | VERIFIED | NOT_IMPLEMENTED | v0.7.1 | App call site 0; real Pack approval |
| Feedback promotion | VERIFIED | VERIFIED | v0.7.1 | Retrieval, not training |
| Example retrieval/reflection | VERIFIED | NOT_IMPLEMENTED | v0.7.1 | App review path 0 |
| NCR Rule Set | SCAFFOLD_ONLY | PLACEHOLDER | placeholder | Real Pack APPROVAL_REQUIRED |
| Local LLM | PLACEHOLDER | NoModelMode VERIFIED | no model | APPROVAL_REQUIRED |

## 4. Dependency Order

```text
GOV-WP-02 (hosted evidence record + protection/security settings)
  -> ARCH-WP-01
  -> UI-WP-12 Prior-Day
  -> DATA-UI-WP-01 / KB-UI-WP-01 / FEEDBACK-WP-03
  -> RUNTIME-WP-01 (.NET 10)
  -> formal Gate B/C
  -> v1.0 Team Pilot
```

User-driven Gate B/C evidence collection may run in parallel from the published v0.7.1 ZIP; formal closure remains required before Team Pilot.

Approval tracks run only when approved and do not block safe Core/UI work: STAB-WP-05 signing, real NCR/internal Pack, R4 LLM.

## 5. Traceability Matrix

Historical WP details and per-test increments remain in `docs/39`.

| Cap | Capability | WP | Evidence | Status |
|---|---|---|---|---|
| C-01~08 | R1 data/limit/reconciliation/report SoT | WP-01~08 | Limit/Mapping/Report/Csv/Xlsx tests | VERIFIED |
| C-10 | KB metadata/index/citation/access guard | R3-WP-01~04 | Kb tests | VERIFIED |
| C-11 | NCR 8-field structure | R3-WP-05 | Ncr tests | SCAFFOLD_ONLY |
| C-12 | version/manifest/runtime/test structure | STAB-WP-01~04 | Packaging tests/build/00~03 | PARTIAL (signing pending) |
| C-13~16 | R2 semantic/streaming/prior-day/visual | R2-WP-01~04 | Limit/DataProfile/Report tests | VERIFIED Core; user reachability PARTIAL |
| C-20 | approved Example retrieval/reflection | FEEDBACK-WP-01/02 | Audit/Generation tests | VERIFIED Core; WPF NOT_IMPLEMENTED |
| C-21 | Test-PC/Pilot evidence | PILOT | `docs/54` (`docs/48` historical) | BLOCKED |
| C-22~30 | Assist/layout/Excel helper | UX/STAB-UX | Assist/UiContract tests | VERIFIED(local-gate) |
| C-28 | Clause Pack/search/snippet gate | KB-WP-01/02 | Kb tests | VERIFIED Core; WPF NOT_IMPLEMENTED |
| C-31 | Reconciliation display truth-state | CORR-WP-01 | Report + UiContract regression; `Total=907` | VERIFIED (#135, `4efb8e6`) |
| C-32 | Public PR CI and branch protection | audit change + GOV-WP-02 | #135 hosted checks + API evidence | PARTIAL: checks green, settings pending |
| C-33 | MainWindow decomposition | ARCH-WP-01 | behavior/UI contract parity | NOT_IMPLEMENTED |
| C-34 | Prior-Day WPF reachability | UI-WP-12 | UiContract/Limit/Report + Gate B | NOT_IMPLEMENTED |
| C-35 | .NET 10 LTS runtime | RUNTIME-WP-01 | build/smoke/package/Gate B | NOT_IMPLEMENTED |

## 6. Risk Register

| ID | Risk | Severity | Control / Next | Status |
|---|---|---|---|---|
| RR-01~03 | demo limit, CP949, dashboard-report drift | High | R1 WPs | VERIFIED mitigation |
| RR-07 | internal/NCR original ingress | High | Gate A + guard + build/03 + ignore | Controlled |
| RR-08 | large/corrupt input memory | Medium | caps + streaming Core | PARTIAL: UI path absent |
| RR-10 | test weakening | High | Smoke governance | Controlled |
| RR-14 | manifest co-tamper/runtime DLL trust | High | STAB-WP-05 | APPROVAL_REQUIRED |
| RR-16 | Gate PASS overclaim | High | formal evidence rule | Controlled, Gate BLOCKED |
| RR-17 | zero reconciliation checks shown as PASS | High | CORR-WP-01 | VERIFIED mitigation (#135) |
| RR-18 | public main unprotected, audit-input CI manual, secret scan off | High | workflow restoration in audit change + GOV-WP-02 settings | PARTIAL mitigation |
| RR-19 | .NET 8 EOS 2026-11-10 | High | RUNTIME-WP-01 before Pilot | OPEN |
| RR-20 | Core-only features described as user-facing | Medium | reachability matrix + UI WPs | OPEN |
| RR-21 | MainWindow 1,614-line concentration | Medium | ARCH-WP-01 | OPEN |
| RR-22 | mutable path roots depend on CWD | Medium | injected app-data root design | OPEN |

## 7. Gates

- Data Spec Gate: R1/R2 correctness; CORR-WP-01 closed at #135, so final v0.7.1 rebuild is unblocked.
- Release Gate: exact main commit, build/smoke, build/00~03, SHA, manifest, forbidden files.
- RAG/NCR Gate: real Pack/content/coefficients require owner approval and stay outside repo.
- Model Gate: runtime/model/embedding approval before implementation.
- Pilot Gate B/C: real Test-PC evidence and rollback/performance rows; formal state remains BLOCKED.
- Governance Gate: PR CI uses actual job names; branch protection cannot require a nonexistent check or an impossible self-approval.

## 8. Handoff

- Implementation source: `docs/39` Resume Brief and one matching prompt.
- Current audit: `docs/53`.
- Architecture: `docs/40`; approval: `docs/41`/`docs/51`; active Gate evidence: `docs/54` (`docs/48` historical); v0.7.1: `docs/52`.
- Work through feature/planning branches and squash PRs. Recheck live head SHA before merge.
