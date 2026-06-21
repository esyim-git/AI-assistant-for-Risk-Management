# Codex R3-WP-01 — KB Document Metadata 확장 (공개 규정)

> 권위 스펙: `docs/17` (R3 RAG WP 분해 · 문서 Metadata), `docs/41 §2`(RAG/NCR Approval Gate). Release: R3. 선행: 없음(기존 `RegulationCatalog`/`KbSearch`).

## 목표
공개 규정 catalog의 메타데이터를 **docs/17 §R3 전체 항목**으로 확장한다: 기존 7컬럼 + **출처(인용 locator)·버전·시행일·폐기일·파일Hash·적재일·승인상태·대체문서·라이선스 상태**(9개). 검색 답변이 이 메타를 인용에 쓸 수 있는 토대를 만든다. **공개 규정 메타만**(원문·내부규정 미포함 불변).

> ⚠️ **출처(`Source`) ≠ 출처기관(`SourceOrg`)**: `SourceOrg`=발행기관(예: 국가법령정보센터), `Source`=**인용 locator**(공개 URL 또는 문서/조문 위치 참조). 기존 `source_type`("Public regulation")은 *유형*이지 locator가 아니므로 별도 `source` 컬럼이 필요(docs/17·docs/41 §2 인용 `출처`/citation 필수).

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§10`, `docs/17`(R3), `docs/41 §2`, 기존 `Core/Kb/RegulationCatalog.cs`(현 7컬럼·`RegulationCatalogEntry`), `Core/Kb/KbSearch.cs`(소비부), `kb/public_regulation_catalog.csv`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/r3-wp-01-kb-metadata origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- `RegulationCatalogEntry`에 9개 메타 필드 추가(아래 Public Interface) — **`Source`(출처 locator) 포함**.
- `RegulationCatalog` 로더가 새 컬럼 파싱. **하위호환**: 새 컬럼은 헤더에 추가하되, 값이 비거나 누락이면 **throw 금지 — 빈 문자열 + "메타 불완전" 경고**(`docs/41 §2` "Metadata 완비" 점검용).
- `kb/public_regulation_catalog.csv` 헤더/행에 메타 컬럼 추가(**공개 규정 메타만**; `INTERNAL_RULES`는 `PROD_ONLY`, `NCR_GUIDE`는 `MANUAL_APPROVAL_REQUIRED` 유지 — 원문 미적재).
- 제외: 역색인 검색(R3-WP-02), 인용형식(R3-WP-03), 적재가드(R3-WP-04), NCR(R3-WP-05). **검색 점수 로직 변경 금지**(이번엔 메타 적재만).

## Public Interface
```csharp
public sealed record RegulationCatalogEntry(
    string SourceId, string Category, string Title, string SourceOrg, string SourceType, string Status, string Note,
    // R3-WP-01 추가(모두 string, 공개 메타). Source=출처 locator(≠ SourceOrg 출처기관):
    string Source, string Version, string EffectiveDate, string RepealDate, string FileHash,
    string LoadedDate, string ApprovalStatus, string SupersededBy, string LicenseStatus);
```
- `RegulationCatalog.LoadFromFile`/`LoadDefault`는 새 컬럼을 읽어 채운다. 누락 컬럼/빈값 → 빈 문자열 + 경고(예외 아님).
- 메타 불완전 노출용: `IReadOnlyList<string> Warnings`(또는 entry별 incomplete 표식). 기존 시그니처 호환 유지(필요 시 오버로드/속성 추가).

## 구현 세부 / 보안
- 날짜는 **문자열(`YYYY-MM-DD`) 그대로 보관**(파싱 강제 안 함; 형식 이상은 경고만). 결정적.
- **`FileHash`는 catalog에 기재된 메타값**(공개 규정 원문 파일의 해시) — **원문은 repo 미적재**. 해시 재계산/원문 적재 금지.
- 공개 규정 메타만. **내부규정 원문·NCR 공식본 원문 repo 미포함**(status로 구분; 원문 컬럼 신설 금지).
- 외부 호출 0, NuGet 0, 경로 가드(기존 reader 패턴, `CsvReader` 경유).
- 기존 `KbSearch` 동작·점수·"검토용 초안"·해시감사 **불변**(이번 WP는 메타 적재만).

## 테스트(필수)
- 확장 catalog 로드 → 9개 메타 필드 정상 채움(샘플 공개 규정 행에 source(locator)/version/effective_date/license 등 값). **`Source`(출처 locator)가 `SourceOrg`(출처기관)와 별개 컬럼으로 채워짐** 확인.
- **기존 KbSearch 회귀 유지**(검색 결과·점수·draft notice 동일).
- 누락/빈 메타 컬럼 행 → **graceful(빈값 + 경고)**, throw 없음.
- `INTERNAL_RULES`(PROD_ONLY)·`NCR_GUIDE`(MANUAL_APPROVAL_REQUIRED)는 **원문 컬럼 없이 메타만**(원문 미포함 회귀).
- NuGet 0 / 기존 SmokeTest 유지.

## 완료/보고
catalog가 docs/17 §R3 metadata 9필드 보유, 기존 KbSearch 테스트 유지. build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17` 진행표 갱신.

## Claude Review Checklist
9 메타필드 추가(**출처 locator `Source` ≠ 출처기관 `SourceOrg`** 포함) / 하위호환(빈값 graceful) / 공개 메타만·원문 미포함(내부·NCR status 유지) / KbSearch 동작·점수 불변 / NuGet 0 / 기존 SmokeTest 유지 / Gate A.
