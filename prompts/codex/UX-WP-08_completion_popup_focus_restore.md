# Codex Prompt — UX-WP-08: Completion Popup Esc/Close 시 원본 TextBox 포커스 복원 (C-7)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-08) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-08-popup-focus-restore` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` UX-WP-08, `SKILLS.md`+`risk-ui-ux-review`·`risk-smoke-governance`, `src/RiskManagementAI.App/Controls/CompletionPopup.xaml.cs`, `src/RiskManagementAI.App/MainWindow.xaml.cs`(Ctrl+Space 배선·`RegisterCompletionTextBox`), `tests/RiskManagementAI.SmokeTests/UiContractTests.cs`.
> **기준선**: main `f8b330a`(VERSION 0.7.0), 정본 SmokeTest `Total=807 PASS=807 FAIL=0`.

## 0. 목표 (단일)
Completion Popup을 **Esc 또는 Close로 닫을 때 원본 편집 TextBox로 포커스를 복원**한다(기록된 nit C-7 — 현재 `OnCompletionListPreviewKeyDown`의 Esc 경로가 `Close()`만 호출하고 포커스를 원본에 돌려주지 않아 키보드 UX가 끊김). **삽입/트리거/accept/audit·자동삽입 0·Core 계약 불변.**

## 1. 작업 범위
1. `CompletionPopup.Show(TextBox placementTarget, …)`가 받은 **`placementTarget`을 필드로 보관**(예: `private TextBox? lastPlacementTarget;`). `RootPopup.PlacementTarget` 설정과 동일 시점.
2. `Close()`(및 Esc 경로 `OnCompletionListPreviewKeyDown`의 `Key.Escape`)에서 **팝업을 닫은 뒤 `lastPlacementTarget`이 non-null이면 해당 TextBox로 포커스 복원**(`Focus()`/`Keyboard.Focus(...)`). accept(Enter/Tab/DoubleClick) 경로는 기존대로 `TryAcceptSelected` 후 호출자(MainWindow)가 포커스를 다루므로 **중복 복원으로 커서 이동/재진입 유발 금지**(accept 경로는 변경 최소화; 필요 시 accept에서는 복원 생략).
3. `grabFocus:false`(as-you-type, UX-WP-05) 경로와 상호작용: 이때는 애초에 리스트 포커스를 뺏지 않으므로 **복원이 원본 커서를 흔들지 않아야** 한다(닫을 때만 복원, 이미 원본에 포커스면 no-op이도록).
4. Core(`RiskManagementAI.Core.Assist.*`) **계약·이벤트 시그니처 변경 0** — 본 WP는 App WPF 레이어 한정.

## 2. 제외 범위
Core 완성 로직·provider·랭킹(R4). 팝업 표시 포맷(UX-WP-06). 신규 완성 트리거. 신규 NuGet. WPF 외 파일.

## 3. 보안조건
정적·NoModel·외부 Editor 0·자동삽입 0 불변 · 실데이터 0 · NuGet 0 · accept 해시 audit(UX-WP-01) 불변 · 입력 원문 미저장.

## 4. 테스트 / 검증
> ⚠️ **WPF 포커스 이동은 SmokeTest(콘솔·비 UI)로 렌더 검증 불가 → 실 검증 = Gate B**(과대표기 금지: 실 Test PC 렌더 증거 전 VERIFIED 금지).
- **가능한 로컬 단언**: `UiContractTests`에 XAML/코드 계약 단언 추가 — `CompletionPopup.Show`가 placement target을 보관하는 계약(예: 신규 test-visible 속성/메서드로 마지막 target 노출, 또는 리플렉션 없이 관측 가능한 seam)·Esc 경로가 복원 진입점을 호출하는 구조. UI 실렌더 없이 검증 가능한 최소 seam만.
- seam이 순수 WPF 의존이라 콘솔 테스트가 불가하면 **테스트 증가 0**(단언 삭제·약화 0, `Total=807` 불변)로 두고, PR 본문에 **"실 검증=Gate B"**를 명시. 억지 UI 테스트를 만들지 않는다.
- 기존 `UiContractTests`/`AssistTests` **전부 보존**. 종료부 **`Total=807(+N) PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄 · Gate A 0 · PackageReference 0 · 변경 파일 · **Gate B 미충족 명시**(포커스 렌더) · **Applied Skill Checklists**.
- Branch `feature/ux-wp-08-popup-focus-restore` · Commit: `fix: restore origin TextBox focus on completion popup Esc/Close (UX-WP-08)`

## 6. Claude Review Checklist
Show가 placement target 보관 / Close·Esc에서 원본 TextBox 포커스 복원(이미 원본이면 no-op) / accept 경로 중복복원·재진입 유발 0 / `grabFocus:false`(as-you-type) 원본 커서 미교란 / Core 계약·이벤트 시그니처 불변 / 자동삽입·audit·정적성 불변 / **실 포커스 렌더=Gate B BLOCKED 명시(과대표기 금지)** / 억지 UI 테스트 0·기존 단언 보존 / NuGet 0 / `Total` 보존(+N) / Gate A.
