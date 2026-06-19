# Risk Management AI Assistant

금융회사 리스크관리 업무를 위한 **오프라인 실행형 Local AI Assistant** 프로젝트입니다.

이 v2 Starter는 환경을 명확히 분리합니다.

```text
GitHub / 개발 PC      = 개발환경 Dev
Local 실행 PC         = 테스트환경 Test
회사 업무망 PC         = 운영환경 Prod
회사 개발망 PC 포함    = 운영환경처럼 취급
```

핵심 원칙은 단순합니다.

> 운영환경에는 소스코드와 개발도구를 가져가지 않고, **Self-contained Release ZIP**만 반입한다.

---

## 1. 목표

초기 MVP는 다음 기능을 대상으로 합니다.

1. Golden6용 SQL 초안 생성 및 위험 구문 검사
2. Excel 2021 VBA 초안 생성 및 위험 API 검사
3. Excel 2021 함수 호환성 검사
4. Golden6 Export CSV/XLSX 데이터 프로파일링
5. 리스크 한도 모니터링 템플릿
6. Excel 2021 보고서 생성 템플릿
7. 규정/NCR Knowledge Base 구조
8. 승인형 피드백 학습 구조
9. 운영환경 오프라인 실행 패키징

초기 버전은 SQL/VBA를 자동 실행하지 않습니다.

---

## 2. 환경별 역할

| 구분 | 위치 | 역할 | 허용 | 금지 |
|---|---|---|---|---|
| Dev | GitHub + 개발 PC | 설계, 구현, 빌드, Release ZIP 생성 | Claude Code, Codex, Git, .NET SDK | 회사 실데이터, 내부규정 원문 |
| Test | Local PC | Release ZIP 검증, 더미 데이터 테스트 | Portable ZIP 실행, 해시 검증 | 운영 데이터 사용 |
| Prod | 회사 업무망/개발망 PC | 업무 실행 | 승인된 Release ZIP 실행 | 소스 빌드, 외부 API, 자동 업데이트 |

---

## 3. 배포 모델

개발환경에서 다음 산출물을 만듭니다.

```text
RiskManagementAI-v0.2.0-win-x64-portable.zip
RiskManagementAI-v0.2.0-win-x64-portable.zip.sha256
ReleaseNote-v0.2.0.md
DependencyList-v0.2.0.csv
```

운영환경에는 위 Release ZIP만 반입합니다.

운영환경 PC에는 아래가 없어도 됩니다.

```text
Visual Studio
VS Code
.NET SDK
Python
pip
Git
NuGet
Claude Code
Codex
인터넷 연결
```

운영환경 PC에는 아래만 필요합니다.

```text
Windows 11
Excel 2021
승인된 Release ZIP
```

---

## 4. 기술 스택

초기 추천 스택:

```text
UI          : C# WPF
Runtime     : .NET self-contained win-x64
Local Store : JSON / CSV / SQLite 후보
Excel       : Excel 2021 VBA / Interop 후보
SQL Tool    : Golden6 수동 실행
AI Model    : 초기 MVP에서는 optional
```

초기 MVP는 Local LLM이 없어도 실행됩니다.
모델이 없으면 AI 생성 기능은 비활성화되고, 룰 엔진/데이터 프로파일링/샘플 분석은 동작합니다.

### 왜 C#/.NET/WPF인가 (Python을 기본 구현에서 제외한 이유)

운영환경(회사 업무망/개발망 PC)은 **실행 전용**이며 .NET SDK·Python·pip·Git·인터넷이 없어도 동작해야 합니다.
Python(pip/pandas/openpyxl/pywin32/PyInstaller) 기반은 의존성 복원, 패키징, 백신 오탐, 외부 라이브러리 보안검토 부담 측면에서 운영환경 반입성이 불리합니다.
따라서 별도 도구 없이 실행되는 **self-contained win-x64 portable release**를 만들 수 있는 C#/.NET/WPF를 기본 스택으로 선택했습니다. (근거: `docs/11_ADR_Initial_Architecture.md`)
Python은 필요 시 Dev/Test의 보조 분석 용도로만 사용합니다.

---

## 5. 빠른 시작 Dev

