# 26. Codex MVP-1 Implementation Guide

> 이 문서는 `docs/26_Create_Solution_Command.md`(솔루션 생성 명령)와 별개로, Codex가 MVP-1을 구현/보강할 때의 작업 지침이다.
> 작업 백로그는 `docs/21_Implementation_Backlog.md`, 초기 프롬프트는 `prompts/codex_mvp1_implementation_prompt.md`를 함께 본다.

## 목적

Codex(Implementation Engineer)가 MVP-1 룰 엔진/데이터 프로파일링/로깅/최소 UI를 안전하고 감사 가능하게 구현하도록 한다.

## 적용 범위

- C# / .NET / WPF self-contained 앱
- `RiskManagementAI.Core`, `RiskManagementAI.App`, `RiskManagementAI.SmokeTests`

## 제외 범위

- Golden6 자동 접속, VBA 자동 실행, 외부 API, 모델 자동 재학습, 실데이터/내부규정 적재 → **모두 금지**

## 반드시 먼저 읽어야 할 파일

1. `AGENTS.md` (구현 헌법 — 모든 구현에서 우선 적용)
2. `CLAUDE.md` (절대 원칙, SQL/VBA/Excel 기준)
3. `README.md` (환경 분리, 배포 모델)
4. `docs/21_Implementation_Backlog.md` (작업 항목 B-01~B-09)
5. `docs/22_Environment_Strategy.md`, `docs/23_Offline_Deployment_Guide.md`
6. `config/security_policy.json`, `rules/*.txt`

## 솔루션/빌드 준비

```powershell
# Dev PC에서 1회
dotnet new sln -n RiskManagementAI
dotnet sln add src/RiskManagementAI.Core/RiskManagementAI.Core.csproj
dotnet sln add src/RiskManagementAI.App/RiskManagementAI.App.csproj
dotnet sln add tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj

dotnet build
dotnet run --project tests/RiskManagementAI.SmokeTests
```

## 구현 순서 (권장)

> 핵심: **이미 구현된 Checker를 다시 만들지 말 것.** 현재 상태표는 `docs/21`을 참조.

1. **B-01 RuleLoader** — `rules/*.txt`를 읽어 Checker에 주입(가장 가치 큰 갭).
2. **B-02~B-04 Checker 검증/보강** — 누락 패턴 추가, 기존 SmokeTest 유지.
3. **B-05 DataProfiler** — 더미 CSV 프로파일링.
4. **B-06 Log Writer** — TaskLog/FeedbackLog 해시 기반 기록.
5. **B-07 PolicyLoader** — `security_policy.json` 로드 및 강제.
6. **B-08 UI 보강**, **B-09 SmokeTest 확장**.

## 구현 규칙

- C# `nullable enable` 유지, 외부 NuGet **최소화**(추가 전 이유 명시).
- 모든 위험 검사 결과는 코드/심각도/메시지/매칭문자열/위치 포함.
- 쓰기 경로는 운영환경에서 `logs/`, `reports/`, `config/`만 사용.
- 파일 경로는 상대경로 우선. 외부 임의 경로 접근 금지.
- 예외 메시지는 사용자 친화적. 로그에 **민감정보 평문 저장 금지(해시만)**.
- 작은 단위 커밋, 각 단위마다 SmokeTest 통과 확인.

## 금지 사항 (위반 시 작업 중단)

- 실제 Golden6 접속/실행 코드
- 실제 운영 DB 접속 문자열
- VBA 자동 실행 코드
- 외부 API / 자동 업데이트 / telemetry 코드
- 모델 가중치 파일 커밋
- 회사 실데이터/내부규정 원문 생성·커밋
- `git push --force`, `git reset --hard`, main 직접 덮어쓰기

## 완료 조건

`docs/21_Implementation_Backlog.md`의 "MVP-1 전체 완료 조건" 전 항목 충족.

## 테스트 조건

- `dotnet build` 성공
- `dotnet run --project tests/RiskManagementAI.SmokeTests` → 모든 PASS, 실패 시 exit 1
- 인터넷/모델 없이 앱 기동 확인

## 진행 방식

1. 먼저 구현 계획(어떤 백로그 항목을, 어떤 파일을, 어떤 순서로)을 제시한다.
2. 작은 단위로 수정하고 단위마다 빌드/테스트한다.
3. NuGet 추가가 필요하면 먼저 이유를 보고하고 승인 후 진행한다.

## 최종 보고 형식

1. 구현한 백로그 항목(B-xx)과 변경 파일 목록
2. 빌드/SmokeTest 결과(출력 포함)
3. 추가한 NuGet(있다면)과 이유
4. 남은 작업/리스크
5. 다음 권장 작업 단위

## 보안 유의사항

- 운영환경 반입 대상은 source가 아니라 **portable release ZIP**이다(`docs/24`).
- 모든 구현은 "사람이 검토 가능한 초안 + 감사 로그" 원칙을 따른다.

## 향후 확장

MVP-2: Local LLM optional 초안 생성, 규정/NCR RAG placeholder, Excel 리포트 생성, 승인형 피드백 학습.
