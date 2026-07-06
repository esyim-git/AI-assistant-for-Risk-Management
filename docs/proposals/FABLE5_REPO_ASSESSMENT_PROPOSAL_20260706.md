# Risk Management AI Assistant — Repository Assessment and Success Proposal

> **작성**: 2026-07-06, Fable 5 (Architecture Lead / Program Reviewer 역할 수행)
> **성격**: 검토용 제안서(Proposal) — 코드 동작을 바꾸지 않으며, 의존성·모델·런타임·실 계수를 추가하지 않는다. 본 문서는 판정(승인)이 아니라 **평가 + 제안**이다. 상태 어휘는 `CLAUDE.md §11.4` 정본(VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED)만 사용한다.
> **기준선**: main `44d1be1`(#130 머지 후) · 코드/테스트 baseline `7094d91`(#127 QA-WP-09; #128~#130은 docs-only라 baseline 불변 — drift 아님) · VERSION `0.7.0` · v0.7.0 태그 `30c1cfb`(미서명, ZIP SHA256 `42C835…E09DD5`) · 직전 v0.6.0 태그 `3dfa80b` · 정본 SmokeTest **`Total=900 PASS=900 FAIL=0`**(local-gate) · 열린 PR 0.
> **최종 상태 판정: `READY_FOR_GATE_BC`** (+ 병행으로 v0.8 안전 구현 트랙 착수 가능 — §14)

---

## 1. Executive Summary

### 1.1 현재 평가 (Current Assessment)

이 프로젝트는 "금융회사 리스크관리용 Local AI Assistant"라는 목표에 대해 **비정상적으로 높은 거버넌스 성숙도와 견고한 코드 기반**을 갖추고 있다. 흔한 실패 패턴(과대표기, 테스트 없는 기능 누적, 의존성 폭증, 문서-코드 괴리)을 방지하는 장치가 이미 **코드와 테스트로 강제**되어 있다:

- **외부 NuGet 0**이 선언이 아니라 실측: 3개 csproj 전부 `PackageReference` 0 + `NuGet.Config` 소스 `<clear/>` + SmokeTest가 csproj 텍스트를 검사해 회귀 차단.
- **SmokeTest 900개**(14개 스위트/15 도메인 분류, PASS 900/FAIL 0 — 합계는 하드코딩이 아닌 런타임 집계이며 Unclassified 발생 시 실패, 이력 추적 가능: 484→…→714→747→…→900 WP별 증분 기록).
- **릴리스 무결성 체인**: build/01 manifest 생성 → build/03 ZIP 추출 검증 → 런타임 `IntegrityVerifier` Fail-Closed 기동 게이트(앱 시작 전 실행, 변조/부재 시 종료코드 2) — 잔여 한계(lock-step co-tamper)까지 코드 주석·테스트로 **정직하게 문서화**.
- **4회 공개 릴리스 실적**(v0.3.0/v0.4.0/v0.6.0/v0.7.0, 각각 SHA256 공표) — GitHub Releases에서 검증 완료.
- **과대표기 금지 규율이 실제로 작동**: 2026-06-30 사용자 수동 Gate B 검증에서 다수 항목이 통과했음에도 증거 미첨부라는 이유로 `user-reported`로만 기록하고 전체 BLOCKED를 유지(docs/48 §B′) — 이 규율 자체가 이 프로젝트의 최대 자산이다.

반면, "사용자가 체감하는 제품"과 "리포지토리의 코드 수준 완성도" 사이에 **3개의 간극**이 있다: ① 실 오프라인 Test PC 증거(Gate B/C)가 한 번도 봉인된 적 없음, ② 출하된 v0.7.0 ZIP(`30c1cfb`)에는 이후 머지된 UX-WP-04~11(Excel Function Helper·Smart Assist as-you-type)이 **미포함** — 사용자 체감 기능과 출하본의 괴리, ③ KB/NCR은 구조(스키마·검색·가드)는 VERIFIED이나 **콘텐츠가 합성 샘플뿐** — "규정 검색 보조"의 실사용 가치는 콘텐츠 적재(승인 게이트) 후에 발생.

### 1.2 성공 가능성 (Likelihood of Success)

**높음 (조건부)** — 판단 근거:

| 관점 | 평가 |
|---|---|
| 기술 실행력 | **높음** — R1(데이터/한도/대사)·R2(분석/시각화)·R3(KB 구조)·R5(피드백 검색) 전 트랙이 local-gate VERIFIED, 회귀 0 유지. Claude 설계→Codex 구현→Claude 4축 리뷰 루프가 #56~#127에서 반복 검증됨 |
| 보안/컴플라이언스 설계 | **높음** — 원문/실데이터/모델/비밀정보 유입 차단이 다층(프로세스·Gate A·KbRepositoryGuard·build/03·gitignore)으로 구현·테스트됨. 본 검토에서도 repo 스캔 결과 모델파일·인증서·대용량 파일 0 확인 |
| 제품-업무 적합성 | **높음** — 조회전용 SQL 보조·한도 모니터링·대사·전일대비·Excel 리포트는 리스크관리 실무의 반복 작업과 정확히 일치. "자동 실행 없는 검토용 초안" 포지셔닝은 금융회사 내부통제와 정합 |
| 남은 리스크 | **중간** — 실 Test PC 증거 부재(전 기능 공통), 출하본-main 기능 괴리, 콘텐츠(KB/NCR) 미적재, UI 구조 부채(MainWindow 1,614줄), 승인 게이트 3건 대기 |

성공의 정의를 "v1.0 Team Pilot에서 실무자가 실제 업무에 쓰고, 감사 가능한 이력이 남으며, 보안 사고 0"으로 잡으면, **현재 코드 기반으로 도달 가능**하다. 실패 시나리오는 기술이 아니라 **운영 결정의 지연**(Gate B/C 미실행, 승인 게이트 방치)과 **콘텐츠 공백**(구조만 있고 규정 콘텐츠 없음)에서 온다.

### 1.3 최대 강점 (Biggest Strengths)

1. **거버넌스가 코드로 강제됨** — 과대표기 금지·상태 어휘·STOP 규칙·truth-sync가 문서 관습이 아니라 테스트/가드/체크리스트(Skills 15종)로 실행된다.
2. **오프라인/무의존 원칙의 일관 구현** — CP949 디코더·XLSX 리더/라이터·차트 렌더까지 인박스 구현. 운영망 반입 리스크 표면적이 구조적으로 작다.
3. **감사 가능성(Auditability)** — 해시 전용 audit log(원문 미저장), JoinAudit, 승인형 피드백(RETRIEVAL, 학습 아님), Fail-Closed 무결성 — 금융권 내부통제 언어로 그대로 설명 가능한 설계.
4. **재현 가능한 릴리스 파이프라인** — VERSION 단일원천 락스텝, build/00~03, SHA256, 미서명 고지까지 4회 릴리스로 검증된 절차.

### 1.4 최대 리스크 (Biggest Risks)

1. **Gate B/C 증거 공백** — 모든 VERIFIED가 local-gate 한정. 실 오프라인 Test PC에서 전 기능이 봉인된 적이 없다(부분 user-reported만 존재). 파일럿 전제조건이자 유일하게 **사용자만 할 수 있는** 작업.
2. **출하본 괴리** — 사용자가 받는 v0.7.0 ZIP에 UX-WP-04~11 미포함. B-5(검사 UX)가 PARTIAL로 남은 직접 원인. v0.7.1 릴리스 컷 전까지 "체감 개선"이 전달되지 않는다.
3. **콘텐츠 공백** — KB clause pack은 합성 더미, NCR은 placeholder 계수. 구조 VERIFIED ≠ 실사용 가치. 승인 게이트(문서오너) 결정 없이는 여기서 정체된다.
4. **UI 구조 부채** — `MainWindow.xaml.cs` 1,614줄 God-class(12개 탭 전 로직 + 차트 렌더 + 인라인 DTO), MVVM/DI 0. 다음 UI 트랙 착수 전 분해하지 않으면 변경 비용이 누적 증가.
5. **테스트 신뢰의 상한** — 900개 SmokeTest는 커스텀 하니스·in-process·UI는 텍스트 계약 검사. 실 WPF 렌더/포커스/Excel 열기는 검증 불가 — Gate B/C가 유일한 보완이며, 그래서 1번 리스크와 결합 시 위험이 커진다.

### 1.5 Top 3 다음 우선순위

1. **Gate B/C 증거 라운드 1 실행(user-driven)** — docs/48 §B″ 턴키 런북 그대로: published ZIP으로 B-6·B-8 수행 + 기존 user-reported 항목 증거 봉인(`evidence/gateB/`). 소요 반나절, 승인 불요, 파일럿 경로의 병목 해소.
2. **v0.7.1 릴리스 컷(REL WP)** — **current main(`44d1be1`) 기준으로 컷을 수행하되, binary-impact 기준선은 `7094d91`로 본다**(#128~#130은 docs-only → 바이너리 동일). 목적은 신규 기능 구현이 아니라 main에 이미 구현된 UX-WP-04~11을 실제 출하본에 반영하는 것 → B-5 재검증 → 출하본-main 괴리 해소. Codex 즉시 실행 가능(기능 변경 0·STOP 비접촉).
3. **승인 게이트 3건 결정 회신(docs/51)** — A(R4 LLM 2단계 PoC), B(STAB-WP-05 서명 §6.2+provenance), C(NCR 실 Pack SOP). 결정 자체가 다음 분기(v0.9~v1.1) 스코프를 확정한다. 특히 B는 경로 A 확정으로 절반 진행된 상태.

---

## 2. Current State Assessment

### 2.1 사실 분류 (Verified / Assumed / Needs User Evidence)

**Verified (본 검토에서 직접 확인):**

| 항목 | 값 | 확인 방법 |
|---|---|---|
| main HEAD | `44d1be1` (#130 docs/51 STOP 결정 패킷) | `git log` |
| 코드/테스트 baseline | `7094d91` (#127 QA-WP-09) — 이후 #128(`315fd30`)·#129(`73d41c5`)·#130(`44d1be1`)은 docs-only. **관례: 문서 전용 머지는 코드/테스트 baseline SHA를 올리지 않는다**(따라서 문서의 `7094d91` 표기는 drift 아님). Current main과 baseline은 항상 구분 표기한다 | `git log` 커밋 내용 |
| VERSION | `0.7.0` | `VERSION` 파일 |
| v0.7.0 태그 | `30c1cfb3129…`(2026-06-30 발행, 미서명 고지 포함, ZIP SHA256 `42C835…E09DD5`) | GitHub Releases/Tags API |
| v0.6.0 태그 | `3dfa80b`(2026-06-22), v0.4.0/v0.3.0도 발행 이력 존재 | GitHub Releases/Tags API |
| 정본 SmokeTest | `Total=900 PASS=900 FAIL=0`, 14개 테스트 도메인 파일, WP별 증분 이력 문서화 | docs/38 §0·README·tests/ 트리 (수치 자체는 문서 정합 — 재실행은 Windows 로컬 게이트) |
| 외부 NuGet 0 | 3개 csproj `PackageReference` 0 + NuGet.Config `<clear/>` + 테스트 강제 | csproj/NuGet.Config/ReportTests 직접 확인 |
| repo 보안 상태 | 모델파일(*.gguf/onnx/safetensors/pt)·인증서/키(*.pfx/pem/key/cer)·5MB 초과 파일 **0**; kb/=공개 catalog+합성 clause 샘플+NCR placeholder만 | 본 검토 파일시스템 스캔 |
| 열린 PR | 0 (#130까지 전부 머지) | GitHub API |
| Gate B/C 상태 | **BLOCKED** — 2026-06-30 부분 user-reported(B-1~4·7·9·10 PASS 보고·증거 미첨부, B-5 PARTIAL, B-6/B-8 PENDING) | docs/48 §B′ |
| 승인 게이트 | STAB-WP-05: 인증서 경로 A(사내 CA) 확정, §6.2·provenance 대기 · R4 LLM: MODEL_APPROVAL_REQUIRED · NCR 실 Pack: APPROVAL_REQUIRED | docs/51·docs/41 |
| NEXT UP | **방향 결정 대기(decision point)** — 후보 3택 (a)신규 기능 트랙 (b)STOP·승인 게이트 (c)Gate B/C | docs/38 §0·docs/50 |

**Assumed (문서 정합으로만 확인, 본 환경에서 재실행 불가):**

- SmokeTest 900 PASS의 **실행 결과 자체** — Linux 컨테이너에서는 WPF 포함 빌드/실행 불가(local-gate 모델상 Windows 로컬 실행이 정본). 문서·PR 이력·테스트 파일 구조와 정합.
- v0.7.0 ZIP 내부 구성 — build 스크립트와 릴리스 노트로 추정, ZIP 자체는 본 환경에 없음(external artifact).

**Needs User Evidence (사용자/실 PC만 생성 가능):**

- Gate B/C 전 항목의 봉인 증거(`Get-FileHash` 출력·스크린샷·기동 로그 → `evidence/gateB/`).
- 사내 반입 정책상 미서명 ZIP 허용 여부(Gate C C5의 ACCEPTED_RISK 수용 여부).
- 승인 게이트 3건의 문서오너 결정(docs/51 §A/§B/§C 기입).

### 2.2 기능 완성 수준 요약

- **R1 Data & Limit Foundation (v0.5.0)**: VERIFIED — CP949/UTF-8/XLSX 입력, Column Mapping, Exposure-Limit Join 7상태(DUPLICATE_LIMIT 포함), 대사 9종, Dashboard=Report 일원화.
- **R3 Regulation/NCR 구조 (v0.6.0)**: VERIFIED(구조) — 공개 catalog 메타 검색·인용·KbAccessPolicy·KbRepositoryGuard. NCR Rule Set은 SCAFFOLD_ONLY.
- **STAB (v0.6.1)**: PARTIAL — 재현성·무결성·정본 테스트 VERIFIED, 코드서명(STAB-WP-05)만 APPROVAL_REQUIRED.
- **R2 Risk Analytics (v0.7.0)**: VERIFIED — Semantic Hardening·Streaming 프로파일(전 값 미보관, 상한 200k행/50MB, Outlier 2-pass parity — dead Welford 필드는 R2-WP-05 #109에서 제거)·Prior-Day·RISK_VISUAL 시각화. 단 streaming 대용량 경로와 Prior-Day는 **WPF UI 미노출**(local-gate 전용, docs/48 B7/B8).
- **UX/Assist + KB clause + Feedback (post-v0.7.0, main에만 존재)**: VERIFIED(local-gate) — Excel Function Helper, as-you-type Smart Assist, clause keyword 검색, 승인 Example 검색·review 경유 반영. **출하본에는 미포함.**
- **QA 하드닝 스윕(QA-WP-01~09)**: DONE — 인박스 테스트 도메인 하드닝 완결, Total 900 도달.
- **Gate B/C·Team Pilot**: BLOCKED — 실 Test PC 증거 대기.
- **Local LLM·NCR 실 계수·내부규정 KB·코드서명**: APPROVAL_REQUIRED/STOP — 구현 가능하되 승인 선행("불가능"이 아니라 "승인 후 구현 가능").

---

## 3. Product Vision

### 3.1 최종 제품상 (Final Product Image)

**"운영망 PC에서 ZIP 하나로 실행되는, 사람이 검토하는 리스크관리 Copilot."**

리스크관리 담당자가 아침에 Golden6에서 export한 CSV/XLSX를 던지면 — ① 데이터 품질을 프로파일링하고(Null/중복/기준일/인코딩), ② 노출-한도를 결합해 7상태·대사 9종·전일대비 변동을 계산하고, ③ RISK_VISUAL 포함 Excel 2021 리포트를 생성하고, ④ SQL/VBA를 안전검사(차단 14종 DML/DDL 등)하고 검증 스니펫·8단계 템플릿으로 작성을 보조하고(자동 실행 0 — **실 초안 생성은 LLM 승인 후**, 현재는 검사·스니펫·템플릿), ⑤ 규정/NCR 질의에 조항 인용과 "검토 필요" 표식이 붙은 초안으로 답하고(10단계 포맷), ⑥ 이 모든 작업 이력을 해시 audit로 남긴다. LLM이 승인되면 같은 파이프라인 위에서 초안 품질이 올라가고(out-of-process, NoModel fallback 유지), 승인되지 않아도 rule/template/retrieval 기반으로 **오늘 그대로 유효**하다.

### 3.2 사용 목적 · 사용자 페르소나

| 페르소나 | 핵심 과업 | 제품이 주는 것 |
|---|---|---|
| 시장리스크 담당자 (한도 모니터링) | 일일 한도 사용률·소진·위반 점검, 전일 대비 변동 파악 | Exposure-Limit Join 7상태, 대사 9종, Prior-Day movers, Dashboard=Report 동일 수치 |
| 데이터/리포트 담당자 | Golden6 export 정제·Excel 보고서 작성 | DataProfile(인코딩/Null/중복/기준일), RISK_VISUAL 자동 생성, Formula Injection 0 보장 리포트 |
| SQL/VBA 작성자 (주니어 포함) | 조회 SQL·Excel 2021 VBA 작성과 검증 | 8단계 SQL 포맷 초안, Safety Checker(차단 DML·금지 API), Excel 2021 함수 대체식, Smart Assist |
| NCR/규정 담당자 | 규정 근거 확인, NCR 산정 검증 보조 | 공개 규정 catalog/조항 인용 검색(검토용 초안 명시), NCR Rule Set 구조 설명 — 실 계수는 승인 후 |
| 감사/내부통제 관점 이해관계자 | "AI가 뭘 했는지" 추적 | 해시 전용 audit trail, 승인형 피드백 이력, 무결성 manifest·Fail-Closed |

### 3.3 Team Pilot 시나리오 (요지 — 상세 §11)

리스크관리팀 2~5인이 4~6주간, 실제 아침 업무(export → 프로파일 → 한도분석 → 리포트) 중 **병행 사용**(기존 절차 유지 + 본 도구로 이중 수행)으로 시작해, 수치 일치가 확인되면 도구 산출물을 1차본으로 승격. 규정 검색·SQL 초안은 처음부터 "검토용 초안" 용도로 자유 사용. 주간 피드백 수집(인앱 Feedback + 양식), KPI는 §11.7.

### 3.4 사내 업무혁신 포지셔닝

- **"자동화"가 아니라 "표준화+검증+감사가능성"** 스토리로 포지셔닝한다 — 금융회사에서 "AI가 알아서 실행"은 승인 장벽이지만, "사람 검토 전제의 초안·검증·근거 제공 + 전 이력 감사 가능"은 내부통제 강화 스토리다.
- 차별점: 외부 API 0·오프라인·미서명조차 고지하는 무결성 체계 — **보안팀이 가장 좋아할 형태의 AI 도입 사례**. 사내 AI 거버넌스 선례(모델 승인 게이트, STOP 규율)로 재사용 가능.
- 정량 스토리는 §13.

---

## 4. Architecture Assessment

*(§4는 본 검토의 아키텍처 분석 에이전트 실측 — 파일 경로·라인 수는 baseline `7094d91` 기준.)*

### 4.1 현재 아키텍처

```text
RiskManagementAI.sln
├─ src/RiskManagementAI.App   (net8.0-windows, WPF, WinExe)  — 2,380줄
│   ├─ App.xaml.cs            : 기동 무결성 게이트(Fail-Closed) + 정책 로드 → static 노출
│   ├─ MainWindow.xaml(.cs)   : 12개 탭 전체 UI 로직 (xaml 517 + cs 1,614줄)
│   └─ Controls/CompletionPopup : 유일한 분리 컨트롤
├─ src/RiskManagementAI.Core  (net8.0, 의존성 0)             — ~10,134줄 / ~73파일
│   Assist(정적 완성엔진) · Config(정책/레이아웃) · Dashboard · Data(CSV/XLSX/CP949/Profiler)
│   Excel(함수 헬퍼/체커) · Feedback(승인 Example) · Generation(Draft 파이프라인/NoModel)
│   Integrity(manifest 검증) · Kb(catalog/clause/가드) · Logging(해시 audit) · Mapping
│   Ncr(Rule Set 구조) · Report(XLSX 빌더/시각화 집계) · Risk(LimitMonitor/PriorDay) · Safety(SQL/VBA 룰)
└─ tests/RiskManagementAI.SmokeTests (net8.0 콘솔, Core만 참조) — 커스텀 하니스, 900 tests/14 도메인
```

- **의존 방향 청정**: App→Core 단방향, Core는 무참조, 순환 0. 테스트는 App을 코드 참조하지 않고 **텍스트 픽스처**로 검사(WPF TFM 회피).
- **모델 심(seam)**: `ILocalDraftService`(Generation) — 유일 구현 `NoModelDraftService`(항상 IsAvailable=false, Mode="NoModelMode", 정책 위반 findings 동봉). `DraftPipeline`이 provider-불문 안전검사·audit를 수행. **미래 LLM은 이 인터페이스 구현 1개 교체로 연결**(현재 `MainWindow` ctor의 `new` 1곳). `model_pack/`은 문서 전용(코드 참조 0).
- **상태 저장**: 전부 로컬 파일(config JSON·rules txt·kb csv·templates·logs JSONL) — DB/레지스트리 0. `ui_layout.local.json`은 경로 가드+clamp+손상 fallback까지 하드닝.
- **무결성**: `IntegrityVerifier`(494줄)가 build/03 §4를 런타임에 미러링(락스텝 테스트로 고정), 기동 전 실행, 실패 시 Exit(2). Dev 우회는 env var+디버거 동시 요구. 잔여 한계(lock-step co-tamper·런타임 DLL 미해시)는 서명(STAB-WP-05)으로만 폐쇄 — 코드·테스트에 명시.

### 4.2 강점

1. **Core의 도메인 분해가 양호** — 15개 하위 도메인이 역할별로 분리, 계약(record/interface) 중심, 결정성(Ordinal 정렬·주입 IClock) 관철.
2. **의존성 0의 리스크를 정면 처리** — CP949(UHC 확장 포함 17,236 entries byte-stable), XlsxReader(XXE 차단·zip 상한), ExcelReportBuilder(정적값 시트로 수식 게이트 리스크 0) 등 "라이브러리 없음"의 비용을 테스트로 상쇄.
3. **Fail-Closed 우선 설계** — 무결성·정책·Rule Pack 부재 시 차단이 기본값. 금융권 반입 심사에 유리.
4. **모델 통합의 사전 설계** — NoModelMode가 UI(대시보드/설정/배너)까지 일관 표기되어, LLM 부재가 "고장"이 아니라 "모드"로 처리됨.
5. **프롬프트 주입 방어의 선제 구현** — `DraftReferenceComposer`가 승인 Example을 컨텍스트에 반영할 때 fencing(라인 프리픽스·개행 정규화·상한 5건/2,000자)으로 지시문형 텍스트("Ignore previous instructions" 류)를 데이터로 중화 — LLM 도입 전인데 이미 테스트로 고정. R4 시 그대로 재사용 가능.
6. **결정 이력의 추적 가능성** — ADR-002~014(docs/40)가 합성한도 제거→인코딩 경로→무결성→서명→clause 검색→피드백까지 주요 결정과 기각 대안을 기록. 신규 참여자/감사 대응 비용을 낮춘다.

### 4.3 기술 부채 (실측)

| # | 부채 | 실측 근거 | 영향 |
|---|---|---|---|
| TD-1 | **MainWindow God-class** | `MainWindow.xaml.cs` 1,614줄 · 인스턴스 필드 21개 · ctor에서 서비스 12종 `new` · 12탭 핸들러 26개 · `RenderRiskCharts` ~133줄 차트 지오메트리 · 인라인 표시용 DTO 6종 | 변경 충돌·리뷰 비용 증가, 신규 탭/기능마다 악화. **라인수 가드 테스트도 없음** |
| TD-2 | **MVVM/커맨드 부재** | `INotifyPropertyChanged`/`ICommand`/ViewModel grep 0건 — 100% code-behind | 프레젠테이션 로직 단위테스트 불가(테스트가 소스 텍스트를 읽는 기형으로 보완 중) |
| TD-3 | **DI 부재 + static 서비스 로케이터** | `App.SecurityPolicyLoadResult`/`App.IntegrityResult` mutable static; 기동 순서 암묵 의존 | 격리 인스턴스화 불가. 단, NuGet 0 원칙의 의도적 산물 — 컨테이너 없이도 수동 조립 팩토리로 개선 여지 |
| TD-4 | **UI 계약 테스트의 취약성** | UiContractTests가 XAML/cs를 문자열 `.Contains`·XDocument로 검사 | 리팩터링 시 위양성/위음성 리스크. 실 렌더 검증은 Gate B가 유일 |
| TD-5 | **거대 파일 2차 집중점** | `LimitMonitor.cs` 1,023줄 · `ExcelReportBuilder.cs` 662줄 · `KbSearch.cs` 561줄 | 도메인 로직 자체는 테스트 밀도 높음 — 우선순위는 TD-1보다 낮음 |

### 4.4 개선 방향 (모듈별 권고)

- **App**: "재설계 금지" 원칙과 충돌하지 않는 **행위 불변 구조 분해**를 권고 — 탭별 부분 클래스/컨트롤러 추출(1단계), 표시 DTO를 Core 또는 App/ViewModels로 이동(2단계), 경량 수동 MVVM(INotifyPropertyChanged 인박스, 커맨드는 선택)(3단계). UI 계약 테스트를 "문자열 검사"에서 "구조 검사(XAML XDocument 기반)"로 점진 전환. **다음 UI 기능 트랙 착수 전(v0.8) 1단계 수행**이 손익분기 — 이후엔 비용이 계속 커진다.
- **Core**: 현 구조 유지(재설계 불필요). LimitMonitor는 신규 상태 추가 시 부분 분해(분류기/대사/감사 분리)만 검토.
- **Risk/Report**: PriorDayAnalyzer·streaming 경로의 **WPF call site 부재**(docs/48 B7/B8)가 아키텍처상 최대 공백 — 기능은 있으나 사용자가 못 쓴다. v0.8에서 UI 배선 권고(§10 WP).
- **Integrity/Release**: 서명(STAB-WP-05) 승인 후 서명 검증을 manifest 선행 앵커로 배치(docs/51 §B 권고안 그대로).
- **Generation**: `ILocalDraftService`를 그대로 R4 Adapter 계약의 기초로 사용하되, R4 설계 WP에서 out-of-process 경계(프로세스 격리·IPC·크래시 복구·타임아웃)를 **별도 계약**(예: `IModelHost`)으로 명시 — 현 인터페이스는 in-process 동기 시그니처라 런타임 경계 표현이 없다.

---

## 5. Capability Matrix

상태 어휘: VERIFIED(코드+테스트, local-gate) · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED(실 PC 증거 대기) · NOT_IMPLEMENTED · APPROVAL_REQUIRED. **모든 VERIFIED는 local-gate 한정이며 실 Test PC 봉인은 Gate B/C(BLOCKED)** — 이 단서는 전 행 공통.

| Capability | Status | Evidence | Gap | Recommended Action | Target |
|---|---|---|---|---|---|
| Rule Engine (SQL/VBA/Excel Safety) | **VERIFIED** | `Core/Safety`(RuleLoader 447줄, rules/*.txt 외부화·fail-safe·RuleVersion=sha 결정), SQL Blocker 14종+경고 4종·VBA Blocker(Shell/Kill/WScript/HTTP)+High(WinAPI/FSO/Outlook)·Excel 365 차단 19종, SafetyTests(QA-WP-01 +15) | 실 UI 표면화 증거(Gate B B13) | Gate B 봉인만 | v0.7.x |
| Data Profiler | **VERIFIED** | `Core/Data`(DataProfiler 376줄: Null 4형·중복행 SHA256 해시만·3σ Outlier·streaming==in-memory 필드 동일 회귀, CP949 17,236 entries SHA 핀), CsvTests/XlsxTests/DataProfileTests(QA-WP-05) | streaming 경로 UI 미노출(B7 local-gate 전용) · XLSX는 in-memory 전용(5,000행/256열 상한 — 설계) | v0.8 UI/harness 배선 | v0.8 |
| Exposure-Limit Join | **VERIFIED** | `Core/Risk/LimitMonitor`(1,023줄) 7상태+`DUPLICATE_LIMIT` 차단, 대사 9종, JoinAudit(R2-WP-01 #79) | Gate B 실 PC 봉인(B4~B6) | Gate B 라운드 1 | v0.7.x |
| Risk Analytics (Prior-Day 등) | **VERIFIED**(코드) / **기능 미노출** | `PriorDayAnalyzer`(293줄, #84), LimitReconciliationTests +18 | **WPF call site 0**(docs/48 B8) — 사용자 접근 불가 | **Prior-Day UI WP**(v0.8 최우선 후보) | v0.8 |
| Excel Report (+RISK_VISUAL) | **VERIFIED** | `ExcelReportBuilder`(662줄, 시트 11종, 정확 Exception Count Number SoT), `RiskVisualAggregator`(HHI/TopN/Heatmap), ReportTests(QA-WP-04) | Excel 2021 실 열기 증거(Gate C C1/C2), B-8 PENDING | Gate B/C 라운드 1 | v0.7.x |
| Excel Function Helper | **VERIFIED**(main) / **출하본 미포함** | UX-WP-04 #102, embedded `excel_function_help.json`, UX-WP-11 #122 카탈로그 확장(차단 함수 추천 0 가드) | v0.7.0 ZIP(`30c1cfb`)에 없음 → 사용자 미체감 | **v0.7.1 릴리스 컷** 후 B-5 재검증 | v0.7.x |
| Smart Assist (as-you-type·팝업) | **VERIFIED**(main) / **출하본 미포함** | UX-WP-01~03·05~09(#70~#113), `CompletionEngine`(SafetyHint/BlockedHint 핀·비삽입 강제·전 항목 RequiresReview=true)+정적 Provider 6종(SQL 스니펫 6종·VBA 안전 스니펫 5종·Excel allow-list·RiskPhrase 8종)+`CompletionTriggerPolicy`, 수락 audit는 InsertTextHash만 | 동상(v0.7.1 필요). 실 포커스/렌더=Gate B | v0.7.1 + Gate B R2 라운드 | v0.7.x |
| KB Clause Search | **VERIFIED**(구조·keyword) | KB-WP-01/02 #94/#101, `ClausePackLoader`·`KbClauseIndex`(역색인, >32자 linear fallback)·`ClauseSnippetAllowed` 게이트(공개+메타 완비 시에만 32자 snippet)·`SourceTextAllowed=false` 하드코딩, KbTests(QA-WP-03) | **콘텐츠=합성 더미** — 카탈로그 5행(메타)·조항 샘플 2행(합성 문구), 실 공개 규정 clause pack 미적재(승인 게이트) | RAG 게이트 결정 + Pack 제작 파이프라인 WP | v0.8 |
| NCR Rule Set | **SCAFFOLD_ONLY** | `NcrRuleSetLoader`(218줄) 8요소 구조, placeholder `APPROVAL_REQUIRED_NO_REAL_COEFFICIENT`, NcrTests(QA-WP-06) | 실 계수/공식본 = repo 영구 미포함(설계) — Prod 권한통제 KB 적재 SOP 승인 대기 | docs/51 §C 결정 → NCR-WP-01 계약 로더 | v0.8.x~v1.1 |
| Feedback Learning (승인 Example) | **VERIFIED**(RETRIEVAL) | FEEDBACK-WP-01/02 #106/#108: ingest 게이트(Blocker 0+ForbiddenTermScanner)·결정적 검색·`ReferencesReviewed` 게이트·hash 이중 audit·자동주입 0 | 검토/승격 UX가 초기 수준, 실 PC 검증 없음 | Feedback Review UI WP | v0.9 |
| Local LLM Adapter | **NOT_IMPLEMENTED**(계약 심만) / Runtime **APPROVAL_REQUIRED** | `ILocalDraftService`+NoModel 구현·`DraftPipeline` 안전검사·model_pack 문서·docs/51 §A 2단계 승인안 | Adapter 계약(R4) 미설계 — out-of-process 경계 표현 없음. Runtime/모델 = STOP | LLM-WP-01 설계 WP(승인 불요) + docs/51 §A 결정 | v0.9(설계)/v1.1(런타임) |
| Release Package | **VERIFIED** | build/00~04, VERSION 락스텝, manifest+Fail-Closed(#59/#61), PackagingTests 398줄(변조 매트릭스), 4회 릴리스 실적 | 미서명(STAB-WP-05), lock-step co-tamper 잔여 | 서명 승인 → 구현 WP | v0.9 |
| Gate B/C | **BLOCKED** | docs/48: 선행 A PASS, §B′ 부분 user-reported(증거 미첨부), B-6/B-8 PENDING, §B″ 턴키 런북 완비 | **실 PC 증거만 미충족** — repo에서 생성 불가 | 사용자 실행(런북 §1~4) | v0.7.x |
| Team Pilot | **NOT_IMPLEMENTED**(준비물 부분 존재) | docs/41 §5 체크리스트, docs/20/30 데모 시나리오, docs/25 운영 가이드 | Gate B/C 선행 + 파일럿 킷(사용자 가이드·피드백 양식·Known Limitations) 미완 | Pilot Kit WP + §11 계획 | v1.0 |

---

## 6. Gap Analysis

### 6.1 기능 갭 (Functional)
1. **Prior-Day·Streaming의 UI 부재** — R2의 절반이 사용자에게 안 보임(docs/48 B7/B8 명시). 가장 값싼 고가치 기능 갭.
2. **출하본 갭** — UX-WP-04~11이 어떤 출하 ZIP에도 없음(v0.7.1 컷 필요).
3. **KB 콘텐츠 갭** — 검색 가능한 실 공개 규정 조항이 0건(카탈로그 메타 5행 + 합성 조항 샘플 2행이 전부). "규정 검색 보조" 가치 실현 불가 상태.
4. **NCR 실 산정 갭** — 구조만(산정 엔진 자체가 없음 — `NcrExplain`은 구조 설명 텍스트만 생성). 승인 전까지는 의도된 갭(설계 준수).
5. **LLM 부재 + 초안 생성 비활성** — 의도된 갭(STOP). `NoModelDraftService`는 텍스트를 생성하지 않으므로 "SQL/VBA 초안 생성"은 런타임상 미구현이며, 8단계 SQL 포맷은 `templates/sql/sql_generation_prompt.md` **템플릿으로만 존재**(코드 미배선). 또한 R4 Adapter **설계**(승인 불요, ADR-003이 설계는 허용)조차 미착수 — 승인이 떨어져도 착수까지 리드타임 발생.

### 6.2 품질 갭 (Quality)
1. 실 WPF 렌더/포커스/DPI/창 크기(1180×720~2560×1440) 검증 0 — 텍스트 계약 테스트의 구조적 한계.
2. Excel 2021 실 호환성(수식 오류·외부링크·매크로·Injection 0) 미봉인(Gate C C1/C2).
3. 성능/메모리 실측 0(C6) — streaming 상한(200k행/50MB)의 실 PC 체감 미확인.
4. 대사 깊이의 보수적 한계(informational) — `RECON_SUM_BALANCE`가 decimal 정밀 등가(허용오차 0), 통화/단위는 문자열 불일치 검출만(환산 없음), 9종 중 fail-code는 3종. 설계상 보수적이나 파일럿에서 기대치 관리 필요. (참고: 통화/단위의 ColumnMapping 경유 전환은 R2-WP-01에서 **완료** — 잔여 하드코딩은 표시용 `DESK_CD`/`PRODUCT_TYPE` 2개뿐.)
5. 시각화 표면 비일치(cosmetic) — Heatmap MID 구간(0.8~1.0)이 Warning 컷(0.9)과 정렬되지 않고, 대시보드 TopN=5 vs 리포트 TopN=10(동일 aggregator·의도적이나 비동일 표면). 파일럿 전 정렬 여부 결정 권고.

### 6.3 운영 갭 (Operational)
1. `evidence/` 수집 체계 미가동(폴더 규약은 docs/48에 정의됨, 실 증거 0건).
2. Rollback 절차 문서만 존재, 리허설 기록 없음(C7).
3. 사용자 가이드/퀵스타트가 파일럿 대상자 수준으로 정리 안 됨(docs/25는 운영자 관점).
4. **GitHub Actions 트리거 복원 결정 지연** — 분 리셋 예정일(~2026-06-30)이 경과했는데 `ci.yml`·`governance-soft-guard.yml`은 여전히 `workflow_dispatch` 수동 전용(파일 주석에 복원 절차 명시됨). local-gate 모델을 유지할지, `pull_request` 자동 트리거(test=ubuntu 1x 최소)를 복원해 보조망을 살릴지 **사용자 결정 필요**.

### 6.4 보안/컴플라이언스 갭 (Security/Compliance)
1. **코드 서명 부재** — lock-step co-tamper·런타임 DLL 변조·폴더 동반 변조 3건 OPEN(테스트가 "미탐지=양성"으로 고정 중 — 정직하나 미폐쇄). 경로 A 확정으로 절반 진행.
2. 미서명 ZIP의 사내 반입 정책 적합성 미확인(C5 — ACCEPTED_RISK 수용 여부는 사용자 조직 결정).
3. 내부규정/NCR 원문 접근통제 KB의 **Prod 측 설계**(역할권한·조회로그 시스템)는 문서 수준 — 적재 승인 시 구체화 필요.
4. (양호) repo 자체는 본 검토 스캔에서 비밀정보·모델·원문 0 확인.

### 6.5 UX 갭
1. B-5 PARTIAL의 원인이던 3건(Excel 함수 상세·as-you-type·팝업)은 구현 완료(main) — **전달 갭**만 남음(v0.7.1).
2. MainWindow 12탭 구조의 정보 밀도·내비게이션은 파일럿 피드백 대상(현재 설계 검증 데이터 없음).
3. 결과 패널의 한국어 메시지 일관성·용어 통일 — 파일럿 전 1회 sweep 권고.

### 6.6 문서 갭 (Documentation)
1. 51개 문서의 truth-sync 부하 — 현재는 규율로 유지 중이나, 기준선 표기가 CLAUDE.md·README·docs/38/39/50 등 다중 위치에 중복(단일 STATUS 파일로 수렴 검토 여지). 본 검토의 drift 샘플링 결과 **핵심 기준선(7094d91·Total=900)은 전 위치 정합** — 규율이 실제 작동 중.
2. docs/51 헤더의 기준선이 `315fd30`(작성 시점 main, docs-only SHA) — 정본 관례(`7094d91`)와 표기 불일치이나 테스트 트리 동일이라 실질 drift 아님(차기 truth-sync에서 주석 권고).
3. **`risk-data-limit-review` skill 체크리스트가 stale** — "6상태"·"통화 하드코딩 CCY_CD(ColumnMapping 미경유)"로 기재되어 있으나 실제 코드는 **7상태**(DuplicateLimit 포함, 테스트가 enum 서수 [0..6] 단언)·통화/단위 ColumnMapping 경유(R2-WP-01 완료). 리뷰 도구가 코드보다 뒤처지면 리뷰 품질이 저하 — 갱신 필요.
4. docs/40 ADR-008 상태·docs/46에 역사적 `Total=572` 인용 잔존 — 시점 증거 인용으로 정당하나 "현재 수치"로 오독 여지(시점 주석 권고).
5. 파일럿 대상자용 문서(비개발자용 시작 가이드·FAQ·Known Limitations 요약) 부재.

---

## 7. Risk Register

Severity/Likelihood: H/M/L. (docs/38 §6 RR-01~16과 상보 — 여기서는 제안서 관점 신규/재구성 항목에 P-ID 부여.)

| ID | 리스크 | Sev | Lik | 영향 | 완화(Mitigation) | Owner | Release Gate |
|---|---|---|---|---|---|---|---|
| P-01 | **내부규정/NCR 원문 유출**(repo/ZIP 유입) | H | L | 규정 위반·프로젝트 중단급 | 5층 방어 유지(프로세스→Gate A→KbRepositoryGuard→build/03→gitignore, docs/51 §C.1) + 승인 SOP 준수. 현 스캔 0건 | 문서오너+Claude(Gate A) | RAG/NCR Gate |
| P-02 | **Local LLM 환각**(승인 후 도입 시) | H | M | 오답 초안의 업무 반영 | 2단계 PoC에서 환각률/인용준수율 실측(ADR-009 §6) 후 채택 판단 · Output→Safety Checker 경유 · "검토용 초안" 고지 · NoModel fallback 유지 | 문서오너(승인)+Claude(설계) | Model Approval |
| P-03 | **SQL/VBA 자동실행 오인**(사용자가 "실행해주는 도구"로 오해) | M | M | 신뢰 손상·감사 지적 | UI 문구·가이드에 "실행 없음" 명시 유지, 파일럿 온보딩에서 시연, Draft 파이프라인 BLOCKED 표기 유지 | Claude(문서)+사용자(교육) | Pilot Gate |
| P-04 | **Gate B/C 증거 부재 장기화** | H | M | 파일럿 지연·"검증 안 된 도구" 낙인 | §B″ 턴키 런북 실행(반나절)·evidence/ 봉인·항목별 승격 규칙 이미 존재 | **사용자(유일)** | Pilot Gate B/C |
| P-05 | **코드 서명 미승인 장기화** | M | M | 반입 정책 충돌 시 배포 불가·co-tamper 잔여 | docs/51 §B provenance 4항목 기입→§6.2 승인→구현 WP(인박스 도구로 NuGet 0 유지) | 문서오너/보안팀 | Code-Signing Gate |
| P-06 | **모델/런타임 라이선스 위반**(승인 후) | H | L | 법적 리스크 | ADR-009 필수항목 3(라이선스 의무) 반려 기준 명시 — 재배포·상업이용 허용본만, 제한 라이선스=반려 | 문서오너 | Model Approval |
| P-07 | **Vector/Embedding 무단 도입**(RAG 고도화 압박) | M | L | 의존성 원칙 붕괴 | STOP 규칙 유지·keyword-only 명시(docs/41 §2)·필요 시 승인 문서 선행 | Claude(STOP 집행) | RAG Gate |
| P-08 | **문서 drift**(51개 문서·다중 기준선 표기) | M | M | 상태 오인·리뷰 비용 | truth-sync skill 지속 + 기준선 표기 위치 축소(단일 STATUS 소스) 검토 | Claude | — (상시) |
| P-09 | **테스트 과신**(900 = 실 PC 품질로 오인) | H | M | 파일럿에서 UI/환경 결함 노출 | "local-gate 한정" 단서 전 문서 유지(현행) + Gate B/C를 릴리스 트레인의 명시 게이트로(현행) + UI 계약 테스트 한계 §6.2 공유 | Claude+사용자 | Pilot Gate B/C |
| P-10 | **실 PC UI 미검증**(DPI·창크기·포커스·팝업) | M | M | 파일럿 첫인상 손상 | Gate B R2 라운드(신규 빌드)에서 B-5 재검증 + 다양한 창 크기 캡처(B10) | 사용자 | Gate B |
| P-11 | **God-class 부채 누적**(TD-1) | M | H | 신규 UI WP마다 비용·리스크 증가 | v0.8 착수 전 행위 불변 분해 1단계(§10 ARCH-WP-01), UI 계약 테스트 동반 갱신 | Claude(설계)+Codex(구현) | Gate A+회귀 |
| P-12 | **출하본-main 괴리 지속** | M | H | 사용자 체감 개선 미전달·B-5 PARTIAL 고착 | v0.7.1 REL 컷(§10 REL-WP-071) — 버전 범프 락스텝 절차 기존 검증됨 | 사용자(발행)+Codex(빌드) | Gate A+Local |
| P-13 | **승인 게이트 결정 정체**(NEXT UP 3택 방치) | M | M | v0.9~v1.1 스코프 불확정·기획 공전 | docs/51 §D 요약표 기반 결정 회신 — 부분 승인 가능(상호 독립) 명시됨 | 문서오너(사용자) | — |
| P-14 | **파일럿 피드백 미수집**(도입 후 방치) | M | M | 개선 루프 단절·확산 실패 | 인앱 Feedback(구현됨)+주간 양식+KPI(§11.7) — Pilot Kit WP로 준비 | 사용자+Claude | Pilot |

---

## 8. Roadmap (v0.7.x → v1.1+)

기존 Release Train(docs/38 §2)을 존중하되, **결정 지점 이후의 실행 순서**를 제안한다. 원칙: ① 사용자만 할 수 있는 일(Gate B/C·승인 결정)을 크리티컬 패스에서 먼저 빼내고, ② Codex가 승인 없이 진행 가능한 안전 트랙을 병행하며, ③ 승인 필요 트랙은 결정 즉시 착수 가능하게 설계를 선행한다.

### v0.7.x — Gate B/C 봉인 + 출하본 정합 (지금 ~ 2주)
- **Goal**: 실 Test PC 증거로 Gate B/C 최초 봉인 + 사용자 체감 기능이 담긴 출하본 확보.
- **Included**: Gate B 라운드 R1(published ZIP: B-6·B-8 + user-reported 봉인) · Gate C(C1~C7, Excel 2021) · **REL-v0.7.1 컷**(current main 기준, binary-impact 기준선 `7094d91` — 이미 머지된 UX-WP-04~11의 출하 반영, 신규 기능 0) · Gate B 라운드 R2(v0.7.1: B-5 재검증) · docs/48 워크시트 기입·evidence/ 커밋.
- **Excluded**: 신규 기능 0 · 서명 0(승인 전) · KB 콘텐츠 0.
- **Test Gate**: 기존 900 유지(v0.7.1 컷은 단언 가감 0 원칙) + build/00~03 PASS.
- **Approval Gate**: 없음(전부 STOP 비접촉). 단 v0.7.1 공개 발행은 사용자 릴리스 오너 결정.
- **DoD**: docs/48 상단 판정이 BLOCKED → 항목별 PASS(명시 예외 B7/B8/C5 수용 포함)로 갱신, evidence/gateB/ 봉인, v0.7.1 Release 발행(SHA256 공표).

### v0.8 — 분석 가치 완성 + KB 콘텐츠 + UI 구조 (2주 ~ 6주)
- **Goal**: "이미 만든 분석 능력을 사용자 손에" + "규정 검색을 실사용 가능하게".
- **Included**: **Prior-Day Analytics UI 배선**(4구획·movers 화면+리포트) · **대용량 streaming UI/한도 안내 배선** · **MainWindow 분해 1단계**(행위 불변, 탭 컨트롤러 추출) · **공개 규정 Clause Pack 제작 파이프라인**(오프라인 pack builder + 검증 도구; pack 자체는 RAG 게이트 결정에 따라 repo 외 배포 또는 공개분 동봉) · 통화/단위 ColumnMapping 경유 전환(docs/41 §1 알려진 한계 해소) · 파일럿 킷 문서 초안.
- **Excluded**: LLM 런타임 · NCR 실 계수 · Vector/Embedding(전부 STOP 유지).
- **Test Gate**: Total 증가(신규 회귀 additive)·기존 삭제/약화 0·Unclassified 0.
- **Approval Gate**: **RAG Approval Gate** — 실 공개 규정 clause pack의 적재 범위/방식(문서오너). 공개 법령 원문이라도 현 정책(`SourceTextAllowed=false` 불변·원문 repo 미포함)의 변경 여부는 승인 사항.
- **DoD**: Prior-Day가 실 UI에서 동작(Gate B 증거), clause 검색이 실 공개 규정 N개 문서에서 인용 반환, MainWindow ≤ 절반 수준으로 분해 + UI 계약 테스트 green.

### v0.9 — 승인 트랙 실행 + 파일럿 준비 완료 (6주 ~ 10주)
- **Goal**: 승인된 게이트의 구현 완료 + 파일럿 직전 상태.
- **Included**: **STAB-WP-05 서명 구현**(승인 시: 인박스 `Set-AuthenticodeSignature`·검증 앵커·잔여 3건 회귀 전환) · **LLM-WP-01 Adapter 계약 설계**(out-of-process 경계·Manifest·Integrity·NoModel fallback — 코드 계약+문서, 런타임 0) · **Feedback Review UI**(승격 검토 화면) · Model Approval Package 준비(후보 매트릭스, 측정 전 항목 기입) · 파일럿 킷 완성(가이드·FAQ·Known Limitations·피드백 양식).
- **Excluded**: LLM 런타임 반입(2단계 승인 전) · 내부규정 원문.
- **Test Gate**: 서명 도입 시 "미탐지→탐지" 회귀 전환 PASS(docs/41 §6.3).
- **Approval Gate**: Code-Signing(§B)·Model 1단계 PoC(§A) — docs/51 결정 필요.
- **DoD**: 서명본 릴리스 후보(승인 시) 또는 미서명 ACCEPTED_RISK 확정, R4 계약 문서+계약 테스트 머지, 파일럿 킷 리뷰 완료.

### v1.0 — Team Pilot (10주 ~ 16주)
- **Goal**: 2~5인 실사용 파일럿 완주와 감사 가능한 결과 보고.
- **Included**: RC 컷(v1.0.0-rc) → Gate B/C 재봉인 → 파일럿 4~6주 운영(§11) → 피드백 반영 패치(v1.0.x) → v1.0.0 정식.
- **Excluded**: 파일럿 중 신규 대형 기능 투입(변인 통제).
- **Test Gate**: RC 기준 전 게이트 green + Gate B/C 봉인.
- **Approval Gate**: 사내 파일럿 승인(부서장/보안 — 미서명이면 C5 ACCEPTED_RISK 서면 수용).
- **DoD**: §11.7 KPI 리포트, 보안 사고 0, 파일럿 사용자 지속 사용 의사 확보.

### v1.1+ — 승인 후 확장 (16주~)
- **Goal**: 승인된 범위에서 AI 능력 실물화.
- **후보(각각 독립 승인)**: ① Local LLM 2단계(격리 PoC 실측 → LLM-WP-02 out-of-process Adapter 구현 → Model Pack 반입 SOP) ② NCR 실 Rule Pack Prod 적재(NCR-WP-01 계약 로더 + 문서오너 적재) ③ 내부규정 Knowledge Pack(권한통제 KB) ④ (수요 발생 시) Vector/Embedding 승인 검토.
- **Test/Approval Gate**: Model Approval 2단계 · NCR Approval · RAG(내부) — 전부 docs/41 정의 준수, 실측 없는 PASS 금지.
- **DoD**: 각 항목 승인 문서 완비 + Prod 적재 후에도 repo 상태 불변(원문/계수/모델 미포함) 확인.

---

## 9. Claude / Codex Execution Plan

### 9.1 역할 분담 (현행 유지 + 보강)

**Claude (Architecture Lead / PM / Security & Release Reviewer / Doc Owner):**
- Truth Sync·Roadmap(docs/38)·WP(docs/39)·ADR(docs/40) 갱신, WP별 Codex 프롬프트(`prompts/codex/`) 작성.
- Codex 결과 4축 리뷰(범위·보안·테스트·문서) + adversarial 검증(#108~#127에서 검증된 방식: 다중 finding 독립 검증 → 0 confirmed 후 머지).
- Traceability(docs/38 §5)·NEXT UP 1개 지정. main 직접 수정/병합 금지, 계획 브랜치 `planning/*`.
- **보강 제안**: ① 대형 리팩터(ARCH-WP)의 경우 "행위 불변 증명" 리뷰 축 추가(before/after 산출 비교), ② 승인 게이트 결정 후 48시간 내 후속 WP+프롬프트 작성을 표준 SLA로.

**Codex (구현자):**
- 한 번에 WP 1개, `feature/<WP-ID>-*`, 로컬 게이트 증거(build 0/0 + `Total=N PASS/0 FAIL` + Gate A + PackageReference 0) 첨부, Claude 승인 전 머지 금지.
- Windows 로컬에서 build/test/packaging 실행(local-gate 모델, CLAUDE.md §11.6).

**사용자 (Product/Release/승인 Owner):**
- Gate B/C 실 PC 실행(유일 실행자), 릴리스 발행, docs/51 승인 3건 결정, 파일럿 조직화.

### 9.2 WP 분해 원칙 (docs/39 형식 준수)
- WP 1개 = 목표 1개, 14필드(목표~Review Checklist) 완비, 파일 겹침 없는 WP만 병렬.
- additive-first: 기존 테스트 삭제/약화 0, 신규는 회귀 추가.
- STOP 접촉 WP는 승인 문서 링크 없이 프롬프트 작성 금지.

### 9.3 병렬 가능 vs 순차

**병렬 가능(파일 비겹침, 승인 불요):**
- Gate B/C 실행(사용자) ∥ v0.7.1 REL 컷(Codex) ∥ Prior-Day UI WP 설계(Claude) — 3자 동시 진행 가능.
- KB pack builder 도구 ∥ Feedback Review UI ∥ 파일럿 킷 문서 — 서로 독립.

**순차(의존):**
- MainWindow 분해(ARCH-WP-01) → 이후의 모든 UI WP(Prior-Day UI 포함)는 분해 후 착수가 이상적. 단 Prior-Day UI가 급하면 역순 허용(분해 시 이동 비용 소폭 증가).
- 서명 구현 → 승인(§B) 후에만. LLM Adapter 구현 → 계약 설계(LLM-WP-01) + 승인(§A 2단계) 후에만.
- v0.7.1 컷 → Gate B R2 라운드(B-5 재검증)의 선행.

### 9.4 리뷰 체크리스트 (Claude, WP 공통 — 기존 skill 4축 + 추가)
1. 범위: diff가 지정 WP만 다루는가. 2. 보안: 원문/실데이터/실 테이블명/모델/NuGet/자동실행/평문 audit 0. 3. 테스트: 도메인 분류·기존 단언 보존·양성/음성 쌍·Total 비감소·Unclassified 0. 4. 문서: 기준선·Traceability·상태 어휘 정합. 5. (UI WP) XAML 계약 테스트 갱신 여부. 6. (리팩터 WP) 행위 불변 증거(동일 입력→동일 출력 회귀).

### 9.5 Recommended NEXT UP

**NEXT UP = PILOT-WP-02 "Gate B/C Evidence Round 1"(user-driven, docs/48 §B″ §1~4)** — 이유: ① 사용자만 가능, ② 승인 불요, ③ 파일럿 크리티컬 패스의 병목, ④ 실패해도(결함 발견) 그 자체가 다음 WP를 정의하는 정보 이득. **병행**: Codex에 REL-WP-071(v0.7.1 컷) 프롬프트를 즉시 발부(파일 비겹침·저위험).

---

## 10. Recommended Work Packages

> 형식: 본 제안서 요약 수준. 채택 시 `risk-wp-planner`로 docs/39 14필드 + `prompts/codex/` 프롬프트를 정식 작성한다. "Codex now?"=승인 없이 지금 착수 가능 여부.

### WP-A. PILOT-WP-02 — Gate B/C Evidence Round 1 (published v0.7.0 ZIP)
- **Purpose**: Gate B/C 최초 봉인. **Scope**: docs/48 §B″ §1~4(무결성·B-6·B-8·Gate C), evidence/gateB/ 수집, 워크시트 기입. **Out of Scope**: B-5 재검증(신규 빌드 필요), 코드 변경 0. **Files**: `docs/48`(상태 갱신), `evidence/gateB/*`. **Tests**: 없음(코드 불변). **Security**: **증거에 실거래/실포지션/고객정보/내부규정 원문/계정정보 포함 금지** — `samples/` dummy 데이터와 masking된 화면만 사용, 커밋 전 Gate A 스캔을 증거 파일에도 적용. **Gate**: Pilot Gate B/C. **Priority**: **P0**. **Codex now?**: No(사용자 전용). **Approval?**: No.

### WP-B. REL-WP-071 — v0.7.1 Release Cut (current main 기준 · binary-impact 기준선 = `7094d91`)
- **Purpose**: **신규 기능 구현이 아니라, main에 이미 구현·머지된 UX-WP-04~11(Excel Function Helper·Smart Assist as-you-type/팝업 등)을 실제 출하본에 반영하는 release cut.** 컷은 current main에서 수행하되 binary-impact 기준선은 `7094d91`로 본다(docs-only 머지 #128~#130은 바이너리 불변 — 문서 전용 머지가 baseline SHA를 올리지 않는 관례 유지). **Scope**: 버전 범프 락스텝(`VERSION`·`IntegrityVerifier.ExpectedVersion`·`PackagingTests` 3파일) 0.7.0→0.7.1, `build/00~03` 전 단계 실행, **산출물 검증 필수: ZIP SHA256(.sha256 대조)·ReleaseNote·DependencyList·approved_manifest**, 태그·Release 발행(로컬). **Out of Scope / 불변식**: **기능 변경 0 · 테스트 단언 가감 0(Total 불변) · STOP 게이트 접촉 0**(외부 NuGet·서명·모델·원문 일절 없음). **Files**: `VERSION`, `IntegrityVerifier.cs`(ExpectedVersion), `PackagingTests.cs`, `docs/47` 계열 신규 릴리스 문서. **Tests**: Total 불변 + 버전 drift 가드 PASS. **Security**: build/03 금지파일·원문 미포함 스캔 PASS·미서명 고지 유지. **Gate**: Gate A + Local-Gate. **Priority**: **P0**. **Codex now?**: Yes. **Approval?**: 발행 자체는 사용자 결정(STOP 아님).

### WP-C. UI-WP-12 — Prior-Day Analytics UI 배선
- **Purpose**: `PriorDayAnalyzer`(코드 VERIFIED·UI 부재)를 사용자 기능으로. **Scope**: Risk 탭에 전일 파일 선택 + Current/Prev/Δ·movers·4구획 표시 + 리포트 시트 연동. **Out of Scope**: 새 분석 엔진 0(기존 2회 diff 방식 유지), 자동 파일 추론 0. **Files**: `MainWindow.xaml(.cs)`(또는 분해 후 해당 컨트롤러), `ExcelReportBuilder.cs`(시트), UiContractTests·ReportTests. **Tests**: UI 계약+리포트 회귀 additive. **Security**: 실데이터 0(합성 샘플), 자동실행 0. **Gate**: Gate A + Gate B(실 렌더 후속). **Priority**: **P1**. **Codex now?**: Yes. **Approval?**: No.

### WP-D. ARCH-WP-01 — MainWindow 행위 불변 분해 1단계
- **Purpose**: TD-1 완화 — 이후 UI WP 비용 절감. **Scope**: 탭별 partial class/컨트롤러 추출(12탭 → 파일 분리), 표시 DTO 이동, 라인수 상한 가드 테스트 신설(예: MainWindow.xaml.cs ≤ 600줄). **Out of Scope**: 동작·XAML 구조·탭 구성 변경 0, MVVM 전면 전환 0(2단계로 유보). **Files**: `src/RiskManagementAI.App/**`(신규 파일 다수), UiContractTests 갱신. **Tests**: 기존 계약 테스트 green + 신규 구조 가드. **Security**: 변경 없음. **Gate**: Gate A + 행위 불변 리뷰(§9.4-6). **Priority**: **P1**(UI-WP-12와 순서는 §9.3). **Codex now?**: Yes. **Approval?**: No.

### WP-E. KB-WP-03 — Public Clause Pack Builder (오프라인 제작 파이프라인)
- **Purpose**: 실 공개 규정(금융투자업규정 등) 조항을 승인 절차 하에 pack으로 만드는 도구. **Scope**: 입력(텍스트/CSV) → clause pack 스키마 검증 → ChunkId 결정성 검사 → 금지 토큰 스캔 → pack 산출(레포 외 보관 기본). repo에는 도구+합성 픽스처만. **Out of Scope**: 실 pack 커밋 0(RAG 게이트 결정 전), Vector/Embedding 0. **Files**: `tools/` 또는 `src/…/Kb`(빌더), KbTests. **Tests**: 스키마/결정성/가드 회귀. **Security**: `SourceTextAllowed=false` 불변, KbRepositoryGuard 통과. **Gate**: RAG Approval(적재 시). **Priority**: **P2**. **Codex now?**: Yes(도구까지). **Approval?**: 실 pack 적재/동봉만 Yes.

### WP-F. LLM-WP-01 — Local LLM Adapter Contract (설계 전용)
- **Purpose**: R4 설계 선행 — 승인 즉시 구현 착수 가능 상태. **Scope**: out-of-process 경계 계약(`IModelHost` 초안: 기동/종료/타임아웃/크래시 복구/IPC 스키마), Model Pack Manifest 스키마(+Integrity Hash), NoModel fallback 불변 규정, Output Safety Pipeline 연결점 문서화(ADR 갱신). 코드 계약+계약 테스트(구현 0). **Out of Scope**: 런타임/모델/외부 라이브러리 0(STOP), PoC 실행 0(승인 §A 1단계 후). **Files**: `Core/Generation`(계약), docs/40 ADR, GenerationTests. **Tests**: 계약 존재·NoModel 불변 회귀. **Security**: PackageReference 0 유지. **Gate**: Model Approval(구현은 승인 후). **Priority**: **P2**. **Codex now?**: Yes(설계·계약 한정). **Approval?**: 구현/런타임은 Yes.

### WP-G. FEEDBACK-WP-03 — Feedback Review UI
- **Purpose**: 승인 Example 검토/승격/반영을 화면에서. **Scope**: Feedback 탭 확장(대기 목록·검토·승격·`ReferencesReviewed` 체크 UX), hash audit 유지. **Out of Scope**: 자동 승격 0·학습 0. **Files**: App(Feedback 탭), Feedback 도메인 소폭, AssistTests/AuditTests. **Priority**: **P2**. **Codex now?**: Yes. **Approval?**: No.

### WP-H. STAB-WP-05 — Authenticode 서명 구현
- **Purpose**: 무결성 잔여 3건 폐쇄. **Scope**: docs/51 §B 권고안(인박스 `Set-AuthenticodeSignature`·검증 앵커·회귀 "미탐지→탐지" 전환). **Priority**: **P1(승인 후)**. **Codex now?**: **No — B.4.0 provenance + §6.2 승인 선행**. **Approval?**: **Yes**.

### WP-I. NCR-WP-01 — Approved NCR Rule Pack Contract Loader 보강
- **Purpose**: 승인 시 즉시 적재 가능한 fail-closed 로더 완성. **Scope**: docs/51 §C.1-3 계약 필수항목 검증 로더(항목 누락=차단), repo는 스키마+placeholder 불변. **Priority**: **P3**. **Codex now?**: Yes(구조 한정). **Approval?**: 실 계수 적재만 Yes.

### WP-J. DOC-WP-PILOT — Pilot Kit
- **Purpose**: 파일럿 온보딩 자료. **Scope**: 비개발자용 퀵스타트·시나리오 워크북(docs/20/30 재사용)·Known Limitations·피드백 양식·인시던트/롤백 1페이지. **Priority**: **P2**. **Codex now?**: Yes(Claude 주도 가능). **Approval?**: No.

### WP-K. GOV-WP-01 — 문서/스킬 정합 스윕 (Claude, planning/*)
- **Purpose**: 본 검토에서 발견된 stale 항목 일괄 해소. **Scope**: `risk-data-limit-review` skill 갱신(7상태·ColumnMapping 경유 반영), docs/40 ADR-008·docs/46의 `Total=572` 시점 주석, docs/51 기준선 표기 주석, CI 트리거 복원 결정 반영(§6.3-4 결정 후). **Out of Scope**: 코드 0. **Files**: `.claude/skills/risk-data-limit-review/*`, docs/40·46·51. **Tests**: 없음(문서). **Security**: 해당 없음. **Gate**: truth-sync. **Priority**: **P2**. **Codex now?**: No(Claude 문서 오너 작업). **Approval?**: No.

**우선순위 요약**: P0 = WP-A(사용자)+WP-B(Codex) 즉시 병행 → P1 = WP-D→WP-C(또는 C 선행)·WP-H(승인 시) → P2 = WP-E·F·G·J·K → P3 = WP-I.

---

## 11. Gate B/C and Pilot Plan

### 11.1 User-driven Offline Test PC 계획 (docs/48 §B″ 그대로 실행)
1. **R1 라운드(현 published ZIP `30c1cfb`)** — 반나절: `Get-FileHash` 대조(`42C835…`) → 트리 스캔(tree.txt) → 인터넷 차단 기동(NoModel·manifest 0.7.0) → B-6(CSV/XLSX·7상태) → B-8(RISK_VISUAL·정확 Exception Count) → 화면=리포트 캡처 2장 → Gate C(Excel 2021 열기·EDR·C4b·C6·C7).
2. **R2 라운드(v0.7.1 또는 §5a test-only 빌드)** — B-5 재검증(Excel Helper·as-you-type·포커스). §5a 빌드는 봉인용 아님(출하본 게이트는 공개 컷에서만) — docs/48의 이 구분은 그대로 유지한다.
3. 증거는 전부 `evidence/gateB/`(해시 출력·캡처·로그), 워크시트(docs/48) 기입 → 회신 → 항목별 재판정.
4. **증거 민감정보 금지**: evidence에는 **실거래/실포지션/고객정보/내부규정 원문/계정·비밀정보를 포함하지 않는다.** 검증 입력은 `samples/` dummy 데이터만 사용하고, 화면 캡처에 사내 정보가 노출될 소지가 있으면 **masking된 화면만** 저장한다. 커밋 전 증거 파일에도 Gate A(`risk-security-guard`) 스캔을 동일 적용한다.

### 11.2 증거 템플릿
docs/48 증거 워크시트(라운드 분리형)가 이미 정본 — 신규 양식 불필요. 공통 메타: `PASS/FAIL/BLOCKED · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`. 증거 파일 자체가 커밋/반입 대상이므로 §11.1-4 민감정보 금지 원칙(실거래/포지션/고객/원문/계정정보 0 — dummy sample·masking만)을 동일 적용한다.

### 11.3 Go/No-Go 기준 (Pilot 진입)
- **Go**: Gate B 전 항목 PASS(명시 예외 B7/B8 수용) + Gate C C1~C4b PASS + C5는 서명본 또는 ACCEPTED_RISK 서면 수용 + C6 측정값 기록 + C7 롤백 확인 + 파일럿 킷 존재.
- **No-Go**: 무결성/기동 실패, Excel 리포트 수식·링크·매크로 검출, EDR 차단, 미서명 반입 불허(→ STAB-WP-05 선행으로 전환).

### 11.4 Pilot 업무 시나리오 (docs/20/30 기반)
- 시나리오 1 "아침 한도 점검": 전일·당일 export → 프로파일 → 한도분석(7상태) → 대사 확인 → (v0.8) 전일대비 movers → 리포트 생성 → 캡처 보관.
- 시나리오 2 "SQL 검증 보조": 실무 SQL 초안 검사(차단/경고 확인) → 검증 SQL 제안 수령 → 사람 실행·확인.
- 시나리오 3 "규정 확인 초안": 공개 규정 질의 → 인용·검토필요 표식 확인 → 담당자 판단.
- 각 시나리오 종료 시 History/Audit에서 이력 확인(감사 가능성 시연).

### 11.5 Pilot 인원 / 기간
- **2~5인**(한도 모니터링 1~2, 리포트 1, SQL/VBA 1, NCR/규정 1) · **4~6주** · 주 1회 체크인.

### 11.6 피드백 수집
- 인앱 Feedback(승인형 Example 파이프라인 — 이미 구현) + 주간 설문 양식(Pilot Kit) + 이슈는 GitHub Issue(사내 정책상 불가 시 오프라인 시트).

### 11.7 성공 KPI (파일럿)
| KPI | 목표(안) | 측정 |
|---|---|---|
| 리포트 작성 시간 | 기존 대비 −30% 이상 | 주간 자가 기록 |
| 데이터 품질 이슈 조기 발견 | 파일럿 중 ≥3건(DUPLICATE_LIMIT·RECON_*·인코딩 등) | Audit log + 사용자 보고 |
| 도구-수기 수치 일치율 | 100%(불일치 발생 시 즉시 조사) | 병행 수행 비교 |
| SQL 검사 활용 | 주당 ≥5건 | SuggestionLog/TaskLog 집계(해시 카운트) |
| 보안 사고 | 0건 | EDR/보안팀 |
| 지속 사용 의사 | 참여자 80% 이상 "계속 사용" | 종료 설문 |

---

## 12. Approval Gate Plan

각 게이트: 왜 승인이 필요한가 / 승인 전 가능한 것 / 승인 후 가능한 것 / 필요 증거. (정본: docs/41·docs/51 — 본 §는 실행 관점 요약+보강.)

### 12.1 Code Signing (STAB-WP-05 — docs/51 §B)
- **왜**: 외부 신뢰 루트(인증서)+서명 도구 도입 = STOP 대상. manifest만으로 못 닫는 잔여 3건(lock-step co-tamper·런타임 DLL·폴더 동반 변조)의 유일 폐쇄 수단.
- **승인 전**: 경로 A 확정(완료) 위에서 provenance 4항목(B.4.0) 기입 준비, 정책 문안(§6.2) 검토 — 코드/인증서/도구 추가 0.
- **승인 후**: WP-H 구현(인박스 서명·검증 앵커·회귀 전환), 실 Test PC 서명/차단 증거 → Gate C C5 해소.
- **증거**: 사내 CA 발급 체인 thumbprint·갱신 SOP·보안팀 적합성 확인 → 이후 서명본 기동/차단 캡처.

### 12.2 NCR Rule Pack (docs/51 §C)
- **왜**: 실 계수/공식본 = 내부 통제 대상 정보. 오적재 시 규정 위반 + "공식 산정"으로 오인될 리스크.
- **승인 전**: 계약 로더 보강(WP-I, fail-closed)·placeholder 유지·검토용 초안 표기 불변.
- **승인 후**: Prod 권한통제 KB에 문서오너 관할 적재(repo·ZIP 영구 미포함), 계약 필수항목(ID/Version/Effective/계수 출처/승인이력/Pack Hash/Rollback) 완비본만.
- **증거**: NCR-WP-01 계약 전 항목 기입 + 적재 승인 기록. 미완비=반려.

### 12.3 Internal KB (내부규정 Knowledge Pack)
- **왜**: 내부규정 원문 = repo 절대 미포함 원칙의 핵심 대상. 권한통제(역할·조회로그·보안등급) 없는 적재는 유출 경로.
- **승인 전**: 스키마·접근정책(`KbAccessPolicy`)·가드(현행 VERIFIED) 유지, Prod 측 권한/로그 요건 문서화.
- **승인 후**: 문서오너 승인 문서별 적재(메타 완비: 문서ID·버전·시행일·보안등급·역할권한·대체문서), 조회로그 운영.
- **증거**: 문서별 승인 기록 + 접근권한 매트릭스 + 조회로그 표본.

### 12.4 Local LLM Runtime (docs/51 §A — 2단계)
- **왜**: 인박스로 불가(외부 런타임 필수)=STOP + 성능/환각 실측 없이 채택하면 §11.4 위반. 순환을 2단계로 절단: ①격리 PoC 측정 승인 → ②운영 채택 승인.
- **승인 전**: LLM-WP-01 계약 설계(WP-F, 승인 불요), 후보 매트릭스(Runtime/모델/라이선스) 서류 작성, ADR-009 템플릿 준비(기입 가능 항목 1·2·3·8·9·10·11).
- **승인 후(1단계)**: 격리 PC PoC — 응답시간·환각률·인용준수율·자원 실측 → Package 완성. **(2단계)**: out-of-process Adapter 구현(LLM-WP-02)·Model Pack 반입 SOP. repo에는 어느 단계에도 런타임/모델 미포함.
- **증거**: 채워진 Model Approval Package(전 12항목, 실측치 포함)·라이선스 의무 확인.

### 12.5 Model Pack
- **왜**: 모델 파일 = repo 미포함 원칙 + 반입 무결성(오프라인 전달) 통제 필요.
- **승인 전**: Manifest 스키마(WP-F 산출물)·`model_pack/` 반입 절차 문서 정비.
- **승인 후**: 승인 모델의 Pack 제작(Manifest+Integrity Hash+라이선스 고지)·오프라인 반입·App과 분리 배포(ADR-003·009).
- **증거**: Pack Manifest·해시 대조 기록·라이선스 문서.

### 12.6 Vector/Embedding
- **왜**: 외부 라이브러리/모델 필수 → STOP. 접근통제 관점에서도 embedding 저장소는 원문 파생물(복원 가능성) — 내부규정에 적용 시 KB와 동급 통제 필요.
- **승인 전**: 도입하지 않는다(keyword/inverted index로 충분성 유지 — 현 검색 품질 데이터를 파일럿에서 수집해 필요성부터 입증).
- **승인 후(필요 입증 시)**: 후보 기술 평가 문서 → 별도 승인 → 내부 콘텐츠 적용 시 권한통제 동반.
- **증거**: 파일럿 검색 만족도/실패 질의 로그(해시 집계) → 필요성 근거.

---

## 13. Business Value / Promotion Narrative

**핵심 명제: "리스크관리 업무의 반복 작업을 표준화하고, 사람의 판단을 빠르고 안전하게 만드는 오프라인 Copilot — 자동화가 아니라 내부통제 강화."**

| 가치 축 | 내용 | 근거 기능 |
|---|---|---|
| 시간 절감 | 리포트 조립·수치 대사·SQL 작성/검증의 수작업 축소(목표 −30%) | Excel Report 자동 생성, 대사 9종, SQL Safety Checker+검증 스니펫(8단계 포맷은 템플릿 정본 — 실 초안 생성은 R4 승인 후) |
| 오류 감소 | 사람이 놓치는 유형을 기계가 상시 검사 | DUPLICATE_LIMIT 차단, RECON_* 9종, BASE_DT 정규화, 인코딩 검증, Formula Injection 0 리포트 |
| 표준화 | 개인기 의존 산출물의 팀 표준 포맷화 | SQL 8단계/규정 10단계 포맷, 공통 LimitAnalysisResult(화면=리포트 동일 수치), 승인형 Example |
| 신입 온보딩 | "팀의 방식"이 도구에 내장 | Safety Checker(하지 말아야 할 것), Function Helper(Excel 2021 대체식), 승인 Example 검색 |
| 감사 가능성 | 전 작업 해시 이력·무결성 검증·검토 게이트 | hash-only Audit JSONL, ReferencesReviewed, Fail-Closed manifest |
| 리스크관리 강화 | 모니터링 빈도·심도 상승(같은 인력으로) | 전일대비 movers, HHI 집중도, Heatmap, 7상태 |
| 보고 품질 일관성 | 담당자와 무관하게 동일 품질 | 결정적(deterministic) 산출 설계 전반 |

**사내 혁신 과제 포지셔닝 각도:**
1. **"AI 거버넌스 선도 사례"** — 모델 승인 게이트·STOP 규율·NoModel fallback을 갖춘 채 시작하는 AI 도입: 보안/준법 부서가 반대할 이유를 설계로 제거한 프로젝트라는 서사. 사내 AI 도입 표준 절차의 파일럿으로 제안.
2. **비용 서사** — 외부 API 0·라이선스 0·클라우드 0: 도입 비용이 사실상 인력 시간뿐. 파일럿 KPI로 절감 시간을 실측해 확장 결재 근거로.
3. **확장 서사** — 파일럿(리스크관리팀) 성공 → 인접 부서(준법·재무)로 수평 확장 가능한 구조(KB pack·rule pack이 콘텐츠 교체형).
4. 발표 자료 뼈대: 문제(반복·오류·감사부담) → 원칙(자동실행 0·오프라인·검토용 초안) → 실물 데모(docs/20/30 시나리오) → 통제 장치(게이트·audit) → 파일럿 결과(KPI) → 확장안.

---

## 14. Final Recommendation

### 즉시 (이번 주)
1. **[사용자] Gate B/C Round 1 실행**(WP-A) — docs/48 §B″ §1~4, evidence/gateB/ 봉인. *반나절.*
2. **[사용자→Codex] REL-WP-071 지시**(WP-B) — v0.7.1 컷·발행. *Codex 반나절 + 발행.*
3. **[사용자] docs/51 §D 승인 3건 검토 개시** — 최소한 §B(서명) provenance 4항목부터(사내 CA 정보 수집).

### 2주 내
4. Gate B Round 2(v0.7.1로 B-5 재검증) → Gate B/C 봉인 재판정 → docs/48 갱신.
5. [Claude] v0.8 트랙 WP 정식화(risk-wp-planner): ARCH-WP-01·UI-WP-12·KB-WP-03 순서 확정, Codex 프롬프트 발부. + GOV-WP-01 문서/스킬 정합 스윕.
6. [Claude] LLM-WP-01 설계 초안(승인 불요 범위) 착수 — 승인 시 리드타임 제거.
7. [사용자] GitHub Actions 분 리셋(6/30) 경과에 따른 `ci.yml` 자동 트리거 복원 여부 결정(보조망 — local-gate 정본 모델은 유지 가능).

### 1개월 내
8. v0.8 구현 트랙 진행(MainWindow 분해 → Prior-Day UI → KB pack builder), 매 WP 4축 리뷰·truth-sync.
9. 승인 결정분 실행: 서명 승인 시 WP-H, RAG 게이트 결정 시 실 공개 규정 pack 제작 개시.
10. Pilot Kit 초안(WP-J) 작성.

### v1.0 전 (파일럿 진입 조건)
11. RC 컷 + Gate B/C 재봉인(서명본이면 C5 해소, 아니면 ACCEPTED_RISK 서면).
12. 파일럿 2~5인·4~6주 운영(§11), KPI 실측 → v1.0.0.

### Go/No-Go 의견 (Fable 5)

**GO.** — 단, "지금 그대로 파일럿"이 아니라 **"Gate B/C 봉인 → v0.7.1 → v0.8 분석 UI 완성 → 파일럿"** 경로의 GO다. 이 프로젝트의 리스크는 기술 부채나 보안 설계가 아니라 (i) 실 PC 증거 공백, (ii) 결정 대기 상태의 장기화, (iii) 콘텐츠 공백 3가지이고, 셋 다 **이 제안서의 P0~P1 실행으로 해소 가능**하다. 거버넌스·테스트·릴리스 체계는 v1.0을 지탱하기에 이미 충분한 수준이며, 과대표기 금지 규율이 지켜지는 한 파일럿에서 신뢰를 잃을 요인이 구조적으로 차단되어 있다.

**최종 상태: `READY_FOR_GATE_BC`** — Gate B/C는 사용자 실행만 남았고(런북·워크시트·판정 규칙 완비), 병행으로 v0.8 안전 구현 트랙(WP-B/C/D/E/F/G/J)은 승인 없이 즉시 착수 가능하다. 승인 게이트 3건(docs/51)은 APPROVAL_REQUIRED로 대기하며, 이는 차단이 아니라 **결정 대기**다.

---

> **관련 정본**: `docs/38`(Release Train·Traceability) · `docs/39`(WP 원장) · `docs/40`(ADR) · `docs/41`(게이트 정의) · `docs/47`(v0.7.0) · `docs/48`(Gate B/C 증거·런북) · `docs/50`(핸드오프) · `docs/51`(승인 결정 패킷) · `CLAUDE.md`/`AGENTS.md`/`SKILLS.md`(운영 규율).
> **본 문서 상태**: 제안(검토용 초안) — 채택 여부·순서는 사용자/문서오너 결정. 본 문서는 어떤 게이트도 PASS로 바꾸지 않는다.
