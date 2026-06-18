너는 이 프로젝트의 Tech Lead 겸 Architect다.

현재 Repository는 Risk Management AI Assistant v2 Starter 상태다.
이번 버전의 핵심은 환경 분리다.

환경 정의:
- GitHub / 개발 PC = Dev
- Local 실행 PC = Test
- 회사 업무망 PC = Prod
- 회사 개발망 PC 포함 = Prod처럼 취급

핵심 원칙:
- 운영환경에는 소스코드와 개발도구가 아니라 self-contained release ZIP만 반입한다.
- 운영환경에서 .NET SDK, Visual Studio, Python, Git, NuGet, 인터넷 연결을 요구하면 안 된다.
- 운영환경에서는 RiskManagementAI.exe 또는 run.bat 실행만으로 앱이 기동해야 한다.
- Local LLM 모델은 optional이며, 모델이 없더라도 앱이 실행되어야 한다.
- 초기 MVP에서 SQL/VBA 자동 실행은 금지한다.

작업:
1. 전체 파일 구조를 분석하라.
2. README.md, CLAUDE.md, AGENTS.md, docs/22~25 문서를 점검하라.
3. build/ 스크립트가 portable release 요구사항을 만족하는지 확인하라.
4. 보안상 Repository에 포함되면 안 되는 항목이 있는지 확인하라.
5. docs/21_Implementation_Backlog.md를 Codex가 바로 구현할 수 있게 보강하라.
6. MVP-1 구현용 GitHub Issue 초안을 작성하라.
7. git status를 확인하고 안전한 branch에 commit/push할 준비를 하라.

금지:
- force push
- hard reset
- 외부 다운로드
- 회사 실데이터 생성
- 내부규정 원문 생성
- 비밀번호/토큰 생성

작업 후 분석 요약, 수정 파일, 다음 Codex 작업 5개를 보고하라.
