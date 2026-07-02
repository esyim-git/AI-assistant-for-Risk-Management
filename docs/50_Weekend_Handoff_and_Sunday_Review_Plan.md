# 50. Weekend Handoff & Sunday 일괄 점검 계획 (2026-07-01 → 07-05)

> **맥락**: Claude 사용 한도 소진 임박 → Codex가 **일요일(07-05) 리셋 전까지 저위험 WP 진행** → 리셋 시 Claude가 **한 주치 결과를 일괄 점검(4축 리뷰 + truth-sync)**. 리셋 전 unspent 한도는 소멸하므로, 지금 남은 한도로 **Codex 작업 큐 + Sunday 리뷰 하네스**를 준비한다. 본 문서는 운영 계획이며 코드 동작을 바꾸지 않는다(절대원칙 §3·STOP §11.5·과대표기 금지 §11.4 전제).
> **✅ 실행 완료(2026-07-01)**: Codex가 큐 4 WP를 예상보다 빠르게 구현 → PR #108(FEEDBACK-WP-02)·#109(R2-WP-05)·#110(UX-WP-07)·#111(UX-WP-08). Claude가 **§C 계획대로 adversarial 4축 workflow(각 finding 독립 검증)** 로 점검 → 4건 전부 APPROVE·**0 confirmed findings** → squash 머지 → truth-sync 완료. 일요일까지 기다릴 필요 없이 사이클 1회 완주. 큐 소진, 다음 방향은 사용자 지정 대기.
> **기준선(갱신)**: main `7094d91`(VERSION 0.7.0, 확장 트랙 Wave 3 #124~#127 머지 후), 정본 SmokeTest `Total=900 PASS=900 FAIL=0`, NEXT UP=**방향 결정 대기(decision point)** — 확장 트랙 Wave 1~3(QA-WP-01~09 + UX-WP-10/11) 완료로 인박스 테스트 도메인 하드닝 스윕 완결·비게이트 저위험 큐 소진; 다음 후보=신규 기능 트랙 설계 / STOP·승인 게이트(STAB-WP-05 서명·NCR 실 Pack·R4) / Gate B/C 실 Test PC(user-driven). Gate B/C 실 Test = user-driven(`docs/48 §B″` 실행 런북 참조). (§B 큐는 Wave 1 기록·정본 순서는 `docs/39` 확장 트랙 참조.)

---

## A. Codex 주간 운영 규칙 (07-01 ~ 07-05, Claude 부재)
1. **한 번에 WP 1개.** `feature/<WP-ID>-*` — **독립 브랜치 off main** 권장(파일이 겹치는 WP만 순차 스택). 여러 WP를 병렬로 열되 각자 별개 PR.
2. **Claude 리뷰 전 main 머지 금지**(§11.1/§11.2). PR은 **열어둔 채 대기**. Codex 자체 머지 0.
3. **완료 WP마다 PR 본문에 로컬 게이트 증거 첨부**: `dotnet build` **0/0** · SmokeTest **`Total=N PASS=N FAIL=0`** + 도메인별 요약(**Unclassified=0**) · Gate A **0** · `dotnet list package` **PackageReference 0** · 변경 파일 목록 · 양성/음성 요지.
4. **STOP 게이트 미접촉**: R4 Local LLM Runtime·NCR 실 Rule Pack(계수)·STAB-WP-05 코드서명(인증서)·Gate B/C 실 Test PC 증거는 **승인 선행**(손대지 않음). 필요해지면 **STOP → 일요일 상의**.
5. **절대원칙 불변**: 외부 NuGet 0·외부 API/Telemetry/AutoUpdate 0·SQL/VBA/Golden6 자동실행 0·해시 전용 Audit(원문 미저장)·실데이터/원문/모델파일 repo 미포함·기존 테스트 삭제·약화 0·force push·hard reset 0.
6. **Automatic Skill Bridge**(`AGENTS.md §9`): 매 구현 전 `AGENTS.md`→`SKILLS.md`→관련 `SKILL.md`→대상 WP 프롬프트 self-read, 완료 보고에 **"Applied Skill Checklists"** 명시.

## B. Codex 작업 큐 (전부 **프롬프트 READY**·저위험·독립 브랜치 off main·일괄 리뷰 가능)
> 파일 겹침 0 → **병렬 진행 안전**. 권장 착수 순서 = 1 → 2 → 3 → 4(가치·명확성 순). 각 WP는 `docs/39` 원장에 등재.

1. **QA-WP-03** (Kb/Citation 하드닝) — `prompts/codex/QA-WP-03_kb_citation_hardening.md`. 검색·인용·원문 가드 경계 회귀. 파일: `tests/RiskManagementAI.SmokeTests/KbTests.cs`.
2. **QA-WP-04** (Report/RISK_VISUAL 하드닝) — `prompts/codex/QA-WP-04_report_hardening.md`. 정확 Exception Count·HHI 0분모·시트 배선 회귀. 파일: `tests/RiskManagementAI.SmokeTests/ReportTests.cs`.
3. **QA-WP-05** (Csv/Xlsx/DataProfile 하드닝) — `prompts/codex/QA-WP-05_csv_xlsx_profile_hardening.md`. CP949·streaming 상한·profile parity 회귀. 파일: `tests/RiskManagementAI.SmokeTests/{CsvTests,XlsxTests,DataProfileTests}.cs`.
4. **UX-WP-11** (Excel 2021 Function Helper 카탈로그 확장) — `prompts/codex/UX-WP-11_excel_function_catalog_expansion.md`. embedded 카탈로그 큐레이션 확장·차단 함수 추천 0 가드. 파일: `src/RiskManagementAI.Core/Excel/Resources/excel_function_help.json` + helper/Assist 테스트.

> STOP 게이트(R4 LLM·NCR 실 Pack·STAB-WP-05 서명·Gate B/C)는 여전히 **승인 선행** — 위 큐에 없음. 큐 소진 시 추가 WP는 일요일 Claude 프롬프트 작성.

## C. 일요일 일괄 점검 계획 (Sunday Comprehensive Review — Claude, 리셋 후)
0. **상태 파악**(`risk-status-sync`): `git log`·`list_pull_requests`로 main HEAD·정본 Total·**열린 PR 목록**·각 PR 대상 WP·의존 관계 재확인.
1. **PR별 4축 리뷰**(생성 순) — `risk-codex-review`(①범위=지정 단일 WP만 ②보안 ③테스트 ④문서·게이트) + `risk-security-guard`(Gate A) + **도메인 Skill**(feedback=`risk-feedback-learning`·UI=`risk-ui-ux-review`·data=`risk-data-limit-review`·RAG=`risk-rag-ncr-governance`) + `risk-smoke-governance`(단언 보존·`Total` 비감소·Unclassified=0) + `risk-branch-governance`.
   - 각 PR 체크: diff가 **지정 WP만** 다루는가 → 보안(원문/실데이터/실 테이블·컬럼/모델파일/NuGet/자동실행/평문 audit **0**) → 테스트(도메인 분류 정확·기존 단언 보존·양성/음성) → 문서 정합 → **로컬 게이트 증거**(build 0/0·`Total` PASS/0 FAIL·Gate A·PackageReference 0) 실재 확인.
   - **과대표기 금지**: 실 오프라인 Test PC 증거 없는 Gate는 **PASS 금지·BLOCKED 유지**(§11.4).
   - (선택) 예산 여유 시 **Workflow(다중 리뷰어×adversarial verify)**로 각 PR 병렬 심층 리뷰.
2. **머지**: 독립 PR은 승인분부터 squash. 스택이면 base부터, retarget 후 **`rebase --onto`**(#103 사례) — force-with-lease는 **브랜치 오너(사용자)** 작업(§8, Claude force push 금지).
3. **머지마다 truth-sync**(`risk-doc-truth-sync`): 기준선 SHA·`Total`·NEXT UP·**Traceability(`docs/38 §5`)**·`docs/39` Resume Brief·`CLAUDE.md`/`AGENTS.md`/`README.md`/`SKILLS.md` 정합.
4. **마무리**: 전체 정합 재확인 → 다음 **NEXT UP 1개** 지정.

## D. 지금(리셋 전) 남은 한도 사용 — 완료
- PR #107: truth-sync FEEDBACK-WP-01 DONE + FEEDBACK-WP-02 프롬프트 + 본 계획 + **cleanup WP 3종(UX-WP-07·UX-WP-08·R2-WP-05) 프롬프트 + `docs/39` 원장 등재**.
- **결과**: Codex 주간 큐 = **4 WP(전부 프롬프트 READY)**. 사용자는 #107 머지 후 Codex를 병렬(독립 브랜치)로 돌리고, 일요일 리셋 시 Claude가 §C대로 열린 PR 일괄 리뷰. Claude PR 워치 미설정(예산 보존).

> 관련: `CLAUDE.md §11`, `AGENTS.md §0·§9`, `docs/38`·`docs/39`, `SKILLS.md`, `prompts/codex/QA-WP-03_kb_citation_hardening.md`, `prompts/codex/QA-WP-04_report_hardening.md`, `prompts/codex/QA-WP-05_csv_xlsx_profile_hardening.md`, `prompts/codex/UX-WP-11_excel_function_catalog_expansion.md`.
