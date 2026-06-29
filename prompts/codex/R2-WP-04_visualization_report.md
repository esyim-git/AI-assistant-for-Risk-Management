# Codex 작업 지시 — R2-WP-04 Visualization / Report 강화 (인박스 차트·Heatmap·TopN·집중도 + 정확 Exception Count)

> 너는 이 프로젝트의 **구현자(Codex)**다. 이 프롬프트의 **단일 WP 1개**만 `feature/r2-wp-04-visualization-report` 브랜치에서 구현·테스트한다. Claude 승인 전 Merge 금지. 한국어로 작업하고 보고한다.

## 0. 절대 원칙 (위반 시 STOP)
- **외부 NuGet PackageReference = 0** (인박스 `System.*` 만). **외부 charting NuGet(OxyPlot/LiveCharts/ScottPlot 등) 도입 금지 → 발견 즉시 STOP.** Vector DB/Embedding/Local LLM/모델파일 = 본 WP 무관·금지.
- 외부 API / Telemetry / AutoUpdate = 0. SQL/VBA/Golden6 **자동실행 0**.
- **실데이터·실 테이블/컬럼명·내부규정/NCR 원문·모델파일 repo 미포함.** seed/샘플은 일반 더미명만.
- 쓰기 경로 = `reports/`(리포트) · `logs/`(audit) **한정**. 차트/이미지를 별도 파일로 떨구지 말 것.
- **결정적**(동일 입력 동일 출력). decimal 반올림 자리수 고정.
- 기존 SmokeTest **삭제·약화 금지**(Total 보존 + 신규). 도메인 분류기 **Unclassified=0** 유지. 과대표기 금지(상태어휘 VERIFIED/PARTIAL/SCAFFOLD_ONLY/NOT_IMPLEMENTED).

## 1. 하나의 목표
기존 결정적 `LimitAnalysisResult`를 입력으로 (1) **정확 Exception Count**, (2) **인박스 집계 시각화 데이터 시트**(TopN movers·집중도·Heatmap 등급), (3) **WPF in-box 차트 화면 렌더**를 추가한다. **외부 charting NuGet 금지(STOP).** `BuildReport` 시그니처는 바꾸지 않는다.

