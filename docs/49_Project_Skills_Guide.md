# 49. Project Skills 운영 가이드 (Claude · Codex)

## 목적 / 범위
Claude Code와 Codex가 반복 작업을 일관되게 수행하도록 도입한 **Project Skills 체계**의 운영 가이드. 정본 인덱스는 root **`SKILLS.md`**, 원칙은 **`CLAUDE.md §12`**(Claude)·**`AGENTS.md §8`**(Codex Bridge)·**`prompts/codex/README_skills_usage.md`**(Codex 사용법). 본 문서는 구조·생명주기 매핑·유지보수 규약을 정리한다. **제외**: Skill 본문(각 `.claude/skills/<name>/SKILL.md`).

## 1. 구조
```text
SKILLS.md                                  # 정본 인덱스(15 skill·호출 순서·금지)
.claude/skills/<skill-name>/
    SKILL.md                               # frontmatter(name/description/호출방식/allowed-tools) + 간결 본문
    <topic>.md                             # 긴 체크리스트/템플릿 support 파일(선택)
CLAUDE.md §12                              # Claude Skill 운영 원칙
AGENTS.md §8                               # Codex Skill Bridge(읽는 체크리스트)
prompts/codex/README_skills_usage.md       # Codex 사용법
```
- **호출 방식**: frontmatter `disable-model-invocation: true` = 수동(Claude 명시 호출 / Codex 참조). `paths:` 스코프 = 해당 경로 작업 시 자동 적용.

## 2. 15 Skill ↔ 생명주기 매핑
| 단계 | Skill |
|---|---|
| 상태 파악 | `risk-status-sync` |
| 계획(WP) | `risk-wp-planner` |
| 구현(Codex) | 도메인 Skill 참조: `risk-data-limit-review`·`risk-rag-ncr-governance`·`risk-analytics-design`·`risk-feedback-learning`·`risk-ui-ux-review`·`risk-llm-approval` |
| 테스트 | `risk-smoke-governance` |
| 리뷰 | `risk-codex-review` + `risk-security-guard` |
| 머지/브랜치 | `risk-branch-governance` |
| 문서 정합 | `risk-doc-truth-sync` |
| 릴리스 | `risk-release-verify` → `risk-gate-bc` |

## 3. Claude 사용 (요약, 정본 = `CLAUDE.md §12`·§13)
작업 시작 시 필요 Skill 선택 → Codex 인계 전 `risk-wp-planner` → 결과 `risk-codex-review` → Release 전 `risk-release-verify` → Gate B/C 전 `risk-gate-bc` → Local LLM은 `risk-llm-approval` 없이 진행 금지 → RAG/NCR은 `risk-rag-ncr-governance` 적용. **자동 Preflight**(`CLAUDE.md §13`): 사용자가 `/risk-*`를 매번 호출하지 않아도 작업 유형별 체인(계획/PR/UI/Data/RAG/Release/Gate/LLM)을 Claude가 표준 절차로 자동 적용한다. STOP Gate(release/gate/llm)는 자동 실행 아님(승인 선행).

## 4. Codex 사용 (요약, 정본 = `AGENTS.md §8`·§9 / `README_skills_usage.md`)
Codex는 Skill을 자동 실행하지 못한다 → **`SKILLS.md` + 관련 SKILL.md를 읽는 체크리스트로** 사용. **Automatic Skill Bridge**(`AGENTS.md §9`): 매 구현 전 항상 `AGENTS.md`→`SKILLS.md`→관련 SKILL.md→대상 WP 프롬프트를 스스로 읽고(사용자에게 문서 목록 재요청 0), 완료 보고에 **"Applied Skill Checklists"**(= "사용한 Skill 체크리스트") 명시. Skill 문서 수정 금지(명시 요청 제외).

## 5. 유지보수 규약
- **신규 Skill**: `.claude/skills/<risk-name>/SKILL.md` 추가(frontmatter name=폴더명 일치) → `SKILLS.md §4` 표·호출 순서 갱신 → 필요 시 `CLAUDE.md §12`/`AGENTS.md §8` 반영.
- **Skill 갱신**: 본문은 코드/문서 현실과 정합 유지(`risk-doc-truth-sync`로 점검). 긴 체크리스트는 support `.md`로 분리.
- **개명/삭제**: `git mv`로 폴더 개명 후 frontmatter `name` + 모든 cross-reference(`SKILLS.md`·다른 SKILL.md·docs)를 새 이름으로 일괄 정렬.
- **드리프트 금지**: Skill은 코드 동작을 바꾸지 않으며, 절대 원칙(`CLAUDE.md §3`)·STOP(`§11.5`)·과대표기 금지(`§11.4`)를 전제한다.

## 6. 보안 유의사항
Skill 문서에 실데이터·실 테이블/컬럼명·내부규정 원문·NCR 공식본 원문·secret/토큰·모델파일 경로(실)·외부 다운로드 지침을 넣지 않는다. 예시는 더미만.

## 7. 테스트 기준 / 향후 확장
- 테스트: Skill은 문서이므로 코드 SmokeTest 영향 0(정본 `Total=714`). Skill 추가/개명이 코드·테스트를 바꾸면 안 된다.
- 향후: 워크플로(다중 에이전트) 패턴, Skill별 자동 점검 hook은 별도 검토(현재 범위 밖).

> 관련: `SKILLS.md`, `CLAUDE.md §12`, `AGENTS.md §8`, `prompts/codex/README_skills_usage.md`, `.claude/skills/`.
