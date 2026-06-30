# Codex Prompt — REL-v0.7.0 Release Cut (VERSION 0.6.0 → 0.7.0 락스텝 버전 범프)

> **우선순위(충돌 시)**: `AGENTS.md` > `docs/39`(REL-v0.7.0 WP) > 본 프롬프트.
> **한 번에 이 WP 하나만.** Feature Branch `feature/rel-v0.7.0-release-cut`. Claude 승인 전 main 머지 금지.
> **선행 읽기**: `AGENTS.md §0·§3·§5·§6`, `docs/39` REL-v0.7.0 WP, `docs/47`(릴리스 노트/런북), `docs/40` ADR-006(VERSION 단일원천)·ADR-008(무결성), `docs/41 §6`(코드서명 — 본 WP 범위 아님).

## 0. 목표 (단일)
R2 트랙 완결(main `35e6f01`, `Total=714`)을 **v0.7.0**으로 정식 릴리스하기 위한 **버전 범프**를 한다. **기능 코드 변경 0** — 오직 버전 단일원천(`VERSION`)과 그에 락스텝으로 묶인 상수/테스트만 `0.6.0 → 0.7.0`으로 올린다. 패키징(`build/01~03`)·태깅은 **Local-Gate(Windows)**에서 수행하고 산출물·해시를 보고한다.

> **STOP**: 본 WP는 외부 의존성·NuGet·서명 도구·인증서를 **추가하지 않는다**. 코드 서명은 STAB-WP-05(APPROVAL_REQUIRED, `docs/40` ADR-012 / `docs/41 §6`)로 분리 — 본 WP에서 손대지 않는다.

## 1. 정확한 변경 범위 (락스텝 — 이 3곳만, 그 외 0)
`VERSION`은 단일원천(ADR-006)이고, 런타임 무결성 게이트가 manifest `version`을 **Core 상수 `IntegrityVerifier.ExpectedVersion`**과 Ordinal 비교(Fail-Closed)하므로, 셋을 **반드시 동시에** 올린다(하나라도 누락 시 기동 brick 또는 테스트 red).

1. **`VERSION`**: `0.6.0` → `0.7.0` (파일 내용 한 줄, trailing newline 정책 기존 유지).
2. **`src/RiskManagementAI.Core/Integrity/IntegrityVerifier.cs:27`**: `public const string ExpectedVersion = "0.6.0";` → `"0.7.0";`. (이게 누락되면 build/01이 만든 manifest `version=0.7.0`과 런타임 `ExpectedVersion=0.6.0` 불일치 → `App.OnStartup`에서 **Fail-Closed Shutdown(2)** = 운영 brick. 또한 아래 ③의 drift 가드 테스트 red.)
3. **`tests/RiskManagementAI.SmokeTests/PackagingTests.cs`**: 합성 manifest 버전 리터럴 **`"0.6.0"` 22곳 → `"0.7.0"`** (전부). 형태: `WriteIntegrityManifest(pkg, "0.6.0", ...)` 및 인라인 JSON `"{\"version\":\"0.6.0\",...}"`. 이 합성 manifest들은 `IntegrityVerifier`가 `ExpectedVersion`과 비교하므로, ②가 `0.7.0`이 되면 합성본도 `0.7.0`이어야 양성/음성 케이스가 의도대로 동작(버전 불일치로 모든 케이스가 무력화되지 않게).
   - **단, drift 가드 단언(현 `PackagingTests.cs:331`)**: `string.Equals(IntegrityVerifier.ExpectedVersion, File.ReadAllText("VERSION").Trim(), Ordinal)` — 이 줄은 **VERSION을 동적으로 읽으므로 리터럴 수정 대상 아님**(②+①이 맞으면 자동 통과). 손대지 말 것.

> **확인**: `Directory.Build.props`에 `<Version>` 리터럴 없음 — 어셈블리/파일 버전은 `build/01`이 `-p:Version=$Version`(=`VERSION`)으로 빌드 시 주입. **csproj/props 편집 불필요.**
> **금지**: 위 3곳 외 어떤 소스/테스트/문서의 `0.6.0`도 본 WP에서 바꾸지 않는다(예: `docs/`의 v0.6.0 릴리스 노트·기준선 이력은 **역사 기록** — 유지). git grep으로 잔여 `0.6.0`이 위 3파일 밖에 남아도 그것은 의도된 역사 표기다.

