# Codex UX-WP-02 — Static SQL/VBA/Excel/Risk Completion Providers

> 권위 스펙: `docs/39 §UX-WP-02`, `docs/46`, `docs/40`(ADR-010). 선행: **UX-WP-01**. 우선순위: `AGENTS.md` > `docs/39` > 본 프롬프트.
> 정적·결정적 provider만. **모델 0**. 차단/허용 판단은 **기존 Safety Checker/RuleSet 재사용**(룰 중복 정의 금지).

## 현재 문제 / 목표
UX-WP-01 계약 위에 실제 추천 콘텐츠를 채운다 — SQL keyword/snippet(조회 전용), VBA 안전 snippet, Excel 2021 함수, Excel 365 차단+대체 힌트, SafetyHint, Risk phrase seed.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §4·§5·§6`, `docs/46`, `Core/Safety/`(`SqlSafetyChecker`·`VbaSafetyChecker`·`Excel2021FunctionChecker`·RuleSet), `docs/16`(VBA), UX-WP-01의 `ICompletionProvider`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/ux-wp-02-static-providers origin/main   # UX-WP-01 머지 후
```
- .NET 8. Gate A. **NuGet 0**.

## 작업 범위 (`Core/Assist/Providers`)
1. **SqlCompletionProvider** (CAP-UX-02): 조회 전용 keyword(`SELECT/FROM/WHERE/JOIN/GROUP BY/ORDER BY/HAVING/ON/AS`)·snippet(`SELECT ... FROM ... WHERE BASE_DT = :BASE_DT`). 차단 DML/DDL(`INSERT/UPDATE/DELETE/MERGE/CREATE/ALTER/DROP/TRUNCATE/GRANT/REVOKE/EXEC/CALL/COMMIT/ROLLBACK`) **미추천** + 입력 시 `BlockedHint`. 차단 목록 = **`SqlSafetyChecker`/RuleSet 재사용**.
2. **VbaCompletionProvider** (CAP-UX-03): 안전 snippet(`Option Explicit` 헤더·에러 처리·배열 루프·`Application` 상태 원복). 금지 API(`Shell/WScript.Shell/Kill/FileSystemObject 삭제·이동/Declare PtrSafe/WinAPI/Outlook/외부 URL`) **미추천** + `BlockedHint`.
3. **Excel2021CompletionProvider** (CAP-UX-04): 허용 함수(`XLOOKUP/XMATCH/FILTER/SORT/SORTBY/UNIQUE/SEQUENCE/LET/SUMIFS/COUNTIFS/INDEX/MATCH`). **Excel365BlockedHintProvider**: 365 전용(`VSTACK/HSTACK/TOCOL/TOROW/TEXTSPLIT/TEXTBEFORE/TEXTAFTER/GROUPBY/PIVOTBY/MAP/REDUCE/BYROW/BYCOL/REGEX*`) 입력 시 **2021 대체안 + `BlockedHint`**. 함수 목록 = **`Excel2021FunctionChecker`/RuleSet 재사용**.
4. **SafetyHintProvider** (CAP-UX-05): 입력을 기존 Checker에 통과시켜 위험/비호환을 `SafetyHint`로 노출(룰 재구현 금지, 동일 RuleSet 경유).
5. **RiskPhraseProvider** (CAP-UX-06): 리스크 코멘트 **일반 문구 seed**(예: "기준일 기준 노출 합계", "한도 초과 항목 후속 점검 필요"). 전부 `RequiresReview=true`.
- **제외**: WPF(UX-WP-03), LLM, 스키마 introspection, 새 NuGet.

## 구현 세부 / 보안
- 결정적. **실 테이블명/내부규정 원문/실데이터 seed 0**(일반 표현·일반 더미명만). RuleSet/Checker 재사용(차단·허용 단일 원천). 외부 0.

## 테스트
- SQL 차단 DML 미추천 + `BlockedHint`(양성). VBA 금지 API 미추천. Excel 2021 허용 추천 + 365 입력 시 대체+`BlockedHint`. SafetyHintProvider 판정 = 기존 Checker와 동일. RiskPhrase 전부 `RequiresReview` + 실데이터/원문 0(스캔). `Total` 보존+신규(분류 키워드 포함).

## 완료/보고
provider 5종(+365 힌트) + 회귀. build 0/0·SmokeTest `Total=N PASS/0 FAIL`·Gate A·NuGet 0. `docs/39` UX-WP-02 DONE 요청.

## Claude Review Checklist
RuleSet/Checker 재사용(룰 분기 0) / 차단 DML·금지 API 미추천 / 365 대체 힌트 / **실데이터·원문 0** / 전부 RequiresReview / NuGet 0 / 기존 테스트 불변 / Gate A.
