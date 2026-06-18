# AGENTS.md

Codex 및 구현 Agent는 이 파일을 반드시 읽고 따른다.

---

## 1. 역할

Codex는 이 프로젝트에서 **Implementation Engineer / Test Engineer** 역할을 수행한다.

Claude Code가 정리한 문서, 아키텍처, 백로그를 기준으로 작은 단위로 구현한다.

---

## 2. 구현 우선순위

1. 안전성
2. 감사 가능성
3. 유지보수성
4. 테스트 가능성
5. 성능
6. UI 미려함

---

## 3. 환경 요구사항

운영환경은 실행 전용이다.

```text
Prod PC에 없어야 하는 것:
- .NET SDK
- Visual Studio
- VS Code
- Python
- pip
- Git
- NuGet
- 인터넷 연결
```

따라서 구현물은 self-contained release ZIP으로 동작해야 한다.

---

## 4. 코딩 표준

- C# nullable enable 유지
- 외부 NuGet 의존성 최소화
- 예외 메시지는 사용자 친화적으로 작성
- 로그에는 민감정보 저장 금지
- 파일 경로는 상대경로 우선
- 운영환경에서 쓰기 경로는 logs/reports/config만 사용
- 모든 위험 검사 결과는 코드/심각도/메시지/위치 정보를 포함

---

## 5. 초기 MVP-1 구현 대상

> 주의: 아래 중 다수는 **이미 구현되어 있다.** 재구현하지 말고 `docs/21_Implementation_Backlog.md`의 현재 상태표를 먼저 확인한다.
> 미구현 갭(RuleLoader, DataProfiler 로직, Log 저장기, PolicyLoader)에 집중한다.

- SqlSafetyChecker (구현됨)
- VbaSafetyChecker (구현됨)
- Excel2021FunctionChecker (구현됨)
- TaskLog 모델 (정의됨), FeedbackLog 모델 (정의됨), SafetyRule/RulePattern 모델 (정의됨)
- RuleLoader (미구현 — 최우선)
- DataProfiler (모델만 존재 — 로직 구현 필요)
- SmokeTest 콘솔 (구현됨, 신규 기능 케이스 추가)

---

## 6. 금지 사항

- 실제 Golden6 자동 접속 구현 금지
- 실제 운영 DB 접속 문자열 포함 금지
- VBA 자동 실행 구현 금지
- 외부 API 호출 구현 금지
- 자동 업데이트 구현 금지
- telemetry 구현 금지
- 모델 파일 Repository 포함 금지
- 회사 실데이터 포함 금지

---

## 7. 테스트 원칙

초기에는 외부 NuGet 테스트 프레임워크에 의존하지 않는 SmokeTest를 제공한다.

필수 테스트:

- SQL 금지 명령 탐지
- SQL SELECT 정상 통과
- VBA Shell 탐지
- VBA Option Explicit 누락 탐지
- Excel 365 전용 함수 탐지
- 더미 CSV 프로파일링

---

## 8. Release 원칙

Release는 다음 과정을 통과해야 한다.

1. build/00_check-prereqs.ps1
2. build/01_publish-win-x64.ps1
3. build/02_package-release.ps1
4. build/03_verify-package.ps1
5. deploy/release_checklist.md 확인

운영환경 반입 대상은 source ZIP이 아니라 portable release ZIP이다.

---

## 9. 적용 우선순위 및 참조 문서

- **AGENTS.md의 지침은 모든 구현에서 최우선으로 적용한다.** 충돌 시 AGENTS.md > 백로그 > 프롬프트 순.
- 작업 시작 전 반드시 확인: `docs/21_Implementation_Backlog.md`(백로그·현재 상태), `docs/26_Codex_MVP1_Implementation_Guide.md`(절차), `prompts/codex_mvp1_implementation_prompt.md`(시작 프롬프트).
- 커밋/푸시 전 `docs/28_Security_Review_Checklist.md` 게이트 A, 동기화는 `docs/29_GitHub_Sync_Guide.md`를 따른다.
