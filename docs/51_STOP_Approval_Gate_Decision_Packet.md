# 51. STOP·승인 게이트 결정 패킷 (Decision Packet — R4 LLM · STAB-WP-05 서명 · NCR 실 Pack)

> **목적**: 현재 진행이 **승인 선행(APPROVAL_REQUIRED)·STOP**으로 막혀 있는 3개 게이트를, 문서오너/승인자가 **한곳에서 결정**할 수 있도록 각 게이트의 *결정 요청·권고안·옵션·보안영향·승인 시 해제 범위·승인 전 금지사항*을 정리한 **승인 준비 문서**다.
> **이 문서는 코드 동작을 바꾸지 않으며, 어떤 의존성/모델/런타임/인증서/실 계수도 추가하지 않는다.** 게이트 정의 원본 = `docs/41`(§2·§3·§6), 요건 ADR = `docs/40`(ADR-003·009·012). 본 문서는 그 위에 **결정 가능한 제안**을 얹는다.
> **과대표기 금지(§11.4)**: 실 측정/실 증거가 필요한 항목은 값을 지어내지 않고 `측정 필요(실 Test PC)`/`(확인 필요)`로 남긴다. 승인은 문서오너/Prod가 하며, 본 문서는 판정하지 않는다.
> **기준선**: main `315fd30`(VERSION 0.7.0), 정본 SmokeTest `Total=900`. NEXT UP 결정지점(`docs/39`)의 STOP·승인 게이트 트랙에 대응.

---

## 0. 사용법 (승인자)
1. §A/§B/§C 각 게이트의 **[결정 요청]** 을 읽고 옵션 중 택1(또는 반려).
2. 각 §의 **[승인 결과]** 표에 승인자·일자·결정을 기입.
3. 승인된 게이트는 §D 요약표에 반영 → 그때 Claude가 **후속 구현 WP + Codex 프롬프트**를 작성한다(승인 전에는 작성/구현하지 않음).
4. **승인 전에는 STOP 유지** — 해당 게이트의 "승인 전 금지" 항목을 그대로 지킨다.

> ⚠️ 세 게이트 모두 **부분 승인 가능**(예: STAB-WP-05만 먼저). 서로 의존하지 않는다.

---

## A. R4 — Local LLM Runtime / Model Approval (ADR-003·009 · `docs/41 §3`)

**상태**: `MODEL_APPROVAL_REQUIRED` (STOP). 현재 = NoModelMode·Adapter 계약만(런타임/모델 0).

### A.0 [결정 요청]
> **실 LLM 추론 능력을 도입할지, 도입한다면 어떤 경로로 평가를 시작할지.** 핵심 난점 = LLM 추론 Runtime은 **인박스(System.*)로 불가** → 반드시 외부 Runtime/모델파일 = **STOP 트리거**. 또한 성능·환각률·응답시간 등 ADR-009 필수 측정치는 **모델을 실제 돌려야** 얻어지는데, 그 실행 자체가 승인 대상 → 순환. 이를 **2단계 승인**으로 끊는다.

### A.1 권고 — 2단계 승인
- **1단계(측정용 격리 PoC 승인)**: 오프라인 격리 PC에서 **후보 Runtime 1 + 후보 Model 1~2**만 반입해 ADR-009 §6 성능/환각/인용/자원 **측정치 수집**. 산출물 = 채워진 Model Approval Package(실측). repo/제품 빌드에는 **아무것도 추가하지 않음**(PoC 환경 전용, 모델·런타임 repo 미포함 유지).
- **2단계(운영 채택 승인)**: 1단계 실측 패�지를 근거로 out-of-process Adapter 실구현 WP(LLM-WP-01) 착수 승인. 이때 비로소 STOP 해제(격리·프로세스 경계·무결성·NoModel fallback 보존 전제).
- **권고 이유**: 측정 없이 운영 채택을 승인하면 §11.4(증거 없는 표기) 위반. PoC를 좁게 승인해 근거를 먼저 만든다.

