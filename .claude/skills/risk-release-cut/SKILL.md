---
name: risk-release-cut
description: Execute a REL release cut — version bump lockstep, build/00~03 packaging, artifact verification (ZIP SHA256, ReleaseNote, DependencyList, approved_manifest), and unsigned-notice release handoff. Zero feature change, zero test-assertion change, zero STOP-gate contact.
argument-hint: "[version]"
arguments: [version]
disable-model-invocation: true
allowed-tools: Read Grep Glob Edit Write Bash(git status *) Bash(git log *) Bash(git diff *) Bash(git tag *) Bash(git push origin v*) Bash(dotnet build *) Bash(dotnet run *) Bash(dotnet list *) Bash(./build/00_check-prereqs.ps1 *) Bash(./build/01_publish-win-x64.ps1 *) Bash(./build/02_package-release.ps1 *) Bash(./build/03_verify-package.ps1 *)
---

# Release Cut (REL WP)

## 목적
REL 계열 WP(예: REL-v0.7.1)의 **정식 릴리스 컷 표준 절차**를 고정한다. 릴리스 컷은 **이미 main에 머지된 기능을 실제 출하본(portable ZIP)에 반영하는 작업**이지 신규 기능 구현이 아니다. 버전 범프 락스텝 → build/00~03 → 산출물 검증 → (로컬) 태그·Release 발행 순서로 진행하며, 코드 동작·테스트 단언을 바꾸지 않는다.

## 언제 사용
- "vX.Y.Z 릴리스 컷 / REL WP / 출하본 갱신 / release cut" 류 작업의 계획(Claude)·구현(Codex)·리뷰 공통.
- 출하본-main 기능 괴리(머지됐지만 어떤 출하 ZIP에도 없는 기능)를 해소할 때.
- Release 체인 Preflight: `risk-status-sync` → **본 스킬(컷 절차)** → `risk-release-verify`(패키지 검증) → `risk-security-guard`(Gate A).

## 절대 원칙
- **기능 변경 0 · 테스트 단언 가감 0(`Total` 불변) · STOP 게이트 접촉 0**(외부 NuGet·서명 도구/인증서·모델·원문·Vector/Embedding 일절 없음). 릴리스 컷 diff = 버전 락스텝 3파일 + 릴리스 문서만.
- **기준선 이중 표기**: 컷은 **current main**에서 수행하되, **binary-impact 기준선**(마지막 코드/테스트 머지 SHA)을 릴리스 문서에 함께 기록한다. **문서 전용 머지는 baseline SHA를 올리지 않는다(관례)** — 문서의 baseline 표기와 current main의 불일치는 drift가 아니다.
- 버전은 `VERSION` 파일 단일원천(ADR-006). **3파일 락스텝**: `VERSION` · `IntegrityVerifier.ExpectedVersion` · `PackagingTests`(drift 가드) 동시 범프 — 하나라도 빠지면 build/테스트가 실패해야 정상.
- **미서명 출하 고지 유지**: 코드 서명 = STAB-WP-05(APPROVAL_REQUIRED). 승인 전 릴리스는 "미서명 + SHA256 + `approved_manifest.json` + 런타임 Fail-Closed" 고지를 ReleaseNote와 Release 본문에 명시한다.
- 컷 완료 ≠ Gate 봉인: 실 오프라인 Test PC 증거 없이 Gate B/C를 PASS로 적지 않는다(신규 Gate 증거 문서는 BLOCKED로 시작, `CLAUDE.md §11.4`).
- 태그 push·GitHub Release 발행은 **로컬(Windows)** 작업(웹/proxy 세션은 태그 push 403, `docs/47 §0`). 공개 발행 여부는 사용자(릴리스 오너) 결정.

## 절차
1. **기준선 확인**: `git log`로 current main SHA와 binary-impact 기준선(마지막 코드/테스트 머지 SHA)을 구분 확인·기록. 직전 태그·published ZIP SHA256도 기록(예: v0.7.0 = `30c1cfb`/`42C835…`).
2. **버전 범프 락스텝**: `VERSION` 범프 + `IntegrityVerifier.ExpectedVersion` + `PackagingTests` 기대값 동시 갱신. 이 3파일 외 코드 변경 0.
3. **로컬 게이트**: `dotnet build`(0 warn/0 err) + SmokeTest **`Total=N PASS / 0 FAIL`**(직전과 **동일 N** — 단언 가감 0 확인) + Gate A.
4. **패키징(Windows)**: `build/00_check-prereqs` → `01_publish-win-x64 -Version X.Y.Z` → `02_package-release` → `03_verify-package`(해시·금지파일·원문 미포함 스캔 — VERSION 불일치 시 throw가 정상).
5. **산출물 검증(필수 4종)**: portable ZIP **SHA256(.sha256 대조)** · **ReleaseNote-vX.Y.Z.md**(Build Commit·SDK·미서명 고지) · **DependencyList-vX.Y.Z.csv**(External NuGet=None·Local LLM Model=Not included) · **approved_manifest.json**(version 락스텝·entries 무결). 상세 = `/risk-release-verify`.
6. **릴리스 문서**: `docs/4x_Release_vX.Y.Z.md`(릴리스 노트 + 패키징 런북 + Gate B 체크리스트) 작성. Gate B/C 증거 문서는 현재 `docs/54` 양식을 기반으로 릴리스별 successor를 새로 만들고 초기 상태 = BLOCKED로 둔다. 과거 원장은 수정하지 않는다.
7. **핸드오프**: 태그 `vX.Y.Z` = 컷 커밋 SHA, Release 본문 = 릴리스 노트 + ZIP SHA256 + 미서명 고지(첨부 = ZIP·`.sha256`·ReleaseNote만 — 소스/모델 금지). 발행 후 `risk-doc-truth-sync`(README·docs/38/39 기준선·태그 반영).

## 산출물/보고
- 릴리스 컷 diff(락스텝 3파일 + 문서) · 로컬 게이트 증거(`Total=N` **불변** 명시) · 산출물 4종 검증 결과 · ZIP SHA256 · 태그/발행 상태.
- 보고 예: `REL-vX.Y.Z 컷 완료 — current main <sha>(binary-impact <sha>), Total=N 불변, ZIP SHA256 <hash>, 미서명 고지 포함, Gate B/C=BLOCKED(증거 대기)`.

## 체크리스트
컷 전/중/후 상세 점검은 [release-cut-checklist.md](release-cut-checklist.md).

## 참조
- `docs/24_Release_Packaging_Guide.md` · `docs/34_Release_Rehearsal_Guide.md` · `docs/42`/`docs/43`/`docs/47`(v0.5/0.6/0.7 릴리스 실례) · `docs/54`(현재 Gate B/C 증거 양식) · `docs/48/45/44`(historical)
- `docs/40` ADR-006(VERSION 단일원천)·ADR-008(무결성 manifest)·ADR-012(코드서명 APPROVAL_REQUIRED) · `docs/41 §6`
- 연계 스킬: [/risk-release-verify](../risk-release-verify/SKILL.md)(패키지 검증) · [/risk-gate-bc](../risk-gate-bc/SKILL.md)(실 PC 증거) · [/risk-security-guard](../risk-security-guard/SKILL.md)(Gate A) · [/risk-doc-truth-sync](../risk-doc-truth-sync/SKILL.md)(발행 후 정합)
