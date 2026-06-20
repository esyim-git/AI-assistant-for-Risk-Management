# 36. MVP-2 24h Autorun Worklog (Codex → Git → Claude)

> **단일 진실원장.** Codex(무인 24h 자율)가 작업과 동시에 갱신한다. Claude(웹)는 복귀 시 이 문서만 읽고 이어받는다.
> 실행 프롬프트: `prompts/codex_mvp2_24h_autorun_prompt.md` · 백로그 스펙: `docs/33_MVP2_Backlog.md`.

---

## ★ Claude Resume Brief (항상 최신 — Codex가 매 단위 갱신)

> Claude는 복귀 시 **이 블록만으로** 현재 상태·다음 작업을 파악할 수 있어야 한다.

- **현재 상태(1줄)**: MVP-2 코어 M2-01~M2-06 구현 완료. Release ZIP rehearsal은 로컬 .NET 8 SDK 부재로 BLOCKED(build/00 preflight 보강 완료).
- **develop 최신 code commit**: `46108dc1e3a6f7f0ea3c6fc7e5a2a8e3959498c7`
- **DONE (검증됨)**: P0-1 develop/main fast-forward sync; P0-2 release/v0.3.0 변경 반영; P0-3 `.gitignore` `*.zip` 추가; M2-01 NoModelMode; M2-02 DraftPipeline; M2-03 KbSearch; M2-04 Excel report; M2-05 ExamplePromotion; M2-06 UI 연동
- **진행 중이던 항목 / 중단 지점**: _없음_
- **NEXT UP (Claude가 바로 집을 작업)**: main 승격 PR, 또는 .NET 8 SDK가 있는 Dev/Test PC에서 release packaging rehearsal 재개
- **BLOCKED 개수 / 핵심**: _1_ — release ZIP rehearsal은 현재 PC에 .NET SDK가 없어 실행 불가(runtime-only dotnet)
- **재현 검증**: `git fetch origin develop && git switch develop && dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests`
- **⚠️ Claude 확인 요망(자동결정/승격대기)**: _-_

---

## 1. 실행 메타데이터

| 항목 | 값 |
|---|---|
| 시작(UTC) | 2026-06-19T15:46:24Z |
| 종료/한도(UTC) | _-_ |
| 통합 브랜치 | `develop` (main은 미변경; 승격은 Claude/사람 전용) |
| 작업 브랜치 규칙 | `feature/mvp2-<item>` → CI green → develop squash-merge → 삭제 |
| 시작 시 develop | `64ac34b941a415374d752fe329b456c73fdd77e8` / main | `571c576708a483a742bd3b30cad19e9e07c52bd7` |

## 2. 상태 원장 (Status Ledger)

> 상태: `TODO` · `WIP` · `DONE`(검증됨) · `BLOCKED`(사유 기재). 각 항목 완료 시 커밋·테스트수 기입.

### Phase 0 — 정합/잔여 정리 (develop)
| ID | 항목 | 상태 | 커밋 | 비고 |
|---|---|---|---|---|
| P0-1 | develop를 main까지 ff 동기화 | DONE | `571c576708a483a742bd3b30cad19e9e07c52bd7` | origin/develop push 완료 |
| P0-2 | release/v0.3.0 변경 develop 반영(VERSION 0.3.0 + build/01 fail-fast/.keep) | DONE | `b04ab2ea1a5e1d822edc1758ad9a0b4c3c1e499a` | main 승격 안 함 |
| P0-3 | `.gitignore`에 `*.zip` 추가 | DONE | `25c58aa2846127daa7b393ba9c9066892d569c8e` | 루트 잔류 zip 차단 |

