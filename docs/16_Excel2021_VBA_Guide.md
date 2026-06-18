# Excel 2021 VBA Guide

## 원칙

- Excel 2021 호환
- Microsoft 365 전용 함수 금지
- 원본 시트 보호
- 결과 시트 별도 생성
- Option Explicit 필수
- 에러 처리 필수

## VBA 템플릿 필수 구조

```text
Option Explicit
Public Sub Main()
    On Error GoTo ErrHandler
    Application 상태 저장
    Application 상태 변경
    처리
CleanExit:
    Application 상태 원복
    Exit Sub
ErrHandler:
    오류 메시지
    Resume CleanExit
End Sub
```

## 금지 기능

- Shell
- WScript.Shell
- Kill
- 외부 URL 호출
- Outlook 자동 발송
- WinAPI 호출
