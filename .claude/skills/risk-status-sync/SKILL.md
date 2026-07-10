---
name: risk-status-sync
description: Sync current project status, VERSION, roadmap, SmokeTest baseline, gates, and NEXT UP before planning the next work. Auto-applied first in Claude's planning Preflight (CLAUDE.md §13).
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git status *) Bash(git log *) Bash(git diff *) Bash(dotnet build *) Bash(dotnet run *)
---

# Project Status Sync

## 목적
`docs/38` Roadmap · `docs/39` Resume Brief(기준선 SHA · NEXT UP · SmokeTest Total) · Traceability Matrix가 실제 main(HEAD SHA · 머지된 WP · 정본 SmokeTest 합계)과 일치하는지 대조하고, 불일치를 정리해 보고한다. 코드/문서를 직접 수정하지 않고 **현황 진단만** 한다.

## 언제 사용
- "현재 상태 / 어디까지 했나 / 다음 뭐 / status / where are we / NEXT UP" 류 질문.
- PR 머지 직후 또는 새 세션 시작 시 기준선 재확인.
- Roadmap / Resume Brief / Traceability가 실제와 어긋난 것 같을 때.
- 수정이 필요하면 직접 고치지 말고 [/risk-doc-truth-sync](../risk-doc-truth-sync/SKILL.md)로 넘긴다.

## 절대 원칙
- 이 스킬은 **읽기·진단 전용**이다. `main`을 수정/병합하지 않는다(`CLAUDE.md §11.1`). 문서 수정은 truth-sync 담당.
- 상태 어휘는 정본만 사용: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED (`CLAUDE.md §11.4`). **과대표기 금지.**
- 실 오프라인 Test PC 증거가 없으면 Gate를 PASS로 적지 않는다. 증거 없으면 BLOCKED 유지(`docs/38 §7`, 현재 `docs/54`, historical `docs/48/45/44`).
- SmokeTest 정본 합계는 `Total=` 라인을 단일 근거로 본다(`docs/39` 재현 검증 절차). 임의 추정치 금지.
- **기준선 이중 표기 규칙**: **Current main SHA**(git HEAD)와 **코드/테스트 baseline SHA**(마지막 코드/테스트 머지)를 항상 구분한다. **문서 전용(docs-only) 머지는 baseline SHA를 올리지 않는다(관례)** — 문서의 baseline 표기가 current main과 다른 것 자체는 drift가 아니며, 코드/테스트를 건드린 머지가 baseline에 미반영일 때만 drift다. 릴리스 컷은 current main 기준으로 수행하되 binary-impact 기준선을 함께 기록한다(`/risk-release-cut`).
- 보고에 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보를 넣지 않는다(`AGENTS.md §3`). 예시는 더미명(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`)만.

## 절차
1. **실제 main 확인**: `git log --oneline -10` 으로 HEAD SHA · 최근 머지 PR(`(#N)`) 확인, `git status --short` 으로 미커밋/브랜치 상태 확인. 기준선 비교는 main 기준(계획 브랜치는 `planning/*`).
2. **Resume Brief 대조**: `docs/39`의 ★ Resume Brief에서 `현재 기준선` SHA · VERSION · `NEXT UP`(WP 1개) · SmokeTest `Total=` 을 읽어 1번 실제값과 비교.
3. **Roadmap 대조**: `docs/38 §0`(기준선·정본 SmokeTest 합계) · `§2` Release Train 상태(R1 DONE / R3 DONE / STAB / PILOT BLOCKED / R2…)가 실제 머지 이력과 맞는지 확인.
4. **Traceability 점검**: `docs/38 §5` Capability↔WP↔Test↔Gate(Cap-ID) 상태가 머지된 WP를 반영하는지, 과대표기(증거 없는 PASS/VERIFIED) 없는지 식별.
5. **불일치 정리**: 항목별 `문서 위치 → 현재값 vs 실제값 → 제안` 으로 목록화. 수정은 직접 하지 말고 [/risk-doc-truth-sync](../risk-doc-truth-sync/SKILL.md)에 위임한다.
6. **NEXT UP 확정**: Resume Brief 기준 NEXT UP 1개를 명시. 구현 착수 시 [/risk-wp-planner](../risk-wp-planner/SKILL.md)로 연결.

## 산출물/보고
다음 형태로 보고한다(문서 미수정, 진단만):

```text
Current main       : <SHA> (git HEAD)
코드/테스트 baseline : <SHA> (마지막 코드/테스트 머지 — docs-only 머지는 baseline 미변경)
VERSION            : <x.y.z>
SmokeTest          : Total=<N> PASS / 0 FAIL  (근거: docs/39 재현 검증)
머지된 WP          : <WP-ID ...> (최근 PR #N)
NEXT UP            : <WP-ID 1개>
불일치             : - <문서:위치> 현재=<...> / 실제=<...> → 제안
                     (없으면 "불일치 없음 (truth-sync 불필요)")
```

## 체크리스트
점검 항목(기준선 / Release Train / Traceability / NEXT UP / 과대표기)은 [status-sync-checklist.md](status-sync-checklist.md) 참조.

## 참조
- `docs/38_v1_Master_Roadmap.md` (§0 기준선 · §2 Release Train · §5 Traceability · §7 게이트)
- `docs/39_Work_Package_Backlog.md` (★ Resume Brief — 기준선 SHA · NEXT UP · SmokeTest 재현)
- `CLAUDE.md §11`(Claude↔Codex Workflow · §11.4 상태 어휘 · §11.6 Local-Gate) · `AGENTS.md §0`(현재 기준선)
- 관련 스킬: [/risk-doc-truth-sync](../risk-doc-truth-sync/SKILL.md)(불일치 실제 수정) · [/risk-wp-planner](../risk-wp-planner/SKILL.md)(NEXT UP 구현 착수)
