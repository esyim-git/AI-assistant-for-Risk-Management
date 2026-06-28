# Codex STAB-UX-01 — Resizable Editor Layout (WPF 레이아웃 안정화, 기능변경 0)

> 권위 스펙: `AGENTS.md` > `docs/39 §STAB-UX-01` > `docs/46 §8`(Resizable Workspace Layout) > 본 프롬프트. 현재 기준선 = main `f6b1405`(STAB-WP-04 #66 머지 후), SmokeTest 정본 `Total=572 PASS=572 FAIL=0`.

## 현재 문제 / 목표
`MainWindow.xaml` 중앙 작업 Grid의 EditorRow가 **고정 `Height="260"`**이라 SQL/VBA/Excel/리스크 코멘트 편집 영역이 좁고 창 리사이즈에 반응하지 않는다. 우측 Safety 패널도 `Width="300"` 고정이다.
**GridSplitter 기반 가변 레이아웃**으로 (1) 에디터↔결과 패널, (2) 중앙 작업영역↔우측 Safety 패널을 사용자가 조절하고 창 크기에 비례 반응하게 만든다. **기능·계약·데이터 흐름·이벤트 핸들러 변경 0 — 순수 WPF 레이아웃/XAML 작업이다.**

## 먼저 읽기
1. `AGENTS.md`(절대원칙·STOP 규칙)
2. `docs/39 §STAB-UX-01`(WP 정본)
3. `docs/46 §8`(Resizable Workspace Layout 설계)
4. `src/RiskManagementAI.App/MainWindow.xaml`(현재 레이아웃 — 아래 "현재 구조" 참조)
5. `tests/RiskManagementAI.SmokeTests/`(특히 `UiContractTests`)

## 브랜치
```powershell
git fetch origin
git switch -c feature/stab-ux-01-resizable-layout origin/main
```

## 현재 구조 (수정 대상, `MainWindow.xaml`)
- **Window**: `Height="720" Width="1180" MinHeight="640" MinWidth="1000"` (ResizeMode/SizeToContent 미지정).
- **최상위 `<Grid>`**: 3 컬럼 `220 / * / 300`, 3 행 `64 / * / 32`.
  - 좌측 메뉴 = `Border Grid.Row="1" Grid.Column="0"`(220 고정 — **유지**).
  - 중앙 작업 = `Grid Grid.Row="1" Grid.Column="1" Margin="18"` → 내부 RowDefinitions `Height="260"`(EditorRow, **문제**) + `Height="*"`(ResultRow). EditorRow에 `TabControl x:Name="MainTabs"`(12 탭), ResultRow에 결과 패널 Grid(`FindingList` ListBox).
  - 우측 Safety = `Border Grid.Row="1" Grid.Column="2"`(컬럼 300 고정).

## 작업 범위 (XAML만, 기능 0)
1. **Window**: `MinHeight="720" MinWidth="1180"`, `ResizeMode="CanResize"`, `SizeToContent="Manual"` 명시. (`Width="1180" Height="720"` 유지.)
2. **세로 분할(에디터↔결과)** — 중앙 작업 `Grid`의 RowDefinitions:
   - EditorRow: `Height="260"` → **`Height="2*" MinHeight="260"`**.
   - **신규 Splitter Row**: `Height="8"` + 그 Row에 `<GridSplitter HorizontalAlignment="Stretch" VerticalAlignment="Stretch" ResizeBehavior="PreviousAndNext" ResizeDirection="Rows" Height="8"/>`.
   - ResultRow: `Height="*"` → **`Height="1*" MinHeight="180"`**. (TabControl/결과패널 `Grid.Row` 인덱스를 splitter row 삽입에 맞춰 재배치.)
3. **가로 분할(중앙↔우측 Safety)** — 최상위 `<Grid>`:
   - 컬럼을 `220 / * / Auto / 300` 형태로 만들거나, 중앙·우측 사이에 splitter 컬럼을 두고 `<GridSplitter Grid.Column="..." Width="6" ResizeBehavior="PreviousAndNext" ResizeDirection="Columns" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>` 추가. **header Border의 `Grid.ColumnSpan`과 모든 `Grid.Column` 인덱스를 새 컬럼 수에 맞게 갱신**한다.
   - Safety 패널 컬럼 `Width="300"` → **`Width="340" MinWidth="280" MaxWidth="560"`**. 좌측 메뉴 `Width="220"` 고정 유지, 중앙 `Width="*"`.
4. **편집 TextBox**(SQL/VBA/Excel/리스크 코멘트 입력용): `HorizontalAlignment="Stretch" VerticalAlignment="Stretch"`, `AcceptsReturn="True" AcceptsTab="True"`, `VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto"`, `FontFamily="Consolas" FontSize="14"`.

## 절대 금지
- 이벤트 핸들러 시그니처/`x:Name`/탭 구조/바인딩 변경, 기능·데이터 흐름 변경
- 자동 실행·자동 삽입 추가
- 외부 NuGet/Editor 패키지(AvalonEdit 등) 추가 — **인박스 WPF만**
- 레이아웃 영속화(`config/ui_layout.local.json`)는 **이번 WP 범위 아님**(후속)
- 실데이터/내부규정 원문/모델 파일 추가
- 기존 SmokeTest 삭제·약화

## 테스트 (SmokeTest, 외부 프레임워크 0 — `UiContractTests`에 XAML Contract 추가)
`MainWindow.xaml` 텍스트를 읽어 단언:
- `GridSplitter`가 **1개 이상** 존재(에디터-결과·중앙-우측).
- EditorRow가 **고정 `Height="260"`이 아님**(`2*` 및 `MinHeight` 포함).
- Window에 `MinWidth="1180"`·`MinHeight="720"` 존재.
- SQL/VBA 편집 TextBox에 `Stretch`·`Consolas`·`FontSize="14"` 존재.
- Safety 패널 컬럼에 `MinWidth`·`MaxWidth` 존재.
- 신규 단언은 **`UiContract` 도메인**으로 분류(`Unclassified=0` 유지).

필수 검증:
```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests -c Release
```

## 완료 기준
- WPF 빌드 0/0(로컬 Windows + .NET 8). 가변 레이아웃 동작(에디터/결과 드래그, 창 리사이즈 비례 반응).
- **기존 SmokeTest `Total=572 PASS=572 FAIL=0` 보존** + XAML Contract 신규 단언(총수 증가, 분류 키워드로 `Unclassified=0`).
- Gate A 0건: PackageReference 0, 금지 자산 0, secret/주민번호 0.

## 완료 보고
- 변경 파일(주로 `MainWindow.xaml`, 신규 XAML Contract 테스트)
- 레이아웃 변경 요약(행/열 구조 before→after, GridSplitter 위치)
- build 결과 / SmokeTest summary 전체 합계 / Gate A 결과
- 기능·계약 변경이 없음을 명시
- 남은 blocker 또는 없음

## Claude Review Checklist
고정높이 제거(EditorRow 비고정) / GridSplitter 존재(에디터-결과·중앙-우측) / Window Min 1180×720 / TextBox Stretch+Consolas/14 / Safety 패널 Min·Max / **기능·계약·이벤트 시그니처 변경 0** / 기존 SmokeTest 보존 / XAML Contract Test·Unclassified 0 / NuGet 0 / Gate A.
