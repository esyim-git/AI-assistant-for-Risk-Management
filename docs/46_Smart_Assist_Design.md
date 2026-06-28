# 46. Smart Assist / Inline Assist 설계 (UX Assist Track)

> 권위 스펙. WP는 `docs/39`(UX-WP-01~03), 아키텍처 결정은 `docs/40`(ADR-010), 로드맵 배치는 `docs/38`(UX Track), 게이트는 `docs/41`·`docs/28`.
> 절대 원칙 전제(Offline · 외부 NuGet 0 · 외부 API/Telemetry/AutoUpdate 0 · **NoModelMode** · 해시 Audit · SQL/VBA/Golden6 자동실행 0 · 실데이터/실 테이블·컬럼명/내부규정·NCR 원문/모델파일 repo 미포함).

## 1. 목적
사용자가 SQL / VBA / Excel 수식 / 리스크 코멘트를 **직접 타이핑하는 중에** 자동완성·snippet·추천 문구·실시간 안전 힌트를 제공한다. 사용자의 작성 속도와 정확성을 돕되, **생성을 대신하지 않고 결정을 대신하지 않는다.**

## 2. 범위 (Capability)
| Cap-ID | Capability | WP |
|---|---|---|
| CAP-UX-01 | Smart Assist Core (Engine·Context·Item·Provider 계약·Registry) | UX-WP-01 |
| CAP-UX-02 | SQL Completion Provider (조회 전용 keyword·snippet) | UX-WP-02 |
| CAP-UX-03 | VBA Completion Provider (안전 snippet) | UX-WP-02 |
| CAP-UX-04 | Excel 2021 Completion Provider (+365 차단 힌트) | UX-WP-02 |
| CAP-UX-05 | Safety Hint Provider (기존 Safety Checker 재사용) | UX-WP-02 |
| CAP-UX-06 | Risk Phrase Provider (검토용 초안 문구 seed) | UX-WP-02 |
| CAP-UX-07 | WPF Completion Popup (Ctrl+Space·선택 삽입) | UX-WP-03 |
| CAP-UX-08 | Accepted Suggestion Audit (해시 전용) | UX-WP-01/03 |

## 3. 제외 범위
- **SQL/VBA 전체 생성 기능과 별개다** — 기존 `DraftPipeline`/`NoModelDraftService`(전체 초안 생성)는 그대로 두고 건드리지 않는다. Smart Assist는 **입력 보조(inline)** 전용.
- **Local LLM 기반 추천**(랭킹/문맥 생성) → **R4 Model Approval Gate 이후로 연기**(ADR-010, ADR-003/009). 현재는 정적 provider만.
- 외부 Editor/Completion 패키지(AvalonEdit·ScintillaNET·RoslynPad 등) **도입 금지**(NuGet 0).
- 자동 삽입·자동 실행 없음. 실 테이블/스키마 introspection 없음(운영 DB 접속 0).

## 4. 아키텍처

### 4.1 데이터/계약 (Core, `RiskManagementAI.Core.Assist`)
```text
enum CompletionLanguage { Sql, Vba, Excel, RiskComment }
enum CompletionItemKind { Keyword, Snippet, Function, Phrase, SafetyHint, BlockedHint }

record CompletionContext(
    CompletionLanguage Language,
    string Text,          // 현재 입력 전체(휘발성 — 로그 저장 금지)
    int CaretIndex,
    string Prefix,        // 커서 앞 현재 토큰
    string Mode);         // "NoModel" (정적)

record CompletionItem(
    string Label,         // 목록 표시
    string InsertText,    // 선택 시 삽입 텍스트
    CompletionItemKind Kind,
    string Source,        // ProviderId
    bool RequiresReview,  // 검토용 초안 표식
    string? SafetyNote,   // 안전 주의(있으면)
    int SortKey);         // 결정적 정렬

interface ICompletionProvider {
    string ProviderId { get; }
    bool Supports(CompletionLanguage language);
    IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context); // 순수·결정적·I/O 0
}

record CompletionResult(IReadOnlyList<CompletionItem> Items, string Mode, IReadOnlyList<string> Warnings);
```

### 4.2 Engine·Registry
- `CompletionProviderRegistry` — provider 등록/언어별 조회(결정적 순서).
- `CompletionEngine.GetCompletions(CompletionContext) → CompletionResult`:
  1. 언어에 맞는 provider들을 registry에서 가져와 각각 `GetCompletions` 호출(외부 호출·모델 0).
  2. 결과 병합 → 중복 제거(ProviderId+Label) → **결정적 정렬**(SortKey, then Label Ordinal) → 개수 상한(예: 50) 적용.
  3. `Mode = "NoModel"`. 동일 Context → 동일 결과(결정성, 테스트 가능).
- **NoModelMode 동작**: Engine은 모델 의존이 전혀 없다. 모든 추천은 정적 provider 산출물이며, 모델 미존재 상태에서 완전 동작한다.

