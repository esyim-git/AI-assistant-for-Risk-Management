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
  - **공개 규정 — `CATALOG_ONLY`(현 catalog 공개 규정 status) 및 승인된 공개 status → `PublicCited`**: 정상 인용(메타+출처).
  - **`PROD_ONLY`(내부규정)·`MANUAL_APPROVAL_REQUIRED`(NCR)**: 검색에 **메타·표식만**("원문 미적재 — Prod 권한통제 KB / 문서오너 승인 필요"), **원문/조항 본문 노출 0**.
  - **라이선스 상태·승인상태 미비** entry → **구조화 `SafetyFinding`**(`KB_LICENSE_MISSING`/`KB_APPROVAL_MISSING` 등 code+severity+message; 비구조 `Warnings` 문자열 금지). 검색 가능하되 "검토 필요" 강조.
- **repo/KB-level 스캔 가드**: 내부규정 원문/NCR 공식본 원문이 **어디에도 없음** 검증 — `kb/`·`data_sources/`(존재 시)·samples 포함. **allowlist**(placeholder·메타전용): `kb/README.md`·`kb/public_regulation_catalog.csv`·`kb/ncr_placeholder.md`. allowlist 외 원문 의심 파일 0(테스트로 고정).
- 제외: NCR Rule Set 구조(WP-05), 실제 Prod 권한통제(사내).

## Public Interface
- `KbSearch` 결과/응답에 **노출 등급/표식**(예: `Disclosure: PublicCited | MetadataOnly | ApprovalRequired`)과 사유. 기존 `Search(...)` 시그니처 호환.
- (선택) `KbAccessPolicy.Evaluate(entry) → (bool CiteFullMeta, bool SourceTextAllowed, string Reason)`.
- **게이트 경고는 구조화 `SafetyFinding`**(code/severity/message) 목록으로 응답에 포함 — 비구조 `Warnings` 문자열 금지(audit 필터 가능해야 함).

## 구현 세부 / 보안
- **status 화이트리스트**(결정적): `CATALOG_ONLY`(+승인된 공개 status) → **PublicCited**; `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED` → **MetadataOnly(원문 0)**; **미지 status → 보수적 MetadataOnly + `KB_UNKNOWN_STATUS` `SafetyFinding`**.
- 내부/NCR entry는 **원문 필드 자체가 없어야**(WP-01에서 원문 컬럼 미신설) — 정책은 "원문 없음"을 재확인하고 표식만.
- 라이선스/승인 미비 → finding(차단 아님, **검토 필요** 강조).
- "검토용 초안"·해시 전용 감사 유지. 외부 0·NuGet 0.

## 테스트(필수)
- **`CATALOG_ONLY` 공개 규정 → 정상 인용(PublicCited, WP-03 형식)**.
- `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED` entry 검색 → **메타·표식만, 원문/조항 본문 0**.
- 라이선스/승인 미비 entry → **구조화 `SafetyFinding`**(code/severity) 노출.
- 미지 status → MetadataOnly + `KB_UNKNOWN_STATUS` finding.
- **repo/KB-level 스캔**(`kb/`·`data_sources/`·samples, allowlist 외)에 내부규정/NCR **원문 파일 부재** 회귀.
- 기존 KbSearch 회귀 유지.

## 완료/보고
게이트가 코드로 강제됨(공개만 인용·내부/NCR 원문 비노출·라이선스 검증). build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17` 진행표 갱신. (→ RAG 완료, 다음 NCR WP-05.)

## Claude Review Checklist
`CATALOG_ONLY`→PublicCited / 내부·NCR **원문 비노출(메타+표식)** / 라이선스·승인·미지status **구조화 SafetyFinding**(code/severity) / **repo·KB-level**(`kb/`·`data_sources/`) 원문 미포함 스캔(allowlist) / 결정적·NuGet 0 / 기존 SmokeTest 유지 / Gate A.
