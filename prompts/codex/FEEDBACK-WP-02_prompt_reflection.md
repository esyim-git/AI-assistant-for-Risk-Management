# Codex Prompt — FEEDBACK-WP-02: 검색된 승인 Example의 review 경유 read-only Prompt 반영 (RETRIEVAL 주입, 학습 아님)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(FEEDBACK-WP-02) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/feedback-wp-02-prompt-reflection`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` FEEDBACK-WP-02, `docs/40` **ADR-014 §결정5**·§결정1/2/4, `docs/41 §3`, `src/RiskManagementAI.Core/Generation/{DraftPipeline,ILocalDraftService,NoModelDraftService}.cs`, `src/RiskManagementAI.Core/Feedback/{PromotedExampleRetriever,PromotedExample,PromotedExampleStore}.cs`, `src/RiskManagementAI.Core/Logging/{LogHash,TaskLog*}.cs`, `tests/RiskManagementAI.SmokeTests/{AuditTests,GenerationTests,SmokeTestContext}.cs`.
> **기준선**: main `f8b330a`(VERSION 0.7.0, FEEDBACK-WP-01 #106 머지 후), 정본 SmokeTest `Total=807 PASS=807 FAIL=0`.

## 0. 목표 (단일)
FEEDBACK-WP-01(승인 Example 본문 ingest 게이트 + 결정적 검색 + audit, DONE)의 검색 결과를 **`DraftRequest.Context`에 read-only 참고 블록으로 결합**한다. **원 Context는 그대로 보존**하고, **명시적 review 승인 없이는 결합하지 않으며(자동 무검토 주입 0)**, 결합은 **결정적**이고, 주입 행위는 **기존 `DraftPipeline` 해시 audit로만** 남긴다. 산출은 항상 **검토용 초안**이다. 본 WP는 **결합(반영)까지**이며 실 모델 추론은 아니다(NoModelMode 유지).

> **STOP/불변**: R5는 **RETRIEVAL 주입이지 학습이 아니다** — 모델 가중치 학습·fine-tune·**모델 파일 쓰기/갱신 0**. `PromotionMode=ExampleCurationOnly` 의미 보존. 외부 NuGet·Vector/Embedding·LLM Runtime·모델파일 0(필요 시 STOP). 실 모델 추론 = R4(별도, APPROVAL_REQUIRED).

## 1. 작업 범위 (Core + 테스트 — App/MainWindow 배선은 본 WP 밖)

### 1.1 신규 non-log DTO — `Core/Generation/DraftReferenceExample.cs`
```csharp
public sealed record DraftReferenceExample(string ExampleId, string ReferenceText);
```
- **출처 불변식**: `ReferenceText`는 **FEEDBACK-WP-01 ingest 게이트를 통과해 저장된 `PromotedExample.ExampleBody`**(게이트 통과 시에만 non-null)에서만 만들어진다. 호출자(App/review 흐름)가 `PromotedExampleRetriever.Search`로 검색 → **사람 검토** → 승인분만 이 DTO로 만든다. **본 WP는 ingest 게이트를 다시 열지 않는다**(WP-01이 이미 원문/실데이터/PII 본문을 null로 차단). 로그 직렬화 타입 아님.

### 1.2 신규 순수 결합기 — `Core/Generation/DraftReferenceComposer.cs`
- 시그니처(예): `public static string? Compose(string? originalContext, IReadOnlyList<DraftReferenceExample> references)`.
- **결정적**(동일 입력 → 동일 출력): `Dictionary` 열거·`DateTime`·`Guid`·`Math.Random`·문화권 의존 포맷 **금지**. 개행은 `'\n'` 고정.
- **입력 순서 보존**: 호출자(retriever)가 이미 `OrderByDescending(Score).ThenBy(ExampleId, Ordinal)`로 결정적 정렬한 순서를 **그대로** 사용한다(결합기 내부 재정렬 없음). 결합기 자체가 비결정성을 도입하지 않음을 보장.
- **필터**: `ReferenceText`가 null/공백인 항목은 **건너뛴다**(반영 대상 아님).
- **상한(필수)**: 결합기는 결정적 상한을 공개 상수로 둔다. 예: `public const int MaxReferenceCount = 5`, `public const int MaxReferenceTextChars = 2000`. 필터 후 입력 순서대로 최대 N건만 사용하고, 각 `ReferenceText`는 문자 수 상한에서 결정적으로 truncate한다. truncate 표식도 고정 문자열로 둔다. 상한은 본 WP 필수 범위이며 테스트가 상수로 단언한다.
- **항등(identity) 규칙**: 유효 reference가 0건이면 **`originalContext`를 그대로 반환**(null이면 null). → "참고 없음 = Context 무변경".
- **원 Context 보존**: `originalContext`가 non-empty면 결합 결과에 **원문이 그대로(부분 문자열로) 포함**되고, 참고 블록과 **명확히 구분**된다. `originalContext`가 null/공백이면 블록만(선행 빈 줄 없이) 출력.
- **read-only 표식 + fencing**: 참고 블록 머리글을 **`public const string ReferenceBlockHeader`**(예: `"[참고 예시 · 읽기 전용 · 검토용 초안 · 자동 적용 아님]"`)로 노출(테스트가 상수로 단언). 각 항목은 raw inline bullet이 아니라 deterministic fenced data block으로 작성한다. 예: `- (i) [ExampleId] chars=<n>` 다음 줄부터 모든 reference line을 `| ` 접두로 indent/prefix하고, CRLF는 `\n`으로 normalize한다. Reference body 안의 newline, instruction-like comment, markdown fence 문자열이 참고 블록 밖 instruction으로 해석되지 않도록 escaping/indentation을 테스트로 고정한다.

### 1.3 `DraftPipelineRequest` — 가산(additive) 옵션 필드
`Core/Generation/DraftPipeline.cs`의 record를 **가산만**:
```csharp
public sealed record DraftPipelineRequest(
    DraftRequestKind Kind,
    string Prompt,
    string UserId,
    string? Context = null,
    IReadOnlyList<DraftReferenceExample>? ReferenceExamples = null,  // 신규(기본 null)
    bool ReferencesReviewed = false);                               // 신규 review 게이트(기본 false)
```
- 기존 3·4-인자 호출부(App·테스트)는 **무변경 컴파일**되어야 한다.

### 1.4 `DraftPipeline.Generate` — review 게이트 + 결합 + audit (핵심, 모호성 제거)
현재 `Generate`(line 41~)는 `new DraftRequest(request.Kind, request.Prompt, request.Context)`(line 43~46)를 만들고, audit는 `LogHash.Sha256Hex($"{request.Kind}|{request.Prompt}|{request.Context ?? string.Empty}")`(line 114)로 Context를 **이미 해시**한다. 다음으로 바꾼다:
1. `Generate` 시작부에서 **effectiveContext** 계산:
   ```csharp
   var hasReviewedReferences =
       request.ReferencesReviewed
       && request.ReferenceExamples is { Count: > 0 };
   var effectiveContext = hasReviewedReferences
       ? DraftReferenceComposer.Compose(request.Context, request.ReferenceExamples!)
       : request.Context;   // review 미승인/참고 없음 → 원 Context 그대로(주입 0)
   ```
2. `new DraftRequest(request.Kind, request.Prompt, effectiveContext)` — 서비스에 **effectiveContext** 전달.
3. audit 해시(line 114)의 `request.Context` → **`effectiveContext`**로 교체. (**주입 행위 해시 audit** — 결합된 Context가 해시에 반영됨. 참고가 없거나 미승인이면 `effectiveContext == request.Context`이므로 **기존 audit 해시 불변** → 기존 GenerationTests/AuditTests 회귀 보존.)
4. **선택 Example identity audit(해시 전용)**: review gate로 실제 반영된 reference의 `ExampleId` 목록을 입력 순서대로 join한 payload를 별도 hash로 남긴다. 구현 방식은 (a) `TaskLogEntry.OutputHash` payload에 selected-id hash를 포함하거나, (b) 별도 `TaskLogEntry` task type(예: `PromotedExampleReflection`)으로 `RequestHash=LogHash.Sha256Hex(effectiveContext)`, `OutputHash=LogHash.Sha256Hex(selectedExampleIdsPayload)`를 남기는 방식 중 하나를 택한다. 어느 쪽이든 raw context/reference/user id 평문 저장 0, 선택된 ExampleId 목록은 hash-only로 감사 가능해야 한다.
- **review 게이트 = `ReferencesReviewed`.** `ReferenceExamples`가 있어도 `ReferencesReviewed==false`면 결합 0(자동 무검토 주입 금지). 이것이 유일한 주입 관문이다.
- `DraftPipeline`은 **`PromotedExampleRetriever`를 직접 호출하지 않는다**(검색은 호출자 몫; 파이프라인은 순수 결합·감사만) — 검색↔주입 분리.

## 2. 제외 범위
실 모델 추론/생성(R4). 모델 가중치 학습·모델파일 쓰기. **App/MainWindow 실 review UI 배선**(Gate B 후속 — 본 WP는 Core+테스트). ingest 게이트 재구현(WP-01). Example revoke/만료. Vector/Embedding. 신규 NuGet.

## 3. 보안조건
- 모델 가중치 학습 0·모델 파일 쓰기 0 · NuGet 0 · Vector/Embedding/LLM Runtime STOP.
- **해시 전용 audit(원문/raw prompt/user id/참고 본문 평문 미저장).** 참고 본문은 in-memory `DraftRequest.Context`에만 실리고 로그에는 **Context 해시**로만 남는다(기존 `LogHash` 단일 원천). 참고 본문 평문 로그 직렬화 0.
- **자동 무검토 주입 금지**: `ReferencesReviewed` 승인 없이는 결합 0.
- 산출은 항상 **검토용 초안**(NoModelMode에서 `DraftText=null`·안전 안내 유지). 결합이 SafetyChecker/게이트를 우회하지 않음(초안 안전 검사 경로 불변).
- 쓰기 경로 `config/`·`logs/` 한정(신규 파일 없음).

## 4. 테스트 (SmokeTest — 도메인 `Audit`)
> ⚠️ **도메인 오분류 함정(PR #106 재발 방지).** `SmokeTestContext.ClassifyDomain`은 위에서 아래로 첫 매칭이다. 신규 단언 설명에:
> - **Kb(line 94) 트리거 금지**: `approval`·`metadata`·`document`·`source`·`citation`·`license`·`catalog`·`Regulation`·`검색`·`원문`·`공개`·`인용`. → "approval" 대신 **`reviewed`/`review gate`**, "approved example" 대신 **`reviewed reference`/`PromotedExample reference`**로 쓴다.
> - **Assist(line 96) 트리거 금지**: `assist`·`completion`·`suggestion`·`provider`·`popup`.
> - **Audit(line 97) 토큰 필수**: 각 신규 단언 설명에 `Feedback`·`PromotedExample`·`Audit`·`request hash` 중 하나 이상 포함(그래야 Generation(line 98)의 `draft`/`DraftPipeline`보다 **먼저** Audit으로 분류됨).
> - 예시 설명: "PromotedExample reference reflection preserves original Context (Feedback audit hash unchanged when review gate off)".
> - `Unclassified=0` 보존.
- **양성(결합)**: `ReferencesReviewed=true` + 유효 `DraftReferenceExample` 다건 → effectiveContext가 **원 Context를 부분문자열로 포함** + `ReferenceBlockHeader` + 각 `ExampleId` 포함. **capture fake `ILocalDraftService`**(신규 테스트용, 받은 `DraftRequest`를 기록)로 결합 Context 관측(=`NoModelDraftService`는 Context를 읽지 않아 직접 관측 불가). audit 1건.
- **review 게이트 음성**: `ReferenceExamples` 존재하지만 `ReferencesReviewed=false` → effectiveContext == 원 Context(결합 0), capture fake가 원 Context 그대로 관측, **audit RequestHash == 무참고 기준선과 동일**.
- **원 Context 보존**: non-empty 원 Context에 대해 결합 결과 `Contains(originalContext)` true, 블록이 구분되어 존재.
- **필터/항등**: `ReferenceText` null/공백 항목 제외; 전부 무효면 Context 무변경(항등). 유효 0건 + `ReferencesReviewed=true`여도 결합 0.
- **상한**: `MaxReferenceCount` 초과 입력은 입력 순서 기준 N건만 반영, `MaxReferenceTextChars` 초과 body는 deterministic truncate 표식 포함. 동일 입력 2회 결과 동일.
- **fencing**: newline/instruction-like reference body가 `| ` 접두 fenced data block 안에만 남고 블록 밖 instruction으로 탈출하지 않음. `ReferenceBlockHeader`와 각 `ExampleId`는 유지.
- **결정성**: 동일 입력 2회 `Compose` → 문자열 동일(입력 순서 보존).
- **audit 해시 전용**: 주입 시 `TaskLogWriter` 출력의 `RequestHash`가 SHA-256 hex(`LogHash`)이고 참고 본문 평문 미포함. 무참고 경로 `RequestHash`가 **FEEDBACK-WP-02 이전과 동일**(effectiveContext==Context 회귀). 선택된 `ExampleId` payload도 hash-only로 남아 "어떤 reviewed reference가 반영됐는지"를 raw context 없이 감사 가능.
- **기존 보존**: 기존 `GenerationTests`(`DraftPipeline`/`NoModelDraftService` 무참고 경로)·`AuditTests` **전부 보존**(단언 수·해시 스키마·6-positional 로그 호출 무변경). 기존 3·4-인자 `DraftPipelineRequest` 호출부 무변경 컴파일.
- 종료부 **`Total=807 → 807+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(`Total=807+N PASS=807+N FAIL=0` + 도메인별 요약, Audit 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 변경 파일 · 양성/음성 요지 · **Applied Skill Checklists**(`SKILLS.md`+`risk-feedback-learning`+`risk-smoke-governance`+`risk-security-guard`).
- Branch `feature/feedback-wp-02-prompt-reflection` · Commit: `feat: review-gated read-only example reflection into draft prompt (FEEDBACK-WP-02)`

## 6. Claude Review Checklist
RETRIEVAL 주입(학습/모델파일 쓰기 0) / **review 게이트 = `ReferencesReviewed`, 승인 없으면 결합 0(자동주입 0)** / 원 Context 보존(부분문자열 + 블록 구분) / `DraftReferenceComposer` 결정적(입력 순서 보존·null/공백 필터·유효 0건 항등·개수/문자수 상한) / reference body fencing·indentation으로 read-only data 보존 / **effectiveContext를 DraftRequest와 audit 해시(line 114) 양쪽에 사용 → 주입 해시 audit·무참고 회귀 불변** / 선택된 `ExampleId` 목록 hash-only audit / `DraftPipeline`이 `PromotedExampleRetriever` 미호출(검색↔주입 분리) / 해시 전용 audit(참고 본문 평문 미저장) / non-log DTO(로그 직렬화 밖) / capture fake로 관측 / 도메인 Audit(Kb·Assist 키워드 회피)·Unclassified 0 / 기존 GenerationTests·AuditTests·3·4-인자 호출부 보존 / NuGet 0·Vector STOP / `Total` 807 보존+신규 / Gate A.