### 4.3 Providers (정적·in-box)
- **SqlCompletionProvider** (CAP-UX-02): 조회 전용 keyword(`SELECT/FROM/WHERE/JOIN/GROUP BY/ORDER BY/HAVING/ON/AS` 등)·snippet(`SELECT ... FROM ... WHERE BASE_DT = :BASE_DT`). 차단 DML/DDL(`INSERT/UPDATE/DELETE/MERGE/CREATE/ALTER/DROP/TRUNCATE/GRANT/REVOKE/EXEC/CALL/COMMIT/ROLLBACK`)은 추천하지 않고, 입력되면 `BlockedHint`. 차단 목록은 **기존 `SqlSafetyChecker`/RuleSet 재사용**(중복 정의 금지).
- **VbaCompletionProvider** (CAP-UX-03): 안전 snippet(`Option Explicit` 헤더·에러 처리 블록·배열 기반 루프·`Application` 상태 저장/원복). 금지 API(`Shell/WScript.Shell/Kill/FileSystemObject 삭제·이동/Declare PtrSafe/WinAPI/Outlook 발송/외부 URL`)는 추천 0, 입력 시 `BlockedHint`.
- **Excel2021CompletionProvider** (CAP-UX-04): Excel 2021 허용 함수(`XLOOKUP/XMATCH/FILTER/SORT/SORTBY/UNIQUE/SEQUENCE/LET/SUMIFS/COUNTIFS/INDEX/MATCH`). **Excel365BlockedHintProvider**: 365 전용(`VSTACK/HSTACK/TOCOL/TOROW/TEXTSPLIT/TEXTBEFORE/TEXTAFTER/GROUPBY/PIVOTBY/MAP/REDUCE/BYROW/BYCOL/REGEX*`) 입력 시 **2021 대체안 + `BlockedHint`**(CLAUDE.md §6). 함수 목록은 **기존 `Excel2021FunctionChecker`/RuleSet 재사용**.
- **SafetyHintProvider** (CAP-UX-05): 입력 텍스트를 기존 `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`에 통과시켜 위험/비호환을 `SafetyHint`(또는 우측 결과 패널 finding)로 실시간 노출. **룰 재구현 금지** — 동일 RuleSet 경유.
- **RiskPhraseProvider** (CAP-UX-06): 리스크 코멘트용 **일반 문구 seed**(예: "기준일 기준 노출 합계", "한도 초과 항목 후속 점검 필요"). 모두 `RequiresReview=true`. **실 내부규정 문구/실데이터/실 계정·테이블명 금지** — 일반 표현만.

### 4.4 UI (App, CAP-UX-07)
- 재사용 `CompletionPopup`(`Popup` + `ListBox`)을 SQL/VBA/Excel 입력 `TextBox`에 부착.
- **Ctrl+Space** → 포커스된 박스 언어로 `CompletionEngine.GetCompletions` → Popup 표시.
- **Enter/Tab** → 선택 항목 `InsertText`를 커서 위치에 삽입. **Esc** → 닫기. **자동 삽입 없음**(명시 선택 시에만).
- 각 항목에 **Source · Kind · RequiresReview** 표시. Safety finding은 **기존 우측 결과 패널**(`ShowFindings`)과 연계.

### 4.5 Accepted Suggestion Audit (CAP-UX-08)
- 추천이 **삽입(accept)** 될 때만 해시 audit 1건 기록(`TaskLogWriter` 패턴 재사용):
  `SuggestionLogEntry { SuggestionId, ProviderId, Language, Mode, UserHash, AcceptedAtUtc }`.
- `SuggestionId` = provider+Label의 **결정적 해시**(원문 아님). `UserHash` = `LogHash.Sha256Hex(user)`. **입력 원문/삽입 본문 전체 저장 금지**.

## 5. 보안 유의사항
- 로그: **suggestion id · provider · mode · user hash · 시각만**. 입력 원문 전체·삽입 본문 미저장(해시 audit 원칙).
- seed/snippet에 **실 테이블명·내부규정 원문·실데이터 금지**(일반 더미·일반 표현만, `RISK_EXPOSURE_DAILY` 류 일반명 한도).
- 모든 추천은 **검토용 초안**(`RequiresReview`)로 표시. 추천은 **자동 삽입·자동 실행 0**.
- SQL/VBA **자동 실행 절대 금지**(완성기는 텍스트만 제안). 운영 DB 접속·스키마 introspection 0.
- 외부 API/NuGet/모델 0(NoModel). 쓰기 경로는 `logs/` audit만.

## 6. 테스트 기준 (SmokeTest, 외부 프레임워크 0)
- **Engine**: 동일 Context → 동일 결과(결정성), 언어별 provider 라우팅, 개수 상한, NoModel 동작.
- **Providers**: SQL 차단 DML 미추천 + `BlockedHint`, VBA 금지 API 미추천, Excel 2021 허용/365 차단+대체 힌트, SafetyHint가 기존 Checker와 동일 판정, RiskPhrase 전부 `RequiresReview`·실데이터 0.
- **Audit**: accept 시 1건 기록, 원문 미저장(원문 문자열 부재 단언), `UserHash` 비원문.
- **UI 계약**(가능 범위): 자동 삽입 없음(선택 시에만 InsertText), 항목에 Source/Kind/RequiresReview 노출.

## 7. 향후 확장
- **R4 이후**: Local LLM 기반 **랭킹/문맥 추천**을 Adapter(ADR-003) 뒤에 옵션으로 추가(승인·STOP 게이트 후). 기본은 항상 정적 provider + NoModel.
- 승인된 KB/규정 catalog를 RiskPhrase의 **참조형 출처**로 연결(원문 미포함 유지, R3/KB 계약 경유).
- 사용자 승인형 snippet 승격(Feedback Learning, R5)와 연계 가능(가중치 자동학습 0).

> 관련: `docs/40`(ADR-010·ADR-003/009)·`docs/39`(UX-WP-01~03)·`docs/38`(UX Track)·`CLAUDE.md §4·§5·§6·§10`·`docs/14`(UI)·`docs/16`(VBA)·`docs/28`(Gate A).
