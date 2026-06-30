# RAG / NCR Governance 체크리스트

> 읽기 전용 점검. 각 항목 `PASS`/`VIOLATION`/`N/A`로 판정하고 위반 위치 + 근거 `docs` 경로를 보고한다. 실 증거 없는 `PASS` 금지(`CLAUDE.md §11.4`). 코드 동작은 바꾸지 않는다.

## A. 공개-only / 원문 미포함

- [ ] 적재/참조 문서가 **공개 규정 catalog · 공개 FAQ · placeholder**만인지 (`docs/04` 문서 분류). 내부규정 원문·NCR 공식본 원문 = repo **0**.
- [ ] catalog에 **원문 컬럼 없음** — 문서명·버전·시행일·조항·요약·접근권한·원문 위치 참조 등 **메타만**(`docs/04`).
- [ ] 실 `file_hash`/실 version/실 effective_date/실 계수가 repo에 비어있고 `(확인 필요)`로 노출(`docs/17`·`docs/41 §2`).
- [ ] `KbAccessPolicy` status 매핑: `CATALOG_ONLY`·`PUBLIC_APPROVED`·`APPROVED_PUBLIC`→`PublicCited`; `PROD_ONLY`·`MANUAL_APPROVAL_REQUIRED`→`MetadataOnly`/`ApprovalRequired`(원문 비노출).
- [ ] `KbRepositoryGuard`가 `kb/`·`data_sources/`·`samples/`·`config/ncr`를 스캔→원문 의심 파일명/내용 Blocker. **토큰 정합**(가드 토큰을 약화/삭제하지 않음).
- [ ] `config/ncr/*.json` 샘플 = placeholder 구조만(예: `APPROVAL_REQUIRED_NO_REAL_COEFFICIENT`), 실 산식값 하드코딩 0.

## B. 규정/NCR 10단계 형식 + 검토용 초안

- [ ] 규정/NCR 답변 경로가 `CLAUDE.md §10` **10단계**를 따른다 (정본: [ten-step-format.md](ten-step-format.md)).
- [ ] 매 답변에 **"검토용 초안 / 공식 법규 해석 아님"** 문구 포함(`KbSearch` ReviewDraftNotice · `NcrRuleSet.DraftNotice`).
- [ ] NCR 답변은 Rule Set **8요소 구조**에서만 설명 — **모델 산식 암기 0**(`docs/08`).

## C. 인용 블록 검증 (7항목 전부)

- [ ] `문서명` · [ ] `버전` · [ ] `시행일` · [ ] `조항` · [ ] `출처(locator)` · [ ] `검색 기준일` · [ ] `"검토 필요" 문구` (`docs/17`).
- [ ] 검색 기준일 = 주입 `IClock` **실제 날짜**(placeholder/하드코딩 금지). invalid date는 안전 fallback.
- [ ] 비어있는 메타는 임의 값 대신 `(확인 필요)`로 노출.

## D. Ingest / Approval Gate (`docs/41 §2`)

- [ ] 적재 문서가 공개 규정/공개 FAQ/**승인된** 내부규정만(승인 전 원문 적재 금지).
- [ ] 내부규정·NCR 원문은 Prod에서 문서오너 승인 + 보안등급 + 역할기반 권한 + **조회로그** 후에만(repo 미포함 유지).
- [ ] release 패키징(`build/03`) ZIP 추출 스캔이 동일 가드 토큰 mirror + SmokeTest drift guard로 차단(`docs/41 §2` 후속 ①).

## E. Keyword-only / STOP 규칙

- [ ] 검색 엔진 = **Keyword / Inverted Index**(in-box BCL, NuGet 0). 결과·순서·점수 결정적.
- [ ] **Vector DB · Embedding Runtime · Local LLM Runtime · 모델파일 · 외부 NuGet/라이브러리** 도입 흔적/요구 0.
- [ ] 위 항목이 필요해지면 **STOP** → 승인 문서 작성(라이선스·크기·오프라인 동작·보안·반입·무결성) 후에만 진행 → `/risk-llm-approval`.

## F. 절대 원칙 (불변식)

- [ ] 실 데이터·실 테이블/컬럼/시스템명 0 (더미 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만).
- [ ] 내부규정 원문·NCR 공식본 원문·secret/key/token·모델파일·외부 다운로드 지시 = 이 스킬/체크리스트/KB 파일에 **0**.
- [ ] 기존 SmokeTest 삭제/약화 0. 자동 실행(SQL/VBA/Golden6) 0. NoModelMode 기본 유지.
- [ ] 상태 어휘 정본만(`VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`). 과대표기 금지.

## 보고 형식

```text
RAG/NCR Governance = OK | VIOLATION
- A 공개-only:   PASS | VIOLATION(위치·근거 docs)
- B 10단계/초안: PASS | VIOLATION
- C 인용 7항목:  PASS | 누락(항목명)
- D Ingest Gate: PASS | VIOLATION
- E Keyword-only:PASS | STOP(Vector/Embedding 도입 시도 → /risk-llm-approval)
- F 불변식:      PASS | VIOLATION
```

> 참조: `CLAUDE.md §10·§11.4·§11.5`, `docs/04`, `docs/08`, `docs/17`, `docs/18`, `docs/41 §2`. 연계: `/risk-llm-approval`, `/risk-security-guard`.
