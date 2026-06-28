# Gate B — 오프라인 Test PC 항목·증거 양식

정본 원장: `docs/45_GateBC_v0.6.0_Evidence.md` (v0.6.0 R1+R3). v0.5.0 R1 전용은 historical `docs/44`.
본 파일은 **양식·판정 가이드**다. 코드 동작을 바꾸지 않으며, 실 데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret·모델파일을 절대 기입하지 않는다. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만.

## 판정 규칙
- 모든 항목 `PASS`라야 전체 `PASS`. 하나라도 누락/불일치 → 해당 항목 + 전체 `BLOCKED` 유지.
- **실 오프라인 Test PC 증거가 없으면 `PASS` 금지** → `BLOCKED` 유지. (CLAUDE.md §11.4)
- 상태 어휘 정본만: `VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`.

## A. 선행 — 패키지 컷 (로컬 Windows)
| # | 항목 | 증거 | 판정 기준 |
|---|---|---|---|
| A1 | `build/00~03 -Version <ver>` 전부 PASS (해시·금지파일·원문 미포함 스캔 포함) | 콘솔 로그 | 3종 자동검증 모두 PASS |
| A2 | ZIP SHA256 = ReleaseNote SHA256 | `Get-FileHash` | **Test PC 재대조** 일치(데스크탑만이면 `PARTIAL`) |
| A3 | (STAB 이후) 무인자 빌드 산출·버전 불일치 실패 | — | 해당 WP 전까지 `N/A` |

> A그룹 선행은 `/release-package-verify` 스킬과 연계.

## B. Gate B — 오프라인 Test PC 실행
| # | 항목 | 증거 유형 | 판정 기준 |
|---|---|---|---|
| B1 | ZIP 내부 필수 자산 존재(`*.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`) | 트리 캡처 | 누락 0 |
| B2 | ZIP 내부 금지 **0**: 모델·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx`·내부규정/NCR 원문 | 트리/스캔 | 의심 파일 0 |
| B3 | **인터넷 차단** 실행 → **NoModelMode 기동** · 자동업데이트/telemetry/외부 API **0** | 차단 캡처 + 앱 로그 | 어댑터 off 또는 방화벽 차단 동봉 |
| B4 | R1: **CP949·UTF-8·XLSX** 입력 → 한도분석 **6상태**(NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR) | 화면 | 3 인코딩 모두 정상 |
| B5 | R1: **대사 9종** + **원천합계=분석합계(PASS)** | 화면/로그 | 키스톤 = 원천=분석 |
| B6 | R1: **화면=리포트 동일 수치**(LIMIT_MONITORING == 대시보드 그리드) | 캡처 2장 | 동일 케이스 수치 일치 |
| B7 | R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요) | 화면 | 인용 블록 전 항목 표기 |
| B8 | R3: 내부/NCR = **메타+표식만(원문 0)** · NCR Rule Set 구조 설명(검토용 초안, 계산 아님) | 화면 | 원문 노출 0 |
| B9 | SQL/VBA/Excel **검사** 동작(자동실행 0) | 화면 | 차단 동작 확인 |
| B10 | **History** 기록 + **Audit JSONL(해시)** 1줄 | 캡처 | 해시-only(원문 미저장) |
| B11 | 종료/재실행 정상 | — | 정상 |

## 증거 메타 (각 항목 공통)
`상태(PASS/FAIL/BLOCKED) · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위 재판정 후 `docs/45` 상단 상태 갱신. 전부 `PASS`면 `docs/43 §3` Gate B를 `PASS`로 봉인.

> 관련: `docs/41 §4`, `docs/43 §3`, `docs/45`. Gate C는 [gate-c-template.md](gate-c-template.md).
