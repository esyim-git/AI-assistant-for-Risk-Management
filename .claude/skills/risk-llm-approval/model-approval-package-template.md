# Model Approval Package 템플릿 (ADR-009 필수 항목)

> 추론 Runtime/Vector DB/Embedding/모델파일 도입 **직전** 작성하는 승인 문서다.
> 이 템플릿을 채워 승인받기 전까지 상태는 `MODEL_APPROVAL_REQUIRED`이며 **Dependency 추가 0**.
> 근거: `docs/40 ADR-003·ADR-009`, `docs/41 §3`, `AGENTS.md §4`.

## 작성 원칙 (위반 금지)
- 실데이터·실 테이블/컬럼/시스템명·내부규정 원문·NCR 공식본 원문·secret/key/token·모델파일/가중치를 이 문서에 **넣지 않는다**. 예시는 generic dummy만(`RISK_EXPOSURE_DAILY`, `RISK_LIMIT_MASTER`).
- 외부 다운로드 URL·외부 NuGet 추가 명령을 적지 않는다. 반입 방식은 "오프라인 Model Pack 반입(절차 참조)" 수준으로만 기술.
- 상태 어휘는 `CLAUDE.md §11.4`만 사용. 실 Test PC 증거 없이 Gate PASS로 적지 않는다.

## 0. 메타
| 항목 | 값 |
|---|---|
| 문서 상태 | `MODEL_APPROVAL_REQUIRED` |
| 작성일 / 작성자 | (YYYY-MM-DD) / (역할) |
| 대상 게이트 | `docs/41 §3` (R4 Model Approval Gate) |
| 관련 ADR | ADR-003, ADR-009 |
| STOP 트리거 | (외부 라이브러리 / NuGet / Vector DB / Embedding / Local LLM Runtime / 모델파일 중) |

## 1. 후보 Runtime
- [ ] 후보 추론 Runtime 명칭/버전
- [ ] 오프라인 동작 가능 여부(인터넷 차단 기동)
- [ ] In-process vs **Out-of-process(ADR-003 기본 방향)** + IPC 방식 후보

## 2. 후보 Model
- [ ] 후보 Model 명칭/파라미터 규모/양자화
- [ ] 용도(SQL 보조 / VBA 보조 / 규정답변 / 분석)

## 3. License
- [ ] Runtime License / Model License (재배포·상업 이용 가능 여부)
- [ ] License 의무사항(고지·소스공개 등)

## 4. 배포 크기
- [ ] Runtime 크기 / Model Pack 크기 / 총 반입 크기

## 5. 자원 요구 (RAM/CPU/GPU)
- [ ] 최소/권장 RAM · CPU · GPU(또는 CPU-only 가능 여부)
- [ ] 메모리 한도·크래시 복구 전제(ADR-003 Out-of-process)

## 6. 성능
- [ ] 응답시간(첫 토큰/전체)
- [ ] SQL/VBA **한국어** 보조 품질
- [ ] 규정답변(10단계 형식) 품질
- [ ] **환각률** 측정
- [ ] **인용 준수율** 측정(`docs/17` 인용 블록 기준)

## 7. 보안성
- [ ] 네트워크 격리(외부 호출 0 보장)
- [ ] Output Safety Check 연계(생성물 → MVP-1 Safety Checker, ADR-003)
- [ ] 프로세스 격리/권한 최소화

## 8. 반입 방식
- [ ] 오프라인 Model Pack 반입 절차(외부 다운로드 의존 0)
- [ ] Model Pack 업데이트 방식
- [ ] **App Release ↔ Model Pack 분리 배포**(ADR-003·ADR-009)

## 9. 무결성 (Integrity Hash)
- [ ] Runtime Integrity Hash 검증
- [ ] Model Pack Manifest + Model Integrity Hash 검증(ADR-003 설계 연계)
- [ ] 모델파일/가중치 **repo 미포함** 확인(`model_pack/` gitignored, Prod 적재)

## 10. Fallback / 자동학습
- [ ] **NoModelMode 기본 유지** + NoModel Fallback 경로 보존
- [ ] 모델 가중치 **자동학습 0** 확인

## 11. 승인 전 차단 항목 (체크 시 차단 유지)
- [ ] PackageReference / 외부 라이브러리 추가 = 0
- [ ] Runtime/모델파일 repo 커밋 = 0
- [ ] 외부 다운로드/네트워크 의존 = 0

## 12. 승인 결과
| 항목 | 값 |
|---|---|
| 승인자(문서오너) | |
| 승인 여부 | (대기 / 승인 / 반려) |
| 승인 후 상태 변경 | `MODEL_APPROVAL_REQUIRED` → (승인 시에만) |
| 비고 | |
