# Codex Skill 사용법 (Project Skills Bridge)

> Codex는 Claude Code Skill을 **자동 실행하지 못한다.** Skill은 Codex에게 **읽고 따르는 체크리스트**다. (정본 인덱스 = root `SKILLS.md`, 원칙 = `AGENTS.md §8` / `CLAUDE.md §12`.)

## 0. 매 구현 전 (자동 — 사용자 재요청 불필요)
> **자동 참조가 기본값.** Codex는 사용자가 `AGENTS.md`/`SKILLS.md`/Skill 문서를 매번 붙여넣지 않아도, **모든 구현 착수 전 항상** 아래를 스스로 읽는다. (정본 = `AGENTS.md §9 Automatic Skill Bridge`.)
1. `AGENTS.md` — 우선순위·절대원칙·STOP·Gate.
2. `SKILLS.md` — 현재 기준선·Skill 목록·금지사항·자동 적용/자동 Preflight/STOP Gate 분류(`SKILLS.md §6`).
3. 지정된 **NEXT UP WP**(`docs/39` Resume Brief)와 그 **Codex 프롬프트**(`prompts/codex/<WP-ID>_*.md`)를 읽는다.
4. 그 WP에 **관련된 `.claude/skills/<skill-name>/SKILL.md`(+support `.md`)를 체크리스트처럼** 읽는다(작업 유형 매핑 = §1).

> 예시 — 사용자가 "**STAB-UX-01 구현해**"라고만 해도 Codex는 자동으로 `risk-ui-ux-review` + `risk-smoke-governance` + `risk-security-guard` + `prompts/codex/STAB-UX-01_resizable_editor_layout.md`를 적용하고, 보고에 `Applied Skill Checklists`를 명시한다.

## 1. 작업 유형 → 먼저 읽을 Skill
| 작업 | 먼저 읽을 Skill |
|---|---|
| 저장소 전체 진단·목표/현황/로드맵 재정렬 | `risk-repo-audit` (코드·WPF 도달성·출하본·실 PC 증거를 분리) |
| 데이터 입력·매핑·한도·대사·리포트 | `risk-data-limit-review` |
| 규정 RAG · KB · NCR · 원문 가드 | `risk-rag-ncr-governance` |
| 분석·차트·Heatmap·TopN·집중도 | `risk-analytics-design` |
| 승인형 Feedback Learning | `risk-feedback-learning` |
| WPF UX·SQL/VBA 에디터·Smart Assist | `risk-ui-ux-review` |
| Local LLM Adapter/Runtime | `risk-llm-approval` (**STOP — 승인 전 의존성/모델 추가 금지**) |
| 테스트 추가/변경 | `risk-smoke-governance` |
| **릴리스 컷(REL WP — 버전 범프·출하본 갱신)** | `risk-release-cut` (**기능 변경 0 · 단언 가감 0 · STOP 접촉 0 · 산출물 4종 검증**) |
| 릴리스 패키지 검증 | `risk-release-verify` |
| **구조 리팩터(행위 불변 분해 — God-class/탭 추출)** | `risk-arch-refactor` (**행위 변경 0 · 계약 테스트 이전 · MVVM 빅뱅 금지**) |
| Gate B/C 증거 | `risk-gate-bc` (**증거 민감정보 0 — dummy·masking만**) |
| Team Pilot 준비/운영 | `risk-team-pilot` (Gate B/C 봉인 선행) |
| 브랜치·PR·머지 | `risk-branch-governance` |
| 보안·민감정보 점검(항상) | `risk-security-guard` |
| 상태·문서 truth-sync | `risk-status-sync` + `risk-doc-truth-sync` |

## 2. 구현 중 지켜야 할 Skill 공통 원칙
- 외부 NuGet 0 · 외부 API/Telemetry/AutoUpdate 0 · SQL/VBA 자동실행 0.
- 해시 전용 Audit(원문 미저장) · NoModelMode 유지.
- 실데이터/실 테이블·컬럼명/내부규정 원문/NCR 공식본 원문/모델파일 **repo 미포함**.
- 모델 가중치 자동학습 0 · 기존 테스트 삭제/약화 0(additive 회귀만).
- Vector/Embedding/LLM Runtime/모델파일이 필요해지면 **즉시 STOP** → 승인 문서(`docs/41`·`docs/40`) 후에만.
- main 직접 push 금지 · force push 금지. 작업 브랜치 = `feature/<WP-ID>-*`.

## 3. 금지
- Codex는 **Skill 문서(`.claude/skills/**`)를 수정하지 않는다** — 사용자가 명시적으로 요청한 경우 제외.
- Skill 원칙을 어기는 구현 금지.

## 4. 완료 보고 형식
build 결과 · SmokeTest **`Total=N PASS / 0 FAIL`** 합계 줄 · Gate A 결과 · 변경 파일 · 양성/음성 케이스 · **"사용한 Skill 체크리스트"**(= **Applied Skill Checklists**, 예: `risk-data-limit-review`, `risk-smoke-governance`, `risk-security-guard`).

## 5. 머지 게이트 (Local-Gate, `CLAUDE.md §11.6`)
로컬 `dotnet build` + `dotnet run --project tests/RiskManagementAI.SmokeTests`(→ `Total=N PASS / 0 FAIL`) 증거 + Claude 코드리뷰(`risk-codex-review`). GitHub CI green을 전제로 요구하지 않는다.
