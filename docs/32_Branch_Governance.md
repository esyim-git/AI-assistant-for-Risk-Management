# 32. Branch Governance

## Current State (2026-07-10)

- Repository visibility: **public**.
- Merge policy: squash only; merge commit/rebase disabled; delete branch on merge enabled.
- `main` branch protection: **not configured** (REST 404), ruleset count 0.
- Audit-input main (`abab29b`) workflows were manual-only. This audit change restores `ci` on PR/main and `governance-soft-guard` on main push; remote evidence starts only after merge/run.
- Actual CI job/check names: **`test`** and **`wpf-build`**. There is no `build` job.
- PR author and merger are currently the same GitHub account. Required approval count 1/Code Owner review would deadlock self-authored PRs.

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

## Phase A Protection (Current Single-Account Workflow)

Apply after this workflow change produces the first green `test` and `wpf-build` checks:

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

- Enable secret scanning and push protection for the public repository.
- Keep Actions token permissions read-only by default; no repository secrets are needed for build/test.
- Pin third-party/official Action steps to immutable full commit SHAs with a version comment.
- Fork PR workflows use standard hosted runners only. Do not attach a self-hosted company runner to public fork PRs.

## Verification

1. Open a test PR and confirm `test` and `wpf-build` run.
2. Configure Phase A protection with exactly those check names.
3. Confirm direct main push and force push are rejected.
4. Confirm unresolved conversation or red check blocks merge.
5. Confirm a clean, up-to-date PR can squash merge without impossible self-approval.

> Related: `docs/29_GitHub_Sync_Guide.md`, `docs/35_Private_Free_Soft_Guard.md`, `docs/53`, `.github/workflows/`.