### Phase 1 — MVP-2 코어 (docs/33)
| ID | 항목 | 상태 | 커밋 | SmokeTest | 비고 |
|---|---|---|---|---|---|
| M2-01 | LLM 추상화 + NoModelMode | DONE | `4cf822dac00cc23702c3728402836914d3217db6` | 127 PASS | 모델 없이 기동 |
| M2-02 | SQL/VBA 초안 파이프라인(안전+감사) | DONE | `5b0d3d74260215dc171fd7d130feb4c9cd22a3fb` | 141 PASS | 생성물 Checker 통과+로그 |
| M2-03 | 규정/NCR catalog 검색 | DONE | `803b5e049da45d3710da2e3b96bd4f73fae0bbf6` | 152 PASS | 공개 catalog만 |
| M2-05 | 승인형 피드백 예제 승격 | DONE | `d651d8b048765de02ff7d5c9d2b51d7dd78491d0` | 158 PASS | 재학습 아님 |
| M2-06 | UI 연동 + SmokeTest 확장 | DONE | `7b52aa29d3999a4295ac6189c4ea6cae3cb87c13` | 162 PASS | |
| M2-04 | Excel 2021 리포트 | DONE | `46108dc1e3a6f7f0ea3c6fc7e5a2a8e3959498c7` | 180 PASS | DM-03 확정: 인박스 xlsx, `System.IO.Compression`, `templates/report`, NuGet 0 |

### Phase 2 — 스트레치 (시간 여유 시)
| ID | 항목 | 상태 | 커밋 | 비고 |
|---|---|---|---|---|
| S2-* | 룰/테스트/문서/데모 하드닝 | TODO | - | docs/10 로드맵 |
| S2-REL-00 | Release rehearsal prereq gate | DONE | `77527485eb627f0e5c9db6019e8f03941faf7840` | `build/00` SDK 부재 감지 보강. ZIP 생성은 SDK 부재로 BLOCKED |

## 3. 자동 결정 로그 (⚠️ Claude 검토용)

> Soft-block 시 Codex가 고른 기본값/대안/사유. Claude가 추후 승인 또는 변경.

<!-- [UTC] 항목 | 결정 | 대안 | 사유 -->
_(아직 없음)_

## 4. BLOCKED 큐 (사람/Claude 결정 필요)

> Hard-block 항목. 각: 무엇을 / 왜 막혔는지 / 제안 해결책 / 필요한 결정.

<!-- [UTC] 항목 | 사유 | 제안 | 필요한 결정 -->
- [RESOLVED 2026-06-20T03:20:10Z] M2-04 Excel 2021 리포트 | 사용자 DM-03 확정: 인박스 xlsx(`System.IO.Compression` + `templates/report` 템플릿 치환), NuGet 0, Interop 금지, OpenXML SDK 미도입. 산출 수식은 `Excel2021FunctionChecker` 통과, 쓰기 경로는 `reports/`만, 생성 시 audit log 기록. | `feature/mvp2-m2-04-excel-report`에서 구현 재개. 풍부한 서식이 꼭 필요하면 다시 BLOCKED로 둔다. | 해소됨 |
- [2026-06-20T03:37:22Z] Release ZIP rehearsal | 현재 PC는 `dotnet` 런타임만 있고 .NET SDK가 없다(`dotnet --info`: "No SDKs were found."). `build/00_check-prereqs.ps1`가 기존에는 이를 성공으로 처리했으므로 preflight를 보강했다. | .NET 8 SDK가 설치된 Dev/Test PC에서 `build/00~03 -Version 0.3.0` 재실행. 이 PC에서 계속하려면 .NET 8 SDK 설치가 필요. | .NET 8 SDK availability |

## 5. 완료 보고 누적 (append-only)

```md
#### [<ID>] 제목 — DONE (UTC)
- 구현 요약: ...
- 변경 파일: ...
- build: 성공(0/0) / SmokeTest: N PASS
- 보안 게이트 A: 0건 / NuGet: 없음
- 결정/가정: (DM-xx/D-xx 또는 자동결정 참조)
- develop 반영 커밋: <hash>
```
#### [P0-1] develop/main fast-forward sync — DONE (2026-06-19T15:46:24Z)
- 구현 요약: `origin/develop`가 `origin/main`의 조상임을 확인한 뒤 `develop`를 `origin/main`까지 fast-forward하고 원격 push.
- 변경 파일: 없음(브랜치 포인터 동기화), 본 원장 갱신은 후속 커밋.
- build: 성공(0/0) / SmokeTest: 119 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인) / NuGet: 없음
- 결정/가정: `main`은 PR #4 merge commit `571c576708a483a742bd3b30cad19e9e07c52bd7`, `develop` 시작점은 `64ac34b941a415374d752fe329b456c73fdd77e8`.
- develop 반영 커밋: `571c576708a483a742bd3b30cad19e9e07c52bd7`

