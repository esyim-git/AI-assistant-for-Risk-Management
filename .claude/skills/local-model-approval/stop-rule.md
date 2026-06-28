# STOP 규칙 — 트리거 · 차단 항목 · 재개 조건

> 근거: `AGENTS.md §4` (STOP 규칙), `docs/40 ADR-003·ADR-009`, `docs/41 §3`, `CLAUDE.md §11.5`.
> 이 문서는 프로세스 가이드다. 코드/패키지/모델을 실제로 추가하지 않는다.

## 1. STOP 트리거 (하나라도 닿으면 즉시 구현 중단)
다음이 **필요해지는 순간** STOP하고 [model-approval-package-template.md](model-approval-package-template.md)를 작성한다:

- 외부 라이브러리 / **외부 NuGet PackageReference**(추가 = 0 원칙)
- **Vector DB** 도입
- **Embedding** Runtime
- **Local LLM Runtime**(추론 라이브러리)
- **모델파일 / 가중치**

> 참고: 검색 엔진은 **Keyword/Inverted Index(in-box, NuGet 0)**로 먼저 완성한다(ADR-007). Vector/Embedding 필요 시 여기로.

## 2. STOP 시 즉시 차단 항목 (승인 전까지 0 유지)
- [ ] 어떤 PackageReference도 추가하지 않는다(외부 NuGet = 0).
- [ ] 추론 Runtime/라이브러리를 빌드/참조에 넣지 않는다.
- [ ] 모델파일/가중치를 repo에 커밋하지 않는다(`model_pack/` gitignored, Prod 적재).
- [ ] 외부 다운로드/네트워크 의존(URL·패키지 복원)을 코드/문서/스크립트에 넣지 않는다.
- [ ] NoModelMode 기본·NoModel Fallback을 약화/제거하지 않는다.
- [ ] 상태를 `MODEL_APPROVAL_REQUIRED`로 유지한다(`CLAUDE.md §11.4` 어휘만).

## 3. STOP 사유 기록 양식
```text
STOP 사유 | 트리거(위 §1 분류) | 영향 범위 | 대안 검토(in-box 가능성) | 다음 행동(Approval Package 작성) | 일시 | 작성자
```

## 4. 현 단계 허용 범위 (STOP 없이 가능 — ADR-003 설계 전용)
런타임 없이 **설계/계약만** 진행 가능:
- `ILocalModelProvider` / `NoModelProvider`(기본·유지) / `ModelProviderFactory`
- `ModelAvailability` / `ModelHealthCheck` / `ModelRequest`·`ModelResponse`
- Timeout/Cancellation, 입력·출력 길이 제한, Output Safety Check 연계
- **Model Pack Manifest** + **Integrity Hash 검증 설계**
- **Process Boundary**: Out-of-process(별도 프로세스 + 로컬 IPC) 기본 방향

> 이 범위를 벗어나 **실 Runtime/모델 채택**이 필요하면 §1 STOP.

## 5. 재개 조건 (모두 충족 시에만)
- [ ] Model Approval Package(ADR-009 필수 항목 전부) 작성 완료
- [ ] 문서오너 **승인** 획득(반입 방식·License·보안·무결성 포함)
- [ ] App Release ↔ Model Pack 분리 배포 전제 확인
- [ ] 승인 후에만 상태 전환(`MODEL_APPROVAL_REQUIRED` → 승인 단계)
- [ ] 실 Test PC 오프라인 증거는 별도 Gate B/C(`docs/41 §4`)에서 확인(과대표기 금지)

> 위 조건 미충족 시: 상태 `MODEL_APPROVAL_REQUIRED` 유지, Dependency/런타임/모델 추가 0.
