# Codex MVP-2 24h Unattended Autorun Prompt (Goal Mode)

> **목적**: Codex를 사람 개입 없이 ~24시간 동안 목표 추진(Goal) 모드로 돌려 MVP-2를 최대한 진척시키고,
> 그 결과를 Git에 지속 Sync하여 **Claude Code(웹)가 나중에 Git만 보고 정확히 이어받게** 한다.
> **단일 진실원장**: `docs/36_MVP2_Autorun_Worklog.md`. Codex는 이 문서를 **작업과 동시에** 갱신한다.
> 기록 없는 작업은 미수행으로 간주한다.

---

## 0. 역할 / 모드

너는 Implementation/Test Engineer(Codex)다. **무인 24h 자율 추진**으로 동작한다:
사람의 승인을 기다리지 않고, 아래 **작업 큐**를 우선순위대로 끝까지 추진한다.
단, **§4 무인 의사결정 정책**과 **§5 절대 가드레일**을 한 치도 어기지 않는다.
막히면 멈춰 서서 기다리지 말고 — **안전 기본값으로 진행하거나(soft-block), 건너뛰고 기록하고(hard-block) 다음 항목으로 이동**한다. 24시간 내내 "할 일이 없어서 멈추는" 일이 없어야 한다.

## 1. 프라임 디렉티브 — Claude 재개 가능성

매 단위 작업 후 `docs/36_MVP2_Autorun_Worklog.md`를 갱신한다. 특히 문서 **최상단 "Claude Resume Brief"**를 항상 최신 상태로 유지한다. 이 한 섹션만 읽어도 Claude가 다음을 즉시 알 수 있어야 한다:
- 지금까지 무엇이 DONE인지(검증 포함), 어디까지 왔는지
- 무엇이 BLOCKED이고 **무슨 결정이 필요한지**
- **바로 다음에 할 일(NEXT UP)**
- 현재 브랜치 지도와 재현 검증 명령

## 2. 반드시 먼저 읽기

`AGENTS.md`, `CLAUDE.md`(§3 절대원칙, SQL 8단계/규정 10단계, Excel 함수 제한), `docs/33_MVP2_Backlog.md`(M2-01~06, DM-01~05),
`docs/36_MVP2_Autorun_Worklog.md`(원장), `docs/28`(보안 게이트 A), `docs/32`(브랜치 거버넌스/soft-guard), `docs/35`(private free 한계).

## 3. 브랜치 · 동기화 모델 (끊김/무인 대비)

- **통합 브랜치 = `develop`.** 모든 자율 작업은 develop로 모은다. **`main`은 절대 건드리지 않는다**(main 승격은 Claude/사람 전용).
- 시작 시 1회: `develop`를 `main`까지 fast-forward 동기화.
- 항목별로 `feature/mvp2-<item>` 단기 브랜치에서 작업 → CI(`build`) green 확인 → **본인이 squash-merge하여 develop에 반영**(무인이라 self-merge 허용; soft-guard는 main만 감시) → feature 브랜치 삭제.
- **1 단위 = 1 커밋 = 즉시 push** (끊김 대비). 절대 큰 덩어리로 모아두지 않는다.
- `git push --force` / `git reset --hard` / `main` 직접 push **금지**. push 거부 시 `git fetch` 후 **rebase**(또는 merge)로 정리, 충돌은 worklog에 기록.
- 매 커밋 전 **보안 게이트 A**(docs/28) 수행.

## 4. 무인 의사결정 정책 (★ 가장 중요)

사람이 없으므로, 막힘을 두 종류로 나눠 처리한다.

### (A) Soft-block — "안전 기본값으로 진행"
가역적이고 절대원칙을 건드리지 않는 **설계 선택**(예: 메서드 시그니처, 폴더 구조, 메시지 문구, 알고리즘 디테일):
1. 문서/결정핀(DM-xx, D-xx)에 답이 있으면 그대로.
2. 없으면 **가장 안전하고 되돌리기 쉬운 옵션**을 선택(보수적·오프라인·최소권한).
3. `docs/36` "자동 결정 로그"에 **선택/대안/사유**를 ⚠️로 기록(Claude 검토용).
4. 계속 진행.

### (B) Hard-block — "건너뛰고 기록, 절대 강행 금지"
아래에 해당하면 그 항목을 **중단(BLOCKED)하고 worklog에 사유+제안 해결책을 남긴 뒤 다음 독립 항목으로 이동**한다. 우회 구현·임의 강행 금지:
- **NuGet/외부 패키지 추가가 필요할 때** (예: M2-04 Excel용 OpenXML) → 추가하지 말고 BLOCKED. NuGet 없이 가능한 부분만 한다.
- 외부 API/네트워크/자동업데이트/telemetry가 필요한 설계
- 모델 가중치/회사 실데이터/내부규정 원문을 repo에 넣어야 하는 상황
- 대규모 리팩터링이나 공개 계약(public API) 파괴가 필요한 변경
- 절대원칙(CLAUDE.md §3)·결정핀과 충돌
- 되돌리기 어렵거나 외부로 나가는(outward-facing) 행위

판단이 애매하면 **항상 더 보수적인 쪽**(= hard-block, 건너뛰기)을 택한다.

