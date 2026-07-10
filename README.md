# Risk Management AI Assistant

금융회사 리스크관리 업무를 위한 **오프라인·사람 검토형 Local Copilot**이다. SQL/VBA를 자동 실행하지 않고, Golden6에서 사용자가 수동 Export한 CSV/XLSX를 검사·분석해 근거와 Excel 2021 리포트를 만든다.

## Current Status

| 항목 | 현재 |
|---|---|
| v0.7.1 release Build Commit | `fa755256` (PR #136, docs-only); current main은 live Git 확인 |
| Product code-test baseline | `4efb8e6` (PR #135) |
| VERSION | `0.7.1` |
| 최신 공개 Release | `v0.7.1` (`fa755256`, 미서명), ZIP SHA256 `282B7138...9018` |
| v0.7.1 | `VERIFIED` (published); build/00~03 PASS, manifest 0.7.1 required 27/27 |
| Build / SmokeTest | warning 0, error 0 / `Total=907 PASS=907 FAIL=0` |
| 외부 NuGet | `PackageReference` 0, `NuGet.Config <clear/>` |
| Formal Gate B/C | `BLOCKED` for v0.7.1 (`docs/54`); v0.7.0 user-reported 이력은 `docs/48` |
| NEXT UP | `GOV-WP-02` (`prompts/codex/GOV-WP-02_branch_security_governance.md`); published v0.7.1 Gate B/C는 사용자 병행 (`docs/54`) |

전체 진단·근거·로드맵은 [docs/53_Repository_Audit_and_v1_Execution_Plan.md](docs/53_Repository_Audit_and_v1_Execution_Plan.md), 실행 원장은 [docs/39_Work_Package_Backlog.md](docs/39_Work_Package_Backlog.md)를 따른다.

## Product Goal

```text
Golden6 수동 Export (CSV/XLSX)
  -> 데이터 품질·인코딩·기준일 검사
  -> Exposure-Limit 7상태 + 대사 + 전일 대비
  -> RISK_VISUAL 포함 Excel 2021 리포트
  -> 사용자 검토·승인
  -> 해시 기반 감사 이력
```

규정/NCR 질의는 공개 catalog/승인 Pack에서 근거를 찾는 **검토용 초안**이다. 공식 법규 해석·공식 NCR 산정이 아니다.

## Capability

상태 어휘: `VERIFIED`, `PARTIAL`, `SCAFFOLD_ONLY`, `PLACEHOLDER`, `BLOCKED`, `NOT_IMPLEMENTED`, `APPROVAL_REQUIRED`.

| 기능 | Core | WPF/User |
|---|---|---|
| SQL/VBA/Excel 2021 Safety Checker | VERIFIED | VERIFIED |
| 해시 전용 Task/Feedback/Suggestion Audit | VERIFIED | VERIFIED |
| CP949/UTF-8 CSV + XLSX reader | VERIFIED | PARTIAL (Data Profile UI는 CSV in-memory) |
| Column Mapping + Exposure-Limit Join 7상태 + 대사 | VERIFIED | VERIFIED |
| Dashboard = Excel Report single result | VERIFIED | VERIFIED |
| Prior-Day Analytics | VERIFIED | NOT_IMPLEMENTED (App call site 0) |
| Streaming Data Profile | VERIFIED | NOT_IMPLEMENTED (App call site 0) |
| RISK_VISUAL / Excel 2021 Report | VERIFIED | VERIFIED |
| 공개 KB catalog 검색 | VERIFIED | VERIFIED |
| Clause keyword search / snippet gate | VERIFIED | NOT_IMPLEMENTED (App call site 0) |
| 승인 Example 승격 | VERIFIED | VERIFIED |
| 승인 Example retrieval / reviewed reflection | VERIFIED | NOT_IMPLEMENTED (App call site 0) |
| NCR Rule Set | SCAFFOLD_ONLY | PLACEHOLDER |
| Local LLM | PLACEHOLDER (NoModelMode) | APPROVAL_REQUIRED |

## Non-Negotiables

- Offline first. 외부 API, telemetry, auto-update 0.
- 외부 NuGet `PackageReference` 0.
- SQL/VBA/Golden6 자동 실행 0.
- 실데이터, 실 테이블/컬럼 사전, 내부규정/NCR 원문, 모델/인증서/비밀정보 repo 포함 0.
- 로그는 원문 대신 SHA-256. 사용자 ID도 해시만.
- Local LLM runtime, 모델, Vector/Embedding, signing credential/tool, 실 NCR Pack은 승인 전 STOP.
- 운영 반입 대상은 self-contained portable Release ZIP만.

## Environments

```text
GitHub / 개발 PC      = Dev  (설계·구현·빌드·패키징)
로컬 검증 PC          = Test (portable ZIP·dummy data·Gate B)
회사 업무망 PC        = Prod (승인 ZIP 실행 전용·Gate C)
```

Prod에서 SDK, Git, NuGet restore, 외부 다운로드가 필요하면 설계 실패다.

## Build And Test

Windows + .NET 8 SDK:

```powershell
dotnet build RiskManagementAI.sln -c Release
dotnet run --project tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj -c Release
```

정상 기준: build warning/error 0, `Total=907 PASS=907 FAIL=0`, `Unclassified=0`.

Release candidate:

```powershell
Get-Content VERSION
./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1 -Version 0.7.1
./build/02_package-release.ps1 -Version 0.7.1
./build/03_verify-package.ps1 -Version 0.7.1
```

후속 merge가 있으면 이전 ZIP/SHA를 폐기하고 latest main에서 다시 생성한다.

## Run

```text
1. portable ZIP SHA256 대조
2. 백신/EDR 검사
3. 압축 해제
4. run.bat 또는 RiskManagementAI.exe
5. dummy/masked 입력으로 Gate 확인
```

세부: [deploy/README_OFFLINE_RUN.md](deploy/README_OFFLINE_RUN.md), [docs/54_GateBC_v0.7.1_Evidence.md](docs/54_GateBC_v0.7.1_Evidence.md). v0.7.0 이력은 [docs/48_GateBC_v0.7.0_Evidence.md](docs/48_GateBC_v0.7.0_Evidence.md)에 보존한다.

## Workflow

```text
Claude: architecture/WP/review/truth-sync
Codex : one WP implementation + local gate + PR
User  : release/approval/Test-PC/Pilot owner
```

PR은 squash-only, main 직접 push와 force push 금지. `test`/`wpf-build` 자동 PR CI는 #134에서 복원됐고 #135에서 첫 green을 확인했다. hard branch protection과 원격 security settings는 [docs/32](docs/32_Branch_Governance.md) Phase A로 닫는다.

## Roadmap

1. `GOV-WP-02`: restored PR CI 증거 + branch protection/secret scanning 적용.
2. `ARCH-WP-01` MainWindow 행위 불변 분해.
3. Prior-Day, streaming/XLSX profile, Clause, Feedback retrieval UI 배선.
4. .NET 10 LTS 전환.
5. published v0.7.1부터 formal Gate B/C 증거를 병행하고, 지원 LTS 후보에서 Team Pilot을 봉인.
6. 승인된 범위에서만 signing, NCR Pack, Local LLM을 진행.

관련 정본: [docs/38_v1_Master_Roadmap.md](docs/38_v1_Master_Roadmap.md), [docs/39_Work_Package_Backlog.md](docs/39_Work_Package_Backlog.md), [docs/40_ADR_Architecture_Evolution.md](docs/40_ADR_Architecture_Evolution.md), [docs/41_Approval_and_Pilot_Gates.md](docs/41_Approval_and_Pilot_Gates.md).
