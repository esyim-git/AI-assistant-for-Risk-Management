# Codex 구현 프롬프트 — R2-WP-03 전일 대비 분석 (Prior-Day Analytics)

너는 이 저장소(금융 리스크관리 오프라인 Local AI Assistant, C#/.NET8, WPF)의 구현 담당이다. **이 프롬프트의 작업 단위(WP) 1개만** 구현한다. 머지는 Claude 승인 후 별도로 진행한다. 한국어로 보고하라.

## 절대 원칙 (위반 시 STOP하고 보고)
- **외부 NuGet PackageReference = 0.** `System.*` 인박스만. 패키지 추가 금지.
- **시각화/차트 라이브러리 0** (OxyPlot/LiveCharts/ScottPlot 등 금지). 본 WP는 시각화가 **없다**(차트는 R2-WP-04).
- **Vector DB / Embedding / Local LLM Runtime / 모델파일 = STOP** (승인 전 금지). LLM·통계 라이브러리 호출 0.
- 외부 API / Telemetry / AutoUpdate 0 · SQL/VBA/Golden6 자동실행 0.
- 실데이터 / 실 테이블·컬럼명 / 내부규정·NCR 원문 / 모델파일 repo 미포함. 테스트 fixture는 더미명만(`PF_*`, `RF_*`, `BASE_DT`, `EQD`, `KRW`).
- **기존 테스트 삭제·약화 금지.** SmokeTest 정본 Total 보존 + 신규만 추가.
- 쓰기 경로는 `logs/`·`reports/`·`config/`로 한정(본 WP는 파일쓰기 없이 구현 권장).
- 과대표기 금지. 상태 어휘: VERIFIED/PARTIAL/SCAFFOLD_ONLY/NOT_IMPLEMENTED 등.

## 목표 (하나의 명확한 목표)
동일 `(PortfolioId, RiskFactor)` 단위로 **당일(BASE_DT=N) 대비 전일(N-1)** 한도분석 결과를 **결정적으로** 결합하여 다음을 산출한다:
1. 행별 Current / Prev / **Δ(증감)** (UsageRatio·ExposureAmount·RemainingLimit 등)
2. 상태전이/이동 분류: New / Resolved / Increased / Decreased / Unchanged / StateTransition
3. **TopN movers** (|UsageRatio Δ| 내림차순)
4. **검토용 초안 4구획 출력 계약**: Data-Fact / Methodology / User-Validation / Hidden-Risk (구조화 record)

**새 분석 엔진·새 상태·새 분류 로직을 만들지 마라.** 기존 `LimitMonitor.Analyze`를 N·N-1 두 번 호출하여 그 결과를 차분(diff)한다.

## 먼저 Read 할 것
- `src/RiskManagementAI.Core/Risk/LimitMonitor.cs` (심볼로 탐색 — 라인은 R2-WP-01 #79로 이동됨: `public LimitAnalysisResult Analyze(...)` 2개 오버로드, `BuildJoinKey`, `enum LimitMonitorStatus`(**7상태** — NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**), `record LimitMonitorRow`)
- `src/RiskManagementAI.Core/Risk/LimitAnalysisResult.cs` (전체 — 기존 record 계약)
- `tests/RiskManagementAI.SmokeTests/SmokeTestHelpers.cs` (`CreateReportSmokeInputs` `:315`, `EmptyLimitAnalysis` `:283`, 더미 fixture 패턴)
- `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs` (테스트 추가 위치 + 어서션 메시지 스타일)
- `tests/RiskManagementAI.SmokeTests/SmokeTestContext.cs` (`SmokeDomain` `:84` — 도메인 분류기. 신규 테스트는 Limit/Reconciliation으로 분류되도록 어서션 메시지에 "limit"/"prior-day"/"LimitMonitor"/"Reconcil" 등 키워드 포함)

## 작업 범위
### 신규 `src/RiskManagementAI.Core/Risk/PriorDayAnalysisResult.cs`
아래 record/enum을 정의(전부 sealed record, plain·결정적):
- `enum PriorDayMovement { New, Resolved, Increased, Decreased, Unchanged, StateTransition }`
- `PriorDayComparisonRow(string PortfolioId, string RiskFactor, string CurrentBaseDate, string PriorBaseDate, LimitMonitorStatus? CurrentStatus, LimitMonitorStatus? PriorStatus, decimal CurrentUsageRatio, decimal PriorUsageRatio, decimal UsageRatioDelta, decimal CurrentExposureAmount, decimal PriorExposureAmount, decimal ExposureAmountDelta, decimal CurrentLimitAmount, decimal PriorLimitAmount, decimal LimitAmountDelta, decimal CurrentRemainingLimit, decimal PriorRemainingLimit, decimal RemainingLimitDelta, PriorDayMovement Movement)` — **Exposure·Limit·UsageRatio·RemainingLimit 전부 Current/Prior/Δ 포함**(한도만 변경·노출 불변 케이스 포착, P2-3).
- `PriorDayKpis(int ComparedCount, int NewCount, int ResolvedCount, int IncreasedCount, int DecreasedCount, int UnchangedCount, int StateTransitionCount)` + `static FromRows(IReadOnlyList<PriorDayComparisonRow>)`
- `PriorDayMovers(IReadOnlyList<PriorDayComparisonRow> TopByUsageRatioDelta)`
- 4구획 계약:
  - `PriorDayDataFact(string CurrentBaseDate, string PriorBaseDate, PriorDayKpis Kpis, IReadOnlyList<PriorDayComparisonRow> ComparisonTable, PriorDayMovers Movers)`
  - `PriorDayMethodology(string PriorBaseDateSelectionRule, string JoinKeyRule, string MoverRankingRule, string DraftNotice)`
  - `PriorDayUserValidation(IReadOnlyList<string> ChecklistItems)`
  - `PriorDayHiddenRisk(IReadOnlyList<SafetyFinding> Findings)`
  - `PriorDayContract(PriorDayDataFact DataFact, PriorDayMethodology Methodology, PriorDayUserValidation UserValidation, PriorDayHiddenRisk HiddenRisk)`
- `PriorDayAnalysisResult(PriorDayContract Contract, LimitAnalysisResult Current, LimitAnalysisResult Prior, bool IsDeterministic)`

### 신규 `src/RiskManagementAI.Core/Risk/PriorDayAnalyzer.cs` (sealed class)
- `public PriorDayAnalysisResult Analyze(CsvTable exposure, CsvTable limit, string currentBaseDate, string priorBaseDate, int topN = 10)`
- `public PriorDayAnalysisResult Analyze(string exposurePath, string limitPath, string currentBaseDate, string priorBaseDate, int topN = 10)`
- 내부에서 `LimitMonitor.Analyze(...)`를 currentBaseDate·priorBaseDate로 **2회** 호출. 조인/**7상태** 분류 **재구현 금지**.
- 두 결과의 `MonitoringTable`을 `(PortfolioId, RiskFactor)` 키로 짝짓는다. 키 의미는 `LimitMonitor.BuildJoinKey`와 **동일**(Trim + `` 구분, OrdinalIgnoreCase 짝짓기). copy-paste 불일치 방지를 위해 `LimitMonitor`에 `internal static string BuildComparisonKey(string, string)`를 **시그니처 비파괴 추가**로 노출하고 Analyzer가 그것을 호출하는 방식을 우선 검토하라(SmokeTests가 internal 접근 필요 시 기존 `InternalsVisibleTo` 설정 확인; 없으면 Analyzer 내부에 동일 로직을 두고 테스트로 동치성을 고정).

### 분류 규칙 (결정적, 우선순위 순)
1. 한쪽에만 존재 → `New`(N만, Prev 측 값 0) / `Resolved`(N-1만, Current 측 값 0).
2. 양측 존재 & 어느 한쪽 상태가 **비숫자 집합 `{NoLimit, InvalidLimit, MappingError, DuplicateLimit}`** 이거나 CurrentStatus ≠ PriorStatus → `StateTransition` (숫자 mover에서 제외, Hidden-Risk finding 추가). 이 행들은 UsageRatio Δ를 mover 랭킹에 쓰지 않는다(0除算·오해 Δ 방지). **`DuplicateLimit`은 R2-WP-01(#79)로 추가된 7번째 상태이며 반드시 비숫자 집합에 포함**(양일 DuplicateLimit 동일 상태 회귀 추가).
3. 양측이 `{Normal, Warning, Breach}` & 동일 상태 → UsageRatioDelta>0 `Increased`, <0 `Decreased`, =0 `Unchanged`.
- Δ 부호 = Current − Prior. 모든 금액 decimal, 부동소수 사용 금지.

### TopN movers
- `StateTransition`/`New`/`Resolved`를 제외한(또는 명확히 정의한 — 테스트로 고정) 숫자 이동 행 중 |UsageRatioDelta| **내림차순**, 동순위는 PortfolioId → RiskFactor **Ordinal**, 상위 `topN`.

### 출력 정렬·결정성
- ComparisonTable: PortfolioId → RiskFactor Ordinal. Movers: 위 규칙. HashSet/Dictionary 열거 순서를 출력에 노출하지 마라. `IsDeterministic=true`.
- `DraftNotice`는 리터럴 상수로 "본 결과는 공식 해석이 아니라 검토용 초안입니다." 류 문구. priorBaseDate 선택 규칙·조인키 규칙·랭킹 규칙을 Methodology에 문자열로 기록. **LLM 호출 0.**

### priorBaseDate 처리 (R2-WP-01 #79 머지 후 기준 — P2-2 반영)
- priorBaseDate는 **항상 호출자가 명시**한다. 달력/영업일 자동 산출·임의 증감 **금지**.
- `LimitMonitor.Analyze`를 currentBaseDate·priorBaseDate에 **각각 개별 호출**한다. R2-WP-01이 입력 baseDate 인자를 정규화(`yyyy-MM-dd`→`yyyyMMdd`)하므로 **두 인자의 포맷이 서로 달라도**(`20260617` vs `2026-06-16`) 각 호출은 정상 동작하고, 결과 행은 `(PortfolioId,RiskFactor)`로 페어링되므로 **두 일자 포맷 차이만으로 0건이 강제되지 않는다.**
- 단, R2-WP-01은 **데이터 행의 BASE_DT 값 자체는 재해석하지 않으므로**, 어느 한 일자가 데이터의 어떤 BASE_DT와도 매칭되지 않으면(예: 파일이 `2026-06-17`로 저장, 인자는 `20260617`) 그 측 분석이 0행이 되어 전부 New(또는 Resolved)가 된다. 이 경우 **임의 보정하지 말고** `BASE_DT_FORMAT_MISMATCH`(또는 `PRIOR_DAY_NO_ROWS`) Hidden-Risk finding을 추가하되, **매칭된 행은 계속 비교**한다(전체 0건 강제 금지).
- currentBaseDate == priorBaseDate 또는 빈 값 → `ArgumentException` 또는 빈 비교표 + finding 중 하나로 결정적 처리(테스트로 고정).

## 제외 범위 (건드리지 마라)
- 차트/Heatmap/시각화·Excel Report 강화 → R2-WP-04.
- 중복키 차단·통화/단위·RECON_UNIT·BASE_DT 정규화 → R2-WP-01.
- Streaming/Welford → R2-WP-02.
- `LimitMonitor`/`LimitAnalysisResult`/`LimitMonitorRow`/`LimitAnalysisKpis`/`LimitMonitorStatus`의 시그니처/의미 변경. (단, 위 `BuildComparisonKey` 비파괴 추가만 허용.)
- Dashboard UI 연결·가중치 학습.

## 테스트 (신규 — `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`에 추가)
어서션 메시지에 "prior-day"/"limit"/"LimitMonitor" 키워드를 넣어 도메인 분류가 Limit/Reconciliation이 되게 하라(Unclassified=0). 최소 6개:
1. prior-day comparison: 양측 존재 시 Current/Prev/Δ 정확 + Increased/Decreased/Unchanged 정확.
2. prior-day New/Resolved movers: N만/N-1만 키 분류 + 결측 측 0.
3. prior-day TopN movers ordering: |UsageRatio Δ| desc + 동순위 PortfolioId→RiskFactor.
4. prior-day state-transition non-numeric mover: Normal→NoLimit이 StateTransition으로 분류 + Hidden-Risk finding, mover 랭킹 제외. **+ 양일 DuplicateLimit 동일 상태 회귀**(비숫자 처리, mover 제외).
5. prior-day BASE_DT mismatch: 한 일자가 데이터 BASE_DT와 미매칭(해당 측 0행) → 그 측 New/Resolved + `BASE_DT_FORMAT_MISMATCH` finding(보정 없음); **매칭된 행은 계속 비교**(두 일자 포맷 차이만으로 전체 0건 강제 안 함, P2-2).
6. prior-day 4-section contract deterministic: 동일 입력 2회 동일 출력(서명 비교) + DraftNotice 존재 + `Current`/`Prior`의 **7상태** 카운트(NormalCount 등) 보존 확인. **+ 한도만 변경(노출 불변) 시 LimitAmountDelta≠0·ExposureAmountDelta=0 회귀**(P2-3).
- 더미 fixture는 `SmokeTestHelpers.CreateReportSmokeInputs` 패턴 재사용(헬퍼를 추가하되 기존 헬퍼는 변경하지 마라). 두 BASE_DT(예 `20260616`/`20260617`)의 exposure/limit CSV를 만들어 N/N-1 행을 구성.

## 완료 조건 / 보고
로컬에서:
```
dotnet build
dotnet run --project tests/RiskManagementAI.SmokeTests
```
- `Total=N PASS / 0 FAIL`(N = 기존+신규), `Unclassified=0` 출력 캡처를 보고에 포함.
- 외부 NuGet 0 유지(`.csproj`에 PackageReference 추가 없음) 확인.
- 결정성(동일 입력 동일 출력) 확인.
- R1 계약(**7상태**·RECON_*·LimitAnalysisResult·Dashboard=Report) 비파괴 확인.
- 외부 의존성·시각화·LLM·통계 라이브러리가 필요해지면 **즉시 STOP하고 사유 보고**(구현 중단).
- Branch: `feature/r2-wp-03-prior-day-analytics`
- Commit: `feat: prior-day analytics (current/prev/delta, TopN movers, 4-section contract) (R2-WP-03)`
- **Claude 승인 전 main 머지 금지.**

## Codex 리뷰 반영 (P2 — 필수 준수) — ↑ 본문 §작업범위/분류규칙/priorBaseDate/테스트에 통합 반영 완료(중복 기재이나 보존)
- **(P2) DuplicateLimit 비숫자 전이**: R2-WP-01 후 `LimitMonitorStatus`에 `DuplicateLimit` 추가. 숫자 mover {Normal,Warning,Breach}에서 제외하는 **비숫자 상태 집합에 `DuplicateLimit` 포함**(NoLimit/InvalidLimit/MappingError와 함께). 양일 DuplicateLimit 동일 상태 회귀 추가.
- **(P2) 날짜 형식 차이로 0건 강제 금지**: 분석기는 현재일/전일에 `LimitMonitor.Analyze`를 **개별 호출**하고 `MonitoringTable` 행을 `(PortfolioId,RiskFactor)`로 페어링하므로 BASE_DT 형식이 달라도(예 `20260617` vs `2026-06-16`) 선택된 행은 비교 가능. **형식 불일치만으로 `0건` 강제 금지** — Hidden-Risk는 **빈/실패한 날짜 선택**에 기반하거나 경고하되 실제 선택 행은 계속 비교.
- **(P2) 한도 델타 계약 포함**: `PriorDayComparisonRow` 계약에 **Current/Prior/Δ `LimitAmount` 필드 추가**(Exposure·Limit·UsageRatio·RemainingLimit 전부). 한도만 변경(노출 불변) 회귀 추가.
