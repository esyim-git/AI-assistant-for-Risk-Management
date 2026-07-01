# SKILLS.md — Project Skills Index (Claude · Codex 공용)

## 1. 목적
Claude Code와 Codex가 **반복 작업을 일관되게 수행**하도록, 프로젝트 전용 Skill을 한곳에서 색인한다. 긴 절차/체크리스트를 매번 CLAUDE.md/AGENTS.md에 누적하지 않고 **Skill 문서로 분리**해 재사용한다. (기준선: main `e69a1ae`, VERSION `0.7.0`, 정본 SmokeTest `Total=792`.)

## 2. Claude Code Project Skills 경로
- 각 Skill = `.claude/skills/<skill-name>/SKILL.md` + (긴 체크리스트는) 같은 폴더의 support `.md`.
- Claude는 **Skill 도구**로 호출하거나, 작업 맥락에 맞는 Skill을 선택해 그 SKILL.md/support를 따른다.

## 3. Codex가 Skill을 활용하는 방식 (Skill Bridge)
Codex는 Claude Code Skill을 **자동 실행하지 못한다**. 따라서 Codex는 구현 전 **`SKILLS.md`를 읽고, 현재 WP에 관련된 `.claude/skills/<skill-name>/SKILL.md`(+support)를 체크리스트처럼 참조**한다. 특히 release · security · RAG/NCR · Local LLM · data-limit · UI/UX 작업은 해당 Skill 문서를 **먼저** 읽는다. (상세 = `AGENTS.md §8 Skill Bridge`, `prompts/codex/README_skills_usage.md`.)

## 4. Skill 목록 (15)
> 호출: **수동** = frontmatter `disable-model-invocation: true`(Claude가 명시 호출/Codex가 참조). **자동** = `paths:` 스코프로 해당 경로 작업 시 적용.

| # | Skill | 목적 | 호출 |
|---|---|---|---|
| 1 | `risk-status-sync` | 현재 상태(VERSION·Roadmap·SmokeTest·Gate·NEXT UP) 정본화 | 수동 |
| 2 | `risk-doc-truth-sync` | 문서 ↔ 실제 구현 정합(과대표기/드리프트 제거) | 수동 |
| 3 | `risk-wp-planner` | Codex용 Work Package + 프롬프트 1개 작성 | 수동 |
| 4 | `risk-codex-review` | Codex PR/Diff 4축 리뷰(범위·보안·테스트·문서·게이트) | 수동 |
| 5 | `risk-smoke-governance` | SmokeTest suite 거버넌스(단언 보존·약화 방지·Unclassified=실패) | 자동 `tests/**` |
| 6 | `risk-release-verify` | 오프라인 Portable Release 패키지 검증(SHA256·manifest·금지파일) | 수동 |
| 7 | `risk-gate-bc` | 실 오프라인 Test PC Gate B/C 증거 준비·기록 | 수동 |
| 8 | `risk-security-guard` | repo/release 민감정보·실데이터·원문·모델파일·금지자동화 점검 | 자동 `**` |
| 9 | `risk-data-limit-review` | 입력·매핑·Exposure-Limit Join·대사·Dashboard=Report·Hidden Risk | 자동 `Core/{Data,Mapping,Risk,Report}/**`·`tests/**` |
| 10 | `risk-analytics-design` | 전일대비·차트·Heatmap·TopN·집중도 등 R2 분석/시각화 설계 | 수동 |
| 11 | `risk-rag-ncr-governance` | 규정 RAG·Public/Internal KB·NCR Rule Pack·원문 가드·승인 게이트 | 자동 `Core/{Kb,Ncr}/**`·`kb/**`·`config/ncr/**`·`docs/08*·17*·41*` |
| 12 | `risk-llm-approval` | Local LLM Adapter/Manifest/Out-of-process 설계 + STOP 승인 요건 | 수동 |
| 13 | `risk-feedback-learning` | 승인형 Feedback Learning(검토·승격·검색·반영·Audit) — 학습 아님 | 수동 |
| 14 | `risk-ui-ux-review` | WPF UX·SQL/VBA 에디터·Smart Assist·Completion Popup 검토 | 자동 `App/**`·`Core/Assist/**`·`docs/**` |
| 15 | `risk-branch-governance` | 브랜치/PR/squash/soft-guard/hard-protection/release tag 정책 | 수동 |

