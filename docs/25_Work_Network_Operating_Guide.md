# Work Network Operating Guide

## 목적

회사 업무망/개발망 PC에서 RiskManagementAI를 운영환경으로 실행한다.

## 반입 대상

```text
RiskManagementAI-v0.2.0-win-x64-portable.zip
RiskManagementAI-v0.2.0-win-x64-portable.zip.sha256
ReleaseNote-v0.2.0.md
```

## 반입 제외

```text
소스코드 ZIP
Git repository
Visual Studio 설치 파일
Python 설치 파일
모델 파일
내부규정 원문
실데이터 파일
```

## 실행

```text
1. 지정 폴더에 압축 해제
2. run.bat 실행
3. Offline Mode 확인
4. 샘플 데이터 분석
5. 업무 파일 분석은 승인된 범위에서만 수행
```

## 로그 위치

```text
logs/
```

## 리포트 위치

```text
reports/
```

## 문제 발생 시

- ZIP 해시 불일치 여부 확인
- Excel 2021 설치 여부 확인
- 압축 경로에 쓰기 권한 있는지 확인
- logs 폴더 생성 가능한지 확인
- 모델 미설정 상태인지 확인
