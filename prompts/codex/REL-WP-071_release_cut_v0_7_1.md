# Codex REL-WP-071 — v0.7.1 출하 정합 릴리스 컷 (버전 범프 락스텝)

> 권위 스펙: `docs/39`(REL-WP-071), `docs/52`(v0.7.1 릴리스 노트/런북), `docs/40`(ADR-006·ADR-008·ADR-012), `docs/24`·`docs/41 §4·§6`. 선행: 없음(#131 `110e9ee` 머지 후 main). 참조 Skill: **`.claude/skills/risk-release-cut/`**(컷 표준 절차·체크리스트 — 먼저 읽기).
>
> **NEXT UP = 이 WP 1개만.** 다른 WP 건드리지 않는다. 충돌 시 우선순위: `AGENTS.md` > `docs/39` WP > 본 프롬프트.

## 현재 문제
published v0.7.0 ZIP(태그 `30c1cfb`, SHA256 `42C835…`)은 그 **이후 main에 머지된 KB-WP-01/02·UX-WP-04~11·FEEDBACK-WP-01/02·QA-WP-01~09(#94~#127)를 미포함**한다 — 사용자 체감 기능(Excel Function Helper·Smart Assist as-you-type·clause 검색·승인 Example 반영)이 어떤 출하본에도 없고, Gate B B-5(B13)가 PARTIAL로 고착된 직접 원인(`docs/48 §B′·§5a`). **이 WP는 신규 기능 구현이 아니라, 이미 구현·리뷰 완료된 main을 출하본에 반영하는 release cut이다.**

## 목표
`VERSION` 단일원천(ADR-006)과 락스텝 상수/테스트만 `0.7.0→0.7.1`로 범프(**정확히 3파일, 기능 코드 변경 0, 테스트 단언 가감 0 — `Total=900` 불변**). 패키징·태깅·발행은 Local-Gate(Windows, `docs/52 §2` 런북).

## 먼저 읽기
`AGENTS.md`(§0 기준선·§9 Automatic Skill Bridge), `CLAUDE.md §3`(절대원칙), `.claude/skills/risk-release-cut/SKILL.md`(+release-cut-checklist.md), `docs/52`(릴리스 노트/런북), `docs/39` REL-v0.7.0 항목(#90 — 직전 컷 선례), `docs/28`(보안검토).

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/rel-wp-071-release-cut origin/main
```
- .NET 8. PR→main(squash, Commit Subject에 `(#PR)`), 게이트 A. **NuGet 0**.
- 작은 Diff(3파일). main 직접 push 금지·force push 금지(`docs/32`·`docs/35`).
- **기준선 이중 표기**: 컷 = current main(#131 `110e9ee` 이후 HEAD) 기준, **binary-impact 기준선 = `7094d91`**(#128~#131은 docs-only — 바이너리 동일). 보고에 두 SHA를 구분 기재.

## 작업 범위 (정확히 3파일, 그 외 0)
1. `VERSION`: `0.7.0` → `0.7.1`.
2. `src/RiskManagementAI.Core/Integrity/IntegrityVerifier.cs:27` `ExpectedVersion = "0.7.0"` → `"0.7.1"` (누락 시 manifest version 불일치 → 런타임 Fail-Closed brick + drift 가드 테스트 red).
3. `tests/RiskManagementAI.SmokeTests/PackagingTests.cs` 합성 manifest의 **`ExpectedVersion`과 같아야 하는 모든 현행 버전 리터럴 `"0.7.0"` 전수** → `"0.7.1"` (합성본이 `ExpectedVersion`과 일치해야 양성/변조 케이스가 유효).
   - `WriteIntegrityManifest(..., "0.7.0", ...)` 호출뿐 아니라 같은 목적의 inline/raw manifest JSON(`"version":"0.7.0"` 등)이 존재하면 전부 포함한다.
   - ⚠️ **버전-불일치 음성 케이스는 예외**(`:257` 인근 "Version mismatch"·`9.9.9` 등 — manifest가 Core 상수와 **다른** 버전을 선언해야 유효): 해당 케이스의 상이-버전 문자열은 치환하지 말고, 범프 후에도 `ExpectedVersion`과 여전히 다름을 확인한다.
   - **drift 가드(`:343`, `ExpectedVersion == File.ReadAllText("VERSION")` 동적 비교)는 리터럴이 아님 — 미수정·자동통과.** 치환 누락/과치환은 SmokeTest Packaging 도메인 FAIL로 탐지된다.
- **제외**: 위 3곳 외 `0.7.0` 표기(docs 릴리스 노트·기준선 이력 = **역사 기록, 유지**) · 코드서명/인증서/서명도구(STAB-WP-05, APPROVAL_REQUIRED) · 기능·계약·단언 수 변경 · 신규 NuGet · `docs/48` 갱신(머지 후 Claude truth-sync 몫) · csproj(어셈블리 버전은 build/01 `-p:Version` 주입).

## 구현 세부 / 보안
- 단순 리터럴 범프. `ExpectedVersion`==`VERSION` 단일원천 불변식(ADR-006)·런타임 Fail-Closed 의미(manifest version 매칭) 유지.
- 외부 호출 0·Telemetry 0·자동실행 0·서명 도구/인증서 0(STOP — `docs/41 §6`). 실데이터/원문/토큰/키 0(Gate A).
- **미서명 출하 고지 유지**: generated `ReleaseNote-v0.7.1.md`는 `build/02` 산출물 그대로 검증하고 수동 편집하지 않는다. 미서명 고지는 `docs/52 §4`와 GitHub Release 본문에 "미서명 + SHA256 + `approved_manifest.json` + 런타임 Fail-Closed"로 명시한다.
- **STOP**: 외부 라이브러리·NuGet·Vector/Embedding·LLM Runtime·모델파일이 필요해지면 **즉시 STOP** → 승인 문서(`docs/41`·`docs/40`) 전까지 추가 금지(`AGENTS.md §4`).

## 테스트 (Windows, Local-Gate)
- `dotnet build RiskManagementAI.sln -c Release` **0 warning / 0 error**.
- `dotnet run --project tests/RiskManagementAI.SmokeTests` → **`Total=900 PASS=900 FAIL=0`** (버전 범프는 단언 가감 없음 → **합계 불변**; PackagingTests 치환 누락 시 그 도메인 FAIL = 누락 탐지기), `Unclassified=0`.
- 패키징: `./build/00_check-prereqs.ps1` → `./build/01_publish-win-x64.ps1 -Version 0.7.1` → `./build/02_package-release.ps1 -Version 0.7.1` → `./build/03_verify-package.ps1 -Version 0.7.1` 전부 PASS(manifest `version=0.7.1`·ZIP SHA256·금지파일·원문 미포함 스캔).
- 산출물 4종 확인: portable ZIP `.sha256` 대조 · `ReleaseNote-v0.7.1.md`(`build/02` 산출물 그대로: Version/Build Commit/SHA 등) · `DependencyList-v0.7.1.csv`(External NuGet=None·Model=Not included) · `approved_manifest.json`.

## 완료/보고
보고에 다음을 포함한다(머지 게이트 = 로컬 증거 + Claude 코드리뷰, GitHub CI green 전제 아님 — `CLAUDE.md §11.6`):
- build 0/0 · SmokeTest **합계 줄 `Total=900 PASS=900 FAIL=0`(불변)** · Gate A 결과 · `dotnet list package` PackageReference 0 · 변경 파일 목록(3파일 확인) · build/00~03 PASS + **ZIP SHA256 값** · current main/binary-impact 두 SHA.
- 태그 `v0.7.1` push·GitHub Release 발행은 **로컬(Windows)** — 발행 여부는 사용자(릴리스 오너) 결정(`docs/52 §4` 핸드오프). 컷 완료 ≠ Gate 봉인(Gate B/C는 BLOCKED 유지, 증거는 `docs/48` 라운드 규약).
- `docs/39` REL-WP-071 상태 갱신(DONE 등 정본 어휘) 요청. 과대표기 금지(`CLAUDE.md §11.4`).
- **Applied Skill Checklists** 명시: `risk-release-cut`, `risk-release-verify`, `risk-security-guard`, `risk-smoke-governance`, `risk-branch-governance`.

## Claude Review Checklist
변경이 정확히 3파일(VERSION·ExpectedVersion·PackagingTests 현행 리터럴 전수: `WriteIntegrityManifest` + inline/raw JSON if present)·그 외 `0.7.0` 역사표기 미변경 / 버전-불일치 음성 케이스의 상이-버전 유효성 유지 / drift 가드(:343) 보존·통과 / generated ReleaseNote 수동 편집 0·미서명 고지는 docs/52+GitHub Release body에서 유지 / 기능·단언 수 변경 0 → `Total=900` 불변·Unclassified 0 / 외부 NuGet·서명·인증서 0(STOP) / Gate A 0 / build/03 PASS(manifest 0.7.1·SHA256·원문 미포함) 증거 / current main·binary-impact 기준선 구분 보고.
