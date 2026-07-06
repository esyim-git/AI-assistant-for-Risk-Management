---
name: risk-analytics-design
description: Design prior-day risk analytics, charts, heatmap, Top N, concentration, market/hedge/valuation/liquidity views, and Excel report extensions.
argument-hint: "[topic or WP-ID]"
arguments: [topic]
disable-model-invocation: true
allowed-tools: Read Grep Glob
---

# Risk Analytics Design

## 목적
R2 완료 이후의 리스크 분석 확장(전일대비 UI, 시각화, 대용량 안내, 후속 분석 WP)을 **ADR 초안(`docs/40`)·WP 분해(`docs/39`)** 로 설계한다. 구현은 Codex가 하고, 이 스킬은 설계/계획 산출물만 만든다(코드 동작 변경 없음).

## 언제 사용
- "전일대비 UI / prior-day / risk analytics design / RISK_VISUAL / heatmap / TopN / concentration / 대용량 streaming 안내 / 분석 UI 확장" 류 요청.
- R2-WP-01~05 완료 상태를 전제로, 후속 분석·시각화 기능을 ADR 결정 + 구현 단위(WP)로 분해해 NEXT UP 후보로 올릴 때.
- 완료된 R2 Semantic Hardening(7상태·ColumnMapping 경유·BASE_DT 정규화)을 재설계하지 않고 회귀 조건으로 고정할 때.

## 절대 원칙
- 외부 NuGet/API/Telemetry/모델파일 **0** — 분석·차트는 인박스(BCL/OOXML/WPF)만. 필요해지면 **STOP** → 승인 문서(`docs/40`·`docs/41`). (`CLAUDE.md §3·§11.5`)
- **결정성**(동일 입력=동일 수치)·**감사가능성**(Join 선택·정규화 규칙을 Audit Metadata에 기록)을 설계 전제로 둔다. (`docs/40` ADR-002)
- 통화·단위 비교는 **승인형 ColumnMapping 논리컬럼** 경유가 R2-WP-01에서 완료된 기준선이다. 후속 설계는 이 상태를 회귀로 보존하고, 미승인 매핑 미반영 원칙을 유지한다.
- Limit Monitor 상태는 **7상태**(`NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/DUPLICATE_LIMIT`)가 기준선이다. 중복 Limit Key는 임의선택하지 않고 명시 상태화한다. 대사 9종 삭제·약화 금지.
- 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보 **repo·스킬파일 미포함** — 예시는 더미명(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`)만. (`CLAUDE.md §3`)
- 상태 어휘 정본만: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED. 과대표기 금지·실 Test PC 증거 없으면 Gate PASS 금지. (`CLAUDE.md §11.4`)

## 절차
1. **기준선·범위 확인**: `docs/38`·`docs/39`에서 R2-WP-01~05 완료 상태(7상태, ColumnMapping 경유, Prior-Day, RISK_VISUAL, dead Welford cleanup)를 확인한다. 후속 설계는 완료사항을 재구현하지 않고 회귀 조건으로 둔다. 상세는 [r2-scope.md](r2-scope.md).
2. **ADR 초안 작성**: 설계 결정은 `docs/40` ADR 형식(상태·맥락·결정·결과·대안/기각)으로 초안화한다. RR-15 Semantic Hardening과 전일대비(WP-09 설계 산출물)를 각 결정 또는 ADR-002 보강으로 정리한다.
3. **R2-WP 분해**: 구현 단위는 `docs/39` R2-WP 형식으로 쪼갠다(목표·범위·수정예상파일·테스트·Branch·Commit·Review Checklist). 단일 WP 프롬프트화는 [/risk-wp-planner](../risk-wp-planner/SKILL.md)로 넘긴다(한 번에 WP 1개).
4. **절대 원칙 준수 설계**: NuGet 0·결정성·승인형 매핑·실데이터 미포함·기존 테스트 보존을 각 WP 보안조건/테스트에 명시한다(7상태 보존, 통화·단위 ColumnMapping 경유 회귀, RECON_UNIT 양성/음성, BASE_DT 정규화, Audit 기록).
5. **게이트 연결**: R2는 **Data Spec Gate**(`docs/41` §1) 후속이다. 각 WP가 결정성·감사가능성·회귀를 어떻게 Data Gate에 연결하는지 명시하고, 실 검증은 Pilot Gate B/C(`docs/41` §4, BLOCKED)로 분리한다. 상세 점검은 [/risk-data-limit-review](../risk-data-limit-review/SKILL.md).

## 산출물/보고
- **분석 ADR 초안**(`docs/40` 형식: 상태/맥락/결정/결과/대안·기각) — R2 완료 기준선 + 후속 분석/UI 확장.
- **후속 WP 분해**(`docs/39` 형식): 목표·범위·테스트·Branch·Commit·Review Checklist. 완료된 R2-WP-01~05는 재실행하지 않는다.
- **Data Gate 연결** 명시: 각 WP의 결정성·감사가능성·회귀 → Data Spec Gate, 실 검증은 Pilot Gate B/C(BLOCKED).
- 상태는 어휘 정본만(과대표기 금지). main 직접 수정 금지 — 계획 작업은 `planning/*` 브랜치(`CLAUDE.md §11.1`).

## 체크리스트
R2 완료 기준선과 후속 분석 설계 노트는 [r2-scope.md](r2-scope.md) 참조.

## 참조
- `docs/38_v1_Master_Roadmap.md` (R2 Release Train · RR-15 · Cap C-13~C-16 Traceability §5)
- `docs/41_Approval_and_Pilot_Gates.md` §1 (Data Spec Gate), §4 (Pilot Gate B/C, BLOCKED)
- `docs/40_ADR_Architecture_Evolution.md` (ADR 형식 · ADR-002 공통 AnalysisResult)
- `docs/39_Work_Package_Backlog.md` (R2-WP-01~05 완료 기준선 · 후속 WP 후보)
- 관련 코드(경로만, 원문 미포함): `src/RiskManagementAI.Core/Risk/LimitMonitor.cs`, `src/RiskManagementAI.Core/Mapping/ColumnMapping*.cs`
- 연계 스킬: [/risk-wp-planner](../risk-wp-planner/SKILL.md)(WP 1개 분해·프롬프트) · [/risk-data-limit-review](../risk-data-limit-review/SKILL.md)(데이터/한도 코드리뷰)
