# Codex Prompt — UX-WP-05: Smart Assist 입력중(as-you-type) 추천 표면화 (정적·NoModel, WPF 트리거만)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(UX-WP-05) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/ux-wp-05-smart-assist-as-you-type`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3·§4·§8`, `SKILLS.md`, `.claude/skills/risk-ui-ux-review/SKILL.md`·`.claude/skills/risk-smoke-governance/SKILL.md`·`.claude/skills/risk-security-guard/SKILL.md`, `docs/39` UX-WP-05, `docs/48 §B′`(Gate B B-5), `docs/46_Smart_Assist_Design.md`(있으면), `src/RiskManagementAI.App/MainWindow.xaml.cs`(특히 `RegisterCompletionTextBox`·`ShowCompletionPopup`·`ExtractCompletionPrefix`·`OnCompletionTextBoxPreviewKeyDown`·`AcceptCompletionItem`·`InsertCompletionText`), `src/RiskManagementAI.App/Controls/CompletionPopup.xaml(.cs)`, `src/RiskManagementAI.Core/Assist/{CompletionEngine,CompletionContracts,CompletionProviderRegistry}.cs`, `tests/RiskManagementAI.SmokeTests/AssistTests.cs`.
> **기준선**: main `e6fba98`(VERSION 0.7.0), 정본 SmokeTest `Total=747 PASS=747 FAIL=0`.
> **⚠️ ID 주의**: 완료·머지된 UX-WP-01/02/03(#70/#72/#73)과 충돌 방지로 본 WP는 **UX-WP-05**(사용자 제안 "UX-WP-02 Smart Assist Core").

## 0. 목표 (단일)
현재 Smart Assist는 **Ctrl+Space 명시 호출 전용**(`RegisterCompletionTextBox`가 `PreviewKeyDown`만 연결, `TextChanged` 없음)이라 입력 중 추천이 뜨지 않는다 — **이는 설계상 정상(by-design), 회귀 아님**(`docs/39` UX-WP-03 "자동삽입 없음"·Ctrl+Space-only). Gate B B-5의 "입력중 추천/snippet 자동완성 없음"을 **정적 범위 내 as-you-type 표면화**로 **신규 추가**한다. **기존 `CompletionEngine`은 이미 stateless·prefix-filtered·NoModel이라 Core 변경 0** — 본 WP는 **WPF 트리거 배선만**.

> **STOP**: 외부 NuGet·Vector·Embedding·LLM·모델파일 0. **실시간 LLM 랭킹/학습 = R4(STOP, 미구현)** — 도입 금지. 디바운스는 인박스 `DispatcherTimer`로(신규 의존성 0).

## 1. 작업 범위 (WPF 레이어 한정 — Core 엔진 변경 0)
1. **디바운스 TextChanged 트리거 추가**: `RegisterCompletionTextBox`(MainWindow.xaml.cs:173-177)에 **debounced `TextChanged`** 핸들러를 추가해, 기존 `ShowCompletionPopup(textBox, language)`(line 207)의 **엔진/결과(`GetCompletions`)·안전핀·Findings 경로를 재사용**하되 **팝업 표시는 아래 focus-preserving show**로 연다(Ctrl+Space 경로는 현행 그대로). 디바운스 = 인박스 `DispatcherTimer`(예: 150~250ms, 마지막 입력 후 1회) — **신규 NuGet 0**.
   - **포커스 불탈취(필수) — 신규 focus-preserving show path 명시**: ⚠️ 현재 `CompletionPopup.Show`(Controls/CompletionPopup.xaml.cs:22-34)는 **무조건 `CompletionList.Focus()`(line 32)로 ListBox에 focus를 잡는다** → as-you-type가 이를 그대로 재사용하면 **입력 중 TextBox focus를 빼앗아 타이핑이 끊긴다**(기존 "non-focusing 동작"은 존재하지 않음 — 새로 만들어야 함). 따라서 **as-you-type 경로는 focus를 옮기지 않는 show path를 쓴다**: `Show`에 `grabFocus` 파라미터/오버로드(또는 별도 non-focusing show 메서드)를 추가해, as-you-type는 **focus·캐럿을 TextBox에 유지**한 채 팝업만 연다(`SelectedIndex=0`). **`CompletionList.Focus()`(focus 이동)는 명시적 Ctrl+Space 경로에서만**(현행 #73 동작 유지). as-you-type 팝업 네비는 **TextBox `OnCompletionTextBoxPreviewKeyDown`(179-205)에서 `IsCompletionOpen`일 때 처리** — **Up/Down=선택 이동**(`CompletionPopup`에 `MoveSelection(delta)` 류 추가; 현재 Up/Down 미처리), Enter/Tab=수락(현 186-189), Escape=닫기(현 192-198). **focus는 as-you-type 동안 TextBox를 떠나지 않는다.**
2. **최소 prefix 게이트 + stale 닫기(필수)**: as-you-type는 **`ExtractCompletionPrefix`(308-323)·`IsCompletionPrefixChar`(325-328) 재사용**해 산출한 prefix 길이가 **임계(예 ≥2자) 이상이고 매칭이 있을 때만** 트리거한다. (empty/짧은 prefix는 `MatchesPrefix`가 **전체 항목**을 반환하므로 매 키 입력마다 전체 덤프 방지.) **prefix가 임계 미만(빈/짧음)이거나 매칭 0이면, 열려 있던 as-you-type 팝업을 닫는다**(stale 표시 금지 — 타이핑/삭제로 조건이 깨지면 즉시 Close). **Ctrl+Space는 현행 유지**(임계 무관, 전체 표시 가능).
3. **재진입 가드(필수)**: `TextChanged`는 **프로그램적 편집에도 발화**한다 — `InsertCompletionText`(299-306)·`AcceptCompletionItem`(237-279)이 `textBox.Text`를 세팅하므로, **suppress 플래그**로 그 구간의 `TextChanged`가 다시 팝업을 열지 않게 한다(수락 직후 재트리거 0).
4. **결정 로직 = 순수 헬퍼로 추출 (반드시 `Core`/net8.0·비-WPF)**: "as-you-type 트리거/유지 여부"(prefix 길이 임계·suppress·언어 지원·매칭유무 → should-show bool) 판정을 **`Core`(net8.0, 비-WPF) 순수 static 메서드**(예: `Core/Assist/`)로 둔다. **`App`(net8.0-windows/WPF)에 두지 않는다** — SmokeTest 프로젝트(net8.0 콘솔)는 `App`을 참조/실행할 수 없으므로, 단언 가능하려면 헬퍼가 **Core**에 있어야 한다. `App`의 `TextChanged` 핸들러는 이 Core 헬퍼를 호출만 한다(WPF 코드에는 분기 로직 최소화). 실제 팝업 렌더·포커스·디바운스 타이밍은 SmokeTest 불가 → **Gate B(Test PC)** 의존.
5. **기존 동작 불변**: Ctrl+Space(200-204)·팝업 네비(Enter/Tab/Escape 186-198)·수락 삽입(`AcceptCompletionItem`)·accept 해시 audit(`AppendSuggestionAudit`→`SuggestionLogWriter`, 281-297) **전부 보존**. 자동삽입 0(여전히 선택 시에만 삽입).
6. **표면화(surface)만으로는 audit 추가 0**: as-you-type가 팝업을 열고/필터링만 할 때는 **audit 신규 0**(accept만 기록 — 로그 스팸/프라이버시 방지). (별도 surface audit는 본 WP 범위 밖; `FromAcceptedItem`이 non-insertable에 throw하고 `AcceptedAtUtc` 의미가 accept-전용이라 재사용 부적합.)
7. **정적·NoModel 보존**: 컨텍스트는 기존대로 `CompletionEngine.NoModelMode`(MainWindow.xaml.cs:214-215)로 빌드. 모델/네트워크/임베딩 도입 0.

## 2. 제외 범위
`CompletionEngine`/providers/contracts **Core 변경**(이미 충분). 신규 provider·실시간 LLM 랭킹(R4 STOP)·자동삽입·surface audit·**팝업 표시 콘텐츠 enrich(Snippet/SafetyNote/Kind 렌더 = UX-WP-06)**·caret-relative placement(UX-WP-06). Vector/모델/NuGet. (단 **focus-preserving show path·`MoveSelection`(상호작용 동작)는 본 WP 범위** — UX-WP-06는 표시 콘텐츠만.)

## 3. 보안조건 (risk-ui-ux-review · risk-security-guard · risk-smoke-governance)
- NoModel 유지(Mode=NoModel) · 외부 API·NuGet·모델 0 · 자동삽입 0 · 자동학습 0.
- **입력 원문 미저장** — surface 시 audit 0, accept 해시 audit(`InsertTextHash`/`UserHash`, `logs/`만) 재사용. 평문 텍스트 디스크 미저장 불변.
- 디바운스/트리거가 **입력 지연·UI 멈춤 유발 0**(마지막 입력 후 1회, 무거운 작업 없음). 프로그램적 편집 재진입 0.
- **as-you-type 팝업은 TextBox focus·캐럿 미탈취**(키 입력 끊김 0) · prefix 임계 미만/매칭 0 시 **stale 팝업 Close**(낡은 추천 표시 0).
- 기존 안전핀(SafetyHint/BlockedHint Insertable=false, findings-only) 동작 불변.

## 4. 테스트 (SmokeTest, 외부 프레임워크 0 — 도메인 `UiContract` 또는 적정)
- **결정 헬퍼 단언**(`Core`/net8.0, WPF 비의존): prefix 길이 < 임계 → should-show=false · prefix ≥ 임계 + 매칭 → should-show=true · **매칭 0 → should-show=false(호출부가 기존 팝업 Close)** · suppress(프로그램적 편집) 중 → false · 미지원 언어/빈 텍스트 → false · Ctrl+Space 경로는 임계 무관(분리) 단언.
- **회귀 보존**: 기존 `AssistTests`(엔진 결정성·cap·dedupe·accept 엔트리·prefix 필터) **전부 PASS**. accept audit 해시 전용 불변.
- WPF 팝업 실제 표시·**포커스 불탈취**·디바운스 타이밍·stale Close 렌더 = **Gate B(Test PC)**(SmokeTest 범위 밖).
- 종료부 **`Total=747 → 747+N PASS / 0 FAIL`**, `Unclassified=0`, build 0/0.

## 5. 보고 / Branch
- build 0/0 · SmokeTest **`Total=N PASS / 0 FAIL`** 합계 줄 · Gate A(추적파일 의도·secret 0·금지확장자 0·`PackageReference` 0) · 변경 파일 · 양성/음성 케이스 · **"Applied Skill Checklists"**(`risk-ui-ux-review`·`risk-smoke-governance`·`risk-security-guard`).
- Branch `feature/ux-wp-05-smart-assist-as-you-type` · Commit: `feat: Smart Assist as-you-type surfacing (debounced, static NoModel) (UX-WP-05)`

## 6. Claude Review Checklist
Core 엔진 변경 0(WPF 트리거만)·`GetCompletions`/안전핀 경로 재사용 / 디바운스=인박스 `DispatcherTimer`(NuGet 0) / **최소 prefix 게이트**(전체 덤프 방지)·`ExtractCompletionPrefix` 재사용 / **prefix 미충족·매칭 0 시 stale 팝업 Close** / **focus 불탈취 = as-you-type show path가 `CompletionList.Focus()` 미호출**(focus 이동은 Ctrl+Space 전용·`Show` grabFocus 파라미터/오버로드)·**Up/Down은 TextBox `OnCompletionTextBoxPreviewKeyDown`에서 선택 이동**(`MoveSelection`)·Enter/Tab/Escape 현행 / **재진입 가드**(프로그램적 편집 suppress·수락 후 재트리거 0) / Ctrl+Space·accept 삽입·accept audit 불변 / 자동삽입 0·surface audit 0(accept만)·입력 원문 미저장 / NoModel 보존·실시간 LLM=R4 STOP / **결정 헬퍼는 `Core`/net8.0(비-WPF)에 위치**(App 아님)→ SmokeTest 단언 가능 / 기존 `AssistTests` 보존 / `Total` 보존+신규·Unclassified 0 / Gate A. **B-5 종료는 본 WP(트리거)로 입력중 추천이 실제 표면화될 때**(렌더 검증=Gate B). **프롬프트**: 본 파일.
