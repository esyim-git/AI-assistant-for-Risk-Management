---
name: release-package-verify
description: "오프라인 portable Release 패키지 검증 — build/00→03 순서, Integrity Manifest, ZIP SHA256, PDB/Dev config/모델/secret 부재, Local-Gate 증거(Total=N PASS/0 FAIL) 수집. 수동 호출 전용."
allowed-tools: Read, Bash(git status:*)
disable-model-invocation: true
shell: powershell
---

# Release Package Verify

## 목적
운영환경 반입용 portable Release ZIP이 **무결성·금지파일 부재·오프라인 기동 요건**을 충족하는지 검증하고, Local-Gate 증거(SmokeTest 합계·ZIP SHA256·manifest entry 수)를 수집한다. 본 스킬은 **런북 + 증거 점검**이며 빌드를 자동 실행하지 않는다.

## 언제 사용
- **수동 호출 전용** (`/release-package-verify`). 모델 자동 호출 금지.
- Release ZIP을 잘라낸 뒤 게이트 B(`docs/28`) 통과 여부를 판정하거나, ReleaseNote SHA256과 실제 ZIP을 대조해야 할 때.
- build/packaging는 **Windows 로컬(사용자/Codex, .NET 8 SDK + PowerShell)**에서 실행한다. Claude는 빌드/패키징을 자동 실행하지 않으며, 산출된 증거를 점검한다(`CLAUDE.md §11.6`).

## 절대 원칙
- **Local-Gate**: build/SmokeTest/packaging 실행은 전부 Local. 머지/반입 게이트 증거 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL` + Claude 검토(`CLAUDE.md §11.6`).
- **ZIP 내 금지파일 0**: 모델파일(`*.gguf/*.safetensors/*.onnx/*.pt`)·`*.pem/key/pfx/env`·`real_data/secrets/credentials/internal_*` 0. 실데이터·실 테이블/컬럼/시스템명·내부규정/NCR 원문 0(더미 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만 허용)(`docs/28` 게이트 B).
- **외부 의존 0**: 외부 NuGet·외부 API·자동 업데이트·telemetry 0. 산출물(`artifacts/`)은 repo 커밋 금지(gitignored).
- **과대표기 금지**: 상태는 정본 어휘만(`VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED`). **실 오프라인 Test PC 증거 없으면 Gate PASS로 적지 않는다**(`CLAUDE.md §11.4`).

## 절차
1. **사전**: build/packaging는 Windows 로컬(사용자/Codex)에서 실행됨을 확인한다. 본 스킬은 런북·증거 점검이며 Claude가 빌드를 자동 실행하지 않는다. `git status`로 작업트리·`artifacts/` 미커밋을 확인한다.
2. **순서 확인**: `build/00_check-prereqs` → `01_publish-win-x64` → `02_package-release` → `03_verify-package` 순서로 실행되었고 각 단계 결과(에러 0)를 확인한다. 명령·로그 양식은 [verify-runbook.md](verify-runbook.md).
3. **무결성 대조**: ZIP SHA256(`Get-FileHash`) ↔ ReleaseNote 값 일치, `approved_manifest.json` version·mandatory entry·entry별 hash/size 일치, PDB·Dev/Test config·모델·secret **0**을 확인한다(런북의 명령으로 재현).
4. **증거 수집**: SmokeTest `Total=N PASS / 0 FAIL` 합계 줄 + ZIP SHA256 값 + manifest entry 수를 기록한다. 증거가 없으면 **BLOCKED**로 판정한다.

## 산출물/보고
- **검증 결과표**: `ZIP SHA256(값) · ReleaseNote 대조(일치/불일치) · manifest(version·mandatory·hash/size) · 금지파일(0/N) · PDB·Dev/Test config(0/N)`.
- **증거**: SmokeTest `Total=N PASS / 0 FAIL` 합계 줄 + ZIP SHA256 + manifest entry 수.
- **최종 한 줄 판정**: **PASS**(게이트 B 충족) 또는 **BLOCKED**(증거 미수집/항목 실패 — 사유 명시). 증거 없는 PASS/VERIFIED는 적지 않는다.

## 체크리스트
build/00~03 실행·검증 런북, PowerShell 명령(자동 실행 아님), 증거 기록 양식은 [verify-runbook.md](verify-runbook.md).

## 참조
- `docs/24_Release_Packaging_Guide.md`(패키징 절차·산출물) · `docs/28`(게이트 B/C) · `docs/34_Release_Rehearsal_Guide.md`(리허설 런북) · `deploy/release_checklist.md`(보안·배포·반입 체크) · `build/00..03_*.ps1`(실행 스크립트).
- `CLAUDE.md §3`(절대 원칙) · `§11.4`(상태 어휘) · `§11.6`(Local-Gate).
- 연계 스킬: `/gate-bc-evidence`(게이트 B/C 증거 수집·정리) · `/security-gate-a`(보안 게이트 A 단독 점검).
