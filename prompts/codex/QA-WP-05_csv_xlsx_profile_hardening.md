# Codex Prompt — QA-WP-05: Csv/Xlsx/DataProfile SmokeTest 하드닝 (CP949·streaming 상한·프로파일 경계)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-05) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-05-csv-xlsx-profile-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-05, `SKILLS.md`+`risk-data-limit-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/Data/*`(`CsvReader`·`Cp949Decoder`·`XlsxReader`·`DataProfiler`·streaming 상한), `tests/RiskManagementAI.SmokeTests/{CsvTests,XlsxTests,DataProfileTests}.cs`.
> **기준선**: main `d8cb415`(VERSION 0.7.0), 정본 SmokeTest `Total=861 PASS=861 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
CP949/UTF-8/XLSX 입력·streaming 상한·DataProfile의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드 변경 0** — 테스트만. R2-WP-02/05 결정성·상한·parity 불변을 회귀로 잠근다.

## 1. 작업 범위 (CsvTests·XlsxTests·DataProfileTests — additive only)
1. `Core/Data/*`와 현 3개 test를 대조 → **미커버 경계만** 추가.
2. 후보(제품 동작 확인·신규 동작 요구 아님):
   - **CP949(UHC)** 경계 바이트·불완전 멀티바이트 graceful·UTF-8 BOM/무BOM·`Decode(byte[])`==`DecodeLines(Stream)` 바이트 동일(streaming 결정성).
   - **streaming 상한**: 행 `MaxRowCount`/바이트 `MaxByteSize` 초과 → `InvalidDataException(max=/actual=)`; 경계값(상한-1·상한).
   - **XLSX**: 인박스 OOXML·zip 안전상한·관계기반 시트해석·손상/비정상 graceful.
   - **DataProfile**: streaming==in-memory 전필드 동일·Welford 3σ Outlier(legacy 2-pass parity)·`DuplicateRowCount`(SHA256 해시·원문 미저장)·null/numeric/BASE_DT 분포 경계·decimal overflow graceful.
3. **합성 더미만** — 실데이터·실 테이블·컬럼명 0(인박스 매핑표·합성 CSV/XLSX만).
> ⚠️ 3개 test 파일을 동시 편집하나 모두 `tests/`이고 다른 WP와 겹치지 않는다. 대용량 fixture는 **코드 생성**(대용량 바이너리 파일 repo 추가 금지 — 상한 테스트는 in-memory 생성 스트림으로).

## 2. 제외 범위
`CsvReader`/`Cp949Decoder`/`XlsxReader`/`DataProfiler` 제품 코드 변경. 신규 인코딩·리더. 기존 단언 수정/삭제/약화. 대용량 바이너리 fixture 파일 추가. 신규 NuGet.

## 3. 보안조건
합성 더미·in-memory 스트림만(실데이터 0·대용량 파일 repo 추가 0) · CP949 매핑표는 기존 인박스 재사용(신규 매핑 0) · NuGet 0 · **기존 테스트 삭제·약화 0**.

## 4. 테스트 (SmokeTest — 도메인 `Csv`/`Xlsx`/`DataProfile`)
> `SmokeTestContext.SmokeDomain`: Xlsx(line ~51: `XlsxReader`/`.xlsx`/`xlsx`) 최상단 · Csv(line ~52: `CsvReader`/`CP949`/`UTF-8`/`BOM`/`encoding`/`CSV parser`) · DataProfile(line ~64: `DataProfiler`/`profile`/`null values`/`duplicate rows`/`numeric`/`BASE_DT distribution`/`source file name`). 각 파일의 신규 단언 설명을 해당 도메인 토큰으로. 주의: Mapping(`mapping`/`mapped`)·Reconciliation·Limit·Report가 Csv/DataProfile보다 위이므로 그 토큰 회피(단 Xlsx/Csv는 최상단이라 안전). `Unclassified=0`.
- 각 경계 → 기대값(디코드 바이트 동일·상한 예외·parity·`DuplicateRowCount`·분포) 단언.
- 기존 `CsvTests`/`XlsxTests`/`DataProfileTests` 단언 **전부 보존**. 종료부 **`Total=861 → 861+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Csv/Xlsx/DataProfile 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-05-csv-xlsx-profile-hardening` · Commit: `test: harden csv/xlsx/dataprofile boundary coverage (QA-WP-05)`

## 6. Claude Review Checklist
제품 코드 변경 0(테스트만) / 추가는 실제 미커버 경계 / CP949 바이트 동일·streaming 상한 예외·Outlier parity·`DuplicateRowCount` 기대값 정확 / 합성 더미·in-memory(실데이터·대용량 파일 0) / 도메인 Csv/Xlsx/DataProfile·Unclassified 0 / 기존 3 test 보존·감소 0 / NuGet 0 / `Total` 861 보존+신규 / Gate A.
