# Codex STAB-WP-04 — SmokeTest Suite Structure (RR-10)

> 권위 스펙: `AGENTS.md` > `docs/39` STAB-WP-04 > 본 프롬프트. 현재 기준선은 main `80fbcb8`(STAB-WP-03b #61 + truth-sync #63 + Smart Assist 설계 #62 머지 후)이며, SmokeTest 정본은 `Total=572 PASS=572 FAIL=0`.

## 목표
`tests/RiskManagementAI.SmokeTests/Program.cs`의 비대한 단일 파일을 외부 테스트 프레임워크 없이 내부 suite 구조로 분리한다. 목적은 유지보수성과 RR-10(기존 테스트 삭제·약화 금지) 보호이며, 기능 동작 변경은 금지한다.

## 먼저 읽기
1. `AGENTS.md`
2. `docs/38_v1_Master_Roadmap.md`
3. `docs/39_Work_Package_Backlog.md`의 Resume Brief 및 STAB-WP-04
4. `docs/40_ADR_Architecture_Evolution.md`
5. `docs/28_Security_Review_Checklist.md`
6. `tests/RiskManagementAI.SmokeTests/Program.cs`

## 브랜치
```powershell
git fetch origin
git switch -c feature/stab-wp-04-test-suites origin/main
```

## 작업 범위
- 외부 NuGet/테스트 프레임워크 0. 현재 console SmokeTest runner 유지.
- `Program.cs`의 assertion helper, summary 출력, domain classification, cleanup 동작을 보존한다.
- 내부 파일/suite 예시:
  - `TestRunner`
  - `SafetyTests`
  - `CsvTests`
  - `XlsxTests`
  - `MappingTests`
  - `LimitTests`
  - `ReconciliationTests`
  - `ReportTests`
  - `KbTests`
  - `NcrTests`
  - `PackagingTests`
  - `UiContractTests`
  - `AuditTests`
- 위 이름은 권장 구조다. 더 작은 단계가 안전하면 suite를 2~4개씩 나눠 커밋해도 된다.

## 절대 금지
- 기존 assertion 삭제·약화
- 실패를 PASS로 바꾸는 조건 완화
- domain summary, `Total=... PASS=... FAIL=...`, fail exit code 제거
- 외부 PackageReference 추가
- 기능 코드 리팩터링 끼워넣기
- 실데이터/내부규정 원문/모델 파일 추가

## 검증
필수:
```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests -c Release
```

완료 기준:
- `Total=572 PASS=572 FAIL=0` 유지. 총수가 바뀌면 변경 사유와 assertion mapping을 `docs/39`에 기록하고, 삭제·약화가 아님을 증명한다.
- Domain별 PASS/FAIL 합계가 Total과 일치.
- Gate A 0건: PackageReference 0, 금지 자산 0, secret/주민번호 0.

## 완료 보고
보고에는 아래를 포함한다:
- 변경 파일
- suite 분리 매핑 요약
- build 결과
- SmokeTest summary 전체 합계
- Gate A 결과
- 테스트 수가 유지됐는지 여부
- 남은 blocker 또는 없음

## Claude Review Checklist
총수 보존 / assertion 이름·의미 보존 / suite mapping 명확 / domain summary 보존 / fail exit code 보존 / 외부 NuGet 0 / Gate A / 기능 변경 없음.
