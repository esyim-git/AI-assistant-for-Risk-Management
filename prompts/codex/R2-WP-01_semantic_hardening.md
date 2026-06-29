# Codex R2-WP-01 — Risk Semantic Hardening (RR-15)

> 권위 스펙: `docs/39` R2-WP-01, `docs/38`(§5 C-13·RR-15), `docs/40`(ADR), `docs/41 §1`(Data Gate). Release: R2. 선행: WP-04(ColumnMapping)·WP-05(공통 `LimitAnalysisResult`·6상태)·WP-06(대사 9종·`ReconciliationSummary`) 모두 완료(main).

## 목표 (하나의 명확한 목표)
`LimitMonitor`의 R1 의미 결함 5개를 **결정적으로 경화**한다 — ① 중복 Limit Key 임의선택(`group.Last()`) 제거 후 **명시 차단·상태화(`DUPLICATE_LIMIT`)**, ② 통화·단위 컬럼을 하드코딩 const에서 **ColumnMapping(승인형)으로 이관**, ③ 단위 비교 컬럼 존재 시 **`RECON_UNIT_MISMATCH` 활성화**, ④ **BASE_DT 형식 검증·정규화**, ⑤ Join 선택 규칙을 **Audit Metadata에 기록**. **인박스만(NuGet 0)·실데이터 0·자동실행 0·결정적.** R1 계약(대사 9종 코드·`LimitAnalysisResult`·Dashboard=Report 일원화·`Passed` 정의)은 **보존**, 상태/필드/논리컬럼은 **추가만** 한다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4·§11.4·§11.5`, `docs/39`(R2-WP-01·WP-04·05·06), `docs/38`(§5·RR-15), `docs/40`, `docs/41 §1`, 그리고 기존 코드:
- `src/RiskManagementAI.Core/Risk/LimitMonitor.cs`(특히 10-12·85·133-136·319-374·410-442·660-684·797-831)
- `src/RiskManagementAI.Core/Risk/LimitAnalysisResult.cs`
- `src/RiskManagementAI.Core/Mapping/ColumnMapping.cs`·`ColumnMappingLoader.cs`(특히 `RequiredColumns`·`ValidateCompleteMapping` 15·102-122)
- `config/column_mapping.json`, `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`·`MappingTests.cs`

## 브랜치 / 동기화
```bash
git fetch origin && git switch -c feature/r2-wp-01-semantic-hardening origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, Subject에 `(#PR)`), 게이트 A, **NuGet 0**(외부 PackageReference 추가 금지 — System.* 인박스만).
- Local-Gate: 로컬 `dotnet build` 0/0 + SmokeTest `Total=N PASS / 0 FAIL`(기존 보존+신규, **Unclassified=0**) 증거 + Claude 리뷰. CI green은 머지 전제 아님.

## 작업 범위 (5개, 순서대로)

### 1) 중복 Limit Key 차단 / 상태화 (group.Last() 제거)
- `Analyze`의 `activeLimits` 빌드(`LimitMonitor.cs:133-136`)에서 `.ToDictionary(g=>g.Key, g=>g.Last(), …)`를 **폐지**한다. 중복 그룹(`group.Count()>1`)은 **유효 한도 사전에 채택하지 않는다**.
- 매칭 루프(`155-159`)에서 **해당 Join Key가 중복 그룹이면** 그 노출 행을 신규 상태 `LimitMonitorStatus.DuplicateLimit`(코드 `DUPLICATE_LIMIT`)로 산출·**차단**한다(NORMAL/WARNING/BREACH/INVALID 산출 금지). `AddNoLimitRow`/`AddInvalidLimitRow` 패턴을 본뜬 `AddDuplicateLimitRow` 헬퍼 신설(Note 예: "동일 기준일·동일 Join Key 한도가 N건이라 단정 불가: 검토 필요", 결정적 메시지).
- 구현 권장: `limit.Rows`를 기준일 필터 후 `GroupBy(JoinKey)`로 만들되, `count==1`인 그룹만 단일 한도 사전에 넣고, `count>1`인 키 집합(HashSet)을 별도로 보관해 매칭 시 분기. 정렬·집합은 결정적이어야 한다.
- 기존 대사 `RECON_DUPLICATE_LIMIT`(Medium, `319-328`)는 **그대로 유지**(상태와 대사는 별개 정보; 둘 다 나타나야 함).
- **금지**: `DUPLICATE_LIMIT`을 `ReconciliationFailCodes`(`37-42`)에 추가하지 말 것 — `ReconciliationSummary.Passed` 정의(R1)는 불변(정책 격상은 별도 ADR 대상).

