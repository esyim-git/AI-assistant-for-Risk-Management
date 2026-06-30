---
name: risk-ui-ux-review
description: Review WPF UX, resizable SQL/VBA editor layout, Smart Assist, completion popup, safety hint panels, and 1180x720 to 2560x1440 usability.
allowed-tools: Read Grep Glob Bash(git diff *)
paths:
  - "src/RiskManagementAI.App/**"
  - "src/RiskManagementAI.Core/Assist/**"
  - "docs/**"
---

# UI/UX Review

## 목적
WPF UX·SQL/VBA 입력창·Resizable Layout·Smart Assist를 사용성과 절대 원칙(외부 Editor/NuGet 0·NoModel·자동삽입/자동실행 0·해시 audit) 관점에서 읽기 전용으로 검토한다. 코드 동작을 바꾸지 않는 **점검/체크리스트 가이드**다.

## 언제 사용
- `src/RiskManagementAI.App/**`(`MainWindow.xaml`/`.xaml.cs`), `src/RiskManagementAI.Core/Assist/**`, `docs/**`(특히 docs/46·ADR-010) 작업 시.
- 트리거 예: "UX 리뷰", "레이아웃 점검", "입력창 확장", "GridSplitter", "Smart Assist", "completion popup", "Safety 패널".

## 체크
- **입력창 확장**: SQL/VBA/Excel/리스크 코멘트 편집 영역이 창 리사이즈에 비례 반응하고 최대화 시 확장. EditorRow는 **고정 `Height="260"`이 아닌** `2*`/`MinHeight` 가변.
- **GridSplitter**: 에디터↔결과(`EditorResultSplitter`), 중앙↔우측 Safety(`WorkspaceSafetySplitter`) 분할 존재, `ResizeBehavior`/`ResizeDirection` 명시.
- **Safety Panel 폭 조정**: `SafetyPanelColumn` `Width="340" MinWidth="280" MaxWidth="560"`. 좌측 메뉴 컬럼 고정·중앙 `Width="*"`.
- **창 Min·해상도 기준**: Window `MinWidth=1180`/`MinHeight=720`(최소), **2560×1440 기준** 사용성 확보.
- **TextBox stretch**: SQL/VBA TextBox `Stretch`(가로/세로 채움) + `Consolas`/`14`.
- **Smart Assist 자동삽입 금지**: Ctrl+Space 트리거 추천, Enter/Tab 삽입, Esc 닫기, **자동 삽입 없음**. 추천은 정적·결정적(`ICompletionProvider`/`CompletionEngine`), NoModelMode 완전 동작.
- **Safety Hint 우선**: `SafetyHint`/`BlockedHint`·`CompletionResult.Findings`는 top-N 절단으로 누락 금지.
- **입력 원문 로그 저장 금지**: accept audit은 해시 전용(`SuggestionLog*`: InsertTextHash 등), 입력 원문/삽입 본문 미저장.
- **레이아웃 영속화**: STAB-UX-02는 레이아웃만 저장(데이터/입력 텍스트 미저장), `config/` 한정, 손상→기본값 fallback.

## 절대 원칙 (STOP)
- **외부 Editor/Completion 패키지(AvalonEdit·ScintillaNET·RoslynPad 등) NuGet 0** — WPF 기본 `TextBox`+`Popup`/`ListBox` 자체 구현. (ADR-010)
- **NoModel 기본 유지** — 모델 기반 랭킹/생성은 R4 Model Approval Gate 이후로 STOP/연기. Vector/Embedding/LLM Runtime/모델파일 = STOP → 승인 문서 후에만.
- **SQL/VBA 자동 실행 0**(텍스트 제안만), 모든 추천 `RequiresReview`(검토용 초안). seed/snippet에 실데이터·실 테이블명·내부규정 원문 0.
- **Safety 룰 중복정의 금지** — 위험 판단은 기존 `SqlSafetyChecker`/`VbaSafetyChecker`/`Excel2021FunctionChecker`+RuleSet 경유.

## 참조
- `docs/46_Smart_Assist_Design.md`(Smart Assist 설계) · `docs/40_ADR_Architecture_Evolution.md` ADR-010(WPF-native Inline Completion, 정적·NoModel)
- `docs/39_Work_Package_Backlog.md` STAB-UX-01(Resizable Layout)·STAB-UX-02(레이아웃 영속화)·UX-WP-01~03(Smart Assist Core·정적 Provider·Completion Popup)
- 관련 코드: `src/RiskManagementAI.App/MainWindow.xaml(.cs)`, `src/RiskManagementAI.Core/Assist/{CompletionEngine,CompletionProviderRegistry,ICompletionProvider,SuggestionLog*}.cs`, `Assist/Providers/StaticCompletionProviders.cs`
- 연계 스킬: `/risk-security-guard`(원문/모델파일 차단) · `/risk-llm-approval`(LLM 랭킹 STOP·승인 게이트)
