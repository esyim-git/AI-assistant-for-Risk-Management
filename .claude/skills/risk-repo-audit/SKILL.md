---
name: risk-repo-audit
description: Audit this repository end to end when asked for current status, final product goal, bugs, gaps, documentation or skill drift, release readiness, user-facing reachability, or a prioritized roadmap. Produce evidence-backed findings and hand them to truth-sync and WP planning without overclaiming gates.
---

# Repository Audit

## Workflow

1. Read `AGENTS.md`, `SKILLS.md`, `CLAUDE.md`, `docs/38`, `docs/39`, `docs/40`, `docs/41`, `docs/28`, and the latest release document.
2. Verify live state: current `origin/main`, worktree dirt, `VERSION`, tags/releases, open PRs, workflow triggers, branch protection, and security settings.
3. Run the local evidence gate when available: Release build, authoritative SmokeTest summary, and package verification for a release candidate.
4. Build a capability matrix with separate columns for Core implementation, WPF/user reachability, published-artifact inclusion, Test-PC evidence, and approval state.
5. Inspect architecture concentration, unsafe or misleading status paths, missing call sites, stale references, and public-repository governance.
6. Classify every finding as confirmed bug, reachability gap, documentation drift, operational gap, approval gate, or future enhancement. Attach a path/line, command output, SHA, API result, or governing document.
7. Select exactly one `NEXT UP` implementation WP. Put independent or lower-priority work in a sequenced queue.
8. Send documentation drift to `risk-doc-truth-sync`; send the chosen implementation to `risk-wp-planner`. Do not silently implement multiple WPs.

## Guardrails

- Keep current main SHA and code/test baseline SHA distinct. A release-cut code/test change advances the baseline even when behavior is otherwise unchanged.
- `VERIFIED` Core logic is not automatically a user-facing feature. Require an App/WPF call site before claiming UI availability.
- A local package candidate is not a published release. Require a remote tag and non-draft GitHub Release before saying published.
- User-reported Test-PC success remains separate from formal attached evidence. Keep the formal Gate `BLOCKED` while required evidence or checklist items are missing.
- Never turn an approval-gated item into implementation work. Local LLM runtime, model files, signing credentials/tools, real NCR coefficients, and internal originals remain STOP.
- Preserve user changes in dirty worktrees. Use a clean worktree for audit edits.

## Output

Use the compact evidence shape in [references/audit-checklist.md](references/audit-checklist.md). Lead with the product goal and current verdict, then confirmed findings by severity, capability reachability, roadmap, and exact next owner/action.