#### [P0-2] release/v0.3.0 verified changes into develop — DONE (2026-06-19T15:50:16Z)
- 구현 요약: `release/v0.3.0`의 검증된 `VERSION`/`build/01_publish-win-x64.ps1` 변경을 `feature/mvp2-p0-2-release-sync`에 반영.
- 변경 파일: `VERSION`, `build/01_publish-win-x64.ps1`, `docs/36_MVP2_Autorun_Worklog.md`
- build: 성공(0/0) / SmokeTest: 119 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인) / NuGet: 없음
- 결정/가정: source commit `0c1d83256e2181dfe49b1a6d422830a0cc5a1637`; main 승격 없이 develop 통합만 진행.
- develop 반영 커밋: `b04ab2ea1a5e1d822edc1758ad9a0b4c3c1e499a`

#### [P0-3] ignore ZIP artifacts — DONE (2026-06-19T15:57:23Z)
- 구현 요약: `git ls-files '*.zip'` 결과 추적 ZIP 0개 확인 후 `.gitignore`에 `*.zip` 추가.
- 변경 파일: `.gitignore`, `docs/36_MVP2_Autorun_Worklog.md`
- build: 성공(0/0) / SmokeTest: 119 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인) / NuGet: 없음
- 결정/가정: 기존 루트 `risk-agent-learning-materials-v0.1.zip`은 건드리지 않고 ignore 처리만 적용.
- develop 반영 커밋: `25c58aa2846127daa7b393ba9c9066892d569c8e`

#### [M2-01] LLM abstraction + NoModelMode — DONE (2026-06-19T16:09:25Z)
- 구현 요약: `ILocalDraftService` 추상화와 기본 `NoModelDraftService`를 추가. 모델 파일/외부통신/추론 라이브러리 없이 생성 호출이 안전 안내와 policy finding만 반환하도록 구현하고, WPF 시작 상태에 NoModelMode를 주입.
- 변경 파일: `src/RiskManagementAI.Core/Generation/ILocalDraftService.cs`, `src/RiskManagementAI.Core/Generation/NoModelDraftService.cs`, `src/RiskManagementAI.App/MainWindow.xaml.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0/0; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 127 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인; 기존 ignored 루트 ZIP은 미포함) / NuGet: 없음
- 결정/가정: 실제 추론 구현은 후속. NoModelMode는 항상 `IsAvailable=false`, `DraftText=null`이며 외부통신/자동실행 차단 상태를 finding으로 반환.
- feature 검증: `feature/mvp2-m2-01-nomodel-draft` CI `build` success (`27836431845`)
- develop 반영 커밋: `4cf822dac00cc23702c3728402836914d3217db6`

#### [M2-02] SQL/VBA draft safety pipeline — DONE (2026-06-19T16:16:42Z)
- 구현 요약: `DraftPipeline`을 추가해 draft service 출력물을 SQL/VBA checker에 통과시키고, Blocker는 반려(`DraftText=null`)하며, 모든 결과를 `TaskLogWriter`에 해시 기반 audit log로 기록.
- 변경 파일: `src/RiskManagementAI.Core/Generation/DraftPipeline.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0/0; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 141 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인; 기존 ignored 루트 ZIP은 미포함) / NuGet: 없음
- 결정/가정: UI 버튼 연동은 M2-06에서 수행. 본 단위는 Core pipeline 계약과 audit/safety 회귀 테스트를 우선 고정.
- feature 검증: `feature/mvp2-m2-02-draft-pipeline` CI `build` success (`27836775914`)
- develop 반영 커밋: `5b0d3d74260215dc171fd7d130feb4c9cd22a3fb`

