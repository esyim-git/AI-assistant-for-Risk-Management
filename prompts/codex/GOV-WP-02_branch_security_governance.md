# GOV-WP-02 - Public Branch And Security Governance

You are the Implementation/Test Engineer. Execute exactly this one Work Package from current `origin/main` after the PR that introduced this prompt is merged.

## 0. Required Reading And Skill Bridge

Read in order:

1. `AGENTS.md`
2. `SKILLS.md`
3. `docs/38_v1_Master_Roadmap.md`
4. `docs/39_Work_Package_Backlog.md` - GOV-WP-02 section
5. `docs/32_Branch_Governance.md`
6. `docs/53_Repository_Audit_and_v1_Execution_Plan.md`
7. `docs/28_Security_Review_Checklist.md`
8. `.claude/skills/risk-branch-governance/SKILL.md`
9. `.claude/skills/risk-security-guard/SKILL.md`
10. `.claude/skills/risk-doc-truth-sync/SKILL.md`

Applied Skill Checklists must be reported: `risk-branch-governance`, `risk-security-guard`, `risk-doc-truth-sync`, `risk-status-sync`.

## 1. Goal

Close GOV-WP-02 by aligning the public repository's live `main` protection and security settings with the already proven checks `test` and `wpf-build`, without creating a single-account self-review deadlock.

## 2. Non-Negotiable Boundaries

- Product code, tests, release assets, tags, and `VERSION` are unchanged.
- External NuGet/API in product code, telemetry, auto-update, and auto-execution remain 0.
- No secret, credential, certificate, model, real data, or internal/NCR original is added.
- No direct push to `main`, force push, hard reset, branch deletion bypass, or destructive probe.
- Do not require one approval or Code Owner review while the author/merger is one GitHub account.
- Do not rename `test` or `wpf-build`; if live check names differ, stop and report the drift.
- This WP does not enable signing, NCR production packs, Local LLM, or Team Pilot.

## 3. Phase 1 - Read-Only Preflight

From a fresh `feature/gov-wp-02-branch-security-governance` branch based on live `origin/main`:

1. Read repository visibility, default branch, allowed merge methods, delete-on-merge, `main` protection, rulesets, and `security_and_analysis` through GitHub REST.
2. Read the latest merged PR and one current/most-recent PR exact-head check runs. Confirm both `test` and `wpf-build` completed with `success` and real runner steps.
3. Confirm `.github/workflows/ci.yml` still triggers on pull requests to `main` and the two job names remain stable.
4. Record a sanitized proposed settings diff in the PR description or a planning comment. Do not include tokens or response headers containing credentials.

## 4. STOP - Operator Approval Before Remote Mutation

Repository protection and security-setting PATCH calls are external, immediate changes. After Phase 1, stop once and request explicit operator approval for this exact target:

- Require pull request before merging: ON.
- Required approving reviews: 0.
- Code Owner review: OFF.
- Required status checks: `test`, `wpf-build`; strict/up-to-date ON.
- Conversation resolution: ON.
- Linear history: ON.
- Enforce for administrators: ON where the repository plan/API permits.
- Force pushes: OFF.
- Branch deletion: OFF.
- Secret scanning: ON.
- Push protection: ON when available for this public repository.

Do not mutate remote settings before that approval. If approval is withheld, leave the WP `APPROVAL_REQUIRED` and report the exact pending payload; do not improvise another path.

## 5. Phase 2 - Apply And Verify After Approval

1. Apply Phase A `main` protection using GitHub REST with the exact target above.
2. Enable secret scanning and push protection through the supported repository security settings API.
3. Re-read all changed settings from GitHub. Do not treat a successful PATCH response alone as evidence.
4. If an API/plan rejects a setting, preserve all successfully verified safe settings, mark only the rejected item `BLOCKED`, and report the status/error without bypassing it.
5. Do not test protection by pushing directly to `main` or force-pushing. API readback plus a subsequent ordinary PR is the safe verification path.

## 6. Repository Changes

Update only governance/truth documents needed to record live evidence, normally:

- `docs/32_Branch_Governance.md`
- `docs/53_Repository_Audit_and_v1_Execution_Plan.md`
- `docs/38_v1_Master_Roadmap.md`
- `docs/39_Work_Package_Backlog.md`
- `README.md`, `AGENTS.md`, `CLAUDE.md`, `SKILLS.md` only when current-state/NEXT-UP truth changes

Do not modify `.github/workflows/*` unless Phase 1 proves a factual drift that makes the named checks unavailable. Such drift is a STOP and requires a separately reviewed scope amendment.

## 7. Local And Hosted Gates

Run and report:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests\RiskManagementAI.SmokeTests\RiskManagementAI.SmokeTests.csproj -c Release
```

Required result: build 0 warnings / 0 errors; `Total=907 PASS=907 FAIL=0`; Unclassified 0; external `PackageReference` 0; Gate A 0. The GOV PR's hosted `test` and `wpf-build` must also succeed.

## 8. Completion Report

Report separately:

- exact branch/head/base SHA and changed files
- local build/SmokeTest/Gate A/PackageReference evidence
- hosted check run IDs and conclusions
- before/after branch protection values from REST
- before/after secret scanning and push protection values from REST
- any setting that remains `BLOCKED` and why
- single-account anti-deadlock confirmation (approvals 0, Code Owner OFF)
- next owner/action and truth-sync status
- Applied Skill Checklists

Do not self-merge. Claude/user review and an explicit merge instruction remain required.
