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
4. **RECON_BASEDATE_MISMATCH** — 요청 기준일이 입력에 **존재하지 않는데**(노출 또는 한도에 해당 기준일 행 0건) 다른 기준일 행은 있음 → **잘못된 기준일 선택 신호**. ⚠️ **정상 멀티-기준일 export(요청일 + 과거일 동시 포함)는 정상 → 예외 0**(다른 날짜 행 존재 자체를 mismatch로 보지 않는다; WP-05가 기준일 필터를 지원함). Sev=Low(정보성).
5. **RECON_CURRENCY_MISMATCH** — 매칭된 노출/한도의 통화 상이. **R1에서는 `Applicable=false`(N/A)**: 한도 측에 통화 컬럼이 없어 비교 불가. **구조만 구현**(노출·한도 *양쪽 모두* 통화 컬럼이 있을 때만 활성). **R1 양성 테스트 불요**(`Applicable=false`·예외 0만 확인). 실제 활성화는 승인된 통화 컬럼 추가 시(Data Gate). Sev=Medium.
6. **RECON_UNIT_MISMATCH** — 단위/스케일 상이. **R1에서는 `Applicable=false`(N/A)**: ColumnMapping(6 논리컬럼)에 단위/스케일 컬럼이 **정의돼 있지 않음** → 비교 대상 부재. **구조만 구현**. **R1 양성 테스트 불요**. 활성화 조건: 승인된 단위/스케일 논리컬럼을 매핑에 추가(Data Gate·WP-04 확장) — 그 전까지 **금액 크기차 추정으로 단위불일치 단정 금지**. Sev=Medium.
7. **RECON_NONPOSITIVE_LIMIT** — 한도 `≤0`(또는 비숫자). WP-05 `InvalidLimit`와 대사 관점 중복(코드 분리). **PASS/FAIL fail-code에 포함**(아래 `Passed` 정의 — 코드 기반이라 Medium이어도 확실히 FAIL). Sev=Medium.
8. **RECON_ROW_AMPLIFICATION** — **분석 모니터링 행 수 > 기준일-필터된 노출 행 수**(= 분석이 쓰는 동일 모집단). 과거일 history 행 때문에 fan-out이 가려지지 않도록 **반드시 기준일 필터 후 모집단과 비교**(raw 전체 행수와 비교 금지). Sev=High.
9. **RECON_SUM_BALANCE (키스톤)** — 기준일 필터된 노출에 대해 **(Σ 원천 노출금액 = Σ 분석 노출금액) AND (누락 행 0)**. **누락 = 비숫자/MappingError로 분석에서 0 처리된 행**. 양쪽 0 상쇄로 통과하지 못하도록 **누락 건수>0이면 합계 일치와 무관하게 FAIL**. Sev=High. (docs/41: 원천합계=분석합계, **증폭/누락 0**.)

> 통화/단위(5·6)는 R1에서 **`Applicable=false`(N/A)가 정상** — 필요 컬럼이 데이터/매핑에 없으면 검증을 지어내지 말 것. 양성 테스트는 컬럼 도입(승인) 후 추가.

## Public Interface / 데이터 모델
- `LimitAnalysisResult`에 `ReconciliationSummary Reconciliation` 추가(기존 필드 유지·하위호환).
- `ReconciliationSummary`: `{ bool Passed; int CheckCount(=9); IReadOnlyList<ReconciliationCheck> Checks }`; `ReconciliationCheck`: `{ string Code; bool Applicable; int ExceptionCount; SafetySeverity MaxSeverity }`.
- **`Passed` 정의 (코드 기반, 심각도 비의존)**: **fail-code 집합 `{ RECON_NONPOSITIVE_LIMIT, RECON_ROW_AMPLIFICATION, RECON_SUM_BALANCE }`** 중 하나라도 발생하면 **FAIL**. (코드 기반이라 `NONPOSITIVE_LIMIT`이 Medium이어도 확실히 FAIL시킴 — 심각도 기반 집계 금지.) 나머지(1·2·3·4, N/A인 5·6)는 정보성 → PASS를 깨지 않음.
- 9종 예외는 WP-05 `ExceptionList`에 합류(코드로 구분). `Findings`에도 요약 finding 1건(예: `RECON_SUMMARY` + PASS/FAIL).

## 구현 세부 / 결정성
- **결정적**: 동일 입력 → 동일 예외 집합·동일 순서(코드·키 정렬 고정). 부동소수 합계 주의 — 금액은 `decimal` 그대로 합산(WP-05와 동일).
- **SUM_BALANCE**: 원천 = **기준일 필터된 노출 행** ExposureAmount 합(숫자 파싱). 분석 = 모니터링 테이블 ExposureAmount 합. **추가로 "누락 건수"를 별도 집계** = 기준일 필터된 노출 행 중 **비숫자/MappingError로 0 처리된 행 수**. **PASS = (합계 일치) AND (누락 0)**; **누락>0이면 합계가 우연히 같아도 FAIL**(양쪽 0 상쇄 통과 차단).
- **건수증폭**: 정상 조인은 (기준일 필터된) 노출 1행→모니터링 1행. **비교 모집단은 raw 전체가 아니라 기준일-필터된 노출 행 수**(과거일 history가 fan-out을 가리지 않게). 정상 **증폭 0을 회귀로 고정**하고 한도 중복키 fan-out 입력으로 탐지 검증.
- 합성 미사용·읽기 전용·외부 0·NuGet 0. 경로 가드는 WP-05 reader 경유.
- 기존 335 SmokeTest **유지**(대사는 추가 필드라 기존 수치·상태 불변).

## 테스트(필수)
- **데이터로 검증 가능한 7종(1·2·3·4·7·8·9)**: 각 **양성 케이스**(이상 주입 → 정확한 코드 예외) + **음성 케이스**(정상 입력 → 예외 0).
- **5·6(통화/단위)은 R1 N/A**: `Applicable=false`·예외 0만 확인(**양성 테스트 불요** — 비교 컬럼이 모델에 없음).
- **BASEDATE_MISMATCH**: 멀티-기준일 정상 export(요청일 + 과거일 포함) → **예외 0**; 요청 기준일이 입력에 아예 없을 때만 예외.
- **SUM_BALANCE**: 정상 = 합계 일치 + 누락 0 → PASS; **비숫자/MappingError 노출 주입 → 누락>0 → FAIL**(양쪽 0 상쇄로 PASS되지 않음).
- **건수증폭**: 기준일-필터 모집단 대비 정상 0(과거일 history 있어도); 한도 중복키 fan-out 주입 시 DUPLICATE_LIMIT + AMPLIFICATION.
- 결정성: 반복 호출 동일 `ReconciliationSummary`.
- 기존 한도/6상태 테스트 수치 불변.

## 완료/보고
`LimitAnalysisResult.Reconciliation` 9종 동작 + 원천=분석 합계 PASS. build 0/0 · SmokeTest 기존 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/39` 원장 갱신.

## Claude Review Checklist
9종 코드 정확/통화·단위 N/A 보수처리/SUM_BALANCE 키스톤/건수증폭 0 회귀/결정성/기존 6상태·수치 불변/합성 미사용/Gate A.
