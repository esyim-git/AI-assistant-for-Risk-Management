# Codex R3-WP-04 — 적재 게이트 가드 (공개/승인만 노출 · 원문 미포함)

> 권위 스펙: `docs/17`(R3 내부규정 권한통제), `docs/41 §2`(RAG/NCR Approval Gate), `CLAUDE.md §10`. Release: R3. 선행: R3-WP-01·03.

## 목표
검색·적재가 docs/41 §2 게이트를 **코드로 강제**한다: **공개/승인 status만 인용 노출**, `PROD_ONLY`(내부규정)·`MANUAL_APPROVAL_REQUIRED`(NCR)는 **원문 비노출**(메타+표식만), **라이선스/승인상태 검증 finding**, **내부규정 원문·NCR 공식본 원문 repo 미포함** 가드.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§10`, `docs/17`(R3), `docs/41 §2`, `docs/08`(NCR), 기존 `Core/Kb/KbSearch.cs`·`RegulationCatalog.cs`(status 값: `CATALOG_ONLY`/`PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED`).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/r3-wp-04-ingest-gate origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- `KbSearch`(또는 `Core/Kb/KbAccessPolicy.cs` 신규)가 **적재/노출 정책**을 강제:
  - **공개 규정(공개/승인 status)**: 정상 인용(메타+출처).
  - **`PROD_ONLY`(내부규정)·`MANUAL_APPROVAL_REQUIRED`(NCR)**: 검색에 **메타·표식만**("원문 미적재 — Prod 권한통제 KB / 문서오너 승인 필요"), **원문/조항 본문 노출 0**.
  - **라이선스 상태·승인상태 미비** entry → **경고 finding**(검색 가능하되 "검토 필요" 강조).
- **내부규정 원문/NCR 공식본 원문이 repo에 존재하지 않음**을 검증하는 가드(테스트로 고정).
- 제외: NCR Rule Set 구조(WP-05), 실제 Prod 권한통제(사내).

## Public Interface
- `KbSearch` 결과/응답에 **노출 등급/표식**(예: `Disclosure: PublicCited | MetadataOnly | ApprovalRequired`)과 사유. 기존 `Search(...)` 시그니처 호환.
- (선택) `KbAccessPolicy.Evaluate(entry) → (bool CiteFullMeta, bool SourceTextAllowed, string Reason)`.

## 구현 세부 / 보안
- **status 화이트리스트**로 노출 결정(결정적). 알 수 없는 status → 보수적으로 **MetadataOnly + 경고**.
- 내부/NCR entry는 **원문 필드 자체가 없어야**(WP-01에서 원문 컬럼 미신설) — 정책은 "원문 없음"을 재확인하고 표식만.
- 라이선스/승인 미비 → finding(차단 아님, **검토 필요** 강조).
- "검토용 초안"·해시 전용 감사 유지. 외부 0·NuGet 0.

## 테스트(필수)
- `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED` entry 검색 → **메타·표식만, 원문/조항 본문 0**.
- 공개 규정 → 정상 인용(WP-03 형식).
- 라이선스/승인 미비 entry → **경고 finding**.
- **repo에 내부규정 원문/NCR 원문 파일 부재** 회귀(소스/샘플 스캔).
- 기존 KbSearch 회귀 유지.

## 완료/보고
게이트가 코드로 강제됨(공개만 인용·내부/NCR 원문 비노출·라이선스 검증). build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17` 진행표 갱신. (→ RAG 완료, 다음 NCR WP-05.)

## Claude Review Checklist
공개/승인만 인용·내부/NCR **원문 비노출(메타+표식)** / 라이선스·승인 검증 finding / 내부규정·NCR 원문 repo 미포함 가드 / 결정적·NuGet 0 / 기존 SmokeTest 유지 / Gate A.
