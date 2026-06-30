# Codex Prompt — KB-WP-01: Clause/Chunk Pack 계약 + 로더 + 원문 가드 강화 (인박스, 원문 repo 미포함)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(KB-WP-01) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/kb-wp-01-clause-pack`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3·§4`, `docs/39` KB-WP-01, `docs/40` **ADR-013**·ADR-007, `docs/41 §2`, `docs/17`(KB RAG), `src/RiskManagementAI.Core/Kb/*`, `tests/RiskManagementAI.SmokeTests/KbTests.cs`.
> **기준선**: main `30c1cfb`(VERSION 0.7.0), 정본 SmokeTest `Total=714 PASS=714 FAIL=0`.

## 0. 목표 (단일)
공개 규정 **원문 Clause/Chunk 검색**(KB 트랙 v0.8)의 **데이터 계약 + Pack 로더 + 원문 유입 가드**를 만든다. **검색(SearchClauses)은 KB-WP-02**, 본 WP는 **계약·로더·가드·합성 샘플**까지. **실 규정 원문은 repo에 넣지 않는다** — 합성(dummy) clause 샘플만.

> **STOP**: 외부 NuGet·Vector DB·Embedding·LLM Runtime·모델파일 추가 0. 필요해지면 즉시 STOP → 승인 문서. 검색은 인박스 keyword만(KB-WP-02).

## 1. 작업 범위
1. **신규 record `RegulationClause`**(`Core/Kb/`): `ChunkId·SourceId·ClauseRef·ClauseText·EffectiveDate·RepealDate·PackVersion·SourceTextHash`. nullable enable.
2. **`ChunkId` 결정성 + 충돌 방지**: `ChunkId = LogHash.Sha256Hex(SourceId|ClauseRef|PackVersion|SourceTextHash)[..12]` 접두("clause-..."). `SourceTextHash = LogHash.Sha256Hex(ClauseText)`. **동일 `(SourceId,ClauseRef,PackVersion)` + 상이 `ClauseText` 로드 시 거부 + Warning**(silent overwrite 금지, 결정적).
3. **신규 `ClausePackLoader`**(`Core/Kb/`): clause-pack CSV 로드. `NcrRuleSetLoader`/`RegulationCatalog`의 **path allowlist + safe-fallback** 패턴 재사용 — **Pack 미적재 시 빈 결과 + catalog-only fallback**(예외 0, 기존 검색 동작 불변). CSV 파싱은 **기존 `CsvReader` 재사용**(인박스, CP949/UTF-8, cap). 헤더 검증(누락=warning, throw 아님).
4. **합성 더미 clause 샘플**: `kb/clause_pack_sample/` 하위에 CSV 1개. **헤더 = 영문 토큰 비충돌**(`clause_ref,clause_body,source_id,effective_date,repeal_date,pack_version`). **본문 = 토큰 비충돌**(예: `제0조 (합성 테스트) 본 더미 조문은 검증용 가짜 문구`) — `원문`/`조항 원문`/`internal_*`/`official text` 등 Suspicious 토큰을 **부분문자열로도 포함 금지**. 실 규정 원문 0.
5. **`KbRepositoryGuard` 원문 가드 강화**(4자 정합 — ADR-013 §결정6 표):
   - `SuspiciousNameTokens`에 `clause_original` add-only, `SuspiciousContentTokens`에 한글 `조항 원문` add-only.
   - **한글 토큰은 `build/03_verify-package.ps1`에 `New-StringFromCodeUnits @(0x....)` UTF-16 code-unit 리터럴로 추가**(기존 패턴 동형) + **`KbTests`의 code-unit 미러 단언(현 L147-149 패턴) 통과**.
   - 합성 샘플 디렉토리/파일을 **`MetadataAllowlist`에 파일-단위 add-only 등재** + build/03 `$SourceTextAllowlist` 미러 동기.
   - **합성 샘플은 `kb/` 하위 → 기존 `ScanDirectories`(kb)로 커버, ScanDirectories 변경 0이 디폴트.** (만약 `config/kb` 적재가 필요하면 `ScanDirectories`+`build/03`+`KbTests:169` 하드코딩 리스트까지 **4번째 미러** 동기 — 본 WP는 디폴트(kb/ 샘플)로 ScanDir 변경 없음 채택.)
6. (게이트 분리 준비만) `KbAccessPolicy.SourceTextAllowed`는 **false 불변**(변경 0). clause 발췌 게이트 `ClauseSnippetAllowed`는 **KB-WP-02**에서 추가 — 본 WP에서 손대지 않음.

## 2. 제외 범위
clause **검색**(SearchClauses)·`KbAccessDecision` 확장·`ClauseSnippetAllowed`(KB-WP-02). 실 규정 원문 적재(Prod). KbIndex 일반화(KB-WP-02). UI. 신규 NuGet/Vector/모델.

## 3. 보안조건
외부 NuGet 0 · 실 규정 원문 repo 0(합성만) · 해시 audit(원문 미저장) · 쓰기 경로 없음(로더는 읽기) · `KbRepositoryGuard.Scan(현 repo) Blocker=0`이 **합성 샘플·신규 토큰 추가 후에도 보존**(신규 토큰이 자기 합성 샘플을 잡으면 안 됨).

## 4. 테스트 (SmokeTest, 외부 프레임워크 0 — 도메인 `Kb`)
- 양성: 합성 clause CSV 로드 → `RegulationClause` 결정적(ChunkId 안정·`DeterministicSignature` 동등), Pack 미존재 → 빈 결과+fallback(예외 0).
- 음성: 동일 `(SourceId,ClauseRef,PackVersion)`+상이 텍스트 → 거부+Warning · path traversal/rooted 거부 · 비-CSV 거부.
- 가드: `KbRepositoryGuard.Scan(repoRoot) Blocker=0` 보존(현 L126-127 회귀) · 신규 토큰 code-unit 미러 단언 통과 · 합성 샘플이 신규/기존 토큰에 **부분일치 0** · MetadataAllowlist 등재 확인.
- 기존 `KbTests` 단언(linear==index·`DeterministicSignature`·한글 부분일치·catalog 경로) **전부 보존**.
- 종료부 **`Total=714 → 714+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄 · Gate A · 변경 파일 · 양성/음성 케이스.
- Branch `feature/kb-wp-01-clause-pack` · Commit: `feat: clause/chunk pack contract + loader + repo guard (KB-WP-01)`

## 6. Claude Review Checklist
ChunkId 충돌방지(SourceTextHash 키 포함) / Pack 미적재 graceful fallback / CsvReader·로더 패턴 재사용(중복 0) / 합성 더미 토큰 비충돌 / 가드 4자 정합(kb/ 디폴트·ScanDir 변경 0) / 한글 토큰 build03 code-unit + KbTests 미러 / `SourceTextAllowed` false 불변 / `KbRepositoryGuard.Scan Blocker=0` 보존 / NuGet 0 / 기존 KbTests 보존 / `Total` 보존+신규 / Gate A.
