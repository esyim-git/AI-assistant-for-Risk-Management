# Codex WP-05 — 실 Exposure-Limit Join + 공통 `LimitAnalysisResult`

> 권위 스펙: `docs/39` WP-05, `docs/40` ADR-002(공통 AnalysisResult), `docs/38` RR-03·5절(상태셋). Release: R1. 선행: WP-01·WP-04(완료).

## 목표
`LimitMonitor`의 **실 조인 결과**(BASE_DT+PORTFOLIO_ID+RISK_FACTOR)를 Dashboard·Excel Report·History·Audit가 함께 쓰는 **단일 공통 객체 `LimitAnalysisResult`**로 표준화한다. 상태셋을 6종으로 확장. **합성값 미사용(WP-01)**, **동일 입력 → 동일 수치(결정적)**.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4`, `docs/39`(WP-05), `docs/40`(ADR-002), `docs/38`(RR-03), 기존 `Core/Risk/LimitMonitor.cs`(현 `Analyze`/`Classify`/`LimitMonitorResult`/`LimitMonitorRow`/`LimitMonitorStatus`), `App/MainWindow.xaml.cs` L480·L634·L860-870(유일 소비부).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-05-join-analysis-result origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- 신규 `Core/Risk/LimitAnalysisResult.cs`: **KPI + MonitoringTable + ExceptionList + Metadata + Findings**.
- `LimitMonitor.Analyze`가 이 타입을 반환. **CsvTable 입력 코어 + 경로 오버로드**(확장자별 CsvReader/XlsxReader).
- 상태셋 6종 + `Classify` 정렬. 매핑 물리컬럼 부재는 **graceful MappingError**.
- 기존 소비부(`MainWindow`)는 **최소 수정**으로 컴파일·수치 유지.
- **제외**: 대사 9종(WP-06), Dashboard·Report 공통객체 **완전 일원화 + `BuildUiLimitRows` 대체(WP-07)**.

## Public Interface (WP-04와 정합 — 매핑은 생성자 주입)
- 매핑은 **WP-04대로 생성자 주입** 유지(`LimitMonitor(ColumnMappingLoadResult)`/`(ColumnMapping)`/기본). docs/39의 예시 `…, ColumnMapping map)` 메서드 파라미터는 **생성자 주입으로 대체**(매핑 소스 이중화 금지).
- 코어: `LimitAnalysisResult Analyze(CsvTable exposure, CsvTable limit, string baseDate)`.
- 호환 오버로드: `LimitAnalysisResult Analyze(string exposurePath, string limitPath, string baseDate)` — 확장자(.csv/.xlsx)에 따라 `CsvReader`/`XlsxReader`로 읽어 코어 호출(기존 호출부 유지, 반환형만 변경).

## 데이터 모델 (`LimitAnalysisResult`)
- `Metadata`: BaseDate, Exposure/Limit 소스명, **컬럼 매핑 fallback 여부·경고**(WP-04 `UsedFallback`/`Warnings` 전파), 결정성 표식.
- `MonitoringTable`: `IReadOnlyList<LimitMonitorRow>`(**기존 Row 재사용**; `Status`는 새 6종).
- `Kpis`: 총건수 + 상태별 카운트(Normal/Warning/Breach/NoLimit/InvalidLimit/MappingError) + 합계(노출합·한도합 등 **결정적**).
- `ExceptionList`: `IReadOnlyList<LimitException>`(코드·심각도·메시지·관련 키) — NoLimit/InvalidLimit/MappingError/매핑 fallback을 예외로.
- `Findings`: 기존 `SafetyFinding` 목록(요약/완료/매핑 fallback). **합성값 0**.

## 상태셋 (6종, 현행 정확 정렬)
PascalCase enum `LimitMonitorStatus { Normal, Warning, Breach, NoLimit, InvalidLimit, MappingError }`:
- **Normal/Warning(사용률 ≥0.9)/Breach(>1)**: `ABS(노출)/한도` — 현행 유지.
- **NoLimit**: 조인 키에 매칭 한도 행 **없음**(현 `MissingLimit`의 "한도행 없음" 케이스).
- **InvalidLimit**: 한도 행은 있으나 **사용 불가** — `USE_YN≠Y`(현 `InactiveLimit`) **또는** 한도 `≤0`/숫자 아님(현 `MissingLimit`의 "≤0" 케이스).
- **MappingError**: 매핑된 물리 컬럼이 실제 입력 헤더에 **없음** → **하드 throw 대신** 해당 행/분석을 MappingError로 표기(graceful). WP-04 매핑과 실데이터 불일치 안전 처리.
- 출력 문자열: `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`.

## 구현 세부 / 결정성 / 호환
- **결정적**: 동일 입력 → 동일 KPI·표·예외(정렬·반올림 고정, 비결정 컬렉션 순서 금지).
- **합성 한도 미사용(WP-01)**. 읽기 전용. 외부 호출 0. NuGet 0.
- **MappingError graceful**: 매핑 물리컬럼이 헤더에 없으면 `GetValue`/`ParseDecimal` 하드 throw 금지 — 행 단위로 잡아 MappingError + ExceptionList 기록, **전체 분석은 계속**.
- 기존 `LimitMonitorResult` 소비부(`MainWindow`)는 컴파일·수치가 깨지지 않게 **최소 수정**으로 `LimitAnalysisResult`를 읽게 한다(상태→문자열 6종, 대시보드 요약). **Dashboard·Report를 단일 객체로 완전 일원화하는 것은 WP-07**.
- 기존 `LimitMonitorResult`/`LimitMonitorStatus` 참조 테스트는 새 타입/상태로 갱신하되 **의미·수치 회귀 유지**(기존 322 PASS 약화 금지).

## 보안
읽기 전용 · 합성 미사용 · 외부 0 · NuGet 0 · 경로 가드(reader 경유).

## 테스트(필수)
- BASE_DT 조인 + **6 상태 각각** 분류(특히 **NoLimit vs InvalidLimit 분리**, **MappingError graceful**).
- **동일 입력 → KPI/표/예외 결정적**(반복 호출 동일 수치).
- **합성 한도 행 0**(WP-01 회귀 유지).
- CsvTable 코어 + 경로 오버로드(.csv·.xlsx) **동일 결과**.
- 매핑 물리컬럼 누락 입력 → **throw 아님**, MappingError + 예외 기록.
- 기존 한도 테스트 의미 유지(상태 enum 갱신 반영).

## 완료/보고
공통 `LimitAnalysisResult` 1개를 Dashboard·Report 양쪽이 소비 가능(WP-07 준비 완료). build 0/0 · SmokeTest 기존 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/39` 원장 갱신.

## Claude Review Checklist
6 상태(현행 정렬 + MappingError graceful) / 결정성 / 합성 미사용 / 매핑 누락 graceful / CsvTable·경로 오버로드 동일 / 기존 한도 테스트 유지·확장 / Gate A.
