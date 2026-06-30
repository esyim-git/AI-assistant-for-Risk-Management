# Codex Prompt — FEEDBACK-WP-01: 승인 Example 본문 Ingest 게이트 + 결정적 검색 + 해시 audit (RETRIEVAL, 학습 아님)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(FEEDBACK-WP-01) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/feedback-wp-01-example-retrieval`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` FEEDBACK-WP-01, `docs/40` **ADR-014**·ADR-003/009, `docs/41 §3`, `src/RiskManagementAI.Core/Feedback/*`, `Core/Logging/{LogHash,FeedbackLog*,TaskLog*}.cs`, `Core/Generation/{DraftPipeline,ILocalDraftService}.cs`, `tests/RiskManagementAI.SmokeTests/AuditTests.cs`.
> **기준선**: main `30c1cfb`(VERSION 0.7.0), 정본 SmokeTest `Total=714 PASS=714 FAIL=0`.

## 0. 목표 (단일)
승인된 Example의 **본문(draft text)을 안전하게 적재(ingest 게이트)·영속**하고, 질의에 맞춰 **결정적으로 검색(retrieval)**하며, 검색 행위를 **해시 audit**한다. **Prompt 반영(주입)은 FEEDBACK-WP-02.** 본 WP는 **저장+검색**까지.

> **STOP/불변**: R5는 **RETRIEVAL이지 학습이 아니다** — 모델 가중치 학습·fine-tune·**모델 파일 쓰기/갱신 0**(절대원칙). `PromotionMode=ExampleCurationOnly` 의미 보존. 외부 NuGet·Vector/Embedding·LLM Runtime·모델파일 0(필요 시 STOP).

## 1. 작업 범위
1. **Example 본문 출처 = 로그 DTO와 분리된 non-log input.** `FeedbackLogEntry`/`FeedbackLogWriter`에는 `DraftBody`를 추가하지 않는다(평문 직렬화 방지). 기존 6-positional 생성자 호출·`AuditTests`(라인 ~29·68) 회귀를 보존한다. MainWindow/승격 경로는 별도 `FeedbackDraftBodyInput`(또는 동등 non-log DTO)로 본문을 전달한다. 본문 없으면 `ExampleBody=null` + 검색은 메타만(정상). 부득이 임시 raw-body 필드가 필요한 경우에도 `FeedbackLogWriter`가 serialize하는 타입 밖에 두거나 `[JsonIgnore]`로 로그 출력과 물리적으로 분리한다.
2. **Ingest 게이트(`ExamplePromotion.PromoteApproved` 승격 시점)**: 본문 저장 전 **`SqlSafetyChecker`/`VbaSafetyChecker` Blocker 0 AND 신규 `ForbiddenTermScanner` 0** 통과 시에만 `ExampleBody` 채움. 실패 시 **본문 null + Warning**(승격은 메타로 진행, 보수적). 본문 유형(SQL/VBA/기타)은 명시 `kind` 또는 본문 휴리스틱; **불확실 → 본문 null+warning**.
3. **신규 `ForbiddenTermScanner`**(`Core/Safety/` 또는 `Core/Feedback/`, 인박스, **단일 토큰 원천**): 정적 토큰 리스트(내부규정/NCR 원문·실데이터·실 테이블/컬럼·PII 패턴 최소 집합) `ScanText(string)` → finding. **`KbRepositoryGuard.Scan`(디렉토리 스캐너) 재사용 금지**(string 검사 불가). 토큰 중복정의 금지.
4. **`PromotedExample`에 `ExampleBody`(nullable) + 본문 길이/유형 메타 추가**(본문 없으면 null). `PromotedExampleStore` JSONL 직렬화 규약(UTF-8 no-BOM·Web options·append-only·`ResolveConfigFile` 샌드박싱) 재사용.
5. **결정적 검색**: `PromotedExampleStore.ReadAll()` 위에 keyword/score 검색(가능 시 KB-WP-01에서 추출될 inverted-index 코어 유틸 재사용; 없으면 인박스 keyword/Contains 점수) + **`OrderByDescending(Score).ThenBy(ExampleId, StringComparer.Ordinal)`** 안정 정렬. 검색은 결정적(동일 질의→동일 결과).
6. **검색 행위 audit**: `TaskLogWriter` 스키마(`UserId`/`RequestHash`/`OutputHash` 모두 SHA-256 hex, `LogHash` 단일 원천) 해시 기록. **raw prompt/user id/Example 본문 평문 미저장.**
7. **`.gitignore`에 `config/promoted_examples*.jsonl`(및 smoke fixture) 추가** — 본문 영속 파일 tracked 금지(STAB-UX-02 `*.local.json` 동형).

