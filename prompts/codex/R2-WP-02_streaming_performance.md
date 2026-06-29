# Codex Prompt — R2-WP-02 Streaming / Performance (RR-08)

> 본 프롬프트는 `docs/39 §R2-WP-02`의 구현 단위다. **WP 1개만** 구현한다. Claude 승인 전 main 머지 금지. Branch = `feature/r2-wp-02-streaming-performance`.

## 0. 절대 원칙 (불변 — 위반 시 STOP 후 승인 요청)
- **외부 NuGet `PackageReference` = 0** (인박스 `System.*`만 사용). Vector DB/Embedding/Local LLM Runtime/모델파일/charting 라이브러리 도입 = **STOP** (승인 전 금지).
- 외부 API/Telemetry/AutoUpdate = 0. SQL/VBA/Golden6 **자동실행 0**.
- **실데이터/실 테이블·컬럼명/내부규정·NCR 원문/모델파일 repo 미포함.** seed·샘플은 일반 더미명만.
- 쓰기 경로 = `logs/`·`reports/`·`config/` 한정. 해시 Audit(원문 미저장). NoModelMode 유지.
- **기존 테스트 삭제·약화 금지.** SmokeTest 정본 Total 보존 + 신규만 추가. SmokeTest = 외부 프레임워크 0, 도메인 분류기(**Unclassified=0**).
- 과대표기 금지(§11.4): 상태 어휘 VERIFIED/PARTIAL/SCAFFOLD_ONLY/NOT_IMPLEMENTED 등. 실 오프라인 Test PC 증거 없으면 Gate PASS로 적지 않는다.

## 1. 하나의 목표
대용량/손상 CSV 입력을 **메모리 안전한 streaming**으로(행/바이트 안전 상한 도입), `DataProfiler` 수치 통계를 **Welford 온라인 평균/분산(1-pass)**으로 전환해 **전 행 값 보관 없이 결정적으로** 프로파일한다. 기존 경로는 **불변 보존**, 신규는 **옵트인 추가**. (선택) `Stopwatch` 벤치 훅을 `logs/` 한정 기록.

## 2. 읽을 것 (구현 전 필수)
- `docs/38`(RR-08), `docs/39 §R2-WP-02`, `docs/39 §WP-02·§WP-03`(CsvReader·XlsxReader 상한 선례), `CLAUDE.md §3·§11.4·§11.6`, `AGENTS.md`.
- 코드: `src/RiskManagementAI.Core/Data/CsvReader.cs`, `DataProfiler.cs`, `DataProfileResult.cs`, `CsvTable.cs`, `XlsxReader.cs`(상한 const 패턴 선례).

## 3. 현재 코드 사실 (근거 라인)
- `CsvReader.Read`(`CsvReader.cs:9`): `File.ReadAllBytes`(L26)로 **전체 적재** → Decode → `ParseRecords(text)`(L128)가 `ReadLine` 루프로 `List<CsvRecord>` **전 행 누적**. **행/바이트 상한 전무.** `ParseCsvLine`은 **따옴표 내 개행 미지원**(ReadLine 단위).
- `DataProfiler.ProfileTable(CsvTable)`(`DataProfiler.cs:46`): 메모리 적재 Rows 순회. `NumericAccumulator`(L146)는 **모든 decimal을 `List<decimal>`에 보관**(L148), `ToProfile`(L176)에서 Sum/Min/Max, `CountSimpleOutliers`(L188)에서 `Average()`+재순회 2-pass 분산(`/values.Count` population, n<4면 0, 3σ, std0이면 0). 중복행은 `string.Join('', normalizedValues)`를 `HashSet<string> duplicateKeys`(L59,76)에 **원문 전체 보관**.
- `XlsxReader`(`XlsxReader.cs:10-14`): `const MaxZipEntries=512`·`MaxUncompressedBytes`·`MaxEntryBytes`·`MaxWorksheetRows=5000`·`MaxWorksheetColumns=256` + `InvalidDataException($"... max={...}, actual={...}")` 규약 → **CSV 상한 설계의 인박스 선례**.

