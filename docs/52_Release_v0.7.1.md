# 52. Release v0.7.1 — 출하 정합(Shipped-Artifact Parity) 릴리스

## 목적 / 범위
v0.7.1 = **published v0.7.0(태그 `30c1cfb`) 이후 main에 머지된 트랙을 출하본에 반영하는 정합 릴리스.** REL-WP-071 자체는 버전 범프 락스텝 3파일·신규 기능 0으로 완료됐다. 최종 발행 전 감사에서 발견된 `CORR-WP-01`(zero-check reconciliation false PASS 제거)은 정확성 correction으로 포함한 뒤 다시 패키징한다. 본 문서는 v0.7.1 **릴리스 노트 + 패키징 런북 + Gate B 체크 연결 + GitHub Release 핸드오프**다. (`docs/47` v0.7.0 문서의 v0.7.1 대응본.)

> **상태: `VERIFIED`.** Publication qualifier = published. Unsigned Latest Release `v0.7.1`은 tag/Build Commit `fa7552567cb432ec6a4afe9900b3eca480fc5780`에서 2026-07-10 발행됐다: <https://github.com/esyim-git/AI-assistant-for-Risk-Management/releases/tag/v0.7.1>.
> **정본 증거**: code/test baseline `4efb8e6`(#135), release Build Commit `fa755256`(#136 docs-only), SmokeTest `Total=907 PASS=907 FAIL=0`, build/00~03 PASS, ZIP SHA256 `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018`, manifest version 0.7.1 / required 27/27 / mismatch 0. Pre-CORR SHA256 `A70D0B37AD92344A2ECFBE0D4D96360F56CBAFFF94363249F0BD1A20ADC1ECDC`는 무효이며 발행되지 않았다.
> **코드 서명**: v0.7.1도 **미서명 + Integrity Manifest/Fail-Closed 앵커**로 출하한다. Authenticode 서명은 **STAB-WP-05 APPROVAL_REQUIRED**(`docs/51 §B` 결정 대기 — 릴리스 전제 아님).

---

## 1. v0.7.1 릴리스 노트 (요약 — v0.7.0 → v0.7.1)

**테마: "main에는 있는데 출하본에는 없던 것"의 해소.** Total 714 → 907(#94~#127 누적 + CORR-WP-01 #135).

- **KB Clause 검색**(KB-WP-01/02 #94/#101): 공개 규정 Clause Pack 계약·로더·원문 가드 + clause keyword 검색·인용·`ClauseSnippetAllowed` 게이트(공개+메타 완비 시 32자 snippet). `SourceTextAllowed=false` 불변·원문 repo 미포함·합성 더미만·Vector/Embedding STOP.
- **Excel Function Helper**(UX-WP-04 #102 + UX-WP-11 #122): 함수 검색·상세·인수·리스크예시·수식예시·365여부·Excel 2021 대체식·추천(정적, embedded resource, 차단 함수 추천 0 가드), 자동삽입 0·검색어 미로그.
- **Smart Assist as-you-type/팝업**(UX-WP-05/06 #103/#104 + 하이진 UX-WP-07/08/09 #110/#111/#113 + 시드 UX-WP-10 #117): `CompletionTriggerPolicy` debounce 트리거·focus-preserving 팝업·Snippet/SafetyNote/Kind 표시·Esc/Close 포커스 복원·이중 핀 축소. **정적·NoModel·자동삽입 0**(실 LLM 랭킹=R4 미구현).
- **승인 Feedback Example 검색·반영**(FEEDBACK-WP-01/02 #106/#108): ingest 게이트(Blocker 0+`ForbiddenTermScanner`)·결정적 검색·`ReferencesReviewed` 경유 read-only Prompt 반영·hash 이중 audit — **RETRIEVAL, 학습 아님**.
- **하이진**(R2-WP-05 #109): dead Welford 필드 제거(동작 불변).
- **테스트 하드닝**(QA-WP-01~09 #115~#127, 제품 코드 0): Safety·Recon/Limit·Kb·Report·Csv/Xlsx/Profile·Ncr·UiContract·Audit/Generation·Mapping/Packaging 도메인 회귀 +57 — **인박스 SmokeTest 도메인 하드닝 스윕 완결, `Total=900`**.
- **Reconciliation truth-state correction**(CORR-WP-01 #135): zero checks는 `NOT_RUN`, nonzero checks만 PASS/FAIL; High `LIMIT_DATA_REQUIRED`와 SUMMARY false-PASS 모순 제거. Report/UiContract 회귀 +7, **`Total=907`**.

**유지된 절대 원칙**: 오프라인 · 외부 NuGet 0 · 외부 API/telemetry/자동업데이트 0 · SQL/VBA 자동실행 0 · 해시 전용 감사 · NoModelMode · 내부규정/NCR 원문·실데이터·모델파일 미포함 · Vector/Embedding/모델 런타임 미도입.

**아직 없는 것(의도적)**: Local LLM 추론(R4, `docs/51 §A` 결정 대기) · 실 공개 규정 clause pack 콘텐츠(합성 더미만 — RAG 게이트) · 승인 NCR 실 계수(`docs/51 §C`) · 서명 바이너리(STAB-WP-05) · Prior-Day/streaming의 WPF UI 배선(v0.8 트랙, 제안서 §8).

---

## 2. 패키징 재현 런북 (완료, Windows PowerShell)

> **실행 결과**: `fa755256` exact main에서 아래 순서를 완료했다. 절차 정본 = `.claude/skills/risk-release-cut/`.

```powershell
git fetch origin tag v0.7.1
git switch --detach v0.7.1
git rev-parse HEAD                                # -> fa7552567cb432ec6a4afe9900b3eca480fc5780
git add --renormalize .                            # CP949 매핑표 LF 정합
Get-Content VERSION                                # -> 0.7.1

./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1  -Version 0.7.1     # VERSION 불일치 시 throw
./build/02_package-release.ps1  -Version 0.7.1
./build/03_verify-package.ps1   -Version 0.7.1     # 해시·금지파일·원문 미포함 스캔
```
산출물(`artifacts/` — gitignored): `RiskManagementAI-v0.7.1-win-x64-portable.zip(.sha256)` · `ReleaseNote-v0.7.1.md` · `DependencyList-v0.7.1.csv` · (ZIP 내) `approved_manifest.json`(version 0.7.1).

**검증 포인트**: manifest `version=0.7.1` 일치 · ZIP SHA256=`.sha256` · PDB/Dev-Test config 0 · 원문 미포함 스캔 PASS · `dotnet list package` PackageReference 0(DependencyList는 self-contained 런타임 목록 — NuGet-0 증거 아님, `docs/47 §2` 동일 주의).

---

## 3. Gate B/C 연결 (v0.7.1)

- v0.7.1 전용 증거 원장은 **`docs/54_GateBC_v0.7.1_Evidence.md`**다. `docs/48`의 v0.7.0 user-reported 결과는 역사 증거이며 v0.7.1 PASS로 승계하지 않는다.
- **B13 봉인 경로 열림**: UX-WP-04~11이 published 아티팩트에 처음 포함됐으므로, published v0.7.1 ZIP에서 Excel Function Helper·as-you-type 팝업·포커스 복원 실 UI를 확인한다.
- B0~B15·C1~C7은 `docs/54` 판정 규칙을 따른다. **실 오프라인 Test PC 증거 없이 PASS 금지, 컷 완료 ≠ Gate 봉인**(초기 상태 BLOCKED).
- 증거 민감정보 금지: 실거래/포지션/고객/원문/계정정보 0. `samples/` dummy와 masking된 화면만 사용한다.

---

## 4. GitHub Release 발행 결과

```powershell
git tag v0.7.1 <컷 커밋 SHA>
git push origin v0.7.1
```
- Tag `v0.7.1` = `fa7552567cb432ec6a4afe9900b3eca480fc5780`; Release는 draft/pre-release가 아닌 Latest stable이다.
- 수동 업로드 asset은 portable ZIP(72,337,105 bytes) + `.sha256`(112 bytes) + packaging-generated `ReleaseNote-v0.7.1.md`(837 bytes) 정확히 3개다. GitHub가 자동 제공하는 Source code(zip/tar.gz)는 수동 asset이 아니며 운영 반입 금지다. **유일한 runtime 반입물은 portable ZIP**이다. 모델/DependencyList는 GitHub asset으로 첨부하지 않았다.
- GitHub 계산 ZIP digest `sha256:282b71385fee83b4ed7ad221caf84ad3a6b4e2b5e5191601f4240aeed0419018`가 로컬/sidecar/ReleaseNote SHA와 일치한다.
- Release 본문은 §1 요약, 최종 SHA256, 미서명 고지, Gate B/C `BLOCKED`를 포함한다.
- 2026-07-11 KST(2026-07-10 UTC) Release 본문의 Gate B/C handoff를 v0.7.1 정본 `docs/54`로 정정했다. `docs/48`은 v0.7.0 historical evidence다.
- Gate B/C는 발행으로 자동 PASS가 되지 않는다. 사용자는 이 published ZIP을 받아 `docs/54`의 v0.7.1 라운드로 증거를 봉인한다.

> 관련: `docs/54`(v0.7.1 Gate B/C 정본), `docs/48`(v0.7.0 역사 증거), `docs/47`(v0.7.0), `docs/24`(패키징), `docs/41 §4·§6`(게이트), `docs/51`(승인 결정 패킷), `docs/proposals/FABLE5_REPO_ASSESSMENT_PROPOSAL_20260706.md §10 WP-B`, `.claude/skills/risk-release-cut/`(컷 절차 정본), `prompts/codex/REL-WP-071_release_cut_v0_7_1.md`.
