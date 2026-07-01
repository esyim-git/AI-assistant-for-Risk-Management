# Smoke Governance — 상세 체크리스트

> 인박스 SmokeTest suite의 회귀(삭제·약화·미분류·기준선 혼동)를 막기 위한 점검표.
> 점검은 **읽기 전용**(`git diff`/Read/Grep/Glob)으로 수행하며, 재현은 `dotnet run --project tests/RiskManagementAI.SmokeTests`.
> 현 정본 기준선: **`Total=807`** (`docs/39` Resume Brief 값과 대조).

---

## 0. 사전 — 변경 범위 파악

```bash
git diff --stat <base>..<branch> -- tests/        # 변경 테스트 파일·라인 규모
git diff <base>..<branch> -- tests/               # 추가/삭제/변경 라인 전수
```

- [ ] `tests/` 변경이 해당 WP 범위 안인가 (무관 suite 대량 변경 없음)
- [ ] 외부 테스트 프레임워크(`PackageReference`로 xUnit/NUnit/MSTest 등) 추가 0 — `*.csproj` 확인

---

## 1. 삭제·약화 회귀 점검 (최우선)

- [ ] 삭제된 `AssertTrue(...)` 라인 0 (있으면 사유·대체 매핑 필수)
- [ ] 단언 약화 0: 조건이 느슨해지지 않음
  - 예시(약화 신호): `==` → `!= null`, 정확값 비교 → 존재만 확인, 범위 축소 → 범위 확대, 음성 테스트(`Throws<T>`) 제거
- [ ] 기존 음성/방어 테스트(예: 차단어·경로 가드·금지파일) 유지
- [ ] 테스트명만 바뀌고 단언이 비어버린 케이스 없음

---

## 2. 총수·기준선 대조

- [ ] 보고/러너의 합계 줄 `Total=N` 확인 (`=== SmokeTest Summary ===` 다음 줄)
- [ ] `N >= 807` (직전 기준선 이상). 감소 시 **WP·사유·매핑** 명시되어야 함
- [ ] 증가분이 신규 기능의 양성/음성 회귀로 설명됨 (단순 중복 부풀리기 아님)
- [ ] `docs/39` Resume Brief의 기준선 Total과 코드 실제값이 일치 (드리프트 시 `/risk-doc-truth-sync`)

---

## 3. 도메인 분류 점검 (`SmokeTestContext.SmokeDomain`)

- [ ] 신규 테스트명이 해당 도메인 키워드를 포함 → `Unclassified=0`
- [ ] 러너 종료부에 `SmokeTest domain classification failed: Unclassified=...` **미출력**
- [ ] 분류기 수정 시 키워드 **추가만** (기존 도메인 매칭을 좁히거나 제거하지 않음)
- [ ] 도메인 요약 줄(`  <Domain>: PASS=p FAIL=f`)이 정상 출력

### 도메인 ↔ 대표 키워드 (분류기 기준)

| Domain | 대표 키워드(부분일치, 대소문자 무시) |
|---|---|
| Xlsx | `XlsxReader`, `.xlsx`, `xlsx` |
| Csv | `CsvReader`, `CP949`, `UTF-8`, `BOM`, `encoding`, `CSV parser` |
| Mapping | `ColumnMapping`, `mapping`, `mapped`, `renamed`, `physical column` |
| Reconciliation | `Reconcil`, `RECON`, `원천합계`, `analysis balance`, `row amplification`, `orphan limit`, `duplicate limit`, `base-date mismatch` |
| Report | `ExcelReport`, `ReportBuilder`, `report `, `LIMIT_MONITORING`, `EXCEPTION_LIST`, `SUMMARY`, `templates/report` |
| Limit | `LimitMonitor`, `limit`, `한도`, `exposure`, `BASE_DT`, `NO_LIMIT`, `INVALID_LIMIT`, `BREACH`, `WARNING`, `MAPPING_ERROR`, `usage ratio` |
| Ncr | `Ncr`, `NCR Rule`, `NCR 공식`, `Rule Set` |
| Kb | `KbIndex`, `KbSearch`, `Regulation`, `catalog`, `citation`, `source locator`, `license`, `approval`, `인용`, `검색`, `원문`, `공개` |
| Packaging | `build/0`, `VERSION`, `global.json`, `packaging`, `source-text`, `KbRepositoryGuard`, `manifest`, `Expand-Archive`, `PowerShell` |
| Assist | `completion`, `smart assist`, `suggestion`, `provider`, `popup`, `assist` |
| Audit | `TaskLog`, `FeedbackLog`, `Audit`, `Feedback`, `PromotedExample`, `request hash`, `raw request` |
| Generation | `NoModelDraftService`, `DraftPipeline`, `draft`, `NoModel`, `NO_MODEL`, `generated draft` |
| UiContract | `UI shell`, `Left menu`, `Main tab`, `navigation`, `snapshot`, `Risk Dashboard`, `Settings`, `Feedback Center`, `Offline Mode`, `Local Model`, `Reports` |
| DataProfile | `DataProfiler`, `profile`, `null values`, `duplicate rows`, `numeric`, `BASE_DT distribution`, `source file name` |
| Safety | `RuleLoader`, `RuleSet`, `SQL`, `SELECT`, `VBA`, `Option Explicit`, `Excel 2021`, `PolicyLoader`, `Security policy`, `External API`, `auto update`, `telemetry`, `safe fallback`, `checker`, `finding`, `DEMO_ONLY` |

> 표는 분류 의도 요약이며, 정본은 `SmokeTestContext.cs`의 `SmokeDomain` 구현이다. 키워드 추가 시 코드와 표를 함께 갱신.

---

## 4. 신규 회귀 충분성 (WP별 additive)

- [ ] 신규 기능마다 **양성**(정상 입력 → 기대 결과) 테스트 추가
- [ ] 신규 기능마다 **음성/방어**(이상·차단 입력 → 안전 fallback/예외) 테스트 추가
- [ ] 경계값·결정성(동일 입력=동일 출력) 케이스 포함(해당 시)

---

## 5. 재현 검증

```bash
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests
```

- [ ] `=== SmokeTest Summary ===` 출력
- [ ] `Total=N PASS=N FAIL=0` (FAIL=0)
- [ ] `Unclassified` 줄 미출력 (종료코드 0)

---

## 6. 판정 템플릿

```
[Smoke Governance] <branch>
- Total       : <현재 기준선 807> → <이후>  (증감 사유: <한 줄>)
- 삭제/약화 단언 : 0  (또는 위반 목록)
- Unclassified  : 0  (또는 N건 목록)
- 신규 회귀     : 양성/음성 추가됨 (또는 부족)
- 외부 프레임워크: 0
판정: 회귀 0건(보존됨) | 회귀 N건(보완 필요)
```

- 증거 없는 PASS/VERIFIED는 적지 않는다.
- 기준선 Total 문서 드리프트는 `/risk-doc-truth-sync`로 분리 처리.

---

## 참조
- `tests/RiskManagementAI.SmokeTests/SmokeTestContext.cs` · `TestRunner.cs` · 각 `*Tests.cs`
- `AGENTS.md §3·§5` · `docs/39`(기준선 Total) · `CLAUDE.md §11.4·§11.6`
- 연계 스킬: `/risk-codex-review` · `/risk-doc-truth-sync`
