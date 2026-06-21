# 44. Gate B Evidence — v0.5.0 (R1 Data Foundation)

## 목적 / 상태
v0.5.0 portable ZIP을 **실 오프라인 Test PC**에서 실행해 `docs/42 §3`·`docs/41 §4` **Gate B(파일럿)** 체크리스트를 **증거 기반**으로 봉인하기 위한 시트.

- **현재 판정: 🔴 BLOCKED (실 Test PC 증거 대기)** — repo/CI/로컬 아티팩트만으로는 오프라인 실행·6상태·대사·화면=리포트 증명 불가.
- 대상 패키지: **v0.5.0 (R1)**. R3(KB 검색·인용·NCR Rule Set)은 v0.5.0 **범위 밖** → 본 시트에서 제외(=v0.6.0 패키징 후 `docs/43 §3`에서 별도).
- 판정 규칙: 아래 모든 항목이 `PASS` 여야 전체 PASS. 하나라도 누락/불일치면 그 항목 `BLOCKED/FAIL` + 전체 BLOCKED 유지.

> 기록 선검증(데스크탑): **v0.5.0 ZIP SHA256 = `1E0AFD692A4F1FA4C4866BE921438A1AEAB943C5AE4E3FF1A1421BC91CA8A60D`** — 사용자 보고값과 **대조 일치**. (단, 이는 데스크탑 무결성 대조이며 항목 1·11의 **오프라인 Test PC 재실행**을 대체하지 않음.)

---

## 증거 레지스터 (Test PC 운영자 기입)

각 항목에 증거 유형(스크린샷/콘솔로그/파일경로)을 남기고 상태를 `PASS`/`BLOCKED`/`FAIL`로 표기.

| # | 항목 (`docs/42 §3`) | 증거 유형 | 상태 | 증거 위치 / 비고 |
|---|---|---|---|---|
| 1 | `build/00~03 -Version 0.5.0` 전부 통과(03이 해시·내용·금지파일 자동검증) | 콘솔 로그 | ⬜ PENDING | |
| 2 | ZIP 내부 필수 자산: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/` 존재 | 트리 캡처 | ⬜ PENDING | |
| 3 | ZIP 내부 금지파일 **0**: 모델(`*.gguf` 등)·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx` | 트리/스캔 | ⬜ PENDING | |
| 4 | **인터넷 차단** 실행 → **NoModelMode 기동** · 자동업데이트/telemetry/외부 API **0** | 차단 캡처 + 앱 로그 | ⬜ PENDING | 어댑터 off 또는 방화벽 차단 상태 함께 |
| 5 | **CP949** 입력 → 한도분석 **6상태**(NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR) | 화면 | ⬜ PENDING | |
| 6 | **UTF-8**(BOM/무BOM) 입력 → 정상 분석 | 화면 | ⬜ PENDING | |
| 7 | **.xlsx** 입력 → 정상 분석 | 화면 | ⬜ PENDING | |
| 8 | **대사 9종**: 미매핑·고아한도·중복·기준일·통화·단위·음수0한도·건수증폭 + **원천합계=분석합계(PASS)** | 화면/로그 | ⬜ PENDING | 키스톤 = 원천=분석 |
| 9 | **화면=리포트 동일 수치**: `LIMIT_MONITORING` == 대시보드 그리드 | 캡처 2장 | ⬜ PENDING | 동일 케이스 |
| 10 | **History** 기록 + **Audit JSONL(해시)** 1줄 | 캡처 | ⬜ PENDING | |
| 11 | ReleaseNote/DependencyList SHA256 == `Get-FileHash` 재대조 일치 | 콘솔 | 🟡 PARTIAL | 데스크탑 대조 일치(`1E0AFD…A60D`); Test PC 재대조 PENDING |
| 12 | 종료/재실행 정상 | — | ⬜ PENDING | |

---

## 회신 방법
위 표를 채운 사본(또는 스크린샷/로그 묶음)을 주시면, 항목 단위로 PASS/BLOCKED를 다시 판정하고 본 문서 상단 상태를 갱신한다. 전부 PASS면 `docs/42 §3` Gate B를 **PASS**로 봉인하고 v0.5.0 GitHub Release 핸드오프(`docs/42 §4`)로 진행.

> 관련: `docs/42`(v0.5.0 릴리스), `docs/41 §4`(Pilot Gate), `docs/28`(게이트 B/C), `docs/43 §3`(v0.6.0 Gate B — 패키징 후 별도).
