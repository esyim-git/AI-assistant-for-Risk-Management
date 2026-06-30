# Codex Prompt — UX-WP-06: Smart Assist Completion Popup 표시 확장 (Snippet·SafetyNote·Kind, 자동삽입 0)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-06) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-06-smart-assist-popup-display`. Claude 승인 전 main 머지 금지.
> **선행 = UX-WP-05 머지(트리거).** 본 WP는 팝업 **표시**만 확장(트리거는 UX-WP-05).
> **선행 읽기**: `AGENTS.md §0·§3·§4·§8`, `SKILLS.md`, `.claude/skills/risk-ui-ux-review/SKILL.md`·`.claude/skills/risk-smoke-governance/SKILL.md`·`.claude/skills/risk-security-guard/SKILL.md`, `docs/39` UX-WP-06, `src/RiskManagementAI.App/Controls/CompletionPopup.xaml`(item template L24-47)·`CompletionPopup.xaml.cs`(Show/Close/TryAcceptSelected/IsCompletionOpen/ItemAccepted), `src/RiskManagementAI.Core/Assist/CompletionContracts.cs`(`CompletionItem`: `Label·InsertText·Kind·Source·RequiresReview·Insertable·SafetyNote·Finding·SortKey`), `src/RiskManagementAI.App/MainWindow.xaml.cs`(`AcceptCompletionItem`), `tests/RiskManagementAI.SmokeTests/AssistTests.cs`.
> **기준선**: main `e6fba98`(VERSION 0.7.0), `Total=747`. **⚠️ ID**: UX-WP-06(사용자 제안 "UX-WP-03 Smart Assist Popup"), 완료 UX-WP-03(#73) 팝업의 **표시 확장**(재구현 아님).

## 0. 목표 (단일)
완료된 `CompletionPopup`(UX-WP-03 #73)은 item template(`CompletionPopup.xaml:24-47`)에 **Label·Source·Kind·RequiresReview + "검토용 초안" badge**만 렌더하고, **Snippet 본문 미리보기·`SafetyNote`(이미 `CompletionItem`에 있으나 UI 미사용)·Kind 구분 표식이 없다.** Gate B B-5의 "snippet 자동완성" 기대를 충족하도록 팝업 **표시를 확장**한다 — **삽입/트리거 동작은 불변**(자동삽입 0, 선택 시에만 삽입).

> **STOP**: 외부 NuGet·모델 0. 표시(XAML/바인딩)만, 새 데이터 소스/추천 알고리즘 0.

## 1. 작업 범위 (팝업 표시 한정)
1. **Snippet kind 구분 표시**: `CompletionItemKind.Snippet` 항목을 keyword/function과 **시각적으로 구분**(라벨/아이콘/배지)하고, 필요 시 `InsertText` 미리보기(여러 줄 snippet 요약)를 **읽기전용**으로 표시.
2. **`SafetyNote`·`Finding` 표면화**: `CompletionItem.SafetyNote`(현재 UI 미바인딩)와 안전 관련 표식을 팝업에 렌더(특히 SafetyHint/BlockedHint). **비-insertable 항목은 findings-only 유지**(삽입 버튼/Enter 삽입 0).
3. **Kind 라벨/정렬 가독성**: Kind(Keyword/Snippet/Function/Phrase/SafetyHint/BlockedHint)별 라벨·아이콘. 기존 정렬(safety-pinned 우선·SortKey·Label) 표시 순서 불변.
4. **삽입/트리거 동작 불변**: `Show`/`Close`/`TryAcceptSelected`/`IsCompletionOpen`/`ItemAccepted` 계약·`AcceptCompletionItem` 삽입·accept 해시 audit **그대로**. **자동삽입 0**(선택 시에만), **non-insertable은 절대 삽입 0**(`Insertable` 게이트 보존).
5. (선택) caret-relative placement 개선: WPF `TextBox.GetRectFromCharacterIndex`로 caret 근처 배치. 복잡하면 현행 placement 유지(범위 밖 가능, 보고 명시).

## 2. 제외 범위
as-you-type 트리거(UX-WP-05). 신규 provider/추천 알고리즘·실시간 LLM 랭킹(R4 STOP). 자동삽입. 매칭 로직 변경(substring/fuzzy = Core 변경, 범위 밖). Vector/모델/NuGet.

## 3. 보안조건
- NoModel·외부 API·NuGet·모델 0 · 자동삽입 0 · non-insertable 삽입 0(`Insertable` 게이트 보존).
- 표시 텍스트는 정적 provider seed(인박스)만 — 실데이터/원문 0. 입력 원문 미저장(accept 해시 audit 불변).
- `RequiresReview`/"검토용 초안"·findings 표식 유지(검토 유도, 자동 신뢰 0).

## 4. 테스트 (SmokeTest, 외부 프레임워크 0)
- 표시 가능 로직(예: Kind→표시 라벨 매핑·SafetyNote 노출 여부·Insertable 게이트)을 **순수 헬퍼/ViewModel**로 두어 단언(WPF 렌더는 Gate B). non-insertable(SafetyHint/BlockedHint) 삽입 0 회귀·`CompletionItem` 계약 불변.
- 기존 `AssistTests` 보존. 종료부 **`Total=747(또는 UX-WP-05 후 수치) → +N PASS / 0 FAIL`**, `Unclassified=0`, build 0/0.
- WPF 팝업 실제 렌더·caret placement = **Gate B(Test PC)**.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄 · Gate A · 변경 파일 · 케이스 · **"Applied Skill Checklists"**(`risk-ui-ux-review`·`risk-smoke-governance`·`risk-security-guard`).
- Branch `feature/ux-wp-06-smart-assist-popup-display` · Commit: `feat: Smart Assist popup display — snippet/SafetyNote/kind (insert-on-select only) (UX-WP-06)`

## 6. Claude Review Checklist
표시(XAML/바인딩)만·트리거/삽입 동작 불변 / Snippet 구분·`SafetyNote` 표면화·Kind 라벨 / **자동삽입 0·non-insertable 삽입 0(`Insertable` 게이트)** / `Show/Close/TryAcceptSelected/ItemAccepted` 계약·accept 해시 audit 불변 / 매칭/엔진 Core 변경 0 / NoModel·NuGet 0·실데이터 0 / 표시 로직 순수 헬퍼 SmokeTest·기존 `AssistTests` 보존 / `Total` 보존+신규 / Gate A. **프롬프트**: 본 파일.
