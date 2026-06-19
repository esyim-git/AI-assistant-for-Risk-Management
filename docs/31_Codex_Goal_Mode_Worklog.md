# 31. Codex Goal-Mode Worklog & Handback Protocol

> 이 문서는 **Codex(목표 추진/Goal 모드)** 가 MVP-1을 로컬에서 추진·완료한 결과를,
> **Git Sync를 통해 Claude Code(웹)가 그대로 인지**할 수 있도록 만드는 **단일 상태 원장(Single Source of Truth)** 이다.
> Codex는 이 문서를 **작업과 동시에 갱신**하고, Claude는 Git에서 이 문서를 읽어 진행 상태를 파악한다.

---

## 목적

- Codex 로컬 작업 ↔ Claude(웹) 사이의 **상태 공유를 Git 문서 하나로 동기화**한다.
- "무엇을, 어디까지, 어떤 커밋으로, 어떤 검증 결과로 완료했는가"를 사람·에이전트가 모두 읽을 수 있게 남긴다.
- 모든 결정/완료가 **감사 가능(auditable)** 하도록 한다. (AGENTS.md 우선순위 2: 감사 가능성)

## 적용 범위

- Dev 환경에서 Codex가 수행하는 MVP-1 백로그(B-01~B-09) 구현/보강.
- 본 문서의 상태 원장·완료 보고·결정 핀다운.

## 제외 범위

- 실제 코드 구현 절차(→ `docs/26`), 백로그 상세(→ `docs/21`).
- 운영환경(Prod)은 Git에 직접 접속하지 않는다(→ `docs/25`, `docs/29`).

---

## 1. 핸드백 루프 (Codex → Git → Claude)

```text
        [Codex / 로컬 Dev PC]                       [Claude Code / 웹]
        ─────────────────────                       ──────────────────
 1. 베이스 브랜치 pull (docs/31 포함분)
 2. B-xx 구현 (작은 단위)
 3. dotnet build + SmokeTest                ┐
 4. 보안 게이트 A (docs/28)                  │ 단위마다 반복
 5. 이 문서(§3 원장, §4 보고) 갱신           │
 6. commit (type 규칙)                       ┘
 7. git push -u origin feature/mvp1-rule-engine ──────►  8. git fetch origin feature/mvp1-rule-engine
                                                         9. docs/31 읽기 → 진행 상태 파악
                                                        10. (선택) 빌드/SmokeTest 재현 검증
                                                        11. 다음 지시 / 리뷰 / 병합 판단
```

핵심: **이 문서가 두 에이전트의 공유 메모리**다. Codex가 §3/§4를 갱신하지 않으면 핸드백은 실패로 본다.

---

## 2. 동기화·브랜치 규약 (docs/06 · docs/29 정합)

| 항목 | 값 |
|---|---|
| Repository | `https://github.com/esyim-git/AI-assistant-for-Risk-Management` |
| Codex 베이스 브랜치 | 이 문서(`docs/31`)와 `prompts/codex_goal_mode_prompt.md`가 포함된 커밋. 기본값 `claude/blissful-feynman-yx1r0r` (또는 사용자가 `develop`로 병합 후 거기서 분기) |
| Codex 작업 브랜치 | `feature/mvp1-rule-engine` (docs/06 컨벤션) |
| Claude 인수 명령 | `git fetch origin feature/mvp1-rule-engine` → `docs/31` 확인 |
| 커밋 타입 | `feat` / `fix` / `docs` / `chore` / `test` / `refactor` / `security` |
| Push 정책 | branch push만, `main` 직접 push 금지, `--force`/`reset --hard` 금지 (docs/29) |
| 커밋 전 필수 | 보안 게이트 A (docs/28) 통과 |

> CI 주의: 현재 `.github/workflows/ci.yml`은 `main`/`develop`에만 트리거된다. Codex 작업 브랜치에서 CI가 돌게 하려면 §5 결정 D-04(트리거에 `feature/**`+PR 추가)를 **설정 단계(S-0)에서 먼저** 반영한다.

---

## 3. 상태 원장 (Status Ledger)

