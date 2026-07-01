# Codex Prompt — UX-WP-09: 동일 finding 이중 핀 정리 (A-4 — BlockedHint + SafetyHint 중복 표면 축소)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-09) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-09-same-finding-pin-dedup` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` UX-WP-09, `SKILLS.md`+`risk-ui-ux-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/Assist/{CompletionEngine,CompletionItem,CompletionResult}.cs`, `Core/Assist/Providers/StaticCompletionProviders.cs`, `Core/Safety/SafetyFinding.cs`(또는 정의 위치), `tests/RiskManagementAI.SmokeTests/AssistTests.cs`.
> **기준선**: main `10030be`(VERSION 0.7.0, #108~#111 머지 후), 정본 SmokeTest `Total=829 PASS=829 FAIL=0`.

## 0. 목표 (단일)
동일 finding에 대해 **BlockedHint(언어 provider)와 SafetyHint(SafetyHintProvider)가 둘 다 핀되어 2줄로 중복 표면**되는 것(기록된 nit A-4)을 **1줄로 축소**한다. **finding 자체는 계속 표면화**(findings 배열·잔존 핀 항목으로)되며, 삽입/트리거/accept/audit·정적성·NoModel 불변. **UX-WP-07(#110)이 A-4를 의도된 이중목적으로 보고 범위 밖으로 뒀으므로, 본 WP는 그 판단을 뒤집는 reviewable 변경이다 — 명확한 테스트 뒤에 두어 Claude가 승인/기각할 수 있게 한다.**

## 1. 배경 (현 동작)
- SQL Blocker(예: `DROP TABLE`) 입력 시:
  - `SqlCompletionProvider`가 **BlockedHint**(`Kind=BlockedHint`, Source=`static-sql`, Label `"Blocked SQL: {finding.Code}"`, `Finding=그 finding`) 방출.
  - `SafetyHintProvider`가 **SafetyHint**(`Kind=SafetyHint`, Source=`static-safety-hint`, Label `"Safety hint: {finding.Code}"`, `Finding=같은 finding`) 방출.
- dedupe 키(`Source+Label+Kind`, UX-WP-07)는 Source·Label·Kind가 모두 달라 **둘 다 생존** → 동일 finding이 2개 핀 항목으로 표면(A-4 잡음).
- 단, `CompletionResult.Findings`는 이미 `.Distinct()`(동일 finding 1건)라 **구조화 finding은 이미 중복 아님** — 문제는 **완성 리스트의 핀 항목 2줄**뿐.

## 2. 작업 범위 (`CompletionEngine`만)
1. `CompletionEngine.GetCompletions`에서 dedupe/정렬 후 **pinned 항목 집계 단계**에, **동일 finding identity를 참조하는 pinned 항목이 여럿이고 그 중 `BlockedHint`가 있으면 `SafetyHint`(들)를 제거하고 `BlockedHint`만 유지**한다.
   - finding identity = `SafetyFinding` 값 동등성(record면) 또는 `finding.Code`(안정 키). null Finding 핀(가드 fallback)은 그룹핑 대상 아님(그대로 유지).
   - `BlockedHint`가 없고 `SafetyHint`만 있는 finding은 **그대로 유지**(축소 대상 아님).
   - **결정적**: 원 정렬 순서 보존(그룹 내 유지 대상 선택도 안정적). `Dictionary` 열거 순서 의존 금지 — 순서는 정렬된 리스트 순회로 결정.
2. **불변 유지**: `pinnedItems.Concat(insertableItems)` 구조·`maxInsertableItems` cap(비핀 항목에만 적용)·삽입가능 항목 목록 불변. `CompletionResult.Findings` 산출(**cap 이전·dedupe 이후·Distinct**)은 그대로 — finding은 축소 후에도 전부 보존(핀 항목 축소가 findings 배열을 줄이지 않음).
3. provider·룰·트리거·팝업·audit **미변경**.

## 3. 제외 범위
provider 콘텐츠·severity 필터(UX-WP-07)·dedupe 키 자체 변경. 삽입가능(비핀) 항목 dedupe. WPF/팝업 표시. 신규 provider·랭킹(R4). 신규 NuGet. 룰/무결성/패키징.

## 4. 보안조건
정적·NoModel·자동삽입 0·accept 해시 audit 불변 · **finding 미손실**(축소 후에도 모든 finding이 `CompletionResult.Findings`에 존재) · 실데이터 0 · NuGet 0.

## 5. 테스트 (SmokeTest — 도메인 `Assist`)
> `SmokeTestContext.SmokeDomain` line ~60(`completion`/`smart assist`/`suggestion`/`provider`/`popup`/`assist`)로 분류. 신규 단언 설명에 이 토큰 사용, Kb 키워드(`검색`/`원문`/`공개`/`인용`·`document`·`source`·`approval`) 회피. `Unclassified=0`.
- **양성(축소)**: BlockedHint + SafetyHint가 **같은 finding**을 참조 → pinned 결과에 그 finding 핀 **1개(BlockedHint)** 만·SafetyHint 제거; **`CompletionResult.Findings`에는 그 finding 여전히 존재**(미손실).
- **비대상 보존**: SafetyHint만 있는 finding(BlockedHint 없음) → 그대로 유지. 서로 다른 finding 다건 → 각각 독립 축소·결정적 순서.
- **불변 회귀**: pinned 우선·cap(비핀만)·findings 절단 안전·삽입가능 목록 불변·accept 해시 audit 불변.
- 기존 `AssistTests`(엔진 결정성·언어 라우팅·개수 상한·SafetyHint pinned·dedupe·UX-WP-07 하이진) **전부 보존**·단언 감소 0.
- 종료부 **`Total=829 → 829+N PASS / 0 FAIL`**, `Unclassified=0`.

## 6. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+도메인 요약·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 변경 파일 · 양성/비대상 · **Applied Skill Checklists**.
- Branch `feature/ux-wp-09-same-finding-pin-dedup` · Commit: `fix: collapse duplicate blocked/safety pins for the same finding (UX-WP-09)`

## 7. Claude Review Checklist
동일 finding pinned 축소(BlockedHint 유지·SafetyHint 제거)·SafetyHint-only 보존·null Finding 그룹핑 제외 / **finding 미손실**(`CompletionResult.Findings` 전부 보존) / 결정적(정렬 순회·Dictionary 순서 비의존) / pinned 우선·cap(비핀만)·findings 절단 안전·삽입가능 목록 불변 / accept audit·정적·NoModel 불변 / 도메인 Assist·Unclassified 0 / 기존 AssistTests·UX-WP-07 보존·단언 감소 0 / NuGet 0 / `Total` 829 보존+신규 / Gate A. (이중 표면이 의도라 판단되면 기각 가능.)
