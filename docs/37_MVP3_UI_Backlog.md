# 37. MVP-3 Backlog — Review UI Screens (Dashboard·한도모니터링·History·Settings·Feedback)

> v0.3.0(MVP-1+2) 이후 다음 마일스톤. PR #12로 좌측 메뉴는 배선됐고, 이제 **stub 화면을 실구현**한다.
> 모든 신규 화면은 **review-only·오프라인·외부 NuGet 0** 원칙을 유지한다. (출처: `docs/10_Roadmap.md`, `docs/30_Demo_Scenario_Limit_Monitoring.md`)
> 핸드백: 본 문서 §상태원장 + §Resume Brief를 Codex가 갱신, Claude가 읽고 검증. 시작 프롬프트 `prompts/codex_mvp3_ui_prompt.md`.

---

## 목적 / 범위 / 제외

- **목적**: 좌측 메뉴가 가리키는 화면(Dashboard / Risk Dashboard·한도모니터링 / History / Settings / Feedback Center)을 실제 동작 화면으로 구현하고, 내비게이션을 견고화한다.
- **범위**: WPF UI 화면 + 이를 받치는 Core 읽기전용 조회/집계 로직 + SmokeTest.
- **제외(절대 금지)**: 외부/클라우드 API, 자동 업데이트, telemetry, SQL/VBA 자동 실행, 모델파일/실데이터/내부규정 원문 repo 포함, 런타임 정책 쓰기, 모델 재학습.

## 현재 UI 구조 (근거)

- 좌측 메뉴(10) → 가운데 `TabControl x:Name="MainTabs"`: `0:SQL 1:Draft 2:VBA 3:Excel 4:Data 5:Report 6:Regulation 7:Feedback`.
- PR #12: 메뉴 버튼에 Click 핸들러 배선(현 인덱스엔 정확하나 **하드코딩**). Dashboard/Risk Dashboard/History/Settings는 안내 finding만 표시(=stub).

---

## 결정 핀다운 (DU-01~DU-06) — 구현자는 그대로 따른다

