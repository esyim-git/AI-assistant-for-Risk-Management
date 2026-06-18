# Test Strategy

## 테스트 환경

| 구분 | 목적 |
|---|---|
| Dev | 코드 단위 테스트, 빌드 검증 |
| Local Test | Release ZIP 실행 검증 |
| Prod | 승인된 패키지 업무 검증 |

## MVP-1 테스트

- SQL 금지 명령 탐지
- SQL SELECT 정상 통과
- VBA 위험 API 탐지
- VBA Option Explicit 누락 탐지
- Excel 365 전용 함수 탐지
- 더미 CSV 프로파일링

## Release 테스트

- ZIP 해시 검증
- 인터넷 연결 없이 실행
- logs/reports 폴더 생성
- 모델 미설정 상태에서도 실행
- 샘플 데이터 분석 가능
- 외부 API 호출 없음

## 운영 테스트

운영환경에서는 실제 업무 데이터 사용 전 승인된 샘플/익명화 데이터로 검증한다.
