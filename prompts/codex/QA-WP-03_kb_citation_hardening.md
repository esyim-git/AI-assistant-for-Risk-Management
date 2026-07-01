# Codex Prompt — QA-WP-03: Kb/Citation SmokeTest 하드닝 (공개 규정 검색·인용·원문 가드)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-03) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-03-kb-citation-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-03, `SKILLS.md`+`risk-rag-ncr-governance`·`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Kb/*`(검색·인용·`KbAccessPolicy`·`KbRepositoryGuard`·`ClauseSnippetAllowed`·`SourceTextAllowed`·`KbKeying`), `kb/*`(공개 catalog 구조), `tests/RiskManagementAI.SmokeTests/KbTests.cs`.
> **기준선**: main `d8cb415`(VERSION 0.7.0, 확장 트랙 Wave 1 머지 후), 정본 SmokeTest `Total=861 PASS=861 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
공개 규정 KB의 **검색·인용·게이트·원문 가드**의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드·정책·가드 로직 변경 0** — 테스트만. `SourceTextAllowed=false` 불변·원문 미노출·placeholder 메타 `(확인 필요)` 등 기존 불변식을 회귀로 잠근다.

## 1. 작업 범위 (KbTests.cs — additive only)
1. `Core/Kb/*` 검색/인용/게이트 로직과 현 `KbTests`를 대조 → **미커버 경계만** 추가(중복 금지).
2. 후보(제품 동작 확인·신규 동작 요구 아님):
   - 검색 결정성(동일 질의→동일 결과·정렬 tie-break)·keyword/inverted index 경계(부분일치·대소문자·빈 질의).
   - 인용 필드 완비(문서명·버전·시행일·조항·출처·검색기준일·검토필요) + placeholder 메타 `(확인 필요)` 노출.
   - `asOf` 유효구간 경계(시행일<=asOf<폐지일·경계값·미파싱 graceful, KB-WP-02 계약).
   - **게이트**: 비공개 status 인용 차단·`ClauseSnippetAllowed`==`Evaluate` 단일 게이트·`SourceTextAllowed=false` 시 원문 미노출.
   - **원문 가드**: `KbRepositoryGuard`가 원문 의심 입력/파일을 Blocker로 표면화(합성 더미로).
3. **합성 더미만** — 실 내부규정/NCR 원문 0, 실 catalog 원문 0(placeholder 메타·합성 clause pack만).

## 2. 제외 범위
Kb 검색/정책/가드/인덱스 제품 코드 변경. 신규 catalog·원문 적재. Vector/Embedding. NCR 산정. 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
**실 규정 원문/내부규정/NCR 공식본 0**(합성 더미만) · `SourceTextAllowed=false`·원문 미노출 회귀 · 원문 의심 입력 Blocker 단언 · NuGet 0 · **기존 테스트 삭제·약화 0**.

## 4. 테스트 (SmokeTest — 도메인 `Kb`)
> ⚠️ **이 WP는 도메인 `Kb`가 목표** — 다른 WP와 **반대로**, 신규 단언 설명에 `SmokeTestContext.SmokeDomain`의 **Kb 키워드**(`KbIndex`/`KbSearch`/`Regulation`/`catalog`/`citation`/`document`/`source`/`license`/`검색`/`원문`/`공개`/`인용`)를 **의도적으로 포함**해 Kb로 분류시킨다. 단, 더 위 도메인(Reconciliation `RECON`/`duplicate limit`·Report `report `·Limit `limit`/`한도`·Ncr `NCR Rule`/`Rule Set`) 트리거는 회피(Ncr는 Kb 바로 위이므로 `NCR Rule`/`Rule Set` 문구 금지). `Unclassified=0`.
- 각 경계 → 기대 결과(검색 hit/미스·인용 필드·게이트 차단·원문 미노출·asOf 포함/제외) 단언.
- 기존 `KbTests` 단언 **전부 보존**. 종료부 **`Total=861 → 861+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Kb 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-03-kb-citation-hardening` · Commit: `test: harden kb search/citation/source-guard coverage (QA-WP-03)`

## 6. Claude Review Checklist
제품 코드/정책/가드 변경 0(테스트만) / 추가는 실제 미커버 경계 / `SourceTextAllowed=false`·원문 미노출·게이트 회귀 정확 / 합성 더미(실 원문·내부규정·NCR 0) / 도메인 Kb·Unclassified 0(Ncr `Rule Set` 문구 회피) / 기존 KbTests 보존·감소 0 / NuGet 0 / `Total` 861 보존+신규 / Gate A.