| ID | 주제 | 결정 |
|---|---|---|
| **DU-01** | 화면 성격 | 모든 신규 화면은 **review-only / read-only**. 데이터 변형·자동실행·외부호출 없음. 사용자 입력은 조회·필터·승인(피드백)까지만. |
| **DU-02** | 차트/UI 라이브러리 | **외부 NuGet 0 유지.** 시각화는 WPF 인박스(`ItemsControl`/`DataGrid`/`Grid`/`ProgressBar`/막대 `Rectangle`)만. 실차트가 꼭 필요하면 **강행 말고 BLOCKED**(승인 게이트). |
| **DU-03** | History 뷰어 | `logs/task_log.jsonl`·`feedback_log.jsonl`를 **읽기 전용·경로 가드**로 로드. **해시/메타데이터만** 표시(원문 복원 금지). 누락/빈/손상 로그는 graceful(안내 finding). |
| **DU-04** | Settings | **뷰 전용.** 로드된 `SecurityPolicy`·fallback 상태·RuleVersion·오프라인/NoModel 상태 표시. **런타임 정책 쓰기 금지**(정책 변경은 `config/*.json` + 검토로만). |
| **DU-05** | Feedback 승격 | 기존 `ExamplePromotion`/`FeedbackLog*` 활용. **해시 전용·승인 게이트·모델 재학습 없음**(DM-04). 승인 동작은 audit log(해시) 1줄. |
| **DU-06** | 내비게이션 | 하드코딩 `SelectedIndex` → **견고한 매핑**(`TabItem` `x:Name` 또는 Header→index 런타임 사전). 메뉴 버튼→의도한 탭 **정확성 회귀 테스트** 추가(PR #12 nit 해소). |
| **공통** | — | NoModel/오프라인 기동 유지, 쓰기 경로는 `logs/`·`reports/`·`config/`만, 매 동작 audit log(해시), 게이트 A 통과. |

---

## 백로그 항목 (우선순위 순)

### U3-00. 내비게이션 견고화 + 회귀 테스트 (foundational)
- **처리**: 각 `TabItem`에 `x:Name` 부여(또는 Header→index 매핑), 메뉴 핸들러가 이름/키로 탭 선택. 
- **완료조건**: 탭 순서가 바뀌어도 메뉴 이동이 정확. 
- **테스트**: 각 메뉴 Content → 기대 탭 Header로 이동함을 검증(XAML 파싱 또는 매핑 단위검증).
- **예상파일**: `App/MainWindow.xaml(.cs)`, `tests/.../Program.cs`

### U3-01. Risk Dashboard / 한도 모니터링
- **목적**: 더미 노출/한도를 조인해 한도 대비 사용률·초과 여부를 review-only로 표시(docs/30 데모).
- **입력**: `samples/dummy_data/risk_exposure_sample.csv`, `risk_limit_sample.csv`
- **처리**: `PORTFOLIO_ID`(+`RISK_FACTOR`) 기준 조인, `EXPOSURE_AMT` vs `LIMIT_AMT` → 사용률%, 초과 플래그, 잔여한도. Core에 읽기전용 집계기.
- **출력**: UI 그리드/막대(인박스). (선택) Excel Report 탭과 연계 가능.
- **완료조건**: 더미 데이터로 한도 모니터링 표 정상 산출, 초과 항목 강조.
- **테스트**: 조인·사용률·초과 판정 단위검증(예: 한 항목 초과 케이스).
- **보안**: 읽기 전용, 실데이터 경로 하드코딩 금지(파라미터화), 외부 없음.
- **예상파일**: `Core/Risk/LimitMonitor.cs`(신규), `App/MainWindow.xaml(.cs)`, tests

### U3-02. History (감사로그 뷰어)
- **목적**: `logs/*.jsonl` 감사 이력을 read-only로 조회.
- **처리**: JSONL 파싱(경로 가드, 읽기 전용), TaskId/CreatedAt/TaskType/ToolType/SafetyResult/RuleVersion/해시앞자리 표시. 평문 복원 금지. 빈/누락 graceful.
- **완료조건**: 로그 존재 시 목록 표시, 없으면 안내.
- **테스트**: 더미 JSONL 1~2줄 → 파싱·표시 항목 검증, 원문 미노출 확인.
- **예상파일**: `Core/Logging/AuditLogReader.cs`(신규), `App/MainWindow.xaml(.cs)`, tests

### U3-03. Settings (정책 뷰어, read-only)
- **처리**: `PolicyLoader` 결과 + RuleVersion + 오프라인/NoModel 상태를 표로 표시. 편집 불가.
- **완료조건**: 정책 false/true·fallback·버전 표시. 쓰기 동작 없음.
- **테스트**: 정책 뷰 모델 구성 단위검증.
- **예상파일**: `App/MainWindow.xaml(.cs)`, (필요 시) `Core/Config` 읽기 헬퍼, tests

### U3-04. Feedback Center
- **처리**: `FeedbackLog`/`ExamplePromotion` 위에 목록·승인 UI. 승인 → 예제 KB 승격(해시·재학습 없음), audit log 기록.
- **완료조건**: 승인 흐름 동작, 미승인 보류, 평문 미저장.
- **테스트**: 승인→승격, 미승인→제외(기존 M2-05 테스트 확장).
- **예상파일**: `App/MainWindow.xaml(.cs)`, `Core/Feedback/*`(활용), tests

### U3-05. Dashboard (home/landing)
- **처리**: 앱 상태 요약 — 오프라인/NoModel, 정책 상태, RuleVersion, 로그 건수, 빠른 링크. 안내 finding 대체.
- **완료조건**: 기동 직후 상태 한눈에. 외부 호출 없음.
- **예상파일**: `App/MainWindow.xaml(.cs)`, tests

### U3-06. SmokeTest 확장
- **완료조건**: U3-00~05 회귀 추가, 기존 + 신규 전부 PASS, 모델/인터넷 없이 기동.

---

## MVP-3 전체 완료 조건 (DoD)

- [ ] `dotnet build RiskManagementAI.sln` 성공, SmokeTest 전부 PASS
- [ ] 좌측 메뉴 5개 stub 화면이 실제 동작(silent no-op 0)
- [ ] 내비게이션이 탭 순서 변화에 견고 + 정확성 테스트
- [ ] 모델/인터넷 없이 기동, 외부 NuGet 0, Interop/OpenXML 없음
- [ ] 쓰기 경로 `logs/`·`reports/`·`config/`만, 동작 시 audit log(해시)
- [ ] 정책 런타임 쓰기 없음, 내부규정/실데이터/모델파일 0, 게이트 A 0건

---

## 상태 원장 (Codex 갱신) · ★ Claude Resume Brief

> Codex는 항목 완료 시 상태/커밋/SmokeTest를 갱신하고, 아래 Resume Brief를 최신으로 유지한다.

### ★ Claude Resume Brief
- **현재 상태(1줄)**: _아직 시작 안 함_
- **main 최신 commit**: `git fetch origin main && git rev-parse origin/main`
- **DONE(검증됨)**: _-_
- **NEXT UP**: U3-00 내비게이션 견고화
- **BLOCKED**: _0_
- **재현 검증**: `git fetch origin main && git switch main && dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests`
- **⚠️ Claude 확인 요망**: _-_

### 진행 원장
| ID | 항목 | 상태 | 커밋 | SmokeTest | 비고 |
|---|---|---|---|---|---|
| U3-00 | 내비게이션 견고화+테스트 | TODO | - | - | PR #12 nit |
| U3-01 | Risk Dashboard/한도모니터링 | TODO | - | - | docs/30 |
| U3-02 | History 감사로그 뷰어 | TODO | - | - | read-only |
| U3-03 | Settings 정책 뷰어 | TODO | - | - | view-only |
| U3-04 | Feedback Center | TODO | - | - | 승격(재학습X) |
| U3-05 | Dashboard home | TODO | - | - | 상태 요약 |
| U3-06 | SmokeTest 확장 | TODO | - | - | 회귀 |

### BLOCKED 큐 / 자동 결정 로그
_(아직 없음 — Hard-block은 여기에, Soft-block 자동결정은 ⚠️로 기록)_

> 관련: `prompts/codex_mvp3_ui_prompt.md`, `docs/30_Demo_Scenario_Limit_Monitoring.md`, `docs/14_UI_Screen_Spec.md`, `docs/32_Branch_Governance.md`, `docs/28_Security_Review_Checklist.md`, `docs/33`(MVP-2 백로그 패턴), `docs/36`(워크로그 패턴)
