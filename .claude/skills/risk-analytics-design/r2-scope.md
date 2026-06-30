# R2 Scope — Risk Analytics 설계 노트 (RR-15 · 전일대비 · 통화/단위 매핑)

> [/risk-analytics-design](SKILL.md) 지원 파일. **설계/체크리스트 가이드**이며 코드 동작을 바꾸지 않는다.
> 모든 항목은 절대 원칙(NuGet 0·결정성·승인형 매핑·실데이터 미포함·기존 테스트 보존)을 전제로 한다.
> 사실/한계는 본문에 복사하지 말고 **문서 경로 참조**로만 다룬다(원문·실데이터·실 컬럼명 금지, 더미명만).

---

## 0. R2 위치 (재설계 금지 기준선)
- R2 = Risk Analytics & Visualization. R1(DONE)·R3(DONE)·STAB 후속. 순서: `docs/38` §2 Release Train.
- Cap 매핑: C-13 Semantic Hardening / C-14 Streaming / C-15 전일대비 / C-16 Visualization (`docs/38` §5 Traceability).
- WP 매핑: R2-WP-01~04 (`docs/39`). 이 스킬은 **설계(ADR/WP 초안)** 까지만; 구현은 Codex.

---

## 1. RR-15 Semantic Hardening (R2-WP-01) — 설계 체크
중복 한도키 임의선택(`group.Last()`) 등 의미 하드닝. 근거: `docs/38` RR-15, `docs/39` R2-WP-01, `docs/41` §1 알려진 한계.

- [ ] **중복 Limit Key 차단/상태화**: 동일 Join Key(BASE_DT·PORTFOLIO_ID·RISK_FACTOR 류 더미) 다중 한도 행 → `group.Last()` 임의선택 제거. 명시 상태/대사로 노출. 기존 6상태(`NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`) **불변** 전제(추가 시 additive).
- [ ] **통화 매핑 전환**: 통화 비교를 하드코딩 `CCY_CD` 존재 기반 → **승인형 ColumnMapping 논리컬럼** 경유로 설계(`docs/41` §1 알려진 한계 해소 방향). 미승인 매핑 미반영·all-or-nothing 유지.
- [ ] **단위 매핑 + `RECON_UNIT_MISMATCH` 활성**: 단위 논리컬럼을 ColumnMapping으로 관리하고 대사코드를 양성/음성 모두 테스트 가능하게 활성. 기존 대사 9종(`RECON_*`) 의미 불변.
- [ ] **BASE_DT 정규화**: 기준일 형식 검증·정규화 규칙을 결정적으로 정의(비정상 입력 처리 명시). 임의 보정 금지·실패 시 명시 finding.
- [ ] **Audit Metadata**: Join 선택 규칙·정규화 규칙을 감사 메타로 기록(감사가능성). 해시-only 원칙과 충돌 없게(원문 텍스트 저장 금지).
- [ ] **결정성**: 동일 입력=동일 수치/상태. 비결정 정렬·임의선택 0.

### R2-WP-01 분해 시 채울 칸 (형식: `docs/39`)
목표 · 선행조건(STAB-WP-01~02) · 수정예상파일(`Core/Risk/LimitMonitor.cs`·`Core/Mapping/ColumnMapping*`·`Core/Risk/LimitAnalysisResult.cs`·tests) · 테스트(중복키 양성/차단·통화·단위 매핑·RECON_UNIT 양성/음성·BASE_DT 비정상 정규화·Audit 기록) · Branch(`feature/r2-wp-01-semantic-hardening`) · Commit · Claude Review Checklist(임의선택 제거 / RECON_UNIT / 매핑 일원화 / 결정성 / 기존 6상태·대사 불변 / Gate A).

---

## 2. 전일대비 (WP-09 설계 → R2-WP-03) — 설계 체크
기준일 N vs N-1 비교 모델. 근거: `docs/39` WP-09(설계 산출물)·R2-WP-03.

- [ ] **공통 Domain Model 재사용**: 공통 `LimitAnalysisResult` 기반(임의 데이터 생성 금지). Current·Previous·Δ·%·New/Resolved/Increased/Decreased·TopN.
- [ ] **분리 표기**: Data Fact / Methodology / User Validation / Hidden Risk 구분(설계로 명시).
- [ ] **결정성·감사가능성**: 비교 기준일 선택 규칙을 결정적으로·감사 메타에 기록.
- [ ] WP-09는 **설계 산출물**(구현은 R2). NEXT UP은 한 번에 1개만(`/risk-wp-planner`).

---

## 3. 절대 원칙 게이트 (모든 R2 WP 공통)
- [ ] 외부 NuGet PackageReference = 0 / 외부 API·Telemetry·자동업데이트 = 0 (인박스 BCL·OOXML·WPF만).
- [ ] SQL/VBA/Golden6 자동실행 0 · 운영 DB 접속문자열 0.
- [ ] 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀/키/토큰·모델파일 **repo·스킬파일 미포함**. 예시는 더미명만(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`).
- [ ] 기존 테스트 삭제·약화 0(각 WP additive·회귀 추가).
- [ ] 머지 게이트 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL` 증거 + Claude 코드리뷰(`CLAUDE.md §11.6`). GitHub CI green을 머지 전제로 요구하지 않음.
- [ ] **STOP**: 외부 라이브러리·NuGet·Vector DB·Embedding·Local LLM Runtime·모델파일·외부 차트 라이브러리가 필요해지면 즉시 STOP → 승인 문서(`docs/40`·`docs/41`).

---

## 4. 게이트 연결
- R2 = **Data Spec Gate** 후속(`docs/41` §1). 각 WP의 결정성·감사가능성·회귀를 Data Gate 항목에 연결한다.
- 실 오프라인 검증은 **Pilot Gate B/C**(`docs/41` §4)로 분리 — 현재 **BLOCKED**(실 Test PC 증거 대기). 증거 없으면 PASS 표기 금지.
- 상태 어휘 정본만(VERIFIED·PARTIAL·SCAFFOLD_ONLY·PLACEHOLDER·BLOCKED·NOT_IMPLEMENTED·APPROVAL_REQUIRED). 과대표기 금지.

---

## 5. 산출물
- R2 ADR 초안(`docs/40` 형식) + R2-WP 분해(`docs/39` 형식) + Data Gate 연결 서술.
- 계획 작업은 `planning/*` 브랜치(main 직접 수정 금지). 단일 WP 프롬프트화는 [/risk-wp-planner](../risk-wp-planner/SKILL.md), 데이터/한도 코드리뷰는 [/risk-data-limit-review](../risk-data-limit-review/SKILL.md).
