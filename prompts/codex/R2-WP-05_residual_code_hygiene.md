# Codex Prompt — R2-WP-05: R2 잔여 데드코드 하이진 (Welford 미사용 필드 제거, 자기검증형·동작 불변)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(R2-WP-05) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/r2-wp-05-residual-code-hygiene` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` R2-WP-05, `SKILLS.md`+`risk-data-limit-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/Data/{DataProfiler,CsvReader}.cs`(및 `NumericAccumulator` 정의 위치), `tests/RiskManagementAI.SmokeTests/{DataProfileTests,CsvTests}.cs`.
> **기준선**: main `f8b330a`(VERSION 0.7.0), 정본 SmokeTest `Total=807 PASS=807 FAIL=0`.

## 0. 목표 (단일 · 동작 불변)
R2-WP-02(#81 `5280d54`)에서 OutlierCount가 legacy 2-pass로 회귀되며 **미사용(dead)으로 남은 Welford 누산 필드(`mean`/`m2` 또는 동등)를 제거**한다. **순수 데드코드 정리 — 관측 가능한 동작·수치·결정성 변경 0.** 프로파일 결과(count/sum/min/max·OutlierCount·`DuplicateRowCount`·`NumericColumnProfile` 6필드)는 **바이트 동일**.

## 1. 작업 범위 (자기검증형)
1. **사용처 확인 먼저**: `NumericAccumulator`(및 streaming 경로)의 Welford `mean`/`m2`(또는 그에 준하는 누산 상태)가 **실제로 어디서도 읽히지 않음**을 usage 검색으로 확정한다. OutlierCount는 R2-WP-02의 **legacy two-pass** 경로로 산출됨을 재확인.
2. **미사용 확정 시에만 제거**: 해당 필드/갱신문을 제거하고, 남는 누산(count/sum/min/max + 필요한 스칼라)만 유지. `ProfileTable`/`ProfileCsv`/`ProfileCsvStreaming`/`Read`/`ReadStreaming` 경로·`NumericColumnProfile` 계약 **불변**.
3. **사용 중이면 STOP**: 만약 해당 필드가 어떤 경로에서든 읽히면(즉 dead가 아니면) **제거하지 말고** 변경 0으로 두고, PR 본문에 "사용 중 → no-change" 사유를 보고한다(억지 리팩터 금지).

## 2. 제외 범위 (record-only/by-design — 손대지 않음)
- R2-WP-01 `RECON_BASEDATE_MISMATCH` 다중 가산·`JoinAudit` 물리컬럼명(인메모리 metadata) — **본 WP 밖**(별도 판단 필요).
- R2-WP-03 movers에 Unchanged(|Δ|=0) 포함(최하위·**by-design**)·`IsNonNumeric` 중복 조건(무해) — **손대지 않음**.
- 스트리밍 상한/OutlierCount 알고리즘·결정성 변경. 신규 기능. 신규 NuGet.

## 3. 보안조건
동작·수치·결정성 불변(회귀 0) · 실데이터 0 · NuGet 0 · streaming 상한(`MaxRowCount`/`MaxByteSize`)·CP949 streaming 결정성 불변 · 기존 테스트 삭제·약화 0.

## 4. 테스트 (SmokeTest — 도메인 `DataProfile`/`Csv`)
> 순수 데드코드 제거이므로 **신규 단언 불필요**가 원칙 — 기존 `DataProfileTests`/`CsvTests`가 **전부 그대로 통과**(streaming==in-memory 전필드 동일·Welford 3σ·OutlierCount 2-pass·`DuplicateRowCount`·byte/row cap·CP949 streaming)함으로 동작 불변을 고정한다.
- **`Total=807` 불변**(단언 가감 0)이 정상. 만약 코드 구조상 불가피하게 단언이 바뀌면 사유·매핑 필수(감소 금지).
- `Unclassified=0` 보존.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(`Total=807 PASS/0 FAIL` 불변 기대) · Gate A 0 · PackageReference 0 · 변경 파일 · **usage 검색 결과(dead 확정 근거 또는 no-change 사유)** · **Applied Skill Checklists**.
- Branch `feature/r2-wp-05-residual-code-hygiene` · Commit: `refactor: remove dead Welford accumulator fields, behavior unchanged (R2-WP-05)`

## 6. Claude Review Checklist
제거 대상이 **진짜 dead**(usage 검색 근거) / 프로파일 수치·OutlierCount·`DuplicateRowCount`·결정성·streaming 상한·CP949 **바이트 동일**(회귀 0) / `NumericColumnProfile`·공개 경로 계약 불변 / by-design/record-only 항목 미접촉 / 기존 DataProfile/Csv 테스트 전부 통과·`Total=807` 불변(가감 시 사유) / NuGet 0 / 사용 중이면 no-change 보고 / Gate A.
