# 49. Project Skills 운영 가이드 (Local Codex)

## 목적 / 범위

Local Codex가 계획·구현·검증·PR·독립 리뷰·머지·정리까지 반복 작업을 일관되게 수행하도록 하는 Project Skills 운영 가이드다. 정본 우선순위는 root `AGENTS.md` → 지정 WP → 대상 Prompt → native lifecycle Skill → 관련 domain checklist다.

## 1. 구조

```text
.agents/skills/risk-local-codex-lifecycle/
    SKILL.md                               # Codex 자동 발견 lifecycle entry
    agents/openai.yaml                    # UI metadata
SKILLS.md                                 # native + transitional catalog index
.claude/skills/<skill-name>/
    SKILL.md                               # transitional domain checklist
    <topic>.md                             # 긴 checklist/support file(선택)
AGENTS.md                                 # current actor, boundary, merge authority
CLAUDE.md                                 # 기존 링크 호환용 guide; active actor 정본 아님
prompts/codex/README_skills_usage.md       # Codex 실행 요약
```

- `.agents/skills/`가 Local Codex의 native discovery surface다.
- 기존 19개 `.claude/skills/`는 한 번에 복제하지 않고 transitional domain checklist로 재사용한다.
- transitional checklist의 기술·보안·테스트 기준은 유지한다. Claude actor 또는 Claude review를 요구하는 legacy 문구는 `AGENTS.md`, native lifecycle Skill, `docs/32`의 현재 규칙으로 대체 해석한다.
- Skill 적용은 Gate PASS가 아니다. SHA·테스트·보안·승인 증거가 별도로 필요하다.

## 2. Native Lifecycle Skill

`risk-local-codex-lifecycle`은 다음 lifecycle을 라우팅한다.

```text
evidence freeze
-> clean worktree / bounded branch
-> one-goal implementation
-> local gate + Gate A
-> Draft PR
-> separate Local Codex exact-head review
-> hosted required checks
-> squash merge
-> truth-sync + cleanup
```

중요 PR은 작성 task/context와 다른 Local Codex task/context가 최종 diff와 exact head를 읽고 PR에 아래 증거를 남긴다.

```text
independent_review_verdict: pass|changes_required
reviewed_head: <full SHA>
```

이는 현재 단일 GitHub 계정에서 불가능한 counted self-approval을 흉내 내는 것이 아니라 내부 독립 검토 증거다. GitHub numeric approval은 별도 approval-capable actor가 생길 때까지 0을 유지한다.

## 3. Transitional Domain Catalog (19)

| 단계 | Checklist |
|---|---|
| 저장소 종합 감사 | `risk-repo-audit` |
| 상태/문서/계획 | `risk-status-sync` → `risk-doc-truth-sync` → `risk-wp-planner` |
| 구현 | 대상 domain checklist + `risk-security-guard` |
| 테스트 | `risk-smoke-governance` |
| 리뷰 | `risk-codex-review` + domain checklist + `risk-security-guard` |
| 브랜치/머지 | `risk-branch-governance` |
| 릴리스 | `risk-release-cut` → `risk-release-verify` → `risk-gate-bc` |
| 파일럿 | `risk-team-pilot` |
| Local model/runtime | `risk-llm-approval` → explicit approval 전 STOP |

도메인 checklist 전체 목록과 분류는 root `SKILLS.md`가 정본이다.

## 4. 실행 원칙

1. 사용자가 Skill 이름을 지정하지 않아도 material work 시작 시 native lifecycle Skill을 적용한다.
2. 대상 작업에 필요한 transitional checklist만 읽는다. 긴 support file은 해당 `SKILL.md`가 직접 지시할 때만 읽는다.
3. dirty checkout의 사용자 변경을 보존하고, 구현/PR 작업은 live `origin/main`에서 만든 clean non-temporary worktree를 우선한다.
4. 한 PR은 측정 가능한 목표 하나만 가진다. cross-repo 또는 범위 밖 발견은 handoff/backlog로 남긴다.
5. completion report에 `Applied Skills/Checklists`, exact SHA, tests, Gate A, hosted checks, independent review, cleanup을 기록한다.

## 5. CI와 머지

- 로컬 Windows Release build/SmokeTest/Gate A는 항상 필요하다.
- hosted `test`/`wpf-build`는 protected branch의 독립 second gate다.
- Actions quota/가용성 문제 시 가장 가까운 local equivalent를 실행하고 `local-fallback-only`로 기록한다.
- local fallback은 hosted green 또는 required check 충족으로 표현하지 않는다. required checks가 성공하기 전에는 merge를 계속 차단한다.
- merge는 exact-head 재확인, unresolved conversation 0, independent verdict `pass`, authorization을 모두 만족한 뒤 squash로만 한다.

## 6. 유지보수 규약

- native lifecycle 변경: `.agents/skills/risk-local-codex-lifecycle` → `SKILLS.md` → `AGENTS.md`/본 문서/usage guide 순으로 정합을 확인한다.
- domain checklist 변경: 필요한 항목만 `.claude/skills/<name>`에서 갱신하고 actor/merge 중복 규칙을 새로 만들지 않는다.
- 신규 native Skill은 중복 기능보다 반복적으로 필요한 독립 workflow일 때만 추가한다.
- Skill 생성/갱신은 Codex `skill-creator` scaffold와 validator를 사용한다.
- historical 문서는 당시 actor/상태를 보존하고 current guide로 링크한다. 과거 기록을 현재 운영 근거로 재해석하지 않는다.

## 7. 보안 / 검증

- Skill 문서에 실데이터, 실 schema, 내부규정/NCR 원문, secret/token, certificate/key, 모델파일, 외부 다운로드 지침을 넣지 않는다.
- Offline, NuGet 0, external API/telemetry/auto-update 0, SQL/VBA auto-execution 0, hash-only audit, NoModel, approval STOP을 유지한다.
- Skill 구조는 `quick_validate.py`로 검증한다. Skill-only 변경도 product test 불변을 확인하며, 코드/도구 동작을 건드렸다면 full local gate를 실행한다.

> 관련: `AGENTS.md`, `SKILLS.md`, `CLAUDE.md`, `docs/32_Branch_Governance.md`, `prompts/codex/README_skills_usage.md`, `.agents/skills/`, `.claude/skills/`.