## 2. 먼저 읽어라 (Read)
- `CLAUDE.md`(§3·§6 Excel 함수 제한·§11.4·§11.5), `AGENTS.md`
- `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs` (특히 `ExpectedSheetNames`, `BuildWorkbook`, `BuildExceptionRows`, `WriteWorkbookPackage`, `BuildLimitRows`)
- `src/RiskManagementAI.Core/Risk/LimitAnalysisResult.cs`, `LimitMonitor.cs`
- `src/RiskManagementAI.App/MainWindow.xaml(.cs)` (`OnRunLimitMonitor` ~line 705, `OnGenerateExcelReport` ~line 748 — R2-WP-01 #79로 이동; 심볼로 탐색)
- `tests/RiskManagementAI.SmokeTests/{ReportTests.cs, PackagingTests.cs, SmokeTestContext.cs}`

## 3. 현재 사실 (검증된 코드 근거)
- `ExcelReportBuilder`는 in-box(`ZipFile` + `templates/report/*.tpl` 치환)로 xlsx 생성, OpenXML SDK/Interop 미사용. 고정 10시트(`ExpectedSheetNames`). 셀 종류 3가지(Text inlineStr / Number / Formula). 그래픽 part 없음.
- **부정확 카운트**: `formulaExceptionCount = "=COUNTA(EXCEPTION_LIST!A:A)"`는 헤더행("Type")과 NO_EXCEPTION placeholder까지 세어 실제 예외 건수와 1~2 차이. SUMMARY에 이 수식이 그대로 기록됨.
- `BuildExceptionRows`는 `analysis.ExceptionList`(Analysis) + `validationFindings` 중 Blocker/High(Validation)를 emit, 둘 다 없으면 NO_EXCEPTION 1행.
- 신규 시트는 `ExpectedSheetNames`에 항목 추가만으로 `WriteWorkbookPackage`가 Content_Types/workbook/rels/app.xml override·sheet{n}.xml를 자동 배선(styles rId=`rId{Count+1}` 자동 시프트).
- 생성 수식은 `Excel2021FunctionChecker.CheckFormula`로 검사 — EXCEL_365_FUNCTION 1개라도 있으면 `throw InvalidDataException`(리포트 미생성).
- Dashboard(`DashboardSnapshotBuilder`)는 앱 상태 대시보드(7행 텍스트)일 뿐 risk-analytics가 아님. UI는 DataGrid+TextBlock만, 차트 패널 없음.

## 4. 구현 (Scope — 정확히 이것만)
1. **정확 Exception Count(SoT 분리)**: `CountExceptions(LimitAnalysisResult analysis, IReadOnlyList<SafetyFinding> validationFindings)` 헬퍼를 만든다(= ExceptionList.Count + validationFindings Blocker/High count, NO_EXCEPTION/헤더 제외). SUMMARY 시트의 `ExceptionCount`를 이 값의 **Number**로 기록한다. 부정확 `=COUNTA(EXCEPTION_LIST!A:A)`는 권위 카운트에서 제거(참조용으로 남길 경우 라벨을 분리하고 SUMMARY 권위값은 Number).
2. **집계 시각화 데이터 시트(신규)**: `ExpectedSheetNames`에 `RISK_VISUAL`(필요 시 TOPN/CONCENTRATION/HEATMAP 분리 가능, 최소 1개) 추가. 내용 = 상태분포(**7상태** 카운트·비율 — NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**, R2-WP-01 #79 반영) / TopN movers(ExposureAmount 또는 UsageRatio 내림차순 상위 N, tie-break PortfolioId Ordinal) / 집중도(상위N 비중, HHI=Σ(share²), 분모=`Abs(ExposureAmount)` 합) / Heatmap 등급(UsageRatio <0.8 LOW / 0.8~1.0 MID / >1.0 HIGH). 셀은 가급적 Number/inlineStr **정적 값**으로 기록(수식 최소화 → Excel2021 게이트 위험 0). 옵션 ASCII 막대(반복문자 길이=비율) 허용.
3. **공유 집계기(신규 Core)**: `RiskVisualAggregator.Aggregate(LimitAnalysisResult result, int topN) -> RiskVisualModel`(결정적, decimal 비율 자리수 고정). Report 시트와 WPF 차트가 **동일 SoT** 사용. 통화 혼합(`LimitMonitorRow.CurrencyCode`) 시 단순 합산 왜곡 → `MIXED_CURRENCY` 주석/Finding을 모델·시트에 남긴다(합산 전제 명시; 통화별 그룹 집계가 가능하면 우선).
4. **WPF in-box 차트(App, 화면 한정)**: `MainWindow`에 `RenderRiskCharts(LimitAnalysisResult result)` + Canvas/Shapes 컨트롤(Risk Dashboard 탭)을 추가해 상태 막대·집중도 막대·Heatmap 셀 렌더(System.Windows.Shapes·Canvas·DrawingVisual·SolidColorBrush만). 기존 DataGrid/TextBlock 보존. **화면 표시일 뿐 '리포트 차트' 아님.**
5. **SmokeTest 추가 + 동반 갱신**: 아래 6항. 기존 시트 개수(10)·`sheet10.xml` 의존 단언을 신규 개수로 갱신(약화/삭제 아님).

## 5. 하지 말 것 (Out of Scope)
- 외부 charting NuGet 도입(STOP). OOXML chartXML(c:barChart)+xl/drawings part 직접 생성은 **이 WP 범위 아님**(채택 필요 시 멈추고 보고 → ADR 결정). 최소범위 = WPF Shapes + 데이터 시각화 시트.
- 전일 대비(Current/Prev/Δ) 산출(=R2-WP-03). `LimitAnalysisResult`/**7상태**/RECON_* 로직 변경. `BuildReport` 시그니처 변경.
- 차트 이미지를 reports/ 외부 또는 별도 파일로 출력.

## 6. 테스트 (SmokeTest — 외부 프레임워크 0, Unclassified=0)
신규 단언 이름은 **Report/Limit/DataProfile 도메인 키워드**로(예: "ExcelReport ...", "report ...", "EXCEPTION_LIST ...", "LIMIT_MONITORING ...", "RECON_ ...", "BASE_DT ...", "concentration limit ...", "TopN limit ...") → 분류기 Unclassified=0. 신규 suite를 만들지 말고 `ReportTests.cs`에 흡수.
- 정확 카운트: 예외 N건 입력 → SUMMARY `ExceptionCount` Number == N(헤더/NO_EXCEPTION 제외), 0건 → 0.
- 신규 시트: `SheetNames.SequenceEqual(ExpectedSheetNames)` 유지, ZIP에 신규 `sheet{N}.xml` 존재, PackagingTests 인벤토리/개수 갱신.
- TopN/집중도 결정성: 동일 입력 2회 동일 순서·동일 비율, Ordinal tie-break.
- Heatmap 등급: 경계값(0.8/1.0) 결정적.
- 통화 혼합: `MIXED_CURRENCY` 노출.
- 회귀: **7상태**(DUPLICATE_LIMIT 포함)·RECON_*·ReconciliationPassed PASS/FAIL·NO_LIMIT_ROW·NuGet 0 단언 전부 유지.

## 7. 완료조건 / 보고
- 로컬 `dotnet build RiskManagementAI.sln -c Release` 0/0, `dotnet run --project tests/RiskManagementAI.SmokeTests` → **`Total=N PASS=N FAIL=0`**(직전 정본 Total[R2-WP-02/03 머지 후] + 신규, Unclassified=0). `git grep PackageReference` = 0.
- 생성 xlsx를 로컬 Excel 2021에서 열어 **손상 경고 없음** 확인(증거 첨부; 불가 시 BLOCKED 명시).
- 보고 형식: 변경 파일 목록 + Diff 요지 / SmokeTest 전후 Total / NuGet 0 증거 / Excel 열기 결과 / 상태어휘(데이터 시트·정확 카운트=VERIFIED 후보, WPF=화면 한정, chart part=미채택). **과대표기 금지.**
- Branch `feature/r2-wp-04-visualization-report`, 1 PR(squash), Subject 예: `Add in-box risk visualization sheet and exact exception count (#NN)`. force push·hard reset·main 직접 push 금지.

## Codex 리뷰 반영 (P2 — 필수 준수)
- **(P2) DuplicateLimit 리포트 반영**: R2-WP-01이 추가하는 `DuplicateLimit`/`DUPLICATE_LIMIT`를 RISK_VISUAL 상태분포·SUMMARY 등 report/aggregator 회귀가 **상태 존재 시 카운트에 포함**(6상태만 동결 금지 — WP-01 hardening 신호 은폐 방지).
- **(P2) 집중도/HHI는 절대 노출 기준**: `LimitMonitorRow.ExposureAmount`는 음수 가능하고 usage ratio는 `Math.Abs(exposure)`를 쓴다(`LimitMonitor.cs:576`). 집중도/HHI 분모·share는 **`Abs(ExposureAmount)` 합** 기준(또는 양수만/부호버킷 규칙 명시). raw 합 0/음수면 undefined → **분모 0 graceful(빈 시각화·Finding)** 회귀 추가.
