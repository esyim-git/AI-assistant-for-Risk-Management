# Codex Prompt — UX-WP-04: Excel Function Helper (검색·상세·예시·Excel 2021 대체식 — 정적·NoModel)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-04) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-04-excel-function-helper`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3·§4·§8`, `SKILLS.md`, `.claude/skills/risk-ui-ux-review/SKILL.md`·`.claude/skills/risk-security-guard/SKILL.md`·`.claude/skills/risk-smoke-governance/SKILL.md`·`.claude/skills/risk-data-limit-review/SKILL.md`, `docs/39` UX-WP-04, `docs/48 §B′`(Gate B B-5), `CLAUDE.md §6`(Excel 함수 제한 정본), `src/RiskManagementAI.Core/Excel/Excel2021FunctionChecker.cs`, `src/RiskManagementAI.Core/Safety/RuleLoader.cs`(+`SafetyRuleSet`), `src/RiskManagementAI.Core/Assist/*`(표면화 참고), `tests/RiskManagementAI.SmokeTests/`.
> **기준선**: main `3e82cc0`(VERSION 0.7.0), 정본 SmokeTest `Total=747 PASS=747 FAIL=0`.
> **⚠️ ID 주의**: 완료·머지된 UX-WP-01/02/03(#70/#72/#73)과 충돌 방지를 위해 본 WP는 **UX-WP-04**다(사용자 제안 "UX-WP-01 Excel Function Helper").

## 0. 목표 (단일)
Gate B(2026-06-30) B-5에서 "Excel 검사가 단순 함수 차단 수준"으로 확인됨 → **함수 차단을 넘어 Helper로 확장**한다. Excel 2021 함수 **검색 + 상세설명 + 인수 설명 + 리스크관리 예시 + 수식 예시 + Microsoft 365 전용 여부 + Excel 2021 대체식 + 사용 가능 함수 추천**을 제공한다. **수식 삽입은 사용자 명시 선택 시에만**(자동삽입 0). **정적·NoModel·외부 API 0·NuGet 0.**

> **STOP**: 외부 NuGet·함수 DB/API·Vector·LLM·모델파일 0. 실시간 LLM 추천/랭킹은 R4(미구현). 필요해지면 즉시 STOP → 승인 문서(`docs/41`·`docs/40`).

## 1. 작업 범위
1. **함수 메타 단일원천 = 기존 룰셋 재사용(중복정의 금지)**: 차단/365-전용/권장 대체 목록은 **기존 `SafetyRuleSet.ExcelBlockedFunctions`·`ExcelPreferredFunctions`(`RuleLoader.LoadDefault()`)를 그대로 재사용**한다. 함수 차단 목록을 Helper에 **재선언하지 않는다**(단일원천). `Excel2021FunctionChecker`의 판정(`EXCEL_365_FUNCTION`)과 일관.
2. **정적 helper 카탈로그 = Core embedded resource(항상 동봉)**: 함수별 **설명·인수 설명·리스크관리 예시·수식 예시·Excel 2021 대체식**을 담는 데이터를 **Core 임베디드 리소스**(예: `Core/Excel/Resources/excel_function_help.json`, csproj `<EmbeddedResource>`; 기존 `Data/Resources/cp949-uhc-map.txt` 동형)로 둔다 — **컴파일-인 항상 존재·결정적**. ⚠️ **`rules/`(및 `templates/`·`kb/`·`config/ncr` 등 critical glob) 아래 '옵션 파일 + 누락 시 빈 helper safe-fallback' 금지**: `rules/*`는 `IntegrityVerifier.RequiredCriticalEntries`/critical glob라 **파일 누락 = 런타임 fail-closed**(빈 helper fallback 성립 안 함). 데이터 = **공개 함수 일반지식 + 더미 예시만**(실 업무데이터/실 테이블·컬럼/실 수식 0). **fallback은 '파일 부재'가 아니라 '리소스 파싱 실패'에만**(빈 helper + warning, 예외 0). loose 파일이 굳이 필요하면 critical 무결성 모델(**항상 존재 + 4중 미러**: `RequiredCriticalEntries`·build/01 manifest·build/03·manifest test)을 따르고 silent fallback 금지 — 이는 별도 **STOP/승인** 검토(본 WP 기본=embedded).
3. **신규 `ExcelFunctionHelper`(Core) + `ExcelFunctionInfo`(record)**:
   - `ExcelFunctionInfo`(`Name`·`Description`·`Args`·`RiskMgmtExample`·`FormulaExample`·`Is365Only`·`Excel2021Alternative`·`Recommended`).
   - `ExcelFunctionHelper.Lookup(string name)` → 단건, `Search(string query)` → 부분일치 결정적 목록(`OrderBy(Name, StringComparer.Ordinal)` 또는 점수 후 Ordinal tie-break). `Is365Only`는 §1.1 룰셋 기준 파생(별도 하드코딩 금지).
4. **사용 가능 함수 추천**: 차단/365 함수 조회 시 **대체식·대체 함수**(`ExcelPreferredFunctions` 기반) 추천 제공.
5. **수식 삽입 = 사용자 명시 선택 시에만**: Helper는 기본 **읽기전용 조회**. 삽입 API는 caller가 사용자 액션으로만 호출(자동삽입/자동수정 0).
6. **입력 원문 로그 미저장**: 검색어/조회어 **평문 미기록**. 감사가 필요하면 기존 해시 패턴(`LogHash.Sha256Hex`)으로만. 원문·수식 평문 디스크 미저장.
7. **WPF 표면화(필수 — B-5 갭 종료 조건)**: **읽기전용 helper view를 실행 Release UI에 배선(필수)**. Gate B B-5는 **UI 가시성 갭**이므로 Core API만으로는 종료되지 않는다 — Core-only 산출을 'B-5 종료'로 표기 금지. 기존 패널/Assist 표면 재사용, 수식 삽입은 사용자 선택 시만(자동삽입 0). **UI 렌더 자체 검증 = Gate B(Test PC)** 의존(SmokeTest는 ViewModel/조회 로직까지). Core와 UI를 부득이 분리해야 하면 **UI를 별도 후속 WP로 명시 분리**하되 **B-5 종료는 그 UI WP에 귀속**한다.

## 2. 제외 범위
실시간 LLM 추천/랭킹(R4). Smart Assist 입력중 추천(UX-WP-05/06). 함수 **자동실행/계산**. 실 수식/실데이터 적재. `SafetyRuleSet` 차단목록 변경(재사용만). Vector/모델/NuGet. 외부 함수 DB/API.

## 3. 보안조건 (risk-security-guard · risk-ui-ux-review · risk-data-limit-review)
- 외부 API 0 · 외부 NuGet 0 · 함수 자동실행 0 · **자동삽입 0**(수식 삽입=사용자 선택만).
- **입력 원문(검색어) 로그 미저장** — 평문 미기록, 해시 전용 audit만(필요 시).
- 데이터 파일 = **공개 함수 일반지식 + 더미 예시만**, 실 업무데이터/실 테이블·컬럼/실 수식 0.
- `CLAUDE.md §6`(기본 금지: VSTACK/HSTACK/TEXTSPLIT/GROUPBY/MAP/REDUCE/REGEX 계열 등 · 대체 우선: XLOOKUP/FILTER/SORT/INDEX-MATCH/PivotTable/보조열/SQL/VBA)와 **정합**. 금지함수는 helper에서 "Excel 2021 비호환 + 대체식"으로 안내(권장이 아니라 대체 유도).
- 쓰기 경로 없음(조회 전용). 룰셋·데이터 파일은 읽기 전용.

## 4. 테스트 (SmokeTest, 외부 프레임워크 0)
- **양성**: 합성/공개 함수 조회 → `ExcelFunctionInfo` 완비(설명·인수·리스크예시·수식예시·`Is365Only`·`Excel2021Alternative`) · `Search` 부분일치 결정적 정렬 · 권장 대체 함수(`SUMIFS`/`INDEX`/`XLOOKUP` 등) 정상 조회.
- **음성/경계**: 365-전용 함수(예: `VSTACK`/`TEXTSPLIT`) → `Is365Only=true` + Excel 2021 대체식 추천(차단목록은 §1.1 룰셋 단일원천에서 파생, helper 재선언 0 회귀) · embedded 리소스 **파싱 실패** → safe-fallback(빈 helper + warning, 예외 0; '파일 부재' 케이스는 critical glob 미사용·embedded라 해당 없음) · 삽입 API는 명시 호출 없이는 동작 0(자동삽입 0 회귀) · 검색어 평문 로그 미기록 회귀.
- **회귀 보존**: 기존 `Excel2021FunctionChecker` 동작·`CheckFormula` 단언 전부 PASS(단일원천 재사용 후에도 동일).
- 종료부 **`Total=747 → 747+N PASS / 0 FAIL`**, `Unclassified=0`, build 0/0.

## 5. 보고 / Branch
- build 0/0 · SmokeTest **`Total=N PASS / 0 FAIL`** 합계 줄 · Gate A(추적파일 의도·secret/주민번호 0·금지확장자 0·`PackageReference` 0) · 변경 파일 · 양성/음성 케이스 · **"Applied Skill Checklists"**(`risk-ui-ux-review`·`risk-security-guard`·`risk-smoke-governance`·`risk-data-limit-review`).
- Branch `feature/ux-wp-04-excel-function-helper` · Commit: `feat: Excel 2021 function helper — search/detail/alt-formula (static NoModel) (UX-WP-04)`

## 6. Claude Review Checklist
함수 메타 단일원천(기존 `SafetyRuleSet`/`Excel2021FunctionChecker` 재사용·차단목록 재선언 0) / `ExcelFunctionInfo` 완비(설명·인수·리스크예시·수식예시·365여부·대체식·추천) / 수식 삽입 사용자선택만(자동삽입 0) / 입력 원문 로그 미저장(해시 전용) / **helper 데이터 = Core embedded resource(critical glob 미사용·항상 동봉)·파싱실패만 fallback·공개/더미만(실데이터 0)** / **읽기전용 helper view 배선(B-5 종료 — 필수; Core-only=B-5 미종료)** / `CLAUDE.md §6` 정합 / 외부 API·NuGet 0 / 기존 `Excel2021FunctionChecker` 회귀 보존 / `Total` 보존+신규·Unclassified 0 / Gate A.