### 2) 통화·단위 ColumnMapping 이관
- `LogicalColumn` enum에 `CurrencyCode`, `UnitCode` **추가**(append). `SafeDefaults()`에 `[CurrencyCode]="CCY_CD"`, `[UnitCode]="UNIT_CD"` 추가. `config/column_mapping.json` `Mappings`에 `"CurrencyCode":"CCY_CD"`, `"UnitCode":"UNIT_CD"` 추가(**더미 물리명만** — 실 컬럼명 금지).
- **중요(회귀 방지)**: `ColumnMappingLoader.RequiredColumns`(`:15`, 현재 `Enum.GetValues<LogicalColumn>()` 전수)와 `ValidateCompleteMapping`(`102-122`)이 신규 enum을 **필수로 요구하면**, 신규 키가 없는 기존 6열 config가 전체 fallback(`UsedFallback=true`)을 일으킨다. 이를 막기 위해 **필수=기존 6열(BaseDate/PortfolioId/RiskFactor/ExposureAmount/LimitAmount/UseYn) / 선택=CurrencyCode·UnitCode**로 분리하라. 선택 컬럼 누락은 경고·fallback 사유가 아니다(매핑되면 사용, 없으면 비활성). `ValidateCompleteMapping`의 필수 루프를 명시적 6열 목록으로 바꾸고, 물리명 중복 검사는 매핑된 값 전체에 대해 유지.
- `LimitMonitor`의 `CurrencyCodeColumn` const(`:12`) 제거 → `mapping.Physical(LogicalColumn.CurrencyCode)`로 치환. `UnitCode`도 동일 도입. **`CreateRow`(`660-684`)와 `AddCurrencyMismatchExceptions`(`410-442`) 양쪽**이 매핑 물리명을 일관 사용하도록 동기 수정. (`DeskCode`/`ProductType` const는 본 WP 범위 밖 — 유지.)

### 3) RECON_UNIT_MISMATCH 활성
- `BuildReconciliationExceptions`(`262-374`)에서 `unitApplicable = HasColumn(exposure, unitCol) && HasColumn(limit, unitCol) && canBuildExposureKey && canBuildLimitKey`를 `currencyApplicable`(`332-335`)과 동형으로 계산. `ReconciliationComputation(…, UnitApplicable: false)` 하드코딩(`373`)을 `UnitApplicable: unitApplicable`로 교체.
- `AddUnitMismatchExceptions`를 `AddCurrencyMismatchExceptions`(`410-442`) 복제로 신설(코드 `RECON_UNIT_MISMATCH`/Medium, 메시지 결정적). `unitApplicable`일 때만 호출.
- 컬럼 부재 시 `Applicable=false`·예외 0 유지. **금액 크기차로 단위불일치 단정 금지** — 단위 컬럼 값 비교만.

### 4) BASE_DT 검증 / 정규화
- `Analyze`의 `normalizedBaseDate = baseDate.Trim()`(`:85`)에 **형식 검증** 추가: 허용 패턴(예 `yyyyMMdd`, 필요 시 `yyyy-MM-dd`)을 `DateTime.TryParseExact(..., CultureInfo.InvariantCulture, DateTimeStyles.None, out _)`로 확인(인박스 `System.Globalization` 이미 import).
- **범위 한정(필수)**: 비교키 의미를 바꾸지 말 것 — 데이터 행의 BASE_DT 값은 현행 `StringComparison.Ordinal` 정확일치로 계속 매칭한다(`113·134·752`). 정규화는 **입력 `baseDate` 인자 자체**의 형식 검증/표준 표기에 한정(데이터 행 BASE_DT 재해석·멀티-기준일 재매칭 금지).
- 불량 형식은 **graceful**: throw 금지. 기존 `RECON_BASEDATE_MISMATCH`(Low) 또는 finding으로 상태화하고 결정적으로 보고. Audit Metadata에 정규화 결과 기록.

### 5) Join 선택 규칙 Audit Metadata
- `LimitAnalysisMetadata`(`LimitAnalysisResult.cs:57-63`)에 `IReadOnlyList<string> JoinAudit`(또는 동등 전용 record) **추가**. `BuildResult`(`LimitMonitor.cs:224-260`)에서 결정적으로 채움.
- 기록 항목(예): Join Key 구성(BASE_DT+PORTFOLIO_ID+RISK_FACTOR), 중복키 처리=`group.Last()` 폐지·차단(N건 키 수), 통화/단위 적용여부(`CurrencyApplicable`/`UnitApplicable`), BASE_DT 정규화 결과. **`group.Last` 임의선택 문구는 더 이상 audit에 없어야 한다**(폐지됨).
- 생성자 시그니처 변경 → `BuildResult` 및 메타데이터 소비부(`DashboardSnapshotBuilder`·`ExcelReportBuilder` 등) 컴파일 호환 동기 수정(기능변경 0).

## 제외 범위
Streaming/상한/Welford(R2-WP-02), 전일대비(R2-WP-03), 차트/Heatmap/Report 강화(R2-WP-04), 새 입력형식, **신규 대사 코드 추가**(기존 9종 보존·활성만), 데이터 행 BASE_DT 재해석, `DUPLICATE_LIMIT`의 Fail-code 격상, `DeskCode`/`ProductType` const 매핑화.

