# Codex R3-WP-03 — 인용형 답변 강화 (출처·버전·시행일·조항·검색기준일)

> 권위 스펙: `docs/17`(R3 검색 답변 표시 항목), `docs/41 §2`, `CLAUDE.md §10`. Release: R3. 선행: R3-WP-01(메타), R3-WP-02(검색).

## 목표
검색 답변(`KbSearch.BuildDraftAnswer`)이 docs/17 인용 항목을 **완비**하게 한다: **문서명 · 버전 · 시행일 · 조항 · 출처(locator) · 검색 기준일 · "검토 필요" 문구**. 항상 **검토용 초안** 명시.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§10`, `docs/17`(R3), `docs/41 §2`, 기존 `Core/Kb/KbSearch.cs`(`KbSearchResult`·`BuildDraftAnswer`·`ReviewDraftNotice`).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/r3-wp-03-citation origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- `KbSearchResult`에 WP-01 메타(Version·EffectiveDate·Source·…) **노출 필드 추가**(검색 결과가 인용 가능).
- `BuildDraftAnswer`가 **결과별 인용 블록**: `[문서ID] 문서명 (버전, 시행일) / 출처: <locator> / 출처기관 / 조항 / 상태·라이선스`. 응답 상단에 **검색 기준일** + **"검토용 초안/검토 필요"**.
- `Search(...)`에 **검색 기준일(asOfDate)** 옵션(기본=호출자 제공; 테스트 결정성 위해 파라미터화).
- 제외: 적재가드(WP-04), NCR(WP-05), **실제 원문 chunk**(Prod KB·gate).

## Public Interface
- `KbSearchResult` 메타 필드 추가(WP-01 항목). `KbSearch.Search(string query, string userId = "anonymous", int maxResults = 5, string? asOfDate = null)`.
- **주입식 시계 `IClock`**(KbSearch 생성자, 기본=시스템 clock). 검색 기준일 = `asOfDate ?? clock.Today` — **항상 실제 날짜**(placeholder 아님). 기존 호출부 호환(추가 인자 optional).

## 구현 세부 / 보안
- **조항(clause)**: catalog가 catalog-level이라 조항 단위 원문이 없으면 → "조항: (catalog 단위 — 조항별 원문은 Prod 권한통제 KB)" 안내. catalog에 조항 필드가 있으면 그 값. **원문 적재 금지**.
- 메타 빈값 → "(미기재)" 표기(경고 finding은 WP-01/04 로직 유지). **단, 검색 기준일은 "(미기재)"/placeholder 금지.**
- **검색 기준일(실제 날짜 필수)**: `asOfDate ?? clock.Today`. `asOfDate` 미지정 경로도 **주입 clock의 실제 날짜**가 찍혀야 함. **`DateTime.Now` 직접 호출 금지**(테스트는 고정 clock 주입으로 결정적).
- "검토용 초안"·해시 전용 감사 **유지**. 외부 0·NuGet 0.

## 테스트(필수)
- 답변에 **문서명·버전·시행일·조항·출처(locator)·검색기준일·"검토" 문구** 포함(결과 있을 때) — **`조항` 필드 포함 단언 필수**.
- `asOfDate` 반영(결정적): 동일 입력+동일 asOfDate → 동일 답변.
- **asOfDate 미지정 경로**: 주입 clock의 **실제 날짜**가 기준일로 출력됨(placeholder/"(미기재)" 아님) 단언.
- 메타 빈값 → "(미기재)" graceful(검색기준일 제외).
- 기존 KbSearch 결과/점수·"검토용 초안" 회귀 유지.

## 완료/보고
검색 답변이 docs/17 인용 항목 완비. build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17` 진행표 갱신.

## Claude Review Checklist
인용 항목 완비(문서명·버전·시행일·**조항(테스트 단언)**·출처·검색기준일·검토필요) / 검색기준일 **실제 날짜**(IClock 주입, Now 직접출력·placeholder 금지, 미지정 경로 테스트) / 원문 chunk 미적재(조항=catalog 단위 안내) / "검토용 초안"·해시감사 유지 / NuGet 0 / 기존 SmokeTest 유지 / Gate A.
