# 48. Gate B/C Evidence — v0.7.0 (R1 + R3 + R2)

## 목적 / 상태
v0.7.0 portable ZIP을 **실 오프라인 Test PC(Gate B)** 및 **운영 반입 / Excel 2021(Gate C)** 에서 실행해 `docs/41 §4`·`docs/47 §3` 체크리스트를 **증거 기반**으로 봉인하기 위한 **정본** 문서. v0.6.0 정본은 `docs/45`(R1+R3) — 본 문서는 그 위에 **R2(Risk Analytics & Visualization)** 항목을 추가한다.

- **현재 판정: 🔴 BLOCKED** — **선행 A(패키지 컷)는 충족**(v0.7.0 정식 릴리스 완료, 아래 A1~A3 PASS), 미충족은 **실 오프라인 Test PC 증거**뿐(repo/CI/로컬 빌드 아티팩트로 생성 불가).
- **판정 규칙**: 아래 모든 항목 `PASS`라야 전체 PASS. 하나라도 누락/불일치면 그 항목 `BLOCKED/FAIL` + 전체 BLOCKED 유지. **실 PC 증거 없이 PASS로 적지 않는다**(`CLAUDE.md §11.4`).
- **기준선**: main `30c1cfb`, VERSION `0.7.0`, **v0.7.0 정식 릴리스 태그 `30c1cfb`**(미서명 portable ZIP), 정본 SmokeTest `Total=714 PASS=714 FAIL=0`.

> 기록: v0.7.0 패키징은 Codex가 `build/00~03 -Version 0.7.0` PASS 보고 — **ZIP SHA256 = `42C835983127B127438AB97747B99FD0C3FA2E4363D4CB85641E45FE62E09DD5`**, manifest `version=0.7.0`(entries 26), ReleaseNote Build Commit `30c1cfb`. 단 **오프라인 Test PC 실행**이 Gate B/C 정본 증거다(아래 B·C는 미충족).
> ⚠️ ZIP 해시는 `AssemblyInformationalVersion`에 커밋 SHA가 박혀 **커밋마다 달라진다** → 검증 시 **태그 대상 `30c1cfb`에서 빌드한 ZIP의 `42C835…`를 정본**으로 대조한다.

---

