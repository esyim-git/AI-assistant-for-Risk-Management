---
name: risk-doc-truth-sync
description: Align README, CLAUDE.md, AGENTS.md, roadmap, gates, release notes, and work package docs with the current implementation. Auto-applied in Claude's planning/post-merge Preflight (CLAUDE.md §13).
disable-model-invocation: true
allowed-tools: Read Grep Glob Edit Bash(git status *) Bash(git log *) Bash(git diff *)
---

# Documentation Truth-Sync

## 목적
문서가 실제 코드/머지 상태를 정확히 반영하고, 정본 상태 어휘만 쓰며, 능력을 과대표기하지 않도록 `docs/38·39·40·48`(현재 Gate B/C 정본, `docs/44/45`는 historical)을 정합화한다(증거 기반 최소 수정).

## 언제 사용
- WP/PR 머지 후 Roadmap·WP Backlog·ADR·Gate 증거 문서를 갱신할 때.
- "문서가 코드와 안 맞는다 / 드리프트 의심 / 상태 표기가 과장된 것 같다"는 요청.
- "VERIFIED인데 증거가 없다", "Gate PASS 근거가 뭐냐" 등 상태 검증 요청.
- 작업은 `planning/*` 브랜치에서 한다. **Claude는 main을 직접 수정/병합하지 않는다**(`CLAUDE.md §11.1`).

## 절대 원칙
- 상태는 정본 7개 어휘만: `VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED` (`CLAUDE.md §11.4`). 정의·규칙은 [status-vocabulary.md](status-vocabulary.md).
- **과대표기 금지**: 실제 AI/RAG/NCR/Local LLM 능력을 실제보다 크게 적지 않는다.
- **증거 없으면 PASS/VERIFIED 금지**: 실 오프라인 Test PC 증거 없는 Gate는 `BLOCKED` 유지(현재 정본 `docs/48`, historical `docs/44/45`). 머지 Gate는 로컬 build/SmokeTest+리뷰와 활성 hosted `test`/`wpf-build`를 독립 증거로 요구하며 서로 대체하지 않는다(`CLAUDE.md §11`).
- **민감정보 금지**: 실데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret/토큰·모델파일·외부 NuGet/다운로드 지침을 문서에 추가하지 않는다. 필요 시 PATH로만 참조한다(예시는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 같은 더미만).
- **기준선 이중 표기 규칙**: 문서 기준선은 **코드/테스트 baseline SHA**(마지막 코드/테스트 머지)로 표기하고, current main과 구분한다. **docs-only 머지는 baseline SHA를 올리지 않는다(관례)** — 이 관례에 따른 표기 차이는 drift로 정정하지 않는다. 릴리스 문서에는 컷 기준(current main)과 binary-impact 기준선을 함께 기록한다.
- **시점 인용 주석 규칙**: 역사적 수치(과거 `Total=N`·과거 SHA)를 증거로 인용할 때는 시점을 명시한다(예: "`Total=572`(STAB-WP-03b #61 시점)") — "현재 수치"로 오독되지 않게 한다.
- 문서는 코드 동작을 바꾸지 않는다. `CLAUDE.md`/`AGENTS.md`/`docs/38-48`과 모순되면 안 된다.

## 절차
1. **범위 파악**: `git log`로 직전 머지/변경 범위(커밋 SHA·PR·WP-ID·테스트 총수 변화)를 확인한다.
2. **영향 항목 식별**: `docs/38`(Roadmap·Capability·Traceability) · `docs/39`(WP Backlog·Resume Brief·진행 원장) · `docs/40`(ADR) · `docs/48`(Gate B/C 증거 — 현재 정본; `docs/44/45`는 historical)에서 영향받는 항목을 Grep으로 찾는다.
3. **증거 대조**: 각 항목을 실제 코드/테스트 증거(커밋 SHA, SmokeTest `Total=N`, ADR 결정)와 대조한다. 어휘별 충족 기준은 [status-vocabulary.md](status-vocabulary.md), 문서별 점검 항목은 [truth-sync-checklist.md](truth-sync-checklist.md).
4. **상태 표기 검증**: 증거가 어휘 정의를 만족하는지 확인한다. 불충족이면 더 약한 어휘(예: `VERIFIED`→`PARTIAL`/`SCAFFOLD_ONLY`/`BLOCKED`)로 정정한다.
5. **최소 수정**: `Edit`로 해당 줄만 정합화하고, 각 상태 표기 옆에 **근거(커밋 SHA / 테스트 `Total=N` / Gate 증거 문서 PATH)**를 명시한다. 기존 테스트·기존 정본 합계를 약화시키지 않는다.
6. **교차 정합**: 같은 사실이 여러 문서에 있으면(예: SmokeTest 합계, 기준선 SHA) 한 정본으로 일치시키고 나머지는 그 정본을 참조한다.

## 산출물/보고
- 정합화된 `docs/*` diff(planning 브랜치 기준, 최소 수정).
- 각 상태 표기마다 증거: **커밋 SHA** 또는 SmokeTest **`Total=N PASS / 0 FAIL`** 또는 Gate 증거 문서 PATH.
- 정정 요약: `<문서 §항목>: <기존 어휘> → <정정 어휘> (근거: <SHA/Total/문서PATH>)` 형태의 줄 목록.
- 증거 없는 PASS/VERIFIED는 보고서에 절대 포함하지 않는다(해당 항목은 `BLOCKED`로 보고).

## 체크리스트
문서별(38/39/40/48, historical 44/45) 정합 점검 항목은 [truth-sync-checklist.md](truth-sync-checklist.md). 상태 어휘 정의·사용 규칙·과대표기 금지 예시는 [status-vocabulary.md](status-vocabulary.md).

## 참조
- `CLAUDE.md §11.1`(Truth Sync 책임) · `§11.4`(상태 어휘·과대표기 금지) · `§11.6`(Local-Gate).
- `docs/38`(Master Roadmap·Capability·Traceability) · `docs/39`(Work Package Backlog) · `docs/40`(ADR) · `docs/48`(Gate B/C current 증거·런북) · `docs/44/45`(historical Gate B/C 증거).
- 연계 스킬: `/risk-status-sync`(기준선·Resume Brief 갱신), `/risk-codex-review`(Codex 결과 Diff·테스트 검토 후 본 스킬로 문서 정합).
