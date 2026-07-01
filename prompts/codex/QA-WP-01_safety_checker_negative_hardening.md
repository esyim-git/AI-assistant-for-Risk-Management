# Codex Prompt — QA-WP-01: Safety Checker 음성/경계 SmokeTest 하드닝 (SQL·VBA·Excel2021)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-01) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-01-safety-negative-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-01, `SKILLS.md`+`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Safety/{SqlSafetyChecker,VbaSafetyChecker,Excel2021FunctionChecker,SafetyRuleSet,RuleLoader}.cs`, `rules/*`, `tests/RiskManagementAI.SmokeTests/SafetyTests.cs`.
> **기준선**: main `10030be`(VERSION 0.7.0, #108~#111 머지 후), 정본 SmokeTest `Total=829 PASS=829 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
기존 SQL/VBA/Excel2021 Safety Checker의 **음성·경계 케이스 SmokeTest 커버리지만 확대**한다. **제품 코드·룰·checker 로직 변경 0** — 테스트만 추가. 실제 커버리지 공백(현 `SafetyTests`가 단언하지 않는 차단 패턴·경계)을 찾아 회귀로 고정한다.

## 1. 작업 범위 (SafetyTests.cs — additive only)
1. 먼저 `Sql/Vba` SafetyChecker + `Excel2021FunctionChecker` + `rules/*`(차단 목록)을 읽어 **현 `SafetyTests`가 이미 단언하는 것과 안 하는 것**을 대조한다. 중복 단언 추가 금지 — **미커버 케이스만**.
2. 추가할 음성/경계(차단·경고가 나와야 하는 입력) 후보:
   - SQL: `INSERT/UPDATE/DELETE/MERGE/CREATE/ALTER/DROP/TRUNCATE/GRANT/REVOKE/EXEC/CALL/COMMIT/ROLLBACK` 각 미커버 키워드 · 대소문자 혼합 · 주석 뒤 은닉(`--`, `/* */`) · 세미콜론 다중문 · 선행 공백/개행. (조회 전용 원칙 §4)
   - VBA: `Shell`·`WScript.Shell`·`Kill`·`FileSystemObject` 삭제/이동·`Declare PtrSafe`·WinAPI·Outlook 자동발송·외부 URL — 미커버 금지 API. `Option Explicit` 누락 경고.
   - Excel 2021: 차단 함수(`VSTACK/HSTACK/TOCOL/TOROW/TAKE/DROP/CHOOSECOLS/TEXTSPLIT/TEXTBEFORE/TEXTAFTER/GROUPBY/PIVOTBY/MAP/REDUCE/BYROW/BYCOL/REGEX*`) 각 미커버 · 대소문자 · 인자 안 은닉.
3. **양성 보존 케이스**도 필요 시 추가(안전한 SELECT·안전한 VBA·2021 허용 함수 → finding 0/차단 0)로 false-positive 회귀 방지.
4. **경계**: 빈 입력·공백·매우 긴 입력 등 checker가 graceful해야 하는 경계.

## 2. 제외 범위
checker/룰/제품 코드 변경. 신규 차단 규칙 추가(룰 변경은 별도 WP). 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
차단 대상이 실제로 차단(Blocker/High)됨을 단언 · 실데이터/실 테이블·컬럼명 0(placeholder만) · 원문 0 · NuGet 0 · **기존 테스트 삭제·약화 0**.

## 4. 테스트 (SmokeTest — 도메인 `Safety`)
> `SmokeTestContext.SmokeDomain`에서 Safety는 하단(line ~65: `RuleLoader`/`RuleSet`/`SELECT`/`VBA`/`Option Explicit`/`Excel 2021`/`checker`/`finding`/`DEMO_ONLY`). 신규 단언 설명은 **`checker`/`SELECT`/`VBA`/`Option Explicit`/`Excel 2021`/`finding`** 토큰으로 Safety에 걸리게 하되, 더 위 도메인 트리거(`limit`/`exposure`/`RECON`/`mapping`/`CP949`/`report `/`Kb`/`completion`) **회피**. `Unclassified=0`.
- 각 미커버 차단 패턴 → Blocker/High finding 단언(양성 차단). 안전 입력 → finding 0. 경계 graceful.
- 기존 `SafetyTests` 단언 **전부 보존**(수 감소 0). 종료부 **`Total=829 → 829+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Safety 도메인 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록(커버 공백 근거) · **Applied Skill Checklists**.
- Branch `feature/qa-wp-01-safety-negative-hardening` · Commit: `test: harden safety checker negative/boundary coverage (QA-WP-01)`

## 6. Claude Review Checklist
제품 코드/룰/checker 변경 0(테스트만) / 추가는 실제 미커버 케이스(중복 아님) / 차단 대상 Blocker·High 단언 · 안전 입력 finding 0 / 실데이터·원문 0 / 도메인 Safety·Unclassified 0 / 기존 단언 보존·감소 0 / NuGet 0 / `Total` 829 보존+신규 / Gate A.
