# Codex Prompt — UX-WP-11: Excel 2021 Function Helper 카탈로그 확장 (embedded resource, 큐레이션)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-11) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-11-excel-function-catalog` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `CLAUDE.md §6`(Excel 2021 함수 제한), `docs/39` UX-WP-11, `SKILLS.md`+`risk-ui-ux-review`·`risk-security-guard`·`risk-smoke-governance`, **UX-WP-04 산출**(`src/RiskManagementAI.Core/Excel/*` Function Helper + embedded 함수 카탈로그 resource·`Excel2021FunctionChecker`·`SafetyRuleSet.ExcelBlockedFunctions`/`ExcelPreferredFunctions`), 해당 helper 테스트.
> **기준선**: main `d8cb415`(VERSION 0.7.0, 확장 트랙 Wave 1 머지 후), 정본 SmokeTest `Total=861 PASS=861 FAIL=0`.

## 0. 목표 (단일)
UX-WP-04 Excel Function Helper의 **함수 카탈로그(embedded resource)를 큐레이션 확장**한다 — 리스크 실무 유용 함수의 목적·인수·리스크예시·**Excel 2021 대체식**(365 차단 함수는 대체식 필수). **정적·자동삽입 0·검색어 미로그·helper/checker 구조 변경 0** — 카탈로그 데이터(+테스트)만.

> **핵심 안전 불변식(§6)**: 카탈로그는 **Excel 2021 호환 함수만** 노출한다. **차단 함수**(`VSTACK/HSTACK/TOCOL/TOROW/TAKE/DROP/CHOOSECOLS/TEXTSPLIT/TEXTBEFORE/TEXTAFTER/GROUPBY/PIVOTBY/MAP/REDUCE/BYROW/BYCOL/REGEX*`)를 **추천 함수로 추가 금지**. 차단 함수는 "대체식" 항목에서만 참조하고 그 자체를 삽입 대상으로 두지 않는다.

## 1. 작업 범위 (embedded 카탈로그 resource + 테스트만)
1. UX-WP-04의 **embedded 함수 카탈로그 resource**(csproj `<EmbeddedResource>`, **critical glob `rules/`·`templates/`·`kb/`·`config/ncr` 미사용** 확인 — UX-WP-04가 이미 그렇게 배치)에 함수 항목 큐레이션 추가. 후보(리스크 실무): `XLOOKUP`·`XMATCH`·`FILTER`·`SORT`/`SORTBY`·`UNIQUE`·`SEQUENCE`·`LET`·`SUMIFS`/`COUNTIFS`/`AVERAGEIFS`·`INDEX`/`MATCH`·`IFERROR`·`SUMPRODUCT` 중 **미수록분**. 각 항목: 목적·인수·리스크 사용예시(placeholder 범위·실데이터 0)·(해당 시) 차단 365 함수 → 2021 대체식.
2. **큐레이션 원칙**: 양보다 질(중복·자명 남발 금지). 결정적 정렬·기존 `Excel2021FunctionChecker`/`SafetyRuleSet` 단일 원천 재사용(룰 중복 0). 자동삽입 0·사용자 선택 삽입만·검색어 미로그.
3. **무결성/패키징 invariant**: resource가 critical glob이 아님을 유지(UX-WP-04와 동형). resource 추가가 `IntegrityVerifier`/`PackagingTests` 인벤토리와 정합(필요 시 인벤토리 갱신 — 단 critical glob에는 넣지 않음).

## 2. 제외 범위
Function Helper WPF view/엔진/검색 로직 변경(UX-WP-04). Smart Assist completion seed(UX-WP-10). 차단 함수 추천 추가. 자동삽입. 실데이터. 신규 NuGet.

## 3. 보안조건
Excel 2021 호환만·**차단 함수 추천 0**(대체식 참조만) · 자동삽입 0·검색어 미로그 · 실 범위/데이터 0(placeholder) · resource critical glob 미사용(무결성 오발 0) · RuleSet 단일 원천 · NuGet 0.

## 4. 테스트 (SmokeTest — 도메인 `Assist`/`Safety`)
> `SmokeTestContext.SmokeDomain`: Assist(`completion`/`assist`/`provider`) 또는 Safety(`Excel 2021`/`checker`/`finding`/`RuleSet`). 신규 단언 설명에 해당 토큰, Kb/Limit/Report 트리거 회피. `Unclassified=0`.
- **안전 불변식 강제(필수)**: 신규 카탈로그의 **모든 추천 함수가 `Excel2021FunctionChecker`/`ExcelBlockedFunctions` 기준 차단 함수 아님**을 단언(자동 회귀 — 차단 함수 유입 차단). 각 항목 필수 필드(목적·인수·대체식) 존재 단언. 결정적 정렬.
- 기존 helper 테스트 **전부 보존**. 종료부 **`Total=861 → 861+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Assist/Safety·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 함수 목록 · **차단 함수 미포함 단언 결과** · resource 무결성 배치 확인 · **Applied Skill Checklists**.
- Branch `feature/ux-wp-11-excel-function-catalog` · Commit: `feat: curate and expand excel 2021 function helper catalog (UX-WP-11)`

## 6. Claude Review Checklist
카탈로그 데이터 확장만(helper/checker/엔진 구조 불변) / **차단 함수 추천 0 — checker 단언으로 강제**·대체식은 참조만 / Excel 2021 호환만 / 자동삽입 0·검색어 미로그 / resource critical glob 미사용(무결성 오발 0)·인벤토리 정합 / placeholder(실데이터 0) / RuleSet 단일 원천 / 도메인 Assist/Safety·Unclassified 0 / 기존 테스트 보존 / NuGet 0 / `Total` 861 보존+신규 / Gate A.
