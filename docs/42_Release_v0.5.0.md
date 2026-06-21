# 42. Release v0.5.0 — R1 Data Foundation

## 목적 / 범위
v0.5.0 = **v0.4.0(MVP-1+2+3) + R1 데이터 파운데이션(WP-01~08)**. 본 문서는 v0.5.0 **릴리스 노트 + Codex/Windows 패키징 런북 + GitHub Release 핸드오프**다. `docs/34`(릴리스 리허설)·`docs/24`(패키징)·`docs/28`(게이트 B/C)를 v0.5.0 실행 단계로 구체화한다.

> 빌드·ZIP·태그는 **Windows + .NET 8 SDK + PowerShell**(Codex 로컬)에서 수행한다. 웹/Linux 세션 및 git proxy는 **태그 push가 403**으로 막히므로(이전 v0.3/v0.4 동일), **태그·Release 발행은 로컬에서** 한다.
> **전제**: `main`이 `76834e5`(WP-07 + Data Gate PASS + CP949 EOL 고정) 이상일 것. 릴리스는 이 시점 이후의 `main`에서 자른다.

---

## 1. v0.5.0 릴리스 노트 (요약)

**무엇이 바뀌었나 (v0.4.0 → v0.5.0): 리스크 한도 분석의 "데이터 정확성" 확립**

- **합성 한도 제거**: UI가 노출의 1.1배로 한도를 지어내던 로직 제거. 실 한도 없으면 `LIMIT_DATA_REQUIRED`/`DEMO_ONLY`로 차단(합성값 미사용). (WP-01)
- **입력 인코딩/형식 확장**: Golden6 CSV의 **CP949(Windows-949/UHC 전체)** 디코딩(내장 매핑표·SHA256 byte-stable 검증)과 **UTF-8(BOM/무BOM)**, **.xlsx 입력**(인박스 OOXML·zip 안전상한·XXE 차단) 지원. (WP-02/03)
- **승인형 컬럼 매핑**: 논리컬럼→물리컬럼 매핑(`config/column_mapping.json`, 기본=현행). 커스텀은 6열 완전·`config/` 경로 가드·safe fallback. (WP-04)
- **실 Exposure-Limit Join + 공통 분석결과**: BASE_DT·PORTFOLIO_ID·RISK_FACTOR 실 조인, 상태 6종(`NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT/MAPPING_ERROR`), 매핑 불일치 graceful. 단일 `LimitAnalysisResult`. (WP-05)
- **대사·예외검증 9종**: 미매핑·고아 한도·중복·기준일·통화·단위·음수0한도·건수증폭 + **원천합계=분석합계(증폭/누락 0)** 키스톤. (WP-06)
- **화면=리포트 일원화**: Excel Report가 공통 `LimitAnalysisResult`를 그대로 사용 → 대시보드와 리포트 수치 일치. 분기 산식 제거. (WP-07)
- **공통 CSV 파서 수렴**(WP-08). **SmokeTest 268 → 368**.

**유지된 절대 원칙**: 오프라인 · 외부 NuGet 0 · 외부 API/telemetry/자동업데이트 0 · SQL/VBA 자동실행 0 · 해시 전용 감사 · NoModelMode · 실데이터/내부규정원문/모델파일 미포함.

**아직 없는 것(의도적)**: Local LLM 추론 런타임/모델(R4 Model Approval Gate 전까지 설계만) · RAG/NCR 검색(R3) · 전일대비(R2). 통화/단위 대사의 승인형 매핑 전환은 R2 후속.

---

## 2. 패키징 런북 (Codex / Windows PowerShell)

```powershell
# 1) 최신 main에서 릴리스 브랜치
git fetch origin main
git switch -c release/v0.5.0 origin/main
git add --renormalize .   # CP949 매핑표 LF 정합(.gitattributes) 1회 확인

# 2) VERSION 정합(이미 0.5.0이어야 함)
Get-Content VERSION        # -> 0.5.0

# 3) 빌드 → 패키징 → 검증
./build/00_check-prereqs.ps1                      # .NET 8 SDK 필수(runtime-only면 실패가 정상)
./build/01_publish-win-x64.ps1  -Version 0.5.0    # self-contained win-x64 + 오프라인 자산 + 금지파일 가드
./build/02_package-release.ps1  -Version 0.5.0    # portable ZIP + .sha256 + ReleaseNote + DependencyList
./build/03_verify-package.ps1   -Version 0.5.0    # SHA256 무결성 + 필수자산 + 금지파일 부재
```

