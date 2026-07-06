# Data & Limit Review — 상세 체크리스트

읽기 전용 점검용. 코드 동작/테스트를 바꾸지 않는다. 근거는 항상 **파일·라인**으로 남기고, 상태는 정본 어휘(`VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`)만 사용한다. 실 Test PC 증거 없이 Gate PASS 표기 금지.

근거 문서: `docs/03_DataCatalog.md`, `docs/30_Demo_Scenario_Limit_Monitoring.md`, `docs/41_Approval_and_Pilot_Gates.md` §1/§4.

---

## 1. 합성 한도 금지 (DEMO_ONLY / LIMIT_DATA_REQUIRED)
- [ ] `노출 × 배수` 류 **합성 한도 산식 0개** — `src/`에서 임의 계수(예: `1.1m` 류 하드코딩 multiplier)로 한도를 만들어내지 않는지 확인.
- [ ] 실 한도 부재 시 빈/한도없음 입력 경로가 `LIMIT_DATA_REQUIRED` 또는 `DEMO_ONLY`로 명시 종료되는지 확인 (한도를 추정·생성하지 않음).
- [ ] 한도값은 입력(한도 테이블)에서만 읽고, 코드가 한도를 발명하지 않는지 확인.
- 근거: `docs/41` §1 ("합성/Demo 한도 산식 0개 — 실 한도 없으면 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY`").

검색 힌트 (Grep, 읽기 전용):
- `pattern: "\\* *1\\.\\d+m"` (하드코딩 배수 의심)
- `pattern: "DEMO_ONLY|LIMIT_DATA_REQUIRED"`