## 2. 검증 (Local-Gate, Windows + .NET 8 SDK)
1. `dotnet build RiskManagementAI.sln -c Release` → **0 Warning(nullable) / 0 Error**.
2. `dotnet run --project tests/RiskManagementAI.SmokeTests -c Release` → 종료부 **`Total=714 PASS=714 FAIL=0`** + `Unclassified=0`. (버전 범프는 단언 가감 없음 → **합계 불변 714**. PackagingTests 22곳을 안 바꾸면 그 도메인이 FAIL로 떨어진다 = 누락 탐지기.)
3. 패키징: `build/00_check-prereqs.ps1` → `01_publish-win-x64.ps1 -Version 0.7.0` → `02_package-release.ps1 -Version 0.7.0` → `03_verify-package.ps1 -Version 0.7.0`.
   - `01`은 `-Version`이 `VERSION`과 다르면 throw — `0.7.0` 일치 확인.
   - `03` PASS: manifest `version=0.7.0`, ZIP SHA256 산출, PDB/Dev-Test 0, **원문 미포함 스캔(v0.6 도입분)** 통과.
4. Gate A(`docs/28`): 실데이터/원문/토큰/인증서/모델파일 0 (문서+버전만).

## 3. 보고 (WP 완료 시)
- `dotnet build` 결과 · SmokeTest **`Total=714 PASS=714 FAIL=0`** 합계 줄 · Gate A 결과.
- `build/00~03` 결과 + **portable ZIP 파일명·SHA256**. **외부 NuGet 0 증거 = `dotnet list package`(또는 csproj)에 외부 `<PackageReference>` 0** — ⚠️ `DependencyList-v0.7.0.csv`는 self-contained 동봉 런타임 어셈블리 목록(문서화용)일 뿐 `PackageReference=0` 증거가 아님(self-contained는 런타임 다수 동봉).
- 변경 파일 목록(정확히 `VERSION`·`IntegrityVerifier.cs`·`PackagingTests.cs` 3개) + diff 라인 수.
- **양성 케이스**: 버전 불일치 시 Fail-Closed가 살아있음을 보이기 위해, (선택) 로컬에서 `ExpectedVersion`만 임시로 `0.6.0`으로 되돌렸을 때 PackagingTests가 FAIL/기동 차단됨을 1회 확인 후 원복(증거용, 커밋 금지).

## 4. 태깅 / GitHub Release (Local — 본 WP의 코드 PR과 분리)
- 버전 범프 PR 머지 후, **로컬에서** `git tag v0.7.0 && git push origin v0.7.0`(웹/프록시는 태그 push 403).
- Release 본문 = `docs/47 §1` 릴리스 노트 + ZIP SHA256 + **미서명 고지**(STAB-WP-05 후속). 첨부 = ZIP + .sha256 + ReleaseNote-v0.7.0.md(소스/모델 금지).

## 5. Branch / Commit
- Branch: `feature/rel-v0.7.0-release-cut`
- Commit: `chore: bump version to 0.7.0 for R2 analytics release (REL-v0.7.0)`

## 6. Claude Review Checklist (사전 합의)
- 변경이 **정확히 3파일**(VERSION·ExpectedVersion·PackagingTests 22리터럴)인가 / 그 외 `0.6.0` 역사표기 미변경인가.
- `ExpectedVersion == VERSION` drift 가드 단언(현 :331) 보존·통과 / 런타임 Fail-Closed 버전 매칭 의미 유지.
- **기능 코드·단언 수 변경 0** → `Total=714` 불변, Unclassified=0.
- 외부 NuGet/서명/인증서 **0 추가**(STOP 준수) / Gate A 0 / 절대원칙 유지.
- 패키징 `build/03` PASS(manifest version 0.7.0·ZIP SHA256·원문 미포함 스캔) 증거 첨부.
