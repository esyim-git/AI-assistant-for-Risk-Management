# 53. Repository Audit and v1 Execution Plan

## 0. 판정

이 저장소의 최종 목적은 **회사 업무망에서 인터넷·SDK·외부 API 없이 portable ZIP 하나로 실행되고, 사람이 검토한 뒤 사용하는 리스크관리 Copilot**이다. Golden6 수동 Export(CSV/XLSX)를 입력으로 데이터 품질·Exposure-Limit·대사·전일대비·Excel 2021 리포트를 제공하고, SQL/VBA 안전검사·공개 규정 검색·승인형 예제·해시 감사 이력을 한 화면에서 다룬다.

자동 SQL/VBA 실행, 공식 법규 해석, 공식 NCR 산정, 무승인 모델 추론, 가중치 자동학습은 최종 목적이 아니다. 이 경계는 유지한다.

**현재 종합 상태 = `PARTIAL`.** v0.7.1 final rebuild/tag/Release와 CORR-WP-01 반영은 완료됐지만, (1) 여러 VERIFIED Core 기능이 WPF에서 도달 불가하며, (2) formal Gate B/C 증거와 Team Pilot이 닫히지 않았고, (3) branch protection/secret scanning이 아직 닫히지 않았다.

---

## 1. 2026-07-10 증거 스냅샷

| 항목 | 실제 상태 | 근거 |
|---|---|---|
| v0.7.1 Build Commit | `fa7552567cb432ec6a4afe9900b3eca480fc5780` | PR #136 docs-only merge; tag `v0.7.1` exact target. Current main may advance via later docs/workflow merges |
| 코드·테스트 baseline | `4efb8e670ce0306d07683d3fbc5ed7b118844b8b` | PR #135 product code/test merge; later docs-only main does not move it |
| VERSION | `0.7.1` | `VERSION`, `IntegrityVerifier.ExpectedVersion`, PackagingTests 락스텝 |
| 최신 공개 Release | unsigned `v0.7.1` | GitHub Release `https://github.com/esyim-git/AI-assistant-for-Risk-Management/releases/tag/v0.7.1` |
| 빌드 | warning 0 / error 0 | `dotnet build RiskManagementAI.sln -c Release` |
| SmokeTest | `Total=907 PASS=907 FAIL=0` | #135 로컬 재실행, Unclassified 0 |
| v0.7.1 package | `VERIFIED` (published) | build/00~03 PASS; SHA256 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`; manifest 27/27 |
| 폐기된 pre-CORR ZIP SHA256 | `A70D0B37AD92344A2ECFBE0D4D96360F56CBAFFF94363249F0BD1A20ADC1ECDC` | Build Commit `abab29b`; #135 merge로 무효, 발행 금지 |
| 외부 NuGet | 0 | `NuGet.Config <clear/>`, csproj `PackageReference` 0 |
| GitHub | public, squash-only, delete-on-merge | GitHub REST API |
| Hosted CI | PR 자동 trigger 복원, #135 run #210 `test`/`wpf-build` success | #134 workflow merge + #135 exact-head run |
| main 보호 | 미적용 | Branch protection API 404, ruleset 0 |
| Secret scanning | disabled | Repository `security_and_analysis` API |
| Gate B/C | v0.7.1 formal `BLOCKED` | `docs/54` 신규 라운드. v0.7.0 user-reported 이력은 `docs/48`에만 보존 |

> v0.7.1 ZIP은 published artifact다. 이후 main 변경은 이 immutable release의 Build Commit/code-test baseline을 소급 변경하지 않는다.

---

## 2. 기능 도달성

`VERIFIED`는 Core 테스트 통과와 사용자 화면 도달을 분리해 적는다.

| Capability | Core | WPF 사용자 경로 | 공개 출하본 | 남은 일 |
|---|---|---|---|---|
| SQL/VBA/Excel 안전검사 | VERIFIED | VERIFIED | v0.7.0 포함 | Gate B formal 증거 |
| CSV/CP949 프로파일 | VERIFIED | PARTIAL: `ProfileCsv` in-memory만 | v0.7.0 포함 | streaming 선택/진행상태/취소 배선 |
| XLSX 프로파일 | VERIFIED (`ProfileTable(XlsxReader.Read)`) | NOT_IMPLEMENTED | 라이브러리는 포함 | Data UI 입력 형식 라우팅 |
| Current-day Limit/7상태/대사 | VERIFIED | VERIFIED(CSV/XLSX 경로 허용, 라벨은 CSV) | v0.7.0 포함 | 입력 UX·Gate B evidence |
| Prior-Day Analytics | VERIFIED | NOT_IMPLEMENTED: App call site 0 | v0.7.1 포함 | UI-WP-12 |
| RISK_VISUAL/Excel Report | VERIFIED | VERIFIED | v0.7.0 포함 | B/C formal evidence |
| KB catalog 검색 | VERIFIED | VERIFIED | v0.7.0 포함 | 실 공개 metadata 갱신 절차 |
| Clause 검색/발췌 게이트 | VERIFIED | NOT_IMPLEMENTED: `SearchClauses` call site 0 | v0.7.1 포함 | KB-UI-WP-01 + 승인 Pack |
| Feedback 승인 승격 | VERIFIED | VERIFIED | v0.7.1 포함 | formal Gate B |
| 승인 Example 검색/Prompt reflection | VERIFIED | NOT_IMPLEMENTED: Retriever/ReferencesReviewed UI call site 0 | v0.7.1 포함 | FEEDBACK-WP-03 |
| Local LLM | PLACEHOLDER/NoModel | NoModelMode VERIFIED | 모델 없음 | `APPROVAL_REQUIRED`, STOP |
| NCR 실 산정 | SCAFFOLD_ONLY | 구조 설명만 | placeholder | 실 Rule Pack `APPROVAL_REQUIRED` |

핵심 결론: **코드 완성도보다 사용자 도달성이 뒤처져 있다.** v0.8은 새 엔진보다 기존 Core 기능의 WPF 배선에 집중한다.

---

## 3. 확인된 결함과 리스크

### P0 — 수정 후 릴리스

1. **CORR-01: VERIFIED mitigation (#135 `4efb8e6`).**
   - `CheckCount == 0`은 `NOT_RUN`; nonzero checks만 기존 `Passed`에 따라 PASS/FAIL이다.
   - empty UI path는 `Passed=false, CheckCount=0`으로 정합했고 `LIMIT_DATA_REQUIRED` 노출을 유지했다.
   - Report/UiContract additive 회귀 +7, `Total=907 PASS=907 FAIL=0`; 공개 계약·7상태·RECON 불변.

2. **REL-01: CLOSED.**
   - Exact main `fa755256`에서 build/00~03을 재실행하고 v0.7.1 tag/Latest Release를 발행했다. ZIP SHA256은 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`; pre-CORR 후보는 발행되지 않았다.

