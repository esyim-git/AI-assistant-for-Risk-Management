# Codex R3-WP-05 — NCR Rule Set 구조 (모델 산식 암기 금지)

> 권위 스펙: `docs/08`(NCR Module · 심화 R3), `docs/18`(NCR Regulation Module Guide), `docs/41 §2`(RAG/NCR Gate), `CLAUDE.md §10`. Release: R3(마지막). 선행: WP-01~04(RAG) 완료.

## 목표
NCR을 **모델이 산식을 "기억"해서 답하는 구조가 아니라** 명시적 **Rule Set 데이터·규칙 구조**로만 산출·설명한다. `docs/08` 심화 R3의 **8요소**를 코드 구조로 구현. 답변은 항상 **검토용 초안**. **NCR 공식본 원문 repo 미포함.**

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§4·§10`, `docs/08`(심화 R3 표·답변구조), `docs/18`, `docs/41 §2`, 기존 `Core/Safety/SqlSafetyChecker.cs`(조회전용 검증), `Core/Kb/KbRepositoryGuard.cs`(원문 미포함 가드), `Core/Config/PolicyLoader.cs`(로더 패턴), `kb/ncr_placeholder.md`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/r3-wp-05-ncr-ruleset origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- 신규 `Core/Ncr/NcrRuleSet.cs`(아래 8요소 record) + `NcrRuleSetLoader.cs`(`config/ncr/*.json`에서 **구조만** 로드, PolicyLoader 패턴·경로 가드·safe fallback).
- `config/ncr/ncr_ruleset_sample.json`: **샘플/placeholder 구조만**(실 계수·실 산식 값 아님 — 승인·Prod 적재). 구성요소·서술적 산식·Validation SQL 템플릿·근거조항·승인이력 **구조**.
- **Validation SQL 템플릿은 `SqlSafetyChecker`로 조회 전용 검증**(차단 동사 포함 시 finding). **자동 실행 0.**
- NCR 설명 산출기 `NcrExplain`(또는 `NcrRuleSet` 메서드): Rule Set Version·Effective Date·Component Map·Formula Description·Validation SQL·Regulation Basis + **"검토용 초안"**. **모델이 산식을 생성/암기하지 않고 구조에서 서술**.
- 제외: **실 NCR 공식 계수·산식 값**(승인·Prod), NCR 최종 판단 자동화, 감독기관 보고 자동생성, **운영 DB/SQL 자동 실행**(조회 SQL도 실행 금지 — 템플릿만).

## Public Interface (8요소, docs/08 심화 R3)
```csharp
public sealed record NcrRuleSet(
    string RuleSetId,
    string RuleSetVersion,
    string EffectiveDate,                       // YYYY-MM-DD 문자열
    IReadOnlyList<NcrComponent> Components,      // 구성요소·계수·분류 (구조; 실값은 승인 후)
    IReadOnlyList<NcrComponentMap> ComponentMap, // 구성요소 ↔ 데이터컬럼/소스
    string FormulaDescription,                  // 서술적 정의(코드/데이터로 검증 가능, 암기 아님)
    IReadOnlyList<string> ValidationSqlTemplates,// 조회 전용 대사 SQL 템플릿
    string RegulationBasis,                      // 근거 규정/조항(출처)
    IReadOnlyList<NcrApprovalRecord> ApprovalHistory);
// NcrRuleSetLoadResult { NcrRuleSet RuleSet, bool UsedFallback, IReadOnlyList<SafetyFinding> Findings }
```
- 로더는 `PolicyLoader` 패턴(`UsedFallback`/`Findings`). `config/ncr/`만 읽기(경로 가드). 누락/손상 → safe fallback + 경고(throw 금지).

## 구현 세부 / 보안 (게이트 docs/41 §2)
- **모델 산식 암기 금지**: 하드코딩된 "산식 결과값"이나 모델 생성 산식 금지. 산출/설명은 **구조(Component·ComponentMap·FormulaDescription·Validation SQL)** 로만 재현·감사 가능.
- **NCR 공식본 원문 repo 미포함**: rule set 파일/샘플에 NCR 공식 해설 **원문 텍스트 금지**(`KbRepositoryGuard` 연계 — 원문 의심 시 Blocker). 구조·서술·근거조항 식별자만.
- **Validation SQL = 조회 전용**: `SqlSafetyChecker`로 검증(`INSERT/UPDATE/DELETE/MERGE/EXEC/...` 포함 시 finding). **자동 실행 0**.
- 답변/설명은 **검토용 초안** 명시(공식 해석 아님, `CLAUDE.md §10`).
- 결정적·외부 0·NuGet 0·**모델 가중치/자동학습 0**.

## 테스트(필수)
- Rule Set **8요소** 로드(샘플 구조). `UsedFallback`/경고 동작(파일 없음/손상).
- **Validation SQL 템플릿이 `SqlSafetyChecker` 통과**(조회 전용); 차단 동사 주입 시 finding.
- NCR 설명 출력에 **Version·EffectiveDate·ComponentMap·FormulaDescription·RegulationBasis·"검토용 초안"** 포함.
- **NCR 공식본 원문 repo 부재** 회귀(`KbRepositoryGuard`/스캔 — `config/ncr/` 포함).
- 하드코딩 산식 결과값/모델 생성 0(구조 기반) 확인.
- NuGet 0 / 기존 SmokeTest 유지.

## 완료/보고
NCR이 Rule Set 8요소 구조로만 산출·설명(모델 암기 0), Validation SQL 조회전용, 원문 미포함. build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17`·`docs/08` 진행 갱신. (→ R3 RAG+NCR 완료.)

## Claude Review Checklist
8요소 구조 / **모델 산식 암기·생성 0(구조 기반 재현)** / Validation SQL 조회전용(SqlSafetyChecker)·자동실행 0 / **NCR 공식본 원문 repo 미포함**(guard) / safe fallback·경로 가드 / 검토용 초안 / NuGet 0·모델 0 / 기존 SmokeTest 유지 / Gate A.
