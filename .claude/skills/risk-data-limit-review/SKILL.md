---
name: risk-data-limit-review
description: Review data ingestion, CP949/XLSX, Risk Column Mapping, Exposure-Limit Join, reconciliation, dashboard/report consistency, and hidden risk.
allowed-tools: Read Grep Glob Bash(dotnet run *) Bash(git diff *)
paths:
  - "src/RiskManagementAI.Core/Data/**"
  - "src/RiskManagementAI.Core/Mapping/**"
  - "src/RiskManagementAI.Core/Risk/**"
  - "src/RiskManagementAI.Core/Report/**"
  - "tests/**"
---

# Data & Limit Review

## 목적
리스크 데이터/한도 코드가 **합성한도 금지·7상태(DUPLICATE_LIMIT 포함)·대사 9종·승인형 ColumnMapping·실데이터 미포함** 원칙을 지키는지 읽기 전용으로 점검한다. 코드 동작은 바꾸지 않는 **점검/체크리스트 가이드**다.

## 언제 사용
- `src/RiskManagementAI.Core/Risk/**`, `src/RiskManagementAI.Core/Mapping/**`, `src/RiskManagementAI.Core/Data/**`, `config/column_mapping.json`, `samples/**` 파일을 작업할 때 **자동 적용**된다(`paths`).
- 트리거 예: "한도 로직 리뷰", "ColumnMapping 점검", "LimitMonitor 상태", "대사 검토", "limit review", "data review".

## 절대 원칙
- 합성/Demo 한도 산식(노출×배수) **0개** — 실 한도 없으면 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY`. (`docs/41_Approval_and_Pilot_Gates.md` §1)
- 실 테이블/컬럼명·실데이터 **repo 미포함** — 더미 일반명만(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 류). (`docs/03_DataCatalog.md`)
- 미승인 ColumnMapping은 미반영, all-or-nothing, `config/` 경로 가드. (`docs/41_Approval_and_Pilot_Gates.md` §1)
- 결정성(동일 입력=동일 수치)·읽기 전용. 운영 DB 자동접속/SQL·VBA 자동실행 **0**. (`CLAUDE.md` §3)
- 외부 NuGet/API/Telemetry/모델파일 **0**. 필요해지면 **STOP** → 승인 문서. (`CLAUDE.md` §11.5)

## 절차
1. **합성한도 점검**: `노출×배수`·`1.1m` 류 합성 한도 산식이 `src/`에 0개인지 확인. 실 한도 부재 시 빈 입력 경로가 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY`로 끝나는지 확인.
2. **ColumnMapping 점검**: 기본=현행 호환(`ColumnMapping.SafeDefaults()`), 커스텀은 all-or-nothing(누락/중복 시 fallback), `config/` 경로 가드(`ColumnMappingLoader.IsSafeRelativeConfigPath`). **미승인 매핑 미반영** 확인.
3. **상태셋·대사 점검**: `LimitMonitorStatus` **7상태**(`NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/DUPLICATE_LIMIT` — R2-WP-01 #79 ADD-ONLY) 분류와 대사 9종(`RECON_*`)·`ReconciliationSummary` 활성/적용(Applicable) 여부 확인. 중복 Join Key는 임의 선택 없이 `DUPLICATE_LIMIT`로 차단되는지 확인(`JoinAudit` 기록 포함).
4. **전일대비(Prior-Day) 점검**: `PriorDayAnalyzer`가 기존 `LimitMonitor.Analyze` 2회 diff(새 엔진 재구현 0)·same-day guard·`PRIOR_DAY_DUPLICATE_KEY`/`BASE_DT_FORMAT_MISMATCH` Hidden-Risk·4구획 계약을 유지하는지 확인. 현재 WPF call site 미노출 상태는 local-gate 전용으로만 표기(docs/48 B8).
5. **실데이터·결정성 점검**: 실 테이블/컬럼명·실데이터 미포함(더미 일반명만), `IsDeterministic=true`(동일 입력=동일 수치), 읽기 전용·자동실행 0 확인.

## 산출물/보고
- **데이터/한도 점검 결과** + 위반 항목 목록: `합성한도` / `미승인매핑` / `실데이터` / `비결정성` 4범주로 분류.
- 각 항목: 파일·라인 근거 + 상태 어휘(`VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`)만 사용. 실 Test PC 증거 없으면 Gate PASS 표기 금지.
- 위반 0건이면 "데이터/한도 점검: 위반 0건(코드리뷰 레벨)"로 보고. 실 오프라인 검증은 `docs/41` §4(Pilot Gate B/C) 별도.

## 체크리스트
상세 점검 항목은 [data-limit-checklist.md](data-limit-checklist.md) 참조 (합성한도/7상태/대사9종/매핑/실데이터/결정성).

## 참조
- `docs/03_DataCatalog.md` (repo 포함/금지 데이터)
- `docs/30_Demo_Scenario_Limit_Monitoring.md` (한도 모니터링 데모·Hidden Risk)
- `docs/41_Approval_and_Pilot_Gates.md` §1 (Data Spec Gate), §4 (Pilot Gate B/C)
- 관련 코드: `src/RiskManagementAI.Core/Risk/LimitMonitor.cs`, `src/RiskManagementAI.Core/Mapping/ColumnMapping*.cs`
- 연계 스킬: `/risk-analytics-design`, `/risk-security-guard`