산출물(모두 `artifacts/` — **gitignored, repo 커밋 금지**):
```text
artifacts/release/RiskManagementAI-v0.5.0-win-x64-portable.zip
artifacts/release/RiskManagementAI-v0.5.0-win-x64-portable.zip.sha256
artifacts/release/ReleaseNote-v0.5.0.md
artifacts/release/DependencyList-v0.5.0.csv
```

---

## 3. 게이트 B 체크 (오프라인 Test PC, `docs/28`·`deploy/release_checklist.md`·`docs/41 §4`)

- [ ] `00~03` 스크립트 전부 통과(03이 해시·내용·금지파일 자동검증)
- [ ] ZIP 내부: `RiskManagementAI.exe`, `run.bat`, `config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/` 존재
- [ ] ZIP 내부: 모델파일(`*.gguf` 등)·`real_data/`·`internal_*`·`secrets/`·`*.pem/key/pfx` **없음**
- [ ] **인터넷 차단** 후 실행 → **NoModelMode 기동**, 자동업데이트/telemetry/외부 API 동작 0
- [ ] **CP949·UTF-8·XLSX** 입력 → 한도분석(6상태)·**대사(원천=분석 PASS)**·Excel Report 생성·History·Audit 확인
- [ ] **화면=리포트 동일 수치** 육안 확인(LIMIT_MONITORING == 대시보드 그리드)
- [ ] ReleaseNote/DependencyList의 SHA256과 `Get-FileHash` 재대조 일치
- [ ] 종료/재실행 정상

> 실 Test PC 미가용 시: 상태 **BLOCKED(Pilot PC 대기)**, 체크리스트는 선완성(`docs/41 §4`).

---

## 4. GitHub Release 핸드오프 (로컬에서 — 태그 push proxy 403 회피)

```powershell
git tag v0.5.0           # main(릴리스 시점) 커밋에
git push origin v0.5.0   # 로컬에서 직접(웹 세션 proxy는 태그 push 403)
```
- Release **첨부물 = portable ZIP + .sha256 + ReleaseNote-v0.5.0.md** (소스/빌드도구/모델 첨부 금지, `docs/29` 산출물 경계).
- Release 본문에 §1 릴리스 노트 + 최종 **ZIP SHA256 값** 기재.

### Release 본문 템플릿 (붙여넣기용)
```text
# v0.5.0 — R1 Data Foundation
v0.4.0(MVP-1+2+3) 위에 리스크 한도 분석의 데이터 정확성을 확립.
- 합성 한도 제거(DEMO_ONLY/LIMIT_DATA_REQUIRED), CP949(UHC)/UTF-8/XLSX 입력
- 승인형 컬럼 매핑, 실 Exposure-Limit Join + 6상태, 대사 9종(원천=분석 PASS)
- 화면=리포트 단일 LimitAnalysisResult 일원화. SmokeTest 268→368.
절대원칙 유지(오프라인·NuGet 0·외부 API/telemetry 0·해시 감사·NoModel).
Local LLM/RAG/NCR 미포함(승인 게이트 전 설계만).
SHA256(zip): <build/03 출력값 붙여넣기>
```

---

## 5. 제외 / 운영 반입 (게이트 C — 사내 절차, 본 범위 밖)
- portable ZIP만 반입 → SHA256 재검증 → 백신검사 → 압축해제 → `run.bat`. (`docs/25`)
- Local LLM은 **R4 Model Approval Gate**(`docs/41 §3`) 전까지 Runtime/모델 미반입.

> 관련: `docs/34`(리허설), `docs/24`(패키징), `docs/28`(게이트 B/C), `docs/41`(Data/Model/Pilot 게이트), `docs/38`(Release Train).