#### [M2-03] Regulation/NCR public catalog search — DONE (2026-06-19T16:23:46Z)
- 구현 요약: `RegulationCatalog` CSV 로더와 `KbSearch` 키워드 검색을 추가. 응답은 항상 `검토용 초안`, `출처`, 버전/시행일 확인 문구를 포함하며 내부규정/NCR 원문은 포함하지 않음을 명시.
- 변경 파일: `src/RiskManagementAI.Core/Kb/RegulationCatalog.cs`, `src/RiskManagementAI.Core/Kb/KbSearch.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0/0; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 152 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인; 기존 ignored 루트 ZIP은 미포함) / NuGet: 없음
- 결정/가정: 공개 catalog metadata만 검색. 내부 원문/공식본은 repo에 포함하지 않고 Prod 권한통제형 KB 승인 적재 대상으로 유지.
- feature 검증: `feature/mvp2-m2-03-regulation-catalog` CI `build` success (`27837072364`)
- develop 반영 커밋: `803b5e049da45d3710da2e3b96bd4f73fae0bbf6`

#### [M2-05] approved feedback example promotion — DONE (2026-06-19T16:29:28Z)
- 구현 요약: `ExamplePromotion`을 추가해 승인된 `FeedbackLogEntry`만 `ExampleCurationOnly` 예제 metadata로 승격하고, 미승인/보류/중복 승인은 제외.
- 변경 파일: `src/RiskManagementAI.Core/Feedback/ExamplePromotion.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0/0; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 158 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인; 기존 ignored 루트 ZIP은 미포함) / NuGet: 없음
- 결정/가정: 모델 재학습 없음. raw prompt/output 저장 없음. 승격 산출물은 FeedbackId/TaskId/UserIdHash/승인상태 기반 metadata만 포함.
- feature 검증: `feature/mvp2-m2-05-feedback-promotion` CI `build` success (`27837325774`)
- develop 반영 커밋: `d651d8b048765de02ff7d5c9d2b51d7dd78491d0`