## 4. 작업범위 (구현)
1. **CSV streaming 리딩**: `File.ReadAllBytes` 전체 적재 대신 `StreamReader` 기반 forward-only 신규 경로. `ParseRecords`를 `IEnumerable<CsvRecord>` yield 스트리밍 파서로 분해(또는 신규 메서드). **기존 `CsvReader.Read` 시그니처·동작·결과 보존**(신규 메서드로 추가).
2. **행/바이트 상한**: `public const int CsvReader.MaxRowCount`·`public const long CsvReader.MaxByteSize` 도입(XlsxReader const 패턴 동형). 초과 시 `InvalidDataException` + `max=…, actual=…` 메시지(XlsxReader 동일 규약). 바이트 상한은 `FileInfo.Length` 또는 누적 카운터.
3. **Welford 온라인 통계**: `NumericAccumulator`의 `List<decimal>`+2-pass를 **count/mean/M2 스칼라 Welford 1-pass**로 교체. Sum/Min/Max는 입력순 고정 스칼라 누적(decimal). 분산/평균은 기존과 동일하게 **double 누적**(부동소수 결과 일치). **규약 정확 재현**: population variance `/n`, n<4→OutlierCount=0, 3σ 임계, std==0→0.
4. **중복행 해시화**: `duplicateKeys`를 정규화 문자열의 **SHA256(또는 안정 해시) 해시값만 보관**(원문 미저장)으로 전환 + 상한 가드. `DuplicateRowCount` 결과 **기존과 동일**.
5. **streaming 프로파일 진입점**: `ProfileTable`/`ProfileCsv` 보존 + streaming 신규 오버로드(예: `ProfileCsvStreaming(string, CsvEncoding=Auto)` 또는 `IEnumerable<CsvRow>` 소비).
6. (선택) **벤치 훅**: `Stopwatch` 측정값을 외부 전송 없이 `logs/` 로컬 로그로만.

## 5. 제외범위 (절대 손대지 말 것)
- **XLSX streaming**(`XlsxReader.cs:198` Descendants) — 이미 상한 보유, **후순위·제외**.
- **RFC4180 따옴표 내 개행 완전 지원** — ReadLine 단위 한계 **동일 유지**. "RFC4180 완전 지원" 표기 금지.
- R1 계약(6상태·RECON_*·`LimitAnalysisResult`·Dashboard=Report 일원화)·인코딩 탐지(CP949/UTF-8) 로직 — **변경 0**.

## 6. Public Interface
- **보존**: `CsvReader.Read(string, CsvEncoding=Auto):CsvTable`, `DataProfiler.ProfileTable(CsvTable):DataProfileResult`, `DataProfiler.ProfileCsv(string):DataProfileResult`.
- **신규(옵트인)**: streaming 리딩/프로파일 메서드 + `CsvReader.MaxRowCount`·`MaxByteSize` const.
- **선택 확장**: `NumericColumnProfile`에 `Mean`·`StdDev` **말미 추가**(기존 6필드 ColumnName/NonNullCount/Sum/Min/Max/OutlierCount 순서·이름 **불변**). 추가 시 기존 호출부·테스트 깨지지 않게.

## 7. 테스트 (DataProfile/Reconciliation 도메인 키워드, Unclassified=0)
- **결정성 회귀**: 동일 입력 → 기존 `ProfileTable` 결과 == streaming 결과(RowCount·NullCounts·DuplicateRowCount·NumericColumns 전 필드·BaseDateDistribution·Warnings).
- **상한**: 행/바이트 상한 초과 → `InvalidDataException` + `max=/actual=`.
- **Welford 정확성**: 기존 Sum/Min/Max/OutlierCount(3σ·n<4=0·std0=0) 회귀 고정.
- **중복 해시화**: `DuplicateRowCount` 기존 동일.
- **메모리 안전**: 대용량 더미(상한 내) 결정적 처리.
- 신규 테스트 키워드: `streaming`·`welford`·`MaxRowCount`·`MaxByteSize`·`duplicate`·`BASE_DT`. 기존 삭제·약화 0.

## 8. 완료조건 (Local-Gate)
- 외부 PackageReference 0 유지. 기존 경로 결과 불변. streaming 결정적. 행/바이트 상한 활성. Welford 1-pass·중복 해시화 적용.
- `dotnet build RiskManagementAI.sln -c Release` → 0 warning / 0 error.
- `dotnet run --project tests/RiskManagementAI.SmokeTests` → `=== SmokeTest Summary ===` + `Total=<정본+신규> PASS=<동수> FAIL=0`. Unclassified=0.
- Gate A(보안 스캔) 0. (선택) 벤치 훅 `logs/` 한정·telemetry 0.

## 9. 보고 (Claude Review용)
- Diff 요약 + 신규 메서드 시그니처 + 상한 상수값 + Welford 결정성 근거(기존 vs streaming 동일 출력 증거) + SmokeTest `Total=N PASS=N FAIL=0` 콘솔 출력 + `PackageReference` grep 0 증거. **Claude 승인 전 머지 금지.**

## 10. Branch / Commit
- Branch: `feature/r2-wp-02-streaming-performance`
- Commit: `feat: add streaming/Welford profiling path with row/byte caps (R2-WP-02)`
