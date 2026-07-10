# Release Cut — 상세 체크리스트

REL WP 실행용. 코드 동작·테스트 단언을 바꾸지 않는다. 상태 어휘는 정본만(`CLAUDE.md §11.4`), 실 Test PC 증거 없는 Gate PASS 표기 금지.

## 1. 컷 전 (Pre-cut)
- [ ] current main SHA 확인·기록 (`git log --oneline -5`).
- [ ] **binary-impact 기준선** 확인: 마지막 코드/테스트 머지 SHA(문서 전용 머지는 바이너리 불변 — baseline SHA 미변경 관례). 릴리스 문서에 두 SHA를 **구분 표기**.
- [ ] 반영 대상 기능 목록 = 직전 태그 이후 머지된 WP(전부 **이미 구현·리뷰 완료**분) — 신규 구현 0 확인.
- [ ] 직전 published 릴리스: 태그 SHA·ZIP SHA256 기록(혼동 방지 — 예: v0.7.0 `30c1cfb`/`42C835…`).
- [ ] 열린 PR·미커밋 변경 0 (`git status` clean).

## 2. 버전 범프 락스텝 (3파일)
- [ ] `VERSION` = X.Y.Z (단일원천, ADR-006).
- [ ] `src/RiskManagementAI.Core/Integrity/IntegrityVerifier.cs` `ExpectedVersion` = X.Y.Z.
- [ ] `tests/RiskManagementAI.SmokeTests/PackagingTests.cs` 버전 drift 가드 기대값 = X.Y.Z.
- [ ] 이 3파일 + 릴리스 문서 외 diff 0.

## 3. 로컬 게이트
- [ ] `dotnet build` 0 warning / 0 error.
- [ ] SmokeTest `Total=N PASS=N FAIL=0` — **N이 직전 정본과 동일**(단언 가감 0). 감소/증가 시 컷 중단·원인 조사.
- [ ] Gate A(`risk-security-guard`): secret/실데이터/원문/모델파일 0.
- [ ] `dotnet list package` 또는 csproj 검사: PackageReference 0.

## 4. 패키징·산출물 (Windows 로컬)
- [ ] `build/00~03 -Version X.Y.Z` 전부 PASS(03의 원문 미포함 ZIP 추출 스캔 포함).
- [ ] ZIP SHA256 = `.sha256` 파일 값 (`Get-FileHash` 대조).
- [ ] `ReleaseNote-vX.Y.Z.md`: Build Commit(= 컷 SHA)·SDK·빌드일·**미서명 고지** 포함.
- [ ] `DependencyList-vX.Y.Z.csv`: External NuGet=None · Local LLM Model=Not included. (self-contained 런타임 어셈블리 목록은 문서화용이지 PackageReference=0의 증거가 아님 — `docs/47 §2` 주의.)
- [ ] `approved_manifest.json`: version=X.Y.Z·mandatory entries·critical globs 무결.
- [ ] ZIP 내부 금지물 0: 모델파일·`real_data/`·`internal_*`·인증서/키·`*.local.json`·PDB·내부규정/NCR 원문.

## 5. 발행·후속
- [ ] 태그 `vX.Y.Z` = 컷 커밋(로컬에서 push — 웹 세션 403).
- [ ] GitHub Release: 본문에 릴리스 노트 + ZIP SHA256 + 미서명 고지, 첨부 = ZIP·`.sha256`·ReleaseNote만.
- [ ] 신규 Gate B/C 증거 문서를 현재 `docs/54` 양식으로 별도 생성하고 초기 상태 = **BLOCKED**(실 PC 증거 대기). 과거 릴리스 원장을 갱신하지 않는다.
- [ ] truth-sync: README·`docs/38`·`docs/39`·`SKILLS.md`/`CLAUDE.md`/`AGENTS.md` 기준선·태그·ZIP SHA 반영(`risk-doc-truth-sync`).

## 보고 양식
```
REL-vX.Y.Z 컷 보고
- current main: <sha> / binary-impact 기준선: <sha>
- 락스텝 3파일: VERSION·ExpectedVersion·PackagingTests = X.Y.Z
- 로컬 게이트: build 0/0 · Total=N PASS / 0 FAIL (불변) · Gate A 0건 · PackageReference 0
- 산출물: ZIP SHA256 <hash> · ReleaseNote/DependencyList/approved_manifest PASS
- 발행: 태그 <sha> · Release <URL> (미서명 고지 포함)
- Gate B/C: BLOCKED (증거 대기, 현재 릴리스별 원장; v0.7.1은 `docs/54`)
```