```powershell
git clone https://github.com/esyim-git/AI-assistant-for-Risk-Management.git
cd AI-assistant-for-Risk-Management

# Claude Code로 문서/아키텍처 정리
# Codex로 MVP-1 구현

./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1 -Version 0.2.0
./build/02_package-release.ps1 -Version 0.2.0
```

---

## 6. 운영환경 실행

운영환경에서는 다음만 수행합니다.

```text
1. Release ZIP 반입
2. SHA256 해시 확인
3. 압축 해제
4. run.bat 실행 또는 RiskManagementAI.exe 실행
```

자세한 내용은 아래 문서를 참고합니다.

- `docs/22_Environment_Strategy.md`
- `docs/23_Offline_Deployment_Guide.md`
- `docs/24_Release_Packaging_Guide.md`
- `docs/25_Work_Network_Operating_Guide.md`

---

## 7. 보안 원칙

절대 Repository에 포함하지 않습니다.

```text
회사 실데이터
내부규정 원문
실제 테이블 사전
계정정보
비밀번호
토큰
대용량 모델 파일
고객정보
운영 로그 원문
```

이 프로젝트의 Repository는 **공개자료/더미데이터/구조/룰/템플릿/문서/소스코드**만 관리합니다.

---

## 8. Claude Code / Codex 역할

```text
Claude Code
- 프로젝트 구조 설계
- 문서/아키텍처 정리
- 개발 표준 정비
- 백로그 분해
- 리뷰 체크리스트 작성

Codex
- 실제 코드 구현
- 테스트 작성
- 버그 수정
- 리팩토링
- Release 빌드 스크립트 정비
```

각 지침 파일:

```text
CLAUDE.md  : Claude Code용 프로젝트 헌법
AGENTS.md  : Codex 구현 규칙
prompts/   : 초기 명령 프롬프트
```

---

## 9. 현재 구현 상태

이 Repository는 v2 Starter 상태이며, MVP-1 룰 엔진 핵심은 **이미 구현**되어 있습니다.

이미 구현됨:

```text
SQL/VBA/Excel2021 Safety Checker (동작)
SafetyFinding/SafetySeverity/RulePattern 모델
TaskLog/FeedbackLog 모델 (정의)
WPF MainWindow (3개 Checker 호출, 오프라인 표시)
SmokeTest 콘솔 (SQL/VBA/Excel 기본 케이스)
빌드/배포 스크립트, 더미 데이터, SQL/VBA 템플릿, 환경별 설정 템플릿, 반입 가이드
```

미구현(다음 작업 = Codex MVP-1):

```text
RuleLoader (rules/*.txt -> Checker 주입; 현재 하드코딩)
DataProfiler 로직 (모델만 존재)
TaskLog/FeedbackLog 저장기 (해시 기반)
security_policy.json 런타임 로딩/강제
```

미포함(설계상 제외):

```text
실제 Local LLM 모델 / 실제 Golden6 연결 / 실제 내부규정 / 실제 NCR 공식본 / 실제 회사 데이터
```

## 10. Codex MVP-1 시작 방법

```powershell
# Dev PC에서
dotnet build
dotnet run --project tests/RiskManagementAI.SmokeTests   # 전부 PASS 확인
```

이후 Codex는 다음 순서로 작업합니다.

1. `prompts/codex_mvp1_implementation_prompt.md`를 시작 프롬프트로 사용
2. `docs/21_Implementation_Backlog.md`의 B-01(RuleLoader)부터 구현
3. `docs/26_Codex_MVP1_Implementation_Guide.md` 절차 준수
4. 단위마다 빌드/SmokeTest, 작은 단위 커밋

> 백로그/가이드: `docs/21`, `docs/26` · 환경/배포: `docs/22`~`docs/25` · 보안/동기화: `docs/28`, `docs/29` · 데모: `docs/30`

### 10-1. 목표 추진(Goal) 모드로 Codex 실행 시

Codex를 자율 추진 모드로 돌리고, 그 결과를 Git Sync로 Claude Code가 인지하게 하려면:

1. 시작 프롬프트: `prompts/codex_goal_mode_prompt.md`
2. 상태 원장/결정/핸드백 규약: `docs/31_Codex_Goal_Mode_Worklog.md`

Codex는 `feature/mvp1-rule-engine`에서 작업하며 단위마다 `docs/31`을 갱신·push하고, Claude는 `git fetch`로 동일 문서를 읽어 진행 상태를 파악한다.
