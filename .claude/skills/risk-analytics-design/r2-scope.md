# R2 Scope — 완료 기준선과 후속 분석 설계 노트

> [/risk-analytics-design](SKILL.md) 지원 파일. **설계/체크리스트 가이드**이며 코드 동작을 바꾸지 않는다.
> 모든 항목은 절대 원칙(NuGet 0·결정성·승인형 매핑·실데이터 미포함·기존 테스트 보존)을 전제로 한다.
> 사실/한계는 본문에 복사하지 말고 **문서 경로 참조**로만 다룬다(원문·실데이터·실 컬럼명 금지, 더미명만).

---

## 0. R2 위치 (재설계 금지 기준선)
- R2 = Risk Analytics & Visualization. R1(DONE)·R3(DONE)·STAB 후속. 순서: `docs/38` §2 Release Train.
- Cap 매핑: C-13 Semantic Hardening / C-14 Streaming / C-15 전일대비 / C-16 Visualization (`docs/38` §5 Traceability).
- **R2-WP-01~05는 완료 기준선**이다. 후속 설계는 이 기능을 재구현하지 않고 회귀 조건으로 둔다.

---

## 1. 완료 기준선 — 보존해야 할 불변식
- [ ] **7상태 보존**: `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/DUPLICATE_LIMIT`. 과거 상태셋으로 되돌리거나 DuplicateLimit를 숨기지 않는다.
- [ ] **중복 Limit Key 임의선택 금지**: 동일 Join Key 다중 한도 행은 명시 상태/대사로 노출한다.
- [ ] **통화·단위 ColumnMapping 경유 보존**: `CurrencyCode`/`UnitCode` optional logical column 매핑 기준. 예전 `CCY_CD` 하드코딩 한계를 미래 작업으로 다시 적지 않는다.
- [ ] **RECON_UNIT_MISMATCH 활성 보존**: 통화 대칭·non-fail 대사 의미 유지.
- [ ] **BASE_DT 정규화·검증 보존**: 기준일 형식 처리와 Audit metadata의 결정성 유지.
- [ ] **공통 AnalysisResult 재사용**: Dashboard·Report·Prior-Day·Visualization 간 수치 단일원천 유지.
- [ ] **결정성**: 동일 입력=동일 수치/상태. 비결정 정렬·임의선택 0.

---

## 2. 후속 분석 설계 체크
- [ ] **Prior-Day UI 배선**: Current/Prior/Delta/Top movers/Hidden Risk를 기존 `PriorDayAnalyzer` 결과에서 표시한다. 새 분석 엔진을 만들지 않는다.
- [ ] **Visualization 확장**: TopN·HHI·Heatmap·Exception Count는 `RiskVisualAggregator`/Report 기준 수치와 일치해야 한다.
- [ ] **대용량 안내**: Streaming/Welford 상한·부분 결과 표기는 계산 로직이 아니라 사용자 안내/상태 표면화로 설계한다.
- [ ] **Data Fact / Methodology / User Validation / Hidden Risk 구분**: 화면·리포트·감사 문구에서 섞지 않는다.
- [ ] **WPF callsite 한계**: local-gate는 계약 검증이며 실 렌더·포커스·Excel 열기는 Gate B/C 증거로 분리한다.

---

## 3. 절대 원칙 게이트
- [ ] 외부 NuGet PackageReference = 0 / 외부 API·Telemetry·자동업데이트 = 0 (인박스 BCL·OOXML·WPF만).
- [ ] SQL/VBA/Golden6 자동실행 0 · 운영 DB 접속문자열 0.
- [ ] 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀/키/토큰·모델파일 **repo·스킬파일 미포함**. 예시는 더미명만(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`).
- [ ] 기존 테스트 삭제·약화 0(각 WP additive·회귀 추가).
- [ ] 머지 게이트 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL` + Claude 코드리뷰 + 활성 hosted `test`/`wpf-build` exact-head green.
- [ ] **STOP**: 외부 라이브러리·NuGet·Vector DB·Embedding·Local LLM Runtime·모델파일·외부 차트 라이브러리가 필요해지면 즉시 STOP → 승인 문서(`docs/40`·`docs/41`).

---

## 4. 게이트 연결
- R2 = **Data Spec Gate** 후속(`docs/41` §1). 후속 WP의 결정성·감사가능성·회귀를 Data Gate 항목에 연결한다.
- 실 오프라인 검증은 **Pilot Gate B/C**(`docs/41` §4)로 분리 — 현재 **BLOCKED**(실 Test PC 증거 대기). 증거 없으면 PASS 표기 금지.
- 상태 어휘 정본만(VERIFIED·PARTIAL·SCAFFOLD_ONLY·PLACEHOLDER·BLOCKED·NOT_IMPLEMENTED·APPROVAL_REQUIRED). 과대표기 금지.

---

## 5. 산출물
- 후속 분석 ADR 초안(`docs/40` 형식) + WP 분해(`docs/39` 형식) + Data Gate 연결 서술.
- 계획 작업은 `planning/*` 브랜치(main 직접 수정 금지). 단일 WP 프롬프트화는 [/risk-wp-planner](../risk-wp-planner/SKILL.md), 데이터/한도 코드리뷰는 [/risk-data-limit-review](../risk-data-limit-review/SKILL.md).
