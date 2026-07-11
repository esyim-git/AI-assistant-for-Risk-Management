# 29. GitHub Sync Guide

## 목적

이 프로젝트를 GitHub와 안전하게 동기화하는 절차를 정의한다.
`docs/06_Development_Workflow.md`(브랜치 전략)와 `docs/24_Release_Packaging_Guide.md`(Release 첨부)를 보완하여, **clone → 작업 → commit → push**와 **Dev→Test→Prod 산출물 이동**의 경계를 명확히 한다.

## 적용 범위

- Dev 환경(개발 PC)에서의 Git 작업.

## 제외 범위

- 운영환경(Prod)은 GitHub에 **직접 접속하지 않는다.** Prod로는 오직 portable Release ZIP만 반입된다(`docs/25`).

## Repository

```
https://github.com/esyim-git/AI-assistant-for-Risk-Management
```

## 핵심 안전 원칙

- `git push --force` **금지**, `git reset --hard` **금지**
- `origin`이 위 URL이 아니면 **중단하고 사용자에게 보고**(절대 덮어쓰지 않음)
- 원격에 기존 commit/README/branch가 있으면 `main` 직접 push 금지 → **branch로 push**
- 현재 repository는 public이며 `docs/32` Phase A hard protection이 적용돼 있다. `docs/35` soft guard는 post-merge 백업 신호로 유지한다. main 직접 push·force push·보호 우회는 금지한다.
- 충돌 발생 시 자동 병합하지 말고 보고
- 커밋 전 반드시 `docs/28_Security_Review_Checklist.md` 게이트 A 수행

## 최초 동기화 절차

```powershell
# 1) 올바른 프로젝트 루트인지 확인
git status              # repo가 아니면 git init
git remote -v

# 2) origin 등록 (없을 때만)
git remote add origin https://github.com/esyim-git/AI-assistant-for-Risk-Management.git
# origin이 다른 URL이면 -> 중단, 보고

# 3) 원격 상태 확인
git ls-remote origin    # 비어 있으면 main 최초 push 허용, 아니면 branch 사용

# 4) 보안 게이트 (docs/28 게이트 A)
git add -A --dry-run

# 5) 커밋
git add -A
git commit -m "chore: bootstrap environment split starter v2"
```

## Push 정책

### 원격이 완전히 비어 있는 경우에만

```powershell
git branch -M main
git push -u origin main
```

### 원격에 기존 내용이 있는 경우 (권장 기본값)

```powershell
git fetch origin
git switch -c bootstrap/envsplit-starter-v2
git push -u origin bootstrap/envsplit-starter-v2
# 이후 GitHub에서 Pull Request로 검토 후 main 병합
```

## 일상 작업 흐름

```powershell
git switch -c feature/<작업명>
# ... 작업 ...
# docs/28 게이트 A 수행
git add -A
git commit -m "<type>: <요약>"     # type: feat/fix/docs/chore/refactor/test/security
git push -u origin feature/<작업명>
# PR 생성 -> 리뷰 -> main 병합
```

## 산출물 이동 경계

```text
Dev  : GitHub clone/push, 소스/문서/더미데이터/룰/템플릿
  │   (소스는 Dev↔Test까지만)
Test : Release ZIP 검증 (소스 가능, 실데이터 금지)
  │   (Prod에는 ZIP만, GitHub 직접접속 없음)
Prod : portable Release ZIP만 반입 -> 해시검증 -> 실행
```

GitHub Release에는 **source가 아니라 portable Release ZIP**을 첨부한다(`docs/24`).

## 인증 실패 시

- push를 반복 재시도하지 않는다.
- 실패 원인(인증/권한/네트워크)을 보고하고, 사용자가 직접 실행할 명령을 안내한다.

```powershell
# 예: PAT 또는 gh auth login 후
gh auth status
git push -u origin bootstrap/envsplit-starter-v2
```

## Push 후 보고 항목

- push 성공 여부 / remote URL / branch명 / commit hash
- GitHub 확인 URL
- main 병합 또는 PR 생성 필요 여부

## 테스트 방법

- `git remote -v`로 origin URL 일치 확인.
- `git ls-remote origin`으로 원격 상태(비었는지/기존 commit) 확인 후 분기 결정.

## 향후 확장

- Public 전환, PR CI(`test`, `wpf-build`) 복원, `docs/32` Phase A main protection, secret scanning/push protection 활성화는 2026-07-11 REST readback으로 `VERIFIED`다. 다음 governance 확장은 독립 reviewer가 생긴 뒤 승인 1/Code Owner review를 켜는 Phase B이며, 그전에는 self-review 교착을 피하도록 approvals 0/Code Owner OFF를 유지한다.

> 관련 문서: `docs/06_Development_Workflow.md`, `docs/24_Release_Packaging_Guide.md`, `docs/28_Security_Review_Checklist.md`, `docs/35_Private_Free_Soft_Guard.md`
