# ADR-001 Initial Architecture

## 상태

Accepted

## 결정

초기 애플리케이션은 C# WPF + .NET self-contained win-x64 portable release로 개발한다.

## 배경

운영환경은 회사 업무망 또는 개발망 PC이며 외부 인터넷 연결이 없을 수 있다.
운영환경에는 개발도구 설치를 요구하지 않는다.

## 선택지

### Python

장점:
- 데이터 분석 라이브러리 풍부
- 빠른 PoC

단점:
- 운영환경 Python/pip/패키지 의존성 문제
- PyInstaller 패키징 및 백신 오탐 가능성
- 외부 라이브러리 검토 부담

### C# WPF

장점:
- Windows 11 데스크톱 앱에 적합
- Self-contained 배포 가능
- 운영환경 개발도구 불필요
- Excel/Windows 연계 용이

단점:
- 초기 UI/구조 설계 필요
- WPF 학습 비용

## 결론

운영환경 반입과 실행 안정성을 우선하여 C# WPF를 선택한다.
Python은 초기 범위에서 제외하고, 필요 시 Dev/Test의 보조 분석 도구로만 검토한다.