### A.2 후보 옵션 (택1 = 승인 결정; 값은 실측 전까지 미확정)
| 축 | 후보 | 절대원칙 영향 | 비고 |
|---|---|---|---|
| Runtime 형태 | **Out-of-process 별도 프로세스 + IPC**(ADR-003 기본) | 프로세스 격리 유리·크래시 복구 | In-process는 안정성/격리 불리(비권장) |
| Runtime 후보 | GGUF 계열 CPU 추론기 / ONNX Runtime 계열 | **외부 바이너리·라이선스 확인 필요 → STOP** | CPU-only 오프라인 가능성이 선택기준 |
| Model 규모 | 소형(≈3B급) / 중형(≈7~8B급) **양자화(Q4/Q5)** | 반입 크기·RAM 결정 | 사내 PC 사양 상한이 제약 |
| Model License | 재배포·상업이용 허용본만 | **라이선스 의무(고지/제한) 확인 필요** | 제한적 라이선스는 반려 사유 |

### A.3 ADR-009 필수 항목 — **전 항목 채움 대상**(1단계 PoC에서 실측; ADR-009 §결정 전부 반영)
ADR-009가 요구하는 승인 문서 필수 항목을 **하나도 빠뜨리지 않는다**:
1. **후보 Runtime**(명·버전·오프라인 기동 여부·In/Out-of-process+IPC)
2. **후보 Model**(명·파라미터 규모·양자화·용도)
3. **License** — Runtime License / Model License(재배포·상업 이용 가부·고지/소스공개 등 **의무사항**). 제한적 라이선스 = 반려 사유.
4. **배포 크기**(Runtime / Model Pack / 총 반입)
5. **자원 요구** — RAM/CPU/GPU 최소·권장(CPU-only 가부)·메모리 한도·크래시 복구
6. **성능** — 응답시간(첫토큰/전체) · SQL/VBA 한국어 보조 품질 · 규정답변 10단계 품질 · **환각률** · **인용 준수율**
7. **보안성** — 네트워크 격리(외부호출 0) · Output→Safety Checker 연계 · 프로세스 격리/권한 최소화
8. **반입 방식** — 오프라인 Model Pack 반입 절차(외부 다운로드 0)
9. **Model Pack 업데이트 방식**
10. **App Release ↔ Model Pack 분리 배포**(ADR-003·009)
11. **무결성** — Runtime Integrity Hash · Model Pack Manifest + Model Integrity Hash · 모델파일 **repo 미포함** 확인
12. **Fallback/자동학습** — NoModelMode 기본 유지 · 가중치 자동학습 0
> 위 3·6·5의 정량 값(응답시간·환각률·인용준수율·자원)은 모델을 실제 돌려야 나오므로 **전부 `측정 필요(격리 PoC)` — 지금 값 지어내지 않음(§11.4)**. 1·2·3·8·9·10·11은 후보 선정 시점에 기입 가능.
> 템플릿: `.claude/skills/risk-llm-approval/model-approval-package-template.md`(0~12 필드 = ADR-009 필수 항목 1:1 대응) — 1단계 승인 시 이 템플릿을 실측으로 채운다.

### A.4 보안·불변식 영향
- 도입 시에도: 외부 API/Telemetry/AutoUpdate **0** · 네트워크 격리(외부호출 0) · 모델파일·가중치 **repo 미포함**(`model_pack/` gitignored, Prod 적재) · 가중치 **자동학습 0** · **NoModelMode 기본 유지**(fallback 미약화) · 생성물은 기존 Safety Checker 경유(ADR-003 §결정).

### A.5 승인 시 해제 / 승인 전 금지
- **해제(1단계)**: 격리 PoC용 Runtime/Model 반입·측정. **해제(2단계)**: LLM-WP-01 out-of-process Adapter 구현 착수.
- **승인 전 금지(STOP 유지)**: PackageReference/외부 라이브러리 추가 0 · Runtime/모델파일 repo 커밋 0 · 외부 다운로드/네트워크 의존 0 · 제품 빌드에 모델 결합 0.

### A.6 [승인 결과]
| 항목 | 값 |
|---|---|
| 승인자(문서오너) | |
| 1단계(격리 PoC 측정) | (대기 / 승인 / 반려) |
| 2단계(운영 채택) | (대기 — 1단계 실측 후) |
| 상태 변경 | `MODEL_APPROVAL_REQUIRED` → (승인 시에만) |
| 비고 | |

