# Risk Management AI Assistant

금융회사 리스크관리 업무를 위한 **오프라인 실행형 Local AI Assistant**.

현재 기준선: **v0.7.0** (main `30c1cfb`, **v0.7.0 정식 릴리스 태그 `30c1cfb`**; 직전 v0.6.0 태그 `3dfa80b`; **R2 Risk Analytics 트랙 완결 + v0.7.0 릴리스 — REL-v0.7.0 #90, 미서명 portable ZIP**). R1 Data & Limit Foundation(완료, v0.5.0) + R3 **공개 규정 Metadata 기반 RAG 구조/NCR Rule Set 구조**(완료, v0.6.0)가 코드/CI 레벨로 완료되고 STAB v0.6.1(WP-01~04 + UX-01/02 Resizable Layout+영속화) + **Smart Assist UX 트랙 완료(UX-WP-01 코어 + UX-WP-02 정적 Provider + UX-WP-03 WPF Popup)** + **R2 Risk Analytics 트랙(Semantic Hardening·Streaming/Welford·Prior-Day·Visualization)** 까지 완료된 상태이며, 실 오프라인 Test PC Gate(B/C)와 다음 트랙(KB/R5 등) 결정이 남아 있습니다. (Smart Assist는 **정적·NoModel·외부 Editor 0·자동삽입 0** 한정이며 실 LLM 랭킹/학습=R4 미구현, 실 Test PC Gate B/C=BLOCKED입니다.) (R3는 공개 catalog/메타데이터 + Keyword/Inverted Index 인용 구조이며 **규정 원문 RAG가 아니고**, NCR은 **Rule Set 8요소 구조일 뿐 실제 NCR 산정이 아닙니다**.)

> 운영환경에는 소스코드·개발도구를 가져가지 않고 **Self-contained Release ZIP**만 반입합니다.

```text
GitHub / 개발 PC      = Dev (설계·구현·빌드·Release ZIP 생성)
Local 실행 PC         = Test (Release ZIP 검증·더미데이터·Gate B)
회사 업무망/개발망 PC  = Prod (승인 Release ZIP 실행 전용·Gate C)
```

---

## 1. 현재 구현 상태 (v0.7.0 정본)

상태 표기: **VERIFIED**(코드+CI 검증) · **PARTIAL** · **SCAFFOLD_ONLY**(구조만) · **PLACEHOLDER** · **BLOCKED**(실 Test PC 증거 대기) · **NOT_IMPLEMENTED** · **APPROVAL_REQUIRED**.

| 기능 | 상태 | 비고 |
|---|---|---|
| SQL/VBA/Excel2021 Safety Checker | VERIFIED | 조회·검사 전용, 자동실행 0 |
| 해시 전용 Audit Log (TaskLog/FeedbackLog/Reader) | VERIFIED | 원문 미저장, SHA256 |
| CP949(UHC 전체)/UTF-8 CSV 입력 | VERIFIED | repo 내장 매핑표(SHA256·LF 고정), NuGet 0 |
| XLSX 입력 (인박스 OOXML, NuGet 0) | VERIFIED | zip 안전상한·관계기반 시트해석 |
| Risk Column Mapping (설정·승인형) | VERIFIED | `config/column_mapping.json`, safe fallback |
| 실 Exposure-Limit Join + 공통 `LimitAnalysisResult` | VERIFIED | 7상태(NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR/**DUPLICATE_LIMIT**) — DUPLICATE_LIMIT은 R2-WP-01(#79) 추가, 중복 Join Key 차단 |
| 대사·예외검증 9종 (`RECON_*`) | VERIFIED | 키스톤=원천합계=분석합계 |
| Dashboard = Excel Report 수치 일원화 | VERIFIED | 단일 AnalysisResult |
| 공개 규정 KB Metadata + Keyword/Inverted Index 검색 | VERIFIED | NuGet 0, Vector/Embedding 미도입(STOP) |
| 인용형 검색 답변 (문서명·버전·시행일·조항·출처·검색기준일·검토필요) | VERIFIED | placeholder 메타 `(확인 필요)` |
| KB 접근정책(`KbAccessPolicy`) + repo/패키지 원문 가드(`KbRepositoryGuard`+build/03) | VERIFIED | 공개 status만 인용, 원문 의심파일 Blocker |
| NCR Rule Set 8요소 **구조** | SCAFFOLD_ONLY | 샘플=placeholder(실 계수 0), 승인 Rule Pack 미적재 |
| 실제 공개 규정 **원문 Clause/Chunk 검색** | NOT_IMPLEMENTED | 현재는 Catalog/Metadata 검색까지 (KB-WP) |
| 승인된 NCR Rule Pack / 내부규정 Knowledge Pack | APPROVAL_REQUIRED | Prod 문서오너 승인 후 권한통제 KB, repo 미포함 |
| 대용량 CSV Streaming / 행·바이트 상한 / Welford 누산 + 정확 Outlier parity / 중복행 해시 | VERIFIED | R2-WP-02(#81, local-gate); bounded streaming + legacy-compatible outlier 2-pass, 기존 결정성·`DuplicateRowCount` 보존, NuGet 0, 실 Test PC Gate B/C BLOCKED |
| 전일 대비 분석 (Prior-Day: Current/Prev/Δ·movers·4구획 계약) | VERIFIED | R2-WP-03(#84, local-gate); `LimitMonitor.Analyze` 2회 diff·검토용 초안·결정적, 실 Test PC Gate B/C BLOCKED |
| 인박스 시각화(RISK_VISUAL 시트: 7상태 분포·TopN·집중도 HHI·Heatmap) · 정확 Exception Count · WPF Shapes 화면차트 | VERIFIED | R2-WP-04(#87, local-gate); 외부 charting NuGet 0·결정적·`Abs(Exposure)` HHI 분모, 실 Test PC Gate B/C(Excel 수동열기 포함) BLOCKED |
| Local LLM Runtime | APPROVAL_REQUIRED | NoModelMode 유지, Adapter 계약만(R4) |
| 승인형 Example 승격(Feedback Learning) | PARTIAL | R5: 승인형 예제 승격 구조 존재, 영속/검색/재사용/Audit 확장 필요 |
| 오프라인 Test PC Gate B/C · Team Pilot | BLOCKED | 실 Test PC 증거 대기(`docs/44`·`docs/45`) |

**SmokeTest**: **Total=714 PASS=714 FAIL=0** (정본 합계 — R2-WP-04(#87, `35e6f01`) local-gate + **REL-v0.7.0(#90, `30c1cfb`) 버전 범프 후 합계 불변**: `dotnet build` 0/0 + SmokeTest, 2026-06-30; 631 + 레이아웃 영속화 +15 + R2-WP-01 +25 + R2-WP-02 +9 + R2-WP-03 +18 + R2-WP-04 Visualization/Report +16). 과거 484/502/513/572/579/602/631/646/671/680/698 등은 이전 베이스라인/중간값이며 현재 정본 수치가 아닙니다.

---

## 2. 절대 원칙 (전 릴리스 유지)

Offline First · 외부 NuGet PackageReference **0** · 외부 API/Telemetry/AutoUpdate **0** · SQL/VBA/Golden6 자동실행 **0** · 해시 기반 Audit Log · **NoModelMode 유지** · 실데이터/실 테이블·컬럼명/내부규정 원문/NCR 공식본 원문/모델파일·Runtime **repo 미포함** · 모델 가중치 자동학습 금지 · **기존 테스트 삭제·약화 금지** · 운영환경은 Portable Release ZIP 실행 전용.

> 외부 라이브러리·Vector DB·Embedding Runtime·Local LLM Runtime·모델파일·추가 NuGet이 필요해지는 순간 **구현 STOP** → 승인 문서 작성 후에만 진행(`docs/41`·`docs/40`).

---

## 3. 배포 모델

```text
artifacts/release/RiskManagementAI-v0.7.0-win-x64-portable.zip(.sha256)
artifacts/release/ReleaseNote-v0.7.0.md / DependencyList-v0.7.0.csv
```
운영환경 PC에는 Windows 11 + Excel 2021 + 승인된 Release ZIP만 필요합니다(.NET SDK·Python·Git·NuGet·인터넷 불필요).

**왜 C#/.NET/WPF인가**: 운영환경이 실행 전용이라 별도 도구 없이 동작하는 self-contained win-x64 portable이 필요. Python 계열은 의존성 복원·패키징·백신 오탐·외부 라이브러리 보안검토 부담으로 반입성이 불리. (근거: `docs/11`, `docs/40`)

---

## 4. 빌드 (Dev / Windows + .NET 8 SDK)

```powershell
git clone https://github.com/esyim-git/AI-assistant-for-Risk-Management.git
cd AI-assistant-for-Risk-Management
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests   # ALL PASS 확인

Get-Content VERSION                                # -> 0.7.0
./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1  -Version 0.7.0
./build/02_package-release.ps1  -Version 0.7.0
./build/03_verify-package.ps1   -Version 0.7.0     # 해시·금지파일·원문 미포함 스캔
```
> 태그·GitHub Release 발행은 로컬에서(`git tag v0.7.0 30c1cfb`). 웹/Linux 세션 git proxy는 태그 push 403.
> VERSION 단일 원천화 + Release 재현성은 STAB-WP-01(완료, `docs/39`)에서 처리되어 `build/01~03`는 `VERSION` 파일(`0.7.0`)을 단일 원천으로 사용합니다.

---

## 5. 운영환경 실행 (Prod)

```text
1. Release ZIP 반입 → 2. SHA256 확인 → 3. 백신/EDR → 4. 압축 해제 → 5. run.bat / RiskManagementAI.exe
```
세부: `docs/22`~`docs/25`. 게이트: `docs/41`(Approval/Pilot), 증거: `docs/44`(v0.5), `docs/45`(v0.6 Gate B/C).

---

## 6. Claude / Codex 역할 (v0.7.0 이후)

```text
Claude Code  = Architecture Lead / Program Manager / Security & Release Reviewer
             - 큰 그림·문서·ADR·Work Package·Codex Prompt·코드 리뷰·Traceability·다음 WP 지정
             - main 직접 수정/병합 금지. 작업 브랜치: planning/*

Codex        = Implementation / Test Engineer
             - Claude가 작성한 WP 1개만 Feature Branch에서 구현·테스트
             - build + SmokeTest + Gate A + Self Review → 보고 → Claude 승인 전 Merge 금지
```

작업 루프: **Claude Planning → Codex Implementation → Claude Review → Codex Fix → Claude Final Gate → PR → 다음 WP**.

지침/계획 문서:
```text
CLAUDE.md   : Claude 헌법 + 역할/워크플로
AGENTS.md   : Codex 구현 규칙 (현재 우선 = docs/38·39 + 지정 WP)
docs/38     : v1.0 Master Roadmap & Release Train (현재화)
docs/39     : Work Package Backlog + Resume Brief(NEXT UP)
docs/40     : ADR (아키텍처 결정)
docs/41/44/45: 게이트·Gate 증거
prompts/codex/<WP-ID>_*.md : WP별 Codex Prompt
```

---

## 7. 보안 원칙 (Repository 미포함)

```text
회사 실데이터 · 실제 테이블/컬럼 사전 · 내부규정 원문 · NCR 공식본 원문
계정정보 · 비밀번호 · 토큰 · 모델파일/Runtime · 고객정보 · 운영 로그 원문
```
Repository는 **공개자료/더미데이터/구조/룰/템플릿/문서/소스코드**만 관리합니다.

---

## 8. 다음 단계

- **STAB v0.6.1 / UX**: STAB WP-01~04 + UX-01/02(Resizable Layout+영속화) + **Smart Assist UX 트랙 완료(UX-WP-01 코어 + UX-WP-02 정적 Provider + UX-WP-03 WPF Popup)** 완료(#56·#57·#59·#61·#66·#68·#70·#72·#73·#76). 잔여 = STAB-WP-05(Authenticode 코드서명) = **APPROVAL_REQUIRED**.
- **R2-WP-01 DONE(#79, local-gate)**: Risk Semantic Hardening — 중복 Limit Key `DUPLICATE_LIMIT` 상태화(7번째 상태, ADD-ONLY)·통화/단위 ColumnMapping 일원화·`RECON_UNIT_MISMATCH` 활성·BASE_DT 검증/정규화·Join Audit. 인박스 NuGet 0, SmokeTest 646→671.
- **R2-WP-02 DONE(#81, local-gate)**: Streaming/Perf — 대용량 CSV streaming 리딩·행(`MaxRowCount=200K`)/바이트(`MaxByteSize=50MB`) 상한·Welford 누산(전 값 미보관) + 정확 OutlierCount(legacy-compatible 2차 streaming pass, 기존과 동일)·중복행 SHA256 해시. CP949는 repo 내장 `Cp949Decoder` 재사용(결정성 보존). 인박스 NuGet 0, SmokeTest 671→680.
- **R2-WP-03 DONE(#84, local-gate)**: Prior-Day Analytics — `LimitMonitor.Analyze`를 Current/Prior 2회 호출해 diff(새 엔진 0). 행별 Current/Prev/Δ·New/Resolved/Increased/Decreased/Unchanged/StateTransition·TopN movers·4구획 출력 계약(검토용 초안). 동일 Join Key 다중 행은 `PRIOR_DAY_DUPLICATE_KEY` Hidden-Risk. 정규화 same-day guard. R1 7상태/계약 보존, NuGet 0, SmokeTest 680→698.
- **R2-WP-04 DONE(#87, local-gate)**: Visualization/Report — 정확 Exception Count(부정확 COUNTA 제거, Number SoT)·신규 `RISK_VISUAL` 인박스 시트(7상태 분포·TopN by `Abs(Exposure)`·집중도 HHI·Heatmap 등급·MIXED_CURRENCY/zero-denom finding)·WPF Shapes/Canvas 화면차트(동일 aggregator SoT). **외부 charting NuGet 0**, 결정적. 시각화 caveat는 `ExcelReportResult.Findings`로 표면화. SmokeTest 698→714. 실 Test PC Gate B/C(Excel 2021 수동열기 포함) BLOCKED. **→ R2 분석 트랙(v0.7.0) 완결.**
- **NEXT UP**: **(트랙 결정 대기)** — REL-v0.7.0(#90)로 v0.7.0 정식 릴리스 완료. 후보: ① STAB-WP-05 코드서명(승인 문서 ADR-012/`docs/41 §6` READY, **사용자 인증서 경로 A~D 결정 선행**) · ② KB 원문 Clause/Chunk 검색(v0.8, Vector/Embedding STOP·승인 선행) · ③ R5 Feedback 확장 · ④ 실 Test PC Gate B/C 증거. 사용자 결정 전 신규 구현 WP 임의 착수 금지.
- 로드맵 v0.6.1~v1.0: `docs/38` · WP 백로그: `docs/39`(NEXT UP은 Resume Brief 참조).
- 실 Test PC Gate B/C는 신규 기능과 분리하여 우선 추진(현재 BLOCKED, 증거 대기).
