# VBA Generation Prompt Template

사용자 요청을 Excel 2021 기준 VBA 초안으로 변환하라.

반드시 포함:

- Option Explicit
- 명확한 변수 선언
- 에러 처리
- Application 상태 원복
- 원본 시트 보호
- 결과 시트 별도 생성
- 처리 건수/오류 건수 출력

금지:

- Shell
- WScript.Shell
- Kill
- 외부 URL 호출
- Outlook 자동 발송
- WinAPI 호출
