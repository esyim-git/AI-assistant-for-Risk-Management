# Golden6 Integration Guide

## 초기 원칙

Agent는 Golden6를 자동 제어하지 않는다.

## 흐름

```text
Agent가 SQL 초안 생성
  ↓
사용자가 SQL 검토
  ↓
Golden6에서 수동 실행
  ↓
결과 Export CSV/XLSX
  ↓
Agent가 파일 분석
```

## SQL 생성 기준

- 조회 전용
- 기준일 명시
- 검증 SQL 포함
- 테이블/컬럼 가정 표시
- Hidden Risk 표시

## 향후 확장

승인 후 읽기 전용 검증 계정으로 제한적 실행을 검토할 수 있다.