## Public Interface (추가만, 기존 시그니처 불변)
- `enum LogicalColumn { …, CurrencyCode, UnitCode }`
- `enum LimitMonitorStatus { Normal, Warning, Breach, NoLimit, InvalidLimit, MappingError, DuplicateLimit }`; `StatusCode`에 `DuplicateLimit => "DUPLICATE_LIMIT"`
- `LimitAnalysisKpis`에 `int DuplicateLimitCount`(`FromRows` 갱신); `LimitAnalysisResult`에 `int DuplicateLimitCount => Kpis.DuplicateLimitCount`
- `LimitAnalysisMetadata`에 `IReadOnlyList<string> JoinAudit`
- `Analyze(CsvTable, CsvTable, string)`·`ColumnMapping.Physical`·`ColumnMappingLoadResult` 불변

## 결정성 / 보안
- 동일 입력 → 동일 상태·예외·순서·audit. 정렬·집합 고정(`OrdinalIgnoreCase`/`Ordinal` 기존 규약 유지). 금액 `decimal`.
- 읽기 전용·외부 0·합성 한도 미사용·`config/`만 읽기·해시 Audit·NoModelMode 불변.
- `config/column_mapping.json` 추가 물리명은 더미(`CCY_CD`/`UNIT_CD`)만 — 실데이터/실 컬럼명 repo 미포함.

## 테스트 (필수 — `LimitReconciliationTests.cs`/`MappingTests.cs` 회귀 추가, 기존 보존)
- **중복키 양성**: 동일 BASE_DT·Join Key 한도 2건 → 노출 행 상태=`DUPLICATE_LIMIT`(BREACH/NORMAL 아님) + `RECON_DUPLICATE_LIMIT` 예외 공존 + `DuplicateLimitCount>0`. **음성**: 유일 한도 → 정상 6상태·`DuplicateLimitCount=0`.
- **통화/단위 매핑**: 커스텀 매핑 물리명 변경 시 비교가 매핑 경유; **6열-only config → `UsedFallback=false`**(신규 선택컬럼 누락이 fallback 유발 안 함).
- **RECON_UNIT_MISMATCH 양성**: 양쪽 단위 컬럼+상이 값 → 예외·`Applicable=true`. **음성**: 단위 컬럼 부재 → `Applicable=false`·예외 0 / 동일 값 → 예외 0.
- **BASE_DT**: 정상 형식 통과; 비정상 형식 → graceful 상태화(throw 없음)·결정적.
- **Audit**: `JoinAudit`에 중복키 차단·통화/단위 적용여부 기록; `group.Last` 임의선택 문구 부재.
- **결정성/보존**: 반복 호출 동일; 기존 6상태·대사 9종 수치·`ReconciliationSummary.Passed` 불변.
- 신규 테스트 명명은 SmokeTest 도메인 분류기에 걸리는 키워드 사용(아래 newTestDomainKeywords) → **Unclassified=0** 유지.

## 완료 / 보고
`group.Last()` 폐지·`DUPLICATE_LIMIT` 차단 / 통화·단위 ColumnMapping 일원화(const 제거) / `RECON_UNIT_MISMATCH` 활성 / BASE_DT 검증·정규화 / `JoinAudit` 기록. 로컬 build 0/0 · SmokeTest `Total=N PASS / 0 FAIL`(기존 보존+신규·Unclassified=0) · NuGet 0 · 게이트 A 0건 · `docs/39` R2-WP-01 원장·`docs/38 §5` C-13/RR-15 갱신.

## STOP
외부 NuGet/Vector/Embedding/Local LLM/charting 라이브러리 필요 신호가 나타나면 **즉시 STOP** → 승인 문서(`docs/41`·`docs/40`) 후에만 진행. 본 WP는 순수 인박스로 구현 가능(STOP 위험 없음).

## Codex 리뷰 반영 (P2 — 필수 준수)
- **(P2) Optional 매핑 접근 — throw 회피**: `ColumnMapping.Physical()`은 미매핑 논리컬럼에 `InvalidDataException`을 throw한다(`ColumnMapping.cs:44`). CurrencyCode/UnitCode를 **Optional**로 다루므로 6열 config(통화/단위 키 없음)에서 `Physical(CurrencyCode)` 직접 호출은 throw → LimitMonitor 실패. 따라서 **`TryPhysical(LogicalColumn, out string?)` 추가**(또는 `PhysicalColumns.ContainsKey` 가드) 후 사용하고, 매핑 부재 시 throw 없이 해당 비교를 **비활성(Applicable=false)** 처리한다. 회귀: 6열 config 로드 시 `UsedFallback=false` + 통화/단위 비교 비활성·예외 0.
