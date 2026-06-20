# 38. v1.0 Master Roadmap & Release Train (v0.4.0 → v1.0.0)

> v0.4.0(MVP-1+2+3, SmokeTest 268) 이후 v1.0 Team Pilot까지의 **통합 실행 로드맵**.
> 세 방향(기능 심화 · v1.0 준비 · 운영 검증)을 **의존성·리스크 기준 Release Train**으로 통합한다.
> 개념 로드맵은 `docs/10`, WP 상세는 `docs/39`, 아키텍처 결정은 `docs/40`, 승인/파일럿 게이트는 `docs/41`.

## 0. 기준선 (재설계 금지)
- v0.4.0 = MVP-1(룰엔진·프로파일러·해시감사로그·정책) + MVP-2(LLM NoModel·초안 파이프라인·규정 catalog·피드백 승격·인박스 Excel 리포트) + MVP-3(Dashboard·한도모니터링·History·Settings·Feedback UI). SmokeTest 268.
- **완료 기능은 재설계하지 않는다.** 본 로드맵은 v0.4.0 다음 단계부터.

## 1. 절대 원칙 (전 릴리스 유지)
Offline · 외부 NuGet 0 · 외부 API/Telemetry/AutoUpdate 0 · SQL/VBA 자동실행 0 · 해시 기반 Audit Log · NoModelMode 유지 · 실데이터/내부규정원문/모델파일 repo 포함 금지 · 모델 가중치 자동학습 금지 · **기존 SmokeTest 삭제/약화 금지**.

## 2. Release Train (버전은 가이드, 순서는 의존성 기준)

| Release | 버전(안) | 테마 | 핵심 게이트 | 상태 |
|---|---|---|---|---|
| **R1** | v0.5.0 | **Data & Limit Foundation** | Data Spec Gate | NEXT (READY_FOR_CODEX) |
| R2 | v0.6.0 | Risk Analytics & Visualization | Data Spec Gate | 설계 |
| R3 | v0.7.0 | Regulation & NCR RAG (공개·인용형) | RAG/NCR Approval Gate | 설계(승인 필요부 STOP) |
| R4 | v0.8.0 | Local LLM **Adapter (설계 전용)** | Model Approval Gate | 설계만 + STOP |
| R5 | v0.9.0 | Feedback Learning (승인형) | — | 설계 |
| R6 | v1.0.0 | Team Pilot | Pilot Gate B/C | 설계 |

> **데이터 정확성을 Local LLM보다 먼저 확보**한다(R1→R2 우선). R3(RAG)는 R1 이후 병렬 가능. R4(LLM)는 설계만, 런타임 도입 시 STOP.

## 3. Capability → Release 배치

| Capability | Release | 비고 |
|---|---|---|
| 합성/Demo 한도 제거·DEMO_ONLY 차단 | R1 | `BuildUiLimitRows` 1.1× 합성 제거(WP-01) |
| 실제 Exposure-Limit Join (LimitMonitor → 공통화) | R1 | WP-05 |
| CP949/UTF-8 CSV 입력 | R1 | WP-02 |
| XLSX 입력 (인박스, NuGet 0) | R1 | WP-03 |
| Risk Column Mapping (설정·승인형) | R1 | WP-04 |
| 대사·예외검증(미매핑/중복/기준일/통화/단위/음수한도/건수증폭/원천합계) | R1 | WP-06 |
| Dashboard·Report 공통 AnalysisResult | R1 | WP-07 |
| 전일 대비 데이터모델 | R1(설계)→R2(구현) | WP-09 |
| 차트·Heatmap·TopN·집중도·관점별 분석 | R2 | 인박스 WPF; 실차트 필요시 BLOCKED |
| Excel 리포트 강화(AI_COMMENTARY 입력 슬롯) | R2 | |
| 공개 규정 원문 적재·버전·인용형 RAG | R3 | keyword/inverted index(NuGet 0); vector/embedding STOP |
| NCR Rule Set/Component Map/Validation SQL/Approval | R3 | 모델이 산식 암기 금지 |
| 권한통제형 내부 KB 구조(원문 repo 미포함) | R3 | Prod 승인·역할권한·조회로그 |
| Local LLM Adapter/Manifest/Integrity/ProcessBoundary | R4 | 설계+ADR만, 런타임 STOP |
| 승인형 Feedback 저장·검색·재사용 | R5 | 가중치 자동학습 금지 |
| Test PC Gate B/C · Team Pilot | R6 | docs/41 |

