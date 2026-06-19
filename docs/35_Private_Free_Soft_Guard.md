# 35. Private Free Soft Guard

## 목적

GitHub private repository에서 branch protection/rulesets를 사용할 수 없는 플랜 상태를 전제로, `main` 직접 변경 위험을 낮추는 soft guard를 정의한다.

## 현재 제약

- Repository visibility: private
- GitHub API branch protection 적용 시도 결과: `403 Upgrade to GitHub Pro or make this repository public to enable this feature.`
- 따라서 `main` 보호 규칙(PR 필수, 승인 1, Code Owners, status check `build`, conversation resolution, force-push 차단)은 현재 GitHub가 강제하지 못한다.

## 적용한 soft guard

1. Repository merge setting
   - Squash merge: ON
   - Merge commit: OFF
   - Rebase merge: OFF
   - Delete branch on merge: ON
   - Visibility: private 유지

2. Review convention
   - `.github/CODEOWNERS`
   - `.github/pull_request_template.md`
   - `docs/28` Gate A
   - PR + CI + review thread resolution을 운영 규칙으로 유지

3. Advisory workflow
   - `.github/workflows/governance-soft-guard.yml`
   - `main` push head commit message가 GitHub PR merge 형식이 아니면 workflow를 실패 처리한다.
   - 보호 규칙이 아니므로 push 자체를 차단하지는 못한다. 실패 신호를 통해 우회를 감지한다.

## 운영 규칙

- `main` 직접 push 금지.
- 모든 변경은 `feature/`, `chore/`, `release/`, `hotfix/` 브랜치에서 PR로 올린다.
- PR merge는 squash merge만 사용한다.
- `main` push 후 `governance-soft-guard`가 실패하면 즉시 원인을 확인하고, 필요한 경우 새 PR로 정정한다.
- force push와 hard reset은 계속 금지한다.

## 강제 보호로 전환하는 조건

다음 중 하나가 되면 `docs/32`의 branch protection 설정을 즉시 적용한다.

- GitHub Pro/Team 이상으로 업그레이드
- repository를 public으로 전환

적용 대상:

- Require PR before merging
- Require approvals: 1
- Require Code Owners
- Dismiss stale approvals
- Require status check `build`
- Require conversation resolution
- Do not allow bypassing
- Allow force pushes OFF
- Allow deletions OFF

## 테스트 기준

- `gh repo view`로 squash only 설정 확인.
- `governance-soft-guard` workflow가 `main` push에서 실행되는지 확인.
- PR은 기존 `ci` workflow의 `build` job green 후 병합한다.

> 관련 문서: `docs/32_Branch_Governance.md`, `docs/28_Security_Review_Checklist.md`, `docs/29_GitHub_Sync_Guide.md`
