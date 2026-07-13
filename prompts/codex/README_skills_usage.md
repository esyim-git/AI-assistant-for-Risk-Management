# Local Codex Skill 사용법

> `.agents/skills/risk-local-codex-lifecycle`이 자동 발견되는 현재 lifecycle entry다. `.claude/skills/*`는 필요한 기술·보안·테스트 기준을 읽는 transitional domain checklist이며, legacy actor/merge 문구는 root `AGENTS.md`와 `docs/32`를 override하지 않는다.

## 0. Material Work 시작 전

1. `AGENTS.md`, `SKILLS.md`, compatibility guide `CLAUDE.md`를 읽는다.
2. `risk-local-codex-lifecycle`을 적용한다.
3. `docs/38`/`docs/39`의 실제 상태, 대상 WP와 `prompts/codex/<WP-ID>_*.md`를 확인한다.
4. `docs/40`, `docs/41`, `docs/28`과 대상 작업에 필요한 `.claude/skills/<name>/SKILL.md`의 직접 support file만 읽는다.
5. repo path, branch, live `origin/main`, dirty state, baseline/release SHA, Gate state, 범위를 먼저 기록한다.

## 1. 작업 유형 → Transitional Checklist

| 작업 | 먼저 읽을 checklist |
|---|---|
| 저장소 전체 진단·로드맵 | `risk-repo-audit -> risk-status-sync -> risk-doc-truth-sync -> risk-wp-planner` |
| 데이터 입력·매핑·한도·대사·리포트 | `risk-data-limit-review` |
| 규정 RAG·KB·NCR·원문 guard | `risk-rag-ncr-governance` |
| 분석·차트·Heatmap·TopN | `risk-analytics-design` |
| 승인형 Feedback retrieval | `risk-feedback-learning` |
| WPF UX·editor·Smart Assist | `risk-ui-ux-review` |
| 구조 refactor | `risk-arch-refactor` |
| 테스트 추가/변경 | `risk-smoke-governance` |
| release cut/package | `risk-release-cut -> risk-release-verify` |
| Gate B/C / Team Pilot | `risk-gate-bc -> risk-team-pilot` |
| branch·PR·merge | `risk-branch-governance` |
| PR/diff 독립 review | `risk-codex-review` + domain checklist |
| 보안·민감정보 점검 | `risk-security-guard` |
| Local LLM/runtime/model | `risk-llm-approval` 후 explicit approval 전 STOP |

## 2. 공통 불변식

- 외부 NuGet/API/telemetry/auto-update/SQL·VBA auto-execution 0.
- hash-only audit, NoModel, 실데이터·실 schema·내부규정/NCR 원문·secret/key/certificate·모델파일 repo 포함 0.
- 기존 테스트 삭제/약화 0. Core-only를 WPF reachability로 과대표기하지 않는다.
- Vector/Embedding/LLM runtime/model/signing/real Pack·NCR은 승인 전 STOP.
- main direct push, force push, hard reset, bypass merge 금지.

## 3. 구현과 검증

- dirty root는 수정하지 않고 clean non-temporary worktree를 사용한다.
- implementation은 `feature/<WP-ID>-<slug>`, docs/truth-sync는 `planning/<slug>`를 사용한다.
- 코드/도구 동작 변경은 Release build, SmokeTest `Total=N PASS=N FAIL=0`, Unclassified 0, Gate A, dependency/boundary 점검이 필요하다.
- 변경 파일을 명시적으로 stage하고 staged diff를 확인한 뒤 commit/push한다.

## 4. 독립 리뷰와 머지

중요 PR은 작성 task/context와 다른 Local Codex task/context가 exact head를 검토하고 PR에 다음을 남긴다.

```text
independent_review_verdict: pass|changes_required
reviewed_head: <full SHA>
```

merge 조건은 local gate + hosted exact-head `test`/`wpf-build` green + unresolved conversation 0 + independent verdict `pass` + authorization이다. squash merge만 허용한다.

Actions가 unavailable/quota-blocked/skipped/cancelled/not-run이면 local equivalent를 실행해 `local-fallback-only`로 보존한다. 이는 hosted green이나 protected check 충족이 아니며 merge를 해제하지 않는다.

## 5. 완료 보고

role, repo path, branch, head/reviewed SHA, deployment class, 변경 파일, local tests, hosted checks, Gate A, independent review, formal Gate/approval, boundary, blockers, rollback, cleanup, guide/Skill update, next owner, 다음 작업 model/effort 추천을 기록한다.

`Applied Skills/Checklists: risk-local-codex-lifecycle, ...` 형식으로 실제 사용한 항목만 명시한다.