> Codex는 각 항목을 완료할 때마다 **상태/커밋/변경파일/빌드/SmokeTest/비고**를 갱신한다.
> 상태값: `TODO`(미착수) · `WIP`(진행중) · `DONE`(완료·검증됨) · `BLOCKED`(중단·사유기재).

### 3.1 스타터 v2 기준 기존 구현 (재구현 금지)

| 영역 | 상태 |
|---|---|
| `SafetyFinding` / `SafetySeverity` / `RulePattern` | DONE (모델 존재) |
| `SqlSafetyChecker` / `VbaSafetyChecker` / `Excel2021FunctionChecker` | DONE (규칙 **하드코딩**) |
| `TaskLogEntry` / `FeedbackLogEntry` | DONE (모델만, 저장기 없음) |
| WPF `MainWindow` (3 Checker 호출) | DONE |
| SmokeTest 콘솔 | DONE (기본 케이스) |

### 3.2 MVP-1 백로그 진행 원장

| ID | 항목 | 시작상태 | 현재상태 | 커밋(short) | 핵심 변경파일 | build | SmokeTest | 비고 |
|---|---|---|---|---|---|---|---|---|
| S-0 | 설정(.sln 생성, ci 트리거 D-04) | TODO | DONE | d09dcdd | `RiskManagementAI.sln`, `.github/workflows/ci.yml`, `NuGet.Config` | PASS (0 warnings, 0 errors) | PASS (5 PASS / 0 FAIL) | repo-local NuGet config 추가(외부 package source 없음) |
| B-01 | RuleLoader (rules/*.txt 주입) | TODO | DONE | e452324 | `Safety/RuleLoader.cs`, `Safety/SafetyRuleSet.cs`, checker 3종, SmokeTest | PASS (0 warnings, 0 errors) | PASS (15 PASS / 0 FAIL) | D-01/D-02/D-05/D-06 반영 |
| B-02 | SqlSafetyChecker 검증/보강 | WIP* | TODO | - | `Safety/SqlSafetyChecker.cs` | - | - | *기존 구현 보강 |
| B-03 | VbaSafetyChecker 검증/보강 | WIP* | TODO | - | `Safety/VbaSafetyChecker.cs` | - | - | FollowHyperlink 등 흡수 |
| B-04 | Excel2021FunctionChecker 검증/보강 | WIP* | TODO | - | `Excel/Excel2021FunctionChecker.cs` | - | - | preferred=안내용(D-02) |
| B-05 | DataProfiler 구현 | TODO | TODO | - | `Data/DataProfiler.cs`(신규) | - | - | 더미 CSV 대상 |
| B-06 | TaskLog/FeedbackLog 저장기 | TODO | TODO | - | `Logging/*Writer.cs`(신규) | - | - | **해시만** 저장 |
| B-07 | PolicyLoader (security_policy.json) | TODO | TODO | - | `Config/PolicyLoader.cs`(신규) | - | - | 없으면 전부 false |
| B-08 | 최소 UI 보강 | WIP* | TODO | - | `App/MainWindow.xaml(.cs)` | - | - | 탭/심각도 색상 |
| B-09 | SmokeTest 확장 | WIP* | TODO | - | `tests/.../Program.cs` | - | - | 신규기능 회귀 |

> `WIP*` = 스타터에 기초 구현이 있어 "검증/보강" 성격임을 의미(신규 생성 아님).

### 3.3 전체 완료 조건 (Definition of Done) 체크

> Codex가 MVP-1 종료 시 아래를 모두 체크한다. (출처: docs/21)

- [ ] `dotnet build` 성공 (외부 NuGet 최소/없음)
- [ ] SmokeTest 전부 PASS (실패 시 exit 1)
- [ ] SQL DELETE/UPDATE/DROP 등 차단, 정상 SELECT 통과(Blocker 0)
- [ ] VBA Shell/WScript/Kill 탐지, Option Explicit 누락 경고
- [ ] Excel VSTACK/HSTACK/TEXTSPLIT/MAP/REDUCE/BYROW/BYCOL 탐지
- [ ] 룰이 `rules/*.txt`에서 로드됨 (RuleLoader)
- [ ] 더미 CSV 프로파일링 동작
- [ ] TaskLog/FeedbackLog가 **해시 기반**으로 기록됨
- [ ] Local LLM 없이 / 인터넷 없이 앱 실행 가능

---

## 4. 항목별 완료 보고 (Codex append-only)

> Codex는 각 백로그 항목 완료 시 아래 블록을 **복사하여 채워** 이 절 하단에 누적한다.
> (Claude는 이 절만 읽어도 "무엇이 어떻게 끝났는지"를 알 수 있어야 한다.)

### 보고 템플릿

```md
#### [B-xx] 제목 — DONE (YYYY-MM-DD)
- 구현 요약: (1~3줄)
- 변경 파일: path1, path2, ...
- 빌드 결과: dotnet build = 성공/실패 (요약)
- SmokeTest 결과: N PASS / M FAIL (신규 추가 케이스: ...)
- 보안 게이트 A: 통과(0건) / 사유
- NuGet 추가: 없음 / (있으면 패키지명 + 사유 + 승인경위)
- 결정/가정: (D-xx 적용 또는 신규 판단)
- 남은 리스크/후속: ...
- 커밋: <short-hash> "<message>"
```

### 완료 보고 누적

<!-- Codex는 이 줄 아래에 완료 보고를 추가한다. (아직 없음) -->

#### [S-0] 설정(.sln 생성, ci 트리거 D-04) — DONE (2026-06-19)
- 구현 요약: `RiskManagementAI.sln`을 생성하고 Core/App/SmokeTests 3개 프로젝트를 포함했다. CI push/pull_request 대상에 `feature/**`를 추가했다. sandbox-local 검증을 위해 외부 package source가 없는 repo-local `NuGet.Config`를 추가했다.
- 변경 파일: `RiskManagementAI.sln`, `.github/workflows/ci.yml`, `NuGet.Config`, `docs/31_Codex_Goal_Mode_Worklog.md`
- 빌드 결과: `dotnet build RiskManagementAI.sln --no-restore` = 성공 (0 warnings / 0 errors)
- SmokeTest 결과: 5 PASS / 0 FAIL (기준선 케이스)
- 보안 게이트 A: 통과(actionable 0건; 기존 정책/패키징 문구 false positive 확인)
- NuGet 추가: 없음
- 결정/가정: D-03, D-04 적용. Local Dev 검증용 .NET SDK 8.0.422는 user-local 설치이며 repo 산출물 아님.
- 남은 리스크/후속: B-01 RuleLoader부터 순서대로 진행.
- 커밋: d09dcdd "chore: set up MVP1 solution and CI trigger"

#### [B-01] RuleLoader (rules/*.txt 주입) — DONE (2026-06-19)
- 구현 요약: `rules/*.txt`를 로드하는 RuleLoader와 SafetyRuleSet을 추가하고, SQL/VBA/Excel checker가 룰셋 주입으로 동작하게 했다. `REQUIRE_PRESENT:`는 부재 시 경고로 처리하며, 룰 파일 누락/손상 시 내장 기본 룰셋으로 폴백하고 finding으로 표시한다.
- 변경 파일: `src/RiskManagementAI.Core/Safety/RuleLoader.cs`, `src/RiskManagementAI.Core/Safety/SafetyRuleSet.cs`, `src/RiskManagementAI.Core/Safety/SqlSafetyChecker.cs`, `src/RiskManagementAI.Core/Safety/VbaSafetyChecker.cs`, `src/RiskManagementAI.Core/Excel/Excel2021FunctionChecker.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/31_Codex_Goal_Mode_Worklog.md`
- 빌드 결과: `dotnet build RiskManagementAI.sln --no-restore` = 성공 (0 warnings / 0 errors)
- SmokeTest 결과: 15 PASS / 0 FAIL (신규 추가 케이스: RuleLoader repo rules 로드, RuleVersion, REQUIRE_PRESENT, 임시 룰 injection, fallback finding)
- 보안 게이트 A: 통과(actionable 0건; 기존 정책/패키징 문구 false positive 확인)
- NuGet 추가: 없음
- 결정/가정: D-01/D-02/D-05/D-06 적용. 외부 임의 경로 로드는 차단하고 relative app-local 디렉터리만 허용.
- 남은 리스크/후속: B-02에서 SQL deny/warn 커버리지와 transaction severity를 정식 보강.
- 커밋: e452324 "feat: load safety rules from rule files"

---

## 5. 결정 핀다운 (Decision Pins) — Codex는 그대로 따른다

> 백로그(docs/21)에서 열려 있던 모호점을 **권위 있는 결정으로 고정**한다.
> 변경이 필요하면 Codex는 임의로 바꾸지 말고 `BLOCKED`로 보고한다.

| ID | 주제 | 결정 |
|---|---|---|
| **D-01** | 룰 버전 식별자 (`TaskLogEntry.RuleVersion`) | 별도 `RULESET_VERSION` 파일을 두지 않는다. RuleLoader가 로드한 `rules/*.txt`들의 **내용을 파일명 정렬 후 연결한 SHA-256**으로 산출하고, `"ruleset-" + 앞 12자리(hex)` 형식을 RuleVersion으로 사용한다. (결정적·드리프트 없음) |
| **D-02** | `excel_2021_preferred_functions.txt` 성격 | **탐지 패턴이 아니라** 메시지/대체안 안내용 참조 목록이다. RuleLoader는 이 파일을 안내 텍스트로만 사용하고, **탐지는 `excel_2021_blocked_functions.txt`만** 사용한다. |
| **D-03** | 솔루션 파일 | Codex는 `RiskManagementAI.sln`을 생성하여 **repo에 커밋**한다(3개 csproj 포함). docs/26의 1회 생성 스텝을 산출물로 고정. |
| **D-04** | CI 트리거 | `.github/workflows/ci.yml`의 `on.push`/`on.pull_request` 브랜치에 `feature/**`(또는 작업 브랜치 패턴)를 추가하여, Codex 푸시 시 build+SmokeTest가 자동 검증되게 한다. (빌드·테스트만 — 외부통신/배포 없음) |
| **D-05** | `REQUIRE_PRESENT:` 패턴 해석 | `rules/*_warn_patterns.txt`의 `REQUIRE_PRESENT:<정규식>` 행은 "있어야 하는 요소"로 해석하여 **부재 시 경고**한다(존재 시 무경고). 예: VBA `Option Explicit`. |
| **D-06** | 룰 파일 폴백 | 룰 파일 부재/손상 시 **코드 내장 기본값**으로 안전 폴백하되, 폴백 사용 사실을 Finding 또는 로그로 표시한다. 외부 임의 경로 로드 금지(앱 상대경로만). |
| **D-07** | 로그 저장 형식 | TaskLog/FeedbackLog는 `logs/`에 **JSON Lines**(`*.jsonl`) append. 민감정보는 평문 금지, **해시만**(`RequestHash`/`OutputHash`). |

---

## 6. Claude 인수(Sync-back) 절차

Codex가 push한 뒤 Claude(웹)는 다음으로 상태를 인지한다.

```bash
git fetch origin feature/mvp1-rule-engine
git log --oneline origin/feature/mvp1-rule-engine -20
# 핵심: 이 문서(docs/31) §3 원장 + §4 완료보고 확인
# (선택) 재현 검증
dotnet build
dotnet run --project tests/RiskManagementAI.SmokeTests
```

Claude는 §3/§4를 기준으로 ① 완료 항목 검증 ② 보안 게이트 A 재확인 ③ 다음 작업 지시 또는 PR/병합 판단을 수행한다.

---

## 7. 보안·금지 리마인드 (위반 시 Codex 즉시 중단·보고)

- 실제 Golden6 자동 접속/실행, 운영 DB 접속 문자열, VBA 자동 실행 코드
- 외부 API / 자동 업데이트 / telemetry 코드
- 모델 가중치 파일, 회사 실데이터, 내부규정 원문의 생성·커밋
- 불필요한 NuGet 추가(추가 전 사유 보고·승인) — 본 문서 §4에 기록
- `git push --force`, `git reset --hard`, `main` 직접 덮어쓰기
- 로그에 원문/민감정보 평문 저장 (해시만)

> 관련 문서: `docs/21_Implementation_Backlog.md`, `docs/26_Codex_MVP1_Implementation_Guide.md`,
> `prompts/codex_goal_mode_prompt.md`, `docs/28_Security_Review_Checklist.md`, `docs/29_GitHub_Sync_Guide.md`, `docs/06_Development_Workflow.md`
