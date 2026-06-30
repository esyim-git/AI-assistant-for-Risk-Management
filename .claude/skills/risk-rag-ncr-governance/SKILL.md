---
name: risk-rag-ncr-governance
description: Govern regulation RAG, Public Knowledge Pack, NCR Rule Pack, internal KB access control, citation, source-text guard, and approval gates.
allowed-tools: Read Grep Glob Bash(git diff *) Bash(dotnet run *)
paths:
  - "src/RiskManagementAI.Core/Kb/**"
  - "src/RiskManagementAI.Core/Ncr/**"
  - "kb/**"
  - "config/ncr/**"
  - "docs/08*"
  - "docs/17*"
  - "docs/41*"
---

# RAG / NCR Governance

## 목적
KB/RAG/NCR 작업이 **공개-only · 원문 미포함 · 인용 검증 · 규정답변 10단계 형식 · Keyword-only** 원칙을 지키는지 강제한다. 검토 결과로 위반(원문 포함/인용 누락/10단계 미준수/Vector 도입 시도)을 보고한다.

## 언제 사용
- **자동 적용**: `paths` 매칭 파일 작업 시 활성화 — `src/RiskManagementAI.Core/Kb/**`, `src/RiskManagementAI.Core/Ncr/**`, `kb/**`, `config/ncr/**`.
- KB catalog/메타 추가·수정, NCR Rule Set(`config/ncr/*.json`) 편집, `KbSearch`/`KbIndex`/`KbAccessPolicy`/`KbRepositoryGuard`/`NcrRuleSetLoader` 검토 시.
- 규정/NCR 검색·답변 형식, 인용 블록, 적재 게이트, 검색 엔진 선택을 점검할 때.

## 절대 원칙
- **공개 catalog/placeholder만** repo에 둔다. 내부규정 원문 · NCR 공식본 원문 · 실 `file_hash`/실 version/실 시행일/실 계수 = repo **0**. (`CLAUDE.md §10`, `docs/04`, `docs/17`)
- 적재 status는 공개만 인용: `CATALOG_ONLY`/`PUBLIC_APPROVED`/`APPROVED_PUBLIC`→`PublicCited`. `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED`→원문 비노출. (`KbAccessPolicy`)
- `KbRepositoryGuard`가 `kb/`·`data_sources/`·`samples/`·`config/ncr` 원문 의심 파일을 스캔→Blocker. **토큰 정합 유지.** (`docs/41 §2`)
- 규정/NCR 답변 = `CLAUDE.md §10` **10단계 형식** + 항상 **"검토용 초안"** 명시(공식 법규 해석 아님).
- 검색 엔진 = **Keyword / Inverted Index(in-box, NuGet 0)**. Vector DB/Embedding/Local LLM Runtime/모델파일 필요 시 **STOP** → 승인 문서 후에만(`docs/41 §2`, `CLAUDE.md §11.5`).
- NCR은 **모델 산식 암기 0** — Rule Set 8요소 구조·Validation SQL(조회 전용)·대사로만 산출·설명(`docs/08`).
- 이 스킬은 **프로세스/체크리스트 가이드**다. 코드 동작을 바꾸지 않는다(읽기 전용 검토).

## 절차
1. **공개-only 확인**: 적재/참조 문서가 공개 catalog/placeholder만인지 검토. 내부규정·NCR 공식본 **원문 0**. `KbAccessPolicy` status 매핑과 `KbRepositoryGuard` 토큰 정합을 확인(`config/ncr` 포함). 실 hash/version/시행일은 `(확인 필요)`로 노출되는지 본다.
2. **10단계 형식 + 검토용 초안**: 규정/NCR 답변 경로가 `CLAUDE.md §10` 10단계(질문요약→…→출처)를 따르고, 매 답변에 "검토용 초안" 문구를 포함하는지 검토. 상세 정본 형식은 [ten-step-format.md](ten-step-format.md).
3. **인용 블록 검증**: 검색 답변에 `문서명 · 버전 · 시행일 · 조항 · 출처 · 검색 기준일 · "검토 필요"`가 전부 있는지 확인(`docs/17`·`KbSearch`). 검색 기준일은 주입 `IClock` 실제 날짜(placeholder 금지).
4. **Keyword-only**: 검색 엔진이 Keyword/Inverted Index인지 확인. Vector/Embedding/모델 도입 흔적·요구가 보이면 **STOP** 처리하고 `/risk-llm-approval`로 승인 경로 안내.

## 산출물/보고
- 거버넌스 점검 결과 한 줄 + 위반 목록. 예:
  `RAG/NCR Governance = OK (공개-only PASS · 10단계 PASS · 인용 7항목 PASS · Keyword-only PASS)`
  또는 `RAG/NCR Governance = VIOLATION (인용 '시행일' 누락 · config/ncr 원문 의심 1건 · Vector 도입 시도 → STOP)`.
- 위반 분류: ① 원문 포함/원문 의심 ② 인용 항목 누락 ③ 10단계 미준수/검토용 초안 누락 ④ Vector/Embedding 도입 시도(STOP).
- 코드/구조 변경 제안은 하지 않고, 위반 위치와 근거 `docs` 경로만 보고한다. 게이트 PASS 판정은 실 증거 기반만(과대표기 금지, `CLAUDE.md §11.4`).

## 체크리스트
- 공개-only/원문미포함/인용/Ingest Gate/Keyword-only 점검: see [rag-ncr-checklist.md](rag-ncr-checklist.md)
- 규정/NCR 답변 10단계 정본 형식: see [ten-step-format.md](ten-step-format.md)

## 참조
- `CLAUDE.md §10` (규정/NCR 10단계 형식·공개-only·검토용 초안), `CLAUDE.md §11.4` (상태 어휘·과대표기 금지), `CLAUDE.md §11.5` (STOP 규칙)
- `docs/04_KB_IngestionPolicy.md` (문서 분류·내부규정 처리·필수 표시)
- `docs/17_KB_RAG_Design.md` (공개 규정 인용형 RAG·검색 엔진·R3-WP 분해), `docs/08_NCR_Module_Design.md` (NCR Rule Set 8요소)
- `docs/18_NCR_Regulation_Module_Guide.md` (NCR 답변 포맷·Hidden Risk), `docs/41_Approval_and_Pilot_Gates.md §2` (RAG/NCR Approval Gate)
- 연계 스킬: `/risk-llm-approval` (Vector/Embedding/Local LLM 도입 STOP→승인), `/risk-security-guard` (커밋 전 보안 게이트·원문/secret 차단)
