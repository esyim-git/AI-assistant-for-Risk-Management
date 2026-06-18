# Codex MVP-1 Implementation Prompt

## 너의 역할

너는 이 프로젝트의 **Implementation Engineer / Test Engineer (Codex)** 다.
Claude Code가 정리한 문서·아키텍처·백로그를 기준으로, **작은 단위로 안전하게** 구현한다.
목표 추진 모드로 진행하되, 아래 금지사항과 완료 조건을 절대 위반하지 않는다.

## 반드시 먼저 읽을 파일

- `AGENTS.md` (구현 헌법 — 모든 구현에서 최우선)
- `CLAUDE.md` (절대 원칙, SQL 8단계/VBA/Excel/규정 10단계 기준)
- `README.md`
- `docs/21_Implementation_Backlog.md` (작업 항목 B-01~B-09 + **현재 구현 상태표**)
- `docs/26_Codex_MVP1_Implementation_Guide.md`
- `config/security_policy.json`, `rules/*.txt`

## 매우 중요 — 이미 구현된 것을 다시 만들지 말 것

스타터 v2에는 **룰 엔진 3종 + 모델 + UI + SmokeTest가 이미 구현**되어 있다.
(SqlSafetyChecker / VbaSafetyChecker / Excel2021FunctionChecker / SafetyFinding / SafetySeverity / RulePattern / TaskLogEntry / FeedbackLogEntry / MainWindow / SmokeTests)
따라서 **재구현 금지**. 미구현 갭에 집중한다.

## MVP-1 범위 (이번 작업)

우선순위 순서:

1. **B-01 RuleLoader** — `rules/*.txt`를 읽어 Checker에 룰 주입(현재 하드코딩 → 외부화). `REQUIRE_PRESENT:` 접두 패턴은 "부재 시 경고"로 해석.
2. **B-02~B-04** — 기존 Checker 검증/누락 패턴 보강(기존 SmokeTest 유지).
3. **B-05 DataProfiler** — 더미 CSV 프로파일링.
4. **B-06 Log Writer** — TaskLog/FeedbackLog **해시 기반** 기록(`logs/`).
5. **B-07 PolicyLoader** — `security_policy.json` 로드 및 강제(없으면 전부 false 폴백).
6. **B-08 UI 보강**, **B-09 SmokeTest 확장**.

범위 밖(LLM 초안 생성, 규정 RAG, Excel 리포트 생성, 피드백 학습 승격)은 **이번에 구현하지 않는다.**

## 금지 사항 (위반 시 즉시 중단·보고)

- 실제 Golden6 자동 접속/실행, 운영 DB 접속 문자열
- VBA 자동 실행
- 외부 API / 자동 업데이트 / telemetry 코드
- 모델 가중치 파일 커밋
- 회사 실데이터 / 내부규정 원문 생성·커밋
- 외부 다운로드, 불필요한 NuGet 추가(추가 전 사유 보고·승인)
- `git push --force`, `git reset --hard`, `main` 직접 덮어쓰기

## 완료 조건 (Definition of Done)

- `dotnet build` 성공(외부 NuGet 최소/없음)
- `dotnet run --project tests/RiskManagementAI.SmokeTests` → 전부 PASS, 실패 시 exit 1
- SQL DELETE/UPDATE/DROP 차단, 정상 SELECT 통과(Blocker 0)
- VBA Shell/WScript/Kill 탐지, Option Explicit 누락 경고
- Excel VSTACK/HSTACK/TEXTSPLIT/MAP/REDUCE/BYROW/BYCOL 탐지
- 룰이 `rules/*.txt`에서 로드됨(RuleLoader)
- 더미 CSV 프로파일링 동작
- TaskLog/FeedbackLog 해시 기반 기록
- Local LLM 없이 / 인터넷 없이 앱 실행 가능

## 테스트 조건

- 신규 기능마다 SmokeTest 케이스를 추가한다.
- 더미 데이터(`samples/dummy_data`)만 사용한다.

## 진행 방식

1. 먼저 **구현 계획**(어떤 B-xx를, 어떤 파일을, 어떤 순서로)을 제시한다.
2. 작은 단위로 수정하고 단위마다 빌드/SmokeTest를 실행한다.
3. NuGet이 필요하면 먼저 사유를 보고하고 승인 후 진행한다.

## 최종 보고 형식

1. 구현한 백로그 항목(B-xx)과 변경 파일 목록
2. 빌드/SmokeTest 결과(콘솔 출력 포함)
3. 추가한 NuGet(있다면)과 사유
4. 남은 작업/리스크
5. 다음 권장 작업 단위
