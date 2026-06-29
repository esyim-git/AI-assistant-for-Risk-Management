# 38. v1.0 Master Roadmap & Release Train (v0.6.0 → v1.0.0)

> **현재 기준선 = v0.6.0 + STAB-WP-01~04 + STAB-UX-01/02 + UX-WP-01~03** (main `61cf782`, PR #76 머지 후). 본 문서는 v0.6.0 다음 단계부터 v1.0 Team Pilot까지의 통합 실행 로드맵이다.
> 개념 로드맵은 `docs/10`, WP 상세는 `docs/39`, 아키텍처 결정은 `docs/40`, 게이트는 `docs/41`, Gate 증거는 `docs/44`(v0.5)·`docs/45`(v0.6).

## 0. 기준선 (재설계 금지)
- v0.4.0 = MVP-1+2+3.
- **v0.5.0 = R1 Data & Limit Foundation — DONE** (CP949/UTF-8/XLSX 입력, Column Mapping, 실 Exposure-Limit Join + 공통 `LimitAnalysisResult`, 6상태, 대사 9종, Dashboard=Report 일원화).
- **v0.6.0 = R3 Regulation/NCR 구조 — DONE** (공개 규정 KB Metadata, Keyword/Inverted Index, 인용형 답변, `KbAccessPolicy`+`KbRepositoryGuard`(+build/03 패키징 스캔), NCR Rule Set 8요소 **구조**).
- **완료 기능(MVP-1~3, R1, R3)은 재설계하지 않는다.**
- **SmokeTest**: **646 PASS / 0 FAIL** (정본 합계 — STAB-UX-02(#76) local-gate 후 `Total=646`). 이력: 513(STAB-WP-02) → 572(STAB-WP-03b +59) → 579(STAB-UX-01 +7) → 602(UX-WP-01 +23) → 631(UX-WP-02/03 +29) → 646(STAB-UX-02 +15). 과거 484/502는 하니스가 합계를 안 찍던 시절의 미집계 추정치였고, STAB-WP-02가 **합계·도메인별 PASS/FAIL·실행시간**을 출력한다.
- ⚠️ R2(Risk Analytics)는 R3보다 뒤로 밀렸다(R3 먼저 출시). R2는 v0.6.1 STAB 이후.

## 1. 절대 원칙 (전 릴리스 유지)
Offline · 외부 NuGet 0 · 외부 API/Telemetry/AutoUpdate 0 · SQL/VBA/Golden6 자동실행 0 · 해시 Audit Log · NoModelMode · 실데이터/실 테이블·컬럼명/내부규정·NCR 원문/모델파일 repo 미포함 · 가중치 자동학습 0 · **기존 테스트 삭제·약화 금지** · 운영=Portable ZIP 실행 전용. **STOP 규칙**: 외부 의존성·Vector/Embedding·LLM Runtime·모델파일 필요 시 즉시 STOP+승인(`docs/41`).

## 2. Release Train (버전은 가이드, 순서는 의존성·운영리스크 기준)

| Release | 버전(안) | 테마 | 핵심 게이트 | 상태 |
|---|---|---|---|---|
| R1 | v0.5.0 | Data & Limit Foundation | Data Spec Gate | **DONE** |
| R3 | v0.6.0 | Regulation/NCR 구조 (공개·인용형 RAG + NCR Rule Set 구조) | RAG/NCR Approval Gate(코드레벨) | **DONE** |
| **STAB** | **v0.6.1** | **Stabilization**(빌드/버전 재현성·Release 보안·Integrity Manifest·정본 테스트 베이스라인·테스트 구조) | — | **STAB-WP-01/02/03/04 DONE**(#56/#57/#59/#61/#66) · **STAB-WP-05**(코드서명) **APPROVAL_REQUIRED** |
| PILOT | (병행) | v0.6 오프라인 Test PC Gate B/C 증거 | Pilot Gate B/C | **BLOCKED**(실 Test PC 증거 대기) |
| **UX** | (병행) v0.7.x | **Smart Assist / Inline Assist** (입력 중 자동완성·snippet·추천 문구·실시간 안전 힌트, **정적·NoModel**) + Resizable Layout(영속화 포함) | Gate A(보안) | **STAB-UX-01/02·UX-WP-01~03 DONE**(#68·#70·#72·#73·#76, local-gate) · **UX/STAB-UX 트랙 완료**(실 LLM 랭킹=R4 미구현; 실 Test PC Gate B/C BLOCKED) |
| R2 | v0.7.0 | Risk Analytics & Visualization (Semantic Hardening·Streaming·전일대비·차트) | Data Spec Gate | **설계 완료**(ADR-011 · R2-WP-01~04 WP+프롬프트 READY) · **R2-WP-01 NEXT**(UX/STAB 마감 후) |
| KB | v0.8.0 | Public Knowledge Pack (조항 원문 Chunk, keyword only) | RAG Approval Gate | 설계(원문 적재 STOP) |
| NCR | v0.8.x | Approved NCR Rule Pack 계약 | NCR Approval Gate | 설계(계수 미포함) |
| R4 | v0.9.0 | Local LLM **Adapter (설계 전용)** | Model Approval Gate | 설계만 + STOP |
| R5 | v0.9.x | Feedback Learning (승인 Example 검색) | — | **PARTIAL**(`Core/Feedback` 승격 구조 존재 · 영속/검색/재사용 확장 필요) |
| R6 | v1.0.0 | Team Pilot | Pilot Gate B/C | 설계 |

> **데이터 정확성·Release 무결성을 LLM보다 먼저** 확보한다. STAB(재현성/무결성) → PILOT(증거, 병행·user-driven) → R2(분석) → KB/NCR(승인형 적재 계약) → R4(LLM 설계만) → R5 → R6.

## 3. Capability → Release 배치 / 상태

상태 어휘: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED.

| Capability | Release | 상태 |
|---|---|---|
| 합성 한도 제거·DEMO_ONLY · CP949/UTF-8/XLSX 입력 · Column Mapping · Exposure-Limit Join+6상태 · 대사 9종 · Dashboard=Report | R1(v0.5.0) | **VERIFIED** |
| 공개 규정 KB Metadata · Keyword/Inverted Index · 인용형 답변 · KbAccessPolicy · KbRepositoryGuard(+build/03) | R3(v0.6.0) | **VERIFIED** |
| NCR Rule Set 8요소 구조 | R3(v0.6.0) | **SCAFFOLD_ONLY** (승인 Rule Pack·계수 미적재) |
| 빌드/버전 재현성 · 정본 테스트 베이스라인 · SmokeTest suite 구조(04) | STAB(v0.6.1) | VERIFIED(STAB-WP-01/02/04) |
| Release 보안 · Integrity Manifest(build측 03a) · 런타임 Fail-Closed(03b) | STAB(v0.6.1) | **VERIFIED**(local-gate; 03a #59, 03b #61. 실 Test PC Gate B/C 별도 BLOCKED) |
| Smart Assist **Core**(Engine·Context·Item·Provider 계약·Registry·NoModel) + Accept 해시 Audit | UX(v0.7.x) | **VERIFIED**(local-gate; UX-WP-01 #70. 계약+코어 한정) |
| 정적 Provider(SQL/VBA/Excel2021+365차단/SafetyHint/RiskPhrase) | UX(v0.7.x) | **VERIFIED**(local-gate; UX-WP-02 #72. 정적·NoModel·RuleSet 단일원천 재사용 한정; 실 LLM 랭킹=R4 미구현) |
| WPF Completion Popup(Ctrl+Space·선택 삽입·자동삽입 없음) | UX(v0.7.x) | **VERIFIED**(local-gate; UX-WP-03 #73. WPF 기본 컨트롤·외부 Editor 0·자동삽입 0 한정; 실 Test PC Gate B/C BLOCKED) |
| Risk Semantic Hardening(중복키/통화·단위 매핑/RECON_UNIT) | R2 | **PARTIAL**(Codex local-gate PASS, PR review pending) |
| Streaming/대용량 · 전일 대비 · 차트/Heatmap/TopN/집중도 · Excel Report 강화 | R2 | NOT_IMPLEMENTED |
| 공개 규정 **원문 Clause/Chunk 검색** | KB | NOT_IMPLEMENTED (현재 Catalog/Metadata까지) |
| 승인 NCR Rule Pack · 내부 Knowledge Pack | NCR/KB | APPROVAL_REQUIRED (Prod 적재, repo 미포함) |
| Local LLM Adapter 계약/Manifest/ProcessBoundary | R4 | 설계만, Runtime APPROVAL_REQUIRED |
| 승인 Feedback Example 검색·Prompt 반영 | R5 | **PARTIAL**(`Core/Feedback` 승격 구조 존재 · 영속/검색/Audit 확장 필요) |
| Test PC Gate B/C · Team Pilot | PILOT/R6 | **BLOCKED** |

## 4. 의존성 그래프
```text
R1(DONE) ─► R3(DONE) ─► STAB(v0.6.1) ─► R2(v0.7) ─► KB(v0.8) ─► R4(LLM 설계) ─► R5 ─► R6(Pilot)
                            │                              └► NCR(승인 Rule Pack)
                            └► PILOT Gate B/C (병행, 실 Test PC 증거 — BLOCKED)
```
- STAB는 모든 후속의 Release 신뢰 기반(재현성·무결성·정본 테스트).
- PILOT(Gate B/C)은 신규 기능과 분리해 **병행**(증거는 user/Test PC).
- R4는 설계만(런타임 STOP), R5는 R4 인터페이스+R3/KB에 의존, R6는 전체 안정화.

## 5. Traceability Matrix (Capability ↔ WP ↔ Test ↔ Gate)

| Cap-ID | Capability | WP | 검증 테스트(요지) | Gate | 상태 |
|---|---|---|---|---|---|
| C-01~08 | R1 데이터·한도·대사·일원화 | WP-01~08 | 6상태·대사 9종·동일수치 | Data | VERIFIED |
| C-10 | KB Metadata·역색인·인용·접근정책·원문가드 | R3-WP-01~04 | 검색 결정성·인용·Blocker 스캔 | RAG | VERIFIED |
| C-11 | NCR Rule Set 8요소 구조 | R3-WP-05 | 구조·조회전용 SQL·검토용초안 | NCR | SCAFFOLD_ONLY |
| C-12 | 빌드/버전 재현성·무결성·정본 테스트·테스트 suite 구조 | STAB-WP-01~04 | VERSION 단일원천·manifest 검증·런타임 Fail-Closed·정본 합계·suite 분리 | — | PARTIAL (STAB-WP-01~04 DONE #56/#57/#59/#61/#66; **STAB-WP-05 코드서명 APPROVAL_REQUIRED**) |
| C-22 | Smart Assist Core (Engine·Context·Item·Provider 계약·Registry·NoModel) | UX-WP-01 | 결정성·언어 라우팅·개수 상한·SafetyHint pinned·accept 해시 audit(원문 미저장) — `AssistTests`(+23) | A | **VERIFIED**(local-gate, #70. 계약+코어 한정; 실 Provider=C-23·UI=C-24 미구현) |
| C-23 | 정적 Provider (SQL/VBA/Excel2021+365차단/SafetyHint/RiskPhrase) | UX-WP-02 | 차단 DML/금지 API 미추천·365 대체힌트·RuleSet 재사용·실데이터 0 | A | **VERIFIED**(local-gate, #72. 정적·NoModel 한정; 실 Test PC Gate BLOCKED) |
| C-24 | WPF Completion Popup (Ctrl+Space·선택 삽입·자동삽입 없음) | UX-WP-03 | 자동삽입 없음·Source/Kind/RequiresReview 노출·결과패널 연계 | A | **VERIFIED**(local-gate, #73. WPF 기본 컨트롤·외부 Editor 0 한정; 실 Test PC Gate BLOCKED) |
| C-25 | Resizable Editor Layout (GridSplitter·고정높이 제거·창 Min·TextBox Stretch) | STAB-UX-01 | GridSplitter 존재·EditorRow 비고정·MinWidth/MinHeight·SQL/VBA Stretch·XAML Contract·기존 SmokeTest 보존 | A | **VERIFIED**(#68, local-gate; XAML Contract +7, 기능변경 0) |
| C-26 | Resizable Layout Persistence (창 크기·분할 비율·Safety 너비 세션 영속) | STAB-UX-02 | round-trip·손상→기본값 fallback·config 경로가드·clamp·**무결성 critical 비대상**·.gitignore | A | **VERIFIED**(local-gate, #76; `Total 631→646`, 무결성 미변경, NuGet 0) |
| C-13 | Risk Semantic Hardening | R2-WP-01 | 중복키 차단(DUPLICATE_LIMIT)·통화/단위 ColumnMapping·RECON_UNIT 활성·BASE_DT 검증/정규화·Join Audit | Data | **PARTIAL**(Codex branch `feature/r2-wp-01-semantic-hardening`, local-gate `Total=671 PASS=671 FAIL=0`; Claude review/merge pending) |
| C-14 | Streaming/Perf (대용량 streaming·행/바이트 상한·Welford 온라인 분산·중복 해시·결정적 프로파일) | R2-WP-02 | streaming==기존 결정성·상한 `InvalidDataException`·Welford 정확·`DuplicateRowCount` 불변·(선택)벤치 | Data | NOT_IMPLEMENTED (프롬프트 READY; 인박스 NuGet 0) |
| C-15 | 전일 대비(Prior-Day Analytics: Current/Prev/Δ·TopN movers·BASE_DT prior-day 결합·4구획 출력계약) | R2-WP-03 | prior-day comparison/New·Resolved/TopN ordering/state-transition 비숫자 mover/BASE_DT format mismatch/4구획 결정성·R1 6상태 보존 (LimitReconciliationTests +6) | Data | **NOT_IMPLEMENTED**(설계; Branch `feature/r2-wp-03-prior-day-analytics`, 논리 선행=R2-WP-01 BASE_DT 정규화) |
| C-16 | Visualization/Report(인박스 차트·Heatmap·TopN·집중도·정확 Exception Count) | R2-WP-04 | 정확 Exception Count(Number SoT)·신규 RISK_VISUAL 시트 배선·TopN/집중도 결정성·Heatmap 등급·NuGet 0 | Data | NOT_IMPLEMENTED (TODO) |
| C-17 | Knowledge Pack Contract/Ingestion | KB-WP-01~02 | Manifest·Chunk·인용검증 | RAG | 설계 |
| C-18 | Approved NCR Rule Pack | NCR-WP-01 | Pack 없으면 계산 차단·APPROVAL_REQUIRED | NCR | 설계 |
| C-19 | Local LLM Adapter 계약 | LLM-WP-01 | NoModel 유지·ProcessBoundary | Model | 설계만 |
| C-20 | 승인 Example 검색 | FEEDBACK-WP-01 | 승인 Example만·가중치 불변 | — | PARTIAL (`Core/Feedback` 승격 구조 존재 · 영속/검색/Audit 확장 필요) |
| C-21 | Gate B/C 증거 | PILOT-WP-01 | docs/45 12+항목 | Pilot | BLOCKED |

## 6. Risk Register

| RR | 리스크 | 영향 | 완화 | 상태 |
|---|---|---|---|---|
| RR-01 | 합성 한도 오인 | 高 | WP-01 차단 | 해소(R1) |
| RR-02 | CP949 깨짐 | 高 | WP-02 UHC 매핑표 | 해소(R1) |
| RR-03 | Dashboard≠Report | 高 | WP-07 공통 결과 | 해소(R1) |
| RR-05 | RAG vector 요구 | 中 | keyword index·STOP | 통제(R3) |
| RR-07 | 내부규정 원문 repo 유입 | 高(보안) | repo 미포함·`KbRepositoryGuard`+build/03 스캔 | 통제(R3) |
| **RR-11** | **빌드 기본 `-Version 0.2.0` ≠ VERSION 0.6.0 → 오버전 산출물** | 高(릴리스) | STAB-WP-01 VERSION 단일원천·불일치 시 실패 | 해소 |
| **RR-12** | **정본 테스트 수 불명(484/502 혼재)** | 中 | STAB-WP-02 합계+도메인 요약 출력 | 해소 |
| **RR-13** | **Release ZIP에 PDB/개인경로/Debug 자산 포함 가능** | 中(보안) | STAB-WP-03a Release 보안·allowlist·manifest(#59) | 해소 |
| **RR-14** | **핵심 파일(policy/rules/template/KB) 변조 미탐지** | 中 | STAB-WP-03a manifest + 03b 런타임 시작 검증(운영 Fail-Closed, #61). 잔여 co-tamper/런타임 DLL은 STAB-WP-05 서명 | 부분해소(03a/03b) · 서명 잔여 OPEN |
| RR-06 | Local LLM 무단 도입 | 高(원칙) | R4 설계만·STOP·Model Approval Gate | 통제 |
| RR-08 | 대용량/손상 파일 메모리 | 中 | R2-WP-02 Streaming·상한 | 계획 |
| RR-15 | 중복 한도키 임의선택(group.Last) | 中 | R2-WP-01 명시 차단/상태 | **PARTIAL**(Codex local-gate PASS; PR review pending) |
| RR-09 | main 직접 push 우회 | 中 | soft guard 유지(`docs/32`) | 통제 |
| RR-10 | 기존 테스트 약화 | 高 | 삭제/약화 금지·회귀 게이트 | 통제 |
| RR-16 | 실 Test PC 증거 없이 Gate PASS 표기(과대표기) | 高(신뢰) | 증거 없으면 BLOCKED 유지(`docs/45`) | 통제 |

## 7. 게이트 (상세: docs/41)
- **Data Spec Gate**(R1·R2) · **RAG/NCR Approval Gate**(R3·KB·NCR) · **Model Approval Gate**(R4) · **Pilot Gate B/C**(PILOT·R6). 실 Test PC 증거 없으면 PASS 금지.

## 8. 진행/핸드백
- WP 단위 실행·상태: `docs/39`(Resume Brief/원장). 작업 브랜치 `feature/<WP-ID>-*` → PR → main(squash, `(#N)`). Claude 계획 브랜치 `planning/*`.
- Claude↔Codex 루프: Claude(설계/WP/프롬프트) → Codex(WP 1개 구현) → Claude(리뷰·검증·Traceability 갱신) → 다음 WP.

> 관련: `docs/10`·`docs/39`·`docs/40`·`docs/41`·`docs/32`·`docs/08`/`docs/17`.
