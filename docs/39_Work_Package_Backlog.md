# 39. Work Package Backlog (R1 DONE · R3 DONE · post-v0.6 NEXT)

> `docs/38` Release Train의 실행 단위(Work Package). Codex는 WP 단위로 구현하고, Claude는 WP별 Review Checklist로 검증한다.
> WP 형식: 목표·선행조건·작업범위·제외범위·읽을문서·수정예상파일·Public Interface·구현세부·보안조건·테스트·완료조건·Branch·Commit·Claude Review Checklist.
> 각 WP는 **하나의 명확한 목표**만 가진다. R1 Codex 프롬프트: `prompts/codex/WP-XX_*.md`.

---

## ★ Resume Brief (Codex 인수 — v0.6.0 기준선)
- **현재 기준선**: main `3dfa80b`, VERSION **0.6.0**. R1(WP-01~08) **DONE**, R3(R3-WP-01~05) **DONE**, REL-v0.6 패키징 가드(#54) **DONE**. SmokeTest **ALL PASS / 0 FAIL** (CI `27926096336`, windows-latest).
- **NEXT UP (Codex가 집을 단 하나의 WP)**: **`STAB-WP-01` Build/Version Reproducibility** → 프롬프트 `prompts/codex/STAB-WP-01_build_version_reproducibility.md`. (이유: `build/01~03` 기본 `-Version`이 `0.2.0`으로 남아 VERSION 0.6.0과 불일치 → 오버전 산출물 위험 RR-11. 모든 후속 Release/Gate 신뢰의 선행.)
- **그 다음 후보(순서, NEXT UP 아님)**: STAB-WP-02(정본 테스트 베이스라인) → STAB-WP-03(Release 보안+Integrity Manifest) → STAB-WP-04(테스트 구조 분리) → R2-WP-01(Risk Semantic Hardening).
- **BLOCKED**: PILOT Gate B/C(실 Test PC 증거 대기 — `docs/45`). 신규 기능과 분리해 user/Test PC가 병행.
- **재현 검증**: `git fetch origin main && git switch main && dotnet build RiskManagementAI.sln -c Release && dotnet run --project tests/RiskManagementAI.SmokeTests` → ALL PASS 확인. (정본 합계 출력은 STAB-WP-02에서 추가.)
- **⚠️ Archived(재실행 금지) 프롬프트**: `prompts/codex_mvp1_implementation_prompt.md`, `prompts/codex_mvp2_*`, `prompts/codex_mvp3_ui_prompt.md`, `prompts/codex_goal_mode_prompt.md`, `prompts/claude_bootstrap_v2_prompt.md`, `prompts/codex/WP-01~07_*`, `prompts/codex/R3-WP-01~05_*`, `prompts/codex/REL-v0.6-packaging-guard.md` — 모두 **완료/Starter** 단계. 신규 작업은 본 Resume Brief의 NEXT UP만 따른다.

## R1 진행 원장 (Codex 갱신)
| WP | 목표 | 상태 | PR/커밋 | SmokeTest | 비고 |
|---|---|---|---|---|---|
| WP-01 | 합성 한도 차단 / DEMO_ONLY | DONE | `feature/wp-01-demo-limit-guard` | 278 PASS / 0 FAIL | 합성 1.1x 산식 제거, `LIMIT_DATA_REQUIRED`/`DEMO_ONLY` 회귀 고정 |
| WP-02 | 인코딩 인식 CSV Reader(CP949/UTF-8) | DONE | `feature/wp-02-csv-encoding` | 296 PASS / 0 FAIL | `CsvReader` 공통화, CP949 Path A(UHC 전체 매핑표·SHA256 검증), UTF-8 BOM/무BOM |
| WP-03 | XLSX 입력 Reader(인박스, NuGet 0) | DONE | `feature/wp-03-xlsx-input` | 308 PASS / 0 FAIL | `XlsxReader` → `CsvTable`, workbook 관계 기반 시트 해석, zip 안전상한 |
| WP-04 | Risk Column Mapping(설정·승인형) | DONE | `feature/wp-04-column-mapping` | 322 PASS / 0 FAIL | 기본=현행 호환, 커스텀 all-or-nothing, Data Gate |
| WP-05 | 실 Exposure-Limit Join + 공통 AnalysisResult | DONE | `feature/wp-05-join-analysis-result` | 335 PASS / 0 FAIL | RR-03, GitHub Actions 기준 |
| WP-06 | 대사·예외검증 9종 | DONE | `feature/wp-06-reconciliation` | 357 PASS / 0 FAIL | RR-04, GitHub Actions 기준 |
| WP-07 | Dashboard·Report 공통화 | DONE | `feature/wp-07-dashboard-report-unify` | 368 PASS / 0 FAIL | RR-03, GitHub Actions 기준 |
| WP-08 | 공통 CSV 파서 통합(3중 중복 제거) | DONE | `feature/wp-02-csv-encoding` | 296 PASS / 0 FAIL | WP-02에 흡수: DataProfiler/LimitMonitor/RegulationCatalog 공통 `CsvReader` 사용 |
| WP-09 | 전일 대비 데이터모델(설계) | TODO | - | - | R2 구현 |

---

## WP-01. 합성 한도 차단 / DEMO_ONLY (RR-01, 최우선)

- **목표**: UI가 노출의 1.1배로 **한도를 합성**(`BuildUiLimitRows`, `MainWindow.xaml.cs` `limitAmount = Math.Max(Math.Abs(exposureAmount)*1.1m, 1m)`)하는 로직을 제거하고, 실제 한도 데이터가 없으면 **DEMO_ONLY로 명시·차단**한다(합성값을 실값처럼 쓰지 않는다).
- **선행조건**: 없음(즉시).
- **작업범위**: 합성 한도 생성 제거. Report/Dashboard가 한도 없이 호출되면 `DEMO_ONLY`/`LIMIT_DATA_REQUIRED` finding(High)으로 안내하고 합성 수치 미생성. (실제 조인은 WP-05에서 도입.)
- **제외범위**: 새 Join 엔진(WP-05), 인코딩(WP-02), 매핑(WP-04).
- **읽을문서**: `docs/38`(RR-01), `docs/30`, `CLAUDE.md §4·§13`.
- **수정예상파일**: `App/MainWindow.xaml.cs`(BuildUiLimitRows 제거/대체), `tests/.../Program.cs`.
- **Public Interface**: 없음(내부 동작 변경). Report 입력은 한도 미존재 시 빈 목록 + 안내 finding.
- **구현세부**: 합성 분기 삭제. 한도 소스가 없으면 `new SafetyFinding("LIMIT_DATA_REQUIRED", High, "실제 한도 데이터가 필요합니다. (데모 합성 한도는 사용하지 않습니다)")`. 데모 샘플 사용 시 결과에 `DEMO_ONLY` 표식.
- **보안조건**: 합성 수치를 audit log/리포트에 실값처럼 기록 금지. 읽기 전용 유지.
- **테스트**: 한도 미제공 시 합성 1.1× 행 **미생성** + `LIMIT_DATA_REQUIRED` finding. 데모 데이터 경로는 `DEMO_ONLY` 표식.
- **완료조건**: 코드베이스에 합성 한도 산식 0개. build+Smoke(기존 유지+신규).
- **Branch**: `feature/wp-01-demo-limit-guard` · **Commit**: `fix: block synthetic limit, mark DEMO_ONLY (WP-01)`
- **Claude Review Checklist**: 합성 산식 제거 확인 / 한도 없음 경로 안내·미생성 / DEMO 표식 / 268 SmokeTest 유지 / Gate A.

## WP-02. 인코딩 인식 CSV Reader (CP949/UTF-8) (RR-02)

- **목표**: 현재 UTF-8 전용 리더를 **CP949(EUC-KR)/UTF-8 모두** 지원하도록 한다(Golden6 export 다수가 CP949).
- **선행조건**: 없음.
- **작업범위**: 공통 `CsvReader`(인코딩 자동감지 또는 명시 지정) 신설. `DataProfiler`/`LimitMonitor`/`RegulationCatalog`의 중복 CSV 파서를 이 리더로 수렴(WP-08 흡수). BOM 감지 + CP949 fallback.
- **제외범위**: XLSX(WP-03), 매핑(WP-04).
- **읽을문서**: `docs/38`, `docs/03_DataCatalog.md`.
- **수정예상파일**: `Core/Data/CsvReader.cs`(신규), `Core/Data/CsvEncoding.cs`(신규), `DataProfiler.cs`/`Risk/LimitMonitor.cs`/`Kb/RegulationCatalog.cs`(파서 위임), tests, 더미 CP949 샘플(`samples/dummy_data/*_cp949.csv`).
- **Public Interface**: `CsvTable CsvReader.Read(string path, CsvEncoding encoding = CsvEncoding.Auto)`; `enum CsvEncoding { Auto, Utf8, Cp949 }`.
- **구현세부**:
  - **WP-02a (UTF-8 공통 리더, READY)**: `CsvReader`/`CsvTable` 신설 + UTF-8(BOM/무BOM) + 3개 파서 수렴. 인코딩 판별 결과를 finding/메타로 노출.
  - **WP-02b (CP949) — 결정 완료: 경로 A 채택(repo 내장 디코더, NuGet 0 유지)** ✅ (2026-06-20 사용자 승인). net8.0에서 `Encoding.GetEncoding(949)`는 **인박스 아님**(`CodePagesEncodingProvider`=`System.Text.Encoding.CodePages` 패키지 필요) → **패키지 추가 금지**.
    - 구현: **repo 내장 자체 디코더 + 공개 표준 Windows-949(UHC/CP949) *전체* 매핑표 리소스**. 매핑표 출처는 공개 표준(예: Unicode Consortium `CP949.TXT`, 또는 WHATWG Encoding 'euc-kr' 통합 인덱스 = 전체 Windows-949/UHC).
    - ⚠️ **EUC-KR/KS X 1001 부분집합만으로는 불충분**: Golden6 export는 **Windows-949(UHC)** 다수 → KS X 1001 밖 확장 한글 음절(현대 한글 11,172자 전체) 미수록 시 정상 CP949 파일을 오디코드/실패. 따라서 path A는 **UHC 전체 매핑표**를 사용한다.
    - 매핑표 무결성: 리소스 파일 **Hash 검증**(로드 시) + 라운드트립 테스트.
  - (기각) **경로 B `System.Text.Encoding.CodePages`(MS 1st-party)** — "External NuGet: None" 불변식 깨짐(오프라인 restore·벤더링 영향) → 미채택.
- **보안조건**: 외부 라이브러리 0(NuGet 0 유지). 경로 가드(기존 readers 패턴). 외부 호출 0. 매핑표 리소스 해시 검증.
- **테스트**: CP949 한글 컬럼/값 라운드트립(**EUC-KR 범위 밖 UHC 확장 음절 포함 필수**), UTF-8(BOM/무BOM) 라운드트립, 인코딩 자동감지 결과 검증, 매핑표 해시 검증.
- **완료조건**: 3 리더가 공통 CsvReader 사용, CP949(UHC 전체) 한글 정상, NuGet 0 유지.
- **Branch**: `feature/wp-02-csv-encoding` · **Commit**: `feat: add encoding-aware CSV reader CP949/UTF-8 (WP-02)`
- **Claude Review Checklist**: WP-02a UTF-8 공통 리더 NuGet 0 / 자동감지 결정성 / 3 파서 수렴. **WP-02b**: NuGet 0 유지(패키지 미추가) / **Windows-949 UHC 전체 매핑표**(EUC-KR 부분집합 아님) / 매핑표 Hash / **확장 음절 라운드트립** / Gate A.
- **Codex 결과(2026-06-20)**: `Core/Data/CsvReader.cs`, `CsvTable`, `CsvRow`, `CsvReadMetadata`, `CsvEncoding`, `Cp949Decoder` 추가. `DataProfiler`/`LimitMonitor`/`RegulationCatalog`는 공통 reader로 수렴. CP949는 repo 내장 `Data/Resources/cp949-uhc-map.txt`(17,236 entries, SHA256 `ca2d8cb6296b659c227237955dd87ba2d212ebc6e18cfc218bacee6c232db67d` — **LF 고정**(`.gitattributes` `text eol=lf`)으로 플랫폼 무관 byte-stable)를 런타임 검증 후 사용. 더미 CP949 샘플(`samples/dummy_data/*_cp949.csv`)과 UHC 확장 음절 `힣` 라운드트립 회귀 추가. build 0/0, SmokeTest 296 PASS / 0 FAIL, NuGet 0 유지.

## WP-03. XLSX 입력 Reader (인박스, NuGet 0) (RR-08)

- **목표**: CSV 외 **.xlsx 입력**을 읽는다. 생성과 동일하게 **인박스 `System.IO.Compression` + OOXML XML 파싱**(NuGet/OpenXML SDK/Interop 0, DM-03/DU-08 정합).
- **선행조건**: WP-02(공통 테이블 모델 권장).
- **작업범위**: `XlsxReader`(첫 시트 또는 지정 시트 → `CsvTable` 동형). sharedStrings/sheet XML 파싱, 손상/비표준 graceful.
- **제외범위**: 수식 평가, 스타일, 다중시트 병합.
- **수정예상파일**: `Core/Data/XlsxReader.cs`(신규), tests, 더미 `.xlsx` 입력 샘플.
- **Public Interface**: `CsvTable XlsxReader.Read(string path, string? sheetName = null)`.
- **구현세부**: `ZipArchive`로 OOXML 파싱. **시트명 해석은 `xl/workbook.xml` + `xl/_rels/workbook.xml.rels` 관계를 통해** 한다(`sheetN.xml` 순번 ≠ 시트명/순서; 기존 writer `ExcelReportBuilder.cs` L183-196이 동일 매핑 생성). `xl/sharedStrings.xml`·inline string·숫자 처리. 손상 zip/누락 part → `InvalidDataException`(UI graceful).
- **보안조건**: 외부 라이브러리 0. zip bomb 방지(엔트리 수·크기 상한). 외부 호출 0.
- **테스트**: 정상 xlsx 파싱(헤더/값/한글), **이름지정 비-첫시트(리네임/재정렬) 정확 선택**, 손상 xlsx → 예외 graceful, 큰 시트 상한.
- **완료조건**: xlsx 입력이 CSV와 동일 분석 파이프라인에 투입 가능.
- **Branch**: `feature/wp-03-xlsx-input` · **Commit**: `feat: add in-box XLSX input reader (WP-03)`
- **Claude Review Checklist**: NuGet 0 / zip 안전(상한) / 손상 graceful / 한글 / Gate A.
- **Codex 결과(2026-06-21)**: `Core/Data/XlsxReader.cs` 추가. `ZipArchive` + OOXML XML 파싱만 사용(NuGet/OpenXML SDK/Interop 0). `xl/workbook.xml`의 sheet `r:id`와 `xl/_rels/workbook.xml.rels` 관계로 worksheet target을 해석하여 `sheetN.xml` 파일명 순번에 의존하지 않음. `sharedStrings.xml`·rich text·inline string·숫자 셀을 `CsvTable`로 변환. `DataProfiler.ProfileTable(CsvTable)` 추가로 XLSX→공통 테이블→프로파일링 경로 고정. 손상 파일, missing sheet, non-xlsx, row/column/zip size 안전상한 회귀 추가. build 0/0, SmokeTest 308 PASS / 0 FAIL, NuGet 0 유지.

## WP-04. Risk Column Mapping (설정·승인형)

- **목표**: 하드코딩 컬럼명(BASE_DT/PORTFOLIO_ID/RISK_FACTOR/EXPOSURE_AMT/LIMIT_AMT/USE_YN)을 **승인된 매핑 규칙으로 구성 가능**하게. 최종 Join Key도 매핑으로 변경 가능.
- **선행조건**: WP-02/03(테이블 모델).
- **작업범위**: `config/column_mapping.json`(논리명→물리컬럼) 로더 + 기본 매핑(현 상수와 동일) + 미매핑 검출. PolicyLoader 패턴(경로 가드·safe fallback).
- **제외범위**: Join 로직(WP-05), 대사(WP-06).
- **수정예상파일**: `Core/Mapping/ColumnMapping.cs`·`ColumnMappingLoader.cs`(신규), `config/column_mapping.json`(기본), tests.
- **Public Interface**: `ColumnMappingLoadResult ColumnMappingLoader.LoadDefault()` — **`PolicyLoader` 패턴대로 `{ ColumnMapping Mapping, bool UsedFallback, IReadOnlyList<string> Warnings }`** 반환(bare `ColumnMapping` 아님; 호출자·테스트·audit가 커스텀 매핑 로드 vs 기본 폴백을 식별 가능). `string ColumnMapping.Physical(LogicalColumn col)`; 미매핑 시 명시 예외/finding.
- **구현세부**: 기본값 = 현재 상수. 파일 있으면 override(검증: 필수 논리컬럼 누락 시 fallback+경고). 매핑 변경은 **승인된 규칙**으로만(Data Gate). 경로 가드(`config/`만).
- **보안조건**: `config/`만 읽기. 임의 경로 금지. 매핑에 민감정보 금지.
- **테스트**: 기본 매핑=현 동작, 커스텀 매핑 적용, 필수 누락→fallback+경고.
- **완료조건**: LimitMonitor/Profiler가 매핑을 통해 컬럼 접근(상수 직접참조 제거).
- **Branch**: `feature/wp-04-column-mapping` · **Commit**: `feat: configurable risk column mapping (WP-04)`
- **Claude Review Checklist**: 기본=현행 호환 / safe fallback / 경로 가드 / 미매핑 검출 / Gate A.
- **Codex 결과(2026-06-21)**: `Core/Mapping/ColumnMapping.cs`, `ColumnMappingLoader.cs`, `config/column_mapping.json` 추가. 기본 매핑은 기존 상수(`BASE_DT`/`PORTFOLIO_ID`/`RISK_FACTOR`/`EXPOSURE_AMT`/`LIMIT_AMT`/`USE_YN`)와 동일. `LoadDefault()`는 `LoadFromFile("config/column_mapping.json")`로 위임하고, 파일 누락/손상/필수 누락/빈값/물리컬럼 충돌은 기본 매핑으로 safe fallback+경고. `../`·rooted·`config/` 밖 경로는 `ArgumentException`으로 거부. `LimitMonitor`/`DataProfiler`는 매핑을 통해 기준일·조인·금액·사용여부 컬럼에 접근. 커스텀 6열 완전 매핑, 부분 매핑 fallback, 물리컬럼 충돌 fallback, 경로 가드, 미매핑 접근 예외 회귀 추가. build/SmokeTest는 CI에서 322 PASS 기준으로 확인.

## WP-05. 실 Exposure-Limit Join + 공통 AnalysisResult (RR-03)

- **목표**: `LimitMonitor`의 실제 BASE_DT+PORTFOLIO_ID+RISK_FACTOR 조인 결과를 **Dashboard와 Excel Report가 함께 쓰는 공통 분석 결과 객체**로 표준화한다(현재 Report는 합성값 사용).
- **선행조건**: WP-01·WP-04.
- **작업범위**: `LimitAnalysisResult`(공통) 정의 — KPI·LimitMonitoringTable·ExceptionList·메타. `LimitMonitor` 출력을 이 타입으로. Dashboard/Report가 동일 객체 소비(WP-07에서 UI 연결).
- **제외범위**: 대사 9종(WP-06), UI 연결(WP-07).
- **수정예상파일**: `Core/Risk/LimitAnalysisResult.cs`(신규/기존 통합), `Risk/LimitMonitor.cs`, tests.
- **Public Interface**: 매핑은 **WP-04 생성자 주입** 유지(`ColumnMapping map` 메서드 파라미터는 생성자 주입으로 대체 — 소스 이중화 금지). 코어 `LimitAnalysisResult LimitMonitor.Analyze(CsvTable exposure, CsvTable limit, string baseDate)` + 호환 경로 오버로드 `Analyze(string exposurePath, string limitPath, string baseDate)`(.csv/.xlsx). 상태 enum(PascalCase) `{ Normal, Warning, Breach, NoLimit, InvalidLimit, MappingError }`(출력 문자열 `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`).
  - 상태 정렬: NoLimit=조인 미매칭, InvalidLimit=`USE_YN≠Y` 또는 한도`≤0`/숫자아님, MappingError=매핑 물리컬럼이 입력 헤더에 없음(**graceful**, 하드 throw 금지). (현 `MissingLimit`/`InactiveLimit`에서 분리.)
- **구현세부**: 상태셋을 docs/38·docs(5절) 정의로 확장(현 `MissingLimit/InactiveLimit` → `NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`로 정렬). ABS 사용률·잔여한도 유지. 동일 입력→동일 수치 보장(결정적).
- **보안조건**: 읽기 전용. 합성 한도 미사용(WP-01). 외부 0.
- **테스트**: BASE_DT 조인, 6 상태 분류, 동일 입력→Dashboard/Report 동일 수치(WP-07 연계 전 단위검증).
- **완료조건**: 공통 `LimitAnalysisResult` 1개를 Dashboard·Report 양쪽이 소비 가능.
- **Branch**: `feature/wp-05-join-analysis-result` · **Commit**: `feat: real exposure-limit join + shared analysis result (WP-05)`
- **Claude Review Checklist**: 6 상태 / 결정성 / 합성 미사용 / 기존 한도 테스트 유지·확장 / Gate A.
- **Codex 결과(2026-06-21)**: `LimitAnalysisResult`/`LimitAnalysisKpis`/`LimitAnalysisMetadata`/`LimitException` 추가. `LimitMonitor.Analyze(CsvTable exposure, CsvTable limit, string baseDate)`를 코어 인터페이스로 승격하고 `.csv`/`.xlsx` 경로 호환 오버로드를 유지. 상태는 `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`로 표준화했으며, 물리 컬럼 미매핑은 throw 대신 `MAPPING_ERROR` 결과/예외/finding으로 graceful 처리. Dashboard 최소 호환은 공통 결과의 KPI/상태 문자열을 표시하는 수준으로 제한(WP-07에서 Report/Dashboard 완전 공통화 예정). 6상태 분류, 결정성, CsvTable vs 경로 오버로드, CSV vs XLSX 동등성, 합성 한도 미사용 회귀를 SmokeTest에 추가.

---

## WP-06~09 (요지, 상세는 R1 진행 중 확정)
- **WP-06 대사·예외검증 9종** (프롬프트 `prompts/codex/WP-06_reconciliation_checks.md`): WP-05 위에 대사 패스 추가 → `ExceptionList` 9종 코드 + `ReconciliationSummary`(PASS/FAIL). 9종: ①`RECON_EXPOSURE_NO_LIMIT` ②`RECON_LIMIT_NO_EXPOSURE` ③`RECON_DUPLICATE_LIMIT` ④`RECON_BASEDATE_MISMATCH` ⑤`RECON_CURRENCY_MISMATCH`(컬럼 없으면 N/A) ⑥`RECON_UNIT_MISMATCH`(컬럼 없으면 N/A) ⑦`RECON_NONPOSITIVE_LIMIT` ⑧`RECON_ROW_AMPLIFICATION` ⑨**`RECON_SUM_BALANCE`(키스톤: 원천합계=분석합계, 증폭/누락 0)**. 기존 6상태·KPI·수치 불변(대사는 추가 필드). **Codex 결과(2026-06-21)**: `LimitAnalysisResult`에 `ReconciliationSummary` 추가. 9개 check summary와 fail-code set(`RECON_NONPOSITIVE_LIMIT`/`RECON_ROW_AMPLIFICATION`/`RECON_SUM_BALANCE`) 적용. 7개 데이터 체크 양성/음성 회귀, 통화·단위 N/A 회귀, 정상 multi-date export의 base-date mismatch 미탐지, 반복 실행 summary 결정성, duplicate limit의 row amplification 감지 추가.
- **WP-07 Dashboard·Report 공통화** (프롬프트 `prompts/codex/WP-07_dashboard_report_unify.md`, **R1 마지막**): 공통 `LimitAnalysisResult` 하나로 Risk Dashboard 그리드 + Excel Report(LIMIT_MONITORING·EXCEPTION_LIST·SUMMARY KPI/대사) + History/Audit를 **동일 수치**로 생성. `ExcelReportRequest`가 `LimitRows` 대신 **`LimitAnalysisResult` 입력**; Report는 6상태 `StatusCode`·`UsageRatio`를 **재계산 없이** 사용. **`BuildUiLimitRows`·`ExcelReportLimitRow`·3상태 `CalculateLimitStatus` 완전 제거**. WP-01(합성 0·`LIMIT_DATA_REQUIRED`/`DEMO_ONLY`)·10시트·Excel2021·audit 해시 전용 보존. 시스템-홈 `DashboardSnapshot`(시스템 헬스)은 제외. **Codex 결과(2026-06-21)**: `ExcelReportBuilder`가 `LimitAnalysisResult`를 직접 소비하도록 변경. LIMIT_MONITORING은 `MonitoringTable`, EXCEPTION_LIST는 `ExceptionList`+High/Blocker validation, SUMMARY는 6상태 KPI+대사 PASS/FAIL을 출력. UI Report 흐름은 실 Risk Dashboard 입력을 분석하고, 입력 부재 시 합성 없이 빈 분석+`LIMIT_DATA_REQUIRED`로 처리.
- **WP-08 공통 CSV 파서 통합**: 3중 중복 제거(WP-02에 흡수).
- **WP-09 전일 대비 데이터모델 설계**: 기준일 N vs N-1 비교 모델(설계 산출물, 구현은 R2).

> R1 WP-01~09는 위 기록을 **DONE 원본**으로 보존한다(재구현 금지). R3-WP-01~05 + REL-v0.6 패키징 가드도 DONE(상세는 `docs/17`·`docs/41 §2`·`docs/43`). 아래는 **v0.6.0 이후 신규 백로그**.

---

# B. post-v0.6 Work Package Backlog (v0.6.1 STAB → v1.0)

> 각 WP는 **하나의 목표**만. Codex는 Resume Brief의 NEXT UP 1개만 집는다. 프롬프트 경로 = `prompts/codex/<WP-ID>_*.md`. 절대원칙·STOP 규칙(`AGENTS.md`) 전부 유지.

## STAB-WP-01. Build / Version Reproducibility — **NEXT UP** (RR-11)
- **현재 문제**: `build/01~03` 기본 `[string]$Version="0.2.0"` ≠ `VERSION`(0.6.0). 무인자 실행 시 0.2.0 산출물 생성 위험. VERSION이 단일 원천이 아님.
- **목표**: `VERSION` 파일을 **단일 버전 원천**으로. 빌드 스크립트가 VERSION을 읽고, `-Version` 전달 시 **불일치하면 실패(exit 1)**. Release Note/ZIP/SHA/DependencyList가 동일 Version 사용. Build metadata(Commit SHA·Test 총수·SDK·Runtime·Build Date)를 Release Note에 기록.
- **선행조건**: 없음.
- **작업범위**: `build/01_publish`·`02_package`·`03_verify`의 `-Version` 기본값을 VERSION 파일 읽기로 대체(+불일치 실패), `global.json`로 .NET SDK 고정 여부 결정(ADR-006, `docs/40`), Release Note 템플릿에 build metadata 행 추가.
- **제외범위**: 기능 코드, 패키지 추가, .NET 메이저 전환(ADR만 — `docs/40` ADR-005).
- **읽을문서**: `AGENTS.md`, `docs/40`(ADR-005/006), `docs/24`, `build/00~03`, `VERSION`.
- **수정예상파일**: `build/01~03*.ps1`, (선택) `global.json`(신규), Release Note 생성부.
- **Public Interface**: 스크립트 동작 — 무인자 시 VERSION 사용, `-Version X`가 VERSION과 다르면 명확 메시지 후 exit 1.
- **보안조건**: 외부 0·결정적·읽기 전용(빌드 산출 외 쓰기 없음).
- **테스트**: (Windows) `build/01~03` 무인자 → 0.6.0 산출. `-Version 9.9.9` → 실패. SmokeTest 유지. PowerShell 5.1+pwsh 7 양쪽(`docs/40` ADR-004 교훈).
- **완료조건**: 코드베이스에 하드코딩 `0.2.0` 0개(샘플/문서 제외), 불일치 실패 동작 확인.
- **Branch**: `feature/stab-wp-01-build-version` · **Commit**: `build: VERSION single-source + fail on version mismatch (STAB-WP-01)`
- **Claude Review Checklist**: VERSION 단일원천 / 불일치 실패 / 산출물 버전 일치 / build metadata 기록 / 양쪽 셸 / NuGet 0 / Gate A.

## STAB-WP-02. Authoritative Test Baseline (RR-12)
- **현재 문제**: SmokeTest 하니스가 합계를 출력하지 않아 484/502 혼재. 정본 수치 부재.
- **목표**: SmokeTest 종료 시 **정본 합계 + 도메인별 PASS/FAIL Summary + 실행시간**을 출력. main에서 1회 실행해 정본 수치를 `docs/38 §0`·Release Note에 고정.
- **작업범위**: 하니스(러너)에 카운터/요약 출력 추가(기존 단언·이름 보존). 실패 시 비0 종료 유지. 추가/감소 테스트 수 사유 기록 규약.
- **제외범위**: 테스트 분리(STAB-WP-04), 기능 변경.
- **수정예상파일**: `tests/RiskManagementAI.SmokeTests/Program.cs`(러너 부분).
- **테스트**: 합계 출력 일치, 도메인 요약 정확, 실패 시 exit≠0. 기존 전부 PASS 유지.
- **Branch**: `feature/stab-wp-02-test-baseline` · **Commit**: `test: emit authoritative SmokeTest total + per-domain summary (STAB-WP-02)`
- **Claude Review Checklist**: 합계/요약 정확 / 기존 단언 불변 / exit code / 정본 수치 docs 반영.

## STAB-WP-03. Release Security + Integrity Manifest (RR-13, RR-14)
- **목표**: (a) Release 산출물에서 **PDB/개인 경로/SourceLink/Debug·Test config/Unsafe BinaryFormatter** 부재 보장(`DebugSymbols=false`, `DebugType=none`, allowlist), (b) **`approved_manifest.json`**(핵심 파일 path·size·SHA256·version·required·security class) 생성 + **앱 시작 시 무결성 검증**(개발=Fallback 경고, 운영=Fail-Closed: policy 불일치→기동/기능 차단, rules→검사 차단, template→Report 차단, KB→검색 차단).
- **선행조건**: STAB-WP-01.
- **작업범위**: `build/01`/`03`에 Release 보안 검증 추가, manifest 생성·검증 모듈(인박스, NuGet 0), 시작 시 검증 + 모드 분기. Code Signing은 **운영 절차 Placeholder**(자동서명 미구현).
- **제외범위**: 실제 Code Signing 인증서, 기능 로직.
- **수정예상파일**: `build/01~03`, `Core/Integrity/*`(신규), App 시작부, `config/approved_manifest.json`(생성물 또는 placeholder).
- **보안조건**: 운영 Fail-Closed/개발 Fallback 명확 분리. 해시 전용. 외부 0.
- **테스트**: 정상=PASS, 변조 파일=차단(도메인별), PDB/개인경로/Debug config 0 검증, ZIP에 manifest 포함.
- **Branch**: `feature/stab-wp-03-integrity` · **Commit**: `feat: release security guard + integrity manifest with fail-closed verify (STAB-WP-03)`
- **Claude Review Checklist**: PDB/개인경로/Debug 0 / manifest 검증 / 운영 Fail-Closed·개발 Fallback / 핵심파일 분류 / NuGet 0 / 기존 테스트 유지.

## STAB-WP-04. SmokeTest Suite Structure (RR-10 보호)
- **목표**: 비대한 단일 `Program.cs`를 **외부 프레임워크 0**으로 내부 Suite(SafetyTests/CsvTests/XlsxTests/MappingTests/LimitTests/ReconciliationTests/ReportTests/KbTests/NcrTests/PackagingTests/UiContractTests + TestRunner)로 분리. **테스트 삭제·약화 금지**, 총수 보존(감소 시 사유·매핑).
- **선행조건**: STAB-WP-02.
- **테스트**: 분리 전후 총수·이름 동일(매핑표), 도메인 Summary, Golden File 유지, 실패 exit code 유지.
- **Branch**: `feature/stab-wp-04-test-suites` · **Commit**: `test: split SmokeTest into internal suites without loss (STAB-WP-04)`
- **Claude Review Checklist**: 총수 보존+매핑 / 단언 불변 / 도메인 Summary / 외부 0.

## PILOT-WP-01. v0.6 Offline Gate B/C Evidence (BLOCKED, user/Test PC)
- **목표**: `docs/45` v0.6 Gate B/C 증거 시트를 실 오프라인 Test PC에서 채워 봉인. **실 PC 증거 없으면 PASS 금지(BLOCKED 유지).**
- **성격**: Codex 코드 작업 아님(문서/운영). Claude는 증거 회신 시 항목별 PASS/BLOCKED 재판정. 신규 기능과 분리·병행.

## R2-WP-01. Risk Semantic Hardening (RR-15)
- **목표**: 중복 Limit Key를 `group.Last()`로 임의 선택하지 않고 **명시 차단/상태화**, 통화·단위 컬럼을 ColumnMapping으로 관리, **`RECON_UNIT_MISMATCH` 활성화**, BASE_DT 형식 검증·정규화, Join 선택 규칙을 Audit Metadata에 기록.
- **선행조건**: STAB-WP-01~02.
- **수정예상파일**: `Core/Risk/LimitMonitor.cs`, `Core/Mapping/ColumnMapping*`, `Core/Risk/LimitAnalysisResult.cs`, tests.
- **테스트**: 중복키 양성/차단, 통화·단위 매핑, RECON_UNIT 양성/음성, BASE_DT 비정상 정규화, Audit 기록.
- **Branch**: `feature/r2-wp-01-semantic-hardening` · **Commit**: `feat: risk semantic hardening (dup key, unit recon, base_dt) (R2-WP-01)`
- **Claude Review Checklist**: 임의선택 제거 / RECON_UNIT / 매핑 일원화 / 결정성 / 기존 6상태·대사 불변 / Gate A.

## R2-WP-02. Streaming / Performance (RR-08)
- **목표**: 전량 메모리 적재 제거 — 파일/행 상한, Streaming CSV, Progress/Cancellation/Timeout, 메모리 Peak 측정, **Welford 평균·분산**(전체 List 저장 제거), CSV 강화(quoted multiline/escaped quote/delimiter/CRLF·LF/Formula Injection/빈·중복 헤더/과대 cell), 대용량 샘플 Generator + 10K/100K/1M 벤치. **STOP**: 외부 의존 필요 시.
- **Branch**: `feature/r2-wp-02-streaming` · **Commit**: `perf: streaming csv + bounded memory (R2-WP-02)`.
- **Claude Review Checklist**: 상한/스트리밍 / Welford / Injection 탐지 / 벤치 / NuGet 0 / 기존 결과 동일.

## R2-WP-03. Prior-Day Analytics
- **목표**: 공통 Domain Model(Exposure/Limit Usage/VaR/Delta/Gamma/Vega/DV01/CS01/P&L/Exception)로 Current·Previous·Δ·%·New/Resolved/Increased/Decreased·TopN. **Data Fact / Methodology / User Validation / Hidden Risk 분리.**
- **Branch**: `feature/r2-wp-03-prior-day` · **Commit**: `feat: prior-day comparison model (R2-WP-03)`.
- **Claude Review Checklist**: 공통 결과 재사용 / 분리표기 / 결정성.

## R2-WP-04. Visualization / Excel Report (NuGet 0)
- **목표**: 인박스(WPF Canvas/Shape 또는 Excel OOXML Chart/조건부서식)로 한도사용률 Bar·전일대비 Trend·TopN·Desk×RiskFactor Heatmap·집중도·통화별·만기 Bucket·예외현황. 임의 데이터 생성 금지(공통 AnalysisResult만). Excel: RAW_DATA 명칭=내용 일치, Source Metadata 분리, MARKET/HEDGE/VALUATION/LIQUIDITY/REG_BASIS, 정확 Formula/Exception Count(Header/NO_EXCEPTION 제외). 외부 차트 필요 시 **STOP**.
- **Branch**: `feature/r2-wp-04-visualization` · **Commit**: `feat: in-box visualization + report sections (R2-WP-04)`.
- **Claude Review Checklist**: NuGet 0 / 공통 결과만 / 정확 카운트 / Excel 2021 호환.

## KB-WP-01. Knowledge Pack Contract (설계 우선)
- **목표**: Application Source ↔ Knowledge Pack 분리 계약 — Pack Manifest/Version/Hash, Document Metadata, **Clause/Chunk Schema(Deterministic Chunk ID)**, As-of Date, Superseded, License/Approval Status, Access Classification, Source Text Hash, Upgrade/Rollback. **원문 repo 미포함**(별도 승인 Data Pack). **Vector/Embedding 미도입 — keyword/inverted index만.**
- **Branch**: `feature/kb-wp-01-pack-contract` · **Commit**: `feat: knowledge pack contract + chunk schema (KB-WP-01)`.
- **Claude Review Checklist**: 원문 미포함 / 결정적 Chunk ID / 인용검증 / NuGet 0 / Vector STOP.

## KB-WP-02. Public Document Ingestion (승인형, 원문 repo 미포함)
- **목표**: 승인된 공개자료만 Offline Ingestion Package로 적재(PDF/HWP 원문 repo 직접포함 금지), 문서/조항 Hash, 검색결과↔원문위치 연결, Metadata 결과 vs 실 Clause 결과 구분, 적용기준일별 유효문서 선택. **APPROVAL_REQUIRED**(라이선스·출처·버전 승인 — `docs/41`).
- **Claude Review Checklist**: 원문 미포함 / 인용검증 / 기준일 선택 / 승인 게이트.

## NCR-WP-01. Approved NCR Rule Pack Contract (계수 미포함)
- **목표**: Rule Set ID/Version/Effective/Expiry, Component(Map), Formula Definition, Coefficient/Unit/Sign/Rounding, Regulation Basis, Validation SQL(조회전용), Approval History, Pack Hash, Reviewer, Rollback. **실 계수·내부기준 repo 미포함** → Prod 승인 Rule Pack 적재. **Pack 없으면: 계산 차단·설명 구조만·`APPROVAL_REQUIRED`·공식 산출 출력 금지.**
- **현재 상태 근거**: NCR Rule Set = **SCAFFOLD_ONLY**(구조만). 본 WP는 승인형 Pack 계약.
- **Claude Review Checklist**: 계수 미포함 / Pack 부재 시 계산 차단 / 조회전용 SQL / 검토용 초안.

## LLM-WP-01. Model Adapter Contract (설계 전용, Runtime STOP)
- **목표**: `ILocalModelProvider`/`NoModelProvider`(유지)/`ModelProviderFactory`/Availability/HealthCheck/Request/Response/Timeout/Cancellation/Max IO/Audit Metadata/Output Safety Pipeline/Model Pack Manifest/Runtime·Model Hash/License/Hardware/Known Limitations. Architecture = **Out-of-process Runtime·App↔Model Pack 분리·Local IPC·외부 Network 차단·Crash 격리·Memory 제한·Health Check·NoModel Fallback**. **실 Runtime/Library/Model 도입 직전 `MODEL_APPROVAL_REQUIRED`로 STOP**(승인 문서 `docs/40` **ADR-003**(Process Boundary 설계)+**ADR-009**(Model Approval Package 요건)/`docs/41 §3`).
- **Claude Review Checklist**: NoModel 유지 / 인터페이스만(런타임 0) / ProcessBoundary 설계 / STOP 문서.

## FEEDBACK-WP-01. Approved Example Retrieval (가중치 불변)
- **목표**: Original Task/Output↔Corrected Final↔Reviewer/Approval/Version/Effective/Deprecated/Usage·Success·Error/Duplicate/Retrieval Score. 흐름: User Feedback→저장→Reviewer 승인→Approved Example Store→유사요청 검색→Prompt Context 반영→사용 Example ID Audit. **모델 가중치 변경 0.**
- **Claude Review Checklist**: 승인 Example만 사용 / 가중치 불변 / Audit / 결정적 검색.

> WP 형식 정본: `docs/39 §0`(목표·선행·범위·제외·읽을문서·수정파일·Interface·구현세부·보안·성능·테스트·완료·Rollback·Branch·Commit·Claude Review Checklist). 관련: `docs/38`(Train·Traceability)·`docs/40`(ADR)·`docs/41`(게이트)·`docs/45`(Gate 증거).