## 5. 수동 호출 순서 (일반 반복 루프)
1. `risk-status-sync` — 현재 상태 정본화 → NEXT UP 확인
2. `risk-wp-planner` — NEXT UP WP + Codex 프롬프트 작성
3. (Codex 구현) → `risk-codex-review` — 4축 리뷰 → APPROVE/Fix
4. `risk-doc-truth-sync` — 머지 후 문서 정합
5. 도메인별 검토는 6·7·8·9·10·11·12·13·14를 작업 성격에 맞게 병용

## 6. 자동 적용 / 자동 Preflight / STOP Gate 구분
Skill은 적용 방식에 따라 **3분류**한다. 정본 Preflight 체인 = `CLAUDE.md §13`(Claude) · `AGENTS.md §9`(Codex Automatic Skill Bridge).

| 분류 | Skill | 적용 방식 |
|---|---|---|
| **자동 적용(path-scoped)** | `risk-security-guard`·`risk-smoke-governance`·`risk-data-limit-review`·`risk-rag-ncr-governance`·`risk-ui-ux-review` | `paths:` 스코프 — 해당 경로 작업 시 Claude Code가 자동 표면화(Codex는 해당 경로 작업 시 체크리스트로 참조) |
| **자동 Preflight(표준 절차)** | `risk-status-sync`·`risk-doc-truth-sync`·`risk-wp-planner`·`risk-codex-review`·`risk-analytics-design`·`risk-feedback-learning`·`risk-branch-governance` | 작업 유형 인지 시 Claude가 명시 호출 없이 표준 절차로 적용(사용자 `/skill` 타이핑 불필요) |
| **STOP Gate(승인형)** | `risk-release-verify`·`risk-gate-bc`·`risk-llm-approval` | Preflight로 참조하되 **자동 실행·자동 PASS 아님** — 실 Test PC 증거·인증서/모델 승인 전 진행·PASS 금지(§7·`CLAUDE.md §11.4·§11.5`) |

> "자동 적용/Preflight"은 **체크리스트 적용**이지 **게이트 통과**가 아니다. Gate B/C·Release·LLM은 증거/승인 선행(STOP).

## 7. 금지사항 (모든 Skill 공통)
외부 다운로드 · 외부 NuGet 추가 · 모델파일 추가 · 실데이터/내부규정 원문/NCR 공식본 원문 추가 · 비밀번호/토큰/secret 추가 · SQL/VBA 자동실행 기능 추가 · main 직접 push · force push · 기존 테스트 삭제/약화. (Skill 문서는 코드 동작을 바꾸지 않는다.)

## 8. Claude ↔ Codex 반복 Workflow
`Claude Planning(risk-status-sync→risk-wp-planner) → Codex Implementation(1 WP, SKILLS.md+관련 SKILL.md 참조) → Claude Review(risk-codex-review) → Codex Fix → Claude Final Gate(+risk-security-guard/risk-smoke-governance) → PR(risk-branch-governance) → Claude truth-sync(risk-doc-truth-sync) → 다음 WP`.

## 9. Release 전 Skill 호출 순서
`risk-status-sync` → `risk-smoke-governance` → `risk-security-guard` → `risk-release-verify` → (실 PC) `risk-gate-bc` → `risk-doc-truth-sync`(릴리스 노트/증거 반영).

## 10. PR Review 전 Skill 호출 순서
`risk-codex-review`(범위·테스트·문서) + `risk-security-guard`(Gate A) + 도메인 Skill(`risk-data-limit-review`/`risk-rag-ncr-governance`/`risk-ui-ux-review`/`risk-analytics-design`/`risk-feedback-learning`/`risk-llm-approval` 중 해당) + `risk-smoke-governance`(테스트 보존) + `risk-branch-governance`(머지 정책).

## 11. Gate B/C 전 Skill 호출 순서
`risk-release-verify`(패키지 정합·SHA256·금지파일) → `risk-gate-bc`(Test PC 증거 양식/수집) → `risk-doc-truth-sync`(증거 시트 `docs/45`·`docs/48` 상태 반영). **실 오프라인 Test PC 증거 없으면 Gate B/C는 PASS 금지(BLOCKED 유지).**

> 관련: `.claude/skills/`(Skill 본문) · `CLAUDE.md §12 Skill 운영` · `AGENTS.md §8 Skill Bridge` · `prompts/codex/README_skills_usage.md` · `docs/49_Project_Skills_Guide.md`.
