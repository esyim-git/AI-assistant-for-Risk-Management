# Release Packaging Guide

## 목적

Dev 환경에서 운영환경 반입용 portable ZIP을 생성한다.

## 사전 요구사항 Dev only

- Git
- .NET SDK
- PowerShell
- 인터넷 접근 또는 사전 복원된 패키지

## 절차

```powershell
./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1 -Version 0.2.0
./build/02_package-release.ps1 -Version 0.2.0
./build/03_verify-package.ps1 -Version 0.2.0
```

## 산출물

```text
artifacts/release/RiskManagementAI-v0.2.0-win-x64-portable.zip
artifacts/release/RiskManagementAI-v0.2.0-win-x64-portable.zip.sha256
artifacts/release/ReleaseNote-v0.2.0.md
artifacts/release/DependencyList-v0.2.0.csv
```

## GitHub Release

GitHub Release에는 source ZIP이 아니라 portable release ZIP을 첨부한다.

## Release Note 필수 내용

- 버전
- 대상 OS
- 포함 기능
- 제외 기능
- 외부 통신 없음
- 실제 회사 데이터 없음
- 내부규정 원문 없음
- 모델 파일 없음 또는 별도 Model Pack
- SHA256
