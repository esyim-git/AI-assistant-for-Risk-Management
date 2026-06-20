# 32. Branch Governance (main 보호 · develop · 리뷰 규약)

## 목적

`main`을 항상 배포 가능(release-ready)한 상태로 유지하고, 모든 변경이 PR·CI·검토를 거치도록 거버넌스를 고정한다.
`docs/06_Development_Workflow.md`(브랜치 전략)와 `docs/29_GitHub_Sync_Guide.md`(동기화)를 **실제 설정값**으로 보강한다.

## 적용 범위

- GitHub 원격 저장소의 브랜치 모델, 보호 규칙, 리뷰(CODEOWNERS/PR 템플릿), 병합 정책.

## 제외 범위

- 운영환경(Prod)은 GitHub에 직접 접속하지 않는다(`docs/25`). 본 문서는 Dev 거버넌스에 한정.

---

## 1. 브랜치 모델 (docs/06 정합)

```text
main                      : 항상 배포 가능. PR로만 갱신. (보호 가능 시 ON, 아니면 soft guard)
develop                   : 통합 개발 브랜치. feature를 모아 검증 후 main으로.
feature/<name>            : 기능/구현 (예: feature/mvp2-llm-draft)
release/vX.Y.Z            : 배포 준비 (버전 고정·Release ZIP)
hotfix/<name>             : 긴급 수정
```

- 신규 작업은 `develop`(또는 최신 `main`)에서 분기 → PR → `develop` 병합 → 안정화 후 `main` 병합.
- `main`/`develop` 직접 push 금지. 보호 규칙이 사용 가능한 플랜에서는 GitHub가 강제하고, private Free 상태에서는 `docs/35` soft guard로 우회를 감지한다. 커밋 타입: `feat/fix/docs/chore/refactor/test/security`.

## 2. main 브랜치 보호 — 적용할 설정 (수동)

> 브랜치 보호는 저장소 **Settings**에서 직접 켠다(현재 자동화 도구 없음).
> 경로: GitHub repo → **Settings → Branches → Add branch ruleset (또는 Add rule)** → 대상 `main`.
> 현재 private repository + GitHub Free 상태에서는 GitHub가 branch protection/rulesets를 제공하지 않는다. `docs/35_Private_Free_Soft_Guard.md`의 soft guard를 적용하고, Pro/Team 업그레이드 또는 public 전환 시 아래 설정으로 즉시 전환한다.

권장 설정:

- [x] **Require a pull request before merging**
  - [x] Require approvals: 1
  - [x] Require review from Code Owners (CODEOWNERS 강제)
  - [x] Dismiss stale approvals on new commits
- [x] **Require status checks to pass before merging**
  - [x] Require branches to be up to date before merging
  - 필수 체크: **`build`** (워크플로 `ci.yml`의 job 이름)
- [x] **Require conversation resolution before merging** (리뷰 스레드 resolve 강제)
- [x] **Do not allow bypassing the above settings** (관리자 포함)
- [ ] (선택) Require linear history → squash 병합과 함께 권장
- 금지: **Allow force pushes = OFF**, **Allow deletions = OFF**

`develop`에도 동일 규칙(승인 1, status check `build`)을 권장하되, 운영 속도를 위해 "Code Owners 강제"는 선택.

## 2.1 private Free 임시 운영

- Repository는 private 유지.
- GitHub branch protection API 적용 결과: `403 Upgrade to GitHub Pro or make this repository public to enable this feature.`
- 강제 보호 대신 repository merge setting을 squash only로 제한하고, `.github/workflows/governance-soft-guard.yml`로 `main` 직접 push 우회를 감지한다.
- soft guard는 push 자체를 막지 못한다. 실패한 workflow를 신호로 보고 즉시 정정한다.

## 3. 리뷰 규약

- **CODEOWNERS** (`.github/CODEOWNERS`): 기본 오너 `@esyim-git`. `rules/`·`config/`·`kb/`·`build/`·`deploy/`·보안문서는 신중 검토.
- **PR 템플릿** (`.github/pull_request_template.md`): 보안 게이트 A(docs/28) + 절대원칙(CLAUDE.md §3) 체크리스트 내장.
- PR은 **CI `build` green + 리뷰 스레드 resolve** 후 병합.

## 4. 병합 정책

- 기본 **Squash merge**(잘게 나뉜 커밋을 한 줄로) — MVP-1 PR #1과 동일.
- private Free soft guard 상태에서는 repository 설정으로 merge commit/rebase merge를 비활성화하고 squash merge만 허용한다. `--force`/`reset --hard`는 금지(docs/29).
- private Free soft guard는 main push head commit message의 GitHub PR 표식(`(#<PR번호>)` 또는 `Merge pull request #...`)을 확인한다. 따라서 `gh pr merge --squash` 사용 시 기본 subject를 유지하거나, custom subject를 쓰더라도 반드시 `(#<PR번호>)`를 포함한다.
- 병합 후 feature 브랜치 삭제 권장.

## 5. 적용 절차 (한 번)

1. (완료) `develop` 브랜치 생성 — `main`에서 분기.
2. (이 PR) `.github/CODEOWNERS`, `.github/pull_request_template.md`, 본 문서 추가.
3. (현재) private Free 제약으로 §2 강제 보호는 미적용, `docs/35` soft guard 적용.
4. Pro/Team 업그레이드 또는 public 전환 시 §2 설정을 `main`(필요 시 `develop`)에 적용.
5. 이후 모든 변경은 PR 경유.

## 테스트 / 확인

- 보호 적용 후, `main`에 직접 push 시도가 거부되는지 확인. private Free soft guard 상태에서는 `main` 우회 push가 `governance-soft-guard` 실패로 감지되는지 확인.
- PR 생성 시 PR 템플릿이 자동 채워지는지, `build` 체크가 필수로 뜨는지 확인.

## 6. Hard Branch Protection Migration (Pro/Team 업그레이드 또는 public 전환 시)

> 현재는 private Free → **Soft Guard 유지**(§2.1): PR 필수·CI 필수·Squash only·main 직접 push 금지·force push 금지·commit subject `(#N)` 규칙·Release Tag 규칙. 아래는 플랜 업그레이드 시 즉시 적용할 전환 체크리스트.

- [ ] **Hard Branch Protection** 활성화(§2 설정값 그대로): `main`(+`develop`)
- [ ] **Required PR review**(승인 1) + **Require review from Code Owners**(CODEOWNERS)
- [ ] **Required status check** = `build`(ci.yml) + (선택) `main-soft-guard`
- [ ] **Restrict who can push**(직접 push 차단) + **force push OFF** + **deletion OFF**
- [ ] **Require conversation resolution** + **Dismiss stale approvals**
- [ ] (검토) **Signed commit** 요구 여부
- [ ] **Release Approval**: Release/태그 생성 권한 제한, Release 발행 전 승인자 확인
- [ ] 전환 후 검증: `main` 직접 push 시도 **거부**되는지, PR 미승인/CI 미통과 머지 **차단**되는지
- [ ] 전환 완료 시 `docs/35` Soft Guard는 **백업 신호**로 유지(중복 무해)

## 향후 확장

- secret-scan / 금지확장자 검사 CI job 추가(docs/28 향후확장), Dependabot/Release Drafter 등.

> 관련 문서: `docs/06_Development_Workflow.md`, `docs/29_GitHub_Sync_Guide.md`, `docs/28_Security_Review_Checklist.md`
