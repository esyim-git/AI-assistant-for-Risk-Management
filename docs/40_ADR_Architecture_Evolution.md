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
