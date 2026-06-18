# Security Review Checklist

## Repository 점검

- [ ] 실데이터 없음
- [ ] 내부규정 원문 없음
- [ ] 비밀번호 없음
- [ ] 토큰 없음
- [ ] 접속 문자열 없음
- [ ] 모델 파일 없음
- [ ] 대용량 업무 파일 없음

## Release ZIP 점검

- [ ] Self-contained publish
- [ ] 외부 인터넷 없이 실행
- [ ] 자동 업데이트 없음
- [ ] telemetry 없음
- [ ] logs/reports 위치 명확
- [ ] SHA256 생성
- [ ] Release Note 작성
- [ ] Dependency List 작성

## 운영 점검

- [ ] 승인된 반입 절차 준수
- [ ] 바이러스 검사
- [ ] 사용자 권한 확인
- [ ] 내부규정 권한통제 확인
- [ ] 작업 로그 정책 확인