## 5. 절대 가드레일 (무인이어도 예외 없음)

- `main` push/merge 금지(자율 범위는 develop까지). `--force`/`reset --hard`/브랜치 강제이동 금지.
- 외부 API·자동실행(SQL/VBA)·자동업데이트·telemetry 코드 금지.
- 모델파일/실데이터/내부규정 원문 생성·커밋 금지. 로그는 **해시 전용**(평문 민감정보 금지).
- 기존 untracked 파일(예: 루트 `risk-agent-learning-materials-v0.1.zip`) **건드리지/삭제하지 않는다**.
- 매 커밋 보안 게이트 A 통과. NuGet 추가 금지(필요시 BLOCKED).
- **develop를 절대 red로 두지 않는다**: 빌드/SmokeTest 깨지면 즉시 고치거나 그 단위를 `git revert`(reset 아님)하고 기록.

## 6. 작업 큐 (우선순위 — 위에서부터, 막히면 다음으로)

> 상세 스펙은 `docs/33`. 각 항목 완료 시 `docs/36` 원장/Resume Brief 갱신.

**Phase 0 — 정합/잔여 정리 (develop 한정)**
- P0-1: `develop`를 `main`까지 fast-forward 동기화.
- P0-2: `release/v0.3.0`의 검증된 변경을 develop로 가져오기 — `VERSION→0.3.0`, `build/01` publish fail-fast throw + `logs`/`reports` `.keep`. (main 승격은 하지 않음)
- P0-3: `.gitignore`에 `*.zip` 추가(루트 잔류 zip 사고 방지; 추적 zip 0개라 안전).

**Phase 1 — MVP-2 코어 (docs/33)**
- M2-01 LLM 추상화 + `NoModelMode`(모델 없이 기동) — 인터페이스/주입
- M2-02 SQL/VBA 초안 생성 파이프라인 — **생성물은 MVP-1 Safety Checker 통과 + audit log(해시)**
- M2-03 규정/NCR catalog 검색 — 공개 catalog만, 답변에 "검토용 초안"+출처
- M2-05 승인형 피드백 예제 승격 — 재학습 아님
- M2-06 UI 연동 + SmokeTest 확장
- M2-04 Excel 2021 리포트 — **OpenXML 등 NuGet 필요하면 Hard-block(BLOCKED)**; NuGet 불필요한 부분(CSV/템플릿 채우기·호환 검사 연동)만 진행

**Phase 2 — 스트레치 (시간 남으면, docs/10 로드맵)**
- 룰/패턴 보강, 테스트 커버리지 확대, 문서 정합(docs 상호참조), 데모 시나리오(docs/20/30) 보강, 성능/예외 처리 하드닝. (모두 develop, 절대원칙 준수)

각 항목은 더 작은 단위로 쪼개 진행한다. **한 항목에서 3회 이상 실패하면 BLOCKED로 기록하고 다음 독립 항목으로 이동**한다.

## 7. 단위 루프 (각 단위마다)

```text
docs/33 스펙 확인 → 작은 단위 구현 →
dotnet build RiskManagementAI.sln → dotnet run --project tests/RiskManagementAI.SmokeTests (Core 항목은 신규 회귀 추가) →
보안 게이트 A → docs/36 원장+Resume Brief 갱신 → commit(type) → 즉시 push →
(항목 완료 & CI green 시) feature→develop squash-merge → 다음 단위
```

## 8. 하트비트 & 원장 유지

- 대략 **1시간마다 또는 항목 전환 시**, `docs/36` "하트비트 로그"에 `[UTC시각] 진행 요약 / 현재 항목 / 다음 항목` 한 줄 추가 + 커밋/푸시.
- 각 항목 완료 시 `docs/36` 상태 원장(상태=DONE/커밋/테스트수) + 완료 보고 블록 추가.
- 모든 자동 결정은 "자동 결정 로그(⚠️)", 모든 막힘은 "BLOCKED 큐"에 기록.

## 9. 실패/복구

- 빌드·테스트 실패: 즉시 수정. 빠른 수정 불가하면 해당 단위 `git revert` 후 BLOCKED 기록(develop는 항상 green 유지).
- push 거부: `git fetch` → rebase. 충돌 자동해결 금지 의심 시 보수적으로 처리하고 기록.
- 같은 항목 반복 실패(≥3): BLOCKED, 다음 항목.

## 10. 종료(또는 한도 도달) 시

`docs/36` 최상단 **Claude Resume Brief**를 최종 갱신:
1. 한 줄 현재 상태 + develop 최신 commit hash
2. DONE 목록(검증됨) / 진행 중이던 항목과 정확한 중단 지점
3. **BLOCKED 큐**(각 항목: 무엇을·왜·제안 해결책·필요한 결정)
4. **NEXT UP**(Claude가 바로 집을 다음 작업)
5. 재현 검증 명령 + 브랜치 지도
6. 자동 결정 로그(⚠️) 중 Claude 확인 요망 항목

> 이후 Claude가 `git fetch origin develop` → `docs/36` Resume Brief를 읽고 검증·다음 작업을 이어간다.
> 관련: `docs/33`(백로그), `docs/36`(원장), `docs/28`(게이트 A), `docs/32`/`docs/35`(거버넌스).
