# Release Package Verify — 런북 + 증거 기록 양식

> **자동 실행 아님.** 아래 PowerShell 명령은 **Windows 로컬(사용자/Codex, .NET 8 SDK + PowerShell)**에서 실행하는 핸드오프 런북이다. Claude는 빌드/패키징을 자동 실행하지 않고, 산출된 로그·해시·manifest를 점검한다. 웹/Linux 세션에서는 실행 불가(`docs/34`).
> 산출물은 모두 `artifacts/`(gitignored) — **repo 커밋 금지**(`CLAUDE.md §8`).

`<VER>`는 `VERSION` 파일 값(예: `0.x.y`). `VERSION`이 단일 진리값(`build/*.ps1`의 `-Version`은 `VERSION`과 일치해야 통과).

---

## 0. 사전 점검 (Local)

```powershell
git -C . status --porcelain        # 작업트리 클린 / artifacts·release 미커밋 확인
Get-Content .\VERSION -Raw          # <VER> 확인 (스크립트 -Version 인자와 일치해야 함)
```

체크:
- [ ] 작업트리 클린(또는 ReleaseNote의 build commit이 `-dirty`로 표기됨)
- [ ] `artifacts/`·`release/`가 커밋 대상이 아님(`.gitignore` 적용)
- [ ] `.NET 8 SDK` 존재(`00`이 검증). runtime-only `dotnet`은 실패해야 정상.

---

## 1. build/00~03 실행 순서 (Local, Windows)

```powershell
.\build\00_check-prereqs.ps1                 # .NET 8 SDK 필수. runtime-only는 실패해야 정상
.\build\01_publish-win-x64.ps1  -Version <VER>   # self-contained win-x64 publish + 오프라인 자산 + 금지파일 가드
.\build\02_package-release.ps1  -Version <VER>   # portable ZIP + .sha256 + ReleaseNote + DependencyList
.\build\03_verify-package.ps1   -Version <VER>   # SHA256 + manifest + 필수자산 + 금지파일 부재 자동검증
```

기대 산출물(`artifacts/release/`):
- `RiskManagementAI-v<VER>-win-x64-portable.zip`
- `RiskManagementAI-v<VER>-win-x64-portable.zip.sha256`
- `ReleaseNote-v<VER>.md`
- `DependencyList-v<VER>.csv`

체크:
- [ ] `00`→`01`→`02`→`03` **순서대로** 실행, 각 단계 에러 0
- [ ] `03_verify-package.ps1`가 마지막에 `Verify completed for v<VER>.` 출력(중간 `exit 1` 없음)

---

## 2. 무결성·금지파일 재대조 (점검 명령)

```powershell
$zip = ".\artifacts\release\RiskManagementAI-v<VER>-win-x64-portable.zip"

# 2a) ZIP SHA256 ↔ ReleaseNote / .sha256 대조
(Get-FileHash $zip -Algorithm SHA256).Hash
Get-Content "$zip.sha256"
Select-String -Path ".\artifacts\release\ReleaseNote-v<VER>.md" -Pattern '^[A-Fa-f0-9]{64}$'

# 2b) ZIP 엔트리 나열 (추출 없이 내용 확인)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zip))
$z.Entries | ForEach-Object { $_.FullName.Replace('\','/') } | Sort-Object
$z.Dispose()
```

체크 (값은 `03` 자동검증과 동일 기준 — 수동 재확인용):
- [ ] `Get-FileHash` 값 == `.sha256` 값 == ReleaseNote의 SHA256 (3중 일치)
- [ ] **필수 파일/폴더 존재**: `RiskManagementAI.exe`, `run.bat`, `config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`
- [ ] **금지 확장자 0**: `*.gguf *.safetensors *.onnx *.pt *.pem *.key *.pfx *.env`
- [ ] **금지 경로 0**: `real_data/ secrets/ credentials/ internal_*`
- [ ] **PDB 0**: `*.pdb` 없음
- [ ] **Dev/Test config 0**: `*.Development.json *.Test.json *.Debug.json` 없음
- [ ] **원문 0**: 내부규정 원문·NCR 공식 원문 토큰 없음(`03`의 source-text 스캔이 자동 검출; 더미 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만 허용)

---

## 3. Integrity Manifest 확인 (`approved_manifest.json`)

`03_verify-package.ps1`이 ZIP을 임시 추출해 manifest를 검증한다. 수동 확인 시:

```powershell
$tmp = Join-Path $env:TEMP ("rmai_verify_" + [Guid]::NewGuid().ToString("N"))
Expand-Archive -LiteralPath $zip -DestinationPath $tmp -Force
$m = Get-Content (Join-Path $tmp "approved_manifest.json") -Raw | ConvertFrom-Json
$m.version                              # == <VER> 이어야 함
($m.files | Measure-Object).Count       # manifest entry 수 (증거로 기록)
$m.files | ForEach-Object { $_.path }   # 필수 core entry 포함 확인
Remove-Item $tmp -Recurse -Force
```

체크:
- [ ] `manifest.version` == `<VER>`
- [ ] mandatory core entry 선언됨: `RiskManagementAI.exe`, `RiskManagementAI.dll`, `RiskManagementAI.Core.dll`, `config/security_policy.json`, `config/column_mapping.json`, `kb/ncr_placeholder.md`
- [ ] entry별 `sha256`·`size` 일치, manifest 경로가 패키지 루트를 벗어나지 않음(traversal 0)
- [ ] **entry 수 기록**(증거)

---

## 4. 오프라인 기동 요건 (Test PC)

> 실 Test PC 증거가 없으면 게이트 B는 **PASS로 적지 않는다**(`CLAUDE.md §11.4`).

체크:
- [ ] **인터넷 차단 후** 압축해제 → `run.bat` 실행 → 룰 검사/데이터 프로파일/샘플 분석 동작(모델 없이, NoModelMode 기본)
- [ ] 자동 업데이트·telemetry·외부 API 동작 0
- [ ] 쓰기 경로(`logs/ reports/ config/`) 권한 정상, 로그는 해시만(원문 저장 0)

---

## 5. 증거 기록 양식 (보고에 그대로 인용)

```text
[Release Package Verify — v<VER>]
- 실행 환경(Local)   : Windows + .NET 8 SDK + PowerShell  (커밋: <short-sha>[-dirty])
- build/00~03 순서   : PASS (00→01→02→03, 에러 0)
- ZIP SHA256         : <64-hex>
- SHA256 3중 대조     : ZIP == .sha256 == ReleaseNote  (일치 / 불일치)
- manifest version   : <VER>  (entry 수: <N>)
- manifest hash/size : 전 entry 일치 (불일치 <K>건)
- 금지파일(model/secret/PDB/Dev-Test config/원문) : 0 (또는 위반 목록)
- SmokeTest          : Total=<N> PASS / 0 FAIL
- 오프라인 기동(Test PC) : 확인됨 / 미확인(BLOCKED)
- 최종 판정          : PASS  /  BLOCKED(<사유>)
```

판정 규칙:
- 모든 체크 충족 + 실 Test PC 오프라인 증거 + SmokeTest `Total=N PASS / 0 FAIL` → **PASS**
- 증거 미수집·항목 실패·Test PC 미확인 → **BLOCKED**(사유 명시). 증거 없는 PASS/VERIFIED 금지.

---

## 참조
- `docs/24_Release_Packaging_Guide.md` · `docs/28`(게이트 B/C) · `docs/34_Release_Rehearsal_Guide.md` · `deploy/release_checklist.md` · `build/00..03_*.ps1`
- 연계 스킬: `/risk-gate-bc` · `/risk-security-guard`
