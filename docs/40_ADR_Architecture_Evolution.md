# 40. ADR — Architecture Evolution (post-v0.4.0)

> `docs/11_ADR_Initial_Architecture.md`(ADR-001, C#/.NET/WPF self-contained)에 이은 결정 기록.
> 모든 ADR은 절대 원칙(Offline·NuGet 0·NoModel·해시감사)을 전제로 한다.

---

## ADR-002. 공통 Analysis Result Model + 통합 Data Reader

- **상태**: 채택 (R1)
- **맥락**: 현재 `LimitMonitor`(실 조인)·`DataProfiler`·`ExcelReportBuilder`·`DashboardSnapshotBuilder`가 **각자 입력/출력 타입**을 쓰고, UI가 노출 1.1배 **합성 한도**(`BuildUiLimitRows`)로 Report를 채워 Dashboard(실값)와 **수치가 분기**된다. CSV 파서도 3곳 중복.
- **결정**:
  1. **공통 `LimitAnalysisResult`**(KPI·LimitMonitoringTable·ExceptionList·메타) 1개를 **Dashboard·Excel Report·History·Audit·향후 AI Commentary 입력**이 공유한다(WP-05/07).
  2. 합성 한도 산식 제거, **실 Exposure-Limit Join만** 사용(WP-01).
  3. CSV/XLSX 파싱은 **단일 `CsvReader`/`XlsxReader` → 공통 `CsvTable`**로 수렴(3중 중복 제거, WP-02/03/08).
  4. 컬럼 접근은 **`ColumnMapping`**(설정·승인형)을 통한다(WP-04).
- **결과**: 단일 분석 진실원장 → 화면·리포트·감사 일관성. 결정적(동일 입력=동일 수치).
- **대안/기각**: 화면별 독립 계산(현행) → 분기·중복 → 기각.

## ADR-003. Local LLM Process Boundary (설계 전용 · 런타임 STOP)

- **상태**: 채택(설계만), 런타임 **MODEL_APPROVAL_REQUIRED** (R4)
- **맥락**: Local LLM은 가치가 크나 추론 라이브러리/모델파일/런타임은 절대원칙(오프라인·NuGet 0·모델파일 미포함) 및 보안·반입에 직접 영향. **승인 전 의존성 추가 금지.**
- **결정 (이번 단계에 구현 가능한 범위)**:
  - 인터페이스/계약만: `ILocalModelProvider`, `NoModelProvider`(기본·유지), `ModelProviderFactory`, `ModelAvailability`, `ModelHealthCheck`, `ModelRequest`/`ModelResponse`, Timeout/Cancellation, 입력·출력 길이 제한, **Output Safety Check**(생성물 → MVP-1 Safety Checker), Model Audit Metadata(해시), **NoModel Fallback 유지**.
  - **Model Pack Manifest**(모델/런타임 메타·해시), **Integrity Hash** 검증 설계.
  - **Process Boundary 결정**: **Out-of-process(별도 프로세스 + 로컬 IPC)** 를 기본 방향으로 한다 — 크래시 격리·메모리 한도·네트워크 격리·런타임 무결성 관점. (최종 IPC 방식은 런타임 승인 시 확정.)
- **미결(런타임 승인 시 확정)**: In-process vs Out-of-process 최종 채택, IPC(named pipe/stdio/local socket), Crash Recovery, Memory Limit, Network Isolation 강제, Runtime Integrity 검증 절차, Model License, Model Pack 배포·반입, **App Release와 Model Pack 분리 배포**.
- **STOP 규칙**: 실제 추론 Runtime/라이브러리/모델파일 추가가 필요한 순간 **작업 STOP** → 구성요소·라이선스·보안·크기·성능·반입방법 문서화 → **승인 전 Dependency 추가 금지**(docs/41 Model Approval Gate).
- **대안/기각**: In-process 직접 로딩 → 크래시·메모리·무결성·격리 취약 → 기본에서 기각(승인 시 재검토).

## ADR-004. 입력 인코딩·형식 전략 (CP949/UTF-8/XLSX)

- **상태**: 채택 (R1)
- **맥락**: 현 리더 UTF-8 전용. 국내 금융 CSV(Golden6 export)는 **CP949** 다수, **XLSX** 입력 수요.
- **결정**:
  1. `CsvReader`가 **UTF-8**(BOM/무BOM) 지원 + Auto 감지(결정적). **CP949는 net8.0에서 인박스 아님** — `Encoding.GetEncoding(949)`는 `CodePagesEncodingProvider`(=`System.Text.Encoding.CodePages` NuGet 패키지)를 필요로 한다. **결정(2026-06-20): 경로 A 채택** — **repo 내장 자체 디코더 + 공개 표준 Windows-949(UHC/CP949) *전체* 매핑표 리소스**(NuGet 0 유지, 리소스 Hash 검증). 매핑표는 **EUC-KR/KS X 1001 부분집합이 아니라 전체 Windows-949/UHC**여야 한다(Golden6 export가 UHC 다수 — 확장 한글 누락 시 오디코드). (기각) 경로 B `System.Text.Encoding.CodePages` 승인 = NuGet-0 불변식 깨짐.
  2. `XlsxReader`는 **인박스 `System.IO.Compression`+OOXML**(NuGet/OpenXML SDK/Interop 0, DM-03/DU-08 정합). zip 안전 상한.
  3. 입력 형식·인코딩 판별 결과는 finding/메타로 **감사 가능**하게 노출.
- **대안/기각**: 외부 인코딩/엑셀 라이브러리(ExcelDataReader 등) → NuGet 도입 → 기각(원칙 위반, 승인 대상).

## ADR-005. .NET 8 (LTS) 유지 vs .NET 10 전환

- **상태**: 채택 — **v1.0까지 .NET 8 LTS 유지**, R4/Pilot 이후 재검토.
- **맥락**: self-contained win-x64는 런타임을 동봉하므로 **보안 패치 책임이 우리에게** 있다. .NET 8 = LTS(지원 ~2026-11). .NET 10 = LTS(2025-11 GA, 더 긴 지원). 전환은 빌드/패키지/테스트/백신 재검증 비용 + Excel/WPF 호환 재확인을 요구.
- **결정**: 안정 기준선(v0.6.0)을 흔들지 않기 위해 **STAB~R2 구간은 .NET 8 유지**. 단 (1) `global.json`로 SDK를 고정해 재현성 확보(ADR-006), (2) .NET 8 지원종료(2026-11) 전 **v1.0 Pilot 전후로 .NET 10 LTS 전환 ADR 재평가**(지원기간·self-contained 패치·win-x64 크기·Excel/WPF 회귀). 전환 자체는 별도 WP+게이트.
- **대안/기각**: 즉시 .NET 10 전환 → 기준선 리스크·재검증 비용 과다 → 현 단계 기각(예약).

## ADR-006. Build/Version Reproducibility (VERSION 단일 원천)

- **상태**: 채택 (STAB-WP-01)
- **맥락**: `build/01~03` 기본 `-Version="0.2.0"` ≠ `VERSION`(0.6.0) → 무인자 시 오버전 산출물(RR-11). 버전 원천 분산.
- **결정**: **`VERSION` 파일이 유일한 버전 원천**. 빌드 스크립트는 VERSION을 읽고, `-Version` 명시 시 **불일치하면 실패(exit 1)**. Release Note/ZIP/SHA/DependencyList 동일 Version. **`global.json`로 .NET SDK 고정**(재현성). Release Note에 **Build Commit SHA·정본 Test 총수·SDK·Runtime·Build Date** 기록. `LangVersion`/`TreatWarningsAsErrors`는 재현성·경고억제 관점에서 STAB에서 명시.
- **대안/기각**: 스크립트 인자 기본값 유지 → 휴먼에러 지속 → 기각.

## ADR-007. Knowledge Pack Contract (App ↔ Pack 분리, keyword-only)

- **상태**: 채택(계약 설계) — 원문 적재 **APPROVAL_REQUIRED** (KB-WP-01/02)
- **맥락**: 현 RAG는 **Catalog/Metadata 검색**까지(원문 Clause/Chunk 미구현). 공개 규정 원문·내부규정 원문을 Application Source Repository에 넣을 수 없다(보안·라이선스·크기).
- **결정**:
  1. **3계층 분리**: ① Application Release(실행파일·검색엔진·접근통제·인용검증·**Empty/Public Catalog**) ② **Public Knowledge Pack**(공개 규정 원문·Clause/Chunk·Metadata·Source Locator·Version·Effective/Superseded·File Hash·License·Pack Manifest/Version/Approval) ③ **Internal Knowledge Pack**(회사 환경에서만 생성·repo 미포함·문서오너 승인·ACL·보안등급·조회로그·Pack Hash·폐기/교체 이력).
  2. **원문은 repo 미포함** — 별도 **Offline Ingestion Package/승인 Data Pack**으로만. PDF/HWP 원문 직접 커밋 금지.
  3. **Deterministic Chunk ID** + Source Text Hash + 검색결과↔원문위치 연결 + Metadata-only 결과 vs 실 Clause 결과 구분 + 적용기준일별 유효문서 선택 + 인용 검증.
  4. **Vector DB/Embedding 미도입 — Keyword/Inverted Index로 먼저 완성**(STOP 규칙). 필요 시 승인.
- **대안/기각**: 원문을 repo에 직접 포함 + 외부 vector DB → 보안·원칙 위반 → 기각.

## ADR-008. Release Integrity Manifest + Fail-Closed (운영)

- **상태**: 채택 (STAB-WP-03). build측 03a **VERIFIED**(local-gate, #59). runtime 03b **VERIFIED**(local-gate, #61 merged 2026-06-28; `Total=572 PASS/0 FAIL`). 실 오프라인 Test PC Gate B/C는 별도 **BLOCKED**(`docs/45`). 독립 신뢰 앵커(코드 서명)는 **STAB-WP-05 APPROVAL_REQUIRED 후속**.
- **맥락**: ZIP SHA만으로는 운영 중 **핵심 파일 변조**(security_policy/rules/template/mapping/KB/NCR placeholder)를 못 잡는다(RR-14). Release에 PDB/개인경로/Debug 자산 유입 위험(RR-13).
- **결정**:
  1. **`approved_manifest.json`**(path·size·SHA256·version·required/optional·security class) 생성 + ZIP 동봉. 필수 대상은 **apphost(`*.exe`) + 관리 앱 어셈블리(`RiskManagementAI.dll`, `PublishSingleFile=false`이므로 시작/검증 코드 실체) + `*.Core.dll` + policy/rules/mapping/KB/NCR placeholder/templates**. CP949 매핑표는 Core DLL 임베디드 리소스이므로 loose 파일로 넣지 않고 **Core DLL 해시로 커버**(런타임은 `Cp949Decoder` 임베디드 해시).
  2. **앱 시작 시 핵심 파일 Hash 검증**: **운영=Fail-Closed** — policy 불일치→기동/기능 차단, rules→검사 차단, template→Report 차단, KB→검색 차단. **manifest 부재/판독실패도 운영에서는 Fail-Closed**(부재로 우회 불가). 개발 Fallback은 **패키지 릴리스에 없는 명시적 dev 전용 스위치/환경**으로만. **manifest 자체는 독립 신뢰 앵커로 검증**(기대 해시를 서명된 관리 어셈블리에 임베드 또는 공개키 서명) — 같은 폴더의 manifest만으로는 폴더 동시 변조에 무력(post-release 변조 미탐지) → 불충분.
  3. Release 보안: `DebugSymbols=false`/`DebugType=none`(PDB 제거), 개인경로/SourceLink 0, Unsafe BinaryFormatter 명시 false, Dev/Test config 미포함, **Production assets allowlist**. **Code Signing은 운영 절차 Placeholder**(자동서명 미구현), Rollback 절차·Release Approval 기록.
  4. **STAB-WP-03b runtime gate (Design 3 interim)**: `RiskManagementAI.Core.Integrity.IntegrityVerifier.VerifyPackage(baseDir, strict)`가 build/03 §4를 in-process로 그대로 포팅(SHA256 전용, NuGet 0). 앱 시작 시 `App.OnStartup` 최상단에서 `PolicyLoader`/`base.OnStartup` 이전에 실행 → `IntegrityGate.Decide`가 **FailClosed=차단(Shutdown 2)**. 데이터/자산 변조(policy·mapping·rules·template·KB·NCR)와 **manifest 부재/판독실패/축소/버전불일치**를 운영 fail-closed로 차단. version은 **Core 빌드 상수(`ExpectedVersion`)** 와 비교(Assembly 버전 아님). dev Fallback은 **`RMAI_DEV_ALLOW_UNVERIFIED=1` + `Debugger.IsAttached`** 동시 충족 시에만(릴리스 패키지에 부재) — 운영 PC에 환경변수가 새도 디버거 없으면 우회 불가.
  5. **잔여 위험(Design 3 interim의 한계 — 명시적 미탐지)**: ① **manifest 독립 신뢰 앵커 부재** — 쓰기 가능 폴더에서 파일과 `approved_manifest.json`을 lock-step으로 동시 변조하면 통과(co-tamper). ② **self-contained 런타임 DLL(~150개: coreclr/hostfxr/System.Security.Cryptography 등)은 manifest 미해시** → 관리 게이트 우회 가능. ③ 폴더 동반 변조(추가 파일). 이 3건은 **Authenticode 코드 서명(독립 신뢰 루트)** 으로만 닫히며 **STAB-WP-05 APPROVAL_REQUIRED 후속 WP**로 이관(자동 서명·인증서 = 외부 신뢰 루트, 본 WP 범위 외). SmokeTest는 co-tamper가 **탐지되지 않음**을 양성으로 고정(과대표기 금지). — **갱신(#61)**: 당초 잔여였던 **비-mandatory critical 자산 co-deletion**(manifest 엔트리+파일 동시 삭제)은 `IntegrityVerifier.RequiredCriticalEntries`(현 build/01 critical 인벤토리 핀 + lock-step 테스트)로 **해소**. 따라서 STAB-WP-05 잔여는 **콘텐츠 co-tamper + 런타임 DLL 미해시(+폴더 동반 변조)** 로 좁혀짐.
- **대안/기각**: ① ZIP SHA만 유지 → 부분 변조·Debug 유출 미탐지 → 기각. ② manifest 기대해시를 Core DLL 상수로 임베드(앵커 시도) → **해시 고정점 부재**(상수 변경→Core.dll 변경→그 manifest 엔트리 변경→manifest 바이트 변경→상수 불일치)로 **정상 clean 패키지를 brick** + blind 검증 불가 → 기각, 코드 서명으로 대체.

## ADR-009. Model Approval Package (LLM Runtime 승인 요건 · STOP 게이트)

- **상태**: 채택(요건 정의) — Runtime 도입 **MODEL_APPROVAL_REQUIRED** (ADR-003 보강, LLM-WP-01)
- **맥락**: ADR-003가 Adapter 계약·Out-of-process 방향을 정함. 실 Runtime/Model 도입 직전 STOP 시 **무엇을 승인받아야 하는가**를 사전 정의.
- **결정 — 승인 문서 필수 항목**: 후보 Runtime · 후보 Model · License · 배포 크기 · RAM/CPU/GPU · 응답시간 · SQL/VBA 한국어 성능 · 규정답변 성능 · 환각률 · **인용 준수율** · 보안성 · 반입 방식 · Model Pack 업데이트 방식 · App↔Model Pack 분리 배포 · Runtime/Model Integrity Hash. 승인 전 Dependency 추가 0. (게이트: `docs/41 §3`.)
- **대안/기각**: 승인 없이 PoC로 런타임 선반입 → 원칙·보안 위반 → 기각.

## ADR-010. Smart Assist — WPF-native Inline Completion (정적·NoModel, 외부 Editor 금지)

- **상태**: 채택(설계) — UX Assist Track (UX-WP-01~03). LLM 기반 추천은 **R4 이후 APPROVAL_REQUIRED**.
- **맥락**: SQL/VBA/Excel/리스크 코멘트 작성 중 inline 자동완성·snippet·안전 힌트 수요. 단 전체 생성(`DraftPipeline`)과 **별개**여야 하고, 절대 원칙(Offline·NuGet 0·NoModel·자동실행 0)을 깨면 안 된다. 일반적 해법인 외부 에디터/완성 패키지(AvalonEdit·ScintillaNET·RoslynPad)는 **NuGet 도입 → 불변식 위반**.
- **결정**:
  1. **UI = WPF 기본 `TextBox` + `Popup`/`ListBox`** 로 완성 UI를 자체 구현(외부 Editor/Completion 패키지 0). Ctrl+Space 트리거, Enter/Tab 삽입, Esc 닫기, **자동 삽입 없음**.
  2. **추천 = 정적·결정적 provider**(`ICompletionProvider`) + `CompletionEngine`(`RiskManagementAI.Core.Assist`). **NoModelMode 완전 동작**(모델 의존 0). 모델 기반 랭킹/생성은 **R4 Model Approval Gate 이후로 연기**(ADR-003/009, STOP).
  3. **Safety/룰 재사용** — SQL/VBA/Excel 차단 판단은 **기존 `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`+RuleSet** 경유(룰 중복 정의 금지). Excel 허용 완성 함수는 전용 RuleLoader 소스(`rules/excel_2021_completion_allow_functions.txt` 또는 동등 RuleSet 그룹)에서만 읽고, `ExcelPreferredFunctions`의 비함수 안내 라벨을 함수 allow-list로 쓰지 않는다. 입력 시 위험은 구조화 `SafetyFinding`을 보존한 `SafetyHint`/`BlockedHint` + `CompletionResult.Findings` + 기존 결과 패널 연계.
  4. **Audit = 해시 전용** — accept 시 `SuggestionId·ProviderId·Language·Kind·Mode·UserHash·InsertTextHash·AcceptedAtUtc`만(입력 원문/삽입 본문 미저장).
  5. **seed/snippet에 실데이터·실 테이블명·내부규정 원문 0**(일반 표현만), 모든 추천 `RequiresReview`(검토용 초안). SQL/VBA **자동 실행 0**(텍스트 제안만).
  6. **안전 힌트 우선순위** — 결과 cap은 일반 삽입 가능 추천에 적용하고, `SafetyHint`/`BlockedHint` 및 `CompletionResult.Findings`는 top-N 절단으로 누락시키지 않는다.
- **대안/기각**: ① 외부 Editor/Completion NuGet(AvalonEdit 등) → NuGet 0 불변식 위반 → 기각. ② 지금 LLM 기반 완성 → STOP·미승인 → R4로 연기. ③ 자동 삽입/자동 실행 → 안전성 위반(사용자 검토 필수) → 기각.

> 관련: `docs/46`(Smart Assist 설계)·`docs/39`(UX-WP-01~03)·`docs/38`(UX Track)·`docs/14`(UI)·`CLAUDE.md §4·§5·§6`.

> 관련: `docs/39`(WP 백로그)·`docs/41`(Data/Model/Pilot Gate)·`docs/11`(ADR-001)·`docs/17`(RAG)·`docs/08`(NCR)·`CLAUDE.md §3·§11`.

## ADR-011. R2 Analytics — 데이터 정확성 우선 · 인박스 분석/시각화 (R1 위 확장)

- **상태**: 채택 (Accepted, 설계) — 구현은 추후 Codex 로컬(이 Linux 환경엔 .NET SDK 없음 → 본 ADR은 설계/계약만, 빌드·실행 증거 0). 실 오프라인 Test PC 게이트 증거 전까지 어떤 WP도 Gate PASS·VERIFIED로 표기하지 않는다(§11.4).
- **범위**: R2 = Risk Analytics & Visualization 4개 WP(R2-WP-01 Risk Semantic Hardening / R2-WP-02 Streaming·Performance / R2-WP-03 Prior-Day Analytics / R2-WP-04 Visualization·Report). 기준선 = main `c5464b8`, VERSION 0.6.0, R1 완료(`LimitMonitor`·공통 `LimitAnalysisResult`·6상태·대사·`ColumnMapping`·`CsvReader`/`XlsxReader`·`DataProfiler`).
- **제외 범위**: Local LLM/추론 Runtime/모델파일(ADR-003·009), Vector DB/Embedding(ADR-007), AI Commentary 본문 생성, 내부규정/NCR 원문 적재. R2는 **결정적(deterministic) 데이터 분석/시각화만** 다룬다.

---

### Context (맥락)

1. **왜 데이터 정확성을 LLM보다 먼저 두는가.** 본 제품의 신뢰는 "수치가 맞다"에서 나온다. 분석 입력(Join·단위·중복키·기준일)이 부정확하면 그 위에 얹는 어떤 AI 코멘터리도 잘못된 전제를 증폭할 뿐이다. ADR-002가 단일 분석 진실원장(`LimitAnalysisResult`)을 세웠으나, R1 구현에는 **무성(silent) 부정확 경로**가 남아 있다(아래 코드 근거). LLM 능력 확장(R4, ADR-003·009 = 승인 게이트 BLOCKED)보다 먼저, R1 위에 데이터 semantic을 경화(harden)하는 것이 우선순위다.

2. **R1 코드에서 확인된 부정확/미활성 경로**(직접 Read로 검증, main `c5464b8`):
   - `LimitMonitor.cs:135-136` — 한도 테이블을 Join Key로 `GroupBy` 후 **`group.Last()`로 중복 키를 임의 1건 선택**. 동일 (PortfolioId, RiskFactor)에 한도가 2건 이상이면 어느 것이 채택됐는지 사용자에게 보이지 않고, 입력 순서에 따라 수치가 흔들린다(비결정성 위험).
   - `LimitMonitor.cs:16,19` — `RECON_DUPLICATE_LIMIT`·`RECON_UNIT_MISMATCH` 상수는 존재하나 단위(통화·금액단위) 대사가 충분히 활성화되어 있지 않다. `ColumnMapping`(`LogicalColumn`)에는 통화/단위 논리컬럼이 없어(`BaseDate/PortfolioId/RiskFactor/ExposureAmount/LimitAmount/UseYn`만, `ColumnMapping.cs:3-11`) 통화·단위가 매핑·검증 대상에서 빠져 있다.
   - BASE_DT는 Ordinal 문자열로 비교(`LimitMonitor.cs:113·134·752` 류)되어 포맷 정규화/검증이 없다. 전일대비(R2-WP-03)에서 두 일자 문자열 포맷이 어긋나면 Join이 전부 New/Resolved noise로 무너진다.
   - `LimitAnalysisMetadata`(`LimitAnalysisResult.cs:57-63`)에 **Join 선택 규칙·중복키 처리 근거가 기록되지 않아** 사후 감사로 "왜 이 한도가 쓰였나"를 추적할 수 없다.

3. **시각화 수요와 절대 원칙의 충돌.** 대시보드/리포트(R2-WP-04)는 차트·Heatmap·TopN·집중도를 요구하나, 외부 charting NuGet(OxyPlot/LiveCharts/ScottPlot 등) 도입은 NuGet-0 불변식(§11.5·ADR-001/004)을 깬다 → STOP 트리거.

4. **대용량/손상 입력.** Golden6 export는 크고 깨질 수 있다. 전체 로드 기반 통계는 메모리/안정성 위험(RR-08)이 있어 streaming + 온라인 통계가 필요하다.

---

### Decision (결정)

**원칙: R2는 R1 위에 "결정적 데이터 정확성"을 경화한 뒤, 그 위에 인박스(in-box)만으로 분석·시각화를 얹는다. 부정확을 보정하지 않고 상태/Finding으로 표면화한다(임의 보정 0).**

1. **① 중복키·단위 Semantic Hardening 상태화 (R2-WP-01, RR-15).**
   - 중복 Limit Key를 `group.Last()` **임의 선택으로 더 이상 무성 처리하지 않는다.** 중복 발생 시 명시적으로 `DUPLICATE_LIMIT` 상태/Finding으로 차단·표면화하고 `RECON_DUPLICATE_LIMIT` 대사를 활성 카운트한다(임의 1건 채택 금지).
   - 통화·금액단위를 `ColumnMapping`(승인형, `config/column_mapping.json`)으로 관리 대상에 편입하고, `RECON_UNIT_MISMATCH`를 실제 활성화한다(노출↔한도 단위 불일치 시 합산 차단·상태화).
   - BASE_DT는 `System.Globalization`(`DateTime.TryParseExact`/Invariant)로 **형식 검증·정규화**한다. 보정 불가 시 `BASE_DT_FORMAT_MISMATCH`로 표면화(추정 보정 금지).
   - Join 선택 규칙·중복키 처리 근거를 **`LimitAnalysisMetadata`(Audit Metadata)에 기록**한다(생성자 확장 → `BuildResult`·`DashboardSnapshotBuilder`·`ExcelReportBuilder` 등 메타 소비부 동기 컴파일 수정, 기능 변경 0).

2. **② 대용량 Streaming + Welford 온라인 통계 (R2-WP-02, RR-08), 인박스만.**
   - 대용량·손상 파일을 `System.IO`(StreamReader 등) 기반 **streaming + 입력 상한**으로 처리(전체 메모리 로드 회피). 평균/분산은 **Welford 온라인 알고리즘**(1-pass, 수치 안정)으로 계산. SHA256 해시 Audit·결정적 프로파일 유지. 외부 NuGet 0.

3. **③ 전일 대비 결정적 출력 — 4구획 계약 (R2-WP-03).**
   - Current/Prev/Δ·TopN movers를 BASE_DT(① 정규화 전제) 기준 prior-day 결합으로 산출. 출력 계약 = **Data-Fact / Methodology / User-Validation / Hidden-Risk 4구획**(검토용 초안·결정적, 임의 보정 0). ① 미완 시 본 분석은 포맷 불일치를 `BASE_DT_FORMAT_MISMATCH` Hidden-Risk로 표면화하되 차단되지는 않는다(권장 선행, 비블로커). 순수 인박스 `decimal` 산술.

4. **④ 인박스 시각화 — 외부 charting NuGet 금지 (R2-WP-04).**
   - 차트/Heatmap/TopN/집중도·정확 Exception Count·Excel Report 강화를 **인박스 수단만으로** 구현: WPF `Shapes`/`DrawingVisual`/`Canvas`(화면) 또는 OOXML `chartXML` 직접 생성(`System.IO.Compression`, ADR-004 DM-03 정합, 엑셀 리포트). **외부 charting NuGet(OxyPlot/LiveCharts/ScottPlot 등) 도입은 STOP.** TopN/집중도 정의는 R2-WP-03의 4구획 계약과 정합시키고 중복 정의를 금지하며, 본 WP는 당일 `LimitAnalysisResult` 기준 집계만 한다. ① 미완 시 통화 혼합 합산은 `MIXED_CURRENCY` Finding으로 전제를 명시(차단 아님).

---

### STOP 가드 (외부 의존 0)

- **외부 NuGet PackageReference = 0** 유지 — R2 전 WP는 인박스 `System.*`(IO/Globalization/Text.Json/Linq/Diagnostics/Security.Cryptography)만 사용. 위반 시 STOP.
- **Vector DB / Embedding / Local LLM Runtime / 모델파일 = STOP**(승인 전 금지, ADR-003·007·009).
- **외부 charting NuGet = STOP** — 시각화는 인박스만(WPF Shapes/DrawingVisual/Canvas, OOXML chartXML).
- 외부 API/Telemetry/AutoUpdate 0 · SQL/VBA/Golden6 자동실행 0 · 해시 Audit(원문 미저장) · NoModelMode 유지 · 쓰기 경로 `logs/`·`reports/`·`config/` 한정.
- 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·모델파일 repo 미포함. seed/샘플은 일반 더미명만.

---

### Consequences (결과)

- **(+)** 무성 부정확(중복키 임의선택·단위 불일치·BASE_DT 포맷오류)이 **상태/Finding/Audit Metadata로 표면화** → ADR-002의 단일 진실원장이 실제로 신뢰 가능해지고, 그 위 분석(전일대비)·시각화가 정합 전제를 갖는다.
- **(+)** R2-WP-01이 ④·③의 통화 혼합 합산·prior-day Join 정합을 떠받치는 사실상의 선행(강결합 아님, 미완 시 차단 대신 명시적 Finding). R2-WP-02는 독립 시작 가능(파일 충돌 낮음: Data/ vs Risk·Mapping/).
- **(-)** `LimitAnalysisMetadata` 생성자 변경으로 메타 소비부(BuildResult·Dashboard·Report) 동기 컴파일 수정 필요(기능 변경 0). 중복키를 더 이상 묵시 채택하지 않으므로, 기존 중복 입력은 이제 `DUPLICATE_LIMIT`로 드러나 사용자 조치(데이터 정합)가 요구된다 — 의도된 행동 변경.
- **(-)** 인박스 시각화는 외부 라이브러리 대비 표현/공수 제약이 크다(차트 종류·렌더 품질). 이를 감수해 NuGet-0·반입 안전을 우선한다.
- **(검증 한계)** 본 ADR·R2 WP는 이 Linux 환경에서 빌드/실행 불가 → 설계/계약만. 기존 SmokeTest Total 보존 + 신규 단언은 도메인 분류기로 분류(Unclassified=0). 생성 xlsx 손상 검증·WPF 렌더·streaming 벤치는 실 Test PC PILOT Gate(BLOCKED) 증거 전까지 PASS/VERIFIED 금지(과대표기 금지 §11.4).

## ADR-012. Authenticode 코드 서명 — 독립 신뢰 앵커 (STAB-WP-05 승인 요건 · STOP 게이트)

- **상태**: 채택(요건 정의) — 서명 도입 **CODE_SIGNING_APPROVAL_REQUIRED**. ADR-008(§결정2·5) 보강, STAB-WP-05. **v0.7.0은 본 결정 전 미서명+manifest/Fail-Closed 앵커로 출하**(서명은 후속·릴리스 전제 아님). 외부 신뢰 루트(인증서·서명 도구)가 필요하므로 **STOP 규칙(§11.5)** — 인증서 경로 승인(`docs/41 §6`) 전 서명 도구/인증서/Dependency 추가 0.
- **인증서 경로 결정(2026-06-30, 사용자 승인) = (A) 사내 Enterprise CA 코드서명 인증서.** 오프라인·비용 0·반입 용이, 내부 신뢰 한정(사내 오프라인 배포 성격에 적합). 나머지 §6.2 항목(인증서 보관·반입 절차·런타임 검증 정책·오프라인 폐기 처리·Rollback)과 서명 도구(**인박스 `Set-AuthenticodeSignature` 1순위**, signtool=Windows SDK이면 별도 승인)는 **STAB-WP-05 구현 WP에서 확정**한다. 실 인증서·서명·검증은 Windows 실 Test PC/운영 환경 의존 → 그 전까지 STAB-WP-05는 **APPROVAL_REQUIRED(경로만 확정)** 유지, 실 증거 전 VERIFIED 금지.
- **맥락**: ADR-008이 Release Integrity Manifest + 런타임 Fail-Closed(STAB-WP-03a/03b, #59/#61 VERIFIED-local)를 세웠으나, **§결정5에서 명시한 3건의 잔여 위험은 manifest만으로 닫히지 않는다**(쓰기 가능 폴더에 manifest가 같이 있어 독립 신뢰 앵커가 없음):
  1. **콘텐츠 lock-step co-tamper** — 파일과 `approved_manifest.json`을 동시에 같은 해시/크기로 변조하면 통과.
  2. **self-contained 런타임 DLL(~150개: coreclr/hostfxr/System.Security.Cryptography 등) 미해시** — 관리 어셈블리 게이트 우회 가능.
  3. **폴더 동반 변조**(추가 파일 주입).
  이들은 **빌드 시점에 외부 신뢰 루트로 서명**하고, 런타임이 그 서명을 **manifest 신뢰의 선행 앵커**로 검증해야만 닫힌다. ADR-008 §대안에서 "기대해시를 Core 상수로 임베드"는 해시 고정점 부재(정상 패키지 brick)로 **기각**되었고 코드 서명으로 대체하기로 명문화됨.
- **결정 — 승인 문서 필수 항목**(서명 도입 전 STAB-WP-05 STOP 해제 조건; 게이트 `docs/41 §6`):
  1. **인증서 경로 선택**(아래 4안 중 1) — 발급 주체·신뢰 체인·**비용/갱신주기**·사내 정책 적합성.
  2. **인증서 보관·반입 절차** — 개인키 저장(HSM/USB 토큰/Windows 인증서 저장소)·서명 PC·키 접근 권한·**repo에 인증서/키 0**(개인키 `*.pfx/*.p12/*.pem/*.key` **및** 공개 인증서 `*.cer/*.crt/*.der` **모두 절대 미포함**, Gate A — 공개 인증서도 신뢰 material이므로 repo에 두지 않고 반입 절차로만).
  3. **서명 대상 범위** — 관리 어셈블리(`RiskManagementAI.exe`/`.dll`/`.Core.dll`) 1차, self-contained 런타임 DLL은 **서명 카탈로그(.cat) 또는 배포 정책**으로 확장(범위 2의 닫힘 방식).
  4. **런타임 검증 정책** — 시작 시 자기(게시자/서명) 검증을 **manifest 신뢰의 선행**으로 둔다(서명 검증 PASS 후에만 manifest 신뢰). 오프라인 환경에서 **체인/타임스탬프/폐기(CRL/OCSP) 검증을 어떻게 처리**하는지(오프라인 = 폐기 조회 불가 → 정책 명시) · 미서명/불일치 시 Fail-Closed 동작.
  5. **Rollback / 인증서 만료·폐기 대응** — 서명 만료·인증서 교체 시 기존 릴리스 동작·재서명 절차.
  6. **서명 도구 선택 + 오프라인·NuGet-0 영향** — 서명 **생성** 도구를 명시·승인한다. ⚠️ **`signtool.exe`는 .NET 8 SDK 인박스가 아니라 Windows SDK 구성요소**(별도 설치) → 도입 시 **외부 도구 = 승인 범위**. **인박스 대안 = PowerShell `Set-AuthenticodeSignature`**(Windows 내장, 추가 설치 0) — 1순위. 서명 **검증**(런타임)은 인박스 `System.Security.Cryptography.X509Certificates`(+ `WinVerifyTrust` P/Invoke)로 가능하므로 NuGet 0 유지. **결론**: 서명 생성은 ⓐ 외부 인증서 + ⓑ 서명 도구(인박스 `Set-AuthenticodeSignature` 우선, signtool은 Windows SDK 승인 시) 둘 다 STOP 트리거 — 본 게이트에서 함께 승인.
- **인증서 경로 후보(승인 결정 = 사용자/문서오너)**:
  - **(A) 사내 Enterprise CA 발급 코드서명 인증서** — 사내 신뢰 루트가 도메인에 배포되어 있으면 **오프라인·비용 0·반입 용이**. 사내 PC 외부에선 신뢰 안 됨(내부 배포 한정 = 본 제품 성격에 적합). **권장 1순위 후보**(사내 CA 존재 시).
  - **(B) 상용 OV(Organization Validation) 코드서명 인증서** — 공인 신뢰 루트, 외부에서도 신뢰. 연 비용·발급 심사. 폐기/타임스탬프 온라인 의존(오프라인 검증 정책 필요).
  - **(C) 상용 EV(Extended Validation) 코드서명 인증서** — 하드웨어 토큰/HSM 강제, SmartScreen 평판 즉시. 비용·운영 부담 최대. 본 제품(사내 오프라인 배포)엔 과함.
  - **(D) 자체 서명(self-signed) + 사내 신뢰 저장소 수동 등록** — 비용 0이나 신뢰 배포가 수동·취약. (A) 불가 시의 최소안.
- **검증(승인 후 — 잔여 3건 폐쇄 매핑, 과대표기 금지)**: ADR-008 §결정5 잔여가 실제로 닫혔음을 **각각 회귀로 고정**한다(현재 STAB-WP-03b SmokeTest는 이 3건을 "미탐지=양성"으로 고정 중 → 서명 도입 시 "탐지/차단"으로 **전환**).
  - ① **콘텐츠 lock-step co-tamper**: 파일+`approved_manifest.json` 동시 변조 패키지 = **서명 검증 실패로 기동 차단**(03b에서 통과하던 케이스가 FAIL→차단). 미서명/서명 불일치도 차단.
  - ② **런타임 DLL 변조**: self-contained DLL(coreclr 등) 변조 = **서명 카탈로그(.cat)/게시자 검증으로 차단**(범위 항목 3의 닫힘 입증). 카탈로그 미적용 DLL이 남으면 그 범위를 **명시적 OPEN으로 표기**(과대표기 금지).
  - ③ **폴더 동반 변조**(미선언 파일 주입): 서명 앵커 + manifest 인벤토리 교차로 **차단/표면화**.
  - 위 회귀가 PASS 전환되기 전까지 STAB-WP-05는 VERIFIED로 적지 않으며, 실 Windows Test PC Gate B/C 증거 전까지 **BLOCKED** 유지.
- **대안/기각**: ① 서명 없이 manifest만 유지 → §결정5 잔여 3건 미해소(co-tamper/런타임 DLL/폴더 변조) → 부분 보호로 잔여 OPEN 명시(현 v0.7.0 상태). ② 승인 없이 인증서·서명 도구 선반입 → STOP·원칙 위반 → 기각. ③ 기대해시 Core 상수 임베드(앵커 시도) → 해시 고정점 부재로 정상 패키지 brick → ADR-008에서 이미 기각.

> 관련: `docs/40` ADR-008(무결성 manifest/Fail-Closed)·`docs/41 §6`(코드서명 승인 게이트)·`docs/39`(STAB-WP-05)·`docs/47 §1·§4`(v0.7.0 미서명 출하 고지)·`CLAUDE.md §3·§8·§11.5`.

## ADR-013. 공개 규정 원문 Clause/Chunk 검색 — 인박스 keyword-only · Pack 적재 (KB 트랙 v0.8)

- **상태**: 채택(설계) — 구현 = KB-WP-01/02(Codex 로컬). ADR-007(Knowledge Pack Contract)의 미구현 단계(Clause/Chunk)를 구체화. 실 규정 원문 적재는 Prod 권한통제(repo 미포함), 실 Test PC Gate 전 VERIFIED 금지(§11.4).
- **맥락**: 현재 KB 검색은 공개 catalog **metadata 레벨**까지만 동작한다(`RegulationCatalog` 16필드 → `KbIndex` 인박스 inverted index → `KbSearch` 인용형 DraftAnswer + 해시 audit). 그러나 **조항(clause) 원문은 의도적으로 부재**: `KbSearchResult.Clause`는 항상 고정 문자열로 채워지고(`KbSearch.cs`), `KbAccessPolicy`의 **`SourceTextAllowed`는 항상 false 하드코딩**이라 어떤 경로로도 원문이 노출되지 않는다. v0.8 목표 "공개 규정 원문 Clause/Chunk 검색"을 **인박스 keyword-only**로 올리되, 절대원칙(원문 repo 미포함·NuGet 0·Vector/Embedding STOP)을 깨지 않아야 한다.
- **결정**:
  1. **데이터 = Clause Pack(repo 미포함, 런타임 적재).** 공개 규정 원문 clause는 ADR-007 ②(Public Knowledge Pack / Offline Ingestion)로만 적재한다. **repo에는 합성(dummy) clause 샘플만** 둔다(테스트·데모용, 실 규정 원문 0). 신규 record `RegulationClause`(또는 `KbChunk`): `ChunkId·SourceId(catalog FK)·ClauseRef(제N조 등)·ClauseText·EffectiveDate·RepealDate·PackVersion·SourceTextHash`. Pack 미적재 시 **catalog-only로 graceful fallback**(기존 검색 동작 불변).
  2. **`ChunkId` 결정성 + 충돌 방지.** `ChunkId`는 `(SourceId, ClauseRef, PackVersion, SourceTextHash)`로 결정한다 — `SourceTextHash`(=`LogHash.Sha256Hex(ClauseText)`)를 키에 포함해, **동일 `(SourceId,ClauseRef,PackVersion)`에 상이한 `ClauseText`가 같은 ID로 silent overwrite되는 것을 차단**. 동일 키+상이 텍스트 로드 시 **거부+Warning**(결정적).
  3. **인덱스 알고리즘 단일 원천(중복 엔진 금지).** `KbIndex`의 inverted index 코어(`TextKeys`/`BoundedSubstrings` L≤32 cap/`SplitTerms`/`RequiresLinearFallback`/`DeterministicSignature`)를 **internal static 유틸로 추출**해 catalog 인덱스와 clause 인덱스가 **동일 알고리즘**을 호출한다(별도 ClauseIndex 엔진 신설 금지). **후보 발견은 단일 원천**, **점수 함수는 입력 타입별 분리**(catalog 7필드 가중 vs clause 본문 가중). 기존 `KbTests`의 linear==index/`DeterministicSignature`/한글 부분일치 단언이 catalog 경로에서 전부 PASS 보존됨을 완료조건화.
  4. **노출 게이트 분리 — `SourceTextAllowed`는 false 불변.** catalog 원문은 영구 차단(`KbAccessPolicy.SourceTextAllowed`=false **변경 0**). clause 발췌(Snippet) 허용은 **신규 단일 게이트 `ClauseSnippetAllowed(entry)`**로만 판정: `status=PublicCited`(공개) **AND** gate-metadata 비-placeholder(`approval_status`/`license_status`가 `CONFIRM_*`/`NOT_LOADED` 아님, `effective_date` 확정)일 때만 true. `KbAccessDecision`에 필드 **additive 추가**. PROD_ONLY/MANUAL_APPROVAL/미지 status·placeholder-metadata 공개항목은 **발췌 0**(메타+표식만).
  5. **clause 유효구간 결정성.** `EffectiveDate`/`RepealDate` 문자열 파싱 계약을 명시: 빈 `RepealDate`=무기한 활성, 미파싱 `EffectiveDate`=비활성+경고, `asOfDate`(주입 `IClock`) 경계 결정적. (기존 `NormalizeSearchDate`는 query asOf만 다루므로 clause 경계 파싱은 별도.)
  6. **원문 유입 가드(4자 정합).** clause-pack/합성샘플 경로는 아래 4곳이 **대칭**이어야 원문 유입을 막는다 — 어느 하나라도 비대칭이면 가드 우회. 합성 샘플은 **`kb/` 하위**(예 `kb/clause_pack_sample/`)에 둬 기존 가드 커버를 받는 것을 **디폴트**로 한다(ScanDirectories 변경 0). `config/kb/clauses` 등 신규 경로가 필요하면 4곳 동기 갱신 필수.

| 경로/가드 | 커버 범위 | 신규 추가 시 동기화 |
|---|---|---|
| `KbRepositoryGuard.ScanDirectories` | `kb`, `config/ncr` | clause-pack가 `config/kb`면 여기에 add-only |
| `build/03_verify-package.ps1` SourceTextScanDirs | `kb`,`config`,`samples`,`data_sources` | 한글 토큰은 `New-StringFromCodeUnits @(0x..)` UTF-16 리터럴 |
| `KbTests.cs:169` 하드코딩 ScanDir 리스트 | `{kb,config,samples,data_sources}` | reflection 미러(옵션) 또는 명시 추가 — **세 곳 미러는 토큰/allowlist에만 자동 적용, ScanDir는 4번째 수동** |
| `KbRepositoryGuard.MetadataAllowlist` | 합성 샘플 파일 등재 | build/03 `$SourceTextAllowlist` 미러 동기 |

  7. **합성 더미 비충돌 규칙.** 합성 clause 샘플의 파일명·헤더·본문은 신규/기존 `SuspiciousNameTokens`·`SuspiciousContentTokens` 어느 것도 부분문자열로 포함하지 않는다(헤더=`clause_ref`/`clause_body` 영문, 본문=`제0조 (합성 테스트) 본 더미 조문…`처럼 `원문`/`조항 원문` 토큰 회피). 신규 한글 토큰(`조항 원문` 등)은 build/03에 UTF-16 code-unit 리터럴로 추가하고 `KbTests` code-unit 미러 단언을 통과시킨다. **`KbRepositoryGuard.Scan(현 repo) Blocker=0`이 합성 샘플·신규 토큰 추가 후에도 보존**됨을 회귀로 고정.
- **STOP 가드**: 검색은 **인박스 keyword/inverted index만**(NuGet 0). Vector DB·Embedding·LLM Runtime·모델파일 필요 시 즉시 STOP→승인. 실 규정 원문 repo 커밋 0(합성 더미만). 해시 전용 audit(검색 행위 `TaskLogWriter` 재사용).
- **대안/기각**: ① clause 원문을 repo에 직접 포함 → 원칙 위반·기각(Pack 적재만). ② 별도 ClauseIndex 엔진 신설 → 알고리즘 중복·기각(코어 유틸 공유). ③ `SourceTextAllowed`를 true로 전환해 발췌 허용 → 기존 metadata-only 경로 붕괴·기각(신규 `ClauseSnippetAllowed`로 분리). ④ Vector/Embedding 도입 → STOP·미승인.

> 관련: `docs/40` ADR-007(Knowledge Pack Contract)·`docs/17`(KB RAG 설계)·`docs/41 §2`(RAG/NCR Approval Gate)·`docs/39`(KB-WP-01/02)·`CLAUDE.md §10`(규정 답변 10단계)·`AGENTS.md §3·§4`.

## ADR-014. R5 Feedback Learning — 승인 Example RETRIEVAL · Prompt 반영 (학습 아님)

- **상태**: 채택(설계) — 구현 = FEEDBACK-WP-01/02(Codex 로컬). 현재 R5 = **PARTIAL**(승인 Example 승격+영속+UI 표시까지). 실 Test PC Gate 전 VERIFIED 금지.
- **맥락**: R5 현 상태는 `FeedbackLogEntry`(승인 표식) → `ExamplePromotion.PromoteApproved`(승인 필터·중복차단·UserIdHash 검증) → `PromotedExampleStore`(`config/promoted_examples.jsonl` append/readAll) → MainWindow Feedback Center 표시까지다. 그러나 **(a) `PromotedExample`은 메타데이터만 보관하고 초안 본문(draft text)이 전무**하며, **(b) 저장본을 질의로 검색하거나 `DraftPipeline`/`DraftRequest`에 주입하는 retrieval 경로가 코드에 0**이다. 즉 "검색+Prompt 반영" 중 **저장만 있고 검색→주입은 NOT_IMPLEMENTED**. no-training 경계는 `PromotionMode="ExampleCurationOnly"` 상수·모델 부재로 사실상 보장되나 retrieval-side 명시 가드는 미정의.
- **결정**:
  1. **RETRIEVAL이지 학습이 아니다(불변).** R5는 **승인된 Example을 결정적으로 검색해 프롬프트에 read-only 참고로 주입**하는 조회다. **모델 가중치 학습·fine-tune·모델 파일 쓰기/갱신 0**(절대원칙). `PromotionMode=ExampleCurationOnly` 의미 보존. retrieval/주입 경로는 어떤 파일도 모델로 쓰지 않으며 append-only audit만 남긴다.
  2. **Example 본문 출처 = `FeedbackLogEntry`에 `string? DraftBody = null`(맨 끝 nullable) additive 추가.** 위치 불변(기존 6-positional 생성자 호출·`AuditTests` 회귀 보존). `FeedbackLogWriter`/MainWindow 입력 경로가 본문을 전달. 본문이 없으면 `ExampleBody=null` + **검색은 메타만**(정상 경로). 본문 유형(SQL/VBA/기타)은 명시 `kind` 또는 본문 휴리스틱으로 판정하되 **불확실 시 본문 미저장(null)+warning**(보수적 기본값).
  3. **Ingest 게이트(승격 시점).** 본문 저장 전 **`SqlSafetyChecker`/`VbaSafetyChecker` Blocker 0 AND 신규 `ForbiddenTermScanner`(Core, 인박스, 정적 토큰 리스트) 0**을 통과해야 한다. 토큰 집합 = 내부규정/NCR 원문·실데이터·실 테이블/컬럼·PII 패턴(최소 집합 명시). **실패 시 본문 null+warning(승격은 메타로 진행)**. — `KbRepositoryGuard.Scan`은 파일/디렉토리 스캐너이며 토큰이 private이므로 **본문 string 검사에 재사용 불가** → 신규 `ForbiddenTermScanner`를 **단일 토큰 원천**으로 둔다(중복정의 금지, NuGet 0).
  4. **검색 = 인박스 결정적.** `PromotedExampleStore.ReadAll()` 위에 keyword/score 검색(가능하면 `KbIndex` 코어 유틸 재사용) + **`OrderByDescending(Score).ThenBy(ExampleId, StringComparer.Ordinal)`** 안정 정렬. Vector/Embedding STOP. 검색/주입 행위는 **`TaskLogWriter` 스키마(UserId/RequestHash/OutputHash 모두 SHA-256 hex)** 해시 audit(원문 미저장).
  5. **Prompt 반영(FEEDBACK-WP-02) = review 경유 read-only 주입.** 검색 결과를 `DraftRequest.Context`(또는 신규 옵션 필드)에 **참고 블록으로 결합**하되 원 Context 보존, **자동 무검토 주입 금지**(review/approval), 산출은 검토용 초안. 관측은 테스트용 capture `ILocalDraftService`로(=`NoModelDraftService`는 Context 결합 직접 관측 불가).
  6. **영속·커밋 가드.** Example 본문 jsonl은 `config/` 샌드박싱(`PromotedExampleStore.ResolveConfigFile` 패턴) + **`.gitignore`에 `config/promoted_examples*.jsonl`(및 smoke fixture) 추가**(STAB-UX-02 `*.local.json` 동형). 본문 영속 파일이 **tracked되지 않음**을 SmokeTest로 고정. 개수/크기 상한·revoke(무효화)는 후속 고려.
  7. **테스트 도메인 = Audit.** `SmokeTestContext.ClassifyDomain`은 `Feedback`/`PromotedExample`/`ExamplePromotion` 토큰을 **Audit(line 97)**로, UiContract보다 먼저 분류한다. 신규 단언 설명에 Kb 키워드(`검색`/`원문`/`공개`/`인용`)를 쓰면 Kb로 오분류되므로 영어 `search`/`retrieval` + `PromotedExample`/`Feedback`/`Audit` 토큰을 쓴다. `Unclassified=0` 보존.
- **STOP 가드**: 모델 가중치 학습 0·모델파일 쓰기 0 · Vector/Embedding/LLM Runtime STOP · NuGet 0 · 해시 전용 audit(원문/raw prompt/user id 평문 미저장) · 쓰기 `config/`·`logs/` 한정 · 실데이터/원문/PII 본문 혼입 차단(ingest 게이트).
- **대안/기각**: ① 모델 미세조정/가중치 갱신 → 절대원칙 위반·기각(retrieval-only). ② `KbRepositoryGuard.Scan` 본문 재사용 → API 불일치·기각(신규 `ForbiddenTermScanner`). ③ 본문 출처 미정 상태로 `ExampleBody` 채움 → 기각(`FeedbackLogEntry.DraftBody` nullable로 명시). ④ 본문 평문 audit → 원칙 위반·기각(해시 전용).

> 관련: `docs/40` ADR-003/009(LLM·Model Approval)·`docs/39`(FEEDBACK-WP-01/02, C-20)·`docs/41 §3`(Model Gate)·`CLAUDE.md §3`·`AGENTS.md §3`.
