# 32. Branch Governance

## Current State (2026-07-11)

- Repository visibility: **public**.
- Merge policy: squash only; merge commit/rebase disabled; delete branch on merge enabled.
- `main` branch protection: **VERIFIED by REST readback**. PR required; strict required checks `test`/`wpf-build` (`github-actions`, app id `15368`); conversation resolution, linear history, and admin enforcement ON; force push/deletion OFF.
- Required approvals are 0 and Code Owner review is OFF because the PR author/merger is currently one GitHub account. Phase B remains deferred until a distinct reviewer can submit a counted `APPROVED` review.
- Secret scanning and secret scanning push protection: **enabled**. Non-provider patterns/validity checks and Dependabot security updates remain outside GOV-WP-02 scope.
- Evidence source: PR #137 exact-head run #218 (`test`/`wpf-build` success); merge main `fa814f3` run #219 and `governance-soft-guard` run #127 success; protection/security readback 2026-07-11.
- Actual CI job/check names: **`test`** and **`wpf-build`**. There is no `build` job.
- Classic protection is active; repository ruleset count remains 0.

Public GitHub Free repositories support protected branches and standard GitHub-hosted runner usage. The old private-Free limitation is historical; see `docs/35`.

## Branch Model

```text
main                 release-ready, PR only
feature/<wp>-<slug> one implementation WP
planning/<slug>     roadmap/docs/Skill truth-sync
release/vX.Y.Z      packaging/tag preparation
hotfix/<slug>       urgent correction
```

No direct push, force push, hard reset, or branch deletion bypass. Recheck live head SHA before squash merge; commit subject includes `(#PR)`.

## Phase A Protection (Applied Current Single-Account Workflow)

Applied and re-read through REST on 2026-07-11:

- Require a pull request before merging: ON.
- Required approving reviews: **0** until an independent GitHub reviewer exists.
- Require Code Owner review: OFF for the same reason.
- Require status checks: **`test` and `wpf-build`**.
- Require branch up to date: ON.
- Require conversation resolution: ON.
- Require linear history: ON.
- Enforce for administrators / do not allow bypass: ON where plan permits.
- Allow force pushes: OFF.
- Allow deletions: OFF.

Claude/Codex review evidence remains mandatory in the PR body/comments under the project workflow even though GitHub's numeric approval count is 0.

## Phase B Protection (Independent Reviewer Added)

When a distinct reviewer account/team or an approved GitHub App can submit a real approval:

- Required approvals: 1.
- Require review from Code Owners: ON.
- Dismiss stale approvals: ON.
- Require approval of the latest reviewable push: ON.

Do not enable Phase B merely because a bot posted comments; verify that the actor can create a GitHub `APPROVED` review that counts toward protection.

## CI Policy

`ci.yml` must run on PRs to `main` and may run on `main` push as post-merge evidence:

- `test`: Ubuntu, Core SmokeTests, authoritative `Total=N`.
- `wpf-build`: Windows, WPF compile.

Local Windows build/smoke/package remains the implementation/release evidence gate. Hosted CI is an independent second gate.

`governance-soft-guard.yml` runs on `main` push and checks merge provenance. It is advisory backup after hard protection, not a substitute.

## Repository Security Settings

- Secret scanning and push protection are enabled for the public repository.
- Keep Actions token permissions read-only by default; no repository secrets are needed for build/test.
- Pin third-party/official Action steps to immutable full commit SHAs with a version comment.
- Fork PR workflows use standard hosted runners only. Do not attach a self-hosted company runner to public fork PRs.

## Verification

1. Read `main` protection through REST and confirm strict `test`/`wpf-build`, PR requirement, conversation resolution, linear history, admin enforcement, and force/deletion OFF.
2. Read repository `security_and_analysis` and confirm secret scanning/push protection remain enabled.
3. On each ordinary PR, confirm both required checks run on the exact head and unresolved conversations/red checks block merge.
4. Confirm a clean, up-to-date PR can squash merge without impossible self-approval.
5. Do **not** probe protection by direct/force pushing to `main`; REST readback plus ordinary PR behavior is the safe verification path.

> Related: `docs/29_GitHub_Sync_Guide.md`, `docs/35_Private_Free_Soft_Guard.md`, `docs/53`, `.github/workflows/`.