---

## B. STAB-WP-05 — Authenticode 코드 서명 (ADR-012 · `docs/41 §6`)

**상태**: `APPROVAL_REQUIRED` — **인증서 경로 A(사내 Enterprise CA) 이미 확정**(2026-06-30). 남은 것 = §6.2 운영 정책 확정 + 서명 도구 확정. v0.7.0은 미서명+manifest/Fail-Closed로 이미 출하(서명은 후속, 릴리스 차단 아님).

### B.0 [결정 요청]
> 경로 A 위에서 **①서명 도구 ②런타임 검증 정책 ③오프라인 폐기 처리 ④서명 범위 ⑤보관·반입·Rollback**을 확정하면 STAB-WP-05 구현 WP를 착수할 수 있다(실 인증서·서명·검증 증거는 Windows 실 Test PC 의존 → 그 전 VERIFIED 금지).

### B.1 권고안 (경로 A 기준 — docs/41 §6.2 전 항목 반영)
| 항목 | 권고 | 근거 |
|---|---|---|
| **인증서 provenance·신뢰체인·갱신·정책 적합성** (§6.2 ①) | 발급 주체 = **사내 Enterprise CA**(경로 A) · 신뢰 체인 = 사내 도메인 배포 루트 → 사내 PC 한정 신뢰(외부 미신뢰, 본 제품 성격 적합) · **비용 = 0**(사내 CA) · **갱신 주기 = 사내 CA 정책 준수**(만료 전 재발급 SOP) · **사내 코드서명 정책 적합성 = 문서오너/보안팀 확인 필요(확인 필요)** | ADR-012 §결정1·§경로A |
| **서명 도구** | **인박스 `Set-AuthenticodeSignature`(PowerShell)** 1순위 | 추가 설치 0·NuGet 0 유지. `signtool.exe`(Windows SDK)는 별도 설치 필요 → 채택 시 "외부 도구 승인"으로 별도 표기 |
| **서명 검증(런타임)** | 인박스 `System.Security.Cryptography.X509Certificates`(+필요시 `WinVerifyTrust` P/Invoke) | NuGet 0 유지 |
| **서명 범위** | 1차 = 관리 어셈블리(`RiskManagementAI.exe`/`.dll`/`.Core.dll`); self-contained 런타임 DLL(~150개)은 서명 카탈로그(.cat)/배포 정책으로 2차 | ADR-008 §결정5 잔여 ② 닫힘 방식 |
| **런타임 검증 순서** | 서명 검증 PASS를 **manifest 신뢰의 선행 앵커**로 → 미서명/불일치 Fail-Closed | ADR-012 §결정4 |
| **오프라인 폐기(CRL/OCSP)** | 오프라인 = 폐기 온라인 조회 불가 → **정책 명시**(사내 CA 신뢰·타임스탬프 유무 정책화) | ADR-012 §결정4 |
| **보관·반입** | 개인키 = 서명 PC의 Windows 인증서 저장소/HW 토큰 · **repo에 인증서/키 0**(`*.pfx/*.p12/*.pem/*.key` 및 공개 `*.cer/*.crt/*.der` 모두 미포함) | Gate A·§8 |
| **Rollback** | 인증서 만료·교체 시 재서명 절차 + 기존 릴리스 동작 정의 | ADR-012 §결정5 |

### B.2 승인 시 해제 / 승인 전 금지
- **해제**: STAB-WP-05 구현 WP 착수(서명 스크립트·런타임 검증 훅·잔여 3건 회귀 "미탐지→탐지" 전환). 실 인증서·서명·검증 증거 = Windows 실 Test PC(그 전 VERIFIED 금지, BLOCKED 유지).
- **승인 전 금지(STOP 유지)**: 인증서/키 파일 repo 커밋 0 · `signtool` 등 외부 도구 도입 0(인박스 대안 승인 전) · 서명 관련 코드/스크립트 추가 0.

### B.3 잔여 위험 폐쇄 매핑(승인·구현 후 회귀로 고정)
① 콘텐츠 lock-step co-tamper → 서명 검증 실패로 차단 · ② 런타임 DLL 변조 → 카탈로그/게시자 검증(미적용 범위는 OPEN 명시) · ③ 폴더 동반 변조 → 서명 앵커+manifest 교차. 현 STAB-WP-03b는 이 3건을 "미탐지=양성"으로 고정 중 → 서명 도입 시 "탐지/차단" 전환 회귀 PASS 되어야 VERIFIED.

