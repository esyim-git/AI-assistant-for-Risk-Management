---
name: risk-feedback-learning
description: Govern approved feedback learning, reviewed example promotion, persistence, retrieval, prompt usage, audit, and no weight-training rule.
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git diff *)
paths:
  - "src/RiskManagementAI.Core/Feedback/**"
  - "logs/**"
  - "config/**"
  - "docs/**"
---

# Feedback Learning Governance

## 목적
승인형 Feedback Learning이 **RETRIEVAL이지 모델 학습이 아님**을 강제한다. Human-Reviewed 피드백을 승인 필터로 승격하고, 결정적으로 검색해 review 경유 read-only로 프롬프트에 참고 주입하며, 행위는 해시 전용 audit(원문 미저장)으로만 남기는지 읽기 전용으로 점검한다. 코드 동작을 바꾸지 않는 **점검/가이드** 스킬이다.

## 언제 사용
- `src/RiskManagementAI.Core/Feedback/**`, `logs/**`, `config/**`, `docs/**`(특히 ADR-014·FEEDBACK-WP) 작업 시.
- 트리거 예: "승인 Example 승격", "Feedback Learning", "promoted example", "검색→프롬프트 반영", "audit 점검".

## 절대 원칙 (STOP)
- **모델 가중치 자동학습·fine-tune·모델 파일 쓰기/갱신 = 0(절대 금지).** R5는 RETRIEVAL이지 training이 아니다. `PromotionMode="ExampleCurationOnly"` 의미 보존. (`docs/40` ADR-014, `CLAUDE.md §3`)
- **Human-Reviewed 승인만 승격.** `ExamplePromotion.PromoteApproved`가 승인(`APPROVED`/`REVIEWER_APPROVED` 류) 필터 + 중복차단 + `UserIdHash` SHA-256 검증을 거친다. Original Task / Output / Corrected Output 연결 유지.
- **Approved Example Store = `config/promoted_examples.jsonl`** (`PromotedExampleStore`, append/readAll, `config/` 경로 샌드박싱). 본문 jsonl은 `.gitignore`로 **untracked** — 영속 파일 repo 미포함.
- **Prompt 반영 = review 경유 read-only 주입.** 검색 결과를 `DraftRequest.Context`에 참고 블록으로 결합하되 원 Context 보존, **자동 무검토 주입 금지**, 산출은 검토용 초안. (FEEDBACK-WP-02)
- **해시 전용 Audit(원문 미저장).** 검색/주입 행위는 `TaskLogWriter` 스키마(UserId/RequestHash/OutputHash 모두 SHA-256 hex)로만 기록. raw prompt·user id 평문 미저장.
- **Ingest 게이트.** 본문 저장 전 SQL/VBA Safety Blocker 0 + Forbidden Term(내부규정/NCR 원문·실데이터·실 테이블/컬럼·PII) 0. 실패 시 본문 null+warning, 승격은 메타로 진행.
- **신규 모델파일 쓰기 0 · Vector/Embedding/LLM Runtime/모델파일 = STOP** → 승인 문서(`docs/41 §3`·`docs/40` ADR-009) 후에만. 외부 NuGet 0.

## 절차
1. **승격 게이트 점검**: `ExamplePromotion.PromoteApproved`의 승인 필터·중복차단·`UserIdHash` 해시 검증이 약화되지 않았는지, `PromotionModeName="ExampleCurationOnly"` 보존을 확인.
2. **영속·커밋 가드**: `PromotedExampleStore.ResolveConfigFile`이 `config/` 하위 jsonl로 제한하는지, `config/promoted_examples*.jsonl`이 `.gitignore`에 있어 untracked인지(`git diff`/`git status`로 본문 파일 추적 0) 확인.
3. **검색 점검**: `PromotedExampleStore.ReadAll()` 위 검색이 결정적(안정 정렬)인지, Vector/Embedding 도입 흔적이 없는지 확인.
4. **Prompt 반영 점검**: 주입이 review 게이트 경유 read-only이고 원 Context 보존·자동주입 0인지(`Core/Generation/{DraftPipeline,ILocalDraftService,NoModelDraftService}.cs`) 확인.
5. **학습·audit 점검**: 모델 가중치 쓰기/모델파일 쓰기 0, 행위 audit이 해시 전용(원문 미저장)인지 확인.

## 산출물/보고
- 점검 결과를 `학습금지(가중치/모델파일 쓰기 0)` / `승인게이트` / `영속·untracked` / `검색결정성` / `review주입` / `해시audit` 범주로 분류, 각 항목 파일·라인 근거 + 상태 어휘(`VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`)만 사용.
- 현 R5 = PARTIAL(승격+영속+UI까지). 실 Test PC Gate 전 VERIFIED 금지. 위반 0건이면 "Feedback Learning 점검: 위반 0건(코드리뷰 레벨)".

## 참조
- `docs/40_ADR_Architecture_Evolution.md` ADR-014(승인 Example RETRIEVAL·Prompt 반영, 학습 아님)
- `docs/39_Work_Package_Backlog.md` FEEDBACK-WP-01(ingest 게이트+영속+결정적 검색+audit) · FEEDBACK-WP-02(Prompt 반영)
- 관련 코드: `src/RiskManagementAI.Core/Feedback/{ExamplePromotion,PromotedExampleStore}.cs`, `src/RiskManagementAI.Core/Logging/{FeedbackLogEntry,FeedbackLogWriter,LogHash,TaskLogWriter}.cs`, `src/RiskManagementAI.Core/Generation/{DraftPipeline,ILocalDraftService,NoModelDraftService}.cs`
- 연계 스킬: `/risk-security-guard`(secret·원문·모델파일 차단) · `/risk-llm-approval`(STOP·승인 게이트)
