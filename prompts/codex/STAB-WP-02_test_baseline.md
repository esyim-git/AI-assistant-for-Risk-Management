# Codex STAB-WP-02 — Authoritative Test Baseline (정본 합계 출력)

> 권위 스펙: `docs/39 §B`(STAB-WP-02), `docs/38 §0`(RR-12). 기준선: main v0.6.0. 선행: STAB-WP-01 병합. **NEXT UP 지정 시에만 착수**(현재 큐 2순위).

## 현재 문제
SmokeTest 하니스가 단언별 `PASS:`만 출력하고 **합계 수치를 찍지 않는다**. 그래서 484/502가 혼재(둘 다 미집계 추정치). 정본 수치가 없다(RR-12).

## 목표
SmokeTest 종료 시 **정본 합계(Total/PASS/FAIL) + 도메인별 PASS/FAIL Summary + 실행시간**을 출력한다. 실패 시 비0 종료 유지. main에서 1회 실행해 정본 수치를 `docs/38 §0`·Release Note에 고정(STAB-WP-01의 metadata 행과 연결).

## 먼저 읽기
`AGENTS.md`, `docs/39 §B`(STAB-WP-02/04), `tests/RiskManagementAI.SmokeTests/Program.cs`(러너/`AssertTrue` 등 단언 헬퍼 부분 — 파일 상단·하단).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/stab-wp-02-test-baseline origin/main
```
- .NET 8. PR→main(squash, `(#PR)`), 게이트 A, NuGet 0(외부 테스트 프레임워크 도입 금지).

## 작업 범위
- 단언 헬퍼(`AssertTrue`/`AssertThrows` 등)에 **성공/실패 카운터** 누적. 도메인 태그(Safety/Csv/Xlsx/Mapping/Limit/Reconciliation/Report/Kb/Ncr/Packaging/UiContract)별 집계(간단한 prefix 또는 섹션 마커).
- 실행 종료부에 `=== SmokeTest Summary: Total=N PASS=N FAIL=0 (도메인별 …) Duration=…s ===` 출력. FAIL>0이면 **exit code ≠ 0**(기존 동작 유지/강화).
- **기존 단언·이름·메시지 보존**(삭제·약화 0). 출력만 추가.
- **제외**: 테스트 파일 분리(STAB-WP-04), 기능 변경.

## 구현 세부 / 보안
- 외부 프레임워크 0(현 콘솔 러너 유지). 결정적 출력. 도메인 태깅은 최소 침습(주석/상수 마커 또는 헬퍼 인자).

## 테스트
- 합계 = 실제 실행 단언 수와 일치(루프 단언 포함). 도메인 Summary 합 = Total. 일부러 실패 주입 시 exit≠0·FAIL 카운트 정확(검증 후 되돌림). 기존 전부 PASS 유지.

## 완료/보고
정본 Total/PASS/FAIL/도메인/Duration 출력. main 실행값을 보고 → Claude가 `docs/38 §0` 정본 수치 고정. `docs/39` STAB-WP-02 DONE 갱신 요청.

## Claude Review Checklist
합계·도메인 Summary 정확 / 기존 단언 불변(개수 보존) / FAIL 시 exit≠0 / 외부 프레임워크 0 / 정본 수치 docs 반영.
