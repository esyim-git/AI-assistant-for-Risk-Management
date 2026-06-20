# Roadmap

## v0.1 Starter

- 아이디어/문서/룰/샘플 구조

## v0.2 Environment Split Starter

- Dev/Test/Prod 환경 분리
- 운영환경 portable release 원칙
- Self-contained WPF skeleton
- Release packaging script

## v0.3 MVP-1 Rule Engine

- SQL Safety Checker
- VBA Safety Checker
- Excel 2021 Function Checker
- SmokeTest

## v0.4 MVP-2 Data Profiler

- CSV/XLSX Export 프로파일링
- Null/중복/기준일/금액 집계
- 컬럼 매핑

## v0.5 MVP-3 Report Generator

- Excel 2021 보고서 템플릿
- Summary/DataProfile/ExceptionList

## v0.6 MVP-4 KB/NCR

- 공개 규정 catalog
- NCR placeholder
- 내부규정 권한통제 설계

## v1.0 Team Pilot

- 팀원 테스트
- 피드백 학습
- 감사로그
- Release 절차 안정화

---

## (갱신) v0.4.0 이후 통합 Release Train

> 위 v0.x 항목은 MVP 트랙의 개념 로드맵이다. 실제로 **v0.3.0 = MVP-1+2**, **v0.4.0 = +MVP-3 UI**로 빠르게 수렴했다(SmokeTest 268). v0.4.0 **이후** 의 상세 실행 로드맵·Release Train·Capability 배치·Traceability·Risk Register는 **`docs/38_v1_Master_Roadmap.md`** 가 정본이다.

- **R1 Data & Limit Foundation** (실 Exposure-Limit Join, CP949/UTF-8/XLSX, Column Mapping, 대사, Dashboard·Report 공통 분석객체) — 데이터 정확성 우선
- **R2 Risk Analytics & Visualization** (전일대비, 차트·Heatmap·TopN, 리포트 강화)
- **R3 Regulation & NCR RAG** (공개 규정 인용형 검색, NCR Rule Set) — `docs/17`/`docs/08` 심화, RAG/NCR 승인 게이트(`docs/41`)
- **R4 Local LLM Adapter** (설계 전용, 런타임 STOP — Model 승인 게이트)
- **R5 Feedback Learning** (승인형, 가중치 자동학습 금지)
- **R6 Team Pilot** (Test Matrix, Gate B/C, RC → v1.0.0)

WP·게이트·ADR: `docs/39`·`docs/41`·`docs/40`.