### B.4 [승인 결과]

#### B.4.0 선행 — 인증서 provenance 확보 (**§6.2 정책 승인 전 필수 기입**, ADR-012 §결정1 / docs/41 §6.2 ①)
> 아래 4개 provenance 항목이 **모두 기입/확인되기 전에는 B.4.1 §6.2 정책 승인을 진행하지 않는다**(provenance 미확보 상태의 정책 승인 금지). 값은 승인자가 실제 인증서 기준으로 채운다.

| provenance 항목 | 기입값 | 확인 |
|---|---|---|
| 발급 주체(사내 Enterprise CA 명/인스턴스) | | ☐ |
| 신뢰 체인(루트→중간→코드서명 인증서 thumbprint) | | ☐ |
| 비용 / 갱신 주기(만료일·재발급 SOP) | | ☐ |
| 사내 코드서명 정책 적합성(문서오너/보안팀 확인) | | ☐ |

#### B.4.1 정책 승인 (B.4.0 provenance 확보 후에만)
| 항목 | 값 |
|---|---|
| 승인자(문서오너) | |
| **B.4.0 provenance 4항목 확보 완료?** | (예 / 아니오 — 아니오면 아래 정책 승인 진행 불가) |
| §6.2 정책 승인(도구·검증·폐기·범위·보관·Rollback) | (대기 / 승인 / 반려) |
| 서명 도구 | (`Set-AuthenticodeSignature` / signtool+SDK승인 / 기타) |
| 상태 변경 | `APPROVAL_REQUIRED` → 구현 WP 착수(실 증거 전 VERIFIED 금지) |
| 비고 | |

---

## C. NCR 실 Rule Pack — 실 계수/공식본 적재 (ADR-013 계열 · `docs/41 §2` · risk-rag-ncr-governance)

**상태**: `APPROVAL_REQUIRED`. 현재 = Rule Set **8요소 구조(SCAFFOLD_ONLY)** + 샘플 placeholder(`APPROVAL_REQUIRED_NO_REAL_COEFFICIENT`, 실 계수 0). NCR 공식본 원문·실 계수는 **repo 미포함**.

### C.0 [결정 요청]
> 실 NCR 계수/공식본을 **어디에·어떻게** 적재하고(권한통제 KB, repo 미포함), **누가 승인**하며, 로딩 시 어떤 안전 게이트를 강제할지 확정. 승인해도 **원문/실 계수는 repo에 들어오지 않는다**(Prod 권한통제 KB 전용).

### C.1 권고 — 적재·승인 SOP
1. **위치**: 실 Rule Pack = Prod 권한통제 KB(오프라인, 역할권한·조회로그·보안등급). repo·release ZIP **미포함**.
2. **원문 미포함 = 다층 방어(단일 스캔 과대의존 금지)**: 어느 한 스캔도 단독으로 유입 0을 "보장"하지 않는다 — 계층으로 막는다. **① 1차 = 프로세스**: NCR 공식본/실 계수는 Dev repo에 애초에 반입하지 않는다(문서오너 관할, Prod 권한통제 KB 전용). **② 커밋 게이트 A**(`docs/28`): secret/원문/실데이터 스캔. **③ `KbRepositoryGuard`**(SmokeTest/CI 런타임 — `kb/`·`data_sources/`·`samples/`·`config/ncr` 파일명/내용 Blocker). **④ `build/03` 릴리스 패키징 ZIP 추출 스캔**(동일 토큰 mirror + SmokeTest drift guard). ③④는 **자동 백스톱**이지 1차 통제가 아니다. **⑤ `.gitignore`**(원문/Pack 경로). — build/03 하나에 기대지 않고 ①~⑤ 병행.
3. **승인 주체·계약 필수 항목**: 준법/리스크관리 문서오너 승인. 적재본은 `docs/39`의 **NCR-WP-01 Approved NCR Rule Pack Contract**를 빠짐없이 만족해야 한다: **Rule Set ID/Version/Effective/Expiry**, Component Map, Formula Definition, **Coefficient/Unit/Sign/Rounding/`ValuePolicy`**, Regulation Basis, 조회전용 Validation SQL, Approval History, **Pack Hash/`file_hash`**, **Reviewer/Approval Owner**, **Rollback/대체 Pack**. 추가 적재 메타는 문서ID·문서명·출처기관·출처 locator·버전·시행일·폐기일·적재일·승인상태·대체문서·**라이선스 상태**·보안등급·역할권한을 포함한다. **계수 출처·근거조항·승인이력**까지 명시된 본만 승인하며, 항목 누락 시 적재 반려(과대표기 금지).
4. **로딩 게이트(코드측, 이미 구조 존재)**: Rule Pack 부재/승인 항목 누락 시 계산 차단(SCAFFOLD_ONLY 유지)·Validation SQL 조회전용(`SqlSafetyChecker`)·자동실행 0·답변은 **검토용 초안** 표기(10단계 형식, `docs/10`/§10). 실 계수 적재 후에도 산정 결과는 "검토용 초안"이며 공식 산정 아님.
5. **repo 상태 불변**: repo는 **스키마+`(확인 필요)` 경고**까지만(현행). `file_hash`·실 version·시행일·계수는 Prod 권한통제 KB 적재 시에만 채워지고 repo 상태는 바뀌지 않는다.

