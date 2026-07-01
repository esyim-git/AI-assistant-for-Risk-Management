# 50. Weekend Handoff & Sunday 일괄 점검 계획 (2026-07-01 → 07-05)

> **맥락**: Claude 사용 한도 소진 임박 → Codex가 **일요일(07-05) 리셋 전까지 저위험 WP 진행** → 리셋 시 Claude가 **한 주치 결과를 일괄 점검(4축 리뷰 + truth-sync)**. 리셋 전 unspent 한도는 소멸하므로, 지금 남은 한도로 **Codex 작업 큐 + Sunday 리뷰 하네스**를 준비한다. 본 문서는 운영 계획이며 코드 동작을 바꾸지 않는다(절대원칙 §3·STOP §11.5·과대표기 금지 §11.4 전제).
> **기준선**: main `f8b330a`(VERSION 0.7.0), 정본 SmokeTest `Total=807 PASS=807 FAIL=0`, NEXT UP=FEEDBACK-WP-02.

---

## A. Codex 주간 운영 규칙 (07-01 ~ 07-05, Claude 부재)
1. **한 번에 WP 1개.** `feature/<WP-ID>-*` — **독립 브랜치 off main** 권장(파일이 겹치는 WP만 순차 스택). 여러 WP를 병렬로 열되 각자 별개 PR.
2. **Claude 리뷰 전 main 머지 금지**(§11.1/§11.2). PR은 **열어둔 채 대기**. Codex 자체 머지 0.
3. **완료 WP마다 PR 본문에 로컬 게이트 증거 첨부**: `dotnet build` **0/0** · SmokeTest **`Total=N PASS=N FAIL=0`** + 도메인별 요약(**Unclassified=0**) · Gate A **0** · `dotnet list package` **PackageReference 0** · 변경 파일 목록 · 양성/음성 요지.
4. **STOP 게이트 미접촉**: R4 Local LLM Runtime·NCR 실 Rule Pack(계수)·STAB-WP-05 코드서명(인증서)·Gate B/C 실 Test PC 증거는 **승인 선행**(손대지 않음). 필요해지면 **STOP → 일요일 상의**.
5. **절대원칙 불변**: 외부 NuGet 0·외부 API/Telemetry/AutoUpdate 0·SQL/VBA/Golden6 자동실행 0·해시 전용 Audit(원문 미저장)·실데이터/원문/모델파일 repo 미포함·기존 테스트 삭제·약화 0·force push·hard reset 0.
6. **Automatic Skill Bridge**(`AGENTS.md §9`): 매 구현 전 `AGENTS.md`→`SKILLS.md`→관련 `SKILL.md`→대상 WP 프롬프트 self-read, 완료 보고에 **"Applied Skill Checklists"** 명시.

## B. Codex 작업 큐 (우선순위 — 전부 저위험·일괄 리뷰 가능)
1. **FEEDBACK-WP-02** (NEXT UP) — `prompts/codex/FEEDBACK-WP-02_prompt_reflection.md` **READY**(#107 머지 후 main). 검색 승인 Example → `DraftRequest.Context` review 경유 read-only 반영. Core+테스트 범위.
2. **안전 cleanup 후보**(각각 자체 완결·저위험 — **WP 승격 시 Claude 프롬프트 선행** 필요; 아래는 근거 인벤토리):
   - **UX-WP-01 nit** — `CompletionEngine` dedupe 키(ProviderId+Label)에 `Kind` 미포함 → 동일 Label로 SafetyHint+일반항목 동시 방출 시 일반항목 소실. `Core/Assist/CompletionEngine.cs`.
   - **UX-WP-03 nit(C-7)** — `CompletionPopup` Esc-on-ListBox 경로에서 원본 TextBox 포커스 미복원. `App/Controls/CompletionPopup.xaml.cs`.
   - **UX-WP-02 nit(A-4/A-5/A-6)** — Blocker가 BlockedHint+SafetyHint 2줄 노출 / 정보성 finding까지 SafetyHint 핀 / `IsWorksheetFunctionName` 필터 무음 누락. `Core/Assist/Providers/StaticCompletionProviders.cs`.
   - **R2 잔여 nit** — 기록만(대개 무해). 신중히, 회귀 없는 범위만.

> ⚠️ FEEDBACK-WP-02 외 추가 WP는 **1 WP 1 프롬프트** 규약상 Claude가 프롬프트를 먼저 써야 한다. 지금 남은 한도로 프롬프트화할 후보를 사용자가 지정하면 착수(아래 §D). 미지정 시 Codex는 **FEEDBACK-WP-02**로 주간 진행하고 나머지는 일요일에 큐잉.

## C. 일요일 일괄 점검 계획 (Sunday Comprehensive Review — Claude, 리셋 후)
0. **상태 파악**(`risk-status-sync`): `git log`·`list_pull_requests`로 main HEAD·정본 Total·**열린 PR 목록**·각 PR 대상 WP·의존 관계 재확인.
1. **PR별 4축 리뷰**(생성 순) — `risk-codex-review`(①범위=지정 단일 WP만 ②보안 ③테스트 ④문서·게이트) + `risk-security-guard`(Gate A) + **도메인 Skill**(feedback=`risk-feedback-learning`·UI=`risk-ui-ux-review`·data=`risk-data-limit-review`·RAG=`risk-rag-ncr-governance`) + `risk-smoke-governance`(단언 보존·`Total` 비감소·Unclassified=0) + `risk-branch-governance`.
   - 각 PR 체크: diff가 **지정 WP만** 다루는가 → 보안(원문/실데이터/실 테이블·컬럼/모델파일/NuGet/자동실행/평문 audit **0**) → 테스트(도메인 분류 정확·기존 단언 보존·양성/음성) → 문서 정합 → **로컬 게이트 증거**(build 0/0·`Total` PASS/0 FAIL·Gate A·PackageReference 0) 실재 확인.
   - **과대표기 금지**: 실 오프라인 Test PC 증거 없는 Gate는 **PASS 금지·BLOCKED 유지**(§11.4).
   - (선택) 예산 여유 시 **Workflow(다중 리뷰어×adversarial verify)**로 각 PR 병렬 심층 리뷰.
2. **머지**: 독립 PR은 승인분부터 squash. 스택이면 base부터, retarget 후 **`rebase --onto`**(#103 사례) — force-with-lease는 **브랜치 오너(사용자)** 작업(§8, Claude force push 금지).
3. **머지마다 truth-sync**(`risk-doc-truth-sync`): 기준선 SHA·`Total`·NEXT UP·**Traceability(`docs/38 §5`)**·`docs/39` Resume Brief·`CLAUDE.md`/`AGENTS.md`/`README.md`/`SKILLS.md` 정합.
4. **마무리**: 전체 정합 재확인 → 다음 **NEXT UP 1개** 지정.

## D. 지금(리셋 전) 남은 한도 사용
- **완료**: PR #107(truth-sync FEEDBACK-WP-01 DONE + FEEDBACK-WP-02 프롬프트) + 본 문서.
- **권장 다음**: 사용자가 §B의 cleanup 후보 중 **프롬프트화할 것을 1~2개 지정**하면 지금 착수(Codex 주간 큐 확장). 미지정이면 여기서 멈추고 한도 보존 종료 — Codex는 FEEDBACK-WP-02 진행, 나머지는 일요일.

> 관련: `CLAUDE.md §11`, `AGENTS.md §0·§9`, `docs/38`·`docs/39`, `SKILLS.md`, `prompts/codex/FEEDBACK-WP-02_prompt_reflection.md`.
