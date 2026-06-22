# Codex STAB-WP-01 — Build/Version Reproducibility (VERSION 단일 원천)

> 권위 스펙: `docs/39 §B`(STAB-WP-01), `docs/40`(ADR-006, ADR-005), `docs/38`(RR-11). 기준선: main v0.6.0(`3dfa80b`). **NEXT UP — 이 WP 하나만 구현.**

## 현재 문제
`build/01_publish-win-x64.ps1`·`02_package-release.ps1`·`03_verify-package.ps1`의 기본 파라미터가 `[string]$Version = "0.2.0"`다. `VERSION` 파일은 `0.6.0`. 무인자 실행 시 **0.2.0 이름의 산출물**이 생성되어 릴리스 무결성을 깬다(RR-11). 버전 원천이 분산돼 있다.

## 목표
`VERSION` 파일을 **유일한 버전 원천**으로 만든다. 빌드 스크립트는 VERSION을 읽고, `-Version`이 명시되며 VERSION과 **다르면 실패(exit 1)**. Release Note/ZIP/SHA/DependencyList가 동일 Version을 쓰고, Release Note에 **Build Commit SHA·정본 Test 총수·SDK·Runtime·Build Date**를 기록한다. `global.json`로 .NET SDK를 고정한다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§11`, `docs/40`(ADR-005 .NET 8 유지 / ADR-006 재현성), `docs/24`(패키징), `VERSION`, `build/00_check-prereqs.ps1`, `build/01_publish-win-x64.ps1`(L1-30), `build/02_package-release.ps1`(L1-70 — Release Note/DependencyList 생성), `build/03_verify-package.ps1`(L1-15).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/stab-wp-01-build-version origin/main
```
- Windows + .NET 8 SDK + PowerShell. PR→main(squash, `(#PR)`), 게이트 A, **NuGet 0**.

## 작업 범위
1. **VERSION 단일 원천**: 각 스크립트가 인자 없으면 `Get-Content VERSION`(trim)으로 버전 결정. `-Version` 명시 시 VERSION과 **불일치하면 명확 메시지 후 `exit 1`**(예: `Requested version X does not match VERSION file Y`).
2. **하드코딩 `0.2.0` 제거**: `build/01~03`의 `="0.2.0"` 기본값 제거(=VERSION 읽기). 문서/샘플의 0.2.0은 본 WP 범위 밖(README는 이미 0.6.0).
3. **버전 일관성**: ZIP·`.sha256`·`ReleaseNote-vX.md`·`DependencyList-vX.csv`가 동일 X 사용(이미 `$Version` 파생이면 유지, 원천만 교정).
4. **Build metadata**: `02_package`의 Release Note 생성부에 `Build Commit: <git rev-parse --short HEAD>`, `SmokeTest Total: <정본 수치 — STAB-WP-02 이후 자동, 그 전엔 "ALL PASS / 0 FAIL (CI)">`, `.NET SDK: <dotnet --version>`, `Runtime: win-x64 self-contained`, `Build Date(UTC)` 행 추가.
5. **`global.json`**(신규, repo 루트): 설치된 .NET 8 SDK 버전으로 `sdk.version` + `rollForward: latestFeature`(또는 `latestPatch`) 고정. (ADR-005: .NET 8 유지.)
- **제외**: 기능 코드, 패키지 추가, .NET 메이저 전환, build/03 원문 스캔 로직(이미 동작).

## 구현 세부 / 보안
- PowerShell-native만. 외부 도구/모듈 0. 결정적·읽기 전용(빌드 산출 외 쓰기 없음).
- VERSION 읽기는 BOM/trailing whitespace에 견고하게(`.Trim()`).
- `global.json`의 SDK 버전은 `build/00_check-prereqs.ps1`이 확인하는 버전과 정합. 누락 SDK면 00이 명확히 실패(기존 동작 유지).
- WinPS 5.1 + pwsh 7 양쪽에서 동작(REL-v0.6의 ADR-004 교훈 — .NET Core 전용 API·BOM 의존 회피).

## 테스트 (Windows, 양쪽 셸)
- `build/01~03` **무인자** 실행 → **0.6.0** 산출물(이름·Release Note·SHA 일치).
- `build/01 -Version 9.9.9` → **exit 1** + 명확 메시지. `-Version 0.6.0` → 정상.
- Release Note에 Build metadata 행 존재. `global.json`로 SDK 고정 확인.
- `dotnet build -c Release` 0/0, SmokeTest ALL PASS 유지. (선택) SmokeTest에 "build 스크립트에 `0.2.0` 기본값 부재" 회귀 1개 추가 가능.

## 완료/보고
무인자 빌드가 VERSION(0.6.0) 산출, 불일치 시 실패. Release Note에 build metadata. `global.json` 고정. 양쪽 셸 실행 로그 + 산출물 버전 일치 + 불일치 실패 캡처 보고. `docs/39` STAB-WP-01 DONE 갱신 요청.

## Claude Review Checklist
VERSION 단일원천(무인자=0.6.0) / `-Version` 불일치 exit 1 / 하드코딩 0.2.0 0개 / 산출물·Note·SHA·Dep 버전 일치 / build metadata 기록 / `global.json` SDK 고정(ADR-005/006) / 양쪽 셸 / NuGet 0 / 기존 SmokeTest 불변 / Gate A.
