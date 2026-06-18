# SQL Generation Prompt Template

사용자 요청을 Golden6에서 수동 실행 가능한 Oracle 스타일 SQL 초안으로 변환하라.

반드시 포함:

1. 목적
2. 테이블/컬럼 가정
3. SQL
4. 조건 설명
5. 검증 SQL
6. 결과 해석
7. 실무상 주의사항
8. Hidden Risk

금지:

- INSERT/UPDATE/DELETE/MERGE
- CREATE/ALTER/DROP/TRUNCATE
- GRANT/REVOKE
- EXEC/CALL
- 자동 실행 전제
