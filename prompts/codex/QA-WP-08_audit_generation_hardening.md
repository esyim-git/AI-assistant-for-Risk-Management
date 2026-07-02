# Codex Prompt — QA-WP-08: Audit/Generation SmokeTest 하드닝 (해시 전용 로그·NoModel draft 경로)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-08) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-08-audit-generation-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-08, `SKILLS.md`+`risk-feedback-learning`·`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Logging/{LogHash,TaskLog*,FeedbackLog*,SuggestionLog*}.cs`, `src/RiskManagementAI.Core/Generation/{DraftPipeline,NoModelDraftService,ILocalDraftService}.cs`, `tests/RiskManagementAI.SmokeTests/{AuditTests,GenerationTests}.cs`.
> **기준선**: main `693488c`(VERSION 0.7.0), 정본 SmokeTest `Total=877 PASS=877 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
해시 전용 Audit 로그와 NoModel draft 경로의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드 변경 0** — 테스트만. **원문/raw prompt/user id 평문 미저장·`IsSha256Hex` 강제·6-positional 로그 스키마·NoModelMode(`DraftText=null`)** 불변식을 회귀로 잠근다.

## 1. 작업 범위 (AuditTests·GenerationTests — additive only)
1. `Core/Logging/*`·`Core/Generation/*`와 현 두 test를 대조 → **미커버 경계만** 추가.
2. Audit 후보: `TaskLogWriter`/`FeedbackLogWriter`/`SuggestionLogWriter`가 `UserId`/`RequestHash`/`OutputHash`를 `IsSha256Hex`로 강제(비해시 거부)·원문/raw/userid 평문 미저장·`FeedbackLogEntry` 6-positional·append-only·`logs/` 경로가드·`PromotedExampleReflection`/`PromotedExampleSearch` 해시 payload.
3. Generation 후보: `NoModelDraftService`가 `IsAvailable=false`·`DraftText=null`·안전 안내 반환·`DraftPipeline` safetyResult(PASS/BLOCKED/REVIEW_REQUIRED/NO_MODEL) 경계·audit 1건·기존 무참고 RequestHash 불변(FEEDBACK-WP-02 회귀).
4. **합성 더미만** — 실 user/raw prompt/원문 0.

## 2. 제외 범위
로깅/생성 제품 코드 변경. 실 모델 추론(R4). 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
**해시 전용(원문/raw prompt/user id 평문 미저장) 회귀** · `IsSha256Hex` 강제 단언 · NoModelMode 불변 · 합성 더미(실 user/원문 0) · NuGet 0 · **기존 테스트 삭제·약화 0**.

## 4. 테스트 (SmokeTest — 도메인 `Audit`/`Generation`)
> `SmokeTestContext.SmokeDomain`: Audit(`TaskLog`/`FeedbackLog`/`Audit`/`Feedback`/`PromotedExample`/`request hash`/`raw request`) — Generation(`NoModelDraftService`/`DraftPipeline`/`draft`/`NoModel`)보다 **위**. Audit 경계는 Audit 토큰으로, Generation 경계는 `draft`/`NoModel` 토큰으로(단 Audit 토큰 없이 — 안 그러면 Audit으로 감). ⚠️ Kb 토큰(`approval`/`metadata`/`document`/`source`) 회피(#106 재발 방지). `Unclassified=0`.
- 각 경계 → 기대(해시 hex·평문 미포함·NoModel null·safetyResult) 단언.
- 기존 `AuditTests`/`GenerationTests` 단언 **전부 보존**. 종료부 **`Total=877 → 877+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Audit/Generation 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-08-audit-generation-hardening` · Commit: `test: harden hash-only audit and no-model draft coverage (QA-WP-08)`

## 6. Claude Review Checklist
제품 코드 변경 0(테스트만) / 추가는 실제 미커버 경계 / 해시 전용·`IsSha256Hex`·평문 미저장·NoModel null·safetyResult 기대값 정확 / 합성 더미(실 user/원문 0) / 도메인 Audit·Generation·Unclassified 0(Kb 토큰 회피) / 기존 두 test 보존·감소 0 / NuGet 0 / `Total` 877 보존+신규 / Gate A.
