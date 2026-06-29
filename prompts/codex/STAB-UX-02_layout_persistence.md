# Codex STAB-UX-02 — Resizable Layout Persistence (세션 간 레이아웃 영속화)

> 권위 스펙: `AGENTS.md` > `docs/39 §STAB-UX-02` > 본 프롬프트. 현재 기준선 main `eae1766`(UX-WP-03 #73 + truth-sync #74 머지 후), SmokeTest 정본 `Total=631 PASS=631 FAIL=0`.
> 순수 WPF + 인박스 `System.Text.Json`만. **외부 NuGet 0**. 레이아웃 외 상태 영속화 금지. 자동실행 0.

## 현재 문제 / 목표
STAB-UX-01(#68)이 가변 레이아웃(창 크기·에디터/결과 GridSplitter·중앙/Safety GridSplitter)을 도입했으나, 매 실행 시 기본값으로 리셋된다. **창 크기 + 에디터/결과 분할 비율 + Safety 컬럼 너비**를 종료 시 저장하고 다음 실행에 복원한다.

## 먼저 읽기
1. `AGENTS.md`(절대원칙·STOP)
2. `docs/39 §STAB-UX-02`(WP 정본)
3. `docs/46 §8`(Resizable Workspace Layout)
4. `src/RiskManagementAI.App/MainWindow.xaml`(아래 "현재 레이아웃")·`MainWindow.xaml.cs`
5. `src/RiskManagementAI.Core/Config/PolicyLoader.cs`(**config 경로 가드 패턴** — `config/`-상대 JSON만 허용, 재사용)
6. `src/RiskManagementAI.Core/Logging/LogPathResolver.cs`(traversal/rooted 거부 패턴)
7. `src/RiskManagementAI.Core/Integrity/IntegrityVerifier.cs`(**무결성 제약** — 아래)

## 브랜치
```powershell
git fetch origin
git switch -c feature/stab-ux-02-layout-persistence origin/main
```

## 현재 레이아웃 (영속 대상, `MainWindow.xaml`)
- **Window**: `Width="1180" Height="720" MinWidth="1180" MinHeight="720"`.
- **중앙 작업 Grid**(`Grid.Row="1" Grid.Column="1"`) RowDefinitions: EditorRow `Height="2*" MinHeight="260"` · Splitter Row `Height="8"`(GridSplitter `x:Name="EditorResultSplitter"`) · ResultRow `Height="*" MinHeight="180"`. → **저장 대상 = EditorRow:ResultRow star 비율**.
- **최상위 Grid** ColumnDefinitions: `220 / *(MinWidth=480) / Auto(GridSplitter x:Name="WorkspaceSafetySplitter") / 340(MinWidth=280 MaxWidth=560)`. → **저장 대상 = Safety 컬럼(마지막) 너비**.

## 작업 범위
1. **레이아웃 store**(신규, **반드시 `Core/Config/UiLayoutStore.cs`** — App 배치 금지): SmokeTest 프로젝트는 `RiskManagementAI.Core`만 참조(`net8.0`)하고 App은 `net8.0-windows`라, store가 App에 있으면 round-trip/fallback/경로가드 SmokeTest가 Windows 전용 App 참조 없이 접근 불가하다. 따라서 store 로직(직렬화·경로가드·clamp·fallback)은 Core에 두고 MainWindow(App)는 이를 호출만 한다.
   - record `UiLayout(double WindowWidth, double WindowHeight, double EditorRowStar, double ResultRowStar, double SafetyColumnWidth, int SchemaVersion)` + 안전 기본값(STAB-UX-01 기본: 1180/720/2/1/340, SchemaVersion=1).
   - `static UiLayout Load(string path = "config/ui_layout.local.json")` / `static void Save(UiLayout, string path = ...)`. JSON = `System.Text.Json`.
   - **경로 가드**: `PolicyLoader`/`LogPathResolver`와 동일하게 `config/`-상대 JSON만 허용(traversal/rooted/`config` 밖 거부 → `ArgumentException`).
2. **MainWindow 연결**:
   - `Loaded`(또는 ctor 후): `UiLayoutStore.Load()` → `Width/Height`, 중앙 Grid `RowDefinitions[0].Height = new GridLength(EditorRowStar, Star)`·`[2].Height = new GridLength(ResultRowStar, Star)`, Safety `ColumnDefinitions[마지막].Width = new GridLength(SafetyColumnWidth)`. **Min/Max 범위로 clamp**(SafetyColumnWidth는 280~560, Window는 Min 이상).
   - 저장: `OnClosing`에서 현재 값으로 `Save` 1회(또는 GridSplitter `DragCompleted`+Window `SizeChanged` 디바운스). 과도한 디스크 쓰기 금지.

## 절대 금지
- 레이아웃 외 상태(입력 텍스트·데이터·감사·탭 선택·정책) 영속화
- 외부 NuGet/Editor 패키지 추가
- 자동 실행·자동 삽입
- **`config/ui_layout.local.json`을 무결성/패키징/git에 포함**(아래)
- 실데이터/내부규정 원문/모델 파일 추가
- 기존 SmokeTest 삭제·약화

## ★ 무결성 제약 (필수)
`config/ui_layout.local.json`은 **런타임 사용자 가변 파일**이다. 따라서:
- `build/01`(manifest 생성)·`IntegrityVerifier`(`MandatoryEntries`/`RequiredCriticalEntries`/`CriticalGlobs`)·`PackagingTests`/패키징 인벤토리·Release ZIP **어디에도 추가하지 않는다**. (추가 시 사용자가 레이아웃을 바꿀 때마다 STAB-WP-03b 런타임 Fail-Closed가 오발한다.)
- **★ publish 복사 제외 (필수)**: `build/01_publish-win-x64.ps1`은 `$RequiredAssetFolders`에 `config`를 포함해 **`config/` 전체를 `Copy-Item -Recurse -Force`로 복사**한다. 따라서 패키징 직전 개발 PC에 `config/ui_layout.local.json`이 존재하면(앱/레이아웃 육안 점검 후) **그대로 Release ZIP에 유입**된다 — manifest 미등록만으로는 부족하다. build/01의 config 복사에서 **`*.local.json`(최소 `ui_layout.local.json`)을 제외**(예: `Copy-Item ... -Exclude '*.local.json'` 또는 복사 후 publish 출력에서 제거)하고, **publish 출력/ZIP에 `*.local.json`이 부재함을 검증**(build/03 금지스캔 또는 PackagingTests 음성 단언)한다. 사용자 로컬 UI 상태가 portable 패키지에 새거나 release 빌드가 패키저의 로컬 레이아웃에 의존하지 않게 한다.
- 파일은 **config 루트**에 둔다(`config/ncr/`가 아님 → `CriticalGlobs("config/ncr","*.json")` 비대상 유지).
- **`.gitignore`에 `config/ui_layout.local.json` 추가**(사용자 상태 커밋 금지).
- 파일 부재/손상은 정상 상태 → **기본 레이아웃으로 fallback**(startup 차단·예외 금지).

## 테스트 (SmokeTest, 외부 프레임워크 0)
- **★ 도메인 분류(필수)**: `SmokeDomain` 분류기(`SmokeTestContext.cs`)에는 **bare `layout`/`UiLayout` 키워드가 없다**(STAB-UX-01 테스트는 메시지가 `UI layout`으로 시작해 기존 `"UI "` 키워드로 `UiContract` 분류됨). 따라서 신규 단언 **메시지를 `UI layout ...`으로 시작**시켜 기존 `"UI "` 키워드로 `UiContract` 분류되게 한다(`Unclassified=0` 유지). 대안: 분류기 `UiContract` 라인에 `"UiLayout"`/`"layout persistence"` 키워드를 추가(이 경우 추가 사실을 보고). 둘 중 하나를 **반드시** 적용.
- **round-trip**: Save→Load 동일 값.
- **fallback**: 파일 부재 / 손상 JSON / SchemaVersion 불일치 → 예외 0, 기본 레이아웃.
- **경로 가드**: `config/` 밖·`..` traversal·rooted 경로 → `ArgumentException`.
- **clamp**: Min/Max 밖 저장값 로드 시 범위로 보정(SafetyColumnWidth 280~560 등).
- **무결성 비대상**: `IntegrityVerifier`의 Mandatory/RequiredCritical/Critical glob 어디에도 `ui_layout.local.json`이 없음을 단언(레이아웃 파일 부재/변경이 startup Fail-Closed를 유발하지 않음).
- **publish 부재**: 패키징 시 `*.local.json`이 publish 출력/Release ZIP에 포함되지 않음을 단언(build/03 금지스캔 또는 PackagingTests 음성).
- **.gitignore**: `config/ui_layout.local.json` 포함 단언.
- 기존 `Total=631` 보존 + 신규(전 신규 단언 `UiContract`, `Unclassified=0`).

## 검증 (로컬 Windows, .NET 8 SDK)
```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests -c Release
```
완료 기준: WPF build 0/0. 레이아웃 저장/복원 육안 1회(창 크기·스플리터 조절 후 재실행 시 복원). `Total` 보존+신규, `Unclassified=0`. Gate A 0(PackageReference 0·금지 자산 0·secret/주민번호 0).

## 완료 보고
변경 파일 / 레이아웃 store API / MainWindow load·save 연결 지점 / 무결성·패키징·gitignore 비포함 확인 / build·SmokeTest summary 합계 / Gate A / 남은 blocker 또는 없음. `docs/39` STAB-UX-02 DONE 요청.

## Claude Review Checklist
레이아웃만 영속(데이터/입력 미저장) / config 경로 가드 재사용 / **무결성 critical 비대상**(레이아웃 변경이 Fail-Closed 유발 안 함) / `.gitignore` 포함 / 안전 fallback(손상→기본값) / clamp(Min/Max) / 외부 NuGet 0 / 기존 테스트 보존·Total 신규 / 자동실행 0 / Gate A.
