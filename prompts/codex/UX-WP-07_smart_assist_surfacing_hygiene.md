# Codex Prompt — UX-WP-07: Smart Assist Completion 표면화 하이진 (dedupe Kind 인지 · Info 힌트 필터 · allow-function 무음 누락 표면화)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-07) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-07-assist-surfacing-hygiene` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` UX-WP-07, `SKILLS.md`+`risk-ui-ux-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/Assist/{CompletionEngine,CompletionItem,CompletionResult}.cs`, `Core/Assist/Providers/StaticCompletionProviders.cs`, `tests/RiskManagementAI.SmokeTests/AssistTests.cs`.
> **기준선**: main `f8b330a`(VERSION 0.7.0), 정본 SmokeTest `Total=807 PASS=807 FAIL=0`.

## 0. 목표 (단일)
Smart Assist 완성 항목의 **표면화 하이진**을 저위험으로 개선한다: ① dedupe 키에 `Kind`를 포함해 동일 Source+Label의 **SafetyHint와 삽입가능 항목이 공존**(안전 힌트가 일반 항목을 삼키지 않음), ② `SafetyHintProvider`가 **순수 Info 심각도 finding까지 핀하지 않음**(핀은 Warning 이상), ③ `Excel2021CompletionProvider`의 allow-function 필터(`IsWorksheetFunctionName`)가 **비적합 라벨을 무음 누락하지 않고 warning으로 표면화**. **삽입/트리거/accept/audit·NoModel·정적성 불변.**

> 이 셋은 기록된 nit(UX-WP-01 dedupe·UX-WP-02 A-5/A-6)이며 **기능/보안 영향 0**의 하이진이다. **A-4(BlockedHint+SafetyHint 2줄)는 의도된 이중목적으로 보고 본 WP 범위 밖**(Claude 지시 시 별도). 각 변경은 명확한 테스트 뒤에 두어 Claude가 승인/기각(의도된 동작이면)할 수 있게 한다.

## 1. 작업 범위
1. **dedupe 키 Kind 포함** — `CompletionEngine.DedupeAndSort`의 키 `item.Source + "" + item.Label`(현 line ~101)에 `item.Kind`를 추가(`Source  Label  Kind`). 정렬(pinned 우선·SortKey·Label·Source·Kind)·`pinnedItems.Concat(insertableItems)`·`maxInsertableItems` cap·`findings` 산출 순서(**cap 이전·dedupe 이후, SafetyHint 절단 안전**)는 **불변**. 결과: 동일 Source+Label이라도 Kind가 다르면(SafetyHint vs 일반) 둘 다 생존. 완전 동일(Source+Label+Kind) 중복은 여전히 1건.
2. **Info 심각도 힌트 핀 제외** — `SafetyHintProvider.GetCompletions`(현 line ~220)가 **모든** finding을 SafetyHint로 방출하는 것을 **`Severity >= Warning`(즉 Info 제외)** 로 필터. Info성 finding(예: `SQL_EMPTY`)은 핀하지 않음. Blocker/High/Warning finding의 SafetyHint·구조화 Finding 보존·`Insertable=false`·`InsertText=""` 불변.
3. **allow-function 무음 누락 표면화** — `Excel2021CompletionProvider` 생성자(현 line ~115)의 `ExcelCompletionAllowFunctions.Where(IsWorksheetFunctionName)` 필터에서 **탈락한 라벨을 warning으로 노출**. 방법(둘 중 택1, 결정적): (a) 생성자에서 탈락 라벨을 모아 `GetCompletions` 결과의 `CompletionResult.Warnings`로 흘리거나(엔진 경유), (b) 탈락 시 Engine `warnings`에 `"Excel completion allow-function skipped: {label}"` 형태로 표면화. **허용 함수 목록·정렬·prefix 매칭·Insertable=true·SafetyNote 불변.** 실제 `rules/excel_2021_completion_allow_functions.txt`는 전부 적합(현 상태)일 수 있으므로, 합성 ruleset으로 비적합 라벨을 주입해 warning 경로를 테스트.

## 2. 제외 범위
A-4(BlockedHint+SafetyHint 이중 표면). 신규 provider·신규 완성 콘텐츠·랭킹/LLM(R4). WPF UI 렌더 변경. 신규 NuGet. 기존 룰/무결성/패키징 인벤토리 변경(신규 파일 0).

## 3. 보안조건
정적·NoModel·외부 Editor 0·자동삽입 0 불변 · 실데이터/원문 0 · NuGet 0 · 해시 accept audit(UX-WP-01 `SuggestionLogWriter`) 불변 · RuleSet 단일 원천 재사용(룰 중복정의 0).

## 4. 테스트 (SmokeTest — 도메인 `Assist`)
> `SmokeTestContext.ClassifyDomain` line 96(`completion`/`smart assist`/`suggestion`/`provider`/`popup`/`assist`)로 분류. 신규 단언 설명에 이 토큰 사용, Kb 키워드(`검색`/`원문`/`공개`/`인용`·`document`·`source`·`approval`·`metadata`) 회피. `Unclassified=0`.
- **dedupe Kind**: 동일 Source+Label·상이 Kind(SafetyHint + 일반 completion) 입력 → 둘 다 생존(안전 힌트가 일반 항목 미삼킴); 동일 Source+Label+Kind → 여전히 1건. pinned 우선·cap·findings 절단 안전 회귀.
- **Info 필터**: Info성 finding만 존재 시 SafetyHint completion 0(핀 없음); Warning/High/Blocker finding → SafetyHint 핀 유지·구조화 Finding 보존.
- **allow-function warning**: 합성 ruleset에 비적합 라벨 주입 → 해당 라벨 completion 미노출 + **warning 표면화**; 적합 라벨은 정상 노출.
- **기존 `AssistTests` 전부 보존**(엔진 결정성·언어 라우팅·개수 상한·SafetyHint pinned·accept 해시 audit·dedupe 기본). 단언 수 감소 0.
- 종료부 **`Total=807 → 807+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+도메인 요약·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 변경 파일 · 양성/음성 · **Applied Skill Checklists**.
- Branch `feature/ux-wp-07-assist-surfacing-hygiene` · Commit: `fix: smart assist completion surfacing hygiene — kind-aware dedupe, info hint filter, allow-function surfacing (UX-WP-07)`

## 6. Claude Review Checklist
dedupe 키 Kind 포함(SafetyHint·일반 공존, 완전중복 여전히 1건) / pinned 우선·cap·findings 절단 안전 불변 / Info finding 핀 제외(Warning 이상만)·구조화 Finding 보존 / allow-function 탈락 라벨 warning 표면화(결정적) / 정적·NoModel·자동삽입 0·accept audit 불변 / RuleSet 단일원천 / 도메인 Assist·Unclassified 0 / 기존 AssistTests 보존·단언 감소 0 / NuGet 0 / `Total` 807 보존+신규 / Gate A. (의도된 동작 판단 시 기각 가능.)
