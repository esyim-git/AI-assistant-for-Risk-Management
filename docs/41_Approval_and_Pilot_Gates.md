# 41. Approval & Pilot Gates (Data · RAG/NCR · Model · Pilot)

> Release Train(`docs/38`)의 게이트 정의. 각 Release는 해당 게이트를 통과해야 다음으로 진행한다.
> 보안 게이트 A(커밋, `docs/28`)·B/C(반입, `docs/28`·`docs/34`)와 별개의 **Capability 승인 게이트**.

---

## 1. Data Spec Gate (R1·R2)
**릴리스-완료(R1 전체 종료 / R2 진입 전) 게이트**다 — 개별 WP PR의 병합 조건이 아니다. **각 WP PR은 자기 DoD + 게이트 A + 자기 테스트만** 충족하면 머지한다(WP-01이 WP-02~07 항목까지 만족할 필요 없음; big-bang 금지). 아래는 **R1 마감 시** 전체 충족 항목.

> ✅ **R1 Data Spec Gate PASS — 2026-06-21** (Claude 검증, `main` `d8c45c6`, SmokeTest **368 PASS / 0 FAIL**). WP-01~08 머지 완료. 코드 레벨 게이트 항목 전부 충족. CP949 매핑표 EOL 결정성은 후속 PR(`.gitattributes` `text eol=lf` 고정 + 핀 `ca2d8cb6…`)로 byte-stable 확정. (실 Test PC 검증 = §4 Pilot Gate B/C, 별도·대기.)

