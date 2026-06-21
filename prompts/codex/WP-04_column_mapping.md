# Codex WP-04 — Risk Column Mapping (설정·승인형)

> 권위 스펙: `docs/39` WP-04, `docs/41 §1`(Data Spec Gate), `docs/40` ADR-002. Release: R1. 선행: WP-02/03 완료(공통 `CsvTable`).

## 목표
리스크 분석이 쓰는 하드코딩 컬럼명(`BASE_DT`/`PORTFOLIO_ID`/`RISK_FACTOR`/`EXPOSURE_AMT`/`LIMIT_AMT`/`USE_YN`)을 **논리명 → 물리컬럼 매핑**으로 구성 가능하게 한다. 기본 매핑은 **현재 상수와 100% 동일**(동작 무변경). Join Key(BASE_DT·PORTFOLIO_ID·RISK_FACTOR)도 매핑으로 바뀐다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4`, `docs/39`(WP-04), `docs/41 §1`(Data Gate), `docs/40`(ADR-002), 기존 `Core/Config/PolicyLoader.cs`(결과타입 패턴 참고), `Core/Risk/LimitMonitor.cs` L9-14 / `Core/Data/DataProfiler.cs` L8(대체 대상 상수).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-04-column-mapping origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0 유지.

## 작업 범위 / 제외
- 신규: `Core/Mapping/ColumnMapping.cs`, `Core/Mapping/ColumnMappingLoader.cs`, `config/column_mapping.json`(기본 매핑), tests.
- `LimitMonitor`·`DataProfiler`가 **상수 직접참조를 제거**하고 매핑을 통해 컬럼 접근.
- 제외: 실 Exposure-Limit Join 엔진(WP-05), 대사 9종(WP-06), 인코딩(WP-02)/XLSX(WP-03).

## Public Interface (PolicyLoader 패턴 그대로)
```csharp
public enum LogicalColumn { BaseDate, PortfolioId, RiskFactor, ExposureAmount, LimitAmount, UseYn }

public sealed record ColumnMappingLoadResult(ColumnMapping Mapping, bool UsedFallback, IReadOnlyList<string> Warnings);

ColumnMappingLoadResult ColumnMappingLoader.LoadDefault();          // config/column_mapping.json 또는 안전기본
string ColumnMapping.Physical(LogicalColumn col);                  // 미매핑이면 명시 예외/finding
```
- **반드시 `ColumnMappingLoadResult`로 반환**(bare `ColumnMapping` 금지) — 호출자·테스트·audit가 *커스텀 매핑 로드* vs *기본 폴백*을 식별 가능해야 함. `PolicyLoader`의 `UsedFallback`/`Warnings`/SafeDefaults 패턴과 동일.

## 구현 세부 / 보안
- **기본값 = 현재 상수**: BaseDate→`BASE_DT`, PortfolioId→`PORTFOLIO_ID`, RiskFactor→`RISK_FACTOR`, ExposureAmount→`EXPOSURE_AMT`, LimitAmount→`LIMIT_AMT`, UseYn→`USE_YN`. 파일 없거나 손상 시 이 기본으로 **safe fallback + 경고**(throw 금지, PolicyLoader와 동일).
- `config/column_mapping.json` 있으면 override. **필수 논리컬럼(6종) 누락/중복/빈값 → 기본으로 fallback + 경고**(`UsedFallback=true`). 부분 override는 허용하되 누락분은 기본 사용 + 경고.
- **경로 가드**: `config/`만 읽는다(`PolicyLoader`/`LogPathResolver` 패턴). 임의/상위경로(`..`)·rooted 경로 거부. 외부 호출 0.
- 미매핑 `LogicalColumn` 접근 → 명시적 `InvalidDataException`(또는 finding). 매핑값에 민감정보/실데이터 금지.
- `LimitMonitor`/`DataProfiler`는 로드된 매핑으로 컬럼 접근. **기존 public 동작·시그니처는 유지**(기본 매핑이라 수치·상태셋 무변경).

## ⚠️ Data Spec Gate 정렬 (docs/41 §1)
- 본 WP의 **기본 매핑은 "현재 코드 상수와 동일"이므로 동작 변화 0** → WP-04 PR은 **자기 DoD + 게이트 A + 자기 테스트**로 머지 가능(개별 PR이 Data Gate 전체를 통과할 필요 없음, big-bang 금지).
- 단, **기본과 다른 커스텀 매핑/Join Key 변경은 "승인된 규칙"으로만** 유효(Data Gate, R1 마감 검토 대상). 그래서 `config/column_mapping.json`은 *승인된 기본 baseline*만 담고, 운영 커스텀 매핑은 repo에 넣지 않는다(주석으로 명시).

## 테스트(필수)
- 기본 매핑 = **현 동작 동일**(기존 LimitMonitor/DataProfiler 회귀 그대로 PASS).
- 커스텀 매핑 적용: 물리 컬럼명이 다른 CSV가 매핑으로 정상 처리(예: `PORT_ID`→PortfolioId)되어 동일 결과.
- 필수 논리컬럼 누락 → **fallback + 경고**(`UsedFallback=true`, `Warnings` 비어있지 않음).
- 경로 가드: `config/` 밖/`..` 경로 거부.
- 미매핑 `LogicalColumn` 접근 → 명시 예외.

## 완료/보고
- `LimitMonitor`/`DataProfiler`에 컬럼명 상수 직접참조 0(매핑 경유). build 0/0 · SmokeTest 기존 유지 + 신규 · NuGet 0 · 게이트 A 0건 · `docs/39` 원장 갱신.
