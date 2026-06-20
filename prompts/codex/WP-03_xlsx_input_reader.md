# Codex WP-03 — XLSX 입력 Reader (인박스, NuGet 0)

> 권위 스펙: `docs/39` WP-03, `docs/40` ADR-004. Release: R1. 선행: WP-02 권장.

## 목표
.xlsx 입력을 **인박스 `System.IO.Compression` + OOXML XML 파싱**으로 읽어 CSV와 동일한 `CsvTable`로 제공한다. **NuGet/OpenXML SDK/Interop 0**(DM-03/DU-08 정합).

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§6`, `docs/39`(WP-03), `docs/40`(ADR-004), 기존 `Core/Report/ExcelReportBuilder.cs`(인박스 OOXML 쓰기 패턴 참고).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/wp-03-xlsx-input origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- `Core/Data/XlsxReader.cs`: `ZipArchive`로 `xl/worksheets/sheetN.xml` + `xl/sharedStrings.xml` 파싱 → 행/열 복원. 첫 시트 또는 지정 시트.
- 제외: 수식 평가, 스타일, 다중시트 병합.

## Public Interface
`CsvTable XlsxReader.Read(string path, string? sheetName = null)`

## 구현 세부 / 보안
- shared string·inline string·숫자 처리. 손상 zip/누락 part → `InvalidDataException`(UI graceful).
- **zip 안전**: 엔트리 수·압축해제 크기 상한(zip bomb 방지). 외부 호출 0. 경로 가드.

## 테스트(필수)
정상 xlsx(헤더/값/한글) 파싱 · 손상 xlsx → graceful 예외 · 큰 시트 상한 동작 · CSV와 동일 파이프라인 투입.

## 완료/보고
build 0/0 · SmokeTest PASS · NuGet 0 확인 · 게이트 A 0건 · `docs/39` 원장 갱신.
