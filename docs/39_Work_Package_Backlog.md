# 39. Work Package Backlog (R1 DONE · R3 DONE · post-v0.6 NEXT)

> `docs/38` Release Train의 실행 단위(Work Package). Codex는 WP 단위로 구현하고, Claude는 WP별 Review Checklist로 검증한다.
> WP 형식: 목표·선행조건·작업범위·제외범위·읽을문서·수정예상파일·Public Interface·구현세부·보안조건·테스트·완료조건·Branch·Commit·Claude Review Checklist.
> 각 WP는 **하나의 명확한 목표**만 가진다. R1 Codex 프롬프트: `prompts/codex/WP-XX_*.md`.

---

## ★ Resume Brief (Codex 인수 — v0.6.0 기준선)
- **현재 기준선**: main `9bc83a3` (R2-WP-03 머지 후), VERSION **0.6.0** (**v0.6.0 정식 릴리스 태그 = `3dfa80b`**; R2 트랙 v0.7.0 진행 중·미릴리스, 잔여 R2-WP-04). R1(WP-01~08)·R3(R3-WP-01~05)·REL-v0.6 가드(#54)·**STAB-WP-01~04(#56/#57/#59/#61/#66)·STAB-UX-01(#68)·UX-WP-01(#70)·UX-WP-02(#72)·UX-WP-03(#73)·STAB-UX-02(#76)·R2-WP-01(#79)·R2-WP-02(#81)·R2-WP-03(#84)** 모두 **DONE**. 추가 머지: truth-sync(#63·#67·#69·#74·#78·#80·#82·#83). SmokeTest **`Total=698 PASS=698 FAIL=0`**(직접 local-gate 재확인: `dotnet build` 0/0 + SmokeTest, 2026-06-30; 631 + STAB-UX-02 +15 + R2-WP-01 +25 + R2-WP-02 +9 + R2-WP-03 +18). **UX/STAB 트랙 완료 + R2-WP-01·R2-WP-02·R2-WP-03 완료** — VERIFIED 범위는 **정적·NoModel·외부 Editor 0·자동삽입 0** 한정이며 실 LLM 랭킹/학습=**R4(NOT_IMPLEMENTED)**, 실 오프라인 Test PC Gate B/C=**BLOCKED**(과대표기 금지). STAB-WP-05(코드서명)=APPROVAL_REQUIRED.
- **STAB-WP-03a DONE (#59, local-gate PASS)**: build측 Release 보안(PDB 0·Dev/Test config 0·UnsafeBinaryFormatter false) + Integrity Manifest 생성(build/01)·검증(build/03 — 필수항목 강제·경로 traversal 차단·hash/size/version). 증거: manifest 25 entries, ZIP SHA256 `3C7D3926…`, PDB/Dev-Test 0, SmokeTest 513. RR-13 + RR-14(build측) 해소.
- **STAB-WP-03b DONE (#61 merged, 2026-06-28, main `682f1d8`, VERIFIED — local-gate; review thread 7건 resolve)**: runtime Fail-Closed(Design 3 interim). 신규 `Core/Integrity/`(IntegrityStatus·IntegrityResult·IntegrityVerifier·IntegrityGate)가 build/03 §4를 in-process 포팅(SHA256 전용, NuGet 0, build/01·03 미변경, 513 불변). `App.OnStartup` 최상단에서 검증→FailClosed=Shutdown(2). 데이터/자산 변조 + manifest 부재/축소/버전불일치 차단. dev 스위치 강화(`RMAI_DEV_ALLOW_UNVERIFIED=1` + `Debugger.IsAttached`). **잔여 위험(미탐지, 명시)**: manifest 독립 앵커 부재(co-tamper)·self-contained 런타임 DLL 미해시·폴더 동반 변조 → **코드 서명(APPROVAL_REQUIRED)** 후속(`STAB-WP-05`). **Codex local-gate(2026-06-28, latest PR #61 delta incl. `947f532`/`8866a07` + RequiredCriticalEntries co-deletion fix) = VERIFIED**: `dotnet build` 0/0, SmokeTest `Total=572 PASS=572 FAIL=0`, Gate A 0, NuGet `PackageReference` 0, build/00~03 PASS(ZIP SHA256 `E3995BD54A1D1DCAA55FEDCD968E18906191255DCF564BA4047A0A59E8402021`). 봇 P2 4건(`99fc508`: ① null 엔트리 ② malformed path try/catch ③ mandatory를 manifest `required` 플래그와 무관하게 path로 강제 ④ 플랫폼 무관 rooted/UNC 거부) 검증 PASS. 추가 P2(`947f532`/`8866a07`) 검증 PASS: ⑤ **manifest 축소 가드** — build/01 critical 글롭(rules/templates/config·ncr/kb) 디스크 스캔으로 미선언 critical 파일 FailClosed(엔트리 드롭+파일 잔존/변조 차단, build/01 lock-step); ⑥ **critical 글롭 required-by-path** — 글롭 자산은 manifest `required` 플래그와 무관하게 필수 강제(엔트리 유지+`required:false`+파일 삭제 차단); ⑥' mandatory 자산 **co-deletion**(엔트리+파일 동시 삭제)은 hard-coded declared-check로 차단(앵커). **추가 보강 검증 PASS**: `RequiredCriticalEntries`로 현 build/01 critical asset inventory를 pin해 비-mandatory critical **co-deletion**(엔트리+파일 동시 삭제)도 FailClosed로 닫음. **잔여(미탐지 고정)**: 파일+manifest hash/size lock-step **co-tamper**(콘텐츠 동시 변조) + self-contained 런타임 DLL 미해시만 남음 → **코드 서명(STAB-WP-05, APPROVAL_REQUIRED)** 후속. (비-mandatory critical **co-deletion**은 `RequiredCriticalEntries` 핀으로 **해소**.)
- **STAB-WP-04 DONE (#66 merged, main `f6b1405`, VERIFIED — local-gate)**: 비대한 단일 `Program.cs`를 외부 프레임워크 0으로 내부 Suite로 분리(`Program.cs`는 3줄 runner 호출만; `SmokeTestContext`+13개 suite 파일 SafetyTests/CsvTests/XlsxTests/MappingTests/LimitReconciliationTests/ReportTests/KbTests/NcrTests/PackagingTests/UiContractTests/GenerationTests/DataProfileTests/AuditTests + `TestRunner`+`SmokeTestHelpers`+`GlobalUsings`). **RR-10 보존 검증(Claude review)**: AssertTrue 호출 426=426, Throws 25=25, 문자열 리터럴 0건 누락(comm -23), `SmokeDomain` 분류·`=== SmokeTest Summary ===`·`Total=N PASS=N FAIL=N`·fail exit code 보존, `Total=572 PASS=572 FAIL=0` 불변, 외부 PackageReference 0. 기능 코드 변경 없음(7개 Core `string.Split`는 SDK 8.0.100 collection-expression 회피용 `new[]{}`로 동작 동일).
- **STAB-UX-01 DONE (#68 merged, main `dd286fa`, VERIFIED — local-gate)**: `MainWindow.xaml` 가변 레이아웃화(순수 WPF/XAML, **기능·계약·이벤트 시그니처 변경 0**). 고정 EditorRow `Height="260"` → `2*` MinHeight=260 + 에디터↔결과 `GridSplitter`(Rows); 컬럼 `220/*/300` → `220/*(Min480)/Auto/340(Min280 Max560)` + 중앙↔Safety `GridSplitter`(Columns); Window Min 1180×720·`ResizeMode=CanResize`; SQL/VBA/Excel/Draft TextBox `Stretch`+`AcceptsTab`+`Consolas`/14. `UiContractTests`에 XAML Contract 단언 7건(전부 `UiContract` 도메인). **Claude 사전검증(SDK 없는 Linux)**: XAML well-formed·단언 술어 7건 전부 True·Unclassified=0; **WPF build+SmokeTest는 user 로컬 게이트(Total 572→579)** 후 머지. 후속 = STAB-UX-02(레이아웃 영속화) 후보.
- **UX-WP-01 DONE (#70 merged, main `600d687`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE-with-nits)**: `Core/Assist` Smart Assist **계약+코어**(실 provider 콘텐츠·UI 아님). `CompletionContext/Item/Result`·`ICompletionProvider`·`CompletionProviderRegistry`(언어별 결정적 조회)·`CompletionEngine`(병합·dedupe(ProviderId+Label)·결정적 정렬·삽입 cap·**SafetyHint/BlockedHint pinned**·`Findings`는 cap 이전·dedupe 이후 산출→절단 안전)·NoModelMode + accept 해시 audit(`SuggestionLogEntry`/`SuggestionLogWriter`, `InsertTextHash`·`UserHash`를 writer가 `IsSha256Hex`로 강제, 원문/사용자 미저장, 삽입 이벤트만, `logs/` 한정). 불변식: 전항목 `RequiresReview=true`, 힌트 `Insertable=false`·`InsertText=""`·구조화 `Finding` 보존. SmokeTest `Assist` 도메인 +23(`Total 579→602`, 기존 suite 무변경·삭제 0). build 0/0·NuGet 0(Codex 로컬). **잔여(미탐지 nit)**: dedupe 키=ProviderId+Label에 Kind 미포함 → 동일 Label로 SafetyHint+일반항목 동시방출 시 일반항목 소실(사양 적합·안전방향 손실, UX-WP-02 가이드). **범위 밖(미구현)**: 실 provider 콘텐츠=UX-WP-02, WPF Popup=UX-WP-03.
- **UX-WP-02 DONE (#72, local-gate)**: `Core/Assist/Providers/StaticCompletionProviders.cs` 정적 provider(SQL/VBA/Excel2021/Excel365차단/SafetyHint/RiskPhrase). 차단 판단은 기존 `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`+`SafetyRuleSet` 단일 원천 재사용(룰 중복 0), Excel 허용 완성은 신규 `SafetyRuleSet.ExcelCompletionAllowFunctions`(`rules/excel_2021_completion_allow_functions.txt`, 실 worksheet 함수만) 전용. 차단 DML/DDL·금지 VBA API·Excel365 미추천+BlockedHint, SafetyHint=구조화 Finding 보존·비삽입, RiskPhrase=일반 seed만(실데이터 0). 무결성/패키징 invariant 유지(IntegrityVerifier.RequiredCriticalEntries+PackagingTests 인벤토리에 신규 rules 추가). **post-merge audit nit(기록만)**: ① Blocker가 BlockedHint+SafetyHint 2줄 노출(A-4, Source 상이로 dedupe 미적용·UX 잡음) ② 정보성 finding(SQL_EMPTY 등)까지 핀 SafetyHint(A-5) ③ `IsWorksheetFunctionName` 필터가 향후 비정형 라벨 무음 누락(A-6). 셋 다 보안·기능 영향 0.
- **UX-WP-03 DONE (#73, local-gate)**: `App/Controls/CompletionPopup.xaml(.cs)`(Popup+ListBox, 외부 Editor NuGet 0) + MainWindow SQL/VBA/Excel/RiskComment 4종 TextBox Ctrl+Space→`CompletionEngine.GetCompletions`→Popup, Enter/Tab 삽입·Esc 닫기, RiskComment=신규 `RiskCommentRequestBox`(Risk Dashboard 탭). 자동삽입 0(명시 선택만), Insertable=false(SafetyHint/BlockedHint) 선택 시 삽입 0, accept 시 UX-WP-01 `SuggestionLogWriter` 해시 audit, 구조화 Finding을 `ShowFindings` 우측 패널로 평문화 없이 전달. Core 계약/이벤트 시그니처 불변. **post-merge audit nit(기록만)**: Esc-on-ListBox 경로에서 원본 TextBox 포커스 미복원(C-7, 키보드 UX nit·보안 무관).
- **STAB-UX-02 DONE (#76 merged, main `61cf782`, VERIFIED — local-gate; Claude review APPROVE)**: `Core/Config/UiLayoutStore.cs`(창 크기·에디터/결과 분할 비율·Safety 너비를 `config/ui_layout.local.json`에 round-trip; 손상/스키마 fallback·config 경로가드·clamp) + `MainWindow` Loaded/Closing 연결(레이아웃 외 상태 미저장, 종료 비차단) + `build/01` `*.local.json` 제거·`build/03` 유입 시 실패 + `.gitignore`. **무결성 비대상**(`IntegrityVerifier` 미변경, 테스트가 `DefaultPath ∉ Mandatory/RequiredCritical` 단언 → 레이아웃 변경이 STAB-WP-03b Fail-Closed 오발 안 함). SmokeTest +15(UiContract+13·Packaging+2, `Total 631→646`, 기존 무변경). NuGet 0. **잔여 nit(기록만)**: Loaded 시 1회 리사이즈 flash·무결성 테스트 glob 미검사(구조상 자명).
- **R2-WP-01 DONE (#79 merged, main `59a752f`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE)**: `LimitMonitor` 의미 경화 — 중복 Join Key를 `group.Last()` 임의선택 제거 후 **`DUPLICATE_LIMIT`(7번째 상태, ADD-ONLY)로 차단**, 통화/단위를 하드코딩 const에서 **ColumnMapping(Optional, `TryPhysical`)으로 이관**, 휴면 **`RECON_UNIT_MISMATCH` 활성**(currency 대칭·non-fail), **BASE_DT 형식 검증/정규화**(yyyyMMdd/yyyy-MM-dd→yyyyMMdd, invalid graceful), **JoinAudit**(duplicateLimitKeys/Rows·blockedExposureRows·통화/단위 적용여부 결정적 기록). `ColumnMappingLoader.RequiredColumns` 명시 6개 축소(통화/단위 Optional·6열 config fallback 미발생)+blank optional skip. **Dashboard=Report 일원화**: `RISK_DASHBOARD_RESULT` finding·Excel SUMMARY에 `DuplicateLimitCount` 노출. 키스톤 sum-balance/row-amplification·대사 9코드·`Passed` 정의·6→7 enum ordinal 보존. SmokeTest **`Total=646→671 (+25)`**(LimitRecon/Mapping/Report/UiContract 회귀, Unclassified=0, 기존 무변경). NuGet 0. **잔여 nit(기록만, 머지 영향 0)**: invalid BASE_DT 시 `RECON_BASEDATE_MISMATCH` 다중 가산(non-fail·게이트 무영향) · JoinAudit 물리컬럼명 포함(인메모리 metadata·해시Audit/Repo 비대상). 실 Test PC Gate B/C=**BLOCKED**.
- **R2-WP-02 DONE (#81 merged, main `5280d54`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE)**: `CsvReader` streaming 리딩(forward-only) + 행 상한 `MaxRowCount=200_000`·바이트 상한 `MaxByteSize=50MB`(초과 시 `InvalidDataException max=/actual=`, XlsxReader 패턴 동형) + `Cp949Decoder.DecodeLines(Stream)`(기존 `Decode(byte[])`와 **바이트 동일** 디코드·StringReader.ReadLine 동일 줄분할 → CP949 streaming 결정성 보존). `DataProfiler.ProfileCsvStreaming(...)` 추가; `NumericAccumulator`는 전 값 보관 제거(count/sum/min/max + 스칼라) + **OutlierCount는 2차 streaming pass로 기존 2-pass와 bit-동일 재현**(legacy two-pass mean/variance; FP 분기 nit 해소); 중복행은 SHA256 해시만 보관(원문 미저장)·`DuplicateRowCount` 동치; decimal sum overflow graceful. `ProfileTable`/`ProfileCsv`/`Read` 등 기존 경로·`NumericColumnProfile`(6필드) 불변. SmokeTest **`Total=671→680 (+9)`**(streaming==in-memory 전필드 동일·Welford 3σ·대용량 결정성·byte/row cap·CP949 streaming 회귀; Unclassified=0). NuGet 0. **머지 델타(`65dca55`→`5280d54`) post-merge 검증**: `ReadStreaming`을 진짜 streaming(`IEnumerable<CsvRow>`)으로·OutlierCount를 legacy two-pass로 정정(리뷰 nit 반영)·overflow 가드·회귀 +2 — 전부 결정적·계약 보존 확인. **잔여 nit(기록만)**: streaming 경로 다회 파일 통과(50MB 상한 내 허용); Welford `mean/m2` 필드는 outlier가 legacy 경로로 회귀되며 미사용(dead·무해). 실 Test PC Gate B/C=**BLOCKED**.
- **R2-WP-03 DONE (#84 merged, main `9bc83a3`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE)**: `PriorDayAnalyzer` + `PriorDay*` records 신규. 기존 `LimitMonitor.Analyze`를 Current/Prior **2회** 호출해 `MonitoringTable`을 `(PortfolioId,RiskFactor)`로 페어링·diff(**새 엔진·새 상태·분류 재구현 0**). 키는 `LimitMonitor.BuildComparisonKey`(internal, `BuildJoinKey`가 위임 → 동작 동일)로 단일 원천화. 분류 New/Resolved/Increased/Decreased/Unchanged/StateTransition(비숫자 집합 `{NoLimit,InvalidLimit,MappingError,DuplicateLimit}` + 상태상이 → StateTransition, `IsNonNumeric`이 Normal/Warning/Breach 외 전부 비숫자로 미래 안전). 행별 Current/Prior/Δ(Exposure·Limit·UsageRatio·RemainingLimit), TopN movers(|UsageRatioDelta| desc + PF/RF Ordinal), **정규화 기준 same-day guard**(`2026-06-17`↔`20260617` 차단), **BASE_DT 미스매치 시 `BASE_DT_FORMAT_MISMATCH` finding + 매칭 행 계속 비교**(0건 강제 안 함), 4구획 출력 계약(검토용 초안·DraftNotice·LLM 0). **머지 전 보정(`87f5419`)**: 동일 Join Key 다중 monitoring 행을 `PRIOR_DAY_DUPLICATE_KEY` Hidden-Risk로 표면화(결정적 단일 비교; bot P2 + Claude nit #1 반영). R1 7상태/대사 9코드/`LimitAnalysisResult` 불변(허용된 `BuildComparisonKey` 비파괴 추가만). SmokeTest **`Total=680→698 (+18)`**(prior-day 회귀 +17 + duplicate-key +1, Unclassified=0, 기존 무변경). NuGet 0·차트/LLM/Vector 0. **잔여 nit(기록만)**: movers에 Unchanged(|Δ|=0) 포함(최하위)·`IsNonNumeric` 중복 조건(무해). 실 Test PC Gate B/C=**BLOCKED**.
- **NEXT UP (다음 WP)**: **`R2-WP-04`(Visualization/Report 강화 — 정확 Exception Count(Number SoT)·신규 `RISK_VISUAL` 인박스 시각화 데이터 시트(7상태 분포·TopN movers·집중도 HHI[분모=`Abs(Exposure)`]·Heatmap 등급)·WPF in-box 차트(Shapes/Canvas)**. **외부 charting NuGet(OxyPlot/LiveCharts/ScottPlot) 금지 → 발견 즉시 STOP**. 인박스·NuGet 0·`LimitAnalysisResult`/7상태/`BuildReport` 계약 불변. **WP 상세 = `docs/39 §R2-WP-04`, Codex 프롬프트 = `prompts/codex/R2-WP-04_visualization_report.md`**(P2 반영본, #82 하드닝 적용). **R2 트랙 마지막 WP.** 병행: PILOT Gate B/C(BLOCKED, 실 Test PC).
- **그 다음 후보(순서, NEXT UP 아님)**: R2-WP-04(R2 트랙 마지막) → 이후 KB(원문 Clause/Chunk, Vector/Embedding STOP·승인 필요) / R5 Feedback / PILOT Gate B/C(실 Test PC 증거). **STAB-WP-05**(Authenticode 코드 서명)=**APPROVAL_REQUIRED/STOP**이라 구현 NEXT UP 부적격.
- **BLOCKED**: PILOT Gate B/C(실 Test PC 증거 대기 — `docs/45`). 신규 기능과 분리해 user/Test PC가 병행.
- **재현 검증**: `git fetch origin main && git switch main && dotnet build RiskManagementAI.sln -c Release && dotnet run --project tests/RiskManagementAI.SmokeTests` → 종료부의 두 줄 `=== SmokeTest Summary ===` 및 `Total=N PASS=N FAIL=0 Duration=...s` 확인(정본 합계). CI/로그 grep은 **`Total=`** 사용.
- **운영 모델(Local-Gate)**: GitHub Actions 2,000분/월 소진(**~2026-06-30 리셋 예정**) 동안 build/test/packaging는 **전부 로컬 실행**. 머지 게이트 = 로컬 `Total=N PASS/0 FAIL` 증거 + Claude 코드리뷰(GitHub CI green 전제 아님). `ci.yml`·`governance-soft-guard.yml`는 `workflow_dispatch` 수동(분 가용 시), `ci.yml` test=ubuntu·wpf=windows. (`CLAUDE.md §11.6`)
- **테스트 수 변경 규약**: 총수 감소 시 사유·매핑 기록(삭제·약화 금지). STAB-WP-02가 합계·도메인 요약·실행시간을 출력하고, 미분류 도메인(`Unclassified`)이 남으면 실패한다.
- **⚠️ Archived(재실행 금지) 프롬프트**: `prompts/codex_mvp1_implementation_prompt.md`, `prompts/codex_mvp2_*`, `prompts/codex_mvp3_ui_prompt.md`, `prompts/codex_goal_mode_prompt.md`, `prompts/claude_bootstrap_v2_prompt.md`, `prompts/codex/WP-01~07_*`, `prompts/codex/R3-WP-01~05_*`, `prompts/codex/REL-v0.6-packaging-guard.md`, `prompts/codex/STAB-WP-01_*`, `prompts/codex/STAB-WP-02_*`, `prompts/codex/STAB-WP-04_*`, `prompts/codex/STAB-UX-01_*`, `prompts/codex/UX-WP-01_*` — 모두 **완료/Starter** 단계. 신규 작업은 본 Resume Brief의 NEXT UP만 따른다.

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

## STAB-WP-01. Build / Version Reproducibility — **DONE** (RR-11)
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
> **분할 완료**: **03a(build측 — Release 보안 + manifest 생성/검증) = DONE(#59, VERIFIED local-gate)**. **03b(runtime — 앱 시작 Fail-Closed) = DONE(#61, VERIFIED local-gate, main `682f1d8`)**. `Total=572 PASS=572 FAIL=0`, Gate A 0, build/00~03 PASS. 비-mandatory critical co-deletion은 `RequiredCriticalEntries` 핀으로 해소. 독립 신뢰 앵커(코드 서명)와 self-contained 런타임 DLL 미해시는 **STAB-WP-05(APPROVAL_REQUIRED)** 로 분리.
- **목표**: (a) Release 산출물에서 **PDB/개인 경로/SourceLink/Debug·Test config/Unsafe BinaryFormatter** 부재 보장(`DebugSymbols=false`, `DebugType=none`, allowlist), (b) **`approved_manifest.json`**(핵심 파일 path·size·SHA256·version·required·security class) 생성 + **앱 시작 시 무결성 검증**(개발=Fallback 경고, 운영=Fail-Closed: policy 불일치→기동/기능 차단, rules→검사 차단, template→Report 차단, KB→검색 차단).
- **선행조건**: STAB-WP-01.
- **작업범위**: `build/01`/`03`에 Release 보안 검증 추가, manifest 생성·검증 모듈(인박스, NuGet 0), 시작 시 검증 + 모드 분기. Code Signing은 **운영 절차 Placeholder**(자동서명 미구현).
- **제외범위**: 실제 Code Signing 인증서, 기능 로직.
- **수정예상파일**: `build/01~03`, `Core/Integrity/*`(신규), App 시작부, `config/approved_manifest.json`(생성물 또는 placeholder).
- **보안조건**: 운영 Fail-Closed/개발 Fallback 명확 분리. 해시 전용. 외부 0.
- **테스트**: 정상=PASS, 변조 파일=차단(도메인별), PDB/개인경로/Debug config 0 검증, ZIP에 manifest 포함.
- **Branch**: `feature/stab-wp-03-integrity` · **Commit**: `feat: release security guard + integrity manifest with fail-closed verify (STAB-WP-03)`
- **Claude Review Checklist**: PDB/개인경로/Debug 0 / manifest 검증 / 운영 Fail-Closed·개발 Fallback / 핵심파일 분류 / NuGet 0 / 기존 테스트 유지.
> **DONE 증거(03b #61)**: null/malformed/rooted/traversal manifest entry fail-closed, mandatory/critical required-by-path, manifest shrink(엔트리 드롭+파일 잔존), mandatory co-deletion, non-mandatory critical co-deletion 모두 SmokeTest로 고정. 남은 미탐지 양성 고정은 파일+manifest hash/size lock-step co-tamper뿐이며 STAB-WP-05 서명 앵커 전까지 과대표기 금지.

## STAB-WP-04. SmokeTest Suite Structure (RR-10 보호) — **DONE (#66, VERIFIED local-gate)**
- **상태**: **DONE** (main `f6b1405`). 단일 `Program.cs` → 3줄 runner + `SmokeTestContext` + 13개 suite(SafetyTests/CsvTests/XlsxTests/MappingTests/LimitReconciliationTests/ReportTests/KbTests/NcrTests/PackagingTests/UiContractTests/GenerationTests/DataProfileTests/AuditTests) + `TestRunner`/`SmokeTestHelpers`/`GlobalUsings`.
- **목표**: 비대한 단일 `Program.cs`를 **외부 프레임워크 0**으로 내부 Suite로 분리. **테스트 삭제·약화 금지**, 총수 보존.
- **선행조건**: STAB-WP-02.
- **테스트**: 분리 전후 총수·이름 동일(매핑표), 도메인 Summary, Golden File 유지, 실패 exit code 유지.
- **DONE 증거(Claude review)**: AssertTrue 426=426, Throws 25=25, 문자열 리터럴 0건 누락(comm -23), `SmokeDomain`/`=== SmokeTest Summary ===`/`Total=N PASS=N FAIL=N`/fail exit code 보존, **`Total=572 PASS=572 FAIL=0` 불변**, PackageReference 0, 기능 변경 없음(Core `string.Split` 7건은 SDK 8.0.100 collection-expression 회피용 `new[]{}`로 동작 동일).
- **Branch**: `feature/stab-wp-04-test-suites` · **Commit**: `test: split SmokeTest into internal suites without loss (STAB-WP-04)`
- **Claude Review Checklist**: 총수 보존+매핑 / 단언 불변 / 도메인 Summary / 외부 0. → **전부 PASS**.

## STAB-WP-05. Authenticode 코드 서명 — 독립 신뢰 앵커 (APPROVAL_REQUIRED · STOP)
- **상태**: **APPROVAL_REQUIRED**. STAB-WP-03b interim이 닫지 못한 **manifest 독립 신뢰 앵커**(쓰기 가능 폴더에서 파일+manifest를 lock-step 동시 변조하는 co-tamper) + **self-contained 런타임 DLL(~150개) 미해시** + 폴더 동반 변조를 닫는다. 외부 신뢰 루트(인증서·서명 도구)가 필요하므로 **STOP 규칙(§11.5)** — 승인(`docs/41`/ADR-008 §결정4·5) 전 진행 금지.
- **목표**: 관리 어셈블리(`RiskManagementAI.dll`/`.exe`) Authenticode 서명 + 시작 시 자기 서명/게시자 검증을 신뢰 앵커로 하여 manifest 신뢰를 확립(서명 검증 후에만 manifest 신뢰). 런타임 DLL 범위는 서명 카탈로그/배포 정책으로 확장.
- **선행조건**: STAB-WP-03b 머지. **승인 문서**(인증서 발급 주체·반입 절차·검증 정책·Rollback).
- **제외범위(STOP 전)**: 자동 서명 파이프라인, 인증서 저장·반입 자동화.
- **테스트(승인 후, Windows 실 Test PC)**: 정상 서명 패키지=기동, 미서명/서명 불일치=차단, co-tamper(파일+manifest 동시 변조)=서명 앵커로 **차단**(03b에서 미탐지였던 케이스 회귀로 PASS 전환).
- **Claude Review Checklist**: 외부 신뢰 루트 승인 근거 / 서명 검증이 manifest 신뢰의 선행 / 03b 잔여위험 3건 폐쇄 매핑 / 절대원칙·NuGet 정책 유지.

## STAB-UX-01. Resizable Editor Layout (WPF 레이아웃 안정화, 기능변경 0) — **DONE (#68, VERIFIED local-gate)**
- **상태**: **DONE** (main `dd286fa`). 변경: `MainWindow.xaml`(Window Min 1180×720·ResizeMode=CanResize, 고정 EditorRow 260→`2*`/MinHeight·에디터↔결과 GridSplitter, 컬럼 220/*/300→220/*(Min480)/Auto/340(Min280 Max560)·중앙↔Safety GridSplitter, SQL/VBA/Excel/Draft TextBox Stretch+AcceptsTab+Consolas/14) + `UiContractTests`(XAML Contract 단언 7건, 전부 `UiContract`). **DONE 증거**: Claude 사전검증(XAML well-formed·단언 술어 7건 True·Unclassified=0) + user 로컬 게이트 머지(WPF build + SmokeTest `Total 572→579`). 기능·계약·이벤트 시그니처·NuGet 변경 0.
- **목표**: `MainWindow.xaml`의 **고정 높이 EditorRow(`Height="260"`)** 때문에 SQL/VBA/Excel/리스크 코멘트 편집 영역이 좁고 창 크기에 반응하지 않는 문제를 해소한다. **GridSplitter 기반 가변 레이아웃**으로 에디터/결과 패널·중앙/우측 Safety 패널을 사용자가 조절하고, 창 리사이즈에 비례 반응하게 한다. **기능·계약·데이터 흐름 변경 0**(순수 레이아웃/XAML).
- **선행조건**: 없음(안정 기준선 `f6b1405`). UX-WP-03(Completion Popup)과 충돌 없는 토대.
- **작업범위**:
  - **Window**: `Width="1180" Height="720"`, **`MinWidth="1180" MinHeight="720"`**, `ResizeMode="CanResize"`, `SizeToContent="Manual"`.
  - **중앙 작업 Grid**(현 `Grid.Row="1" Grid.Column="1"`): EditorRow `Height="260"`(고정) → **`Height="2*" MinHeight="260"`**, 그 아래 **Splitter Row `Height="8"`** + GridSplitter(에디터/결과 세로 분할), ResultRow `Height="*"` → **`Height="1*" MinHeight="180"`**.
  - **중앙↔우측 Safety 패널**: 최상위 Grid 컬럼에 GridSplitter 추가, Safety 패널 컬럼 `Width="300"` → **`Width="340" MinWidth="280" MaxWidth="560"`**. **좌측 메뉴 컬럼은 `Width="220"` 고정 유지**, 중앙 컬럼 `Width="*"`.
  - **편집 TextBox**(SQL/VBA/Excel/Draft): `HorizontalAlignment="Stretch" VerticalAlignment="Stretch"`, `AcceptsReturn="True" AcceptsTab="True"`, `VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto"`, `FontFamily="Consolas" FontSize="14"`.
- **제외범위**: 레이아웃 영속화(`config/ui_layout.local.json` 저장/복원)는 **후속 WP(STAB-UX-02 후보)**. Smart Assist 기능(UX-WP-01~03), 신규 컨트롤/탭, 데이터/계약 변경, 신규 NuGet.
- **읽을문서**: `docs/46`(UX 설계 §Resizable Workspace Layout), `docs/14`(UI), `App/MainWindow.xaml`.
- **수정예상파일**: `App/MainWindow.xaml`(레이아웃만), 필요 시 `App/MainWindow.xaml.cs`(GridSplitter 관련 최소 코드비하인드 — 자동실행/계약 변경 0), `tests/.../`(XAML Contract SmokeTest).
- **Public Interface**: 없음(앱 내부 레이아웃). Core 계약·이벤트 핸들러 시그니처 불변.
- **구현세부**: 순수 XAML 레이아웃. 기존 `x:Name`·이벤트 바인딩·탭 구조 보존. GridSplitter는 `ResizeBehavior`/`ResizeDirection` 명시. 외부 Editor 패키지 0. 자동실행 0.
- **보안조건**: 외부 NuGet 0. 기능/데이터 흐름·자동실행·로그 변경 0. 실데이터·원문 0.
- **테스트**(XAML Contract SmokeTest, 외부 프레임워크 0): ① `MainWindow.xaml`에 **GridSplitter ≥1 존재**, ② EditorRow가 **고정 `Height="260"`이 아님**(`2*`/`MinHeight`), ③ Window `MinWidth`/`MinHeight` 설정됨(1180/720), ④ SQL/VBA TextBox `Stretch`+`Consolas`/`14`, ⑤ Safety 패널 컬럼 `MinWidth`/`MaxWidth` 설정, ⑥ **기존 STAB-WP-04 정본 572 보존 + XAML Contract +7 → `Total=579 PASS=579 FAIL=0`**(전부 `UiContract`), ⑦ NuGet PackageReference 0.
- **완료조건**: 가변 레이아웃 + GridSplitter(에디터/결과·중앙/우측) + 창 Min + TextBox Stretch + XAML Contract Test. build 0/0(WPF 로컬 컴파일) · `Total` 보존+신규.
- **Branch**: `feature/stab-ux-01-resizable-layout` · **Commit**: `feat: resizable editor layout with grid splitters (STAB-UX-01)`
- **Claude Review Checklist**: 고정높이 제거(EditorRow 비고정) / GridSplitter 존재(에디터-결과·중앙-우측) / Window Min 1180×720 / TextBox Stretch+Consolas/14 / Safety 패널 Min·Max / 기능·계약 변경 0 / 기존 SmokeTest 보존 / XAML Contract Test / NuGet 0 / Gate A.

## STAB-UX-02. Resizable Layout Persistence (세션 간 레이아웃 영속화) — **DONE (#76, VERIFIED local-gate)** (CAP-UX 사용성)
- **목표**: STAB-UX-01이 도입한 **창 크기 + 에디터/결과 분할 비율(EditorRow:ResultRow) + 중앙/Safety 컬럼 너비**를 종료 시 저장하고 다음 실행에 복원한다. 매 실행 기본 레이아웃으로 리셋되는 불편 해소. **순수 WPF + 인박스 `System.Text.Json`, 외부 NuGet 0.**
- **선행조건**: STAB-UX-01(#68, GridSplitter `EditorResultSplitter`/`WorkspaceSafetySplitter`·`MainTabs` 그리드 행/열). 안정 기준선 `eae1766`.
- **작업범위**:
  - `Core` 레이아웃 store: `config/ui_layout.local.json`에 `{ WindowWidth, WindowHeight, EditorRowStar, ResultRowStar, SafetyColumnWidth, SchemaVersion }` 직렬화. **경로 가드**는 기존 `PolicyLoader`/`LogPathResolver` 패턴 재사용(`config/`-상대 JSON만, traversal/rooted 거부).
  - `MainWindow`: `OnStartup`/`Loaded`에서 저장값을 읽어 `Window.Width/Height`·중앙 Grid `RowDefinition.Height`(star)·Safety `ColumnDefinition.Width`에 적용(MinWidth/MaxWidth/MinHeight 범위로 clamp). 종료(`OnClosing`) 또는 splitter `DragCompleted`에서 저장(과도한 쓰기 방지 — 종료 시 1회 또는 디바운스).
- **제외범위**: **레이아웃 외 상태 영속화 금지**(데이터/입력 텍스트/감사/탭 선택 등). 신규 레이아웃 기능(STAB-UX-01 범위), Smart Assist 로직, 신규 NuGet.
- **읽을문서**: `docs/46 §8`, `App/MainWindow.xaml(.cs)`(STAB-UX-01 GridSplitter·행/열), `Core/Config/PolicyLoader.cs`(config 경로 가드), `Core/Logging/LogPathResolver.cs`, `Core/Integrity/IntegrityVerifier.cs`(아래 무결성 제약).
- **수정예상파일**: **`Core/Config/UiLayoutStore.cs`(신규, Core 필수 — App 배치 금지: SmokeTest가 Core만 참조)**, `App/MainWindow.xaml.cs`(load/save 연결 호출만), `build/01_publish-win-x64.ps1`(config 복사에서 `*.local.json` 제외), `.gitignore`(+`config/ui_layout.local.json`), `tests/.../`(round-trip·fallback·경로가드·publish부재 SmokeTest).
- **Public Interface**: `UiLayoutStore.Load()→UiLayout(불변 record, 안전 기본값)` · `UiLayoutStore.Save(UiLayout)`. Core 계약·이벤트 시그니처 불변.
- **구현세부**: 결정적·안전 fallback. 파일 부재/손상/스키마 불일치 → **예외 없이 기본 레이아웃**(STAB-UX-01 기본값). 쓰기는 `config/` 한정. 자동실행 0.
- **보안조건(★ 무결성 제약)**: `config/ui_layout.local.json`은 **런타임 사용자 가변 상태** → **build/01 manifest·`IntegrityVerifier`(Mandatory/RequiredCritical/CriticalGlobs)·패키징 인벤토리·git 어디에도 포함 금지**(포함 시 레이아웃 변경마다 STAB-WP-03b Fail-Closed 오발). config 루트(`config/ncr/` 아님)에 두어 `CriticalGlobs(config/ncr,*.json)` 비대상 유지. **★ publish 제외**: `build/01`이 `config/` 전체를 `Copy-Item -Recurse`하므로 패키징 직전 존재 시 ZIP 유입 → build/01 config 복사에서 `*.local.json` 제외 + publish/ZIP 부재 검증. `.gitignore`에 추가. 외부 NuGet 0·실데이터 0.
- **테스트**(SmokeTest, 외부 프레임워크 0): round-trip(Save→Load 동일값), 파일 부재/손상 JSON → 기본값 fallback(예외 0), 경로 가드(`config/` 밖·traversal 거부), clamp(Min/Max 범위 밖 저장값 보정), **레이아웃 파일이 무결성 critical 인벤토리에 없음**(부재/변경이 startup Fail-Closed 유발 안 함) 단언, **publish/ZIP에 `*.local.json` 부재** 단언, `.gitignore` 포함 확인. **분류**: 분류기에 bare `layout`/`UiLayout` 키워드가 없으므로 단언 메시지를 `UI layout ...`으로 시작(기존 `"UI "` 키워드 → `UiContract`) 또는 분류기에 키워드 추가 — `Unclassified=0` 유지. 기존 `Total=631` 보존+신규.
- **완료조건**: 영속화 store + load/save 연결 + 안전 fallback + 무결성 비대상 + `.gitignore` + SmokeTest. build 0/0(WPF 로컬) · `Total` 보존+신규.
- **Branch**: `feature/stab-ux-02-layout-persistence` · **Commit**: `feat: persist resizable layout across sessions (STAB-UX-02)`
- **Claude Review Checklist**: 레이아웃만 영속(데이터/입력 미저장) / config 경로 가드 / **무결성 critical 비대상**(레이아웃 변경이 Fail-Closed 유발 안 함) / `.gitignore` / 안전 fallback(손상→기본값) / clamp / NuGet 0 / 기존 테스트 보존 / Gate A. **Codex 프롬프트**: `prompts/codex/STAB-UX-02_layout_persistence.md`.
- **Codex 결과(2026-06-29, review pending)**: `UiLayoutStore`를 Core에 구현(기본값·round-trip·손상/스키마 fallback·config 경로가드·clamp), `MainWindow`는 Loaded/Closing에서 호출만 수행(레이아웃 외 상태 저장 0). `build/01`은 `*.local.json` 제거, `build/03`은 `*.local.json` 유입 시 실패. `config/ui_layout.local.json`은 `.gitignore` 포함·manifest/RequiredCriticalEntries 비대상. Local-gate: `dotnet build` 0/0, SmokeTest **`Total=646 PASS=646 FAIL=0`**, Gate A 0.

## PILOT-WP-01. v0.6 Offline Gate B/C Evidence (BLOCKED, user/Test PC)
- **목표**: `docs/45` v0.6 Gate B/C 증거 시트를 실 오프라인 Test PC에서 채워 봉인. **실 PC 증거 없으면 PASS 금지(BLOCKED 유지).**
- **성격**: Codex 코드 작업 아님(문서/운영). Claude는 증거 회신 시 항목별 PASS/BLOCKED 재판정. 신규 기능과 분리·병행.

## UX Assist Track — Smart Assist / Inline Assist (UX-WP-01~03)
> 권위 설계 = `docs/46`, ADR-010(`docs/40`). 전체 생성(`DraftPipeline`)과 **별개**. 정적·NoModel·외부 Editor 패키지 0·자동삽입/자동실행 0·해시 audit. (STAB 이후 R2와 **병행 가능**, 선행 = 없음/안정 기준선.)

## UX-WP-01. Smart Assist Core (Engine·Provider 계약, NoModel) (CAP-UX-01, CAP-UX-08) — **DONE (#70, VERIFIED local-gate)**
- **상태**: **DONE** (main `600d687`, Claude 4축 리뷰 APPROVE-with-nits). 구현: `Core/Assist/{CompletionContracts,CompletionEngine,CompletionProviderRegistry,ICompletionProvider,SuggestionLogEntry,SuggestionLogWriter}.cs` + `AssistTests.cs`(AssertTrue 23, `Assist` 도메인) + 분류기/Runner/Usings 등록. **계약+코어 한정** — 실 provider 콘텐츠는 UX-WP-02, WPF Popup은 UX-WP-03(둘 다 NOT_IMPLEMENTED). 검증: 전항목 RequiresReview·힌트 비삽입+InsertText 비움+Finding 보존·`Findings` cap 이전 산출(절단 안전)·결정적 정렬/dedupe·NoModel·accept 해시 audit(`InsertTextHash`/`UserHash` writer 강제, 원문 미저장, 삽입 이벤트만, logs/ 한정)·`Total 579→602`(기존 무변경). **nit**: dedupe 키=ProviderId+Label에 Kind 미포함(동일 Label 다중 Kind 동시방출 시 일반항목 소실 — 사양 적합·안전방향, UX-WP-02 가이드).
- **목표**: inline 완성 엔진의 **계약과 코어**를 만든다 — `CompletionEngine`·`CompletionContext`·`CompletionItem`·`ICompletionProvider`·Provider Registry. **NoModelMode 완전 동작**. Accept 시 해시 audit.
- **선행조건**: 없음(안정 기준선). 후속 UX-WP-02/03의 토대.
- **작업범위**: `Core/Assist/`에 `CompletionLanguage`/`CompletionItemKind` enum, `CompletionContext`/`CompletionItem`/`CompletionResult` record, `ICompletionProvider`, `CompletionProviderRegistry`, `CompletionEngine`(병합·중복제거·결정적 정렬·개수 상한). **`CompletionItem`에 `Insertable`(SafetyHint/BlockedHint=false)·`Finding`(구조화 `SafetyFinding` 보존)·`RequiresReview`(전 항목 true) 포함. `CompletionResult`에는 `Findings` 컬렉션을 두어 SafetyHint/BlockedHint의 구조화 finding을 UI가 문자열 Warnings에 의존하지 않고 전달받게 한다.** `SuggestionLogEntry`(+`InsertTextHash`·`Kind`) + accept audit writer(`TaskLogWriter` 패턴, 해시 전용). SmokeTest 분류기에 **`Assist` 도메인** 추가.
- **제외범위**: 실제 provider 콘텐츠(UX-WP-02), WPF UI(UX-WP-03), LLM(R4).
- **읽을문서**: `docs/46`, `docs/40` ADR-010, `Core/Logging`(LogHash/TaskLogWriter), `Core/Safety`(SafetyFinding).
- **수정예상파일**: `Core/Assist/*.cs`(신규), `tests/.../Program.cs`(회귀 + `Assist` 도메인 매핑).
- **Public Interface**: `CompletionResult CompletionEngine.GetCompletions(CompletionContext)`; `CompletionResult(Items, Mode, Warnings, Findings)`; `interface ICompletionProvider`; `CompletionProviderRegistry.Register/Resolve`; `CompletionItem(Label, InsertText, Kind, Source, RequiresReview, Insertable, Finding, SafetyNote, SortKey)`.
- **구현세부**: 순수·결정적. 모델 의존 0. Engine `Mode="NoModel"`. 동일 Context→동일 결과. **전 항목 `RequiresReview=true`**; `SafetyHint/BlockedHint`는 `Insertable=false`·`InsertText=""`·구조화 `Finding` 보존. **SafetyHint/BlockedHint는 개수 상한보다 먼저 pinned 처리하고, 일반 추천 cap 때문에 `CompletionResult.Findings`가 누락되면 안 된다.** accept 로그에 **입력 원문/삽입 본문 미저장** — `SuggestionId`=provider+Label 해시, **`InsertTextHash`=`LogHash.Sha256Hex(InsertText)`**, `UserHash`=`LogHash.Sha256Hex`.
- **보안조건**: 외부 0·NuGet 0·자동실행 0. 로그 = id/provider/kind/mode/userHash/insertTextHash/시각만. 쓰기 = `logs/`.
- **테스트**(이름에 `Assist`/`completion` 등 분류 키워드): 결정성, 언어 라우팅, 개수 상한, **cap 적용 후에도 SafetyHint/BlockedHint와 `CompletionResult.Findings` 보존**, NoModel, **전 항목 RequiresReview**, **SafetyHint Insertable=false**, accept audit 1건·**InsertTextHash 기록·원문 미저장 단언**, `Unclassified=0`.
- **완료조건**: CompletionEngine/Context/Item(Insertable·Finding)/ICompletionProvider/Registry + NoModel + accept 해시 audit + `Assist` 도메인 + SmokeTest. build 0/0·`Total` 보존+신규.
- **Branch**: `feature/ux-wp-01-completion-core` · **Commit**: `feat: smart assist completion core + provider contract (UX-WP-01)`
- **Claude Review Checklist**: 계약 명확(Insertable·Finding·CompletionResult.Findings 포함) / 결정성 / NoModel / cap이 safety finding을 누락하지 않음 / 전항목 RequiresReview / 힌트 비삽입 / 해시 audit(InsertTextHash·원문 미저장) / `Assist` 도메인·Unclassified 0 / NuGet 0 / 기존 테스트 불변 / Gate A.

## UX-WP-02. Static SQL/VBA/Excel/Risk Providers (CAP-UX-02~06) — **DONE (#72, local-gate, VERIFIED 범위=정적·NoModel)**
- **목표**: 정적 provider 5종 — SQL keyword/snippet(조회전용), VBA 안전 snippet, Excel 2021 함수, Excel 365 차단+대체 힌트, SafetyHint(기존 Checker 재사용), Risk phrase seed.
- **선행조건**: UX-WP-01.
- **작업범위**: `SqlCompletionProvider`·`VbaCompletionProvider`·`Excel2021CompletionProvider`·`Excel365BlockedHintProvider`·`SafetyHintProvider`·`RiskPhraseProvider`. 차단 목록은 **기존 `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`+RuleSet 재사용**(중복 정의 금지). Excel 허용 완성 함수는 UX-WP-02가 추가하는 전용 RuleLoader 소스 `rules/excel_2021_completion_allow_functions.txt`(또는 동등한 RuleSet 그룹 `excel_completion_allow`)에서만 읽고, `ExcelPreferredFunctions`를 직접 allow-list로 사용하지 않는다.
- **제외범위**: WPF UI(UX-WP-03), LLM 랭킹(R4), 스키마 introspection.
- **읽을문서**: `docs/46`, `CLAUDE.md §4·§5·§6`, `Core/Safety`(기존 Checker/RuleSet), `docs/16`(VBA).
- **수정예상파일**: `Core/Assist/Providers/*.cs`(신규), `rules/`(필요 시 seed, 실데이터 0), `tests/.../Program.cs`.
- **Public Interface**: 각 provider가 `ICompletionProvider` 구현(`ProviderId`·`Supports`·`GetCompletions`).
- **구현세부**: 결정적. **전 항목 `RequiresReview=true`**. SQL 차단 DML/DDL 미추천+`BlockedHint`. VBA 금지 API 미추천. **Excel 차단 목록은 `Excel2021FunctionChecker`/RuleSet 단일 원천에서만**(provider 자체 하드코딩 금지) → 365 입력 시 2021 대체+`BlockedHint`. **Excel 허용 완성 함수는 전용 allow-list 소스에서만 읽고 실제 worksheet 함수만 허용**한다(`PivotTable`/`HelperColumn`/`VBA`/`SQLAggregation` 같은 안내 라벨 미추천). SafetyHintProvider는 **구조화 `SafetyFinding`(code·severity·position) 그대로 `CompletionItem.Finding` 및 `CompletionResult.Findings`에 보존**(평문화 금지), `Insertable=false`. **실 테이블명/내부규정/실데이터 seed 0**(일반 표현만).
- **보안조건**: 외부 NuGet 0. seed에 민감정보 0. RuleSet 재사용(룰 분기 금지·차단셋 단일 원천).
- **테스트**: SQL DML 미추천+`BlockedHint`, VBA 금지 API 미추천, Excel 2021 허용/365 차단+대체, **Excel provider 차단셋 = RuleSet 차단셋 동기화(drift 0) 단언**, Excel 허용 완성 allow-list가 비함수 라벨(`PivotTable`/`HelperColumn`/`VBA`/`SQLAggregation`)을 추천하지 않음, SafetyHint=기존 Checker **동일 구조화 Finding 보존**·`Insertable=false`, **전 항목 RequiresReview**·실데이터 0.
- **완료조건**: 5(+365힌트) provider + 회귀. 차단셋 단일 원천. NuGet 0. build 0/0.
- **Branch**: `feature/ux-wp-02-static-providers` · **Commit**: `feat: static SQL/VBA/Excel/risk completion providers (UX-WP-02)`
- **Claude Review Checklist**: RuleSet 재사용(차단셋 단일 원천·drift 0) / Excel 허용 완성 전용 allow-list·비함수 라벨 미추천 / 차단 DML·금지 API 미추천 / 365 대체힌트 / SafetyHint 구조화 Finding 보존·비삽입 / 전항목 RequiresReview / 실데이터·원문 0 / NuGet 0 / Gate A.

## UX-WP-03. WPF Completion Popup UI (CAP-UX-07) — **DONE (#73, local-gate, VERIFIED 범위=WPF 기본 컨트롤·외부 Editor 0·자동삽입 0)**
- **목표**: **SQL·VBA·Excel·리스크 코멘트(RiskComment) 4종 입력창 모두**에서 **Ctrl+Space**로 추천 Popup, **Enter/Tab** 삽입, **Esc** 닫기. 항목에 Source·Kind·RequiresReview 표시. Safety finding은 기존 결과 패널 연계. **자동 삽입 없음**.
- **선행조건**: UX-WP-01, UX-WP-02.
- **작업범위**: App 레이어 재사용 `CompletionPopup`(`Popup`+`ListBox`)을 **SQL·VBA·Excel·RiskComment `TextBox` 전부**에 부착(외부 Editor 패키지 0). RiskComment 입력은 `MainWindow.xaml` Risk Dashboard 탭의 분석 액션 행 아래에 `TextBox x:Name="RiskCommentRequestBox"`로 추가한다(검토용 리스크 코멘트 작성 입력 전용, DB/운영 연결 0). 입력 이벤트(Ctrl+Space/Enter/Tab/Esc) 처리, accept(삽입) 시 UX-WP-01 audit 호출.
- **제외범위**: Core 로직 변경(UX-WP-01/02), LLM, 자동 삽입.
- **읽을문서**: `docs/46`, `docs/14`(UI), `App/MainWindow.xaml(.cs)`(기존 입력창·`ShowFindings`).
- **수정예상파일**: `App/Controls/CompletionPopup.xaml(.cs)`(신규), `App/MainWindow.xaml(.cs)`(4종 입력창 부착·이벤트), `tests/.../Program.cs`(UI 계약 가능 범위).
- **Public Interface**: 없음(앱 내부 UI). Core 계약은 불변.
- **구현세부**: 추천 표시는 `CompletionEngine` 결과만. **`Insertable=false`(SafetyHint/BlockedHint) 항목은 선택해도 삽입 0**(정보 표시만), `Insertable=true`만 삽입. **자동 삽입 0**(명시 선택 시에만). 항목 구조화 `Finding`/`CompletionResult.Findings`를 `ShowFindings`로 그대로 전달(평문화 금지). 삽입 본문 로그 미저장(audit=id/InsertTextHash).
- **보안조건**: 자동삽입/자동실행 0. 외부 패키지 0. 입력 원문 로그 미저장.
- **테스트**: 4종 입력창 연결(**`RiskCommentRequestBox` 존재·RiskComment 언어 매핑 포함**), 자동삽입 없음(Insertable 항목 선택 시에만 InsertText), **비삽입 힌트 선택 시 삽입 0**, 항목 Source/Kind/RequiresReview 노출, 구조화 Finding 결과패널 전달(가능 범위 계약 테스트).
- **완료조건**: 4종 입력창 Ctrl+Space/Enter·Tab/Esc + 자동삽입 없음 + 비삽입 힌트 + 감사 연계. build 0/0(WPF 로컬 컴파일).
- **Branch**: `feature/ux-wp-03-wpf-popup` · **Commit**: `feat: WPF completion popup integration (UX-WP-03)`
- **Claude Review Checklist**: 외부 Editor 패키지 0 / **4종 입력창(RiskComment 포함)** / 자동삽입 없음 / 비삽입 힌트 / Source·Kind·RequiresReview / 구조화 Finding 결과패널 / 입력 원문 미저장 / Gate A.

## R2-WP-01. Risk Semantic Hardening (RR-15) — 상태: DONE (#79 merged `59a752f`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE)

- **목표**: `LimitMonitor`의 R1 의미 결함 5개를 **결정적으로 경화(harden)**한다 — ① 중복 Limit Key를 `group.Last()` 임의선택 대신 **명시 차단·상태화(`DUPLICATE_LIMIT`)**, ② 통화(Currency)·단위(Unit) 컬럼을 LimitMonitor 하드코딩 const(`CCY_CD`)에서 **ColumnMapping(승인형)으로 이관**, ③ 단위 비교 컬럼이 존재할 때 **`RECON_UNIT_MISMATCH` 활성화**(현재 `UnitApplicable: false` 고정), ④ **BASE_DT 형식 검증·정규화** 단계 추가, ⑤ 중복키 처리·Join 선택 규칙을 **Audit Metadata에 기록**. R1 계약(6→7상태 확장은 추가만, 대사 9종 코드·`LimitAnalysisResult`·Dashboard=Report 일원화)은 **보존**.
- **선행조건**: WP-04(ColumnMapping), WP-05(공통 `LimitAnalysisResult`·6상태), WP-06(대사 9종·`ReconciliationSummary`), STAB-WP-01~02(VERSION·정본 SmokeTest). 모두 완료(main).
- **작업범위**:
  1. **중복 Limit Key 차단/상태화**: `Analyze`의 `activeLimits` 빌드(`LimitMonitor.cs:133-136`)에서 `.ToDictionary(g=>g.Key, g=>g.Last())` 제거. 중복 그룹(`group.Rows.Count>1`)은 매칭 사전에서 **유효 한도로 채택하지 않고**, 해당 Join Key의 노출 행을 신규 상태 `LimitMonitorStatus.DuplicateLimit`(코드 `DUPLICATE_LIMIT`)로 산출·차단한다(`AddNoLimitRow`/`AddInvalidLimitRow` 패턴 재사용한 `AddDuplicateLimitRow` 헬퍼 신설). 기존 대사 `RECON_DUPLICATE_LIMIT`(Medium) 예외는 **그대로 유지**(분석 상태와 대사는 별개 정보).
  2. **통화·단위 ColumnMapping 이관**: `LogicalColumn` enum에 `CurrencyCode`·`UnitCode` 추가, `SafeDefaults()`에 더미 물리명 `CCY_CD`·`UNIT_CD` 추가, `config/column_mapping.json`에 동일 키 추가. `LimitMonitor`의 `CurrencyCodeColumn` const 제거 후 `mapping.Physical(LogicalColumn.CurrencyCode)`로 치환, `UnitCode` 동일 도입. `CreateRow`·`AddCurrencyMismatchExceptions`가 일관되게 매핑 물리명 사용.
  3. **RECON_UNIT_MISMATCH 활성**: `BuildReconciliationExceptions`의 `ReconciliationComputation` 생성(`LimitMonitor.cs:365-374`)에서 `UnitApplicable: false` 하드코딩 제거 → `currencyApplicable`과 동형으로 `unitApplicable = HasColumn(exposure, UnitCol) && HasColumn(limit, UnitCol) && canBuildExposureKey && canBuildLimitKey` 계산. `AddUnitMismatchExceptions`(통화용 `AddCurrencyMismatchExceptions:410-442` 복제, 코드 `RECON_UNIT_MISMATCH`/Medium) 신설. 컬럼 부재 시 `Applicable=false`·예외 0 유지(과대 단정 금지).
  4. **BASE_DT 검증·정규화**: `Analyze`의 `normalizedBaseDate`(`LimitMonitor.cs:85`) 단계에 입력 `baseDate` 형식 검증(`yyyyMMdd` 등 허용 패턴, `DateTime.TryParseExact`/`CultureInfo.InvariantCulture` 인박스)을 추가. **비교키 의미는 보존**(현행 `StringComparison.Ordinal` 정확일치 유지) — 정규화는 입력 `baseDate` 인자 자체의 형식 검증·표준화로 **범위를 좁게 한정**(데이터 행의 BASE_DT 값 자체를 재해석/재매칭하지 않음). 불량 형식은 기존 `RECON_BASEDATE_MISMATCH`(Low) 또는 finding으로 상태화하며 throw 금지(graceful).
  5. **Join 선택 규칙 Audit Metadata**: `LimitAnalysisMetadata`(`LimitAnalysisResult.cs:57-63`)에 Join/중복키 감사 필드(예: `IReadOnlyList<string> JoinAudit`)를 추가하고 `BuildResult`(`LimitMonitor.cs:224-260`)에서 채움 — 기록 항목: Join Key 구성(BASE_DT+PORTFOLIO_ID+RISK_FACTOR), 중복키 처리 방식(=`group.Last()` 폐지·차단), 통화/단위 적용여부(`CurrencyApplicable`/`UnitApplicable`), BASE_DT 정규화 결과. **`group.Last()` 임의선택 사실은 audit에 더 이상 나타나지 않아야 한다**(폐지됨).
- **제외범위**: Streaming/대용량 상한·Welford(R2-WP-02), 전일대비(R2-WP-03), 차트/Heatmap/Excel Report 강화(R2-WP-04). 새 입력형식·신규 대사 코드 추가 금지(기존 9종 코드 보존, 의미만 활성). 데이터 행의 BASE_DT 재해석/멀티-기준일 재매칭 금지. **DUPLICATE_LIMIT을 `ReconciliationFailCodes`(Passed)로 격상하지 않는다**(R1 `Passed` 정의 보존; 정책 변경은 별도 ADR).
- **읽을문서**: `AGENTS.md`, `CLAUDE.md §3·§4·§11.4·§11.5`, `docs/38`(§5 Traceability C-13·RR-15), `docs/39`(WP-04·05·06·R2-WP-01), `docs/40`(ADR), `docs/41 §1`(Data Gate), 기존 `Core/Risk/LimitMonitor.cs`·`LimitAnalysisResult.cs`·`Mapping/ColumnMapping.cs`·`ColumnMappingLoader.cs`·`config/column_mapping.json`.
- **수정예상파일**: `src/RiskManagementAI.Core/Mapping/ColumnMapping.cs`(enum+SafeDefaults), `src/RiskManagementAI.Core/Mapping/ColumnMappingLoader.cs`(필수/선택 분리+blank optional skip), `config/column_mapping.json`(더미 키 추가), `src/RiskManagementAI.Core/Risk/LimitMonitor.cs`, `src/RiskManagementAI.Core/Risk/LimitAnalysisResult.cs`, `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`(+신규 회귀). **실제 머지(#79) 추가 변경**: `src/RiskManagementAI.App/MainWindow.xaml.cs`·`tests/.../SmokeTestHelpers.cs`(`LimitAnalysisMetadata` ctor 동기, §310), `tests/.../MappingTests.cs`(blank optional 회귀), 그리고 **Dashboard=Report 일원화**로 `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs`(SUMMARY `DuplicateLimitCount`)·`tests/.../ReportTests.cs`·`tests/.../UiContractTests.cs`(Dashboard summary 노출 회귀). 기능변경은 신규 상태 노출 한정.
- **Public Interface**:
  - `enum LogicalColumn { BaseDate, PortfolioId, RiskFactor, ExposureAmount, LimitAmount, UseYn, CurrencyCode, UnitCode }`(추가만).
  - `enum LimitMonitorStatus { Normal, Warning, Breach, NoLimit, InvalidLimit, MappingError, DuplicateLimit }`(추가만); `LimitMonitorRow.StatusCode`에 `DuplicateLimit => "DUPLICATE_LIMIT"` 추가.
  - `LimitAnalysisKpis`에 `int DuplicateLimitCount` 추가(`FromRows` 갱신), `LimitAnalysisResult`에 `int DuplicateLimitCount => Kpis.DuplicateLimitCount` 추가.
  - `LimitAnalysisMetadata`에 `IReadOnlyList<string> JoinAudit`(또는 동등 전용 record) 추가 — 생성자 시그니처 변경 시 모든 호출부 동기 수정.
  - `ColumnMapping.Physical(LogicalColumn)`·`ColumnMappingLoadResult`·`LimitMonitor.Analyze(CsvTable, CsvTable, string)` 시그니처 불변.
- **구현세부 / 결정성**: 인박스만(NuGet 0, `System.Globalization` 등 기존 using). 동일 입력→동일 상태·예외·순서·audit(정렬 고정). 금액 `decimal` 유지. **CurrencyCode/UnitCode는 Optional 논리컬럼**으로 다뤄 `ColumnMappingLoader.ValidateCompleteMapping`의 필수 검사(`RequiredColumns`)에 포함시키지 않는다 — 기존 6열 config가 신규 키 없이도 fallback 없이 로드되도록 **필수=기존 6열 / 선택=Currency·Unit로 분리**(선택 컬럼 누락은 경고/fallback 사유 아님). DuplicateLimit 행 매칭 시 `RECON_ROW_AMPLIFICATION`/`RECON_SUM_BALANCE` 모집단 정의(기준일 필터 노출 행)와 정합 유지(증폭 오탐 금지). BASE_DT 정규화는 입력 인자 검증 한정.
- **보안조건**: 읽기 전용. 외부 API/Telemetry/AutoUpdate 0. SQL/VBA/Golden6 자동실행 0. 합성 한도 미사용. `config/`만 읽기. `config/column_mapping.json` 추가 물리명은 **더미(`CCY_CD`/`UNIT_CD`)만** — 실데이터/실 테이블·컬럼명 repo 미포함. 해시 Audit·NoModelMode 불변.
- **테스트(필수, 회귀 추가·기존 보존)**:
  - 중복키 **양성**: 동일 BASE_DT·Join Key 한도 2건 주입 → 노출 행 상태 `DUPLICATE_LIMIT`(NORMAL/BREACH 산출 안 됨) + 기존 `RECON_DUPLICATE_LIMIT` 예외 동시 존재 / **음성**: 유일 한도 → 정상 분류·`DuplicateLimitCount=0`.
  - 통화·단위 매핑: 커스텀 매핑으로 물리명 변경 시 통화/단위 비교가 매핑 경유 동작; 6열-only config는 fallback 없이 로드(`UsedFallback=false`).
  - `RECON_UNIT_MISMATCH` **양성**(양쪽 단위 컬럼+상이 값 → 예외) / **음성**(단위 컬럼 부재 → `Applicable=false`·예외 0; 값 동일 → 예외 0).
  - BASE_DT: 정상 형식 → 통과; 비정상 형식 → graceful 상태화(throw 없음, 결정적).
  - Audit: `JoinAudit`에 중복키 차단·통화/단위 적용여부 기록; `group.Last` 임의선택 문구 부재.
  - 결정성: 반복 호출 동일 결과. 기존 6상태·대사 9종 수치·`ReconciliationSummary.Passed` 불변(SmokeTest Total 보존+신규).
- **완료조건**: `group.Last()` 임의선택 제거·`DUPLICATE_LIMIT` 7번째 상태로 차단; 통화/단위 ColumnMapping 일원화(const 제거); `RECON_UNIT_MISMATCH` 컬럼 존재 시 활성; BASE_DT 검증/정규화; Audit Metadata에 Join/중복키 규칙 기록. 로컬 `dotnet build` 0/0 + `dotnet run --project tests/RiskManagementAI.SmokeTests` → `Total=N PASS / 0 FAIL`(기존 보존+신규, Unclassified=0) + NuGet 0 + Gate A 0건.
- **Codex local-gate 결과(2026-06-29, #79 merged `59a752f`, Claude 4축 리뷰 APPROVE)**: `dotnet build RiskManagementAI.sln` 0 warnings / 0 errors. `dotnet run --project tests/RiskManagementAI.SmokeTests` → `Total=671 PASS=671 FAIL=0`, Unclassified=0. 신규 단언은 DuplicateLimit 차단·Optional Currency/Unit mapping(blank optional 포함)·`RECON_UNIT_MISMATCH` 활성/비활성·BASE_DT 정상화/invalid graceful·JoinAudit 결정성(duplicateLimitKeys/Rows·blockedExposureRows·orphan duplicate)·Dashboard/Report summary 노출. 외부 NuGet 0 / 실데이터 0 / 자동실행 0. **Claude 리뷰 검증**: 6→7 enum ordinal 보존·키스톤 sum-balance/row-amplification 불변·`Passed` 정의 불변·대사 9코드 불변·ctor 3개 호출부 동기·`RequiredColumns` 6개 축소(6열 fallback 미발생). **리뷰 후 머지 델타(`4e3629d`→`59a752f`)**: Dashboard/Report `DuplicateLimitCount` 노출 + loader blank-skip 정정 + JoinAudit 카운트 세분화 + 회귀 +5(666→671) — 전부 ADD-ONLY·결정적·일원화 원칙 정합으로 post-merge 검증 완료. 실 Test PC Gate B/C=**BLOCKED**(과대표기 금지).
- **Branch**: `feature/r2-wp-01-semantic-hardening` · **Commit**: `feat: risk semantic hardening (dup key block, unit recon, base_dt, join audit) (R2-WP-01)`
- **Claude Review Checklist**: `group.Last()` 임의선택 제거 / `DUPLICATE_LIMIT` 추가만(6상태 보존) / 통화·단위 매핑 일원화·const 제거 / Currency·Unit는 Optional(기존 6열 config fallback 미발생) / `RECON_UNIT_MISMATCH` 활성·과대단정 없음 / BASE_DT 정규화 범위 좁음(비교키 의미 보존) / `Passed` 정의·대사 9종 코드 불변 / Audit 기록 / 결정성 / NuGet 0·실데이터 0·자동실행 0 / SmokeTest Total 보존+신규·Unclassified=0 / Gate A.

> **★ R2 트랙 리뷰 반영 (Codex P2 — 본 보정이 각 WP 계약/테스트에 우선 적용)**
> - **R2-WP-01**: CurrencyCode/UnitCode는 Optional → `ColumnMapping.Physical()`(미매핑 시 `InvalidDataException` throw, `ColumnMapping.cs:44`) 대신 **`TryPhysical`/`ContainsKey` 가드** 후 사용(부재 시 throw 없이 비활성). 6열 config `UsedFallback=false` 유지.
> - **R2-WP-02**: 기존 `OutlierCount`는 **2-pass**(`DataProfiler.cs:188-208`) → streaming은 2차 pass/상한-bounded 보존으로 **정확 재현**(single-pass 동일 주장 금지). CP949는 .NET Encoding이 아니라 커스텀 `Cp949Decoder.Decode(byte[])` → streaming도 **바이트 디코더 재사용**(CP949 streaming 명시 테스트).
> - **R2-WP-03**: `DuplicateLimit`(WP-01 신규)을 **비숫자 상태 집합 포함**. BASE_DT 형식 차이만으로 **비교 0건 강제 금지**(빈/실패 선택 기준). `PriorDayComparisonRow`에 **Current/Prior/Δ `LimitAmount` 필드 포함**(Exposure·Limit·UsageRatio·RemainingLimit 전부).
> - **R2-WP-04**: report/aggregator는 **`DuplicateLimit` 상태 존재 시 카운트 포함**. 집중도/HHI는 **`Abs(ExposureAmount)` 기준**(usage ratio가 Abs 사용, `LimitMonitor.cs:576`) + **분모 0 graceful** 회귀.

## R2-WP-02. Streaming / Performance (RR-08) — 상태: DONE (#81 merged `5280d54`, VERIFIED — local-gate; Claude 4축 리뷰 APPROVE)

- **상태**: **VERIFIED**(local-gate, #81 `5280d54`; Claude 4축 리뷰 APPROVE). `dotnet build` 0/0, SmokeTest `Total=680 PASS=680 FAIL=0`(직접 local-gate 재확인, 2026-06-30), Unclassified=0, NuGet 0. CP949 streaming 결정성·OutlierCount 2-pass bit-동일·상한 `InvalidDataException`·중복행 SHA256·기존 경로 불변 검증. **머지 델타(`65dca55`→`5280d54`)**: `ReadStreaming` 진짜 streaming화·OutlierCount legacy two-pass 정정(리뷰 nit 반영)·overflow 가드·회귀 +2, post-merge 검증 완료. 실 Test PC Gate B/C=**BLOCKED**.
- **목표**: 대용량/손상 CSV 입력을 **메모리 안전한 streaming**으로 처리하고(행/바이트 안전 상한 도입), `DataProfiler`의 수치 통계는 Welford 누산으로 전 값 보관을 피하되 기존 OutlierCount와 bit-동일한 결과를 위해 legacy-compatible 추가 streaming pass를 허용한다. **하나의 명확한 목표 = "전 행 메모리 적재 없이도 결정적·상한적·메모리 안전한 CSV 프로파일/리딩 경로를 추가한다."** 기존 `CsvReader.Read`/`DataProfiler.ProfileTable`/`ProfileCsv` 시그니처·동작·결과는 **불변 보존**(신규 옵트인 경로로만 추가). (선택) `System.Diagnostics.Stopwatch` 기반 벤치 훅을 `logs/` 한정 로컬 기록으로 추가.
- **선행조건**: 없음(R1 WP-02 CsvReader·MVP-1 DataProfiler 기준선 존재). STOP 트리거 없음(인박스 `System.IO`/`System.Diagnostics`만).
- **작업범위**:
  1. **CSV streaming 리딩**: `File.ReadAllBytes` 전체 적재(`CsvReader.cs:26`) 대신 `StreamReader` 기반 forward-only 신규 경로(예: `CsvReader.ReadStreaming(...)` 또는 `IEnumerable<CsvRecord>` yield 파서). 기존 `Read` 시그니처·동작 보존.
  2. **행/바이트 안전 상한**: `MaxRowCount`·`MaxByteSize` 상수 도입(`XlsxReader.cs:10-14` const 상한 패턴 동일 차용). 초과 시 `XlsxReader`와 **동일 예외형(`InvalidDataException`) + 동일 메시지 규약(`max=… , actual=…`)**. 손상/무한대 파일 차단.
  3. **Welford 온라인 통계**: `DataProfiler` `NumericAccumulator`(`DataProfiler.cs:146`)의 `List<decimal> values` 전체 보관 + 2-pass 분산을 **Welford(count/mean/M2 스칼라 필드) 1-pass**로 교체. Sum/Min/Max는 스칼라 누적으로. **기존 결정적 결과 정확 재현**(population variance `/n`, n<4면 OutlierCount=0, 3σ 임계, std==0이면 0).
  4. **중복행 검출 메모리 안전화**: `duplicateKeys` `HashSet<string>`(원문 행 전체 키 보관, `DataProfiler.cs:59,76`)을 **해시 기반(원문 미저장 원칙 부합) + 상한 가드**로 전환하되 `DuplicateRowCount` 결과는 기존과 동일.
  5. **streaming 프로파일 진입점**: `DataProfiler.ProfileTable(CsvTable)`/`ProfileCsv(string)` 보존 + streaming 신규 오버로드(`IEnumerable<CsvRow>`/`IEnumerable<CsvRecord>` 소비) 추가.
  6. (선택) **벤치 훅**: `Stopwatch` 측정값을 외부 전송 없이 `logs/` 로컬 로그로만 기록.
- **제외범위**:
  - **XLSX streaming**(`XlsxReader.cs:198` Descendants XDOM)은 이미 안전 상한 보유 → **이 WP 제외**(후순위). CSV streaming + Welford에 집중.
  - **RFC4180 따옴표 내 개행 완전 지원**: `ParseRecords`는 `ReadLine` 단위라 streaming 전환 후에도 **따옴표 안 줄바꿈 미지원 한계 동일 유지**. streaming을 "RFC4180 완전 지원"으로 표기하지 않는다(과대표기 금지 §11.4).
  - R1 계약(**7상태** NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**·RECON_*·`LimitAnalysisResult`·Dashboard=Report 일원화) 변경 0. 인코딩 탐지(CP949/UTF-8) 로직 변경 0.
- **읽을문서**: `docs/38`(RR-08), `docs/39 §WP-02·§WP-03`(CsvReader·XlsxReader 상한 선례), `CLAUDE.md §3·§11.4·§11.6`, `AGENTS.md`.
- **수정예상파일**: `Core/Data/CsvReader.cs`(streaming 신규 경로·상한 상수), `Core/Data/DataProfiler.cs`(`NumericAccumulator` Welford·중복 해시화·streaming 오버로드), `Core/Data/DataProfileResult.cs`(선택: Welford 평균/표준편차 말미 필드 추가, 기존 필드 순서·이름 보존), `tests/RiskManagementAI.SmokeTests/DataProfileTests.cs`·`CsvTests.cs`(회귀+신규), (선택) `Core/Diagnostics/` 벤치 훅. **`Risk/`·`Mapping/`·`Dashboard/`·`Report/` 비변경.**
- **Public Interface**(신규는 옵트인, 기존 보존):
  - 기존 보존: `CsvTable CsvReader.Read(string path, CsvEncoding encoding = CsvEncoding.Auto)`, `DataProfileResult DataProfiler.ProfileTable(CsvTable)`, `DataProfileResult DataProfiler.ProfileCsv(string)`.
  - 신규(예시, Codex 확정): `DataProfileResult DataProfiler.ProfileCsvStreaming(string csvPath, CsvEncoding encoding = CsvEncoding.Auto)` 또는 `CsvReader.ReadStreaming(...)` + streaming 프로파일 오버로드. 상한 상수: `public const int CsvReader.MaxRowCount`, `public const long CsvReader.MaxByteSize`.
  - 선택 확장: `NumericColumnProfile`에 `decimal Mean`·`decimal StdDev`(또는 double) **말미 추가**(기존 6필드 순서·이름 불변).
- **구현세부**:
  - **Welford**: `count`/`mean`/`M2` 누적. `RegisterValue`에서 `mean += (x-mean)/count; M2 += (x-mean_old)*(x-mean_new)`. population variance = `M2/count`. **부동소수 누적 일관성**: 기존 `(double)value` 평균/분산 규약과 동일하게 double로 누적해 OutlierCount 일치. Sum/Min/Max는 decimal 스칼라(누적 순서 입력순 고정 → 결정성). n<4면 OutlierCount=0, std==0이면 0 그대로.
  - **결정성 회귀**: 동일 입력 CSV에 대해 `ProfileTable`(기존, 메모리)와 `ProfileCsvStreaming`(신규)의 `DataProfileResult`(RowCount·NullCounts·DuplicateRowCount·NumericColumns Sum/Min/Max/OutlierCount·BaseDateDistribution·Warnings)가 **완전 일치**함을 SmokeTest로 고정.
  - **상한 예외**: `throw new InvalidDataException($"CSV 행 수가 안전 상한을 초과했습니다. max={MaxRowCount}, actual={...}")` — `XlsxReader.cs:61`/`:96` 규약 동형.
  - **중복 해시**: 행 정규화 문자열을 SHA256(또는 안정 해시)로 환원해 `HashSet`에 해시만 보관(원문 미저장). 충돌 무시 가능(결정적·DuplicateRowCount 동일 보장 위해 알고리즘 고정).
- **보안조건**: 외부 NuGet `PackageReference` 0(인박스 `System.IO`/`System.Diagnostics`/`System.Security.Cryptography`만). 외부 API/Telemetry 0. 자동실행 0. 실데이터 0(seed/샘플 더미명만). 쓰기 경로 `logs/`·`reports/`·`config/` 한정(벤치 훅 포함). 중복 검출 해시화는 **원문 행 미저장**(해시 Audit 원칙 부합). 손상/대용량 파일은 상한으로 Fail-Closed.
- **테스트**(SmokeTest 정본 보존+신규, Unclassified=0):
  - **결정성 회귀**: 동일 입력에 대해 기존 `ProfileTable` 결과 == streaming 결과(전 필드).
  - **상한**: 행/바이트 상한 초과 → `InvalidDataException` + `max=/actual=` 메시지.
  - **Welford 정확성**: 기존 기대 Sum/Min/Max/OutlierCount(3σ·n<4=0·std0=0) 회귀 고정.
  - **중복 해시화**: `DuplicateRowCount` 기존 동일.
  - **메모리 안전**: 대용량 더미 입력에서 전 행 보관 없이 처리(상한 내) — 결정적 단언.
  - 신규 테스트는 **DataProfile/Reconciliation 도메인 키워드**(streaming·welford·MaxRowCount·MaxByteSize·duplicate·BASE_DT 등)로 분류(Unclassified=0). 기존 테스트 삭제·약화 0.
- **완료조건**: 코드베이스에 외부 PackageReference 0 유지. 기존 `CsvReader.Read`/`ProfileTable`/`ProfileCsv` 결과 불변. streaming 경로가 동일 입력에 동일 결과(결정적). 행/바이트 상한 활성. Welford 누산·legacy-compatible OutlierCount·중복 해시화 적용. `dotnet build` 0/0 + SmokeTest `Total=정본+신규 PASS / 0 FAIL`(Local-Gate). Gate A 0. (선택) 벤치 훅 `logs/` 한정.
- **Branch**: `feature/r2-wp-02-streaming-performance` · **Commit**: `feat: add streaming/Welford profiling path with row/byte caps (R2-WP-02)`
- **Claude Review Checklist**:
  - 기존 `CsvReader.Read`/`ProfileTable`/`ProfileCsv` 시그니처·동작·결과 불변(신규 옵트인만) 확인.
  - 외부 NuGet `PackageReference` 0(인박스 System.*만), Vector DB/Embedding/LLM/charting lib 0 → STOP 트리거 없음 확인.
  - Welford 결과가 기존 2-pass 분산·OutlierCount(3σ·n<4=0·std0=0)와 **결정적으로 일치**.
  - 행/바이트 상한 예외형·메시지(`max=/actual=`)가 XlsxReader 규약과 동형.
  - 중복 검출 해시화 = 원문 행 미저장(해시 Audit 원칙) + DuplicateRowCount 불변.
  - RFC4180 따옴표 내 개행 미지원 한계 = 제외범위 명시(과대표기 없음).
  - R1 계약(**7상태**·RECON_*·LimitAnalysisResult·Dashboard=Report) 비변경.
  - SmokeTest 정본 보존 + 신규 회귀, Unclassified=0, 외부 프레임워크 0.
  - 쓰기 경로 logs//reports//config/ 한정(벤치 훅 telemetry 0).
  - 실데이터/실컬럼명 0(더미 seed만).

## R2-WP-03. 전일 대비 분석 (Prior-Day Analytics) — Current/Prev/Δ · TopN movers · 4구획 출력 계약 (Cap C-15, DONE — #84 merged `9bc83a3`, VERIFIED local-gate, Claude review APPROVE)

> 상태: **VERIFIED**(local-gate, #84 `9bc83a3`; `dotnet build` 0/0, SmokeTest `Total=698 PASS=698 FAIL=0`, Unclassified=0, NuGet 0, Gate A 0). `PriorDayAnalyzer`가 `LimitMonitor.Analyze`를 2회 호출해 diff(새 엔진 0), 동일 Join Key 다중 행은 `PRIOR_DAY_DUPLICATE_KEY` Hidden-Risk로 표면화(머지 전 보정 `87f5419`), 정규화 same-day guard·BASE_DT 미스매치 finding 포함. R1 7상태/계약 불변. 실 Test PC Gate B/C=**BLOCKED**. R2-WP-04(Visualization/Report)는 본 WP의 **제외 범위**이며 NEXT UP이다.

- **목표**: 하나의 명확한 목표 — 동일 (PortfolioId, RiskFactor) 단위로 **당일(BASE_DT=N) 대비 전일(N-1)** 한도분석 결과를 결정적으로 결합하여, 행별 Current/Prev/**Δ(증감)**, 상태전이(New/Resolved/Increased/Decreased/Unchanged), **TopN movers**, 그리고 **검토용 초안 4구획 출력 계약(Data-Fact / Methodology / User-Validation / Hidden-Risk)** 을 구조화 record로 산출한다. 새 분석 엔진·새 상태·새 분류 로직은 만들지 않고, 기존 `LimitMonitor.Analyze`를 두 번(N, N-1) 호출하여 그 결과를 차분(diff)한다.
- **선행조건**: R1 완료(VERIFIED — `LimitMonitor`/`LimitAnalysisResult` **7상태**·대사) + **R2-WP-01 머지됨(#79 `59a752f`)**. R2-WP-01이 입력 baseDate 인자를 정규화(`yyyy-MM-dd`→`yyyyMMdd`)하므로 두 일자 인자 포맷이 달라도 각 `Analyze` 호출은 정상 동작하고, 결과 행은 `(PortfolioId,RiskFactor)`로 페어링되어 **포맷 차이만으로 0건이 강제되지 않는다**. 단 R2-WP-01은 데이터행 BASE_DT를 재해석하지 않으므로, 한 일자가 데이터 BASE_DT와 미매칭이면(그 측 0행) `BASE_DT_FORMAT_MISMATCH` Hidden-Risk finding을 추가하되 매칭된 행은 계속 비교한다(임의 보정 금지).
- **작업범위**:
  1. 신규 `PriorDayAnalyzer`(sealed). 입력 = exposure/limit 두 `CsvTable`(또는 경로) + **명시적** currentBaseDate·priorBaseDate 두 문자열. 내부에서 `LimitMonitor.Analyze`를 N·N-1 두 번 호출(분류/조인 로직 재구현 금지).
  2. (PortfolioId, RiskFactor) 키로 Current/Prev 행을 짝지어 행별 Δ(ExposureAmount·LimitAmount·UsageRatio·RemainingLimit) 계산. 짝이 한쪽에만 있으면 New(N만)/Resolved(N-1만)로 분류.
  3. 상태전이 분류: 숫자 mover(Increased/Decreased/Unchanged) vs 상태전이(New/Resolved, 그리고 Normal↔{NoLimit/InvalidLimit/MappingError/**DuplicateLimit**} 같은 비숫자 전이). 비숫자 상태 행(NoLimit/InvalidLimit/MappingError/**DuplicateLimit**, R2-WP-01 #79의 7번째 상태 포함)은 LimitAmount/RemainingLimit=0이므로 **UsageRatio Δ를 숫자 mover로 계산하지 않고** 상태전이로만 분류한다(0除算·오해 Δ 방지).
  4. TopN movers: |UsageRatio Δ| 내림차순(동순위 시 PortfolioId→RiskFactor Ordinal)으로 상위 N개. N은 파라미터(기본값 고정, 결정적).
  5. **4구획 출력 계약**을 구조화 record로 산출(Data-Fact/Methodology/User-Validation/Hidden-Risk). 전부 결정적 plain record/string — LLM 생성 금지. "검토용 초안" 고지 문자열을 Methodology/User-Validation에 명시.
  6. priorBaseDate 선택 규칙을 Methodology(및 신규 Audit metadata)에 기록.
- **제외범위**: 차트/Heatmap/시각화·Excel Report 강화(전부 R2-WP-04). 영업일/달력 계산(priorBaseDate 자동 산출 금지 — 항상 호출자 명시 또는 `DataProfileResult.BaseDateDistribution`의 직전 distinct BASE_DT만 사용, 임의 증감 금지). 중복키 차단·통화/단위·RECON_UNIT(R2-WP-01, 머지됨 #79). Streaming/Welford(R2-WP-02). `LimitMonitor`/`LimitAnalysisResult`/`LimitMonitorRow`/**7상태 enum**(DUPLICATE_LIMIT 포함)의 변경·확장. Dashboard UI 연결(별도). 가중치 학습.
- **읽을 문서**: `CLAUDE.md` §3·§4·§11.4·§11.5 / `docs/38` §1·§3·§5(C-15) / `docs/39` Resume Brief·WP-05~06(공통 결과 계약) / `docs/40`(ADR — 결정성·인박스·NoModel). 코드: `src/RiskManagementAI.Core/Risk/{LimitMonitor.cs, LimitAnalysisResult.cs}`, `tests/RiskManagementAI.SmokeTests/{LimitReconciliationTests.cs, SmokeTestHelpers.cs, SmokeTestContext.cs}`.
- **수정 예상 파일**:
  - 신규 `src/RiskManagementAI.Core/Risk/PriorDayAnalyzer.cs`
  - 신규 `src/RiskManagementAI.Core/Risk/PriorDayAnalysisResult.cs`(아래 record 묶음)
  - (선택) `src/RiskManagementAI.Core/Risk/LimitMonitor.cs` — `BuildJoinKey` 동등 키를 내부 공유 helper로만 노출(`internal static`)할 경우. 시그니처 비파괴 추가만.
  - 수정 `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`(prior-day 회귀 테스트 추가) · (필요 시) `SmokeTestHelpers.cs`(N/N-1 더미 fixture 헬퍼 추가, 기존 헬퍼 변경 없이 추가).
  - **금지**: `LimitAnalysisResult`/`LimitAnalysisMetadata`/`LimitMonitorRow`/`LimitAnalysisKpis`의 positional ctor 시그니처 변경(파괴). 신규 필드 필요 시 별도 record로.
- **Public Interface**:
  - `public sealed class PriorDayAnalyzer { public PriorDayAnalysisResult Analyze(CsvTable exposure, CsvTable limit, string currentBaseDate, string priorBaseDate, int topN = 10); public PriorDayAnalysisResult Analyze(string exposurePath, string limitPath, string currentBaseDate, string priorBaseDate, int topN = 10); }` — 내부에서 `LimitMonitor.Analyze` 2회 호출.
  - `public enum PriorDayMovement { New, Resolved, Increased, Decreased, Unchanged, StateTransition }`
  - `public sealed record PriorDayComparisonRow(string PortfolioId, string RiskFactor, string CurrentBaseDate, string PriorBaseDate, LimitMonitorStatus? CurrentStatus, LimitMonitorStatus? PriorStatus, decimal CurrentUsageRatio, decimal PriorUsageRatio, decimal UsageRatioDelta, decimal CurrentExposureAmount, decimal PriorExposureAmount, decimal ExposureAmountDelta, decimal CurrentLimitAmount, decimal PriorLimitAmount, decimal LimitAmountDelta, decimal CurrentRemainingLimit, decimal PriorRemainingLimit, decimal RemainingLimitDelta, PriorDayMovement Movement)` — Exposure·Limit·UsageRatio·RemainingLimit 전부 Current/Prev/Δ 포함(한도만 변경·노출 불변 케이스 포착). Current/Prev 한쪽만 있으면 해당 측 nullable status, 결측 측 금액=0.
  - `public sealed record PriorDayKpis(int ComparedCount, int NewCount, int ResolvedCount, int IncreasedCount, int DecreasedCount, int UnchangedCount, int StateTransitionCount)` + `static FromRows(IReadOnlyList<PriorDayComparisonRow>)`.
  - `public sealed record PriorDayMovers(IReadOnlyList<PriorDayComparisonRow> TopByUsageRatioDelta)` — |UsageRatioDelta| desc, tie PortfolioId→RiskFactor Ordinal, 상위 topN.
  - **4구획 출력 계약**(구조화 record): `public sealed record PriorDayContract(PriorDayDataFact DataFact, PriorDayMethodology Methodology, PriorDayUserValidation UserValidation, PriorDayHiddenRisk HiddenRisk)`
    - `public sealed record PriorDayDataFact(string CurrentBaseDate, string PriorBaseDate, PriorDayKpis Kpis, IReadOnlyList<PriorDayComparisonRow> ComparisonTable, PriorDayMovers Movers)` — 사실(수치)만.
    - `public sealed record PriorDayMethodology(string PriorBaseDateSelectionRule, string JoinKeyRule, string MoverRankingRule, string DraftNotice)` — 방법·priorBaseDate 선택 규칙·조인키·랭킹 규칙·"검토용 초안" 고지.
    - `public sealed record PriorDayUserValidation(IReadOnlyList<string> ChecklistItems)` — 사용자 확인 항목(예: BASE_DT 포맷 일치, prior-day 데이터 존재 여부).
    - `public sealed record PriorDayHiddenRisk(IReadOnlyList<SafetyFinding> Findings)` — BASE_DT 포맷 불일치, prior-day 행 0건, 비숫자 상태전이, 한쪽 결측 대량 등.
  - `public sealed record PriorDayAnalysisResult(PriorDayContract Contract, LimitAnalysisResult Current, LimitAnalysisResult Prior, bool IsDeterministic)` — 두 입력 일자 분석 결과는 그대로 보존(R1 계약 비파괴).
- **구현 세부**:
  - currentBaseDate/priorBaseDate 모두 비면 `ArgumentException`(또는 빈 비교표 + finding — 결정적 택1, 테스트로 고정). 동일일자 가드는 원문 문자열뿐 아니라 **R2-WP-01 정규화 후 결과(`Current.BaseDate`/`Prior.BaseDate`) 기준**으로도 적용한다. 예: `20260617` vs `2026-06-17`처럼 원문은 달라도 정규화 후 동일하면 자기 자신과 비교하지 않고 deterministic하게 차단/표면화한다.
  - 키 정합: `LimitMonitor.BuildJoinKey`와 **동일 의미**(Trim + `` 구분, OrdinalIgnoreCase 짝짓기). copy-paste 대신 `internal static string LimitMonitor.BuildComparisonKey(...)` 노출을 우선 검토(시그니처 비파괴 추가). SmokeTests 프로젝트에서 internal 접근 필요 시 `InternalsVisibleTo` 기존 설정 재사용 여부 확인(없으면 helper를 public 비노출 대신 Analyzer 내부 동일 로직 복제 + 테스트로 동치 고정).
  - Δ 부호: Current − Prior. New(N만)=Prev 측 0, Resolved(N-1만)=Current 측 0.
  - Movement 분류 우선순위(결정적): (1) 한쪽 결측 → New/Resolved. (2) 양측 존재 & 한쪽 이상 상태가 {NoLimit,InvalidLimit,MappingError,DuplicateLimit} 이거나 상태가 바뀜 → StateTransition(숫자 mover에서 제외). (3) 양측 Normal/Warning/Breach & UsageRatioDelta>0 → Increased, <0 → Decreased, =0 → Unchanged.
  - 모든 출력 순서 완전 정렬(ComparisonTable: PortfolioId→RiskFactor Ordinal; Movers: |Δ| desc → PortfolioId → RiskFactor). HashSet/Dictionary 열거 순서를 출력에 노출 금지.
  - `IsDeterministic=true` 보장(부동소수 미사용, decimal만). DraftNotice 등 텍스트는 리터럴 상수(LLM 호출 0).
- **보안 조건**(Gate A): 외부 NuGet PackageReference 0(System.* 인박스만) · 외부 API/Telemetry/AutoUpdate 0 · SQL/VBA/Golden6 자동실행 0 · 차트/시각화 라이브러리 0(본 WP는 시각화 없음) · Vector/Embedding/LLM/모델파일 0 · 실데이터·실 테이블/컬럼명·내부규정/NCR 원문 미포함(테스트 fixture는 더미명 PF_*/RF_*/BASE_DT만) · 쓰기 경로 `logs/`·`reports/`·`config/` 한정(본 WP는 파일쓰기 없음 권장) · 원문 미저장(필요 시 해시 Audit). LLM/통계 라이브러리 도입 필요해지면 **STOP**(§11.5).
- **테스트**(신규, `LimitReconciliationTests.cs`에 추가 → 도메인 분류 Limit/Reconciliation, Unclassified=0):
  1. "prior-day comparison" — N/N-1 동일 키 양측 존재 시 Current/Prev/Δ 정확, Movement(Increased/Decreased/Unchanged) 정확.
  2. "prior-day New/Resolved movers" — N만/N-1만 키 → New/Resolved 분류, 결측 측 금액=0.
  3. "prior-day TopN movers ordering" — |UsageRatio Δ| 내림차순 + 동순위 PortfolioId→RiskFactor 결정적 순서.
  4. "prior-day state-transition non-numeric mover" — Normal→NoLimit 전이가 숫자 mover가 아니라 StateTransition으로 분류 + Hidden-Risk finding. **양일 DuplicateLimit 동일 상태도 비숫자 StateTransition으로 분류하고 mover 랭킹에서 제외**(R2-WP-01 hardening 신호 은폐 금지).
  5. "prior-day BASE_DT format mismatch hidden risk" — 한 일자가 데이터 BASE_DT와 미매칭(해당 측 0행) → 그 측 New/Resolved + `BASE_DT_FORMAT_MISMATCH` 또는 `PRIOR_DAY_NO_ROWS` Hidden-Risk finding(임의 보정 없음). **두 일자 인자 포맷 차이만으로 전체 비교 0건을 강제하지 않고, 매칭된 행은 계속 비교**한다. 정규화 후 동일일자(`Current.BaseDate == Prior.BaseDate`)는 별도 same-day guard로 차단/표면화.
  6. "prior-day 4-section contract deterministic" — 동일 입력 2회 → DataFact/Methodology/UserValidation/HiddenRisk 동일(서명 비교), DraftNotice("검토용 초안") 존재, `Current`/`Prior` 가 기존 **7상태** `LimitAnalysisResult` 계약 보존(NormalCount·DuplicateLimitCount 등) 확인. **한도만 변경(노출 불변) 시 `LimitAmountDelta != 0`·`ExposureAmountDelta == 0` 회귀**.
  - **기존 SmokeTest Total 보존 + 신규 추가**(삭제·약화 0). Total 증가분은 R1 진행 원장/§5에 기록.
- **완료 조건**: 로컬 `dotnet build` 0 error · `dotnet run --project tests/RiskManagementAI.SmokeTests` → `Total=N PASS / 0 FAIL`(N = 기존+신규, Unclassified=0) · 외부 NuGet 0 유지 · 결정성(동일 입력 동일 출력) · R1 **7상태**/RECON_*/LimitAnalysisResult/Dashboard=Report 계약 비파괴 · Claude 코드리뷰(Diff·보안·문서정합) 승인. 실 Test PC Gate B/C 증거 없으면 PASS 표기 금지(BLOCKED 유지).
- **Codex local-gate 결과(2026-06-30, #84 merged `9bc83a3`)**: `dotnet build RiskManagementAI.sln -c Release` → 0 warnings / 0 errors. `dotnet run --project tests\RiskManagementAI.SmokeTests -c Release` → `Total=698 PASS=698 FAIL=0`, Unclassified=0. 신규 단언 +18: prior-day Current/Prev/Δ, New/Resolved, TopN ordering, StateTransition 비숫자 mover(NoLimit + DuplicateLimit), BASE_DT no-row Hidden-Risk + same-day normalization guard, 4-section deterministic contract + limit-only delta, duplicate-key Hidden-Risk(`PRIOR_DAY_DUPLICATE_KEY`). 외부 NuGet 0 / 실데이터 0 / 차트·LLM·Vector 0. **남은 게이트**: 실 Test PC Gate B/C BLOCKED.
- **Branch**: `feature/r2-wp-03-prior-day-analytics`
- **Commit**: `feat: prior-day analytics (current/prev/delta, TopN movers, 4-section contract) (R2-WP-03)`
- **Claude Review Checklist**:
  - [ ] `LimitMonitor.Analyze` 2회 호출만 — 조인/**7상태** 분류 **재구현 없음**, `LimitMonitorStatus` enum 미변경.
  - [ ] `LimitAnalysisResult`/`LimitAnalysisMetadata`/`LimitMonitorRow`/`LimitAnalysisKpis` positional ctor 비파괴(SmokeTestHelpers.EmptyLimitAnalysis·ExcelReportBuilder 호출부 무영향).
  - [ ] 키 정합: BuildJoinKey와 동일 의미(Trim/``), copy-paste 불일치 없음.
  - [ ] NoLimit/InvalidLimit/MappingError/DuplicateLimit 전이가 숫자 mover에서 제외(0除算·오해 Δ 없음) + Hidden-Risk.
  - [ ] priorBaseDate **자동 산출 없음**(달력·임의 증감 0); 선택 규칙 Methodology/Audit 기록.
  - [ ] 출력 완전 정렬·결정적(decimal만, HashSet/Dict 순서 미노출), DraftNotice "검토용 초안" 명시, LLM 호출 0.
  - [ ] 4구획 record 구조 정확(DataFact/Methodology/UserValidation/HiddenRisk).
  - [ ] 외부 NuGet 0 / 시각화·LLM·Vector 0 / 실데이터·실컬럼명 0 / 파일쓰기 무(또는 허용경로 한정).
  - [ ] SmokeTest Total 보존+신규, Unclassified=0, 기존 테스트 삭제·약화 0.
  - [ ] §5 Traceability C-15 갱신, 상태 어휘 정확(과대표기 0).

## R2-WP-04. Visualization / Report 강화 (인박스 차트·Heatmap·TopN·집중도 + 정확 Exception Count)

> 상태: **NOT_IMPLEMENTED (설계)**. 본 WP는 실 구현 전 설계 문서이며, 빌드/실행 증거 없음(이 Linux 환경엔 .NET SDK 없음). 구현은 Codex 로컬.
> 작업단위 = docs/39 14필드 WP. **하나의 명확한 목표**: 기존 결정적 분석 결과(`LimitAnalysisResult`)를 입력으로, (1) 정확 Exception Count, (2) 인박스 집계 시각화(TopN movers·집중도·Heatmap 등급) 데이터 시트, (3) WPF in-box 차트 화면 렌더를 추가하되 외부 charting NuGet 0 · 실데이터 0 · 자동실행 0 · 결정적을 유지한다.

### 1. 목표 (Goal)
Excel Report와 Risk Dashboard 화면에 **인박스(NuGet 0) 시각화**를 더하고 **Exception Count를 정확화**한다. 구체:
- `ExcelReportBuilder`의 부정확한 `=COUNTA(EXCEPTION_LIST!A:A)` 예외 카운트를, 실제 예외 건수(Analysis ExceptionList + High/Blocker Validation)를 단일 진실원(SoT)에서 Number로 직접 기록하도록 교체한다.
- 신규 집계 시각화 **데이터 시트**(TopN movers·집중도(상위N 비중/HHI)·Heatmap 등급)를 `LimitAnalysisResult`만으로 결정적 산출한다.
- Risk Dashboard 탭에 **WPF in-box 차트**(System.Windows.Shapes / DrawingVisual / Canvas + SolidColorBrush)로 상태분포 막대·집중도 막대·Heatmap 셀을 렌더한다(화면 표시 한정).
- 시각화는 **집계 데이터 계약**(TopN 정렬·집중도 비율·정확 카운트·Heatmap 등급)으로 결정적 테스트화한다. 차트 픽셀은 테스트하지 않는다.

### 2. 선행조건 (Preconditions)
- R1 `LimitAnalysisResult`(**7상태** NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**, R2-WP-01 #79)·`LimitAnalysisKpis`(`DuplicateLimitCount` 포함)·`ExceptionList`·`ReconciliationSummary` 계약 그대로 사용(변경 금지).
- `ExcelReportBuilder`(in-box ZipFile + templates/report XML 치환, OpenXML SDK/Interop 미도입) 경로 유지.
- SmokeTest 정본 Total 보존(현 기준 698; R2-WP-03 #84 머지 후, `9bc83a3` 직접 local-gate 재확인). 도메인 분류기 Unclassified=0 유지.
- 쓰기 경로 `reports/` 한정(ResolveReportsDirectory 강제). 별도 이미지 파일 산출 금지.

### 3. 작업범위 (Scope)
1. **정확 Exception Count (SoT 분리)**: `BuildExceptionRows`가 emit하는 행 카운트(헤더·NO_EXCEPTION placeholder 제외)를 산출하는 헬퍼 `CountExceptions(analysis, validationFindings)`를 분리한다. SUMMARY 시트의 `ExceptionCount`는 이 값을 **Number**로 기록한다. 부정확한 `=COUNTA(EXCEPTION_LIST!A:A)` 수식은 제거하거나, 참조용으로 남기더라도 SUMMARY의 권위 카운트는 Number로 한다.
2. **집계 시각화 데이터 시트(신규)**: `ExpectedSheetNames`에 `RISK_VISUAL`(또는 `TOPN`/`CONCENTRATION`/`HEATMAP`로 분리 가능, 최소 1개) 추가. 내용:
   - 상태분포(**7상태** 카운트·비율, DUPLICATE_LIMIT 포함),
   - TopN movers/노출 상위(예: ExposureAmount 또는 UsageRatio 내림차순 상위 N — 결정적 tie-break: PortfolioId Ordinal),
   - 집중도: 상위N 비중(상위N `Abs(ExposureAmount)` 합 / 전체 `Abs(ExposureAmount)` 합), HHI(=Σ(share^2)) — short/negative exposure를 signed 합산하지 않는다. 분모 0이면 graceful(빈 시각화·Finding) 처리. 통화 혼합 시 의미 왜곡 주의(아래 보안조건),
   - Heatmap 등급: UsageRatio를 결정적 임계(예: <0.8 LOW / 0.8~1.0 MID / >1.0 HIGH)로 등급화한 텍스트/숫자.
   - 시각화는 **inlineStr/Number 셀**과 옵션으로 **ASCII 막대(반복문자 길이=비율)** 텍스트로 표현(차트 part 없이도 100% 성립).
3. **WPF in-box 차트(App, 화면 한정)**: `MainWindow.OnRunLimitMonitor` 결과를 받아 `RenderRiskCharts(LimitAnalysisResult)`(신규 메서드/컨트롤)가 Canvas+Shapes로 상태 막대·집중도 막대·Heatmap 셀을 그린다. 색상은 SolidColorBrush. DataGrid/TextBlock 기존 표시는 보존.
4. **회귀 SmokeTest 추가**: 집계 계약(정확 카운트·TopN 정렬·집중도 비율·Heatmap 등급·신규 시트 배선)을 결정적으로 단언. 기존 ReportTests/PackagingTests의 시트 개수(10)·`sheet10.xml` 의존 단언을 신규 개수에 맞춰 **갱신**(약화/삭제 아님, 동반 갱신).

### 4. 제외범위 (Out of Scope)
- 외부 charting NuGet(OxyPlot/LiveCharts/ScottPlot 등) 도입 — **금지(STOP)**. Vector DB/Embedding/Local LLM/모델파일 — 본 WP 무관·금지.
- 전일 대비(Current/Prev/Δ) 산출 — R2-WP-03 소관(중복 정의 금지, 출력 계약만 정합).
- OOXML chartXML(c:barChart) + xl/drawings part 직접 생성 — **본 WP 기본 범위 아님**. 채택 시 별도 결정(ADR) 필요하며 상태는 SCAFFOLD_ONLY/PARTIAL로 보수 표기. 최소범위 = (A) WPF Shapes + (b2) 데이터 시각화 시트로 STOP 없이 달성.
- 차트 이미지 파일을 reports/ 외부 또는 별도 파일로 떨구기 — 금지(시각화는 xlsx 내부 또는 WPF 화면 내 한정).
- LimitMonitor **7상태**/RECON_* 로직·`LimitAnalysisResult` 계약 변경.

### 5. 읽을 문서 (Docs to Read)
- `CLAUDE.md`(§3 절대원칙·§6 Excel 함수 제한·§11.4 상태어휘·§11.5 STOP), `AGENTS.md`
- `docs/38`(Roadmap §5 Traceability), `docs/39`(WP 형식·Resume Brief), `docs/40`(ADR — DM-03 in-box xlsx)
- `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs`, `src/RiskManagementAI.Core/Risk/LimitAnalysisResult.cs`, `LimitMonitor.cs`
- `src/RiskManagementAI.Core/Dashboard/DashboardSnapshotBuilder.cs`, `src/RiskManagementAI.App/MainWindow.xaml(.cs)`
- `tests/RiskManagementAI.SmokeTests/{ReportTests.cs, PackagingTests.cs, SmokeTestContext.cs}`

### 6. 수정 예상 파일 (Files Likely Touched)
- `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs` — `ExpectedSheetNames` 신규 시트, `BuildWorkbook`(정확 카운트 Number 기록), 신규 `CountExceptions`/`BuildRiskVisualRows`, SUMMARY 카운트 교체.
- (신규) `src/RiskManagementAI.Core/Report/RiskVisualAggregator.cs`(또는 동등) — TopN·집중도·Heatmap 등급 결정적 산출(SoT). Report와 WPF가 공유.
- `src/RiskManagementAI.App/MainWindow.xaml(.cs)` — `RenderRiskCharts` + Canvas/Shapes 컨트롤(Risk Dashboard 탭).
- `templates/report/*.tpl` — 변경 불필요(인덱스 자동 배선). 신규 part 도입 안 함.
- `tests/RiskManagementAI.SmokeTests/{ReportTests.cs, PackagingTests.cs}` — 시트 개수/sheetN 의존 갱신 + 신규 단언. (필요 시 신규 suite는 추가하지 않고 ReportTests에 흡수해 Report 도메인 분류 유지.)
- `docs/38`(§5 Traceability·RR-08/RR-15 인접 표기), `docs/39`(본 WP), `prompts/codex/R2-WP-04_*.md`.

### 7. Public Interface
- `ExcelReportBuilder.BuildReport(ExcelReportRequest) -> ExcelReportResult` — **시그니처 불변**(요청/결과 레코드 그대로). 호출부(MainWindow.OnGenerateExcelReport) 변경 불필요.
- (신규, Core) `RiskVisualAggregator.Aggregate(LimitAnalysisResult, int topN) -> RiskVisualModel` (결정적). `RiskVisualModel`: 상태분포·TopN movers·집중도(상위N 비중·HHI)·Heatmap 등급. decimal 비율, Ordinal tie-break.
- (신규, App) `void RenderRiskCharts(LimitAnalysisResult result)` — 화면 렌더만, 반환 없음, 파일 산출 없음.
- `ExcelReportBuilder.ExpectedSheetNames` — Count가 10→N(신규 시트 수만큼) 증가. 의존 테스트 동반 갱신.

### 8. 구현세부 (Implementation Detail)
- **카운트 SoT**: `BuildExceptionRows`와 `CountExceptions`가 동일 입력(analysis.ExceptionList + validationFindings의 Blocker/High)을 쓰되, NO_EXCEPTION placeholder·헤더는 카운트에서 제외. SUMMARY의 `ExceptionCount = Number(CountExceptions(...))`. (예외 0건이면 0.)
- **집중도/TopN**: 집중도/HHI share는 `Abs(ExposureAmount)` 기준으로 계산한다(`LimitMonitor`의 사용률 ABS 규칙과 정합). signed exposure 합산 금지. 전체 절대노출 분모가 0이면 빈 집중도/HHI + Finding으로 graceful 처리한다. 통화 혼합(`LimitMonitorRow.CurrencyCode` 존재) 시 단순 합산은 의미 왜곡 → 동일 통화 그룹별 집계 또는 통화 표기를 명시하고, 혼합 시 `MIXED_CURRENCY` 주석/Finding을 시각화 시트에 남긴다(R2-WP-01 통화/단위 매핑과 정합, 본 WP는 합산 전제를 명시만). decimal 반올림 자리수 고정(예: 비율 4자리).
- **결정적 정렬**: 모든 TopN/등급 산출은 `(키 내림차순, PortfolioId Ordinal 오름차순)` tie-break로 비결정성 제거.
- **Excel 2021 게이트**: 신규 셀에 수식 추가 시 `Excel2021FunctionChecker.CheckFormula`를 통과해야 함(EXCEL_365_FUNCTION 금지 — VSTACK/TEXTSPLIT/GROUPBY 등 §6 금지군 사용 불가). 가급적 시각화는 Number/inlineStr 정적 값으로 기록(수식 최소화)해 게이트 위험 0.
- **신규 시트 배선**: `ExpectedSheetNames`에 항목 추가만으로 `WriteWorkbookPackage`가 Content_Types/workbook/rels/app.xml override·sheet{n}.xml를 자동 생성(코드 확인됨). styles rId(`rId{Count+1}`) 자동 시프트. **단 OOXML 무결성은 로컬 Excel 열기로 검증**(BLOCKED 증거는 Test PC).
- **WPF 차트**: System.Windows.Shapes(Rectangle/Line/Polyline)·Canvas·DrawingVisual·SolidColorBrush만. 외부 의존 0. 화면 표시일 뿐 '리포트 차트'로 표기 금지(과대표기 금지).
- **출력 계약 정합(R2-WP-03)**: 시각화 산출은 Data-Fact / Methodology / User-Validation / Hidden-Risk 4구획 정신을 따르되, 본 WP는 결정적 집계·검토용 초안 명시.

### 9. 보안조건 (Security Constraints)
- 외부 NuGet PackageReference = 0(인박스 System.* 만). 외부 charting NuGet 도입 시 **STOP**. 외부 API/Telemetry/AutoUpdate 0.
- 실데이터/실 테이블·컬럼명/내부규정·NCR 원문/모델파일 repo 미포함. seed/샘플은 일반 더미명만.
- 쓰기 경로 `reports/`(리포트)·`logs/`(audit) 한정. 차트/이미지 별도 파일 금지.
- Audit는 해시(원문 미저장) 유지 — SQL/UserId 평문 미기록(기존 TryAppendAuditLog 패턴 보존).
- 자동실행 0(SQL/VBA/Golden6). 차트 렌더는 결과 표시만, 외부 실행 없음.
- 통화 혼합 합산 왜곡을 시각화 시트에 명시(MIXED_CURRENCY)해 오해석 방지.

### 10. 테스트 (Tests — SmokeTest, 외부 프레임워크 0, Unclassified=0)
신규 단언은 **Report/Limit/DataProfile 도메인 키워드**로 명명해 분류기 Unclassified=0 유지(예: "ExcelReport ... ", "report ...", "EXCEPTION_LIST ...", "LIMIT_MONITORING ...", "RECON_ ...", "BASE_DT ...", "concentration ... limit ...", "TopN limit ...").
- 정확 카운트: 예외 N건 입력 시 SUMMARY `ExceptionCount` Number == N(헤더/NO_EXCEPTION 제외), 0건 시 0. (기존 `=COUNTA` 의존 단언은 갱신.)
- 신규 시트 배선: `ExpectedSheetNames`에 신규 시트 포함, ZIP에 `xl/worksheets/sheet{N}.xml` 존재, `SheetNames.SequenceEqual(ExpectedSheetNames)` 유지. PackagingTests의 시트 인벤토리/개수 동반 갱신.
- TopN/집중도 결정성: 동일 입력 2회 → 동일 순서·동일 비율(decimal). tie-break(PortfolioId Ordinal) 단언. 집중도/HHI는 `Abs(ExposureAmount)` 분모·share를 사용하고, raw signed 합산 0/음수 케이스를 graceful하게 처리.
- Heatmap 등급: 임계 경계값(0.8/1.0)에서 등급 결정적.
- 통화 혼합: MIXED_CURRENCY 주석/Finding 노출 단언.
- 회귀 보존: ReportTests의 **7상태**(DUPLICATE_LIMIT 포함)·RECON_*·ReconciliationPassed PASS/FAIL·NO_LIMIT_ROW·NuGet 0 단언 전부 유지.
- 완료 시 `Total=N PASS=N FAIL=0`, 모든 신규 단언 분류됨(Unclassified=0).

### 11. 완료조건 (Definition of Done)
- 로컬 `dotnet build RiskManagementAI.sln -c Release` 0 error / 0 warning, `dotnet run --project tests/RiskManagementAI.SmokeTests` → `Total=N PASS=N FAIL=0`(기존 정본 698 + R2-WP-04 신규, 도메인 Unclassified=0).
- `git grep PackageReference` → 0(Core/App). 외부 charting 의존 0.
- 생성 xlsx가 로컬 Excel 2021에서 손상 경고 없이 열림(**BLOCKED 증거 — Test PC**). SDK 없는 Linux에서는 코드 정합·게이트만 사전검증.
- 정확 Exception Count·신규 시각화 시트·WPF 차트 렌더 동작. 기존 R1 계약(**7상태**·RECON_*·Dashboard=Report 일원화) 회귀 0.
- Claude 4축 리뷰(Diff·보안 Gate A·테스트 보존+회귀·문서 정합) APPROVE.
- 상태 표기: 데이터 시각화 시트 + 정확 카운트 = (로컬 게이트 후) VERIFIED 가능. WPF Shapes 화면 = VERIFIED(화면 한정 명시). OOXML chartXML part(미채택) = N/A. **과대표기 금지.**

### 12. Branch
`feature/r2-wp-04-visualization-report`

### 13. Commit 규약
- Subject(영문, 명령형, 72자 이내) + `(#PR)` 머지 시. 예: `Add in-box risk visualization sheet and exact exception count (#NN)`.
- 1 WP = 1 PR(squash 머지). force push·hard reset·main 직접 push 금지. 작업은 feature 브랜치에서만.
- 커밋 본문에 NuGet 0·실데이터 0·외부 charting 미도입·SmokeTest Total 변화(기존 정본→N) 명시.

### 14. Claude Review Checklist
- [ ] **외부 NuGet 0**: Core/App csproj에 PackageReference 0(charting 포함). STOP 트리거(charting/Vector/LLM) 부재.
- [ ] **계약 보존**: `LimitAnalysisResult` **7상태**(DUPLICATE_LIMIT 포함)·KPI·ExceptionList·RECON_* 불변. `BuildReport` 시그니처 불변(호출부 무변경).
- [ ] **정확 카운트 SoT**: SUMMARY ExceptionCount = `CountExceptions(...)` Number, 헤더/NO_EXCEPTION 제외, EXCEPTION_LIST 실 행수와 일치.
- [ ] **결정적**: TopN/집중도/Heatmap 동일 입력 동일 출력, Ordinal tie-break, decimal 자리수 고정.
- [ ] **Excel 2021 게이트**: 신규 수식(있다면) EXCEL_365_FUNCTION 미검출. §6 금지 함수 미사용.
- [ ] **쓰기 경로**: reports/·logs/ 한정, 차트 이미지 별도 파일 0. 통화 혼합 MIXED_CURRENCY 명시.
- [ ] **테스트 보존+신규**: 기존 ReportTests/PackagingTests 약화·삭제 0, 시트개수/sheetN 동반 갱신, 신규 단언 Report/Limit/DataProfile 도메인 분류(Unclassified=0), Total 보존+증가.
- [ ] **Audit**: 해시 유지, SQL/UserId 평문 미기록.
- [ ] **과대표기 금지**: WPF Shapes = 화면 한정 표기, OOXML chart part 미채택, 상태어휘 정확. 로컬 게이트 증거 없으면 PASS 금지(BLOCKED).
- [ ] **문서 정합**: docs/38 §5 Traceability(C-16 신규 행) 상태 갱신, docs/39 본 WP·Resume Brief 정합.

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
