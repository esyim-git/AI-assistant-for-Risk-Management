# Codex Prompt — UX-WP-10: Smart Assist 정적 completion seed 라이브러리 확장 (SQL·VBA·RiskPhrase, 큐레이션)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-10) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-10-completion-seed-expansion` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `CLAUDE.md §4·§5·§6`(SQL/VBA/Excel 원칙), `docs/39` UX-WP-10, `SKILLS.md`+`risk-ui-ux-review`·`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Assist/Providers/StaticCompletionProviders.cs`, `src/RiskManagementAI.Core/Safety/{SqlSafetyChecker,VbaSafetyChecker}.cs`, `tests/RiskManagementAI.SmokeTests/AssistTests.cs`.
> **기준선**: main `0f6e1d7`(VERSION 0.7.0, UX-WP-09 #113 머지 후), 정본 SmokeTest `Total=834 PASS=834 FAIL=0`.

## 0. 목표 (단일)
`SqlCompletionProvider`·`VbaCompletionProvider`·`RiskPhraseProvider`의 **정적 seed 라이브러리를 큐레이션 확장**한다(리스크관리 실무 유용 패턴). **전부 정적·NoModel·인박스·자동삽입 0** 유지. **엔진/트리거/팝업/audit·provider 구조 변경 0** — `Seeds` 배열 추가만.

> **핵심 안전 불변식**: seed는 `Insertable=true`(사용자 선택 시 삽입)이고 엔진이 seed 콘텐츠를 재검사하지 않으므로, **모든 신규 SQL seed는 조회 전용(SELECT)·차단 키워드 0**, **모든 신규 VBA seed는 `Option Explicit`+에러처리+금지 API 0**이어야 한다. 이를 **테스트로 강제**(아래 §4).

## 1. 작업 범위 (StaticCompletionProviders.cs — Seeds 확장만)
1. **SQL seeds**(`SqlCompletionProvider.Seeds`) 큐레이션 추가 — 조회 전용·placeholder(`<TABLE_NAME>`/`BASE_DT`/`PORTFOLIO_ID`/`RISK_FACTOR`/`EXPOSURE_AMT` 등, **실 테이블·컬럼명 0**). 후보: 한도 사용률 계산(SELECT), 전일대비 조인(SELECT), 집중도/TopN(SELECT + `ORDER BY`), 대사용 합계 비교(SELECT), NULL/중복 점검(SELECT). §4 조회 전용 원칙 준수(INSERT/UPDATE/DELETE/DDL/GRANT/EXEC/COMMIT 0).
2. **VBA seeds**(`VbaCompletionProvider.Seeds`) 큐레이션 추가 — Excel 2021·`Option Explicit`·에러처리·원본보호·Application 상태 원복·배열 처리 우선(§5). 금지(Shell/WScript/Kill/FSO 삭제/Declare PtrSafe/WinAPI/Outlook/외부 URL) 0. 후보: 안전 범위 읽기→배열, 워크시트 안전 쓰기(상태 원복), 조건부 서식 값 계산(보조열), 결과 시트 생성(삭제 없음).
3. **RiskPhrase seeds**(`RiskPhraseProvider.Seeds`) 큐레이션 추가 — 검토용 초안 문구(한도초과 후속·집중도·데이터 품질·준법 확인 등). 실데이터·단정적 법규해석 표현 금지(항상 "검토용 초안").
4. **큐레이션 원칙**: 양보다 질 — 중복·자명한 항목 남발 금지. `SortKey`는 기존 체계와 충돌 없이 부여. Excel 허용 함수(`Excel2021CompletionProvider`)는 본 WP 밖(별도 rules 파일 = UX-WP-11).

## 2. 제외 범위
엔진/트리거/팝업/audit·provider 구조·dedupe·severity 필터 변경. Excel 함수 카탈로그(UX-WP-11). 신규 provider·언어. 룰/무결성/패키징. 신규 NuGet. LLM/랭킹(R4).

## 3. 보안조건
정적·NoModel·자동삽입 0·accept audit 불변 · **SQL seed 조회 전용·VBA seed 안전 패턴(테스트 강제)** · 실 테이블·컬럼명/실데이터/원문 0(placeholder만) · 단정적 법규해석 문구 0(검토용 초안) · NuGet 0.

## 4. 테스트 (SmokeTest — 도메인 `Assist`)
> `SmokeTestContext.SmokeDomain` line ~60(`completion`/`smart assist`/`suggestion`/`provider`/`popup`/`assist`). 신규 단언 설명에 이 토큰, Kb 트리거(`검색`/`원문`/`공개`/`인용`·`document`·`source`·`approval`) 회피. `Unclassified=0`.
- **안전 불변식 강제(필수)**: 신규(가능하면 전체) **SQL seed의 `InsertText`를 `SqlSafetyChecker`로 검사 → Blocker 0**, **VBA seed를 `VbaSafetyChecker`로 검사 → Blocker/High 0** 단언(자동 회귀 — 향후 unsafe seed 유입 차단).
- prefix 매칭·결정적 정렬·개수 상한(엔진 cap) 회귀. RiskPhrase는 "검토용 초안" 성격 유지.
- 기존 `AssistTests`(엔진 결정성·언어 라우팅·SafetyHint pinned·dedupe·UX-WP-07/09) **전부 보존**. 종료부 **`Total=834 → 834+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Assist·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 seed 목록(언어별 개수) · **seed 안전 검사 단언 결과** · **Applied Skill Checklists**.
- Branch `feature/ux-wp-10-completion-seed-expansion` · Commit: `feat: curate and expand static SQL/VBA/risk-phrase completion seeds (UX-WP-10)`

## 6. Claude Review Checklist
seed 확장만(엔진/provider 구조 불변) / **SQL seed 조회 전용·VBA seed 안전 패턴 — checker 단언으로 강제** / placeholder만(실 테이블·컬럼·데이터 0) / RiskPhrase 검토용 초안(단정 법규해석 0) / 큐레이션(중복 남발 아님) / 정적·NoModel·자동삽입·audit 불변 / 도메인 Assist·Unclassified 0 / 기존 AssistTests 보존 / NuGet 0 / `Total` 834 보존+신규 / Gate A.
