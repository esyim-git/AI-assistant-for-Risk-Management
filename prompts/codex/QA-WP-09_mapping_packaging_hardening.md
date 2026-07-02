# Codex Prompt — QA-WP-09: Mapping/Packaging SmokeTest 하드닝 (Column Mapping·Integrity Manifest)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(QA-WP-09) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/qa-wp-09-mapping-packaging-hardening` (독립 off main). Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3`, `docs/39` QA-WP-09, `SKILLS.md`+`risk-data-limit-review`·`risk-security-guard`·`risk-smoke-governance`, `src/RiskManagementAI.Core/Mapping/*`(`ColumnMappingLoader`)·`src/RiskManagementAI.Core/Integrity/*`(`IntegrityVerifier`·`RequiredCriticalEntries`)·`build/01`·`build/03`, `tests/RiskManagementAI.SmokeTests/{MappingTests,PackagingTests}.cs`.
> **기준선**: main `693488c`(VERSION 0.7.0), 정본 SmokeTest `Total=877 PASS=877 FAIL=0`.

## 0. 목표 (단일 · 순수 additive 테스트)
Risk Column Mapping과 Integrity Manifest/패키징 가드의 **미커버 경계만** SmokeTest로 고정한다. **제품 코드·무결성 로직·build 스크립트 변경 0** — 테스트만. critical glob·required·co-deletion·fail-closed·원문 스캔(build/03) 불변식을 회귀로 잠근다.

## 1. 작업 범위 (MappingTests·PackagingTests — additive only)
1. `Core/Mapping/*`·`Core/Integrity/*`·`build/01·03`와 현 두 test를 대조 → **미커버 경계만** 추가.
2. Mapping 후보: `ColumnMappingLoader` 설정·safe fallback·`TryPhysical`·필수 6열·Optional(통화/단위) blank skip·물리컬럼명(인메모리 metadata)·매핑 실패 → `MAPPING_ERROR` 경계.
3. Packaging 후보: `IntegrityVerifier` critical glob(`rules`/`templates`/`kb`/`config·ncr`)·`RequiredCriticalEntries` 인벤토리·co-deletion fail-closed·manifest 축소/버전불일치 차단·경로 traversal 거부·`*.local.json` 유입 실패·build/03 원문 의심파일 Blocker(`KbRepositoryGuard`)·금지 확장자(`*.gguf/*.pem/*.cer` 등) 스캔.
4. **합성 더미만** — 실 매핑 실컬럼명·실 모델파일·원문 0. Packaging 단언은 **현 인벤토리와 정합**(신규 critical asset 추가 금지 — 테스트만).

## 2. 제외 범위
Mapping/Integrity 제품 코드·build 스크립트 변경. 신규 critical asset 추가. 기존 단언 수정/삭제/약화. 신규 NuGet.

## 3. 보안조건
무결성/매핑 제품 코드 변경 0 · fail-closed·co-deletion·원문 스캔·금지 확장자 회귀 정확 · 합성 더미(실 컬럼명·모델파일·원문 0) · NuGet 0 · **기존 테스트 삭제·약화 0** · Packaging 인벤토리 정합(과대표기 금지).

## 4. 테스트 (SmokeTest — 도메인 `Mapping`/`Packaging`)
> `SmokeTestContext.SmokeDomain`: Mapping(`ColumnMapping`/`mapping`/`mapped`/`renamed`/`physical column`) — 상단부 · Packaging(`build/0`/`VERSION`/`packaging`/`manifest`/`KbRepositoryGuard`/`source-text`/`Expand-Archive`/`PowerShell`). 신규 단언 설명을 각 도메인 토큰으로. 주의: Reconciliation/Report/Limit이 Packaging보다 위이므로 그 토큰 회피; Mapping은 상단이라 안전. `Unclassified=0`.
- 각 경계 → 기대(fallback·MAPPING_ERROR·critical glob·co-deletion 차단·manifest 축소 차단·원문 Blocker·금지 확장자) 단언.
- 기존 `MappingTests`/`PackagingTests` 단언 **전부 보존**. 종료부 **`Total=877 → 877+N PASS / 0 FAIL`**, `Unclassified=0`.

## 5. 보고 / Branch
- build 0/0 · SmokeTest 합계 줄(+Mapping/Packaging 증가·Unclassified 0) · Gate A 0 · `dotnet list package` PackageReference 0 · 추가 케이스 목록 · **Applied Skill Checklists**.
- Branch `feature/qa-wp-09-mapping-packaging-hardening` · Commit: `test: harden column mapping and integrity manifest coverage (QA-WP-09)`

## 6. Claude Review Checklist
제품 코드/무결성/build 변경 0(테스트만) / 추가는 실제 미커버 경계 / fail-closed·co-deletion·원문 스캔·금지 확장자·Mapping fallback 기대값 정확 / Packaging 인벤토리 정합(신규 critical asset 0) / 합성 더미(실 컬럼명·모델파일·원문 0) / 도메인 Mapping·Packaging·Unclassified 0 / 기존 두 test 보존·감소 0 / NuGet 0 / `Total` 877 보존+신규 / Gate A.
