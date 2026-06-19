# 21. Implementation Backlog (MVP-1)

## 목적

Codex가 MVP-1을 작은 단위로 구현/보강할 수 있도록 작업 항목을 정의한다.
각 항목은 ID / 제목 / 목적 / 입력 / 처리 / 출력 / 완료 조건 / 테스트 조건 / 보안 유의사항 / 예상 수정 파일 형식으로 작성한다.

## 적용 범위

- C# / .NET / WPF 기반 Local AI Assistant의 룰 엔진, 데이터 프로파일링, 로깅, 최소 UI.
- Local LLM, 규정 RAG, NCR 연동, Excel 리포트 생성 등 LLM/대용량 의존 기능은 후속 단계(MVP-2 이후)로 분리한다.

## 제외 범위

- 실제 Golden6 자동 접속/실행
- VBA 자동 실행
- 외부 API / 클라우드 호출
- 모델 자동 재학습
- 실제 회사 데이터/내부규정 원문 적재

---

## 현재 구현 상태 요약 (스타터 v2 기준)

> 아래는 Claude Code가 소스를 직접 점검하여 확인한 사실이다. Codex는 **이미 구현된 항목을 다시 만들지 말고**, 검증/보강 또는 신규 항목에 집중한다.

| 영역 | 상태 | 비고 |
|---|---|---|
| `SafetyFinding`, `SafetySeverity`, `RulePattern` | ✅ 구현됨 | `record`/`enum` 모델 존재 |
| `SqlSafetyChecker` | ✅ 구현됨 | 규칙이 **C# 코드에 하드코딩** |
| `VbaSafetyChecker` | ✅ 구현됨 | 규칙이 **C# 코드에 하드코딩** |
| `Excel2021FunctionChecker` | ✅ 구현됨 | 금지 함수 목록 **하드코딩** |
| `TaskLogEntry`, `FeedbackLogEntry` | ✅ 모델만 존재 | 저장(persist) 로직 없음 |
| WPF `MainWindow` 3개 Checker 호출 | ✅ 구현됨 | 결과를 텍스트로 표시 |
| SmokeTest 콘솔 | ✅ 구현됨 | SQL/VBA/Excel 기본 케이스 |
| **Rule Loader (`rules/*.txt` → Checker)** | ❌ 미구현 | 룰 파일이 코드에 반영되지 않음 (핵심 갭) |
| **DataProfiler 로직** | ❌ 미구현 | `DataProfileResult` 모델만 존재 |
| **TaskLog/FeedbackLog 저장기** | ❌ 미구현 | `logs/`에 기록하는 writer 없음 |
| **설정/정책 로딩** | ❌ 미구현 | `config/security_policy.json` 미사용 |

---

## 백로그 항목

### B-01. RuleLoader 구현 (룰 파일 → Checker 주입)

- **목적**: 위험 룰을 코드 하드코딩이 아니라 `rules/*.txt`에서 로드하여, 운영 중 룰 갱신과 룰 버전 관리를 가능하게 한다. (감사 가능성 향상)
- **입력**: `rules/sql_deny_patterns.txt`, `sql_warn_patterns.txt`, `vba_deny_patterns.txt`, `vba_warn_patterns.txt`, `excel_2021_blocked_functions.txt`, `excel_2021_preferred_functions.txt`
- **처리**: 파일을 읽어 주석(`#`)/빈 줄 제외, 정규식 패턴 목록 생성. `RulePattern` 컬렉션으로 변환. 파일 부재 시 코드 내장 기본값으로 안전하게 폴백. **`REQUIRE_PRESENT:` 접두 패턴**(예: `vba_warn_patterns.txt`의 `Option Explicit`)은 "있어야 하는 요소"로 해석하여 **부재 시 경고**한다(존재 시 경고하지 않음).
- **출력**: `IReadOnlyList<RulePattern>` 또는 룰 세트 객체. 룰 세트의 버전 식별자(예: 파일 해시 또는 `rules/RULESET_VERSION` 값).
- **완료 조건**: `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`가 RuleLoader로부터 룰을 주입받아 동작. 룰 파일 수정만으로 탐지 패턴이 바뀜.
- **테스트 조건**: 임시 룰 파일로 신규 패턴 추가 시 탐지됨. 파일 없을 때 폴백 동작. 기존 SmokeTest 전부 통과.
- **보안 유의사항**: 룰 파일 경로는 상대경로(앱 기준). 외부 임의 경로 로드 금지. 룰 파일 자체에 민감정보 금지.
- **예상 수정 파일**: `src/RiskManagementAI.Core/Safety/RuleLoader.cs`(신규), `SqlSafetyChecker.cs`, `VbaSafetyChecker.cs`, `Excel/Excel2021FunctionChecker.cs`, `tests/.../Program.cs`

### B-02. SqlSafetyChecker 검증/보강

