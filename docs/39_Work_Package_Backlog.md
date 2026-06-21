# 39. Work Package Backlog & R1 Specs

> `docs/38` Release Train의 실행 단위(Work Package). Codex는 WP 단위로 구현하고, Claude는 WP별 Review Checklist로 검증한다.
> WP 형식: 목표·선행조건·작업범위·제외범위·읽을문서·수정예상파일·Public Interface·구현세부·보안조건·테스트·완료조건·Branch·Commit·Claude Review Checklist.
> 각 WP는 **하나의 명확한 목표**만 가진다. R1 Codex 프롬프트: `prompts/codex/WP-XX_*.md`.

---

## ★ R1 Resume Brief (Codex 갱신 · Claude 인수)
- **현재 상태**: WP-01 합성 한도 차단/DEMO_ONLY 구현 완료. WP-02 인코딩 인식 CSV Reader(CP949/UTF-8) 구현 완료. WP-03 XLSX 입력 Reader(인박스/NuGet 0) 구현 완료. WP-04 Risk Column Mapping(설정·승인형) 구현 완료.
- **NEXT UP**: **WP-05**(실 Exposure-Limit Join + 공통 AnalysisResult) → WP-06 → WP-07.
- **BLOCKED**: 0.
- **재현 검증**: `git fetch origin main && git switch main && dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests` (322+ PASS 유지).
- **⚠️ 확인 요망**: 비-기본 커스텀 매핑/Join Key 변경은 R1 마감 시 Data Spec Gate(docs/41) 검토 대상.

## R1 진행 원장 (Codex 갱신)
| WP | 목표 | 상태 | PR/커밋 | SmokeTest | 비고 |
|---|---|---|---|---|---|
| WP-01 | 합성 한도 차단 / DEMO_ONLY | DONE | `feature/wp-01-demo-limit-guard` | 278 PASS / 0 FAIL | 합성 1.1x 산식 제거, `LIMIT_DATA_REQUIRED`/`DEMO_ONLY` 회귀 고정 |
| WP-02 | 인코딩 인식 CSV Reader(CP949/UTF-8) | DONE | `feature/wp-02-csv-encoding` | 296 PASS / 0 FAIL | `CsvReader` 공통화, CP949 Path A(UHC 전체 매핑표·SHA256 검증), UTF-8 BOM/무BOM |
| WP-03 | XLSX 입력 Reader(인박스, NuGet 0) | DONE | `feature/wp-03-xlsx-input` | 308 PASS / 0 FAIL | `XlsxReader` → `CsvTable`, workbook 관계 기반 시트 해석, zip 안전상한 |
| WP-04 | Risk Column Mapping(설정·승인형) | DONE | `feature/wp-04-column-mapping` | 322 PASS / 0 FAIL | 기본=현행 호환, 커스텀 all-or-nothing, Data Gate |
| WP-05 | 실 Exposure-Limit Join + 공통 AnalysisResult | TODO | - | - | RR-03 |
| WP-06 | 대사·예외검증 9종 | TODO | - | - | RR-04 |
| WP-07 | Dashboard·Report 공통화 | TODO | - | - | RR-03 |
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
- **Codex 결과(2026-06-20)**: `Core/Data/CsvReader.cs`, `CsvTable`, `CsvRow`, `CsvReadMetadata`, `CsvEncoding`, `Cp949Decoder` 추가. `DataProfiler`/`LimitMonitor`/`RegulationCatalog`는 공통 reader로 수렴. CP949는 repo 내장 `Data/Resources/cp949-uhc-map.txt`(17,236 entries, SHA256 `7af60dbf6153362b4e7c65eaac24c233209fd1b854e182b09bc9601ec531851c`)를 런타임 검증 후 사용. 더미 CP949 샘플(`samples/dummy_data/*_cp949.csv`)과 UHC 확장 음절 `힣` 라운드트립 회귀 추가. build 0/0, SmokeTest 296 PASS / 0 FAIL, NuGet 0 유지.

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
- **Public Interface**: `LimitAnalysisResult LimitMonitor.Analyze(CsvTable exposure, CsvTable limit, string baseDate, ColumnMapping map)`; 상태 enum `{ NORMAL, WARNING, BREACH, NO_LIMIT, INVALID_LIMIT, MAPPING_ERROR }`.
- **구현세부**: 상태셋을 docs/38·docs(5절) 정의로 확장(현 `MissingLimit/InactiveLimit` → `NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`로 정렬). ABS 사용률·잔여한도 유지. 동일 입력→동일 수치 보장(결정적).
- **보안조건**: 읽기 전용. 합성 한도 미사용(WP-01). 외부 0.
- **테스트**: BASE_DT 조인, 6 상태 분류, 동일 입력→Dashboard/Report 동일 수치(WP-07 연계 전 단위검증).
- **완료조건**: 공통 `LimitAnalysisResult` 1개를 Dashboard·Report 양쪽이 소비 가능.
- **Branch**: `feature/wp-05-join-analysis-result` · **Commit**: `feat: real exposure-limit join + shared analysis result (WP-05)`
- **Claude Review Checklist**: 6 상태 / 결정성 / 합성 미사용 / 기존 한도 테스트 유지·확장 / Gate A.

---

## WP-06~09 (요지, 상세는 R1 진행 중 확정)
- **WP-06 대사·예외검증**: Exposure/Limit 미매핑·중복 Limit·기준일 불일치·통화 불일치·단위 불일치·음수/0 Limit·Join 후 건수 증폭·원천합계 vs 분석합계 대사 → ExceptionList + 상태.
- **WP-07 Dashboard·Report 공통화**: `LimitAnalysisResult` 하나로 KPI/표/ExceptionList/Excel/History/Audit 생성. `BuildUiLimitRows` 완전 대체.
- **WP-08 공통 CSV 파서 통합**: 3중 중복 제거(WP-02에 흡수).
- **WP-09 전일 대비 데이터모델 설계**: 기준일 N vs N-1 비교 모델(설계 산출물, 구현은 R2).

> R2~R6 WP는 본 백로그에 이어서 추가한다. 관련: `docs/38`(Train·Traceability), `docs/40`(ADR), `docs/41`(게이트).
