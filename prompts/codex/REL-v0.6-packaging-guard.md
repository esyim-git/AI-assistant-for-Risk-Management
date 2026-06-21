# Codex REL-v0.6 — 패키징 원문 미포함 스캔 (KbRepositoryGuard 연결)

> 권위 스펙: `docs/41 §2`(후속①), `docs/43`(v0.6 릴리스 §2 ④), `docs/28`(게이트 B). Release: v0.6.0 패키징 하드닝. 선행: R3 완료(main).

## 목표
release 패키징 단계가 **내부규정/NCR 원문 미포함**을 **코드로 강제**하게 한다. 현재 `KbRepositoryGuard`(원문 의심 파일 Blocker 스캔)는 **SmokeTest/CI에서만** 실행되고, `build/01`/`build/03`은 **확장자·경로명 deny-list만** 본다 — 중립 이름의 원문 파일이 `kb/`·`config/`·`samples/`에 들어가면 패키지에 포함될 수 있다. 이를 **패키징 검증에서 차단**한다.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§8`, `docs/41 §2`, `docs/43`, 기존 `src/RiskManagementAI.Core/Kb/KbRepositoryGuard.cs`(`ScanDirectories`·`SuspiciousContentTokens`·`SuspiciousNameTokens`·`MetadataAllowlist`·텍스트 확장자), `build/01_publish-win-x64.ps1`(L30-50 자산 복사), `build/03_verify-package.ps1`(L40-75 deny-list 검증).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/rel-v0.6-packaging-guard origin/main
```
- Windows + .NET 8 SDK + PowerShell. PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위
- **원문 미포함 스캔을 `build/03_verify-package.ps1`에 추가**한다 — 릴리스 산출물 검증의 정본은 03(`docs/41 §2`·게이트 B). PowerShell-native; 외부 도구 0.
  - **스캔 대상 = 실제 ZIP 산출물 내용**: `build/03`이 portable ZIP을 **임시 디렉터리에 추출**한 뒤 그 안의 `kb/`, `config/`(incl `config/ncr`), `samples/`, `data_sources/`(존재 시)를 스캔. ZIP **entry 이름만** 보는 기존 검증으로는 중립 이름 원문 파일을 못 잡으므로 **내용까지** 본다. 스캔 후 임시 디렉터리 정리.
  - (선택·권장) `build/01` publish 디렉터리에서도 **조기 차단** 추가 스캔 — 단 **03 스캔을 대체하지 않음**(stale ZIP·publish 후 변조 대비 03이 정본).
  - **토큰 세트 = `KbRepositoryGuard`와 완전 동일(누락 금지)**. 현재 guard 값:
    - **내용 토큰**(`SuspiciousContentTokens`): `"내부규정 원문"`, `"NCR 공식본 원문"`, `"official text"`, `"full text"`.
    - **파일명 토큰**(`SuspiciousNameTokens`): `internal_rule_original`, `internal_regulation_original`, `ncr_official_original`, `official_text`, `full_text`.
    - **내용 검사 대상 확장자**: `.csv .json .jsonl .md .txt .sql`(그 외 확장자는 파일명 토큰만 검사).
  - **allowlist**(메타/placeholder): `kb/README.md`, `kb/public_regulation_catalog.csv`, `kb/ncr_placeholder.md`.
  - allowlist 외 파일이 **내용 토큰 또는 파일명 토큰**에 해당 → **패키징 실패(`exit 1`)** + 경로 포함 명확한 메시지.
- **드리프트 방지(필수)** — 둘 중 하나:
  - **(A, 권장) 컴파일된 `KbRepositoryGuard.Scan`을 추출 디렉터리에 직접 재사용**(예: 빌드된 self-contained exe의 숨은 `--scan-source-text <dir>` 서브커맨드 또는 전용 guard 러너). PS는 토큰을 재정의하지 않음 → 드리프트 0.
  - **(B) PS가 위 토큰 세트를 미러링**하되, **SmokeTest 회귀**로 `KbRepositoryGuard`의 `SuspiciousContentTokens`·`SuspiciousNameTokens`·allowlist·텍스트 확장자 **전체가 빌드 스크립트에 존재**함을 단언(guard에 토큰 추가 시 CI가 드리프트 차단).
- 제외: C# `KbRepositoryGuard` 로직 변경(이미 동작), 새 외부 의존성.

## 구현 세부 / 보안
- 경로 (A)면 `dotnet`/빌드 산출 exe 재사용만(외부 0); 경로 (B)면 **PowerShell 내장만**(`Select-String`/`Get-Content` 등). 외부 바이너리/모듈 0.
- 인코딩 주의: CP949/UTF-8 자산 모두 스캔되도록(바이트/문자열). 한글·영문 토큰 모두 대소문자 무시 매칭(guard는 `OrdinalIgnoreCase`).
- 결정적·읽기 전용. 실데이터/모델/secret 확장자·경로명 deny-list(기존) **유지**(추가, 대체 아님).
- **C#↔PS drift 방지**: 경로 (A)는 guard 재사용으로 구조적 0; 경로 (B)는 PS 상단 "`KbRepositoryGuard`와 동기화" 주석 + **SmokeTest 회귀**로 내용 토큰 4종·파일명 토큰 5종·allowlist 3파일이 빌드 스크립트에 **전부** 존재함을 단언.

## 테스트(필수, 로컬 Windows)
- **clean repo**: `build/00~03 -Version 0.6.0` 전부 통과(원문 스캔 PASS).
- **음성→양성(내용 토큰)**: 중립 이름 파일(예: `kb/note_x.txt`)에 `"내부규정 원문"` → 패키징 **실패(exit 1)**. 영문 토큰도 확인: `"official text"` 넣은 `samples/note.txt` → **실패**. 제거 후 통과.
- **음성→양성(파일명 토큰)**: 중립 내용이라도 `samples/ncr_official_original.txt`처럼 파일명 토큰 포함 → **실패**.
- **03이 ZIP 내용을 잡는지**: 위 파일을 ZIP에 포함시켜 `build/03` 단독 실행 → **실패**(entry 이름만이 아니라 추출 내용 검사 확인).
- allowlist 3파일은 통과(차단 안 됨).
- 경로 (B) 채택 시 SmokeTest 회귀: 내용 토큰 4종·파일명 토큰 5종·allowlist 3파일 전부 빌드 스크립트에 존재 확인.

## 완료/보고
패키징 검증(`build/03`, ZIP 내용 추출 스캔)이 원문 미포함을 강제(SmokeTest/CI + **패키징** 이중). `build/00~03 -Version 0.6.0` 결과·스캔 PASS·양성 케이스(내용·파일명·ZIP) 차단 확인 · 드리프트 방지 경로(A/B) 명시 · `docs/41 §2 후속①` DONE 갱신.

## Claude Review Checklist
`build/03`이 **ZIP 추출 내용** 스캔(entry 이름만 아님) / 토큰 세트 = guard 완전 동일(내용 4·파일명 5)·allowlist 3 / 양성(내용·영문·파일명·ZIP) 차단·clean 통과 / 드리프트 방지(A 재사용 또는 B 미러+SmokeTest 회귀) / 기존 deny-list 유지(추가) / 외부 0 / 결정적 / Gate A.
