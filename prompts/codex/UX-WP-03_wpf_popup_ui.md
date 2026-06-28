# Codex UX-WP-03 — WPF Completion Popup UI Integration

> 권위 스펙: `docs/39 §UX-WP-03`, `docs/46`, `docs/40`(ADR-010). 선행: **UX-WP-01, UX-WP-02**. 우선순위: `AGENTS.md` > `docs/39` > 본 프롬프트.
> WPF **기본 TextBox + Popup/ListBox** 자체 구현. 외부 Editor/Completion 패키지 **금지**(NuGet 0). **자동 삽입·자동 실행 0**.

## 현재 문제 / 목표
Core 엔진/Provider(UX-WP-01/02)를 SQL/VBA/Excel 입력창에 연결한다 — **Ctrl+Space** 추천 Popup, **Enter/Tab** 삽입, **Esc** 닫기, 항목에 Source·Kind·RequiresReview 표시, Safety finding은 기존 결과 패널 연계.

## 먼저 읽기
`AGENTS.md`, `docs/46`, `docs/14`(UI), `App/MainWindow.xaml`·`MainWindow.xaml.cs`(기존 SQL/VBA/Excel 입력창·`ShowFindings`·`FindingDisplay`), UX-WP-01 `CompletionEngine`, UX-WP-02 provider 등록.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/ux-wp-03-wpf-popup origin/main   # UX-WP-01/02 머지 후
```
- .NET 8 WPF(`net8.0-windows`). Gate A. **NuGet 0**. **로컬 build+run 검증 필수**(WPF는 windows 컴파일).

## 작업 범위 (App)
1. 재사용 컨트롤 `App/Controls/CompletionPopup.xaml(.cs)` — `Popup` + `ListBox`. ItemTemplate에 **Label · Source · Kind · RequiresReview(검토용 초안 배지)** 표시.
2. **SQL·VBA·Excel·리스크 코멘트(RiskComment) `TextBox` 4종 모두**에 부착 — **Ctrl+Space**: 박스 언어로 `CompletionContext` 구성 → `CompletionEngine.GetCompletions` → Popup 표시. **Enter/Tab**: 선택 항목 삽입. **Esc**: 닫기. (`CompletionLanguage` 4종 전부 UI 연결 — RiskComment 누락 금지.)
3. **자동 삽입 없음** — 타이핑만으로 삽입 0(명시 선택 시에만). **`Insertable=false`(SafetyHint/BlockedHint) 항목은 선택해도 삽입 0**(정보 표시만), `Insertable=true`만 `InsertText` 삽입. accept(삽입) 시 UX-WP-01 audit 호출(해시 전용).
4. Safety finding(`SafetyHint`/`BlockedHint`)은 항목의 **구조화 `Finding`을 그대로** 기존 우측 결과 패널(`ShowFindings`)로 전달(평문화 금지).
- **제외**: Core 로직 변경(UX-WP-01/02), LLM, 자동 삽입/실행, 새 NuGet, 외부 Editor 패키지.

## 구현 세부 / 보안
- 추천 표시는 `CompletionEngine` 결과만(앱이 룰/추천 자체 생성 금지). **자동 삽입·자동 실행 0**. 입력 원문/삽입 본문 **로그 미저장**(audit=id/provider/mode/userHash). Application 상태 안전.

## 테스트 (Windows 로컬)
- **4종 입력창(RiskComment 포함) 연결**. 자동 삽입 없음(타이핑→삽입 0; Insertable 항목 선택 시에만 InsertText). **비삽입 힌트(SafetyHint/BlockedHint) 선택 시 삽입 0** 단언. 항목에 Source/Kind/RequiresReview 노출. 구조화 Finding 결과패널 전달. (UI는 가능 범위에서 계약/뷰모델 단위 테스트 — 이름에 `Assist`/`completion` 등 분류 키워드 → `Unclassified=0`.) `Total` 보존+신규.

## 완료/보고
4종 입력창 Ctrl+Space/Enter·Tab/Esc + 자동삽입 없음 + 비삽입 힌트 + 감사 연계. **로컬** build 0/0·SmokeTest `Total=N PASS/0 FAIL`·Gate A·NuGet 0. `docs/39` UX-WP-03 DONE 요청.

## Claude Review Checklist
외부 Editor 패키지 0(WPF 기본만) / **4종 입력창(RiskComment 포함)** / **자동 삽입 없음** / **비삽입 힌트** / Source·Kind·RequiresReview 노출 / **구조화 Finding 결과패널** / 입력 원문 미저장 / 자동 실행 0 / NuGet 0 / 기존 테스트 불변 / Gate A.
