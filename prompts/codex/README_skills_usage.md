# Codex Skill 사용법 (Project Skills Bridge)

> Codex는 Claude Code Skill을 **자동 실행하지 못한다.** Skill은 Codex에게 **읽고 따르는 체크리스트**다. (정본 인덱스 = root `SKILLS.md`, 원칙 = `AGENTS.md §8` / `CLAUDE.md §12`.)

## 0. 매 구현 전 (필수)
1. `SKILLS.md`를 읽는다 — 현재 기준선·Skill 목록·금지사항.
2. 지정된 **NEXT UP WP**(`docs/39` Resume Brief)와 그 **Codex 프롬프트**(`prompts/codex/<WP-ID>_*.md`)를 읽는다.
3. 그 WP에 **관련된 `.claude/skills/<skill-name>/SKILL.md`(+support `.md`)를 체크리스트처럼** 읽는다.

## 1. 작업 유형 → 먼저 읽을 Skill
| 작업 | 먼저 읽을 Skill |
|---|---|
| 데이터 입력·매핑·한도·대사·리포트 | `risk-data-limit-review` |
| 규정 RAG · KB · NCR · 원문 가드 | `risk-rag-ncr-governance` |
| 분석·차트·Heatmap·TopN·집중도 | `risk-analytics-design` |
| 승인형 Feedback Learning | `risk-feedback-learning` |
| WPF UX·SQL/VBA 에디터·Smart Assist | `risk-ui-ux-review` |
| Local LLM Adapter/Runtime | `risk-llm-approval` (**STOP — 승인 전 의존성/모델 추가 금지**) |
| 테스트 추가/변경 | `risk-smoke-governance` |
| 릴리스 패키징 | `risk-release-verify` |
| Gate B/C 증거 | `risk-gate-bc` |
| 브랜치·PR·머지 | `risk-branch-governance` |
| 보안·민감정보 점검(항상) | `risk-security-guard` |

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
build 결과 · SmokeTest **`Total=N PASS / 0 FAIL`** 합계 줄 · Gate A 결과 · 변경 파일 · 양성/음성 케이스 · **"사용한 Skill 체크리스트"**(예: `risk-data-limit-review`, `risk-smoke-governance`, `risk-security-guard`).

## 5. 머지 게이트 (Local-Gate, `CLAUDE.md §11.6`)
로컬 `dotnet build` + `dotnet run --project tests/RiskManagementAI.SmokeTests`(→ `Total=N PASS / 0 FAIL`) 증거 + Claude 코드리뷰(`risk-codex-review`). GitHub CI green을 전제로 요구하지 않는다.
