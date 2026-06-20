# 41. Approval & Pilot Gates (Data · RAG/NCR · Model · Pilot)

> Release Train(`docs/38`)의 게이트 정의. 각 Release는 해당 게이트를 통과해야 다음으로 진행한다.
> 보안 게이트 A(커밋, `docs/28`)·B/C(반입, `docs/28`·`docs/34`)와 별개의 **Capability 승인 게이트**.

---

## 1. Data Spec Gate (R1·R2)
**릴리스-완료(R1 전체 종료 / R2 진입 전) 게이트**다 — 개별 WP PR의 병합 조건이 아니다. **각 WP PR은 자기 DoD + 게이트 A + 자기 테스트만** 충족하면 머지한다(WP-01이 WP-02~07 항목까지 만족할 필요 없음; big-bang 금지). 아래는 **R1 마감 시** 전체 충족 항목.
- [ ] 합성/Demo 한도 산식 0개 (WP-01) — 실 한도 없으면 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY`
- [ ] Join Key(BASE_DT·PORTFOLIO_ID·RISK_FACTOR) + **승인된 Column Mapping**(WP-04)
- [ ] 상태셋 `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR` 정의·테스트
- [ ] 대사 9종(미매핑·중복·기준일·통화·단위·음수/0한도·건수증폭·원천합계 대사) (WP-06)
- [ ] **원천합계 = 분석합계 대사 PASS**(증폭/누락 0)
- [ ] CP949/UTF-8/XLSX 입력 검증 (WP-02/03) — CP949는 **경로 A(내장 Windows-949/UHC 디코더)**, **EUC-KR 범위 밖 UHC 확장 음절 라운드트립 포함**
- [ ] Dashboard·Report **동일 입력→동일 수치** (WP-07, 공통 AnalysisResult)
- [ ] 기존 SmokeTest 유지 + 신규 회귀
> CP949 결정(2026-06-20): **경로 A(repo 내장 Windows-949/UHC 디코더, NuGet 0)** 채택 — `System.Text.Encoding.CodePages` 패키지 **미도입**(불변식 유지). 향후 인코딩 코드페이지 확장 패키지가 필요하면 여기서 **승인** 후에만.

## 2. RAG / NCR Approval Gate (R3)
공개 규정·NCR 적재 및 검색 도입 게이트. (`docs/17`·`docs/08`)
- [ ] 적재 문서가 **공개 규정/공개 FAQ/승인된 내부규정**만 (자본시장법·시행령·금융투자업규정·시행세칙·NCR 해설 등)
- [ ] **내부규정 원문은 repo 미포함** — Prod에서 문서오너 승인·보안등급·역할권한·조회로그
- [ ] 문서 Metadata 완비: 문서ID·문서명·출처기관·출처·버전·시행일·폐기일·**파일 Hash**·적재일·승인상태·대체문서·**라이선스 상태**
- [ ] 검색 답변에 문서명·버전·시행일·조항·출처·검색기준일·**"검토 필요" 문구**
- [ ] 검색 엔진 = **Keyword/Inverted Index(NuGet 0)**. **Vector DB/Embedding Runtime/외부 라이브러리 필요 시 구현 STOP** + 승인 문서(라이선스·크기·오프라인 동작·보안) 작성
- [ ] NCR: **모델이 산식 암기로 답하는 구조 금지**. Rule Set·Rule Set Version·Effective Date·Component Map·Formula Description·Validation SQL Template·Regulation Basis·Approval History 구조로만
- [ ] 답변은 항상 **검토용 초안** 명시

## 3. Local LLM / Model Approval Gate (R4)
실제 추론 Runtime·모델파일 도입 전 **반드시** 통과. (ADR-003)
- [ ] 이 단계까지는 **Adapter/Interface/Manifest/Integrity/NoModel Fallback/Process Boundary 설계만** (런타임 0)
- [ ] 추론 Runtime/라이브러리/모델파일 추가가 필요해지면 **작업 STOP**
- [ ] 승인 문서: 구성요소·**라이선스**·보안·**크기**·성능·**반입방법**·무결성(Hash)·네트워크 격리·메모리 한도·크래시 복구
- [ ] **승인 전 Dependency/모델파일 추가 금지**. 모델파일·가중치 **repo 미포함**(`model_pack/` gitignored, Prod 적재)
- [ ] 모델 가중치 **자동학습 금지**
- [ ] 상태: **MODEL_APPROVAL_REQUIRED** (승인 전까지)

## 4. Pilot Gate B/C (R6) — 실행 계획 + 결과 양식
실제 Test PC 오프라인 검증. 현재 실 PC 미가용 시 **BLOCKED 표기 + 체크리스트/결과양식 선완성**.

### Gate B (Test PC, 오프라인 실행)
- [ ] Release ZIP **SHA256 확인**(릴리스 본문 값과 `Get-FileHash` 대조)
- [ ] 압축 해제 → **인터넷 차단** 상태 실행 → **NoModelMode 기동**
- [ ] 룰엔진 실행 / CSV·XLSX 입력 / **한도분석** / **Excel Report 생성** / History·Audit Log 확인
- [ ] 종료 및 재실행 정상

### Gate C (Excel 2021 + 환경)
- [ ] Excel 2021에서 리포트 열기 — **수식 오류 없음 / 외부 링크 없음 / Macro 없음 / Formula Injection 없음**
- [ ] 대용량 파일 처리 / **성능·메모리 측정** / 백신·EDR 확인 / **Code Signing** 확인 / Rollback 확인

### 결과 기록 양식 (각 항목)
```text
항목 | 결과(PASS/FAIL/N/A) | 측정값(시간·메모리·해시) | 비고 | 검증자 | 일시
```
> 실 PC 미가용 단계: 상태 **BLOCKED(Pilot PC 대기)**, 본 체크리스트·양식은 선완성.

## 5. Team Pilot Readiness Checklist (R6, `docs/10` 연계)
- **테스트**: 기존 268 유지 / 신규 기능 테스트 / Test Matrix / Golden File / 대용량 / CP949 / XLSX 손상 / RAG 인용 / 접근권한 / NoModel Fallback / Release Hash / 성능 / Memory / Offline Startup
- **문서**: 데모 스크립트 / 사용자·운영·관리자 가이드 / KB 업데이트 가이드 / Model Pack 가이드 / Incident Response / Rollback / Known Limitations / Pilot Feedback Form / Release Checklist
- **산출**: Release Candidate → Gate B/C PASS → v1.0.0

> 관련: `docs/38`(Train), `docs/39`(WP), `docs/28`(보안 게이트), `docs/34`(릴리스 리허설), `docs/32`(거버넌스), `docs/17`/`docs/08`(RAG/NCR).
