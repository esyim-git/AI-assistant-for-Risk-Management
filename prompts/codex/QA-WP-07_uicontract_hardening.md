# Codex Prompt — QA-WP-07: UiContract SmokeTest 하드닝 (XAML Contract·Resizable 레이아웃·영속·스냅샷)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-07) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-07-uicontract-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-07, `SKILLS.md`+`risk-ui-ux-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.App/MainWindow.xaml`(XAML Contract 원천)·`src/RiskManagementAI.Core/Config/UiLayoutStore.cs`, `tests/RiskManagementAI.SmokeTests/UiContractTests.cs`.
> **기준선**: main `693488c`(VERSION 0.7.0), 정본 SmokeTest `Total=877 PASS=877 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
WPF UI 계약(XAML Contract·Resizable 레이아웃·레이아웃 영속·읽기전용 스냅샷·탭 내비게이션)의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드·XAML 변경 0** — 테스트만(XAML은 문자열 계약으로 read-only 단언). 실 렌더는 콘솔 SmokeTest 범위 밖(Gate B)임을 존중(과대표기 금지).

## 1. 작업 범위 (UiContractTests.cs — additive only)
1. `MainWindow.xaml`·`UiLayoutStore`와 현 `UiContractTests`를 대조 → **미커버 XAML/계약 경계만** 추가.
2. 후보(계약 확인·신규 동작 요구 아님): GridSplitter 존재(Rows/Columns)·EditorRow 비고정(`2*`)·Window Min 1180×720·SQL/VBA/Excel/Draft TextBox Stretch+AcceptsTab · `UiLayoutStore` round-trip(창 크기/분할/Safety 너비)·손상/스키마 fallback·config 경로가드·clamp·`DefaultPath ∉ Mandatory/RequiredCritical`(무결성 비대상) · 읽기전용 스냅샷 계약(Offline Mode/Local Model/Reports 등)·탭 키 내비게이션.
3. **합성/문자열 계약만** — 실 렌더·실데이터 0.

## 2. 제외 범위
XAML/`UiLayoutStore`/제품 코드 변경. 실 WPF 렌더 검증(Gate B). 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
XAML/제품 코드 변경 0 · 무결성 비대상 계약(`UiLayoutStore` DefaultPath) 회귀 · 실데이터 0 · NuGet 0 · **기존 테스트 삭제·약화 0** · 실 렌더=Gate B 과대표기 금지.

## 4. 테스트 (SmokeTest — 도메인 `UiContract`)
> `SmokeTestContext.SmokeDomain` UiContract 토큰(`UI shell`/`Left menu`/`Main tab`/`navigation`/`snapshot`/`Risk Dashboard`/`Settings`/`Feedback Center`/`Offline Mode`/`Local Model`/`Reports`). 신규 단언 설명에 이 토큰 사용, 더 위 도메인(`Report `·`Limit`·`completion`/`assist`(Assist)·`Kb`) 트리거 회피. `Unclassified=0`.
- 각 계약 경계 → XAML 술어/`UiLayoutStore` round-trip·fallback·clamp 단언. 실 렌더는 단언 안 함(Gate B 명시).
- 기존 `UiContractTests` 단언 **전부 보존**. 종료부 **`Total=877 → 877+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+UiContract 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **실 렌더=Gate B 명시** · **Applied Skill Checklists**.
- Branch `feature/qa-wp-07-uicontract-hardening` · Commit: `test: harden ui contract and layout persistence coverage (QA-WP-07)`

## 6. Claude Review Checklist
제품 코드/XAML 변경 0(테스트만) / 추가는 실제 미커버 계약 / `UiLayoutStore` round-trip·fallback·clamp·무결성 비대상 회귀 정확 / 실 렌더=Gate B 명시(과대표기 금지) / 도메인 UiContract·Unclassified 0 / 기존 UiContractTests 보존·감소 0 / NuGet 0 / `Total` 877 보존+신규 / Gate A.
