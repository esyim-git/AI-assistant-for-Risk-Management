# Gate B — 오프라인 Test PC 항목·증거 양식 (v0.7+)

정본 원장: `docs/48_GateBC_v0.7.0_Evidence.md` (R1 + R3 + R2). `docs/45`(v0.6.0)·`docs/44`(v0.5.0)는 historical이다.
본 파일은 **양식·판정 가이드**다. 코드 동작을 바꾸지 않으며, 실 데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문·secret·모델파일을 절대 기입하지 않는다. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만.

## 판정 규칙
- 모든 항목 `PASS`라야 전체 `PASS`. 하나라도 누락/불일치 → 해당 항목 + 전체 `BLOCKED` 유지.
- **실 오프라인 Test PC 증거가 없으면 `PASS` 금지** → `BLOCKED` 유지. (CLAUDE.md §11.4)
- `user-reported`는 봉인 증거가 아니다. 스크린샷·로그·File Hash·검증자·검증시각·Test PC 사양을 항목별로 연결해야 한다.
- published 출하 ZIP 라운드(R1)와 test-only 로컬 빌드 라운드(R2)는 분리 기록한다. test-only 증거로 published ZIP Gate를 봉인하지 않는다.
- 상태 어휘 정본만: `VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`.

## A. 선행 — 패키지 컷 / 무결성
| # | 항목 | 증거 | 판정 기준 |
|---|---|---|---|
| A1 | `build/00~03 -Version <ver>` 전부 PASS(해시·금지파일·원문 미포함 스캔 포함) | 콘솔 로그·ReleaseNote | 3종 자동검증 모두 PASS |
| A2 | GitHub Release ZIP SHA256 = `.sha256` = Release 본문 | Test PC `Get-FileHash` | Test PC 재대조 일치 |
| A3 | ZIP manifest 버전·필수 entries·PDB 0·Dev/Test config 0 | `approved_manifest.json`·verify 로그 | 불일치 0 |

> A그룹 선행은 `/risk-release-verify` 스킬과 연계한다.

## B. Gate B — 오프라인 Test PC 실행
| # | 항목 | 증거 유형 | 판정 기준 |
|---|---|---|---|
| B0 | Release ZIP을 Test PC에 반입하고 압축 해제 | 해제 경로·hash 캡처 | 소스 ZIP이 아니라 portable ZIP |
| B1 | ZIP 내부 필수 자산 존재(`*.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`) | 트리 캡처 | 누락 0 |
| B2 | ZIP 내부 금지 파일 0: 모델·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx`·내부규정/NCR 원문 | 트리/스캔 | 의심 파일 0 |
| B3 | 인터넷 차단 실행 → NoModelMode 기동 · 자동업데이트/telemetry/외부 API 0 | 차단 캡처 + 앱 로그 | 외부 통신 시도 0 |
| B4 | CP949·UTF-8·XLSX 입력 → 한도분석 **7상태**(`NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/DUPLICATE_LIMIT`) | 화면/리포트 | 3 입력 모두 정상, DuplicateLimit 포함 |
| B5 | 대사 9종 + 원천합계=분석합계(PASS) | 화면/로그 | 원천=분석 수치 일치 |
| B6 | 화면=리포트 동일 수치(`LIMIT_MONITORING`/Dashboard/Report) | 캡처 2장 이상 | 동일 케이스 수치 일치 |
| B7 | Streaming/Welford/대용량 경계는 local-gate 증거 참조 | 릴리스 노트·테스트 결과 | 실 PC 실행 필요 시 별도 항목화 |
| B8 | Prior-Day Analytics: Current/Prior/Delta/Hidden Risk 및 duplicate key flag | 화면/리포트 | 비교표·finding 일치 |
| B9 | RISK_VISUAL: TopN/Concentration/Heatmap/Exception Count | 화면/리포트 | Report와 UI 수치 일치 |
| B10 | WPF chart/heatmap 렌더·스크롤·탭 이동 | 캡처/짧은 영상 | 빈 화면·겹침·크래시 0 |
| B11 | KB Clause Search: 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요) | 화면 | 인용 블록 전 항목 표기 |
| B12 | NCR Rule Set 구조: 메타+표식만(원문 0), 계산 아님 고지 | 화면 | 원문 노출 0·과대표기 0 |
| B13 | SQL/VBA/Excel checker + UX assist: 검사 동작, 자동실행/자동삽입 0 | 화면 | 차단·경고 동작 확인 |
| B14 | History 기록 + Audit JSONL(해시-only) | 캡처·로그 일부 | 원문 미저장, 해시-only |
| B15 | 종료/재실행/설정 영속화 정상 | 캡처/로그 | 정상 재기동 |

## 증거 메타 (각 항목 공통)
`상태(PASS/FAIL/BLOCKED/PARTIAL) · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위 재판정 후 현재 Gate 시트(예: `docs/48` 또는 릴리스별 successor) 상단 상태를 갱신한다. 전부 `PASS`면 해당 릴리스 Gate B를 `PASS`로 봉인한다.

> 관련: `docs/41 §4`, `docs/48`. Gate C는 [gate-c-template.md](gate-c-template.md).
