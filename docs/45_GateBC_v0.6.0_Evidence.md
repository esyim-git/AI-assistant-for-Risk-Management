# 45. Gate B/C Evidence — v0.6.0 (R1 + R3)

## 목적 / 상태
v0.6.0 portable ZIP을 **실 오프라인 Test PC(Gate B)** 및 **운영 반입(Gate C)** 에서 실행해 `docs/41 §4`·`docs/42 §3`·`docs/43 §3` 체크리스트를 **증거 기반**으로 봉인하기 위한 **정본** 문서.

- **현재 판정: 🔴 BLOCKED** — 두 선행이 모두 미충족:
  1. **v0.6.0 패키지 미컷**(전제는 충족: packaging-guard PR #54 병합 완료). 사용자가 로컬에서 `build/00~03 -Version 0.6.0` → ZIP/SHA 생성 필요.
  2. **실 오프라인 Test PC 증거 없음**(repo/CI/로컬 아티팩트로 생성 불가).
- **판정 규칙**: 아래 모든 항목 `PASS`라야 전체 PASS. 하나라도 누락/불일치면 그 항목 `BLOCKED/FAIL` + 전체 BLOCKED 유지. **실 PC 증거 없이 PASS로 적지 않는다.**
- **v0.5 ↔ v0.6 중복 정리**: `docs/44`(v0.5.0 R1 전용)는 **historical**. 운영 진행 기준선은 **v0.6.0(R1+R3)** 이며 본 문서가 정본. v0.5 항목 1~12(R1)은 본 문서 B-그룹에 흡수되고, R3(KB/NCR)·Gate C가 추가된다. v0.5.0 ZIP SHA256 = `1E0AFD…A60D`(데스크탑 대조 일치, historical).

> 기록: v0.6.0 패키징 검증은 Codex가 pwsh 7에서 `build/03 -Version 0.6.0` PASS(SHA256 `429DD8FB…BD332`, PR #54) 보고. 단 **사용자 로컬 실 컷 + 오프라인 Test PC 실행**이 정본 증거다.

---

## A. 선행 (패키지 컷 — 로컬 Windows)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| A1 | `build/00~03 -Version 0.6.0` 전부 PASS (해시·금지파일·**원문 미포함 스캔** 포함) | ⬜ PENDING | 콘솔 로그 |
| A2 | ZIP SHA256 = ReleaseNote SHA256 | ⬜ PENDING | `Get-FileHash` |
| A3 | (STAB-WP-01 이후) 무인자 빌드가 0.6.0 산출·버전 불일치 실패 | ⬜ N/A until STAB | — |

## B. Gate B — 오프라인 Test PC (`docs/43 §3`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| B1 | ZIP 내부 필수: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/` | ⬜ | 트리 |
| B2 | ZIP 내부 금지 **0**: 모델·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx`·**내부규정/NCR 원문** | ⬜ | 트리/스캔 |
| B3 | **인터넷 차단** 실행 → **NoModelMode** · 자동업데이트/telemetry/외부 API **0** | ⬜ | 차단 캡처 + 로그 |
| B4 | R1: **CP949·UTF-8·XLSX** 입력 → 한도분석 **6상태** | ⬜ | 화면 |
| B5 | R1: **대사 9종**(원천합계=분석합계 PASS) | ⬜ | 화면/로그 |
| B6 | R1: **화면=리포트 동일 수치**(LIMIT_MONITORING == 대시보드) | ⬜ | 캡처 2장 |
| B7 | R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요) | ⬜ | 화면 |
| B8 | R3: 내부/NCR = **메타+표식만(원문 0)** · **NCR Rule Set 구조** 설명(검토용 초안, 계산 아님) | ⬜ | 화면 |
| B9 | SQL/VBA/Excel **검사**(자동실행 0) 동작 | ⬜ | 화면 |
| B10 | **History** 기록 + **Audit JSONL(해시)** | ⬜ | 캡처 |
| B11 | 종료/재실행 정상 | ⬜ | — |

## C. Gate C — 운영 반입 / Excel 2021 (`docs/41 §4`·`docs/28`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| C1 | Excel 2021에서 Report Open · **Formula Error 0** | ⬜ | 캡처 |
| C2 | **External Link 0 · Macro 0 · Formula Injection 0** | ⬜ | 검사 |
| C3 | 백신/EDR 통과 | ⬜ | 로그 |
| C4 | **PDB/개인경로 0** (STAB-WP-03 검증) | ⬜ | 스캔 |
| C5 | Code Signing 상태(운영 절차 Placeholder) | ⬜ | 기록 |
| C6 | Performance / Memory 측정 | ⬜ | 측정값 |
| C7 | Rollback 절차 확인 | ⬜ | 기록 |

---

## 증거 메타 (각 항목 공통 기입)
`PASS/FAIL/BLOCKED · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위로 PASS/BLOCKED 재판정 → 본 문서 상단 상태 갱신. 전부 PASS면 `docs/43 §3` Gate B + Gate C를 PASS로 봉인하고 v0.6.0 Release 핸드오프(`docs/43 §4`).

> 관련: `docs/41`(게이트)·`docs/43`(v0.6 릴리스)·`docs/44`(v0.5 historical)·`docs/28`(보안검토)·`docs/39`(STAB-WP-03 Integrity).