- **목적**: 이미 구현된 SQL 검사기가 CLAUDE.md의 차단 목록(INSERT/UPDATE/DELETE/MERGE/CREATE/ALTER/DROP/TRUNCATE/GRANT/REVOKE/EXEC/CALL/COMMIT/ROLLBACK)을 모두 커버하는지 검증하고, 경고 패턴(SELECT *, WHERE 1=1, CROSS JOIN, 옵티마이저 힌트)을 추가한다.
- **입력**: 임의 SQL 텍스트
- **처리**: deny/warn 룰 매칭. 매칭 시 `SafetyFinding`(코드/심각도/메시지/매칭문자열/위치) 생성.
- **출력**: `IEnumerable<SafetyFinding>`
- **완료 조건**: 13개 deny 키워드 + 주요 warn 패턴 탐지. 정상 SELECT는 Blocker 없음.
- **테스트 조건**: DELETE/UPDATE/DROP → Blocker. `SELECT col FROM t WHERE base_dt=:d` → Blocker 없음. `SELECT *` → Medium 경고.
- **보안 유의사항**: 검사 대상 SQL을 로그에 원문 저장하지 말 것(해시만). 자동 실행 절대 금지.
- **예상 수정 파일**: `Safety/SqlSafetyChecker.cs`, `rules/sql_*`

### B-03. VbaSafetyChecker 검증/보강

- **목적**: VBA 위험 API(Shell/WScript.Shell/Kill/FileSystemObject/Declare PtrSafe/Outlook.Application/WinHttp/MSXML2.XMLHTTP/FollowHyperlink) 탐지 및 Option Explicit 누락 경고를 검증한다.
- **입력**: 임의 VBA 텍스트
- **처리**: deny/warn 룰 매칭 + Option Explicit 존재 여부 확인.
- **출력**: `IEnumerable<SafetyFinding>`
- **완료 조건**: 위 API 전부 탐지. Option Explicit 없으면 경고.
- **테스트 조건**: `Shell "cmd.exe"` → `VBA_SHELL` Blocker. Option Explicit 누락 → `VBA_OPTION_EXPLICIT_MISSING`.
- **보안 유의사항**: VBA 자동 실행 금지. 외부 URL/네트워크 호출 패턴은 Blocker.
- **예상 수정 파일**: `Safety/VbaSafetyChecker.cs`, `rules/vba_*`

### B-04. Excel2021FunctionChecker 검증/보강

- **목적**: Excel 365 전용 함수(VSTACK/HSTACK/TOCOL/TOROW/TAKE/DROP/CHOOSECOLS/TEXTSPLIT/TEXTBEFORE/TEXTAFTER/GROUPBY/PIVOTBY/MAP/REDUCE/BYROW/BYCOL/REGEX*) 사용을 탐지하고 대체안을 안내한다.
- **입력**: 수식 문자열 또는 텍스트
- **처리**: 금지 함수 목록 매칭. 매칭 시 대체안(보조열/INDEX·MATCH/PivotTable/VBA) 메시지 포함 `SafetyFinding` 생성.
- **출력**: `IEnumerable<SafetyFinding>`
- **완료 조건**: 금지 함수 전부 탐지. `=XLOOKUP(...)` 등 허용 함수는 경고 없음.
- **테스트 조건**: `=VSTACK(A1:A3,B1:B3)` → `EXCEL_365_FUNCTION`.
- **보안 유의사항**: 없음(정적 검사). 
- **예상 수정 파일**: `Excel/Excel2021FunctionChecker.cs`, `rules/excel_2021_*`

### B-05. DataProfiler 구현

- **목적**: Golden6 Export 더미 CSV/XLSX를 프로파일링한다.
- **입력**: CSV 파일 경로(우선 CSV. XLSX는 후속).
- **처리**: 행 수, 컬럼 수, 컬럼별 Null 수, 중복 행 수, 기준일(예: `BASE_DT`) 분포, 숫자 컬럼 합계/최소/최대, 단순 이상값 표시.
- **출력**: `DataProfileResult`
- **완료 조건**: `samples/dummy_data/risk_exposure_sample.csv` 프로파일 정상 산출.
- **테스트 조건**: SmokeTest에서 더미 CSV 행/컬럼/Null 카운트 검증.
- **보안 유의사항**: 실데이터 경로 하드코딩 금지. 읽기 전용. 결과에 원본 행 다량 저장 금지.
- **예상 수정 파일**: `Data/DataProfiler.cs`(신규), `Data/DataProfileResult.cs`, `tests/.../Program.cs`, (선택) NuGet 없이 자체 CSV 파서.

### B-06. TaskLog / FeedbackLog 저장기 구현

- **목적**: 작업 이력과 승인형 피드백을 감사 가능한 형태로 `logs/`에 기록한다.
- **입력**: `TaskLogEntry` / `FeedbackLogEntry`
- **처리**: JSON Lines 또는 CSV append. 민감정보는 해시(`RequestHash`/`OutputHash`)만 저장.
- **출력**: `logs/task_log.jsonl`, `logs/feedback_log.jsonl`(또는 .csv)
- **완료 조건**: 검사 1회 수행 시 TaskLog 1줄 기록. 피드백 입력 시 FeedbackLog 기록.
- **테스트 조건**: 기록 후 파일 존재/형식 검증. 원문 SQL/데이터가 평문으로 저장되지 않음 확인.
- **보안 유의사항**: **원문 저장 금지(해시만)**. `logs/`는 운영환경 쓰기 허용 경로. 로그에 계정/비밀정보 금지.
- **예상 수정 파일**: `Logging/TaskLogWriter.cs`, `Logging/FeedbackLogWriter.cs`(신규), 모델 파일.

