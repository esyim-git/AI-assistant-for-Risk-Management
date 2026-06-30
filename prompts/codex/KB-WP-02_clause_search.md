# Codex Prompt — KB-WP-02: Clause keyword 검색 + clause 인용 + ClauseSnippetAllowed 게이트 (인박스, 원문 repo 미포함)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(KB-WP-02) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/kb-wp-02-clause-search`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3·§4·§8`, `SKILLS.md`, `.claude/skills/risk-rag-ncr-governance/SKILL.md`(+support)·`.claude/skills/risk-security-guard/SKILL.md`·`.claude/skills/risk-smoke-governance/SKILL.md`, `docs/39` KB-WP-02, `docs/40` **ADR-013**·ADR-007, `docs/41 §2`, `docs/17`(KB RAG), `src/RiskManagementAI.Core/Kb/{KbIndex,KbSearch,KbAccessPolicy,RegulationClause,ClausePackLoadResult,ClausePackLoader,RegulationCatalog}.cs`, `tests/RiskManagementAI.SmokeTests/KbTests.cs`.
> **기준선**: main `b7f56ce`(VERSION 0.7.0), 정본 SmokeTest `Total=747 PASS=747 FAIL=0`. **선행 = KB-WP-01(#94) 머지 완료**(`RegulationClause`·`ClausePackLoadResult`·`ClausePackLoader` 존재, fail-closed catalog-only fallback).

## 0. 목표 (단일)
KB-WP-01이 깐 **Clause Pack 계약/로더** 위에서 **clause 본문 keyword 검색 + clause-level 인용 + 발췌(snippet) 노출 게이트**를 추가한다. 검색 결과는 caller가 **추가 catalog lookup·본문 재파싱 없이** 문서명·버전·시행일·**조항(ClauseRef)**·출처·검색기준일·검토필요 문구를 표시/검증할 수 있어야 한다. **실 규정 원문은 repo에 넣지 않는다** — 검색 대상은 KB-WP-01 합성(dummy) Pack(운영 실 Pack = ADR-007 ② Offline Ingestion).

> **STOP**: 외부 NuGet·Vector DB·Embedding·LLM Runtime·모델파일 추가 0. 검색은 **인박스 keyword-only**(부분문자열/토큰). 의미검색/임베딩이 필요해지면 즉시 STOP → 승인 문서(`docs/41`·`docs/40`).

## 1. 작업 범위
1. **KbIndex 코어 키잉(keying) 단일원천 추출**: 현 `KbIndex`의 `MaxSubstringKeyLength`·`TextKeys`/`SplitTerms`/`BoundedSubstrings`(L≤32 substring cap 포함)를 **`internal static` 유틸**(예: `KbKeying` 또는 `KbIndex` 내부 static)로 **추출해 catalog index와 clause index가 동일 키잉을 공유**한다. **별도 검색 엔진/중복 알고리즘 금지**(후보발견 단일원천, 점수만 타입별 분리). **기존 `KbIndex.Build`/`FindCandidates(string)`/`DeterministicSignature()` 시그니처·동작 불변**(linear-contains fallback 포함) — 기존 `KbTests`(linear==index·`DeterministicSignature`·한글 부분일치·catalog 경로) 전부 PASS.
2. **Clause 인덱스 + `KbSearch.SearchClauses(query, userId, maxResults, asOfDate)`**: KB-WP-01 `ClausePackLoader`로 적재한 `IReadOnlyList<RegulationClause>` 위에 ① 공유 키잉으로 후보 발견(검색 필드 = `ClauseText`·`ClauseRef`), ② clause-type 점수, ③ **`OrderByDescending(Score).ThenBy(ClauseId, StringComparer.Ordinal)`** 안정 정렬, ④ `Take(Math.Max(1,maxResults))`. `ClausePackLoadResult.UsedFallback=true`(Pack 미적재/거부) → **검색 결과 0건 + warning + catalog-only 안내**(예외 0, fail-closed). 생성자/조립은 기존 `KbSearch`(catalog) 경로를 깨지 않게 **additive**(별도 ctor 인자 또는 옵션 Pack 주입).
3. **신규 `KbClauseSearchResult`(record)** — caller가 추가 lookup 없이 인용/검증 가능하도록 **clause 식별 + 인용 metadata + disclosure**를 모두 포함:
   - clause: `ClauseId`(=`ChunkId`)·`SourceId`·`ClauseRef`·`Snippet`·`Score`.
   - 인용(연결 catalog entry에서): `DocumentName`(Title)·`Version`·`EffectiveDate`·`RepealDate`·`SourceLocator`(Source)·`Category`·`SourceOrg`.
   - 게이트/문구: `Disclosure`(`KbDisclosure`)·`DisclosureReason`·`SnippetAllowed`(bool)·`SearchDate`·`ReviewDraftNotice`(="검토용 초안").
   - clause↔catalog 조인은 **`SourceId`로 결정적**. **catalog 매칭 실패 시 fail-closed**: `Disclosure=MetadataOnly`/`SnippetAllowed=false`/`Snippet=""` + warning(임의 노출 금지).
4. **신규 `ClauseSnippetAllowed` 게이트(`KbAccessPolicy`)** — `SourceTextAllowed`와 **분리된 별도 게이트**:
   - `public static bool ClauseSnippetAllowed(RegulationCatalogEntry entry)`: **`Disclosure==PublicCited` AND 비-placeholder 인용/승인 metadata**일 때만 `true`. 필수 확인값은 `version`·`effective_date`·`approval_status`·`license_status`이며, 각 값이 blank/`CONFIRM_*`/`NOT_LOADED`이면 fail-closed. `PROD_ONLY`·`MANUAL_APPROVAL_REQUIRED`·`MetadataOnly`·unknown·placeholder → `false`.
   - `KbAccessDecision`에 **`bool ClauseSnippetAllowed = false` 를 기본값 있는 positional 파라미터로 add-only**(기존 4-인자 생성 호출부 불변). 기존 `Evaluate(...)`는 이 값을 채워 반환.
   - **`SourceTextAllowed`는 false 불변**(절대 변경 0). ClauseSnippetAllowed=false면 **항상 `Snippet=""`**로 발췌 0. 거부 사유는 `DisclosureReason`/warning에만 둔다(비어 있지 않은 안내문을 `Snippet`에 넣지 않는다).
5. **Snippet 결정성**: 발췌는 `ClauseText`에서 **결정적 window**(첫 매칭 term 주변 고정 길이 또는 선두 N자, `MaxSubstringKeyLength` 동형 상한)로 산출 — 비결정 0, 줄바꿈/제어문자 정규화. 게이트 false면 본문 미접근.
6. **Clause 유효구간(asOf 경계)**: `EffectiveDate`/`RepealDate`를 KB-WP-01과 동일 결정적 파싱(`yyyy-MM-dd`, `CultureInfo.InvariantCulture`)으로 처리. **blank `RepealDate`는 무기한 active**로 해석한다. non-empty invalid `EffectiveDate`/`RepealDate`만 파싱 실패 = warning + 보수적 처리(노출 축소). `asOfDate` 밖(시행 전/폐기 후) clause는 **결과에서 제외 또는 `(기준일 외)` 표식**(결정적·테스트 고정).
7. **검색 행위 해시 audit 재사용**: clause 검색도 기존 `TaskLogWriter`/`TaskLogEntry` 스키마(UserId/RequestHash/OutputHash 모두 SHA-256 hex)로 audit, **원문/쿼리 평문 미저장**. `auditRuleVersion` 없으면 기존 패턴대로 미기록+warning.

## 2. 제외 범위
`SourceTextAllowed` 변경(false 불변). **실 규정 원문 적재**(Prod·Offline Ingestion). clause **편집/쓰기**. UI 위젯(검색 결과 표시 화면). FEEDBACK 트랙. 신규 NuGet/Vector/Embedding/모델. `KbRepositoryGuard` 토큰 추가(KB-WP-01에서 완료, 본 WP 변경 0이 디폴트).

## 3. 보안조건 (risk-rag-ncr-governance · risk-security-guard)
- 외부 NuGet 0 · 외부 API/Telemetry 0 · 쓰기 경로 = audit(`logs/`)만, clause Pack은 **읽기 전용**.
- **발췌(snippet) 노출은 `ClauseSnippetAllowed`만 게이트**(공개·비-placeholder version/effective_date/approval_status/license_status만). `PROD_ONLY`/`MANUAL`/placeholder-metadata/unknown/catalog-미매칭 → **발췌 0, `Snippet=""`**.
- 해시 전용 audit(원문·쿼리·userId 평문 미저장). `ReviewDraftNotice="검토용 초안"`·공식해석 아님 문구 유지.
- 실데이터/실 테이블·컬럼명/내부규정 원문/NCR 공식본 원문 **repo 미포함**(검색 대상 = 합성 Pack). 신규 단언·샘플도 합성 더미만.
- `SourceTextAllowed` false 불변 회귀로 고정.

## 4. 테스트 (SmokeTest, 외부 프레임워크 0 — 도메인 `Kb`)
> 단언 설명에 한글 `검색`/`원문`/`공개`/`인용` 토큰만 쓰면 도메인 오분류 위험 없음(이미 `Kb`). `Unclassified=0` 유지.
- **양성**: 합성 clause Pack hit → `KbClauseSearchResult` 인용 metadata 완비(문서명·버전·시행일·조항·출처·검색일·검토필요)를 **추가 lookup/본문 파싱 없이** 검증 · 점수 내림차순+`ClauseId` Ordinal tie-break 결정성 · snippet 결정적 window.
- **게이트 음성**: `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED`/placeholder-metadata(`version`·`effective_date`·`approval_status`·`license_status`의 blank/`CONFIRM_*`/`NOT_LOADED`)/catalog-미매칭 → `SnippetAllowed=false`·`Snippet=""`(발췌 0). `SourceTextAllowed` false 불변 회귀.
- **유효구간**: `asOfDate` 시행 전/폐기 후 clause 제외(또는 표식) 경계 결정성. blank `RepealDate`는 active indefinitely, non-empty invalid repeal date만 warning/보수 처리.
- **fail-closed**: `ClausePackLoadResult.UsedFallback=true`(Pack 미적재) → `SearchClauses` 0건 + warning(예외 0).
- **코어 유틸 추출 회귀**: 기존 `KbTests`의 catalog 경로(linear==index·`DeterministicSignature`·한글 부분일치) **전부 보존**(키잉 단일원천화 후에도 동일 결과).
- 종료부 **`Total=747 → 747+N PASS / 0 FAIL`**, `Unclassified=0`, build 0/0.

## 5. 보고 / Branch
- build 0/0 · SmokeTest **`Total=N PASS / 0 FAIL`** 합계 줄 · Gate A(추적파일 의도·secret/주민번호/금지확장자 0) · 변경 파일 · 양성/음성 케이스 · **"Applied Skill Checklists"**(`risk-rag-ncr-governance`·`risk-security-guard`·`risk-smoke-governance`).
- Branch `feature/kb-wp-02-clause-search` · Commit: `feat: clause keyword search + citation + snippet gate (KB-WP-02)`

## 6. Claude Review Checklist
코어 키잉 유틸 단일원천(중복 0)·기존 `KbIndex`/catalog 경로 후방호환·`DeterministicSignature` 불변 / `ClauseSnippetAllowed` **단일 게이트**·`KbAccessDecision` 필드 add-only(기본값, 4-인자 호출부 불변) / `SourceTextAllowed` false 불변 / version·effective_date·approval_status·license_status placeholder/blank·PROD_ONLY·catalog-미매칭 발췌 차단(`Snippet=""`, fail-closed) / blank `RepealDate` active indefinitely·non-empty invalid date 보수 처리 / 인용 metadata 완비(추가 lookup 0) / snippet·정렬·유효구간 결정성 / `UsedFallback` 시 0건 graceful / 해시 audit(원문 미저장) / NuGet 0 / 기존 `KbTests` 보존 / `Total` 보존+신규 / Gate A.
