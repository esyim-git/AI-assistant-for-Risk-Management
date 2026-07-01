# Codex Prompt — QA-WP-04: Report/ExcelReport SmokeTest 하드닝 (RISK_VISUAL·정확 Exception Count·Dashboard=Report)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-04) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-04-report-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-04, `SKILLS.md`+`risk-data-limit-review`·`risk-analytics-design`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/Report/*`(`ExcelReportBuilder`·`RiskVisualAggregator`·`RISK_VISUAL`·Exception Count·SUMMARY), `tests/RiskManagementAI.SmokeTests/ReportTests.cs`.
> **기준선**: main `d8cb415`(VERSION 0.7.0), 정본 SmokeTest `Total=861 PASS=861 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
인박스 OOXML Report와 `RISK_VISUAL` 시각화·정확 Exception Count·Dashboard=Report 일원화의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드 변경 0** — 테스트만. 외부 charting NuGet 0 불변.

## 1. 작업 범위 (ReportTests.cs — additive only)
1. `Core/Report/*`(R2-WP-04 산출)와 현 `ReportTests`를 대조 → **미커버 경계만** 추가.
2. 후보(제품 동작 확인·신규 동작 요구 아님):
   - `RISK_VISUAL` 시트: 7상태 분포(DuplicateLimit 포함)·TopN(`Abs(Exposure)` 정렬·tie-break)·집중도 HHI(분모=`Σ Abs(Exposure)`·**0분모 graceful** `VISUAL_CONCENTRATION_ZERO_DENOMINATOR`)·Heatmap 등급 경계(<0.8 LOW/≤1.0 MID/>1.0 HIGH)·`MIXED_CURRENCY` finding.
   - **정확 Exception Count**(Number SoT·`CountExceptions` — 부정확 COUNTA 아님) 경계.
   - **Dashboard=Report 일원화**(동일 AnalysisResult·`DuplicateLimitCount` 노출·화면 수치=리포트 수치).
   - 시트 배선(`ExpectedSheetNames` 개수·`RISK_VISUAL` 정적 Number/text 값만 → Excel2021 수식 게이트 위험 0)·시각화 caveat `ExcelReportResult.Findings` 표면화.
   - OOXML 구조(ZIP·sheet xml·셀 내용) 결정성.
3. **합성 더미만** — 실 PF/RF/한도·실 테이블·컬럼명 0.

## 2. 제외 범위
`ExcelReportBuilder`/`RiskVisualAggregator` 제품 코드 변경. 신규 시트·집계. 외부 charting NuGet. 기존 단언 수정/삭제/약화.

## 3. 보안조건
합성 더미만(실데이터 0) · `RISK_VISUAL` 정적값(수식 주입 0) 회귀 · 외부 charting NuGet 0 · **기존 테스트 삭제·약화 0**.

## 4. 테스트 (SmokeTest — 도메인 `Report`)
> `SmokeTestContext.SmokeDomain` Report(line ~55: `ExcelReport`/`ReportBuilder`/`report `/`LIMIT_MONITORING`/`EXCEPTION_LIST`/`SUMMARY`/`templates/report`). 신규 단언 설명에 이 토큰 사용, **더 위** Reconciliation(`RECON`/`duplicate limit`/`row amplification`)·(Report는 Limit보다 위이므로 `limit` 토큰 있어도 Report로 감) 주의는 Reconciliation만. `Unclassified=0`.
- 각 경계 → 기대값(분포·TopN·HHI·0분모·Heatmap·Exception Count·동일 수치) 단언·OOXML 결정성.
- 기존 `ReportTests` 단언 **전부 보존**. 종료부 **`Total=861 → 861+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Report 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-04-report-hardening` · Commit: `test: harden report and risk-visual coverage (QA-WP-04)`

## 6. Claude Review Checklist
제품 코드 변경 0(테스트만) / 추가는 실제 미커버 경계 / HHI 0분모·Heatmap 경계·정확 Exception Count·Dashboard=Report 기대값 정확 / `RISK_VISUAL` 정적값(수식 주입 0) / 합성 더미(실데이터 0) / 도메인 Report·Unclassified 0 / 기존 ReportTests 보존·감소 0 / 외부 charting NuGet 0 / `Total` 861 보존+신규 / Gate A.
