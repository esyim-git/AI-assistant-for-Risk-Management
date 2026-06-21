# Codex REL-v0.6 — 패키징 원문 미포함 스캔 (KbRepositoryGuard 연결)

> 권위 스펙: `docs/41 §2`(후속①), `docs/43`(v0.6 릴리스 §2 ④), `docs/28`(게이트 B). Release: v0.6.0 패키징 하드닝. 선행: R3 완료(main).

## 목표
release 패키징 단계가 **내부규정/NCR 원문 미포함**을 **코드로 강제**하게 한다. 현재 `KbRepositoryGuard`(원문 의심 파일 Blocker 스캔)는 **SmokeTest/CI에서만** 실행되고, `build/01`/`build/03`은 **확장자·경로명 deny-list만** 본다 — 중립 이름의 원문 파일이 `kb/`·`config/`·`samples/`에 들어가면 패키지에 포함될 수 있다. 이를 **패키징 검증에서 차단**한다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§8`, `docs/41 §2`, `docs/43`, 기존 `src/RiskManagementAI.Core/Kb/KbRepositoryGuard.cs`(스캔 디렉터리·forbidden phrase·allowlist), `build/01_publish-win-x64.ps1`(L30-50 자산 복사), `build/03_verify-package.ps1`(L40-75 deny-list 검증).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/rel-v0.6-packaging-guard origin/main
```
- Windows + .NET 8 SDK + PowerShell. PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위
- `build/01_publish-win-x64.ps1`(또는 `03`)에 **원문 미포함 content 스캔** 추가(PowerShell-native; 외부 도구 0):
  - 스캔 대상: 패키지에 들어가는 `kb/`, `config/`(incl `config/ncr`), `samples/`, `data_sources/`(존재 시).
  - **forbidden phrase**: `"내부규정 원문"`, `"NCR 공식본 원문"` — **`KbRepositoryGuard`와 동일하게 유지**(주석으로 sync 명시).
  - **allowlist**(메타/placeholder): `kb/README.md`, `kb/public_regulation_catalog.csv`, `kb/ncr_placeholder.md`.
  - allowlist 외 파일이 forbidden phrase 포함 → **패키징 실패(`exit 1`)** + 명확한 메시지.
- `build/03`은 ZIP entry 기반이라 content 스캔이 어려우면, **publish 디렉터리(또는 source asset 폴더)** 단계(`build/01`)에서 스캔하고 실패 시 중단(권장).
- 제외: C# `KbRepositoryGuard` 로직 변경(이미 동작), 새 외부 의존성.

## 구현 세부 / 보안
- **PowerShell 내장만**(`Select-String`/`Get-Content` 등). 외부 바이너리/모듈 0.
- 인코딩 주의: CP949/UTF-8 자산 모두 스캔되도록(바이트/문자열). 한글 phrase 매칭.
- 결정적·읽기 전용. 실데이터/모델/secret deny-list(기존) **유지**(추가, 대체 아님).
- **C#↔PS phrase drift 방지**: PS 스크립트 상단에 "`KbRepositoryGuard`와 동기화" 주석 + (선택) SmokeTest가 `build/01`(또는 03) 텍스트에 두 phrase가 모두 있는지 확인하는 회귀.

## 테스트(필수, 로컬 Windows)
- **clean repo**: `build/00~03 -Version 0.6.0` 전부 통과(원문 스캔 PASS).
- **음성→양성**: `kb/`(또는 samples/)에 중립 이름 파일(예: `kb/note_x.txt`)에 `"내부규정 원문"` 넣고 패키징 → **실패(exit 1)**. 제거 후 통과.
- allowlist 3파일은 통과(차단 안 됨).
- (선택) SmokeTest 회귀: 빌드 스크립트에 forbidden phrase 2종 존재 확인.

## 완료/보고
패키징 검증이 원문 미포함을 강제(SmokeTest/CI + **패키징** 이중). `build/00~03 -Version 0.6.0` 결과·스캔 PASS·양성 케이스 차단 확인 · `docs/41 §2 후속①` DONE 갱신.

## Claude Review Checklist
패키징 단계 원문 스캔(allowlist·forbidden phrase=guard 동기화) / 양성 차단·clean 통과 / 기존 deny-list 유지(추가) / PowerShell 내장만·외부 0 / 결정적 / Gate A.
