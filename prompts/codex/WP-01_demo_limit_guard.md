# Codex WP-01 — 합성 한도 차단 / DEMO_ONLY

> 권위 스펙: `docs/39` WP-01. Release: R1(Data Foundation). 목표 1개만 수행.

## 목표
UI가 노출의 1.1배로 **한도를 합성**하는 로직(`MainWindow.xaml.cs`의 `BuildUiLimitRows`: `limitAmount = Math.Max(Math.Abs(exposureAmount)*1.1m, 1m)`)을 제거하고, 실제 한도 데이터가 없으면 **DEMO_ONLY/LIMIT_DATA_REQUIRED로 명시·차단**한다(합성값을 실값처럼 쓰지 않는다).

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md`(§4·§13), `docs/38`(RR-01), `docs/39`(WP-01), `docs/28`(게이트 A).

## 브랜치/동기화 (docs/32)
```bash
git fetch origin && git switch -c feature/wp-01-demo-limit-guard origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests   # 268 기준선
```
- PR→main(squash), 머지 subject에 `(#PR)` 유지. main 직접 push/force/reset 금지. 매 커밋 게이트 A.

## 작업 범위 / 제외
- 범위: 합성 한도 산식 제거. 한도 미존재 시 `LIMIT_DATA_REQUIRED`(High) finding + 합성 미생성. 데모 샘플 사용 시 `DEMO_ONLY` 표식.
- 제외: 실 Join 엔진(WP-05), 인코딩(WP-02), 매핑(WP-04). **이번엔 합성 제거만.**

## 구현 세부
- `BuildUiLimitRows` 합성 분기 삭제(또는 한도 소스 없을 때 빈 목록 반환 + 안내 finding).
- 합성 수치를 audit log/리포트에 실값으로 기록 금지. 읽기 전용 유지.

## 테스트(필수)
- 한도 미제공 → 합성 1.1× 행 **미생성** + `LIMIT_DATA_REQUIRED` finding.
- 데모 데이터 경로 → `DEMO_ONLY` 표식.
- 기존 268 SmokeTest 유지.

## STOP 트리거
NuGet/외부 의존, 절대원칙 충돌, WP 범위 밖 대규모 변경 → 중단·보고.

## 완료/보고
build 0/0 · SmokeTest(기존+신규) PASS · 게이트 A 0건 · PR/커밋 · `docs/39` 원장 갱신.
> 완료 후 Claude가 docs/39 Review Checklist로 검증.
