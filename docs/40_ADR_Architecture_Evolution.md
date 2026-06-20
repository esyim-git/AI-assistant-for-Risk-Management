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

> 관련: `docs/39`(WP-01~05), `docs/41`(Model/Data Gate), `docs/11`(ADR-001), `CLAUDE.md §3`.
