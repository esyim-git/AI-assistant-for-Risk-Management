# Codex Prompt — QA-WP-02: Reconciliation·Limit 7상태/RECON 경계 SmokeTest 하드닝

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-02) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-02-recon-limit-edge-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-02, `SKILLS.md`+`risk-data-limit-review`·`risk-smoke-governance`·`risk-security-guard`, `src/RiskManagementAI.Core/{Risk,Data,Mapping}/*`(`LimitMonitor`·`LimitAnalysisResult`·대사 코드·7상태), `tests/RiskManagementAI.SmokeTests/LimitReconciliationTests.cs`.
> **기준선**: main `10030be`(VERSION 0.7.0), 정본 SmokeTest `Total=829 PASS=829 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
Exposure-Limit Join **7상태**(NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**)와 **대사 9코드**의 **경계·전이 SmokeTest 커버리지만 확대**한다. **제품 코드 변경 0** — 테스트만. 키스톤(원천합계=분석합계·row-amplification)·`Passed` 정의·enum ordinal은 그대로 검증.

## 1. 작업 범위 (LimitReconciliationTests.cs — additive only)
1. `LimitMonitor`·`LimitAnalysisResult`·대사 코드·7상태 산정 로직과 현 `LimitReconciliationTests`를 대조 → **미커버 경계만** 추가.
2. 후보(제품 동작 확인, 신규 동작 요구 아님):
   - 7상태 **경계값**: WARNING↔BREACH 임계 usage ratio 경계, NORMAL↔WARNING 경계, `NO_LIMIT`(한도 부재)·`INVALID_LIMIT`(0/음수/비수치 한도)·`MAPPING_ERROR`(매핑 실패)·`DUPLICATE_LIMIT`(중복 Join Key) 각 트리거 경계.
   - 대사 9코드: 각 `RECON_*` 코드가 나오는 최소 입력·안 나오는 경계. 원천합계=분석합계 불일치 감지·row amplification 감지.
   - BASE_DT 형식(yyyyMMdd/yyyy-MM-dd 정규화·invalid graceful) 경계.
   - 결정성: 동일 입력 → 동일 상태/코드/정렬.
3. 실데이터 금지 — **합성 더미**(placeholder PF/RF, 임의 숫자)만.

## 2. 제외 범위
`LimitMonitor`/대사/매핑 제품 코드 변경. 신규 상태·코드 추가. 기존 단언 수정/삭제/약화. Prior-Day/Visualization(별도). 신규 NuGet.

## 3. 보안조건
합성 더미만(실 PF/RF/한도/테이블·컬럼명 0) · 원문 0 · NuGet 0 · **기존 테스트 삭제·약화 0** · 키스톤/`Passed`/enum ordinal 불변 검증.

## 4. 테스트 (SmokeTest — 도메인 `Reconciliation`/`Limit`)
> `SmokeTestContext.SmokeDomain`: Reconciliation(line ~54: `Reconcil`/`RECON`/`duplicate limit`/`orphan limit`/`row amplification`/`base-date mismatch`) · Limit(line ~56: `LimitMonitor`/`limit`/`한도`/`exposure`/`BASE_DT`/`NO_LIMIT`/`INVALID_LIMIT`/`BREACH`/`WARNING`/`MAPPING_ERROR`/`usage ratio`). 신규 단언 설명을 이 토큰으로 두 도메인에 걸리게 하고, 더 위(`Xlsx`/`Csv`/`Mapping`) 트리거는 상황 맞게. `Unclassified=0`.
- 각 상태/코드 경계 → 기대 상태/코드 단언. 키스톤·row-amplification·결정성 회귀.
- 기존 `LimitReconciliationTests` 단언 **전부 보존**. 종료부 **`Total=829 → 829+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Reconciliation/Limit 증가·Unclassified 0) · Gate A 0 · PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-02-recon-limit-edge-hardening` · Commit: `test: harden reconciliation and limit 7-state edge coverage (QA-WP-02)`

## 6. Claude Review Checklist
제품 코드 변경 0(테스트만) / 추가는 실제 미커버 경계 / 7상태·대사 9코드 기대값 정확 / 키스톤·`Passed`·ordinal 불변 검증 / 합성 더미(실데이터 0) / 도메인 Reconciliation·Limit·Unclassified 0 / 기존 단언 보존·감소 0 / NuGet 0 / `Total` 829 보존+신규 / Gate A.
