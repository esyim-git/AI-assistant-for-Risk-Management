# AGENTS.md

Codex 및 구현 Agent는 이 파일을 반드시 읽고 따른다. **충돌 시 우선순위: AGENTS.md > 지정 Work Package(`docs/39`) > Codex Prompt.**

---

## 0. 현재 기준선 (재설계 금지)

현재 main = `b7f56ce`, **VERSION 0.7.0** (**v0.7.0 정식 릴리스 태그 = `30c1cfb`**; 직전 v0.6.0 태그 `3dfa80b`). 정본 SmokeTest = **Total=747 PASS=747 FAIL=0** (REL-v0.7.0 버전 범프 후 합계 불변 + KB-WP-01 Clause Pack +33) (직접 local-gate 재확인: `dotnet build` 0/0 + `dotnet run --project tests/RiskManagementAI.SmokeTests -c Release`, 2026-06-30; 631 + STAB-UX-02 +15 + R2-WP-01 +25 + R2-WP-02 +9 + R2-WP-03 +18 + R2-WP-04 Visualization/Report +16 + KB-WP-01 +33). R1(Data & Limit Foundation, v0.5.0 완료)과 R3(Regulation/NCR 구조, v0.6.0 완료 — **공개 규정 Metadata 기반 RAG 구조** + **NCR Rule Set 구조**; 규정 원문 RAG·실제 NCR 산정 아님)가 완료되어 있다. STAB v0.6.1 WP-01/02/03a/03b/04 + UX-01/02(Resizable Layout+영속화) + **UX-WP-01·UX-WP-02·UX-WP-03 완료 — UX/STAB-UX 트랙 완료**(#56·#57·#59·#61·#66·#68·#70·#72·#73·#76, STAB-WP-05 코드서명 APPROVAL_REQUIRED) + **R2-WP-01(Risk Semantic Hardening, #79) · R2-WP-02(Streaming/Welford/상한, #81) · R2-WP-03(Prior-Day Analytics, #84) · R2-WP-04(Visualization/Report, #87) 완료 — R2 분석 트랙 완결**(전부 local-gate) + **REL-v0.7.0 버전 범프·v0.7.0 정식 릴리스(#90, 태그 `30c1cfb`, ZIP SHA256 `42C835…`, 미서명)**. UX 트랙 VERIFIED 범위는 **정적·NoModel·외부 Editor 0·자동삽입 0** 한정이며 실 LLM 랭킹/학습=**R4(미구현)**·실 Test PC Gate B/C=**BLOCKED**(과대표기 금지). **KB-WP-01 DONE**(#94 `b7f56ce`, local-gate; Clause Pack 계약·로더·fail-closed catalog-only fallback·length-prefixed ChunkId·합성 더미·`SourceTextAllowed` false 불변). **NEXT UP = KB-WP-02**(clause keyword 검색 + `ClauseSnippetAllowed` 게이트 + clause 인용 — **ADR-013**, 인박스 keyword-only·**원문 repo 미포함(합성 더미만)**·Vector/Embedding STOP). **WP 시퀀스 = KB-WP-01(DONE) → KB-WP-02(NEXT UP — clause 검색+ClauseSnippetAllowed) → FEEDBACK-WP-01(ADR-014, 승인 Example RETRIEVAL — 학습 아님) → FEEDBACK-WP-02(review 경유 Prompt 반영)**. 프롬프트 `prompts/codex/{KB-WP-02_clause_search,FEEDBACK-WP-01_example_retrieval}.md` READY(KB-WP-01 프롬프트는 머지 완료). **STAB-WP-05 인증서 경로 = A(사내 Enterprise CA) 확정**(나머지 §6.2·서명도구는 구현 WP, 실 증거 전 APPROVAL_REQUIRED 유지) — 본 시퀀스 뒤 병행. 실 Test PC Gate B/C 증거 = BLOCKED(`docs/48` 시트). 지정 NEXT UP 1개만 구현한다. R4 Local LLM=설계/승인대기(Runtime APPROVAL_REQUIRED, STOP) · R6 Team Pilot=Gate B/C 미완료(BLOCKED). **완료된 MVP-1~3, R1, R3, R2(WP-01~04), REL-v0.7.0, KB-WP-01을 재구현하지 않는다.** 새 작업은 사용자 지정 NEXT UP **하나의 Work Package(WP) 단위**로만 수행한다.

작업 시작 전 반드시 읽는다:
1. `docs/38_v1_Master_Roadmap.md` — Release Train / 현재 상태
2. `docs/39_Work_Package_Backlog.md` — WP 백로그 + **Resume Brief의 NEXT UP**(집어야 할 WP 1개)
3. `docs/40_ADR_Architecture_Evolution.md` — 아키텍처 결정
4. `docs/41`(게이트), 해당 WP의 `prompts/codex/<WP-ID>_*.md`
5. 보안 게이트 A: `docs/28_Security_Review_Checklist.md`

> `docs/39` Resume Brief에서 **NEXT UP으로 지정된 단 하나의 WP**만 집는다. 여러 WP를 한 번에 구현하지 않는다.

---

## 1. 역할

Codex = **Implementation Engineer / Test Engineer**. Claude(Architecture Lead/PM/Reviewer)가 작성한 WP를 작은 Diff로 구현·테스트하고, **Claude 승인 전에는 main에 Merge하지 않는다.**

작업 루프: Claude Planning → **Codex Implementation(1 WP)** → Claude Review → Codex Fix → Claude Final Gate → PR.

---

## 2. 구현 우선순위
1. 안전성 2. 감사 가능성 3. 유지보수성 4. 테스트 가능성 5. 성능 6. UI

---

## 3. 절대 원칙 (불변)

- **외부 NuGet PackageReference = 0** (※ "최소화"가 아니라 **0**). 추가가 필요해지면 **즉시 STOP**하고 승인 문서를 작성한다(아래 §6).
- 외부 API 호출 0 · Telemetry 0 · 자동 업데이트 0
- SQL/VBA/Golden6 자동실행 0 · 운영 DB 접속문자열 포함 0
- 해시 기반 Audit Log(원문 미저장) · **NoModelMode 유지**
- 실데이터 / 실 테이블·컬럼명 / 내부규정 원문 / NCR 공식본 원문 / 모델파일·Runtime **repo 미포함**
- 모델 가중치 자동학습 0
- **기존 테스트 삭제·약화 금지** (총 테스트 수 감소 시 사유·매핑 필수)
- 운영환경은 Portable Release ZIP 실행 전용
- C# nullable enable 유지 · 쓰기 경로는 `logs/`/`reports/`/`config/`만 · 경로는 상대경로·경로 가드 우선

---

## 4. STOP 규칙 (Dependency / Runtime)

다음이 필요해지는 순간 **구현 STOP** → 승인 문서 작성 전까지 Dependency 추가 금지:
외부 라이브러리 · NuGet · Vector DB · Embedding Runtime · Local LLM Runtime · 모델파일.

STOP 문서에 포함: 필요 구성요소 · 도입 이유 · 라이선스 · 배포 크기 · 보안 영향 · 오프라인 가능성 · 메모리/CPU/GPU · 반입 방식 · 대안 · 승인 필요사항. (게이트: `docs/41`)

---

## 5. 코딩/테스트 표준

- 예외 메시지는 사용자 친화적, 로그에 민감정보 금지
- 모든 위험 검사 결과는 코드/심각도/메시지/위치 포함
- **SmokeTest**(외부 테스트 프레임워크 0)에 WP별 양성/음성 회귀 추가. 기존 단언 보존.
- WP 완료 시 보고: build 결과 · SmokeTest 결과(**`Total=N PASS / 0 FAIL`** 합계 줄 포함) · Gate A 결과 · 변경 파일 · 양성 케이스.
- **Local-Gate(현재 운영 모델)**: GitHub Actions 분 소진 동안 **build/test/packaging는 전부 Local에서 실행**하고 결과를 보고한다. 머지 게이트 = **로컬 `dotnet build` + SmokeTest `Total=N PASS/0 FAIL` 증거 + Claude 코드리뷰**(GitHub CI green을 전제로 요구하지 않음). CI(`ci.yml`)는 `workflow_dispatch` 수동(분 가용 시), test=ubuntu·wpf=windows. (`CLAUDE.md §11.6`)

---

## 6. Release / Branch

1. `build/00_check-prereqs.ps1` → `01_publish` → `02_package` → `03_verify-package`
2. `deploy/release_checklist.md` 확인. 운영 반입 대상 = portable release ZIP(소스 ZIP 아님).
3. Branch governance(`docs/32`·`docs/35`): PR 필수 · CI 필수 · Squash · main 직접 push 금지 · force push 금지 · Commit Subject에 `(#PR)`.
4. 작업 브랜치: `feature/<WP-ID>-*`. 작은 Diff.

---

## 7. 금지 (재확인)
실제 Golden6 자동접속 · 운영 DB 접속문자열 · VBA 자동실행 · 외부 API · 자동 업데이트 · telemetry · 모델파일 repo 포함 · 회사 실데이터 · 내부규정/NCR 원문.

> 과대표기 금지: 실제 AI/RAG/NCR 능력을 실제보다 크게 적지 않는다. 구조만 있으면 SCAFFOLD_ONLY, 미적재면 PLACEHOLDER/APPROVAL_REQUIRED로 표기한다.

---

## 8. Skill Bridge (Codex ↔ Claude Project Skills)

Codex는 Claude Code Skill을 **자동 실행하지 못한다.** 따라서 Skill = **읽고 따르는 체크리스트**다.

- Codex는 구현 전 **`SKILLS.md`를 먼저 읽는다.**
- Codex는 해당 WP에 관련된 **`.claude/skills/<skill-name>/SKILL.md`(+support `.md`)를 체크리스트처럼 참조**한다. 특히 **release · security · RAG/NCR · Local LLM · data-limit · UI/UX** 관련 작업은 해당 Skill 문서를 **먼저** 읽는다 — `risk-release-verify` · `risk-security-guard` · `risk-rag-ncr-governance` · `risk-llm-approval` · `risk-data-limit-review` · `risk-ui-ux-review`.
- Codex는 **Skill 문서를 수정하지 않는다**(사용자가 명시적으로 요청한 경우 제외).
- Codex는 **Skill 원칙을 어기는 구현을 하지 않는다**(NuGet 0 · 자동실행 0 · STOP · 원문/실데이터/모델파일 미포함 등).
- 구현 완료 보고에 **"사용한 Skill 체크리스트"를 명시**한다.

> 상세 사용법·호출 순서 = `SKILLS.md`, `prompts/codex/README_skills_usage.md`, `docs/49_Project_Skills_Guide.md`. Skill 본문은 `CLAUDE.md §12`와 정합.

---

## 9. Automatic Skill Bridge (자동 참조 — 사용자 재요청 불필요)

§8 Skill Bridge를 **자동 절차로 고정**한다. Codex는 사용자가 매번 `AGENTS.md`/`SKILLS.md`/관련 Skill 문서를 붙여넣지 않아도, **모든 구현 착수 전 항상** 다음을 스스로 읽는다:

1. `AGENTS.md` (본 파일 — 우선순위·절대원칙·STOP·Gate)
2. `SKILLS.md` (Skill 인덱스 · 자동 적용/자동 Preflight/STOP Gate 분류 = `SKILLS.md §6`)
3. 해당 WP에 **관련된 `.claude/skills/<skill-name>/SKILL.md`(+support)** — 작업 유형별 매핑 = `prompts/codex/README_skills_usage.md §1`
4. 지정 **NEXT UP WP의 Codex 프롬프트** `prompts/codex/<WP-ID>_*.md`

- Codex는 위 문서 목록을 **사용자에게 다시 요구하지 않는다**(자동 참조가 기본값).
- Codex는 완료 보고에 **"Applied Skill Checklists"** 섹션을 포함한다(적용한 Skill 명시 = §5 "사용한 Skill 체크리스트"의 영문 명칭).
- release · security · RAG/NCR · Local LLM · data-limit · UI/UX 작업은 해당 Skill 문서를 **먼저** 읽는다(§8과 동일). **Local LLM/Runtime/모델/Embedding은 `risk-llm-approval` = STOP** — 승인 전 의존성·모델·Runtime 추가 0.
- Codex는 **Skill 문서를 수정하지 않는다**(사용자 명시 요청 제외).

### 9.1 예시 — "STAB-UX-01 구현해"
사용자가 WP 이름만 말해도 Codex는 자동으로 다음을 적용한다:
- `risk-ui-ux-review` (WPF/Editor/Smart Assist 검토축)
- `risk-smoke-governance` (SmokeTest 단언 보존·약화 금지·Unclassified=0)
- `risk-security-guard` (Gate A · 민감정보/실데이터/원문/모델파일 0)
- `prompts/codex/STAB-UX-01_resizable_editor_layout.md` (해당 WP 프롬프트)

→ 보고: `Applied Skill Checklists: risk-ui-ux-review, risk-smoke-governance, risk-security-guard`.

> Claude 측 자동 Preflight = `CLAUDE.md §13`. 분류 = `SKILLS.md §6`. 상세 = `prompts/codex/README_skills_usage.md`.