### P1 — 거버넌스/사용자 가치

3. **GOV-01: public 저장소인데 main protection/ruleset이 없다.** 기존 문서는 private Free 전제를 유지하며 실제 check 이름도 존재하지 않는 `build`로 적혀 있다.
4. **GOV-02: PARTIAL mitigation.** #134가 PR `test`/`wpf-build` 트리거를 복원하고 Action을 immutable SHA로 pin했으며 #135 run #210이 첫 hosted green을 제공했다. hard protection 적용 전까지 `PARTIAL`; local-gate는 독립 검증으로 유지한다.
5. **SEC-01: GitHub secret scanning/push protection이 disabled다.** Gate A는 유지하되 public 저장소의 원격 방어층을 추가한다.
6. **ARCH-01: `MainWindow.xaml.cs` 1,614줄 God-class.** 12개 탭, 차트, persistence, DTO가 한 파일에 집중돼 UI 배선 WP의 충돌 위험을 높인다.
7. **UX-REACH-01: Prior-Day/streaming/XLSX profile/Clause/Example reflection이 Core-only다.** 문서에서 UI 기능처럼 읽히지 않도록 분리 표기하고 v0.8에서 순차 배선한다.

### P2 — 운영수명/품질

8. **REL-02: 생성 ReleaseNote가 `verify CI`를 지시한다.** 현재 정본은 local-gate이므로 build/02 문구를 실제 로컬 build/smoke 증거 경로와 맞춘다.
9. **OPS-01: 주요 파일 I/O가 WPF UI thread에서 동기 실행된다.** 50MB/200k행 경계에서는 응답없음 체감 위험이 있다. 취소/진행상태를 포함한 별도 UI WP로 처리한다.
10. **RUNTIME-01: .NET 8 지원 종료가 2026-11-10이다.** .NET 10 LTS 전환을 v1.0 Pilot 이전 별도 WP로 수행한다. 기능 WP와 섞지 않는다.
11. **PATH-01: mutable state 루트가 여러 곳에서 `Environment.CurrentDirectory`에 분산돼 있다.** portable `run.bat`에서는 정상이나 직접 실행/외부 launcher의 CWD가 다르면 logs/reports/config 위치가 달라질 수 있다. 공통 주입형 root resolver를 장기 하드닝 후보로 둔다.

