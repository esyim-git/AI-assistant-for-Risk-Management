# 30. Demo Scenario — 일별 리스크 한도 모니터링 자동화

## 목적

`docs/20_Demo_Scenario.md`의 일반 데모를 확장하여, **일별 리스크 한도 모니터링**을 처음부터 끝까지(End-to-End) 보여주는 단일 시나리오를 정의한다.

## 적용 범위

- 더미 데이터(`samples/dummy_data/*.csv`)와 `sql/templates/limit_monitoring_template.sql` 기반의 시연.
- Local LLM 없이 동작하는 룰/프로파일링/템플릿 기능 범위.

## 제외 범위

- 실데이터, 실제 Golden6 자동 접속/실행, VBA 자동 실행, 외부 API. (전부 금지)

## 전제

- 운영/테스트 환경에서 모델이 없어도 동작.
- SQL은 초안/검증용이며 **사용자가 Golden6에서 직접 실행**한다.

---

## 시나리오 (10단계)

1. **데이터 준비**: 사용자가 Golden6에서 추출했다고 가정한 더미 CSV/XLSX를 준비한다.
   - 예: `samples/dummy_data/risk_exposure_sample.csv`(노출), `risk_limit_sample.csv`(한도).
2. **데이터 프로파일링**: Assistant가 파일을 읽어 행 수/컬럼 수/타입을 요약한다(B-05 DataProfiler).
3. **기본 점검**: 기준일(`BASE_DT`) 분포, Null, 중복 행, 숫자 컬럼 합계/이상값을 점검한다.
4. **한도 사용률 계산**: 포트폴리오/리스크팩터별 `노출 ÷ 한도`로 사용률(%)을 산출한다.
   - 근거 SQL 초안: `sql/templates/limit_monitoring_template.sql` (READ-ONLY, `:BASE_DT` 바인드).
   - SQL은 `SqlSafetyChecker`를 통과해야 한다(차단 명령 없음).
5. **초과/경고 식별**: 사용률 ≥ 100% = 초과(Blocker), ≥ 90% = 경고로 분류한다.
6. **Excel 2021 리포트 생성**: 아래 시트를 가진 리포트 초안을 만든다(Excel 365 전용 함수 금지).
7. **시트 구성**:
   - `SUMMARY` — 기준일, 총 포트폴리오 수, 초과/경고 건수
   - `DATA_PROFILE` — 행/컬럼/Null/중복/기준일 분포
   - `LIMIT_MONITORING` — 포트폴리오별 노출·한도·사용률·상태
   - `EXCEPTION_LIST` — 초과/경고 항목과 사유
   - `SQL_USED` — 사용한 검증/조회 SQL 초안
   - `CHANGE_LOG` — 생성 시각, 룰/템플릿 버전, 작업자
8. **AI_COMMENTARY (placeholder)**: 모델이 없으면 "모델 미탑재 — 코멘트 비활성" 안내만 표시.
9. **작업 이력 저장**: `TaskLogEntry`를 `logs/`에 해시 기반으로 기록한다(원문 저장 금지, B-06).
10. **사용자 최종 검토**: 사용자가 EXCEPTION_LIST와 SQL을 검토하고 Golden6에서 직접 재실행/확정한다.

---

## 한도 모니터링 Hidden Risk 체크 (시연 포인트)

- 기준일(`BASE_DT`) 조건 누락 / 일중 임시 데이터와 월말 확정 데이터 혼동
- 취소·재발행 거래 포함, 중복 Join으로 금액 부풀림
- 원화환산/환율 기준일 불일치, 포지션 부호 반대
- 한도 마스터 최신성 오류, 수기조정/예외승인 항목 누락

## 데모 성과지표 (KPI)

- 리포트 작성 시간 절감 (수작업 대비)
- 오류 탐지 건수 (기준일 누락/중복/Null/미매핑 한도)
- 검증 체크리스트 자동화 정도
- 팀 표준화 가능성 (동일 템플릿 재사용)
- 운영환경 반입 가능성 (오프라인 동작, 외부통신 없음)

## 테스트 방법

- `samples/dummy_data`로 4단계 사용률 계산 결과가 한도표와 일치하는지 확인.
- 초과/경고 분류가 EXCEPTION_LIST와 일치.
- 생성 SQL이 `SqlSafetyChecker`에서 Blocker 0건.
- 인터넷/모델 없이 전체 흐름 동작.

## 향후 확장

- MVP-2: Risk Column Mapper로 컬럼 자동 매핑.
- MVP-3: Excel 리포트 자동 생성 엔진, 한도 대시보드.
- MVP-5: Local Model 탑재 시 AI_COMMENTARY 실제 생성.

> 관련 문서: `docs/20_Demo_Scenario.md`, `docs/21_Implementation_Backlog.md`(B-05/B-06), `sql/templates/limit_monitoring_template.sql`