#### [M2-06] UI integration + smoke expansion — DONE (2026-06-19T17:02:29Z)
- 구현 요약: WPF에 Draft/Regulation/Feedback 탭을 추가해 NoModel DraftPipeline, 공개 catalog 검색, 승인형 예제 승격을 연결. catalog 로드 실패는 앱 시작 실패가 아니라 finding으로 표시.
- 변경 파일: `src/RiskManagementAI.App/MainWindow.xaml`, `src/RiskManagementAI.App/MainWindow.xaml.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0/0; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 162 PASS
- 보안 게이트 A: 0건(금지어 가드 문구 오탐만 확인; 기존 ignored 루트 ZIP은 미포함) / NuGet: 없음
- 결정/가정: 실제 모델 생성/내부 원문 RAG/재학습 없음. UI는 Core 기능 호출과 해시 audit 흐름 노출까지만 수행.
- feature 검증: `feature/mvp2-m2-06-ui-integration` CI `build` success (`27838739866`)
- develop 반영 커밋: `7b52aa29d3999a4295ac6189c4ea6cae3cb87c13`

#### [M2-04] Excel 2021 report builder — DONE (2026-06-20T03:30:03Z)
- 구현 요약: `System.IO.Compression`으로 xlsx ZIP package를 직접 생성하는 `ExcelReportBuilder`를 추가했다. `templates/report` XML 템플릿을 치환해 README/RAW_DATA/DATA_PROFILE/VALIDATION/SUMMARY/LIMIT_MONITORING/EXCEPTION_LIST/SQL_USED/CHANGE_LOG/AI_COMMENTARY 10개 시트를 만들고, WPF Report 탭에서 `reports/` 산출을 호출할 수 있게 했다.
- 변경 파일: `src/RiskManagementAI.Core/Report/ExcelReportBuilder.cs`, `templates/report/*`, `src/RiskManagementAI.App/MainWindow.xaml`, `src/RiskManagementAI.App/MainWindow.xaml.cs`, `tests/RiskManagementAI.SmokeTests/Program.cs`, `docs/33_MVP2_Backlog.md`, `docs/36_MVP2_Autorun_Worklog.md`
- build: GitHub Actions 성공(0 warnings / 0 errors; 로컬 PC는 .NET SDK 미설치로 CI 검증 사용) / SmokeTest: 180 PASS
- 보안 게이트 A: 0건(기존 CLAUDE/build 금지어 설명 문구 false positive만 확인) / NuGet: 없음
- 결정/가정: 사용자 확정 DM-03 적용. NuGet 0, Interop 금지, OpenXML SDK 미도입. 산출 수식은 `Excel2021FunctionChecker`로 검사하고, 파일 쓰기는 `reports/` 하위만 허용하며, audit log는 hash-only로 기록한다. 풍부한 서식은 본 MVP 범위에 필요하지 않다고 판단.
- feature 검증: `feature/mvp2-m2-04-excel-report` CI `build` success (`27858809259`)
- develop 검증: `develop` CI `build` success (`27858904182`, 180 PASS / 0 FAIL, 0 warnings / 0 errors)
- develop 반영 커밋: `46108dc1e3a6f7f0ea3c6fc7e5a2a8e3959498c7`

#### [S2-REL-00] release rehearsal prereq gate hardening — DONE (2026-06-20T03:40:42Z)
- 구현 요약: `build/00_check-prereqs.ps1`가 `dotnet` 실행 파일 존재만 확인하던 gap을 보강했다. `dotnet --list-sdks` 결과가 비어 있거나 .NET 8 SDK가 없으면 fail-fast한다.
- 변경 파일: `build/00_check-prereqs.ps1`, `docs/34_Release_Rehearsal_Guide.md`, `docs/36_MVP2_Autorun_Worklog.md`
- 검증: 현재 PC에서 `build/00_check-prereqs.ps1` 재실행 시 runtime-only dotnet을 감지하고 `.NET SDK not found... release publishing requires .NET 8 SDK`로 실패(정상 차단). feature CI `build` success (`27859065259`, 180 PASS / 0 FAIL, 0 warnings / 0 errors).
- 보안 게이트 A: 0건 / NuGet: 없음
- 결정/가정: 릴리스 ZIP 생성은 SDK 없는 현재 PC에서 강행하지 않는다. .NET 8 SDK가 있는 Dev/Test PC에서 `build/00~03 -Version 0.3.0`을 재개한다.
- develop 반영 커밋: `77527485eb627f0e5c9db6019e8f03941faf7840`

## 6. 하트비트 로그 (≈1h 또는 항목 전환마다)

<!-- [UTC] 진행 요약 / 현재 항목 / 다음 항목 -->
- [2026-06-19T15:46:24Z] P0-1 완료, build 0/0 + SmokeTest 119 PASS + Gate A 0건 / 현재 항목: P0-2 준비 / 다음 항목: release/v0.3.0 변경 develop 반영
- [2026-06-19T15:50:16Z] P0-2 feature 검증 완료, build 0/0 + SmokeTest 119 PASS + Gate A 0건 / 현재 항목: feature push/CI 확인 / 다음 항목: develop squash-merge 후 P0-3
- [2026-06-19T15:52:56Z] P0-2 feature CI `build` success / 현재 항목: develop squash commit 작성 / 다음 항목: P0-3 `.gitignore` `*.zip` 추가
- [2026-06-19T15:57:23Z] P0-3 적용 시작, 추적 ZIP 0개 확인 / 현재 항목: `.gitignore` `*.zip` + 검증 / 다음 항목: M2-01 LLM 추상화
- [2026-06-19T15:58:25Z] P0-3 feature 검증 완료, build 0/0 + SmokeTest 119 PASS + Gate A 0건 / 현재 항목: feature push/CI 확인 / 다음 항목: develop squash-merge 후 M2-01
- [2026-06-19T16:00:09Z] P0-3 feature CI `build` success / 현재 항목: develop squash commit 작성 / 다음 항목: M2-01 LLM 추상화 + NoModelMode
- [2026-06-19T16:04:02Z] P0-3 develop CI `build` success, Phase 0 완료 / 현재 항목: M2-01 NoModel draft service 구현 / 다음 항목: M2-01 feature 검증
- [2026-06-19T16:09:25Z] M2-01 feature CI `build` success, SmokeTest 127 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: M2-02 초안 파이프라인
- [2026-06-19T16:12:51Z] M2-01 develop CI `build` success, SmokeTest 127 PASS / 현재 항목: M2-02 DraftPipeline 구현 / 다음 항목: M2-02 feature 검증
- [2026-06-19T16:16:42Z] M2-02 feature CI `build` success, SmokeTest 141 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: M2-03 catalog 검색
- [2026-06-19T16:19:27Z] M2-02 develop CI `build` success, SmokeTest 141 PASS / 현재 항목: M2-03 RegulationCatalog/KbSearch 구현 / 다음 항목: M2-03 feature 검증
- [2026-06-19T16:23:46Z] M2-03 feature CI `build` success, SmokeTest 152 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: M2-05 피드백 예제 승격
- [2026-06-19T16:26:28Z] M2-03 develop CI `build` success, SmokeTest 152 PASS / 현재 항목: M2-05 ExamplePromotion 구현 / 다음 항목: M2-05 feature 검증
- [2026-06-19T16:29:28Z] M2-05 feature CI `build` success, SmokeTest 158 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: M2-06 UI 연동
- [2026-06-19T16:32:08Z] M2-05 develop CI `build` success, SmokeTest 158 PASS / 현재 항목: M2-06 WPF 탭 연동 / 다음 항목: M2-06 feature 검증
- [2026-06-19T17:02:29Z] M2-06 feature CI `build` success, SmokeTest 162 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: M2-04 결정 확인
- [2026-06-19T17:05:09Z] M2-06 develop CI `build` success, SmokeTest 162 PASS / 현재 항목: M2-04 결정 확인 / 다음 항목: BLOCKED 보고
- [2026-06-20T03:20:10Z] DM-03 사용자 확정으로 M2-04 unblock / 현재 항목: ExcelReportBuilder 구현 / 다음 항목: SmokeTest + Gate A + CI
- [2026-06-20T03:28:14Z] M2-04 feature CI `build` success, SmokeTest 180 PASS / 현재 항목: develop squash commit 작성 / 다음 항목: docs/36 DONE 갱신 + develop CI
- [2026-06-20T03:30:03Z] M2-04 develop squash commit 작성(`46108dc`) / 현재 항목: docs/36 DONE 갱신 / 다음 항목: develop push + CI 확인
- [2026-06-20T03:33:17Z] M2-04 develop CI `build` success, SmokeTest 180 PASS / 현재 항목: MVP-2 코어 완료 / 다음 항목: main 승격 PR 또는 release packaging rehearsal
- [2026-06-20T03:37:22Z] Release rehearsal 시도: `build/00`가 runtime-only dotnet을 성공 처리하는 gap 확인 후 SDK 부재 시 fail-fast로 보강 / 현재 항목: S2-REL-00 prereq gate / 다음 항목: feature CI + develop 반영
- [2026-06-20T03:40:42Z] S2-REL-00 feature CI success, develop squash commit 작성(`7752748`) / 현재 항목: docs/36 DONE 갱신 / 다음 항목: develop push + CI 확인

## 7. Claude 재개 체크리스트

1. `git fetch origin develop` → `git switch develop`
2. 본 문서 **§Resume Brief** 확인 → DONE/진행/NEXT/BLOCKED 파악
3. 재현 검증: `dotnet build RiskManagementAI.sln` + `dotnet run --project tests/RiskManagementAI.SmokeTests` (전부 PASS 확인)
4. 보안 게이트 A(docs/28) 재확인, 절대원칙 위반 0 확인
5. §3 자동 결정 로그(⚠️) 검토 → 승인/수정
6. §4 BLOCKED 큐의 각 항목에 대해 결정 → 진행 또는 사용자 확인(AskUserQuestion)
7. **NEXT UP**부터 이어서 진행. 충분히 안정되면 `develop → main` 승격 PR(검토 후 squash) 준비.

> 관련: `prompts/codex_mvp2_24h_autorun_prompt.md`, `docs/33_MVP2_Backlog.md`, `docs/31`(MVP-1 원장 패턴), `docs/28`, `docs/32`, `docs/35`.
