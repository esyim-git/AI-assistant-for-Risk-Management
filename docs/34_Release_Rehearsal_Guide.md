# 34. Release Rehearsal Guide (v0.3.0 — MVP-1 Rule Engine)

## 목적

MVP-1이 `main`에 병합되었으므로, **운영환경 반입용 portable Release ZIP** 생성을 리허설(예행)한다.
`docs/24_Release_Packaging_Guide.md`(패키징)와 `docs/28` 게이트 B를 **실행 단계**로 구체화한다.

> 중요: 이 리허설은 **Windows + .NET 8 SDK + PowerShell** 환경(Dev/Test PC, 예: Codex 로컬)에서 수행한다.
> 웹/Linux 세션에서는 실행 불가하므로, 아래는 Codex/Windows 핸드오프 런북이다.

## 범위 / 제외

- 범위: `build/00~03` 스크립트 실행, ZIP+SHA256 생성, 내용 검증, Test PC 오프라인 실행 확인.
- 제외: 실제 운영환경(Prod) 반입(게이트 C·사내 절차). 모델 파일/실데이터 포함.

---

## 1. 사전 준비

- Windows 11 + .NET 8 SDK + PowerShell. (Prod 아님 — Dev/Test에서만 빌드)
  - `dotnet` 런타임만 있는 상태는 불충분하다. `build/00_check-prereqs.ps1`가 .NET 8 SDK 존재를 확인하지 못하면 중단한다.
- 작업 브랜치: `release/v0.3.0` (`main`에서 분기; docs/32 모델).
- **버전 정합**: `VERSION` 파일을 `0.3.0`으로 갱신(현재 `0.2.0-envsplit`). 스크립트는 `-Version 0.3.0`로 호출.

```powershell
git switch -c release/v0.3.0 origin/main
"0.3.0" | Set-Content -Path VERSION -NoNewline
```

## 2. 빌드 → 패키징 → 검증 (build/*)

```powershell
./build/00_check-prereqs.ps1                  # .NET 8 SDK 필수. runtime-only dotnet은 실패해야 정상.
./build/01_publish-win-x64.ps1   -Version 0.3.0      # self-contained win-x64 publish + 오프라인 자산 복사 + 금지파일 가드
./build/02_package-release.ps1   -Version 0.3.0      # portable ZIP + .sha256 + ReleaseNote + DependencyList
./build/03_verify-package.ps1    -Version 0.3.0      # SHA256 무결성 + 필수자산 존재 + 금지파일 부재
```

산출물(모두 `artifacts/`, **gitignored** — repo 커밋 금지):

```text
artifacts/release/RiskManagementAI-v0.3.0-win-x64-portable.zip
artifacts/release/RiskManagementAI-v0.3.0-win-x64-portable.zip.sha256
artifacts/release/ReleaseNote-v0.3.0.md
artifacts/release/DependencyList-v0.3.0.csv
```

## 3. 게이트 B 체크 (docs/28 · deploy/release_checklist.md)

- [ ] `00~03` 스크립트 전부 통과(03이 해시·내용·금지파일 자동검증)
- [ ] ZIP 내부: `RiskManagementAI.exe`, `run.bat`, `config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/` 존재
- [ ] ZIP 내부: 모델파일(`*.gguf` 등)·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx` **없음**
- [ ] **Test PC에서 인터넷 차단 후 실행** → 룰 검사/데이터 프로파일/샘플 분석 동작 확인 (모델 없이)
- [ ] 자동 업데이트/telemetry/외부 API 동작 없음
- [ ] ReleaseNote/DependencyList의 SHA256과 `Get-FileHash` 재대조 일치

## 4. GitHub Release (선택)

- 태그 `v0.3.0` 생성. **첨부물은 source가 아니라 portable Release ZIP + .sha256 + ReleaseNote**(docs/29 §산출물 이동 경계).
- 소스/빌드도구/모델은 첨부 금지.

## 5. 운영 반입 (게이트 C — 사내 절차, 본 리허설 범위 밖)

- portable ZIP만 반입 → SHA256 재검증 → 백신검사 → 압축해제 → `run.bat` 실행. (`docs/25`)

## 참고: Codex 핸드오프

- 기존 `prompts/codex_release_packaging_prompt.md`를 시작 프롬프트로, 본 문서의 v0.3.0 절차를 적용.
- 완료 보고: 스크립트별 결과, 최종 **SHA256 값**, ZIP 내용 검증 결과, Test PC 오프라인 실행 결과.
- 산출물(ZIP 등)은 **repo에 커밋하지 말 것**(`.gitignore`의 `artifacts/`·`release/`).

> 관련 문서: `docs/24_Release_Packaging_Guide.md`, `docs/23_Offline_Deployment_Guide.md`, `docs/25_Work_Network_Operating_Guide.md`, `docs/28_Security_Review_Checklist.md`(게이트 B/C), `docs/29_GitHub_Sync_Guide.md`
