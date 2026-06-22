# 43. Release v0.6.0 — R3 RAG/NCR

## 목적 / 범위
v0.6.0 = **v0.5.0(R1 데이터 파운데이션) + R3(RAG/NCR)**. 본 문서는 v0.6.0 **릴리스 노트 + Codex/Windows 패키징 런북 + Gate B 체크리스트 + GitHub Release 핸드오프**다. `docs/42`(v0.5.0)·`docs/34`(리허설)·`docs/24`(패키징)·`docs/28`(게이트 B/C)를 v0.6.0 단계로 갱신.

> 빌드·ZIP·태그는 **Windows + .NET 8 SDK + PowerShell**(Codex 로컬). 웹/Linux 세션 및 git proxy는 **태그 push가 403** → **태그·Release 발행은 로컬에서**.
> **전제**: `main`이 `ee0e93c`(R3 완료 + 게이트 PASS) 이상 + **패키징-guard 연결 PR 병합 후**(아래 §2 ④) 컷한다.

---

## 1. v0.6.0 릴리스 노트 (요약)

**v0.5.0 → v0.6.0: 규정/NCR 검색(RAG)을 인용·권한통제·감사 가능한 구조로**

- **공개 규정 KB Metadata 확장**(WP-01): 9필드(출처 locator·버전·시행일·폐기일·파일Hash·적재일·승인상태·대체문서·라이선스). 공개 메타만, **원문 미포함**.
- **Keyword/Inverted Index 검색**(WP-02): linear → 역색인(결정적, NuGet 0). 결과·순서·점수 현행 동일, 한글 부분일치 보존, substring L=32 cap + 긴쿼리 fallback. **Vector/Embedding 미도입(STOP)**.
- **인용형 답변**(WP-03): 문서명·버전·시행일·**조항**·출처·**검색 기준일(주입 IClock 실제 날짜)**·**"검토 필요"** 완비. placeholder 메타 `(확인 필요)` + 경고.
- **적재 게이트 가드**(WP-04): `KbAccessPolicy` — 공개 status만 인용, `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED`는 **원문 비노출(메타+표식)**, 라이선스/승인/미지 status 구조화 `SafetyFinding`. **`KbRepositoryGuard`**가 kb/·data_sources/·samples/·config/ncr 원문 의심파일 Blocker 스캔.
- **NCR Rule Set 구조**(WP-05): **모델이 산식을 암기해 답하지 않고** Rule Set 8요소(Version·Effective Date·Component Map·Formula Description·Validation SQL·Regulation Basis·Approval History)로만 산출·설명. 샘플=placeholder(실 계수 0), Validation SQL=조회 전용. **NCR 공식본 원문 repo 미포함**.
- **패키징 가드 강화**: release 검증에 **원문 미포함 스캔** 연결(§2 ④).
- **SmokeTest 368 → 460**.

**유지된 절대 원칙**: 오프라인 · 외부 NuGet 0 · 외부 API/telemetry/자동업데이트 0 · SQL/VBA 자동실행 0 · 해시 전용 감사 · NoModelMode · **내부규정/NCR 원문·실데이터·모델파일 미포함** · **Vector/Embedding/모델 런타임 미도입**.

**아직 없는 것(의도적)**: Local LLM 추론(R4 Model Approval Gate 전 설계만) · 전일대비(R2 WP-09) · 승인된 실 내부규정/NCR 원문·계수(Prod 적재).

---

## 2. 패키징 런북 (Codex / Windows PowerShell)

```powershell
git fetch origin main
git switch -c release/v0.6.0 origin/main
git add --renormalize .                            # CP949 매핑표 LF(.gitattributes) 정합
Get-Content VERSION                                # -> 0.6.0

./build/00_check-prereqs.ps1
./build/01_publish-win-x64.ps1  -Version 0.6.0
./build/02_package-release.ps1  -Version 0.6.0
./build/03_verify-package.ps1   -Version 0.6.0     # ④ 원문 미포함 스캔 포함
```
산출물(모두 `artifacts/` — **gitignored**):
```text
artifacts/release/RiskManagementAI-v0.6.0-win-x64-portable.zip(.sha256)
artifacts/release/ReleaseNote-v0.6.0.md / DependencyList-v0.6.0.csv
```

**④ 패키징 원문 미포함 스캔 (이번 릴리스 신규)**: `prompts/codex/REL-v0.6-packaging-guard.md`로 `build/03`에 스캔을 연결한다 — `build/03`이 portable ZIP을 **임시 추출**해 그 안의 `kb/`·`config/`(incl `config/ncr`)·`samples/`·`data_sources/`를 **`KbRepositoryGuard`와 동일 토큰 세트**(내용 4 + 파일명 5)로 검사, allowlist(`kb/README.md`·`kb/public_regulation_catalog.csv`·`kb/ncr_placeholder.md`) 외 **원문 의심 파일** 발견 시 패키징 **실패**. ZIP **entry 이름만** 보던 기존 검증의 빈틈(중립 이름 원문)을 닫는다. 드리프트 방지: 토큰 mirror + SmokeTest 회귀로 `KbRepositoryGuard`와 `build/03`의 토큰/allowlist 불일치를 CI에서 차단한다.

---

## 3. 게이트 B 체크 (오프라인 Test PC, `docs/28`·`docs/41 §4`)

- [ ] `00~03` 통과(03이 해시·내용·금지파일·**원문 미포함 스캔** 자동검증)
- [ ] ZIP 내부: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`
- [ ] ZIP 내부: 모델파일·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx`·**내부규정/NCR 원문 0**
- [ ] **인터넷 차단** 실행 → **NoModelMode** · 자동업데이트/telemetry/외부 API 0
- [ ] R1: CP949·UTF-8·XLSX → 한도분석(6상태)·**대사(원천=분석 PASS)**·Excel Report·**화면=리포트 동일 수치**·History·Audit
- [ ] R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요), 내부/NCR은 **메타+표식만(원문 0)**, **NCR Rule Set 구조** 설명(검토용 초안)
- [ ] ReleaseNote SHA256과 `Get-FileHash` 일치 · 종료/재실행 정상

> 실 Test PC 미가용 시: **BLOCKED(Pilot PC 대기)**, 체크리스트 선완성.

---

## 4. GitHub Release 핸드오프 (로컬)
```powershell
git tag v0.6.0
git push origin v0.6.0
```
- 첨부 = portable ZIP + .sha256 + ReleaseNote-v0.6.0.md (소스/모델 첨부 금지).
- 본문 = §1 릴리스 노트 + 최종 ZIP **SHA256**.

## 5. 제외 / 운영 반입 (게이트 C — 사내, 범위 밖)
- portable ZIP만 반입 → SHA256 재검증 → 백신 → 압축해제 → `run.bat`(`docs/25`).
- 승인된 실 내부규정/NCR 원문·계수 = Prod 문서오너 승인 후 권한통제 KB. Local LLM은 R4(`docs/41 §3`) 전 미반입.

> 관련: `docs/42`(v0.5.0), `docs/34`(리허설), `docs/24`(패키징), `docs/41`(게이트), `docs/38`(Release Train).
