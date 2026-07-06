---
name: risk-team-pilot
description: Prepare, run, and evaluate the v1.0 Team Pilot — Go/No-Go criteria, pilot kit, parallel-run scenarios, KPI collection, feedback loop, and sensitive-data rules. Gate-type skill; no pilot completion claims without real pilot evidence.
argument-hint: "[phase: prep|go-nogo|run|wrap]"
arguments: [phase]
disable-model-invocation: true
allowed-tools: Read Grep Glob Edit Write Bash(git status *) Bash(git log *)
---

# Team Pilot (R6 / v1.0)

## 목적
Team Pilot(R6)의 준비(Pilot Kit)·진입 판정(Go/No-Go)·운영(시나리오/피드백/KPI)·종료 보고를 표준화한다. 파일럿은 실 사용자·실 환경 검증이므로 **실 증거 없이 완료/성공을 표기하지 않는다**(과대표기 금지, `CLAUDE.md §11.4`). 본 스킬은 문서/절차 가이드이며 코드 동작을 바꾸지 않는다.

## 언제 사용
- Pilot Kit(비개발자용 퀵스타트·Known Limitations·피드백 양식·인시던트/롤백 1페이지) 작성/검토.
- 파일럿 진입 판정(Go/No-Go), 파일럿 중간 체크인, KPI 집계, 종료 보고.

## 절대 원칙
- **Gate B/C 봉인 선행**: Gate B 전 항목 PASS(명시 예외 수용 포함) + Gate C(C5는 서명본 또는 미서명 `ACCEPTED_RISK` 서면 수용) 없이 파일럿 Go 금지(`docs/41 §4·§5`, `docs/48`).
- **병행 수행 원칙**: 파일럿 초기에는 기존 절차를 대체하지 않고 **이중 수행** — 도구-수기 수치 일치가 확인된 후에만 도구 산출물을 1차본으로 승격. 수치 불일치 발생 시 즉시 조사(파일럿 지속 여부 판단 대상).
- **민감정보 금지**: 파일럿 피드백·화면 캡처·KPI 증적에 **실거래/실포지션/고객정보/내부규정 원문/계정·비밀정보 0** — dummy sample·masking된 화면만. 피드백 본문은 승인형 파이프라인(ingest 게이트: SQL/VBA Blocker 0 + `ForbiddenTermScanner` 0) 경유, audit는 해시 전용.
- 도구 산출물은 항상 **검토용 초안**(자동 실행 0·공식 해석 아님) — 온보딩에서 명시적으로 시연·공지한다.
- KPI는 **실측만** 기입(추정치 금지). 로그 기반 집계는 해시 카운트 등 비식별 방식만 사용.

## 절차
1. **Go/No-Go 판정(prep/go-nogo)**: Gate B/C 봉인 상태 · Pilot Kit 완비 · 롤백 리허설 기록(C7) · 참여자/기간 확정을 점검. 미충족 항목은 `BLOCKED`로 명시하고 Go를 내리지 않는다.
2. **구성**: 참여자 **2~5인**(한도 모니터링 · 리포트 · SQL/VBA · NCR/규정 역할 커버) · **4~6주** · 주 1회 체크인.
3. **온보딩**: 퀵스타트 실습 + **"자동 실행 없음" 시연**(SQL/VBA는 검사·초안뿐임을 확인시킴) + Known Limitations 공유 + 피드백 방법 안내.
4. **운영 시나리오(병행 수행)**: ① 아침 한도 점검(export→프로파일→한도분석 7상태→대사 9종→리포트/RISK_VISUAL) ② SQL 검증 보조(차단/경고 확인→검증 SQL→사람 실행) ③ 규정 확인 초안(인용·검토필요 표식 확인→담당자 판단). 각 시나리오 종료 시 History/Audit 확인(감사 가능성 시연).
5. **피드백 수집**: 인앱 Feedback(승인형 Example 파이프라인) + 주간 설문 양식. 이슈는 재현 조건 중심으로 기록(민감정보 제외).
6. **KPI 집계(주간)**: ① 리포트 작성 시간(목표 −30%) ② 데이터 품질 이슈 조기 발견 건수(DUPLICATE_LIMIT·RECON_*·인코딩 등, 목표 ≥3건) ③ 도구-수기 수치 일치율(목표 100%) ④ SQL 검사 활용 빈도(주 ≥5건, 해시 카운트) ⑤ 보안 사고(목표 0건) ⑥ 지속 사용 의사(목표 ≥80%).
7. **종료 보고(wrap)**: KPI 실측표 + 발견 결함/개선 백로그(차기 WP 후보화) + 권고(확산 Go / 조건부 / 중단).

## 산출물/보고
- Go/No-Go 판정표(BLOCKED 사유 포함) · Pilot Kit 체크리스트 · 주간 KPI 표 · 종료 보고서(전 항목 실측 기반).
- 보고 예: `Pilot Go/No-Go = NO-GO (BLOCKED: Gate B B-6/B-8 증거 미봉인, Pilot Kit 미완) — 선행 작업: docs/48 §B″ 실행`.

## 참조
- `docs/41 §4·§5`(Pilot Gate B/C·Readiness Checklist) · `docs/48`(Gate B/C 증거 원장·런북)
- `docs/20_Demo_Scenario.md` · `docs/30_Demo_Scenario_Limit_Monitoring.md`(시나리오 원형) · `docs/25_Work_Network_Operating_Guide.md`(운영 반입)
- `docs/proposals/FABLE5_REPO_ASSESSMENT_PROPOSAL_20260706.md §11`(파일럿 계획·KPI 정본 초안)
- 연계 스킬: [/risk-gate-bc](../risk-gate-bc/SKILL.md)(선행 봉인) · [/risk-feedback-learning](../risk-feedback-learning/SKILL.md)(승인형 피드백) · [/risk-security-guard](../risk-security-guard/SKILL.md)(증적 민감정보 0) · [/risk-doc-truth-sync](../risk-doc-truth-sync/SKILL.md)(결과 반영)