- [x] 합성/Demo 한도 산식 0개 (WP-01) — 실 한도 없으면 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY` · *증거: `src/`에 `1.1m`/`BuildUiLimitRows`/`ExcelReportLimitRow` 0개(WP-01·WP-07), 빈 입력→`BuildEmptyLimitAnalysis`+`LIMIT_DATA_REQUIRED`*
- [x] Join Key(BASE_DT·PORTFOLIO_ID·RISK_FACTOR) + **승인된 Column Mapping**(WP-04) · *증거: `config/column_mapping.json` 기본 baseline=현행 상수(승인됨), all-or-nothing 커스텀·`config/` 경로 가드. **비-기본 커스텀 매핑은 미반영**(승인 시에만)*
- [x] 상태셋 `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR` 정의·테스트 (WP-05) · *증거: `enum LimitMonitorStatus`(6) + 6상태 분류 회귀*
- [x] 대사 9종(미매핑·중복·기준일·통화·단위·음수/0한도·건수증폭·원천합계 대사) (WP-06) · *증거: 9 `RECON_*` 코드 + `ReconciliationSummary`. 통화·단위는 **노출·한도 양쪽에 컬럼(`CCY_CD` 등) 존재 시 활성**(현재 한도 샘플 미포함 → R1 N/A·Applicable=false).* ⚠️ **알려진 한계**: 통화 비교가 현재 **하드코딩 `CCY_CD` 존재 기반**(ColumnMapping 미경유)이라, 미승인 `CCY_CD`가 포함된 한도 export에선 정보성 `RECON_CURRENCY_MISMATCH`(비-fail-code, PASS/FAIL 무영향)가 나올 수 있음 → **승인형 ColumnMapping 통화/단위 논리컬럼으로 전환은 R2 후속**(RR/Data Gate).
- [x] **원천합계 = 분석합계 대사 PASS**(증폭/누락 0) (WP-06) · *증거: `RECON_SUM_BALANCE`=합계일치 AND 누락0(비숫자/MappingError 누락 시 FAIL), `RECON_ROW_AMPLIFICATION`=기준일-필터 모집단 대비. fail-code 기반 PASS/FAIL*
- [x] CP949/UTF-8/XLSX 입력 검증 (WP-02/03) — CP949는 **경로 A(내장 Windows-949/UHC 디코더)**, **EUC-KR 범위 밖 UHC 확장 음절 라운드트립 포함** · *증거: `cp949-uhc-map.txt`(17,236 entries·SHA256 `ca2d8cb6…` **byte-stable**: `.gitattributes` `text eol=lf`로 EOL 고정 → 플랫폼 무관 동일 해시)·`힣` 라운드트립, `XlsxReader`(workbook-rels 시트해석·zip 안전상한·XXE 차단)*
- [x] Dashboard·Report **동일 입력→동일 수치** (WP-07, 공통 AnalysisResult) · *증거: `ExcelReportRequest`가 `LimitAnalysisResult` 소비, LIMIT_MONITORING=`MonitoringTable`(6상태·사용률 재계산 없음), EXCEPTION_LIST=분석 `ExceptionList`(`RECON_*`)+High validation*
- [x] 기존 SmokeTest 유지 + 신규 회귀 · *증거: R1 마감 시점 **368 PASS**(현재 정본은 STAB suite 분리 후 **572 PASS / 0 FAIL**), 삭제·약화 0(각 WP additive)*
> CP949 결정(2026-06-20): **경로 A(repo 내장 Windows-949/UHC 디코더, NuGet 0)** 채택 — `System.Text.Encoding.CodePages` 패키지 **미도입**(불변식 유지). 향후 인코딩 코드페이지 확장 패키지가 필요하면 여기서 **승인** 후에만.
> **R1 잔여(다음 단계)**: ① 실 Test PC 오프라인 검증(§4 Gate B/C) ② v0.5 릴리스 ZIP/태그 ③ WP-09(전일대비 설계, R2). Local LLM은 R4 Model Approval Gate(§3) 전까지 Runtime/모델 미도입(설계만).

## 2. RAG / NCR Approval Gate (R3)
공개 규정·NCR 적재 및 검색 도입 게이트. (`docs/17`·`docs/08`)

> ✅ **R3 RAG/NCR Gate — 코드/테스트(CI) 레벨 PASS — 2026-06-21** (Claude 검증, `main` `2fd0277`, SmokeTest **460 PASS / 0 FAIL**). R3-WP-01~05 머지 완료. **항목별 정밀 범위·후속을 그대로 명시**(과대표기 금지). 승인된 **실** 내부규정/NCR 원문·계수 적재는 Prod 문서오너 승인 후(repo 범위 밖).

- [x] 적재 문서가 **공개 규정/공개 FAQ/승인된 내부규정**만 · *증거: `KbAccessPolicy` — 공개 status(`CATALOG_ONLY`·`PUBLIC_APPROVED`·`APPROVED_PUBLIC`)만 `PublicCited`, 그 외 `MetadataOnly`/`ApprovalRequired`. repo엔 공개 catalog 메타만(WP-04)*
- [x] **내부규정 원문은 repo 미포함** — Prod에서 문서오너 승인·보안등급·역할권한·조회로그 · *증거: catalog 원문 컬럼 없음 + **`KbRepositoryGuard`**(kb/·data_sources/·samples/·`config/ncr` 스캔→Blocker), **SmokeTest/CI에서 실행** + **release 패키징(`build/03`) ZIP 추출 스캔**(동일 토큰 mirror + SmokeTest drift guard)로 원문 의심 파일명/내용 차단. 권한통제 적재는 Prod*
- [~] 문서 Metadata **스키마 완비** + 완비-경고 (문서ID·문서명·출처기관·출처·버전·시행일·폐기일·**파일 Hash**·적재일·승인상태·대체문서·**라이선스 상태**) · *증거: WP-01 9필드(출처 locator≠출처기관). ⚠️ **`file_hash`/실 version/effective_date 값은 repo에 비어있음**(원문 미포함 → 해시할 artifact 없음) → `(확인 필요)`+경고(WP-03)로 노출. **실 hash·시행일은 Prod 승인 적재 시** 채움(아래 ②)*
- [x] 검색 답변에 문서명·버전·시행일·조항·출처·검색기준일·**"검토 필요" 문구** · *증거: WP-03 인용 블록 전 항목, 검색기준일=주입 `IClock` 실제 날짜(placeholder 금지)*
- [x] 검색 엔진 = **Keyword/Inverted Index(NuGet 0)**. **Vector/Embedding 필요 시 STOP** · *증거: WP-02 `KbIndex`(역색인, substring L=32 cap + 긴쿼리 linear fallback, 결과 현행 동일). **Vector/Embedding 미도입**(STOP 규칙)*
- [x] NCR: **모델이 산식 암기로 답하는 구조 금지**. Rule Set 8요소(Version·Effective Date·Component Map·Formula Description·Validation SQL·Regulation Basis·Approval History) · *증거: WP-05 `NcrRuleSet` 8요소, 구조 기반(산식값 하드코딩 0), 샘플=placeholder(`APPROVAL_REQUIRED_NO_REAL_COEFFICIENT`), Validation SQL=조회전용(`SqlSafetyChecker`)·자동실행 0, NCR 공식본 원문 repo 미포함*
- [x] 답변은 항상 **검토용 초안** 명시 · *증거: `KbSearch` ReviewDraftNotice + `NcrRuleSet.DraftNotice`*
> **R3 후속 상태**: ① **DONE - `KbRepositoryGuard` 원문 미포함 토큰을 release 패키징 검증(`build/03`)에 연결**(`build/03`이 portable ZIP을 임시 추출해 `kb/`·`config/`·`samples/`·`data_sources/`의 의심 파일명/내용을 차단, SmokeTest drift guard 포함). ② 실 `file_hash`·version·시행일은 **Prod 승인 적재 시** 채움(repo는 스키마+경고까지) ③ 승인된 실 내부규정/NCR 원문·계수 = Pilot/Prod 문서오너 승인 후 권한통제 KB(repo 미포함 유지). Local LLM은 **R4 Model Approval Gate(§3)** 전까지 설계만.

## 3. Local LLM / Model Approval Gate (R4)
실제 추론 Runtime·모델파일 도입 전 **반드시** 통과. (ADR-003)
- [ ] 이 단계까지는 **Adapter/Interface/Manifest/Integrity/NoModel Fallback/Process Boundary 설계만** (런타임 0)
- [ ] 추론 Runtime/라이브러리/모델파일 추가가 필요해지면 **작업 STOP**
- [ ] 승인 문서: 구성요소·**라이선스**·보안·**크기**·성능·**반입방법**·무결성(Hash)·네트워크 격리·메모리 한도·크래시 복구
- [ ] **승인 전 Dependency/모델파일 추가 금지**. 모델파일·가중치 **repo 미포함**(`model_pack/` gitignored, Prod 적재)
- [ ] 모델 가중치 **자동학습 금지**
- [ ] 상태: **MODEL_APPROVAL_REQUIRED** (승인 전까지)

## 4. Pilot Gate B/C (R6) — 실행 계획 + 결과 양식
실제 Test PC 오프라인 검증. **실 오프라인 Test PC 증거가 없으면 Gate B/C는 PASS로 적지 않고 BLOCKED를 유지한다**(§11.4). 현재 R6 Team Pilot은 실 Test PC 미가용으로 **BLOCKED** — 체크리스트/결과양식만 선완성한다. (R1/R3 Capability 게이트의 PASS는 §1·§2의 **코드/CI(local-gate) 레벨**이며 실 PC Gate B/C를 대체하지 않는다.)

### Gate B (Test PC, 오프라인 실행)
- [ ] Release ZIP **SHA256 확인**(릴리스 본문 값과 `Get-FileHash` 대조)
- [ ] 압축 해제 → **인터넷 차단** 상태 실행 → **NoModelMode 기동**
- [ ] 룰엔진 실행 / CSV·XLSX 입력 / **한도분석** / **Excel Report 생성** / History·Audit Log 확인
- [ ] 종료 및 재실행 정상

### Gate C (Excel 2021 + 환경)
- [ ] Excel 2021에서 리포트 열기 — **수식 오류 없음 / 외부 링크 없음 / Macro 없음 / Formula Injection 없음**
- [ ] 대용량 파일 처리 / **성능·메모리 측정** / 백신·EDR 확인 / **Code Signing** 확인(현재 placeholder — STAB-WP-05 **APPROVAL_REQUIRED**) / Rollback 확인

### 결과 기록 양식 (각 항목)
```text
항목 | 결과(PASS/FAIL/N/A) | 측정값(시간·메모리·해시) | 비고 | 검증자 | 일시
```
> 실 PC 미가용 단계: 상태 **BLOCKED(Pilot PC 대기)**, 본 체크리스트·양식은 선완성.

## 5. Team Pilot Readiness Checklist (R6, `docs/10` 연계)
- **테스트**: 현재 정본 SmokeTest **572 PASS / 0 FAIL** 유지 / 신규 기능 테스트 / Test Matrix / Golden File / 대용량 / CP949 / XLSX 손상 / RAG 인용 / 접근권한 / NoModel Fallback / Release Hash / 성능 / Memory / Offline Startup
- **문서**: 데모 스크립트 / 사용자·운영·관리자 가이드 / KB 업데이트 가이드 / Model Pack 가이드 / Incident Response / Rollback / Known Limitations / Pilot Feedback Form / Release Checklist
- **산출**: Release Candidate → Gate B/C PASS → v1.0.0

> 관련: `docs/38`(Train), `docs/39`(WP), `docs/28`(보안 게이트), `docs/34`(릴리스 리허설), `docs/32`(거버넌스), `docs/17`/`docs/08`(RAG/NCR).
