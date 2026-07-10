---
name: risk-codex-review
description: Review Codex PR/diff against a Work Package, checking scope, security, tests, docs, gates, and no-regression rules. Auto-applied first in Claude's PR-review Preflight (CLAUDE.md §13).
argument-hint: "[PR-number or WP-ID]"
arguments: [target]
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Bash(git status:*), Bash(git diff:*), Bash(dotnet build:*), Bash(dotnet run:*)
---

# Codex Result Review

## 목적
`feature/<WP-ID>-*` 구현 PR 또는 `planning/*` truth-sync PR을 머지 전에 **Diff·보안·테스트·문서 4축**으로 검토하고, 승인 또는 항목별 수정요청을 낸다. 이 스킬은 검토(읽기) 전용이며 코드 동작을 바꾸지 않는다.

## 언제 사용
- Codex가 WP 1개 구현을 보고하고 **머지 승인을 요청**할 때.
- "Codex 결과 검토", "PR 리뷰", "브랜치 머지해도 되나", "Diff 확인", "Gate A 통과했나" 같은 요청.
- 머지 게이트 판단 직전(로컬 `dotnet build` + SmokeTest `Total=N PASS/0 FAIL` 증거 + Claude 코드리뷰)(`CLAUDE.md §11.6`).
- 검토는 읽기 전용으로 수행한다. **Claude는 main을 직접 수정/병합하지 않는다**(`CLAUDE.md §11.1`).

## 절대 원칙
- **충돌 우선순위**: `AGENTS.md` > 지정 WP(`docs/39`) > Codex Prompt. 검토 기준은 항상 이 순서로 본다.
- **외부 NuGet PackageReference = 0** · 외부 API 0 · Telemetry 0 · 자동 업데이트 0. Diff에 하나라도 추가되면 **수정요청(머지 불가)** (`AGENTS.md §3·§4`, STOP 규칙).
- **민감정보 0**: 실데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret/토큰/접속문자열·모델파일이 Diff에 들어오면 즉시 머지 불가. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 같은 일반명만 허용(`docs/28` 게이트 A).
- **기존 테스트 삭제·약화 금지**: SmokeTest 총수 감소 시 사유·매핑 없으면 수정요청. WP별 양성/음성 회귀가 추가됐는지 확인(`AGENTS.md §3·§5`).
- **과대표기 금지**: 보고/문서 상태는 정본 어휘만(`VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED`). **실 오프라인 Test PC 증거 없으면 Gate PASS로 적지 않는다**(`CLAUDE.md §11.4`).

## 절차
1. **변경 범위 확인**: `git log origin/main..<branch>` 와 `git diff --stat origin/main..<branch>` 로 커밋·변경 파일·라인 규모를 파악한다(작은 Diff 원칙).
2. **Diff 축**: `git diff origin/main..<branch>` 로 변경이 **지정 WP 범위(`docs/39`) 안**에 있는지, 범위 외 변경·기능 회귀가 없는지 확인한다. Public Interface·쓰기 경로(`logs/`·`reports/`·`config/`)·경로 가드 준수 확인.
3. **보안 축(Gate A)**: `docs/28` 게이트 A 항목 + NuGet 0·외부 API 0·secret/실데이터/원문 0·금지 확장자 부재를 Diff에 대해 점검한다. 상세는 [review-dimensions.md](review-dimensions.md). `/risk-security-guard` 스킬과 동일 기준.
4. **테스트 축**: SmokeTest **이전 Total 보존 + 신규 회귀 추가** 여부, 단언 약화/삭제 0, 보고에 `Total=N PASS / 0 FAIL` 합계 줄이 포함됐는지 확인. 총수 감소면 사유·매핑 필수.
5. **문서 축**: 구현 PR은 해당 WP의 **Claude Review Checklist**(`docs/39`), planning PR은 선언한 audit/truth-sync scope를 기준으로 `docs/38·40·48` 정합과 상태 어휘를 확인한다. 문서 정합 정정은 `/risk-doc-truth-sync`로 이어간다.
6. **Hosted 축**: workflow가 활성화된 PR은 exact head의 `test`·`wpf-build` 결론과 annotation을 확인한다. queued/skipped/not-run/red는 success가 아니다.
7. **판정 정리**: 4축 각각 PASS / 수정요청을 항목별 근거와 함께 정리하고 머지 가부를 낸다.

## 산출물/보고
- **4축 판정표**: `Diff / 보안(Gate A) / 테스트 / 문서` 각각 `PASS` 또는 `수정요청` + 근거.
- 테스트 근거는 보고에 인용된 **`Total=N PASS / 0 FAIL`** 합계 줄(이전 Total 대비 증감 포함).
- 수정요청은 `항목 — 위치(파일:라인) — 사유 — 기대` 형태의 줄 목록.
- 최종 한 줄: **머지 가능** 또는 **머지 불가(수정요청 N건)**. 증거 없는 PASS/VERIFIED는 적지 않는다.

## 체크리스트
4축(Diff/보안/테스트/문서) 상세 점검 항목·명령·판정 기준은 [review-dimensions.md](review-dimensions.md).

## 참조
- `AGENTS.md`(구현 표준·보고·우선순위) · `docs/28`(보안 게이트 A) · `docs/39`(해당 WP Review Checklist) · `docs/38`(Roadmap·Traceability) · `docs/40`(ADR) · `docs/54`(현재 Gate 증거·런북) · `docs/48/45/44`(historical Gate 증거).
- `CLAUDE.md §11.1`(main 직접 수정 금지) · `§11.3`(리뷰 루프) · `§11.4`(상태 어휘·과대표기 금지) · `§11.6`(Local-Gate).
- 연계 스킬: `/risk-security-guard`(보안 축 단독) · `/risk-doc-truth-sync`(문서 정합 후속) · `/risk-branch-governance`(머지·브랜치 규약).
