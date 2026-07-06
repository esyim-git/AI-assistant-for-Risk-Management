---
name: risk-arch-refactor
description: Guard behavior-invariant structural refactors (God-class decomposition, tab-controller extraction, display-DTO relocation) — zero behavior change, UI contract tests migrated in lockstep, no MVVM big-bang, no new dependencies.
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git diff *) Bash(dotnet build *) Bash(dotnet run *)
---

# Behavior-Invariant Refactor Guard (ARCH-WP)

## 목적
구조 리팩터 WP(ARCH-WP류 — 예: `MainWindow.xaml.cs` 1,600줄급 God-class 분해)가 **행위 불변**을 증명하며 진행되도록 계획·구현·리뷰 기준을 고정한다. "완료 기능 재설계 금지" 원칙(`CLAUDE.md` 기준선)과 충돌하지 않는 유일한 리팩터 형태 = **동작·산출 불변의 구조 이동**이다. 본 스킬은 점검/절차 가이드이며 그 자체로 코드 동작을 바꾸지 않는다.

## 언제 사용
- MainWindow 등 대형 클래스 분해, 탭별 컨트롤러/partial class 추출, 표시용 DTO 이동, 파일 분리 류 WP의 계획(`risk-wp-planner` 전) · 구현(Codex) · 리뷰(`risk-codex-review`에 행위 불변 축 추가).
- UI 계약 테스트(`UiContractTests` — XAML XDocument·소스 텍스트 단언 방식)가 영향받는 모든 App 구조 변경.

## 절대 원칙
- **행위 변경 0**: 동일 입력 → 동일 출력(화면 수치·리포트 산출·audit 해시 대상 불변). 기능 추가/제거/수정과 리팩터를 **한 WP에 섞지 않는다**.
- **XAML 구조·탭 구성·`x:Name` 변경 0**(계약 테스트 대상 보존). 코드 이동으로 `UiContractTests`의 파일 경로/문자열 단언이 영향받으면 **단언을 약화하지 말고 동등 강도 이상으로 이전**한다(`risk-smoke-governance` — 단언 손실 금지).
- **MVVM 빅뱅 금지**: 단계별 진행 — 1단계 탭별 partial/컨트롤러 분리 → 2단계 표시 DTO 분리 → 3단계 인박스 `INotifyPropertyChanged` 선택 도입. **한 WP = 한 단계.**
- 외부 NuGet 0 유지(DI 컨테이너 포함 금지 — 수동 조립 팩토리만) · public 시그니처 보존 우선 · 기존 테스트 삭제/약화 0 · SmokeTest `Total` 비감소.
- **재비대화 방지 가드 신설 권장**: 분해 목표를 고정하는 구조 가드 회귀(예: `MainWindow.xaml.cs` 라인 상한) 추가 — 현재 라인수를 막는 테스트가 없다는 것이 확인된 갭이다.

## 절차
1. **현황 계측**: 대상 파일의 라인 수·메서드/필드 수·이벤트 핸들러 수와, 계약 테스트가 참조하는 경로/문자열 단언 목록을 먼저 계측·기록한다.
2. **추출 단위 설계**: 탭/도메인 단위 이동 계획(파일→파일 매핑표). **이동만 하고 로직 수정 0** — 리네임·시그니처 변경 최소화.
3. **계약 테스트 이전 계획**: 영향받는 텍스트 픽스처 단언을 새 파일 경로 기준으로 이전(강도 동등 이상) + 신규 구조 가드 추가 계획을 WP에 명시.
4. **행위 불변 증거(구현 후)**: `dotnet build` 0/0 · SmokeTest `Total` 비감소·기존 단언 전부 green · (가능 시) 동일 샘플 입력의 산출물(리포트/프로파일) 해시 전후 비교.
5. **리뷰 5축**: ①범위(순수 이동인가) ②보안(동작 변경 없음 확인) ③테스트(이전·가드·Total 비감소) ④문서(docs/38 §5 Traceability 반영) ⑤**행위 불변 증명**(4의 증거 실재).

## 산출물/보고
- 파일→파일 이동 매핑표 · 계측 전/후 수치(라인/메서드/핸들러) · `Total=N`(비감소) 증거 · 행위 불변 증거(산출물 해시 비교 등) · 신규 구조 가드 목록.
- 보고 예: `ARCH-WP-01 1단계 완료 — MainWindow.xaml.cs 1,614→<N>줄(가드 상한 <M>), 이동 파일 <k>개, Total=900 불변, 산출물 해시 동일`.

## 참조
- `docs/40_ADR_Architecture_Evolution.md`(구조 결정 이력 — 필요 시 ARCH ADR 추가) · `docs/38 §5`(Traceability)
- `tests/RiskManagementAI.SmokeTests/UiContractTests.cs`(텍스트/XAML 계약 방식 — 이전 대상)
- `docs/proposals/FABLE5_REPO_ASSESSMENT_PROPOSAL_20260706.md §4.3~4.4·§10 WP-D`(분해 단계·근거 계측)
- 연계 스킬: [/risk-ui-ux-review](../risk-ui-ux-review/SKILL.md) · [/risk-smoke-governance](../risk-smoke-governance/SKILL.md) · [/risk-codex-review](../risk-codex-review/SKILL.md) · [/risk-wp-planner](../risk-wp-planner/SKILL.md)
