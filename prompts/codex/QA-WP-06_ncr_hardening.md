# Codex Prompt — QA-WP-06: NCR Rule Set 구조 SmokeTest 하드닝 (SCAFFOLD_ONLY·조회전용·검토용 초안)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-06) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-06-ncr-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-06, `SKILLS.md`+`risk-rag-ncr-governance`·`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Ncr/*`(Rule Set 8요소 구조), `docs/08*`/`docs/18*`(NCR 정본 포맷), `tests/RiskManagementAI.SmokeTests/NcrTests.cs`.
> **기준선**: main `693488c`(VERSION 0.7.0, 확장 트랙 Wave 2 머지 후), 정본 SmokeTest `Total=877 PASS=877 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
NCR Rule Set **8요소 구조(SCAFFOLD_ONLY)**의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드 변경 0** — 테스트만. **실 NCR 계수·공식본 원문은 없다**(placeholder·검토용 초안)는 불변식을 회귀로 잠근다. 과대표기 금지: 이 WP는 "구조 검증"이지 "NCR 산정 검증"이 아니다.

## 1. 작업 범위 (NcrTests.cs — additive only)
1. `Core/Ncr/*`와 현 `NcrTests`를 대조 → **미커버 경계만** 추가.
2. 후보(구조 확인·신규 동작 요구 아님): Rule Set 8요소 필드 존재·구조 결정성 · 샘플=placeholder(실 계수 0)·검토용 초안 표식 · 조회전용 SQL(INSERT/UPDATE/DDL 0) · Rule Pack 미적재 시 계산 차단(SCAFFOLD_ONLY 상태 유지) · 승인 Rule Pack 부재 graceful.
3. **합성 더미만** — 실 NCR 공식본 원문·실 계수 0.

## 2. 제외 범위
NCR 산정 로직 구현·실 Rule Pack 적재(APPROVAL_REQUIRED). Rule Set 구조 제품 코드 변경. 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
**실 NCR 공식본 원문/실 계수 0**(placeholder만) · 검토용 초안 표식 회귀 · 조회전용 · NuGet 0 · **기존 테스트 삭제·약화 0** · 과대표기 금지(구조 검증만).

## 4. 테스트 (SmokeTest — 도메인 `Ncr`)
> ⚠️ 도메인 `Ncr`가 목표 — 신규 단언 설명에 `SmokeTestContext.SmokeDomain`의 **Ncr 토큰**(`Ncr`/`NCR Rule`/`NCR 공식`/`Rule Set`)을 포함해 Ncr로 분류(Ncr은 Kb 바로 위이므로 Kb 토큰 `검색`/`원문`/`공개`/`인용`/`citation` 회피). 더 위(Report `report `·Limit·Reconciliation) 트리거도 회피. `Unclassified=0`.
- 각 구조 경계 → 기대(8요소 존재·placeholder·검토용 초안·조회전용·Pack 부재 차단) 단언.
- 기존 `NcrTests` 단언 **전부 보존**. 종료부 **`Total=877 → 877+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Ncr 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-06-ncr-hardening` · Commit: `test: harden ncr rule set structure coverage (QA-WP-06)`

## 6. Claude Review Checklist
제품 코드 변경 0(테스트만) / 추가는 실제 미커버 경계 / 실 NCR 계수·공식본 원문 0(placeholder·검토용 초안) / 조회전용 / 과대표기 금지(구조 검증만·산정 아님) / 도메인 Ncr·Unclassified 0(Kb 토큰 회피) / 기존 NcrTests 보존·감소 0 / NuGet 0 / `Total` 877 보존+신규 / Gate A.
