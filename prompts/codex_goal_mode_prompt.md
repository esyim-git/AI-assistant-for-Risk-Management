# Codex MVP-1 Goal-Mode Prompt (목표 추진)

> 이 프롬프트는 Codex를 **목표 추진(Goal) 모드**로 로컬 Dev PC에서 실행하기 위한 시작 프롬프트다.
> 기존 `prompts/codex_mvp1_implementation_prompt.md`의 상위 버전이며, **자율 추진 루프 + Git Sync 핸드백**을 포함한다.

---

## 0. 너의 역할 / 모드

너는 이 프로젝트의 **Implementation Engineer / Test Engineer (Codex)** 다.
이번에는 **목표 추진(Goal) 모드**로 동작한다:

- 아래 "목표"를 달성할 때까지 **B-01 → B-09를 순서대로 자율 추진**한다. 매 단계마다 사람의 승인을 기다리지 않는다.
- 단, **"즉시 중단·보고(STOP)" 트리거**(§5)에 해당하면 추진을 멈추고 보고한다.
- 모든 추진 결과는 **`docs/31_Codex_Goal_Mode_Worklog.md`에 기록**하고 Git에 push하여, Claude(웹)가 동기화로 인지할 수 있게 한다. **기록 없는 완료는 미완료로 간주한다.**

## 1. 목표 (Definition of Done)

`docs/21_Implementation_Backlog.md`의 "MVP-1 전체 완료 조건"을 모두 충족한다:

- `dotnet build` 성공(외부 NuGet 최소/없음), SmokeTest 전부 PASS(실패 시 exit 1)
- SQL DELETE/UPDATE/DROP 차단, 정상 SELECT 통과(Blocker 0)
- VBA Shell/WScript/Kill 탐지, Option Explicit 누락 경고
- Excel VSTACK/HSTACK/TEXTSPLIT/MAP/REDUCE/BYROW/BYCOL 탐지
- 룰이 `rules/*.txt`에서 로드됨(RuleLoader)
- 더미 CSV 프로파일링 동작
- TaskLog/FeedbackLog가 **해시 기반**으로 기록됨
- Local LLM 없이 / 인터넷 없이 앱 실행 가능

## 2. 반드시 먼저 읽을 파일 (순서대로)

1. `AGENTS.md` — 구현 헌법(최우선)
2. `CLAUDE.md` — 절대 원칙, SQL/VBA/Excel 기준
3. `README.md` — 환경 분리/배포 모델
4. `docs/21_Implementation_Backlog.md` — 작업 항목 B-01~B-09 + 현재 상태표
5. `docs/26_Codex_MVP1_Implementation_Guide.md` — 구현 절차
6. **`docs/31_Codex_Goal_Mode_Worklog.md` — 상태 원장 / 결정 핀다운(D-01~D-07) / 핸드백 규약**
7. `config/security_policy.json`, `rules/*.txt`

## 3. 이미 구현된 것 — 재구현 금지

스타터 v2에는 **룰 엔진 3종 + 모델 + UI + SmokeTest가 이미 구현**되어 있다
(`SqlSafetyChecker` / `VbaSafetyChecker` / `Excel2021FunctionChecker` / `SafetyFinding` / `SafetySeverity` / `RulePattern` / `TaskLogEntry` / `FeedbackLogEntry` / `MainWindow` / `SmokeTests`).
**재구현하지 말고** 미구현 갭(RuleLoader/DataProfiler/LogWriter/PolicyLoader)과 검증/보강에 집중한다.

## 4. 추진 순서 & 단위 루프

### 설정 단계 (S-0, 1회)
1. 베이스 브랜치 최신화 후 작업 브랜치 생성:
   ```bash
   git fetch origin
   git switch -c feature/mvp1-rule-engine origin/claude/blissful-feynman-yx1r0r
   ```
   (사용자가 `develop`로 병합해 두었다면 거기서 분기해도 된다.)
