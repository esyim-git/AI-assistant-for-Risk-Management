# Gate C — Excel 2021 / 운영 반입 항목·증거 양식

정본 원장: `docs/45_GateBC_v0.6.0_Evidence.md` C그룹. 기준: `docs/41 §4`(Pilot Gate C) · `docs/28`(보안검토).
본 파일은 **양식·판정 가이드**다. 코드 동작을 바꾸지 않으며, 실 데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret·모델파일을 절대 기입하지 않는다. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만.

## 판정 규칙
- 모든 항목 `PASS`라야 전체 `PASS`. 하나라도 누락/불일치 → 해당 항목 + 전체 `BLOCKED` 유지.
- **실 PC(Excel 2021/반입 환경) 증거가 없으면 `PASS` 금지** → `BLOCKED` 유지. (CLAUDE.md §11.4)
- 상태 어휘 정본만: `VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`.

## C. Gate C — Excel 2021 / 운영 반입
| # | 항목 | 증거 유형 | 판정 기준 |
|---|---|---|---|
| C1 | Excel 2021에서 Report Open · **수식 오류(Formula Error) 0** | 캡처 | 오류 셀 0 |
| C2 | **외부 링크(External Link) 0 · Macro 0 · Formula Injection 0** | 검사 | 3종 모두 0 |
| C3 | 백신/EDR 통과 | 로그 | 탐지 0 |
| C4 | **PDB/개인경로 0** (v0.6.0=수동 ZIP 검사로 확인. 자동 가드/Integrity Manifest는 STAB-WP-03=v0.6.1) | 스캔 | 개인경로·심볼 0 |
| C5 | Code Signing 상태 (운영 절차 Placeholder) | 기록 | 절차 기록(현 단계 `PLACEHOLDER`) |
| C6 | Performance / Memory 측정 | 측정값 | 임계값 충족(처리시간·메모리) |
| C7 | Rollback 절차 확인 | 기록 | 절차 재현 가능 |

## Formula Injection 점검 메모 (참고)
- 셀 값이 `=`, `+`, `-`, `@` 로 시작하면 수식 주입 의심 → 산출물에서 0이어야 한다.
- 외부 링크: `[Workbook]Sheet!Ref` / `xl/externalLinks/` 존재 0.
- Macro: `.xlsx`(매크로 비포함 포맷) 유지, `vbaProject.bin` 0.
- 위 항목은 산출물(리포트) 검증용이며, **자동 실행/수정 금지** — 열람·검사만.

## 증거 메타 (각 항목 공통)
`상태(PASS/FAIL/BLOCKED) · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위 재판정 후 `docs/45` 상단 상태 갱신. Gate B + Gate C 전부 `PASS`면 `docs/43 §3` Gate B + Gate C를 `PASS`로 봉인하고 v0.6.0 Release 핸드오프(`docs/43 §4`).

> 관련: `docs/41 §4`, `docs/28`, `docs/45`, `docs/39`(STAB-WP-03 Integrity). Gate B는 [gate-b-template.md](gate-b-template.md).
