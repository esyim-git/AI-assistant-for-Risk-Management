# Codex WP-06 — 대사·예외검증 9종 (Reconciliation)

> 권위 스펙: `docs/39` WP-06, `docs/41 §1`(Data Gate 대사 항목), `docs/38` RR-04. Release: R1. 선행: WP-05(완료, `LimitAnalysisResult`/`LimitException`).

## 목표
WP-05의 실 조인 위에 **대사(reconciliation)·예외검증 9종**을 얹어, 데이터 품질 이상을 **결정적 예외(`LimitException`)**로 노출한다. 핵심은 **원천합계 = 분석합계 대사(증폭/누락 0)**. 합성값 미사용·읽기 전용·NuGet 0.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4`, `docs/39`(WP-06·WP-05), `docs/41 §1`, `docs/38`(RR-04), 기존 `Core/Risk/LimitMonitor.cs`·`LimitAnalysisResult.cs`(WP-05 결과 구조).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-06-reconciliation origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- WP-05 분석 직후 **대사 패스**를 추가해 `ExceptionList`를 9종 코드로 채우고, **`ReconciliationSummary`**(PASS/FAIL + 코드별 건수)를 `LimitAnalysisResult`에 추가한다.
- **기존 6상태 분류·KPI·수치는 변경 금지**(대사는 *추가* 정보; 조인 로직 재작성 아님).
- **제외**: Dashboard·Report UI 일원화(WP-07), 새 입력형식, 전일대비(WP-09).

## 대사 9종 (각 → `LimitException` 코드, 결정적)
1. **RECON_EXPOSURE_NO_LIMIT** — 노출에 매칭 한도 없음(조인 미스 = WP-05 `NoLimit`). Sev=Medium.
2. **RECON_LIMIT_NO_EXPOSURE** — 한도 행(해당 기준일)에 매칭 노출 없음(미사용/고아 한도). Sev=Low.
3. **RECON_DUPLICATE_LIMIT** — 동일 기준일·동일 Join Key에 **한도 행 2건 이상**(WP-05는 `.Last()`로 조용히 선택 → 여기서 명시 예외). 중복 key별 1건. Sev=Medium.
4. **RECON_BASEDATE_MISMATCH** — 입력에 **요청 기준일과 다른 BASE_DT** 행 존재(노출/한도 각각). Sev=Low(정보성).
5. **RECON_CURRENCY_MISMATCH** — 매칭된 노출/한도의 통화 상이. **조건부**: 노출·한도 *양쪽 모두* 통화 컬럼이 있을 때만 비교(한도에 통화 컬럼 없으면 **N/A**, false positive 금지). Sev=Medium.
6. **RECON_UNIT_MISMATCH** — 단위/스케일 상이. **조건부·보수적**: 노출·한도 *양쪽 모두* 단위/스케일 컬럼(매핑/설정으로 식별)이 있을 때만 비교; 없으면 **N/A**(금액 크기차 추정만으로 단위불일치 단정 금지). Sev=Medium.
7. **RECON_NONPOSITIVE_LIMIT** — 한도 `≤0`(또는 비숫자). WP-05 `InvalidLimit`와 대사 관점 중복 노출(코드만 분리). Sev=Medium.
8. **RECON_ROW_AMPLIFICATION** — **분석 모니터링 행 수 > 원천 노출 행 수**(중복 Join Key fan-out 등). Sev=High.
9. **RECON_SUM_BALANCE (키스톤)** — **Σ 원천 노출금액(기준일 필터 후) = Σ 분석 노출금액**. 불일치(증폭/누락) 시 예외. Sev=High. (docs/41: 원천합계=분석합계 PASS 필수.)

> 통화/단위(5·6)는 **필요 컬럼이 없으면 N/A로 건너뛴다**(예외 0). 데이터 없는 검증을 지어내지 말 것.

## Public Interface / 데이터 모델
- `LimitAnalysisResult`에 `ReconciliationSummary Reconciliation` 추가(기존 필드 유지·하위호환).
- `ReconciliationSummary`: `{ bool Passed; int CheckCount(=9); IReadOnlyList<ReconciliationCheck> Checks }`; `ReconciliationCheck`: `{ string Code; bool Applicable; int ExceptionCount; SafetySeverity MaxSeverity }`.
- **`Passed` 정의**: High 심각도 대사 위반(8·9, 그리고 발생 시 7) 0건이면 PASS. (Low/Medium 정보성 예외는 PASS를 깨지 않음 — 단, 9 SUM_BALANCE 불일치는 FAIL.)
- 9종 예외는 WP-05 `ExceptionList`에 합류(코드로 구분). `Findings`에도 요약 finding 1건(예: `RECON_SUMMARY` + PASS/FAIL).

## 구현 세부 / 결정성
- **결정적**: 동일 입력 → 동일 예외 집합·동일 순서(코드·키 정렬 고정). 부동소수 합계 주의 — 금액은 `decimal` 그대로 합산(WP-05와 동일).
- **SUM_BALANCE**: 원천 = 기준일 필터된 노출금액 합(매핑 ExposureAmount, 숫자 파싱 가능 행). 분석 = 모니터링 테이블의 ExposureAmount 합. MappingError/비숫자 행은 **누락으로 집계**(차이로 드러나야 함).
- **건수증폭**: 정상 조인은 노출 1행→모니터링 1행. 증폭은 한도 측 중복키가 노출 측으로 fan-out될 때만 — 현 구조(한도 dictionary)에선 발생하지 않아야 정상이므로, **증폭 0을 회귀로 고정**하고 인위적 중복 입력으로 탐지 검증.
- 합성 미사용·읽기 전용·외부 0·NuGet 0. 경로 가드는 WP-05 reader 경유.
- 기존 335 SmokeTest **유지**(대사는 추가 필드라 기존 수치·상태 불변).

## 테스트(필수)
- 9종 각각 **양성 케이스**(해당 이상 주입 → 정확한 코드 예외) + **음성 케이스**(정상 입력 → 해당 예외 0).
- 통화/단위: 컬럼 없을 때 **N/A(예외 0)**, 양쪽 있고 다를 때만 예외.
- **SUM_BALANCE**: 정상 = 원천합계=분석합계 PASS; MappingError/비숫자 누락 시 FAIL로 드러남.
- **건수증폭**: 정상 증폭 0; 한도 중복키 주입 시 DUPLICATE_LIMIT + (해당 시) AMPLIFICATION.
- 결정성: 반복 호출 동일 `ReconciliationSummary`.
- 기존 한도/6상태 테스트 수치 불변.

## 완료/보고
`LimitAnalysisResult.Reconciliation` 9종 동작 + 원천=분석 합계 PASS. build 0/0 · SmokeTest 기존 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/39` 원장 갱신.

## Claude Review Checklist
9종 코드 정확/통화·단위 N/A 보수처리/SUM_BALANCE 키스톤/건수증폭 0 회귀/결정성/기존 6상태·수치 불변/합성 미사용/Gate A.
