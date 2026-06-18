# Architecture

## 전체 구조

```text
RiskManagementAI.App
  ├─ UI / WPF
  ├─ Rule Engine
  ├─ SQL Assistant
  ├─ VBA Assistant
  ├─ Data Profiler
  ├─ Analytics Templates
  ├─ Report Export
  ├─ Knowledge Base
  ├─ Feedback Learning
  └─ Audit Log
```

## 실행 모드

| 모드 | 설명 |
|---|---|
| NoModelMode | 모델 없음. 룰/템플릿/데이터 분석만 사용 |
| LocalModelMode | 별도 Model Pack이 승인·설치된 경우 사용 |
| TemplateMode | 승인 예제/템플릿 기반 생성 |

## 운영환경 요구사항

운영환경은 self-contained release ZIP을 압축 해제해서 실행한다.
운영환경에서 NuGet restore, dotnet build, 외부 다운로드를 요구하지 않는다.

## 데이터 흐름

```text
Golden6 SQL 수동 실행
  ↓
CSV/XLSX Export
  ↓
RiskManagementAI 파일 분석
  ↓
Data Profile / Limit Monitoring / Exception List
  ↓
Excel 2021 Report 생성
  ↓
사용자 검토
```

## AI 흐름

```text
사용자 요청
  ↓
Prompt Template
  ↓
승인 예제 검색
  ↓
Local Model 또는 Template Engine
  ↓
Safety Checker
  ↓
검증 SQL/체크리스트 생성
  ↓
사용자 승인
```