### C.2 승인 시 해제 / 승인 전 금지
- **해제**: Prod 권한통제 KB에 실 Rule Pack 적재 절차 개시(문서오너 관할). 필요 시 로더/게이트 보강 WP(구조만, 실 계수 repo 미포함).
- **승인 전 금지(STOP/불변식 유지)**: 실 NCR 계수·공식본 원문 repo 커밋 0 · release ZIP 유입 0 · placeholder→실값 하드코딩 0 · 산정결과를 "공식/확정"으로 표기 0(검토용 초안 유지).

### C.3 [승인 결과]
| 항목 | 값 |
|---|---|
| 승인자(준법/리스크 문서오너) | |
| 적재 SOP 승인 | (대기 / 승인 / 반려) |
| 적재 위치 | (Prod 권한통제 KB — repo 미포함 확인) |
| **NCR-WP-01 계약 필수항목 완비?** | (예 / 아니오 — 아니오면 실 Rule Pack 승인 불가) |
| 상태 변경 | SCAFFOLD_ONLY → (Prod 적재 시 권한통제 KB에서만; repo 상태 불변) |
| 비고 | |

---

## D. 승인 요약 (한눈에 — 승인자 기입)
| 게이트 | 현재 | 결정 요청 요지 | 권고 | 승인 |
|---|---|---|---|---|
| **A. R4 LLM** | MODEL_APPROVAL_REQUIRED (STOP) | 실 추론 도입/평가 경로 | 2단계(격리 PoC 측정 → 운영 채택) | ☐ 대기 |
| **B. STAB-WP-05 서명** | APPROVAL_REQUIRED (경로 A 확정) | **provenance 확보(B.4.0) → §6.2 정책·서명도구 확정** | 인박스 `Set-AuthenticodeSignature`+선행 검증 앵커 | ☐ 대기 |
| **C. NCR 실 Pack** | APPROVAL_REQUIRED (SCAFFOLD_ONLY) | 적재·승인 SOP | Prod 권한통제 KB·repo 미포함·검토용 초안 유지 | ☐ 대기 |

> **승인 후**: 해당 §의 상태를 갱신하고 Claude에게 알리면, Claude가 **후속 구현 WP + Codex 프롬프트**를 작성한다(승인 전에는 작성하지 않음). 실 Test PC/Prod 증거가 필요한 항목은 그 증거 전까지 **VERIFIED/PASS 금지**(§11.4).

> 관련: `docs/40`(ADR-003·009·012·013)·`docs/41`(§2 RAG/NCR·§3 Model·§6 Code-Signing Gate)·`docs/39`(NEXT UP 결정지점)·`docs/48`(Gate B/C 증거)·`.claude/skills/`(risk-llm-approval·risk-rag-ncr-governance)·`CLAUDE.md §3·§11.4·§11.5`.
