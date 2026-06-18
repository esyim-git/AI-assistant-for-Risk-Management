# Module Backlog

## MVP1-001 SQL Safety Checker

- 목적: Golden6용 SQL 초안의 위험 구문 탐지
- 입력: SQL Text
- 처리: 금지 패턴, 경고 패턴, SELECT 여부 검사
- 출력: SafetyFinding 목록
- 완료 조건: 금지 DML/DDL 탐지 가능
- 테스트 조건: SELECT 정상, DELETE 차단
- 보안 유의사항: SQL 자동 실행 금지

## MVP1-002 VBA Safety Checker

- 목적: Excel 2021 VBA 위험 코드 탐지
- 입력: VBA Text
- 처리: Shell, WScript, Kill, WinAPI 등 검사
- 출력: SafetyFinding 목록
- 완료 조건: 위험 API 탐지 가능
- 테스트 조건: Shell 탐지, Option Explicit 누락 경고
- 보안 유의사항: 자동 실행 금지

## MVP1-003 Excel 2021 Function Checker

- 목적: Excel 365 전용 함수 사용 탐지
- 입력: Formula Text 또는 VBA 생성 설명
- 처리: 금지 함수 검색
- 출력: 호환성 경고
- 완료 조건: VSTACK, TEXTSPLIT 등 탐지
- 테스트 조건: SUMIFS 정상, VSTACK 경고
- 보안 유의사항: 없음

## MVP1-004 Task/Feedback Log Model

- 목적: 작업/피드백 이력 구조화
- 입력: 작업 요청, 결과, 피드백
- 처리: JSON/CSV 저장 후보
- 출력: 감사 가능 로그
- 완료 조건: 민감정보 저장 방지 필드 설계
- 테스트 조건: 더미 작업 저장/조회
- 보안 유의사항: 원문 운영데이터 저장 금지

## MVP2-001 Golden6 Export Data Profiler

- 목적: CSV/XLSX Export 데이터 품질 점검
- 입력: CSV/XLSX 파일
- 처리: 행 수, 컬럼 수, Null, 중복, 기준일 분포
- 출력: DataProfileResult
- 완료 조건: CSV 더미 데이터 분석
- 테스트 조건: samples/dummy_data 검증
- 보안 유의사항: 운영 데이터 학습 저장 금지

## MVP3-001 Excel 2021 Report Template

- 목적: 리스크관리 보고서 시트 구조 생성
- 입력: 분석 결과
- 처리: Summary, DataProfile, ExceptionList 생성
- 출력: Excel Report
- 완료 조건: Excel 2021에서 열리는 보고서 생성
- 테스트 조건: 더미 데이터 리포트 생성
- 보안 유의사항: 원본 데이터 보호
