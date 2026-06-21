# Codex WP-07 — Dashboard·Report 공통화 (단일 `LimitAnalysisResult`) · `BuildUiLimitRows` 제거

> 권위 스펙: `docs/39` WP-07, `docs/40` ADR-002(공통 AnalysisResult), `docs/38` RR-03. Release: **R1 마지막 WP**. 선행: WP-05·WP-06(완료).

## 목표
**공통 `LimitAnalysisResult` 하나**가 Risk Dashboard 그리드와 Excel Report(LIMIT_MONITORING·EXCEPTION_LIST·SUMMARY)를 **동일 수치로** 생성하게 한다. 화면이 쓰는 실 분석값(6상태·사용률·잔여·예외·대사)을 Report도 **그대로** 쓰고, **합성 잔재 `BuildUiLimitRows`와 분기 산식(`ExcelReportLimitRow`·`CalculateLimitStatus`의 3상태 OK/BREACH/NO_LIMIT)을 완전 제거**한다 → "화면 = 리포트 = 감사 수치 일치"(ADR-002).

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4·§5`, `docs/39`(WP-07·WP-05·WP-06·WP-01), `docs/40`(ADR-002). 기존:
- `App/MainWindow.xaml.cs`: `OnRunLimitMonitor`(L476, **실 분석→그리드**), `OnGenerateExcelReport`(L519, **합성 Report 흐름**), `BuildUiLimitRows`(L665, 빈배열 잔재)·`BuildUiLimitFindings`(L670, LIMIT_DATA_REQUIRED/DEMO_ONLY), `RiskLimitRowDisplay.FromRow`(L843).
- `Core/Report/ExcelReportBuilder.cs`: `ExcelReportLimitRow`(L12), `ExcelReportRequest.LimitRows`(L19-26), `BuildLimitRows`(L315), `BuildExceptionRows`(L337), `CalculateUtilization`/`CalculateLimitStatus`(L361·366 — **3상태 분기, 제거 대상**), `ExpectedSheetNames`(L37, 10시트).
- `Core/Risk/LimitAnalysisResult.cs`(WP-05/06: `MonitoringTable`·`Kpis`·`ExceptionList`·`Reconciliation`·`Metadata`), `LimitMonitorRow`(`StatusCode` 6상태·`UsageRatio`·`RemainingLimit`).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-07-dashboard-report-unify origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위
1. **`ExcelReportRequest`가 `LimitAnalysisResult`를 입력**으로 받는다(`IReadOnlyList<ExcelReportLimitRow> LimitRows` 대체). LIMIT_MONITORING 시트는 **`result.MonitoringTable`**(LimitMonitorRow: BaseDate·Portfolio·RiskFactor·Exposure·Limit·**UsageRatio**·**RemainingLimit**·**StatusCode(6상태)**·Note)에서 생성 — **Report가 상태·사용률을 재계산하지 않는다**.
2. **EXCEPTION_LIST 시트 = `result.ExceptionList`(LimitException, WP-06 `RECON_*` 포함) + High/Blocker validation findings**(기존 머지 유지, 단 한도 예외는 분석 결과에서).
3. **SUMMARY**에 6상태 KPI(`result.Kpis`) + **대사 PASS/FAIL(`result.Reconciliation.Passed`)** 노출.
4. **`BuildUiLimitRows` 완전 삭제** + `ExcelReportLimitRow`·`CalculateUtilization`·`CalculateLimitStatus`(3상태) 제거. App report 흐름은 실 `LimitAnalysisResult`(또는 한도 없음 시 **빈 분석**)를 전달.
5. **WP-01 보존**: 실 한도 없거나 데모 경로면 **합성 미생성** + `LIMIT_DATA_REQUIRED`/`DEMO_ONLY` finding 유지(`BuildUiLimitFindings` 로직을 분석-기반으로 보존/이전). 빈 분석 → LIMIT_MONITORING "데이터 없음" 안내(합성 0).

## 제외 범위
- 시스템-홈 `DashboardSnapshot`(오프라인·모델·감사 카운트 등 **시스템 헬스**, 한도 분석 아님)은 **대상 아님**.
- 분석 로직/상태셋/대사 변경(WP-05/06 그대로 사용). 새 입력형식·전일대비(WP-09).

## Public Interface (정합)
- `ExcelReportRequest`: `LimitRows` 제거 → **`LimitAnalysisResult Analysis`** 추가(필요 시 `MonitoringTable`+`ExceptionList`+`Kpis`+`Reconciliation`를 묶어 전달). 한도 미존재 시 **빈 `LimitAnalysisResult`**(rows 0, `LIMIT_DATA_REQUIRED` finding) 전달.
- `ExpectedSheetNames`(10) **불변**. 시트 의미 동일, **출처만 분석 결과로**.

## 구현 세부 / 결정성 / 호환
- **단일 진실원장**: 동일 `LimitAnalysisResult` → Dashboard 그리드 행 == Report LIMIT_MONITORING 행(Portfolio·RiskFactor·Exposure·Limit·UsageRatio·**StatusCode** 동일). Report는 **분석값 그대로** 출력(분기 산식 0).
- **결정적**: 동일 입력 → 동일 리포트 셀(정렬·반올림 고정).
- **합성 0(WP-01)**: `1.1m`·합성 한도 0 유지. 합성값 audit/리포트 기록 금지.
- 읽기 전용·외부 0·NuGet 0·**인박스 xlsx(DM-03)**. Excel 2021 호환(365 함수 0)·Formula Injection 0·외부링크 0·Macro 0 유지.
- 감사: 리포트/분석 audit **해시 전용**(원문 SQL/사용자ID 미저장) 유지.
- 기존 357 SmokeTest 유지(변경된 Request 형태에 맞춰 갱신하되 **의미·수치·시트구조·WP-01/05/06 회귀 유지**).

## 테스트(필수)
- **화면=리포트 동일 수치**: 동일 `LimitAnalysisResult`로 Report 생성 → LIMIT_MONITORING 행이 `MonitoringTable`과 **1:1(Status/UsageRatio 포함) 일치**.
- **6상태 그대로**: BREACH/WARNING/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR가 리포트에 분석값대로 노출(3상태 OK/BREACH/NO_LIMIT 분기 사용 안 함).
- **EXCEPTION_LIST = 분석 ExceptionList(`RECON_*` 포함) + High/Blocker validation**.
- **WP-01 보존**: 한도 없음 → 합성 0 + `LIMIT_DATA_REQUIRED`; 데모경로 `DEMO_ONLY`.
- **합성/분기 제거 회귀**: `BuildUiLimitRows`·`ExcelReportLimitRow`·`CalculateLimitStatus` 소스 0(스캔).
- 10시트·Excel2021(365 함수 0)·audit 해시 전용 기존 회귀 유지. **결정성**(반복 동일).

## 완료/보고
공통 `LimitAnalysisResult` 1개가 Dashboard·Report·History·Audit를 **동일 수치**로 구동. `BuildUiLimitRows`·분기 산식 0. build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/39` 원장 갱신. (R1 데이터 파운데이션 종료 → docs/41 §1 Data Gate 전체 체크 준비.)

## Claude Review Checklist
단일 `LimitAnalysisResult` 소비(화면=리포트 동일 수치) / 6상태·사용률 **재계산 없음** / EXCEPTION_LIST=분석예외+`RECON_*`+High validation / `BuildUiLimitRows`·`ExcelReportLimitRow`·3상태 산식 **제거** / WP-01 합성 0 보존 / 10시트·Excel2021·audit 해시 / 결정성 / 기존 357 유지 / Gate A.
