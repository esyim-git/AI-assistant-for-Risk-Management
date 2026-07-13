---
name: risk-local-codex-lifecycle
description: Govern material work in the Risk Management AI Assistant repository. Use for planning, implementation, docs or Skill changes, PR creation, independent review, merge decisions, truth-sync, release work, or approval-gated changes in this repo.
---

# Risk Local Codex Lifecycle

This is the native Codex entry point for the repository lifecycle. It routes work to the existing domain checklists while keeping evidence, review independence, offline boundaries, and GitHub state explicit.

## Authority And Startup

1. Read root `AGENTS.md`, `SKILLS.md`, and compatibility guide `CLAUDE.md`.
2. Read `docs/38`, `docs/39`, `docs/40`, `docs/41`, and `docs/28` as required by `AGENTS.md`.
3. Read the task WP/prompt and only the relevant `.claude/skills/<name>/SKILL.md` plus its direct support files.
4. Treat `.agents/skills/` as the native Codex discovery surface. Treat `.claude/skills/` as the transitional domain-checklist catalog.
5. If a transitional checklist assigns work to Claude or requires Claude review, preserve its technical checks but follow the current actor and merge rules in `AGENTS.md`, `docs/32`, and this Skill.

Never weaken Offline, NuGet 0, no external API/telemetry/auto-update, no automatic SQL/VBA execution, hash-only audit, real-data exclusion, NoModel, or approval STOP rules.

## Route The Work

- Repository/status/roadmap: `risk-repo-audit -> risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner`.
- Implementation: one WP plus its domain checklist, `risk-smoke-governance`, and `risk-security-guard`.
- Docs/Skill governance: `risk-doc-truth-sync`, `risk-security-guard`, and `risk-branch-governance`.
- PR review: `risk-codex-review`, the relevant domain checklist, Gate A, local evidence, and hosted exact-head checks.
- Release: `risk-release-cut -> risk-release-verify -> risk-gate-bc -> risk-doc-truth-sync`.
- Model/runtime, real Pack/NCR, signing, or other approval-gated work: use the matching approval checklist and STOP until explicit owner approval.

## Governed Lifecycle

1. **Freeze evidence.** Record repo path, current branch, `origin/main`, dirty state, relevant release/baseline SHA, formal Gate state, and task boundary.
2. **Isolate changes.** Preserve unrelated user edits. If the checkout is dirty, use a clean non-temporary worktree from live `origin/main`. Use `feature/<WP-ID>-<slug>` for implementation and `planning/<slug>` for docs/truth-sync.
3. **Implement narrowly.** One measurable goal per PR. Cross-cutting findings become a documented follow-up unless required for correctness or safety.
4. **Verify locally.** Run the exact WP checks. Code/tooling behavior changes require Release build, SmokeTest with exact `Total=N PASS=N FAIL=0`, Unclassified 0, Gate A, and dependency/boundary checks.
5. **Publish deliberately.** Stage explicit paths, inspect the staged diff, commit, push the branch, and open a Draft PR with scope, exclusions, tests, Gate state, and rollback.
6. **Review independently.** Important PRs require a separate Local Codex task/context that did not author the final diff. The reviewer reads the exact PR head and posts evidence containing:

   ```text
   independent_review_verdict: pass|changes_required
   reviewed_head: <full SHA>
   ```

   A self-review may prepare the PR but does not satisfy this independent-review gate.
7. **Merge only when eligible.** Recheck exact head; require local gate, hosted `test` and `wpf-build` success, no unresolved conversations, independent verdict `pass`, and current user/standing authorization. Squash merge only.
8. **Close cleanly.** Confirm merged SHA, update truth docs when in scope, remove the merged worktree/local branch safely, prune stale references, and report any retained branch or artifact with a reason. Remote branch deletion follows repository policy and authorization.

## GitHub Actions Unavailable

If Actions are queued, unavailable, quota-blocked, skipped, cancelled, or not run:

- run the closest local equivalent and preserve exact logs/SHA;
- label the result `local-fallback-only`, not hosted green;
- do not treat local fallback as satisfying required protected-branch checks;
- keep merge blocked unless the required checks later run successfully or the operator explicitly approves a documented governance change. Never bypass protection ad hoc.

## Completion Report

Report role, repo path, branch, reviewed/head SHA, deployment class, changed files, local tests, hosted checks, Gate A, independent-review verdict, formal approval/Gate state, boundary confirmation, blockers, rollback, cleanup state, guide/Skill update status, next owner, and the recommended Codex model/effort for the next task when useful.
