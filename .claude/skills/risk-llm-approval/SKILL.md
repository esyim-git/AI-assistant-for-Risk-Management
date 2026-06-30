---
name: risk-llm-approval
description: Design and review Local LLM adapter, Model Pack manifest, out-of-process runtime boundary, integrity checks, and STOP approval conditions.
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git diff *)
---

# Local Model Approval Gate

## 목적
추론 Runtime/Vector DB/Embedding/모델파일 도입 **직전 STOP**을 걸고 **Model Approval Package**를 작성한다. 승인 문서 완료 전까지 의존성·런타임·모델 추가는 **0**으로 유지한다(상태 `MODEL_APPROVAL_REQUIRED`).

## 언제 사용
- **수동 호출 전용** (`/risk-llm-approval`). 자동 적용 안 함(`disable-model-invocation: true`).
- 외부 라이브러리/NuGet · Vector DB · Embedding Runtime · Local LLM Runtime · 모델파일이 **필요해지는 순간** 호출한다.
- `/risk-rag-ncr-governance`가 Vector/Embedding 도입 흔적을 STOP 처리하며 이 스킬로 승인 경로를 안내할 때.
- ADR-003 설계 범위(Adapter/Interface/Manifest/Integrity/NoModel Fallback)를 넘어 실 Runtime 채택을 검토할 때.

## 절대 원칙
- **외부 NuGet PackageReference = 0**. 외부 API/Telemetry/자동 업데이트 = 0. **승인 전 Dependency 추가 0**. (`AGENTS.md §3·§4`, `CLAUDE.md §11.5`)
- STOP 트리거(외부 라이브러리·NuGet·Vector DB·Embedding·Local LLM Runtime·모델파일) 중 하나라도 닿으면 **즉시 구현 중단** → 승인 문서 후에만 진행. (`AGENTS.md §4`, `docs/40 ADR-003·ADR-009`)
- 모델파일·가중치 **repo 미포함**. `model_pack/`은 gitignored, Prod 적재 전용. 모델 가중치 **자동학습 0**. (`docs/41 §3`, `AGENTS.md §3`)
- **NoModelMode가 기본**으로 유지된다. NoModel Fallback을 약화/제거하지 않는다. (`docs/40 ADR-003`)
- 상태는 승인 완료 전까지 **`MODEL_APPROVAL_REQUIRED`**. 상태 어휘는 `CLAUDE.md §11.4`만 사용(과대표기 금지, 실 Test PC 증거 없이 Gate PASS 금지).
- 이 스킬은 **프로세스/문서 가이드**다. 코드 동작을 바꾸지 않으며, 모델/런타임/패키지를 실제로 추가하지 않는다.

## 절차
1. **STOP**: 외부 라이브러리/NuGet/Vector DB/Embedding/Local LLM Runtime/모델파일이 필요해지면 즉시 구현을 중단한다. STOP 사유·트리거 분류·차단 항목은 [stop-rule.md](stop-rule.md) 기준으로 기록한다.
2. **Model Approval Package 작성**: ADR-009 필수 항목(후보 Runtime·후보 Model·License·배포 크기·RAM/CPU/GPU·응답시간·SQL/VBA 한국어 성능·규정답변 성능·환각률·인용 준수율·보안성·반입 방식·Model Pack 업데이트 방식·App↔Model Pack 분리 배포·Runtime/Model Integrity Hash)을 [model-approval-package-template.md](model-approval-package-template.md)로 채운다. 실데이터/실 시스템명/내부규정·NCR 원문/secret/모델파일은 문서에 포함하지 않는다(generic dummy만).
3. **상태 유지·repo 가드**: 상태를 `MODEL_APPROVAL_REQUIRED`로 명시. 모델파일/가중치는 `model_pack/`(gitignored)·Prod 적재 전제이며 repo에 포함하지 않음을 확인한다. ADR-003 설계물(Adapter/Manifest/Integrity/NoModel Fallback/Process Boundary)만 현 단계 범위임을 재확인한다.
4. **승인 전 차단 유지**: 승인 문서가 완료·승인되기 전에는 어떤 의존성/런타임/모델도 추가하지 않는다. 재개 조건은 [stop-rule.md](stop-rule.md) 참조.

## 산출물/보고
- **Model Approval Package 초안**(ADR-009 필수 항목 채움) + **STOP 사유**(트리거 분류) + **승인 전 차단 항목 목록**.
- 보고 한 줄 예: `Model Approval = MODEL_APPROVAL_REQUIRED (STOP: Local LLM Runtime 필요 · Package 초안 작성 · Dependency 추가 0 차단 유지)`.
- 게이트 판정은 하지 않는다(승인은 문서오너·Prod). 상태 어휘는 `CLAUDE.md §11.4`만 사용한다.

## 체크리스트
- ADR-009 승인 문서 필수 항목 템플릿: see [model-approval-package-template.md](model-approval-package-template.md)
- STOP 규칙 트리거·차단 항목·재개 조건: see [stop-rule.md](stop-rule.md)

## 참조
- `docs/40_ADR_Architecture_Evolution.md` (ADR-003 Local LLM Process Boundary·ADR-009 Model Approval Package)
- `docs/41_Approval_and_Pilot_Gates.md §3` (Local LLM / Model Approval Gate, R4)
- `AGENTS.md §4` (STOP 규칙), `AGENTS.md §3` (절대 원칙), `CLAUDE.md §11.4`·`§11.5` (상태 어휘·STOP)
- 연계 스킬: `/risk-rag-ncr-governance` (Vector/Embedding 도입 흔적 → STOP 안내), `/risk-security-guard` (커밋 전 보안 게이트·모델파일/secret 차단)