---

## 4. 보완 로드맵

### Phase 0 — v0.7.1 봉인

1. **CORR-WP-01 VERIFIED**: #135 `4efb8e6`, Reconciliation `NOT_RUN`, additive SmokeTest +7.
2. **REL-WP-071 VERIFIED** (published): tag/Build Commit `fa755256`, unsigned ZIP SHA256 `282B7138...9018`, manifest 27/27.
3. **NEXT UP = GOV-WP-02**: hosted green 증거를 정본화하고 실제 check 이름으로 보호 설정. User-driven published v0.7.1 Gate B/C는 병행한다.
4. main hard protection 단계 적용: PR 필수·conversation resolution·linear history·force/deletion off. 단일 계정 self-review 교착 때문에 승인 1/Code Owner 강제는 독립 reviewer가 생긴 뒤 켠다.
5. Secret scanning + push protection 활성화.

### Phase 1 — v0.8 사용자 가치

1. **ARCH-WP-01**: MainWindow 행위 불변 분해. MVVM 전면 전환은 하지 않는다.
2. **UI-WP-12**: Prior-Day Current/Prev/Delta, movers, 4구획, report 연동.
3. **DATA-UI-WP-01**: CSV streaming/XLSX profile 라우팅, 진행상태·취소.
4. **KB-UI-WP-01**: Clause search UI, catalog fallback과 disclosure를 그대로 노출.
5. **FEEDBACK-WP-03**: 승인 Example 검색·사용자 검토·`ReferencesReviewed` 반영 UI.

### Phase 2 — v0.9 운영수명/승인 준비

1. **RUNTIME-WP-01**: .NET 10 LTS migration, self-contained/package/Gate B 회귀.
2. **KB-WP-03**: repo 밖 공개 Clause Pack builder/validator. 실제 Pack 적재는 RAG 승인 후.
3. **DOC-WP-PILOT**: 비개발자 Quick Start, Known Limitations, rollback/incident sheet.
4. STAB-WP-05, NCR 실 Pack, LLM runtime은 각각 승인 조건 충족 시만 착수.

### Phase 3 — v1.0 Team Pilot

Gate B/C formal evidence 봉인 → RC → 2~5인 4~6주 병행 사용 → 정확성·시간절감·재작업·보안사고 KPI → Go/No-Go. 실 증거 없이 Pilot 완료/성공을 표기하지 않는다.

---

## 5. 목표 달성 기준

| 축 | 현재 | v1.0 완료 조건 |
|---|---|---|
| Offline/Safety foundation | VERIFIED | 회귀 0, NuGet/API/auto-execute 0 |
| 데이터·리포트 정확성 | PARTIAL (CORR-01 해소) | UI=Report + Gate B/C formal |
| 사용자 도달성 | PARTIAL | Core-only 4기능 WPF 배선 및 실 UI 증거 |
| 규정/NCR | PARTIAL/SCAFFOLD_ONLY | 승인 공개 Pack 또는 명확한 placeholder 유지; 공식 과대표기 0 |
| Release governance | PARTIAL | published tag/SHA와 CI checks는 충족; hard branch protection·secret scanning 잔여 |
| Runtime support | PARTIAL | Pilot 시점 지원 중 LTS(.NET 10 권고) |
| Team Pilot | BLOCKED | formal Gate B/C + 운영 KPI + rollback evidence |

최종 제품 성공은 “LLM이 들어갔다”가 아니라 **수치가 맞고, 사람이 검토할 수 있고, 감사 가능하며, 오프라인 업무망에서 반복 사용되는가**로 판정한다.

---

## 6. 공식 외부 기준

- GitHub public repository standard hosted runners는 무료: <https://docs.github.com/en/billing/concepts/product-billing/github-actions>
- Public GitHub Free 저장소는 protected branch 사용 가능: <https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches>
- .NET 8 지원 종료 2026-11-10, .NET 10 LTS 지원 종료 2028-11-14: <https://dotnet.microsoft.com/en-us/platform/support/policy>

> 연계 정본: `docs/38`(release/capability), `docs/39`(WP/NEXT UP), `docs/40`(ADR), `docs/41`(승인/Pilot), `docs/54`(v0.7.1 Gate evidence), `docs/48`(v0.7.0 historical evidence), `docs/52`(v0.7.1), `.claude/skills/risk-repo-audit/`.