2. **D-03**: `RiskManagementAI.sln` 생성 후 3개 csproj 추가, 커밋.
3. **D-04**: `.github/workflows/ci.yml`의 `on.push`/`on.pull_request` 브랜치에 `feature/**` 추가, 커밋.
4. `dotnet build` + `dotnet run --project tests/RiskManagementAI.SmokeTests`로 기준선 PASS 확인.

### 항목 루프 (B-01 → B-09 순서)
각 백로그 항목에 대해 다음을 반복한다:

```text
계획(어떤 파일/어떤 변경) →
구현(작은 단위) →
dotnet build →
dotnet run --project tests/RiskManagementAI.SmokeTests (신규 케이스 추가) →
보안 게이트 A (docs/28: git add -A --dry-run + 금지어/주민번호/금지확장자 스캔) →
docs/31 §3 원장 갱신 + §4 완료보고 append →
commit (type 규칙) →
다음 항목
```

추진 우선순위: **B-01 RuleLoader**(최우선) → B-02~B-04 Checker 검증/보강 → B-05 DataProfiler → B-06 Log Writer → B-07 PolicyLoader → B-08 UI 보강 → B-09 SmokeTest 확장.

### 종료 단계
- 모든 항목 DONE + DoD 체크 완료 시:
  ```bash
  git push -u origin feature/mvp1-rule-engine
  ```
  (네트워크 실패 시에만 2s/4s/8s/16s 백오프로 최대 4회 재시도. 인증 실패는 재시도 말고 보고.)
- push 후 §6 최종 보고를 출력한다.

## 5. STOP — 즉시 중단·보고 트리거

아래는 자율 추진을 멈추고 **사용자/Claude에게 보고**한다(`docs/31`에 `BLOCKED` 기록):

- 새 **NuGet 추가**가 필요할 때 (사유 먼저 보고 → 승인 후 진행)
- `docs/31` 결정 핀다운(D-01~D-07)과 **상충**하는 설계가 필요할 때
- 백로그/문서로 **해소되지 않는 모호성**이 있을 때
- 아래 **금지 사항** 중 하나라도 요구될 때:
  - 실제 Golden6 자동 접속/실행, 운영 DB 접속 문자열
  - VBA 자동 실행, 외부 API / 자동 업데이트 / telemetry
  - 모델 가중치/회사 실데이터/내부규정 원문 생성·커밋
  - 보안 게이트 A 위반(시크릿/주민번호/금지확장자 탐지)
  - `git push --force`, `git reset --hard`, `main` 직접 덮어쓰기

## 6. 구현 규칙

- C# `nullable enable` 유지. 외부 NuGet **최소화**(추가 전 STOP·보고).
- 모든 위험 검사 결과 = 코드/심각도/메시지/매칭문자열/위치 포함.
- 쓰기 경로는 `logs/`, `reports/`, `config/`만. 파일 경로는 **상대경로**, 외부 임의 경로 금지.
- 로그에 **민감정보 평문 저장 금지(해시만)** — D-07.
- 작은 단위 커밋, 각 단위마다 build/SmokeTest PASS 확인.
- 결정 핀다운(D-01~D-07)을 그대로 적용한다.

## 7. 최종 보고 형식 (push 후 출력)

1. 구현한 백로그 항목(B-xx)과 변경 파일 목록
2. `dotnet build` / SmokeTest 결과(콘솔 출력 포함)
3. 추가한 NuGet(있다면)과 사유
4. 보안 게이트 A 결과(0건 확인)
5. push 결과: branch명 / 최신 commit hash / remote URL
6. 남은 작업/리스크 + 다음 권장 단위
7. **`docs/31` 갱신 완료 확인** (Claude가 fetch로 인지 가능)

> 함께 보는 문서: `docs/31`(상태 원장·결정·핸드백), `docs/21`(백로그 상세), `docs/26`(절차),
> `docs/28`(보안 게이트), `docs/29`(Sync), `docs/06`(브랜치/커밋 규칙).