## A. 선행 (패키지 컷 — 로컬 Windows) — ✅ 충족
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| A1 | `build/00~03 -Version 0.7.0` 전부 PASS (해시·금지파일·**원문 미포함 스캔** 포함) | ✅ PASS | Codex local-gate(#90), 콘솔 로그 |
| A2 | ZIP SHA256 = ReleaseNote SHA256 = Release 본문 SHA256 (`42C835…`) | ✅ PASS | `Get-FileHash` + Release 본문 |
| A3 | 무인자/`-Version` 불일치 빌드 실패 + `ExpectedVersion == VERSION`(0.7.0) drift 가드 | ✅ VERIFIED | STAB-WP-01·REL-v0.7.0 `PackagingTests:331`, SmokeTest `Total=714` |

## B. Gate B — 오프라인 Test PC (`docs/47 §3`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| B1 | ZIP 내부 필수: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`·`approved_manifest.json`(version `0.7.0`) | ⬜ | 트리 |
| B2 | ZIP 내부 금지 **0**: 모델·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx/p12/cer/crt/der`·**내부규정/NCR 원문** | ⬜ | 트리/스캔 |
| B3 | **인터넷 차단** 실행 → **NoModelMode 기동**(무결성 검증 PASS, manifest version `0.7.0`) · 자동업데이트/telemetry/외부 API **0** | ⬜ | 차단 캡처 + 기동 로그 |
| B4 | R1: **CP949·UTF-8·XLSX** 입력 → 한도분석 **7상태**(incl `DUPLICATE_LIMIT`) | ⬜ | 화면 |
| B5 | R1: **대사 9종**(원천합계=분석합계 PASS) | ⬜ | 화면/로그 |
| B6 | R1: **화면=리포트 동일 수치**(LIMIT_MONITORING == 대시보드, `DuplicateLimitCount` 노출) | ⬜ | 캡처 2장 |
| B7 | **R2-신규**: **대용량 CSV**(행 상한 200,000·바이트 상한 50MB 동작) → **스트리밍 프로파일 = in-memory 동일 수치**(Welford·Outlier parity) | ⬜ | 화면/로그 + 상한 초과 거부 캡처 |
| B8 | **R2-신규**: **전일대비**(Current/Prior 2일 입력) → New/Resolved/Increased/Decreased/Δ·TopN movers, same-day guard, `BASE_DT_FORMAT_MISMATCH`/`PRIOR_DAY_DUPLICATE_KEY` 표면화 | ⬜ | 화면 |
| B9 | **R2-신규**: **`RISK_VISUAL` 시트 생성**(7상태 분포·TopN·집중도 HHI·Heatmap·`MIXED_CURRENCY`) + **Exception Count = 정확 숫자**(COUNTA 아님, Number SoT) | ⬜ | 리포트/화면 |
| B10 | **R2-신규**: **WPF Shapes/Canvas 화면 차트** 렌더(외부 charting NuGet 0, 다양 창크기) | ⬜ | 캡처 |
| B11 | R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요) | ⬜ | 화면 |
| B12 | R3: 내부/NCR = **메타+표식만(원문 0)** · **NCR Rule Set 구조** 설명(검토용 초안, 계산 아님) | ⬜ | 화면 |
| B13 | SQL/VBA/Excel **검사**(자동실행 0) 동작 | ⬜ | 화면 |
| B14 | **History** 기록 + **Audit JSONL(해시)** | ⬜ | 캡처 |
| B15 | 종료/재실행 정상 | ⬜ | — |

## C. Gate C — 운영 반입 / Excel 2021 (`docs/41 §4`·`docs/28`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| C1 | Excel 2021에서 Report Open(**`RISK_VISUAL` 시트 수동열기 포함**) · **Formula Error 0** | ⬜ | 캡처 |
| C2 | **External Link 0 · Macro 0 · Formula Injection 0**(`RISK_VISUAL`은 Number/text 정적값만) | ⬜ | 검사 |
| C3 | 백신/EDR 통과 | ⬜ | 로그 |
| C4 | **PDB/개인경로 0**(자동 가드 + Integrity Manifest 생성/검증 = STAB-WP-03a, build/01·03) | ⬜ / 자동분 03a로 충족 | 스캔 |
| C4b | **런타임 Fail-Closed 무결성 게이트**(STAB-WP-03b, #61 — 앱 시작 시 manifest version `0.7.0` 검증, 변조/부재/축소/co-deletion=차단). **local-gate VERIFIED**; 실 Test PC 기동 증거만 대기 | ⬜ (코드=VERIFIED; Test PC 기동 BLOCKED) | 기동 로그·차단 캡처 |
| C5 | Code Signing 상태 = **미서명(placeholder)** → 독립 신뢰 앵커 = **STAB-WP-05 APPROVAL_REQUIRED**(`docs/40` ADR-012 / `docs/41 §6`; 인증서 경로 A~D 결정 선행). 03b 잔여 = **콘텐츠 co-tamper + 런타임 DLL 미해시 + 폴더 동반 변조**, 서명 후 폐쇄 | ⬜ (미서명 출하 — 의도) | 기록 |
| C6 | Performance / Memory 측정(**대용량 CSV 스트리밍 처리 시간·메모리** 포함) | ⬜ | 측정값 |
| C7 | Rollback 절차 확인(미서명 ZIP 반입 정책 → 서명본 대체 가능 시 STAB-WP-05) | ⬜ | 기록 |

---

## 증거 메타 (각 항목 공통 기입)
`PASS/FAIL/BLOCKED · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위로 PASS/BLOCKED 재판정 → 본 문서 상단 상태 갱신. 전부 PASS면 `docs/47 §3` Gate B + Gate C를 PASS로 봉인하고 v0.7.0 Gate B/C 증거 봉인(`CLAUDE.md §11.4` 준수 — 실 PC 증거 전까지 PASS 금지).

> 관련: `docs/41`(게이트)·`docs/47`(v0.7.0 릴리스)·`docs/45`(v0.6 증거·양식 출처)·`docs/40`(ADR-008 무결성·ADR-012 코드서명)·`docs/28`(보안검토)·`docs/39`(REL-v0.7.0·STAB-WP-05).