## 4. 의존성 그래프 (요약)
```text
R1(Data Foundation) ──► R2(Analytics/Viz)
        │                    │
        ├──────────────► R6(Pilot)
        ▼                    ▲
R3(RAG/NCR) ───────────────►│
R4(LLM Adapter, 설계) ──► R5(Feedback) ─►│
```
- R2/R3는 R1의 **공통 AnalysisResult + 정확 데이터**에 의존.
- R4는 설계만(런타임 미도입), R5는 R4 인터페이스 + R3 KB에 의존.
- R6는 전체 안정화.

## 5. Traceability Matrix (Capability ↔ WP ↔ Test ↔ Gate)

| Cap-ID | Capability | WP | 검증 테스트(요지) | Gate |
|---|---|---|---|---|
| C-01 | DEMO_ONLY 차단 | WP-01 | 합성한도 미생성·DEMO 표식 | Data |
| C-02 | CP949/UTF-8 CSV | WP-02 | CP949 한글 라운드트립 | Data |
| C-03 | XLSX 입력 | WP-03 | xlsx 파싱·손상파일 graceful | Data |
| C-04 | Column Mapping | WP-04 | 매핑룰 적용·미매핑 검출 | Data |
| C-05 | Exposure-Limit Join + AnalysisResult | WP-05 | BASE_DT 조인·상태·건수증폭 | Data |
| C-06 | 대사·예외검증 | WP-06 | 9종 예외 케이스 | Data |
| C-07 | Dashboard·Report 공통화 | WP-07 | 동일 입력→동일 수치 | Data |
| C-08 | 전일대비 모델 | WP-09 | 모델 설계 단위검증 | Data |
| C-09~ | (R2~R6) | docs/39 | (각 WP) | RAG/Model/Pilot |

> 모든 Capability는 본 Matrix와 `docs/39` WP에서 추적 가능해야 한다.

## 6. Risk Register

| RR | 리스크 | 영향 | 완화 |
|---|---|---|---|
| RR-01 | 합성 한도(1.1×)가 실값으로 오인되어 리스크 오판 | 高 | WP-01 즉시 차단·DEMO_ONLY 표식 (R1 최우선) |
| RR-02 | CP949 Golden6 CSV 한글 깨짐 | 高 | WP-02 인코딩 감지/지정 |
| RR-03 | Dashboard·Report 수치 불일치(분기) | 高 | WP-07 공통 AnalysisResult |
| RR-04 | Join 후 중복 한도로 건수 증폭 | 中 | WP-06 건수증폭·중복 검증 |
| RR-05 | RAG가 vector/embedding 라이브러리 요구 | 中 | NuGet 0 keyword index; 필요시 STOP·승인(docs/41) |
| RR-06 | Local LLM 런타임 무단 도입 | 高(원칙위반) | R4 설계만·런타임 STOP·Model Approval Gate |
| RR-07 | 내부규정 원문 repo 유입 | 高(보안) | repo 미포함·Prod 권한통제(docs/41 RAG Gate) |
| RR-08 | XLSX/대용량 파일 메모리·손상 | 中 | 스트리밍·검증·R6 대용량 테스트 |
| RR-09 | private Free → main 직접 push 우회 | 中 | soft guard 유지·Hard Protection Migration(docs/32) |
| RR-10 | 기존 268 SmokeTest 약화 | 高 | 삭제/약화 금지 원칙·회귀 게이트 |

## 7. 게이트 (상세: docs/41)
- **Data Spec Gate** (R1·R2): 컬럼매핑·인코딩·조인키·대사 규칙 확정 + 원천합계 대사 PASS.
- **RAG/NCR Approval Gate** (R3): 공개문서 라이선스·출처·버전 승인, 내부원문 미포함, vector/embedding 도입 시 STOP.
- **Model Approval Gate** (R4): 추론 런타임/모델파일/라이선스/무결성/반입 승인 전 Dependency 추가 금지(STOP).
- **Pilot Gate B/C** (R6): Test PC 오프라인 실행 + Excel 2021 검증.

## 8. 진행/핸드백
- WP 단위 실행·상태: `docs/39`(§Resume Brief/원장). 작업 브랜치 `feature/wp-XX-*` → PR → main(squash, `(#N)`).
- Claude↔Codex 루프: Claude(설계/WP/프롬프트) → Codex(WP 구현) → Claude(리뷰·검증·Traceability 갱신) → 다음 WP.

> 관련: `docs/10`(로드맵), `docs/39`(WP), `docs/40`(ADR), `docs/41`(게이트), `docs/32`(거버넌스), `docs/08`/`docs/17`(NCR/RAG)
