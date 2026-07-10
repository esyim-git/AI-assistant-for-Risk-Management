# 35. Soft Guard History and Public Migration

## Purpose

This file preserves why the advisory soft guard exists and records the migration from private-Free limitations to the current public repository.

## Historical State

While the repository was private on GitHub Free, branch protection API calls returned an upgrade/public-conversion error. The project therefore used:

- squash-only merge settings;
- CODEOWNERS and PR template conventions;
- local build/SmokeTest + Claude review;
- `governance-soft-guard.yml` to detect direct-main push provenance after the fact.

That constraint ended when the repository became public.

## Current State (2026-07-10)

- Visibility is public.
- Hard branch protection is available but not configured.
- Audit-input main (`abab29b`) kept the advisory workflow manual-only; this audit change restores its `main` push trigger.
- Secret scanning and push protection are disabled.

Therefore soft guard alone is no longer the intended end state. Follow `docs/32` Phase A, then Phase B when an independent approving reviewer exists.

## Continued Use

Keep `governance-soft-guard` as a post-merge signal even after protection. It checks the squash subject/provenance convention and gives an additional audit trace. It must run on `push` to `main`, not be required as a PR check.

## Invariants

- Main direct push, force push, hard reset, and bypass merge remain prohibited.
- PRs use squash and include `(#PR)` in the final subject.
- Actual required CI checks are `test` and `wpf-build`, not `build`.
- Numeric approval 1 and Code Owner enforcement wait for a distinct approval-capable reviewer; avoid self-review deadlock.

> Current settings and migration steps: `docs/32_Branch_Governance.md`.
