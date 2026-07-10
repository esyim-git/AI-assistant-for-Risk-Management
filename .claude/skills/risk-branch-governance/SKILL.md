---
name: risk-branch-governance
description: Review branch governance, PR flow, squash merge, soft guard, issue freeze, hard branch protection migration, and release tag policy.
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git status *) Bash(git log *)
---

# Branch Governance

## 목적
git 브랜치/PR/머지가 거버넌스(브랜치 명명·squash·`(#PR)`·금지조작·Local-Gate)를 따르도록 강제한다. 이 스킬은 점검(읽기) 전용이며 코드/히스토리를 직접 바꾸지 않는다.

## 언제 사용
- 새 작업 브랜치를 만들거나, PR을 올리거나, 머지 가부를 판단할 때.
- "브랜치 어떻게 따나", "PR 머지해도 되나", "squash", "force push 해도 되나", "main에 직접 올려도 되나" 같은 요청.
- 머지 게이트 직전 — 브랜치명·금지조작·증거(`Total=N PASS / 0 FAIL`)·`(#PR)` 규약을 한 번에 점검할 때.
- **Claude는 main을 직접 수정/병합하지 않는다**(`CLAUDE.md §11.1`). 계획 작업은 `planning/*`에서만.

## 절대 원칙
- **브랜치 명명**: Codex 작업 = `feature/<WP-ID>-*`, Claude 계획 = `planning/*`. `main`/`develop` **직접 push·직접 수정 금지** (`AGENTS.md §6`, `docs/32 §1`, `docs/35`).
- **금지 조작**: `--force`/`force push`, `reset --hard` **금지**. 작은 Diff 유지 (`CLAUDE.md §8`, `docs/32 §4`, `docs/35`).
- **머지 정책**: **Squash merge만** 허용(merge commit·rebase merge OFF). Commit Subject 끝에 **`(#<PR번호>)`** 포함(soft guard가 PR 표식을 확인) (`docs/32`, `docs/35`).
- **Local + Hosted Gate**: 로컬 `dotnet build` + SmokeTest **`Total=N PASS / 0 FAIL`** 증거 **＋** Claude 코드리뷰(Diff·보안·문서)는 항상 필요하다. 자동 PR CI 복원 후 `test`·`wpf-build`도 독립 증거이며, Phase A protection 적용 뒤에는 필수 check다 (`CLAUDE.md §11`, `AGENTS.md §7`).
- **과대표기 금지**: 증거 없는 PASS/VERIFIED를 적지 않는다. 상태 어휘만 사용(`CLAUDE.md §11.4`).

## 절차
1. **브랜치 확인**: `git branch --show-current`·`git status`로 현재 브랜치가 `feature/<WP-ID>-*`(Codex) 또는 `planning/*`(Claude 계획)인지, `main`/`develop` 위에서 직접 작업 중이 아닌지 확인한다.
2. **히스토리·조작 확인**: `git log --oneline origin/main..HEAD`로 커밋 규모(작은 Diff)와 Subject 규약을 본다. force push/hard reset 흔적·main 직접 변경 여부를 점검한다. 상세는 [branch-rules.md](branch-rules.md).
3. **머지 전 게이트**: `/risk-security-guard`(보안 축)와 `/risk-codex-review`(4축 리뷰) 통과 + 로컬 **`Total=N PASS / 0 FAIL`** + 활성화된 hosted checks를 확인한다. 하나라도 red/미실행이면 머지하지 않는다.
4. **Squash·(#PR) 규약**: 머지는 squash로, Commit Subject에 `(#<PR번호>)`가 들어가는지 확인한다(soft guard 신호). custom subject도 `(#N)` 필수.
5. **머지 후**: `/risk-doc-truth-sync`로 `docs/38·39·40·45` 정합을 맞추고, 병합된 feature 브랜치 삭제를 권장한다.

## 산출물/보고
- **거버넌스 점검표**: `브랜치명 / 금지조작(force·hard reset) / main 직접 변경 / Squash·(#PR) / Local-Gate 증거` 각각 `PASS` 또는 `위반`.
- 위반은 `항목 — 근거(브랜치명·커밋·증거 누락) — 정정 조치` 줄 목록.
- 머지 증거 줄: 인용된 로컬 **`Total=N PASS / 0 FAIL`** + Claude 리뷰 결과 + 활성 hosted `test`/`wpf-build` 결론.
- 최종 한 줄: **머지 가능(거버넌스 PASS)** 또는 **머지 불가(위반 N건)**. 증거 없는 PASS는 적지 않는다.

## 체크리스트
브랜치 명명·머지·금지조작·Local-Gate 머지 게이트 상세 규칙·점검 명령은 [branch-rules.md](branch-rules.md).

## 참조
- `docs/32_Branch_Governance.md`(public Phase A/B 보호·리뷰·머지) · `docs/35_Private_Free_Soft_Guard.md`(private-Free 시절 soft guard 역사·현재 백업 역할).
- `AGENTS.md §6`(Release/Branch·`feature/<WP-ID>-*`·작은 Diff) · `CLAUDE.md §8`(Git 원칙·force push/hard reset 금지) · `§11.1`(main 직접 수정 금지·`planning/*`) · `§11.6`(Local-Gate).
- 연계 스킬: `/risk-security-guard`(보안 축) · `/risk-codex-review`(머지 전 4축 리뷰) · `/risk-doc-truth-sync`(머지 후 문서 정합).
