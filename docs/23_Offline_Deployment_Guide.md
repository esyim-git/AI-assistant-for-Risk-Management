# Offline Deployment Guide

## 목적

회사 업무망 또는 외부 인터넷이 차단된 PC에서 별도 개발도구 설치 없이 RiskManagementAI를 실행한다.

## 배포 형태

Self-contained Windows x64 portable ZIP.

```text
RiskManagementAI-v0.2.0-win-x64-portable.zip
```

## 운영환경 필요사항

- Windows 11 x64
- Excel 2021
- 승인된 Release ZIP

필요하지 않음:

- Visual Studio
- VS Code
- .NET SDK
- Python
- pip
- Git
- NuGet
- 인터넷

## ZIP 구조

```text
RiskManagementAI
├─ RiskManagementAI.exe
├─ *.dll
├─ runtimes
├─ config
├─ rules
├─ kb
├─ templates
├─ samples
├─ logs
├─ reports
├─ run.bat
└─ README_OFFLINE_RUN.md
```

## 실행 절차

1. ZIP 해시 확인
2. 승인된 위치에 압축 해제
3. `run.bat` 실행
4. Dashboard에서 Offline Mode 확인
5. 샘플 데이터 분석으로 정상 동작 확인

## 제한사항

초기 MVP에서 Local LLM 모델이 없으면 AI 생성 기능은 비활성화된다.
단, 룰 엔진, 데이터 프로파일링, 샘플 분석, 템플릿 확인 기능은 동작한다.
