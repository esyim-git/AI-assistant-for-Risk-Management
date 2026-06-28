# Codex UX-WP-01 — Smart Assist Completion Core (정적·NoModel)

> 권위 스펙: `docs/39 §UX-WP-01`, `docs/46`(Smart Assist 설계), `docs/40`(ADR-010). 우선순위: `AGENTS.md` > `docs/39` > 본 프롬프트.
> **전체 생성(`DraftPipeline`)과 별개**다. inline 완성의 **계약+코어**만. 모델 0(NoModel). 외부 NuGet 0.

## 현재 문제 / 목표
SQL/VBA/Excel/리스크 코멘트 입력 중 inline 추천을 줄 토대가 없다. **결정적·정적** 완성 엔진과 provider 계약을 만든다 — `CompletionEngine`·`CompletionContext`·`CompletionItem`·`ICompletionProvider`·Registry + **NoModelMode 완전 동작** + accept **해시 audit**.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3`, `docs/46`, `docs/40`(ADR-010), `Core/Logging/`(`LogHash`·`TaskLogWriter` 패턴), `Core/Safety/`(`SafetyFinding`).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/ux-wp-01-completion-core origin/main
```
- .NET 8. PR→main(squash, `(#PR)`), Gate A, **NuGet 0**(인박스만).

## 작업 범위 (`RiskManagementAI.Core.Assist`)
1. enum `CompletionLanguage { Sql, Vba, Excel, RiskComment }`, `CompletionItemKind { Keyword, Snippet, Function, Phrase, SafetyHint, BlockedHint }`.
2. record `CompletionContext(CompletionLanguage Language, string Text, int CaretIndex, string Prefix, string Mode)` — `Text`는 휘발성(로그 미저장).
3. record `CompletionItem(string Label, string InsertText, CompletionItemKind Kind, string Source, bool RequiresReview, string? SafetyNote, int SortKey)`, record `CompletionResult(IReadOnlyList<CompletionItem> Items, string Mode, IReadOnlyList<string> Warnings)`.
4. `interface ICompletionProvider { string ProviderId; bool Supports(CompletionLanguage); IReadOnlyList<CompletionItem> GetCompletions(CompletionContext); }` — 순수·결정적·I/O 0.
5. `CompletionProviderRegistry`(등록/언어별 결정적 조회) + `CompletionEngine.GetCompletions(CompletionContext)` — provider 호출→병합→중복제거(ProviderId+Label)→결정적 정렬(SortKey, then Label Ordinal)→개수 상한(예 50). `Mode="NoModel"`.
6. **Accepted Suggestion Audit**: `SuggestionLogEntry(SuggestionId, ProviderId, Language, Mode, UserHash, AcceptedAtUtc)` + writer(`TaskLogWriter` 패턴, JSONL, `logs/`). `SuggestionId`=provider+Label 결정적 해시. `UserHash`=`LogHash.Sha256Hex`. **입력 원문/삽입 본문 저장 금지**.
- **제외**: 실제 provider 콘텐츠(UX-WP-02), WPF(UX-WP-03), LLM(R4), 새 NuGet.

## 구현 세부 / 보안
- 모델 의존 0. 외부 호출 0. 동일 Context→동일 결과(결정성). 쓰기 = `logs/`만. 자동 실행 0.
- 더미 provider(테스트용)는 일반 토큰만(실데이터/실 테이블명/원문 0).

## 테스트 (SmokeTest, 외부 프레임워크 0)
- 동일 Context→동일 `CompletionResult`(결정성). 언어별 라우팅(SQL provider가 VBA Context에 미동작). 개수 상한. `Mode=="NoModel"`. accept 시 audit 1건 + **로그에 입력 원문 문자열 부재** 단언 + `UserHash`≠원문. `Total` 보존+신규(미분류 도메인 0 — 이름에 "completion"/"smart assist"/"suggestion" 등 분류 키워드 포함).

## 완료/보고
계약+코어+NoModel+해시 audit. build 0/0·SmokeTest `Total=N PASS/0 FAIL`·Gate A·NuGet 0 보고. `docs/39` UX-WP-01 DONE 요청.

## Claude Review Checklist
계약 명확(Engine/Context/Item/ICompletionProvider/Registry) / 결정성 / NoModel 동작 / accept 해시 audit(**원문 미저장**) / NuGet 0 / 기존 테스트 불변 / Gate A.
