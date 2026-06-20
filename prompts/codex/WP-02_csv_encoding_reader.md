# Codex WP-02 — 인코딩 인식 CSV Reader (CP949/UTF-8)

> 권위 스펙: `docs/39` WP-02, `docs/40` ADR-004. Release: R1.

## 목표
UTF-8 전용 CSV 리더를 **CP949(EUC-KR)/UTF-8** 모두 지원하도록 하고, 중복된 3개 CSV 파서(`DataProfiler`/`LimitMonitor`/`RegulationCatalog`)를 공통 `CsvReader`로 수렴한다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3`, `docs/39`(WP-02), `docs/40`(ADR-004), `docs/28`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-02-csv-encoding origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0 유지.

## 작업 범위
- `Core/Data/CsvReader.cs` + `CsvEncoding{Auto,Utf8,Cp949}` + 공통 `CsvTable`.
- 3개 기존 파서를 공통 리더로 위임(동작 동일성 유지).
- 더미 CP949 샘플 추가(`samples/dummy_data/*_cp949.csv`).

## Public Interface
`CsvTable CsvReader.Read(string path, CsvEncoding encoding = CsvEncoding.Auto)`

## 구현 세부 / 결정성
- **CP949 = 경로 A 확정(repo 내장 디코더, NuGet 0)**. `System.Text.Encoding.CodePages` 패키지 **추가 금지**(`Encoding.GetEncoding(949)`는 net8.0 인박스 아님).
- **repo 내장 자체 디코더 + 공개 표준 Windows-949(UHC/CP949) *전체* 매핑표 리소스**. 매핑표 출처: 공개 표준(예: Unicode Consortium `CP949.TXT` 또는 WHATWG Encoding 'euc-kr' 통합 인덱스 = 전체 Windows-949/UHC). ⚠️ **EUC-KR/KS X 1001 부분집합 금지** — UHC 확장 한글 음절(현대 11,172자 전체) 누락 시 정상 Golden6 CP949 오디코드. 매핑표 리소스 **Hash 검증**(로드 시).
- Auto: BOM이면 UTF-8, 아니면 결정적 규칙(예: CP949 디코딩 후 U+FFFD 발생 시 UTF-8 재시도)으로 선택하고 **선택 결과를 메타/finding으로 노출**.
- 경로 가드 유지, 외부 호출 0.

## ⚠️ STOP 가능 지점
- **패키지를 추가하려는 순간 STOP**. CP949는 인박스/패키지가 아니라 **내장 매핑표 디코더(경로 A)**로만 구현(docs/39 WP-02b · docs/40 ADR-004). 경로 B(`System.Text.Encoding.CodePages`)는 **기각**됨 — 도입 금지.

## 테스트(필수)
CP949 한글 컬럼/값 라운드트립(**EUC-KR 범위 밖 UHC 확장 음절 포함 필수**) · UTF-8(BOM/무BOM) · Auto 감지 결과 검증 · **매핑표 Hash 검증** · 3 파서 수렴 후 기존 동작 유지.

## 완료/보고
build 0/0 · SmokeTest PASS · NuGet 0 확인 · 게이트 A 0건 · `docs/39` 원장 갱신.
