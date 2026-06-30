# 47. Release v0.7.0 — R2 Risk Analytics & Visualization

## 목적 / 범위
v0.7.0 = **v0.6.0(R1 데이터 파운데이션 + R3 RAG/NCR 구조 + STAB v0.6.1 + UX Assist)** 위에 **R2 Risk Analytics & Visualization**(R2-WP-01~04)을 얹은 릴리스. 본 문서는 v0.7.0 **릴리스 노트 + Codex/Windows 패키징 런북 + Gate B 체크리스트 + GitHub Release 핸드오프**다. `docs/43`(v0.6.0)·`docs/42`(v0.5.0)·`docs/34`(리허설)·`docs/24`(패키징)·`docs/28`(게이트 B/C)를 v0.7.0 단계로 갱신.

> 빌드·ZIP·태그는 **Windows + .NET 8 SDK + PowerShell**(Codex 로컬). 웹/Linux 세션 및 git proxy는 **태그 push가 403** → **태그·Release 발행은 로컬에서**.
> **상태: v0.7.0 정식 릴리스 발행 완료(2026-06-30)**. 태그 `v0.7.0` = main `30c1cfb`(REL-v0.7.0 #90 머지 — 버전 범프 락스텝 `VERSION`·`IntegrityVerifier.ExpectedVersion`·`PackagingTests`). **`Total=714 PASS=714 FAIL=0`**(버전 범프 단언 가감 0 — 합계 불변, drift 가드 `PackagingTests:331` 통과). 직전 v0.6.0 태그 `3dfa80b`.
> **코드 서명**: v0.7.0은 **미서명 + Integrity Manifest/Fail-Closed 앵커**로 출하한다. Authenticode 코드 서명은 **STAB-WP-05 APPROVAL_REQUIRED**(인증서·외부 신뢰 루트 = STOP, `docs/40` ADR-012 / `docs/41 §6`) — v0.7.0 릴리스의 전제 아님(후속).

---

## 1. v0.7.0 릴리스 노트 (요약)

**v0.6.0 → v0.7.0: Exposure-Limit 분석을 의미적으로 경화하고, 대용량 입력·전일대비·인박스 시각화까지 결정적(deterministic)으로 확장**

- **Risk Semantic Hardening**(R2-WP-01): 중복 Join Key를 임의 선택(`group.Last()`)하지 않고 **`DUPLICATE_LIMIT`(7번째 상태, ADD-ONLY)로 차단**. 통화/단위를 하드코딩 const에서 **승인형 `ColumnMapping`(Optional)**으로 이관, 휴면 **`RECON_UNIT_MISMATCH` 활성**(currency 대칭·non-fail), **BASE_DT 형식 검증/정규화**(yyyyMMdd/yyyy-MM-dd→yyyyMMdd), **JoinAudit**(중복키·차단행·통화/단위 적용여부 결정적 기록). Dashboard=Report 일원화 유지(`DuplicateLimitCount` 노출).
- **Streaming / 대용량 입력**(R2-WP-02): `CsvReader` forward-only 스트리밍 + **행 상한 `MaxRowCount=200,000`·바이트 상한 `MaxByteSize=50MB`**(초과 시 `InvalidDataException`), `Cp949Decoder.DecodeLines(Stream)`는 기존 `Decode(byte[])`와 **바이트 동일** 디코드(CP949 스트리밍 결정성 보존). `DataProfiler.ProfileCsvStreaming`은 전 값 보관 제거(Welford 누산) + **OutlierCount는 2차 패스로 기존 2-pass와 bit-동일**, 중복행은 **SHA256 해시만 보관(원문 미저장)**. 기존 in-memory 경로·`NumericColumnProfile`(6필드) 불변(동일 입력→동일 수치).
- **전일대비(Prior-Day Analytics)**(R2-WP-03): `PriorDayAnalyzer`가 기존 `LimitMonitor.Analyze`를 Current/Prior **2회** 호출해 `(PortfolioId,RiskFactor)`로 페어링·diff(**새 엔진·새 상태 재구현 0**). New/Resolved/Increased/Decreased/Unchanged/StateTransition 분류, 행별 Current/Prior/Δ, TopN movers, **정규화 기준 same-day guard**, **`BASE_DT_FORMAT_MISMATCH`**/**`PRIOR_DAY_DUPLICATE_KEY`** Hidden-Risk, 4구획 출력(검토용 초안).
- **인박스 시각화 / Report 강화**(R2-WP-04): 신규 `RiskVisualAggregator`(결정적·Ordinal) — **7상태 분포**(`Enum.GetValues`로 `DuplicateLimit` 자동 포함), **집중도 HHI**(분모=`Σ Abs(Exposure)`·분모0 graceful `VISUAL_CONCENTRATION_ZERO_DENOMINATOR`), **TopN**(`Abs(Exposure)` desc+tie-break), **Heatmap**(<0.8 LOW/≤1.0 MID/>1.0 HIGH), **`MIXED_CURRENCY`** finding. `ExcelReportBuilder`: SUMMARY `ExceptionCount`를 부정확 `=COUNTA(...)`에서 **정확 Number SoT(`CountExceptions`)**로 교체, 신규 **`RISK_VISUAL` 인박스 시트**(Number/text 정적값만 → Excel 2021 수식게이트 위험 0, `ExpectedSheetNames` 10→11). `RenderRiskCharts`는 **동일 aggregator SoT**로 WPF Canvas/Shapes 화면차트(**외부 charting NuGet 0**). 시각화 caveat는 `ExcelReportResult.Findings`로 표면화.
- **SmokeTest**: R2 트랙 진행으로 **698 → 714 PASS / 0 FAIL**(R2-WP-04 +16). 이력: 646(STAB-UX-02) → 671(R2-WP-01 +25) → 680(R2-WP-02 +9) → 698(R2-WP-03 +18) → 714(R2-WP-04 +16). 버전 범프(REL-v0.7.0)는 단언 가감 없음 → **합계 불변**.
- **무결성(STAB v0.6.1, local-gate VERIFIED — v0.7.0에 그대로 유지)**: STAB-WP-03a(build측 manifest, #59) + STAB-WP-03b(런타임 Fail-Closed, #61). manifest `version` 필드는 **`VERSION` 단일원천**과 락스텝(`IntegrityVerifier.ExpectedVersion` == `VERSION`, drift 가드 테스트). **잔여**(콘텐츠 lock-step co-tamper + self-contained 런타임 DLL 미해시 + 폴더 동반 변조) + **Code Signing(현재 placeholder)** = **STAB-WP-05(APPROVAL_REQUIRED)** — `docs/40` ADR-012 / `docs/41 §6`.

**유지된 절대 원칙**: 오프라인 · 외부 NuGet 0 · 외부 API/telemetry/자동업데이트 0 · SQL/VBA 자동실행 0 · 해시 전용 감사 · NoModelMode · **내부규정/NCR 원문·실데이터·모델파일 미포함** · **Vector/Embedding/모델 런타임 미도입** · **외부 charting NuGet 미도입**(WPF Shapes 자체 렌더).

**아직 없는 것(의도적)**: Local LLM 추론(R4 Model Approval Gate 전 설계만) · 공개 규정 **원문 Clause/Chunk** 검색(KB, 인박스 keyword-only는 가능·원문 적재 STOP) · 승인된 실 내부규정/NCR 원문·계수(Prod 적재) · **서명된 바이너리**(STAB-WP-05 승인 대기).

---

## 2. 패키징 런북 (Codex / Windows PowerShell)

> **선행**: REL-v0.7.0 WP(버전 범프 락스텝) PR이 머지되어 main `VERSION=0.7.0`인 상태에서 컷한다.

```powershell
git fetch origin main
git switch -c release/v0.7.0 origin/main
git add --renormalize .                            # CP949 매핑표 LF(.gitattributes) 정합
Get-Content VERSION                                # -> 0.7.0

./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1  -Version 0.7.0     # VERSION 단일원천과 불일치 시 throw
./build/02_package-release.ps1  -Version 0.7.0
./build/03_verify-package.ps1   -Version 0.7.0     # 해시·내용·금지파일 + 원문 미포함 스캔(v0.6 도입분 유지)
```
산출물(모두 `artifacts/` — **gitignored**):
```text
artifacts/release/RiskManagementAI-v0.7.0-win-x64-portable.zip(.sha256)
artifacts/release/ReleaseNote-v0.7.0.md / DependencyList-v0.7.0.csv
```

**검증 포인트(v0.7.0)**:
- `build/03`이 manifest `version`=`0.7.0` 일치, ZIP SHA256, PDB/Dev-Test config 0, **원문 미포함 스캔**(v0.6.0 도입, `KbRepositoryGuard` 토큰 mirror) 통과.
- **외부 NuGet 0 증거 = 프로젝트 `<PackageReference>` 0** — `dotnet list package`(또는 csproj 검사)에 외부 패키지 0(차트 포함 자체 렌더). ⚠️ **`DependencyList-v0.7.0.csv`는 self-contained 동봉 .NET 런타임 어셈블리(~150개 `System.*`/coreclr 등) 목록(문서화·반입용)이지 `PackageReference=0`의 증거가 아니다** — self-contained는 런타임을 동봉하므로 항상 다수 어셈블리가 나열된다.
- ZIP 내부에 신규 분석/시각화 산출물은 **런타임 생성물**(`reports/`)이며 패키지에 동봉되지 않음(샘플만).

---

## 3. 게이트 B 체크 (오프라인 Test PC, `docs/28`·`docs/41 §4`)

- [ ] `00~03` 통과(03이 해시·내용·금지파일·**원문 미포함 스캔** 자동검증), VERSION `0.7.0`
- [ ] ZIP 내부: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`·`approved_manifest.json`(version `0.7.0`)
- [ ] ZIP 내부: 모델파일·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx/p12/cer/crt/der`·**내부규정/NCR 원문 0**
- [ ] **인터넷 차단** 실행 → **NoModelMode** 기동(무결성 검증 PASS) · 자동업데이트/telemetry/외부 API 0
- [ ] R1: CP949·UTF-8·XLSX → 한도분석(**7상태** incl `DUPLICATE_LIMIT`)·**대사(원천=분석 PASS)**·Excel Report·**화면=리포트 동일 수치**·History·Audit
- [ ] **R2-신규**: **대용량 CSV**(행/바이트 상한 동작) 입력 → 스트리밍 프로파일 = in-memory 동일 수치 · **전일대비**(Current/Prior 2일 입력 → New/Resolved/Δ·movers) · **`RISK_VISUAL` 시트** 생성(7상태 분포·TopN·집중도 HHI·Heatmap) · 화면 차트(WPF Shapes) 렌더 · **Exception Count = 정확 숫자**(COUNTA 아님)
- [ ] R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요), 내부/NCR은 **메타+표식만(원문 0)**, **NCR Rule Set 구조** 설명(검토용 초안)
- [ ] ReleaseNote SHA256과 `Get-FileHash` 일치 · 종료/재실행 정상
- [ ] **코드 서명**: 현재 **미서명**(placeholder) — 이 항목은 **STAB-WP-05 승인 후** Gate C에서 검증(서명 차단/통과). v0.7.0은 manifest+Fail-Closed 앵커로 BLOCKED 항목 표기.

> **실 오프라인 Test PC 증거가 없으면 Gate B는 PASS로 적지 않고 BLOCKED(Pilot PC 대기)를 유지**, 체크리스트만 선완성. (현재 R6 Team Pilot Gate B/C = BLOCKED — v0.7.0 Gate B/C 증거 시트는 후속 PILOT WP, `docs/45` v0.6 시트 양식 재사용.)

---

## 4. GitHub Release 핸드오프 (로컬) — **발행 완료**
> **발행 완료(2026-06-30)**: 태그 `v0.7.0` @ `30c1cfb` push · Release 발행(draft=false, prerelease=false) · 첨부 3개(portable ZIP·`.sha256`·ReleaseNote-v0.7.0.md). **최종 ZIP SHA256 = `42C835983127B127438AB97747B99FD0C3FA2E4363D4CB85641E45FE62E09DD5`** · ReleaseNote Build Commit `30c1cfb` · 본문에 실제 SHA256 + 미서명 고지 포함 확인. (참고: ZIP 해시는 `AssemblyInformationalVersion`에 커밋 SHA가 박혀 커밋마다 달라지므로, **태그 대상 `30c1cfb`에서 빌드한 ZIP의 실제 해시가 정본**이다.)
```powershell
git tag v0.7.0 30c1cfb
git push origin v0.7.0
```
- 첨부 = portable ZIP + .sha256 + ReleaseNote-v0.7.0.md (소스/모델 첨부 금지).
- 본문 = §1 릴리스 노트 + 최종 ZIP **SHA256**.
- **미서명 고지**: 릴리스 본문에 "v0.7.0은 미서명 portable ZIP — 무결성은 SHA256 + 동봉 `approved_manifest.json` + 런타임 Fail-Closed로 검증. Authenticode 서명은 STAB-WP-05(승인 대기)" 명시.

## 5. 제외 / 운영 반입 (게이트 C — 사내, 범위 밖)
- portable ZIP만 반입 → SHA256 재검증 → 백신 → 압축해제 → `run.bat`(`docs/25`).
- 미서명 바이너리 반입 정책은 사내 절차 확인(STAB-WP-05 승인 시 서명본으로 대체 가능).
- 승인된 실 내부규정/NCR 원문·계수 = Prod 문서오너 승인 후 권한통제 KB. Local LLM은 R4(`docs/41 §3`) 전 미반입.

> 관련: `docs/43`(v0.6.0), `docs/42`(v0.5.0), `docs/34`(리허설), `docs/24`(패키징), `docs/41`(게이트 — §6 코드서명 신규), `docs/40`(ADR-012 코드서명 승인 요건), `docs/38`(Release Train).