### B-07. 설정/정책 로딩 (security_policy.json)

- **목적**: `config/security_policy.json`을 앱 시작 시 로드하여 외부 API/자동실행/자동업데이트/telemetry 기본 차단 상태를 보장하고 UI에 표시한다.
- **입력**: `config/security_policy.json`, `config/appsettings.*.json`
- **처리**: 정책 읽기. `AllowExternalApi=false` 등이면 관련 기능 비활성. 위반 시도 시 차단.
- **출력**: 런타임 정책 객체. 시작 화면에 "오프라인/외부통신 차단" 상태 표시.
- **완료 조건**: 정책 false 값이 실제 동작을 막음.
- **테스트 조건**: 정책 로드 단위 테스트(또는 SmokeTest 확장).
- **보안 유의사항**: 정책 파일이 없으면 **가장 안전한 값(전부 false)** 으로 폴백.
- **예상 수정 파일**: `Config/SecurityPolicy.cs`, `Config/PolicyLoader.cs`(신규), `App.xaml.cs`, `MainWindow.xaml.cs`

### B-08. 최소 UI 보강

- **목적**: 현재 단일 입력/결과 창을 SQL/VBA/Excel/Data 탭 구분과 심각도 색상 표시로 보강.
- **입력**: 사용자 텍스트/파일
- **처리**: 탭별 Checker 호출, Finding 목록을 심각도별로 정렬/색상 표시, "오프라인 모드/모델 없음" 상태 배지.
- **출력**: 사람이 읽기 쉬운 Finding 목록 UI.
- **완료 조건**: 모델/인터넷 없이 앱 기동·동작.
- **테스트 조건**: 수동 실행 확인 + 기존 SmokeTest 통과.
- **보안 유의사항**: UI에 실데이터 캐시/잔존 금지.
- **예상 수정 파일**: `App/MainWindow.xaml`, `MainWindow.xaml.cs`

### B-09. SmokeTest 확장

- **목적**: 위 신규 기능(RuleLoader/DataProfiler/Log/Policy) 회귀 테스트 추가.
- **완료 조건**: `dotnet run --project tests/RiskManagementAI.SmokeTests` 시 전부 PASS, 실패 시 exit code 1.
- **보안 유의사항**: 테스트 데이터는 더미만 사용.
- **예상 수정 파일**: `tests/RiskManagementAI.SmokeTests/Program.cs`

---

## MVP-1 전체 완료 조건 (Definition of Done)

- [ ] `dotnet build` 성공 (외부 NuGet 의존성 최소/없음)
- [ ] SmokeTest 전부 PASS
- [ ] SQL DELETE/UPDATE/DROP 등 차단 명령 탐지
- [ ] 정상 SELECT 통과(Blocker 없음)
- [ ] VBA Shell/WScript/Kill 탐지, Option Explicit 누락 경고
- [ ] Excel VSTACK/HSTACK/TEXTSPLIT/MAP/REDUCE/BYROW/BYCOL 탐지
- [ ] 룰이 `rules/*.txt`에서 로드됨(RuleLoader)
- [ ] 더미 CSV 데이터 프로파일링 동작
- [ ] TaskLog/FeedbackLog가 해시 기반으로 기록됨
- [ ] 결과가 사람이 이해 가능한 Finding 목록으로 반환
- [ ] Local LLM 없이 앱 실행 가능
- [ ] 인터넷 없이 앱 실행 가능

## 향후 확장 (MVP-2+)

- Local LLM 연동(optional)을 통한 SQL/VBA 초안 생성(8단계/표준 포맷)
- 규정/NCR RAG (권한통제형 placeholder → 운영환경 내 승인 적재)
- Excel 2021 리포트 자동 생성(README/RAW_DATA/DATA_PROFILE/VALIDATION/SUMMARY/LIMIT_MONITORING/EXCEPTION_LIST/SQL_USED/CHANGE_LOG/AI_COMMENTARY)
- 승인형 피드백 학습(검토자 승인 → 팀 표준 예제 승격)

> 관련 문서: `docs/26_Codex_MVP1_Implementation_Guide.md`, `prompts/codex_mvp1_implementation_prompt.md`, `docs/12_Test_Strategy.md`, `docs/13_Module_Backlog.md`
>
> 목표 추진(Goal) 모드로 진행 시: `prompts/codex_goal_mode_prompt.md`(시작 프롬프트), `docs/31_Codex_Goal_Mode_Worklog.md`(상태 원장·결정 핀다운·Git 핸드백). 결정 D-01~D-07이 백로그의 미해소 모호점을 고정한다.