## 2. 상태셋 7상태 (R2-WP-01 이후 정본)
- [ ] `LimitMonitorStatus` enum이 정확히 **7값**: `Normal/Warning/Breach/NoLimit/InvalidLimit/MappingError/DuplicateLimit` (enum 서수 [0..6] 회귀 유지).
- [ ] `StatusCode` 매핑이 `NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/DUPLICATE_LIMIT`와 일치.
- [ ] 분류 기준: |사용률| > 1 → BREACH, ≥ 0.9 → WARNING, 그 외 → NORMAL(절대값 노출 기준 — 숏 포지션 처리). 한도 미매칭 → NO_LIMIT, 사용불가 한도(≤0 또는 USE_YN≠Y) → INVALID_LIMIT, 매핑 물리컬럼 불일치 → MAPPING_ERROR, **동일 Join Key 한도 >1행 → DUPLICATE_LIMIT(차단 — `group.Last()` 류 임의 선택 금지, `JoinAudit`에 기록)**.
- [ ] 상태별 회귀 테스트가 존재/유지되는지 확인 (삭제·약화 금지).
- 근거: `LimitMonitor.cs`, R2-WP-01(#79), `docs/38 §5` C-13, `docs/41` §1.

## 3. 대사 9종 (RECON_*)
- [ ] 9개 코드 모두 존재: `RECON_EXPOSURE_NO_LIMIT`, `RECON_LIMIT_NO_EXPOSURE`, `RECON_DUPLICATE_LIMIT`, `RECON_BASEDATE_MISMATCH`, `RECON_CURRENCY_MISMATCH`, `RECON_UNIT_MISMATCH`, `RECON_NONPOSITIVE_LIMIT`, `RECON_ROW_AMPLIFICATION`, `RECON_SUM_BALANCE`.
- [ ] `ReconciliationSummary`가 9 check 모두 보고하고, `Applicable` 플래그(통화/단위는 양쪽 컬럼 존재 시에만 활성)를 정확히 표시.
- [ ] PASS/FAIL은 fail-code 기반(`RECON_NONPOSITIVE_LIMIT`/`RECON_ROW_AMPLIFICATION`/`RECON_SUM_BALANCE`) — 정보성 코드는 PASS/FAIL에 영향 없음.
- [ ] **원천합계 = 분석합계** 대사(`RECON_SUM_BALANCE`): 합계 불일치 또는 누락(비숫자/MappingError) 시 FAIL.
- [ ] 건수증폭(`RECON_ROW_AMPLIFICATION`): 기준일 노출 행 수 대비 분석/잠재 조인 행 수가 크면 FAIL.
- [ ] **통화/단위는 승인형 ColumnMapping(Optional 논리컬럼 `CurrencyCode`/`UnitCode`) 경유** — R2-WP-01(#79)에서 하드코딩 const 이관 완료, `RECON_UNIT_MISMATCH` 활성(비-fail·currency 대칭). 잔여 하드코딩은 **표시 전용** `DESK_CD`/`PRODUCT_TYPE` 2개뿐(조인/계산 미사용 — 매핑 전환은 후속 WP, `docs/39` 명시).
- [ ] 대사 깊이 한계 인지(informational): `RECON_SUM_BALANCE`는 decimal 정밀 등가(허용오차 0), 통화/단위는 문자열 불일치 검출만(환산 없음) — 보수적 설계이며 완화 시 승인 필요.
- 근거: `LimitMonitor.cs`, R2-WP-01(#79), `LimitReconciliationTests.cs`(통화/단위 매핑 경유 회귀), `docs/41` §1.

## 4. ColumnMapping (승인형)
- [ ] 기본값 = 현행 호환 상수(`ColumnMapping.SafeDefaults()`: `BASE_DT/PORTFOLIO_ID/RISK_FACTOR/EXPOSURE_AMT/LIMIT_AMT/USE_YN`) — 더미 일반명.
- [ ] **all-or-nothing**: 필수 논리컬럼 누락·중복(물리컬럼 중복) 시 커스텀 전체 거부 → `SafeDefaults()` fallback + 경고(`UsedFallback=true`, `COLUMN_MAPPING_FALLBACK`).
- [ ] **config 경로 가드**: `config/`로 시작하는 상대 `.json`만 허용, rooted/`.`/`..` 금지(`IsSafeRelativeConfigPath`).
- [ ] **미승인 매핑 미반영**: repo 기본 `config/column_mapping.json`은 승인된 baseline만. 비-기본 커스텀 매핑은 승인 시에만 반영.
- [ ] fallback 경고가 분석 결과 exception/finding으로 노출되는지 확인.
- 근거: `ColumnMapping.cs`, `ColumnMappingLoader.cs`, `docs/41` §1.

## 5. 실데이터/실컬럼명 미포함
- [ ] repo·skill 파일·samples 어디에도 회사 실거래/포지션/고객정보, 실제 테이블명·컬럼명 전체 사전, 내부규정 원문, NCR 공식본 원문, 계정/비밀번호/접속문자열, 모델파일 없음.
- [ ] 예시·테스트는 더미 일반명만(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 등). 운영 DB 접속문자열 0.
- [ ] `samples/**`는 더미 CSV/XLSX placeholder만.
- 근거: `docs/03_DataCatalog.md` (포함 금지 데이터), `CLAUDE.md` §3.

## 6. 결정성 / 읽기 전용
- [ ] 동일 입력 → 동일 수치/정렬(난수·시계·환경 의존 0). `IsDeterministic=true` 메타가 일관.
- [ ] 정렬 키가 안정적(`OrdinalIgnoreCase` 등 고정) — Dashboard·Report 동일 입력→동일 수치(공통 `LimitAnalysisResult` 소비).
- [ ] 사용률을 Dashboard/Report에서 **재계산하지 않고** 분석 결과를 그대로 소비.
- [ ] SQL/VBA/Golden6 **자동실행 0**, 운영 DB 자동접속 0, 외부 API/NuGet/Telemetry/모델파일 0. 필요해지면 STOP → 승인 문서(`docs/41`).
- 근거: `LimitMonitor.cs` (`LimitAnalysisMetadata`), `docs/41` §1, `CLAUDE.md` §3/§11.5.

## 7. 전일대비 (Prior-Day Analytics, R2-WP-03)
- [ ] `PriorDayAnalyzer`는 기존 `LimitMonitor.Analyze`를 Current/Prior **2회 호출**해 diff — 새 분석 엔진/새 상태 재구현 0.
- [ ] 기준일은 호출자 공급(자동 영업일 계산 없음) · 정규화 후 동일 기준일 → 예외(same-day guard).
- [ ] Movement 분류(`New/Resolved/Increased/Decreased/Unchanged/StateTransition`) · TopN movers는 `|UsageRatioDelta|` 기준(New/Resolved/StateTransition 제외).
- [ ] Hidden-Risk finding: `BASE_DT_FORMAT_MISMATCH` · `PRIOR_DAY_STATE_TRANSITION` · `PRIOR_DAY_DUPLICATE_KEY` 유지.
- [ ] 4구획 출력 계약(`DataFact/Methodology/UserValidation/HiddenRisk`) + 검토용 초안 고지 · `IsDeterministic=true`.
- [ ] **현 상태 표기**: WPF call site 미노출 → Test PC PASS로 봉인 금지, local-gate 전용(docs/48 B8). UI 배선 WP(UI-WP-12류) 후 Gate B 대상 전환.
- 근거: `PriorDayAnalyzer.cs`, R2-WP-03(#84), `docs/48` B8.

---

## 보고 양식 (예시)
```
데이터/한도 점검 결과 (코드리뷰 레벨)
- 합성한도:   위반 N건  [근거 file:line / 상태]
- 미승인매핑: 위반 N건  [...]
- 실데이터:   위반 N건  [...]
- 비결정성:   위반 N건  [...]
종합: 위반 0건 → VERIFIED(코드리뷰), 실 오프라인 검증 = docs/41 §4(BLOCKED 대기)
```

## 연계
- `/risk-analytics-design` — 한도/분석 설계 정합성.
- `/risk-security-guard` — 커밋 전 보안 게이트(secret/실데이터 스캔).
