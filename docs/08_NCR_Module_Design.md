# NCR Module Design

## 목적

NCR 산정과 관련된 데이터 매핑, 산식 버전, 검증 SQL, 예외사항을 관리한다.

## 초기 범위

- NCR 문서 placeholder
- 산식 버전 테이블 설계
- 구성요소 매핑 테이블 설계
- 검증 SQL 템플릿
- Excel 검증 리포트 구조

## 제외 범위

- NCR 최종 판단 자동화
- 감독기관 보고 자동 생성
- 내부 승인 없는 산식 적용

## 답변 구조

1. 적용 기준일
2. 산식 버전
3. 필요 데이터
4. 구성요소 매핑
5. 검증 SQL
6. 산출 결과 대사
7. Hidden Risk
8. 승인 필요사항

---

## (심화 R3) NCR Rule Set 구조 — 모델 산식 암기 금지

> **원칙: 모델이 NCR 산식을 "기억"해서 답하는 구조를 금지한다.** NCR은 아래 명시적 구조(데이터·규칙)로만 산출·설명하며, 답변은 항상 검토용 초안이다. 적재/승인 게이트: `docs/41 §2`.

| 요소 | 설명 |
|---|---|
| **NCR Rule Set** | NCR 산정 규칙 집합(구성요소·계수·분류) |
| **Rule Set Version** | 규칙셋 버전 식별자 |
| **Effective Date** | 적용 시행일 |
| **Component Map** | NCR 구성요소 ↔ 데이터 컬럼/소스 매핑 |
| **Formula Description** | 산식의 **서술적** 정의(코드/데이터로 검증 가능, 모델 암기 아님) |
| **Validation SQL Template** | 구성요소·산출 결과 대사용 조회 전용 SQL 템플릿 |
| **Regulation Basis** | 근거 규정/조항(출처) |
| **Approval History** | 규칙셋 승인 이력(검토자·일시·버전) |

- 내부규정·NCR 공식본 **원문은 Repository 미포함** — Prod 권한통제 KB로만(`docs/17` 심화, `CLAUDE.md §10`).
- 산출은 Validation SQL/대사로 **재현·감사 가능**해야 한다.

**Codex 결과(2026-06-21, R3-WP-05)**: `Core/Ncr/NcrRuleSet` 8요소 구조와 `NcrRuleSetLoader`를 추가했다. 로더는 `config/ncr/*.json`만 허용하며 누락/손상/위험 SQL은 safe fallback으로 처리한다. `config/ncr/ncr_ruleset_sample.json`은 placeholder 구조만 포함하고, Validation SQL 템플릿은 `SqlSafetyChecker`로 조회 전용을 검증한다. `NcrExplain`은 구조에서만 설명을 만들고 항상 **검토용 초안**을 명시한다. SmokeTest 460 PASS.

> 관련: `docs/17_KB_RAG_Design.md`, `docs/18_NCR_Regulation_Module_Guide.md`, `docs/41`(RAG/NCR 게이트).
