# Codex MVP-2 Goal-Mode Prompt (Local LLM · RAG · Excel 리포트 · 피드백)

> Codex를 목표 추진(Goal) 모드로 MVP-2에 투입하기 위한 시작 프롬프트.
> 권위 있는 스펙·결정은 `docs/33_MVP2_Backlog.md`(M2-01~M2-06, DM-01~DM-05)에 있다.

## 0. 역할 / 모드

너는 Implementation Engineer(Codex)다. 목표 추진 모드로 `docs/33`의 M2 항목을 순서대로 구현하되,
**STOP 트리거**(아래)면 멈추고 `BLOCKED` 보고. 모든 진행은 worklog에 기록하고 Git에 push(핸드백).

## 1. 시작 전 (브랜치/동기화 — docs/32 거버넌스 준수)

```bash
git fetch origin
git switch -c feature/mvp2-llm-foundation origin/develop   # develop에서 분기 (없으면 origin/main)
dotnet build RiskManagementAI.sln
dotnet run --project tests/RiskManagementAI.SmokeTests       # 기준선 PASS 확인 (현재 119 PASS)
```

반드시 먼저 읽기: `AGENTS.md`, `CLAUDE.md`(§3 절대원칙, SQL 8단계/규정 10단계, Excel 함수 제한),
`docs/33`(백로그·결정), `docs/17`(RAG), `docs/28`(보안 게이트 A), `docs/32`(브랜치 거버넌스).

## 2. 구현 순서

1. **M2-01** LLM 추상화 + `NoModelMode`(모델 없이 기동)
2. **M2-02** SQL/VBA 초안 생성 파이프라인 — **생성물은 반드시 MVP-1 Safety Checker 통과 + audit log(해시)**
3. **M2-03** 규정/NCR catalog 검색 (공개 catalog만, 답변에 "검토용 초안"+출처)
4. **M2-04** Excel 2021 리포트 — **생성 방식(NuGet?) 결정 DM-03 선행: NuGet 필요시 STOP·승인**
5. **M2-05** 승인형 피드백 예제 승격 (재학습 아님)
6. **M2-06** UI 연동 + SmokeTest 확장

> 큰 항목은 더 작은 PR로 쪼개도 좋다(예: `feature/mvp2-llm-foundation`, `feature/mvp2-rag-catalog`).

## 3. 단위 루프 (항목마다)

```text
스펙 확인(docs/33) → 작은 단위 구현 → dotnet build → SmokeTest(신규 회귀 추가) →
보안 게이트 A(docs/28) → worklog 갱신 → commit(type) → git push (항목마다 즉시)
→ PR(→ develop) → CI(build) green → Tech Lead(Claude) 검증/병합
```

## 4. STOP — 즉시 중단·보고

- **NuGet 추가 필요**(예: OpenXML for M2-04) → 사유 먼저 보고·승인 후 진행 (`NuGet.Config` `<clear/>` 유지)
- 결정 핀다운(DM-01~05) 또는 절대원칙과 상충
- 모델/내부규정/실데이터/모델파일을 repo에 넣어야 하는 상황
- 외부 API/자동실행/telemetry/자동재학습이 필요한 설계
- `git push --force`, `git reset --hard`, `main`/`develop` 직접 push

## 5. 완료 조건 (docs/33 DoD)

- 모델/인터넷 없이 앱 기동(NoModelMode), build+SmokeTest 전부 PASS
- 생성 초안 = Safety Checker 통과 + audit log(해시), 규정답변 = "검토용 초안"+출처
- Excel 리포트 = 2021 호환(365 함수 0건), `reports/`에만
- 피드백 = 예제 승격만(재학습 없음), 외부API/모델파일 커밋 0건

## 6. 최종 보고 형식

1. 구현한 M2 항목 + 변경 파일
2. build / SmokeTest 결과(출력, 총 PASS 수)
3. NuGet 추가(있으면 사유·승인경위) — 없으면 "없음"
4. 보안 게이트 A 결과(0건)
5. push/PR 결과(브랜치·commit·PR 번호)
6. worklog 갱신 확인 + 남은 리스크/다음 항목

> 완료 후 Claude(Tech Lead)가 `git fetch` + CI로 재검증하고 리뷰 스레드를 정리한다.
