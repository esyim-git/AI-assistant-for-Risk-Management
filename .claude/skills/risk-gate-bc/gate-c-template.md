# Gate C — Excel 2021 / 운영 반입 항목·증거 양식 (v0.7+)

정본 원장: `docs/54_GateBC_v0.7.1_Evidence.md` C그룹. `docs/48`(v0.7.0)·`docs/45`(v0.6.0)·`docs/44`(v0.5.0)는 historical이다. 기준: `docs/41 §4`(Pilot Gate C) · `docs/28`(보안검토).
본 파일은 **양식·판정 가이드**다. 코드 동작을 바꾸지 않으며, 실 데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret·모델파일을 절대 기입하지 않는다. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만.

## 판정 규칙
- 모든 항목 `PASS`라야 전체 `PASS`. 하나라도 누락/불일치 → 해당 항목 + 전체 `BLOCKED` 유지.
- **실 PC(Excel 2021/반입 환경) 증거가 없으면 `PASS` 금지** → `BLOCKED` 유지. (CLAUDE.md §11.4)
- 상태 어휘 정본만: `VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`.
- 미서명 ZIP 반입은 서명 WP 완료 전까지 C5 `ACCEPTED_RISK` 또는 `BLOCKED`로만 기록한다. 임의 PASS 금지.

## C. Gate C — Excel 2021 / 운영 반입
| # | 항목 | 증거 유형 | 판정 기준 |
|---|---|---|---|
| C1 | Excel 2021에서 Report Open · **수식 오류(Formula Error) 0** | 캡처 | 오류 셀 0 |
| C2 | **외부 링크(External Link) 0 · Macro 0 · Formula Injection 0** | 검사 | 3종 모두 0 |
| C3 | 백신/EDR 통과 | 로그 | 탐지 0 |
| C4 | **PDB/개인경로 0** + Dev/Test config 0 | 스캔/manifest | 개인경로·심볼·Dev/Test config 0 |
| C5 | Code Signing 상태 | 기록 | 서명본 PASS 또는 미서명 `ACCEPTED_RISK` 서면 수용 |
| C6 | Performance / Memory 측정 | 측정값 | 임계값 충족(처리시간·메모리) |
| C7 | Rollback 절차 확인 | 기록 | 절차 재현 가능 |

## Formula Injection 점검 메모 (참고)
- 셀 값이 `=`, `+`, `-`, `@` 로 시작하면 수식 주입 의심 → 산출물에서 0이어야 한다.
- 외부 링크: `[Workbook]Sheet!Ref` / `xl/externalLinks/` 존재 0.
- Macro: `.xlsx`(매크로 비포함 포맷) 유지, `vbaProject.bin` 0.
- 위 항목은 산출물(리포트) 검증용이며, **자동 실행/수정 금지** — 열람·검사만.

## 증거 메타 (각 항목 공통)
`상태(PASS/FAIL/BLOCKED/PARTIAL/ACCEPTED_RISK) · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위 재판정 후 현재 Gate 시트(`docs/54` 또는 릴리스별 successor) 상단 상태를 갱신한다. Gate B + Gate C 적용 대상 전부 `PASS`(명시 예외 수용 포함)면 해당 릴리스 Gate B/C를 봉인한다.

> 관련: `docs/41 §4`, `docs/28`, `docs/54`; `docs/48`은 historical. Gate B는 [gate-b-template.md](gate-b-template.md).