## 2. 제외 범위
**Prompt 반영/주입**(FEEDBACK-WP-02). 모델/LLM. Example revoke/만료. Vector/Embedding. 신규 NuGet.

## 3. 보안조건
모델 가중치 학습 0·모델 파일 쓰기 0 · NuGet 0 · 해시 전용 audit(원문/raw prompt/userid 평문 미저장) · 쓰기 `config/`·`logs/` 한정 · ingest 게이트로 실데이터/원문/PII 본문 차단 · 기존 `AuditTests` 음성(`ui draft smoke`/`user-smoke` 미저장) 보존.

## 4. 테스트 (SmokeTest — 도메인 `Audit`)
> ⚠️ `SmokeTestContext.ClassifyDomain`은 `Feedback`/`PromotedExample`/`ExamplePromotion`(line 97)을 UiContract보다 먼저 **Audit**으로 분류. 신규 단언 설명에 Kb 키워드(`검색`/`원문`/`공개`/`인용`) 사용 금지 — 영어 `search`/`retrieval` + `PromotedExample`/`Feedback`/`Audit` 토큰 사용. `Unclassified=0` 보존.
- 양성: 승인+안전 본문 → `ExampleBody` 채움·결정적 검색 hit·tie-break(Ordinal)·audit 1건(해시).
- 음성(ingest 게이트 3종 분리): (1) `DROP TABLE` 류 → 본문 null+warning, (2) 내부규정 원문 토큰 → 본문 null, (3) 실데이터/PII 유사 토큰 → 본문 null. + UserIdHash 비해시 거부·config 밖 경로 거부(기존 패턴).
- 음성: `FeedbackLogWriter` 출력에 DraftBody/raw 평문 미저장 단언. `.gitignore`에 `promoted_examples` 포함 단언(Packaging/Audit 도메인).
- 기존 `AuditTests`(FeedbackLogEntry 6-positional·PromotedExample append/dedupe·원문 미저장) **전부 보존**(`FeedbackLogEntry` raw body 필드 추가 금지).
- 종료부 **`Total=714 → 714+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄 · Gate A · 변경 파일 · 양성/음성.
- Branch `feature/feedback-wp-01-example-retrieval` · Commit: `feat: approved-example ingest gate + deterministic retrieval + audit (FEEDBACK-WP-01)`

## 6. Claude Review Checklist
RETRIEVAL-only(학습/모델파일 쓰기 0) / DraftBody는 `FeedbackLogEntry`·`FeedbackLogWriter` 직렬화 경로 밖(non-log DTO, 기존 호출 보존) / ingest 게이트(SafetyChecker AND ForbiddenTermScanner, 실패→본문 null) / `ForbiddenTermScanner` 단일 토큰 원천(가드 string 재사용 안 함) / 결정적 검색 + Ordinal tie-break / 해시 audit(평문 미저장) / `.gitignore` promoted_examples / 테스트 도메인 Audit(Kb 키워드 회피)·Unclassified 0 / NuGet 0 / 기존 AuditTests 보존 / `Total` 보존+신규 / Gate A.
