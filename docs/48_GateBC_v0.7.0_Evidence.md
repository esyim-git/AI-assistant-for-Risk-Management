# 48. Gate B/C Evidence — v0.7.0 (R1 + R3 + R2)

## 목적 / 상태
v0.7.0 portable ZIP을 **실 오프라인 Test PC(Gate B)** 및 **운영 반입 / Excel 2021(Gate C)** 에서 실행해 `docs/41 §4`·`docs/47 §3` 체크리스트를 **증거 기반**으로 봉인하기 위한 **정본** 문서. v0.6.0 정본은 `docs/45`(R1+R3) — 본 문서는 그 위에 **R2(Risk Analytics & Visualization)** 항목을 추가한다.

- **현재 판정: 🔴 BLOCKED** — **선행 A(패키지 컷)는 충족**(v0.7.0 정식 릴리스 완료, 아래 A1~A3 PASS), 미충족은 **실 오프라인 Test PC 증거**뿐(repo/CI/로컬 빌드 아티팩트로 생성 불가). 단, WPF portable UI에 노출되지 않은 R2 내부 경로는 **local-gate 전용 증거로만 기록**하고 Test PC PASS로 봉인하지 않는다.
- **2026-06-30 사용자 수동 Gate B 검증(부분 수행)**: 실제 Release ZIP로 `docs/47 §3` Gate B 10항목 중 다수가 **user-reported PASS**(아래 **§B′**) — **단, 실 증거(`Get-FileHash` 출력·스크린샷·기동 로그)가 repo에 미첨부**이므로 정식 봉인 PASS가 아니라 **user-reported**로만 기록한다(`CLAUDE.md §11.4`·본 문서 §판정 규칙). **B-5 SQL/VBA/Excel 검사 = 🟡 PARTIAL**(Excel 함수 상세설명/예시/대체식 부재 → 신규 **UX-WP-04**; Smart Assist/입력중 추천 부재 → **UX Enhancement**, Gate B blocker 아님), **B-6 CSV/XLSX 분석·B-8 Excel Report = ⬜ PENDING**. 증거 미첨부 + 미수행 항목 → **전체 Gate B는 🔴 BLOCKED 유지**.
- **판정 규칙**: Gate B/C는 아래 Test PC 실행 항목이 `PASS`이거나, 문서에 명시된 `N/A`/`ACCEPTED_RISK` 예외여야 봉인할 수 있다. 하나라도 누락/불일치면 그 항목 `BLOCKED/FAIL` + 전체 BLOCKED 유지. **실 PC 증거 없이 PASS로 적지 않는다**(`CLAUDE.md §11.4`).
- **기준선**: 문서 truth-sync 기준 `dafa63b`(#91), VERSION `0.7.0`, **v0.7.0 정식 릴리스 태그 `30c1cfb`**(미서명 portable ZIP), 정본 SmokeTest `Total=714 PASS=714 FAIL=0`.

> 기록: v0.7.0 패키징은 Codex가 `build/00~03 -Version 0.7.0` PASS 보고 — **published GitHub Release ZIP SHA256 = `42C835983127B127438AB97747B99FD0C3FA2E4363D4CB85641E45FE62E09DD5`**, manifest `version=0.7.0`(entries 26), ReleaseNote Build Commit `30c1cfb`. 단 **오프라인 Test PC 실행**이 Gate B/C 정본 증거다(아래 B·C는 미충족).
> ⚠️ ZIP 해시는 rebuild-from-tag로 재현 검증하지 않는다. `approved_manifest.json`에 `generatedAtUtc`가 포함되어 같은 commit을 나중에 다시 빌드해도 ZIP hash가 달라질 수 있다. Gate B/C는 **GitHub Release에 게시된 ZIP asset과 그 `.sha256`/Release 본문 SHA256**을 `Get-FileHash`로 대조한다.

---

## A. 선행 (패키지 컷 — 로컬 Windows) — ✅ 충족
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| A1 | `build/00~03 -Version 0.7.0` 전부 PASS (해시·금지파일·**원문 미포함 스캔** 포함) | ✅ PASS | Codex local-gate(#90), 콘솔 로그 |
| A2 | Published Release asset ZIP SHA256 = `.sha256` = Release 본문 SHA256 (`42C835…`) | ✅ PASS | GitHub Release `v0.7.0` + `Get-FileHash` + Release 본문 |
| A3 | 태그 `v0.7.0` = `30c1cfb`; 문서 truth-sync 기준 = #91 `dafa63b`; `ExpectedVersion == VERSION`(0.7.0) drift 가드 | ✅ VERIFIED | `git ls-remote origin refs/tags/v0.7.0` + REL-v0.7.0 `PackagingTests:331`, SmokeTest `Total=714` |

## B. Gate B — 오프라인 Test PC (`docs/47 §3`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| B0 | **Test PC로 반입한 ZIP 자체를 `Get-FileHash`로 재대조** — SHA256 = published Release 값 `42C835983127B127438AB97747B99FD0C3FA2E4363D4CB85641E45FE62E09DD5` | 🟢 user-reported (B′ B-1) — 증거 첨부 시 봉인 | Test PC `Get-FileHash` 출력 + Release URL |
| B1 | ZIP 내부 필수: `RiskManagementAI.exe`·`run.bat`·`config/ rules/ kb/ templates/ samples/ deploy/ logs/ reports/`·`approved_manifest.json`(version `0.7.0`) | ⬜ | 트리 |
| B2 | ZIP 내부 금지 **0**: 모델(`*.gguf/*.bin/*.safetensors/*.onnx/*.pt`)·`real_data/`·`internal_*`·민감정보 디렉터리·`credentials/`·`exports/`·`*.pem/*.key/*.pfx/*.p12/*.cer/*.crt/*.der/*.env`·**내부규정/NCR 원문** | ⬜ | 트리/스캔 + build/03 출력 |
| B3 | **인터넷 차단** 실행 → **NoModelMode 기동**(무결성 검증 PASS, manifest version `0.7.0`) · 자동업데이트/telemetry/외부 API **0** | 🟢 user-reported (B′ B-3·B-4) — 증거 첨부 시 봉인 | 차단 캡처 + 기동 로그 |
| B4 | R1: **CP949·UTF-8·XLSX** 입력 → 한도분석 **7상태**(incl `DUPLICATE_LIMIT`) | 🟡 user-reported(한도분석 실행, B′ B-7); 입력 다양성·7상태 상세·증거 = B-6 PENDING | 화면 |
| B5 | R1: **대사 9종**(원천합계=분석합계 PASS) | ⬜ | 화면/로그 |
| B6 | R1: **화면=리포트 동일 수치**(LIMIT_MONITORING == 대시보드, `DuplicateLimitCount` 노출) | ⬜ | 캡처 2장 |
| B7 | **R2 local-gate 전용**: 대용량 CSV streaming parity·행/바이트 상한·Welford/Outlier parity는 현재 WPF portable UI에 직접 노출되지 않음. Test PC에서 PASS로 봉인하지 말고, 후속 UI/harness가 생기기 전까지 local-gate 증거로만 유지 | N/A (local-gate) | SmokeTest `Csv`/`DataProfile` 도메인 + #81 local-gate |
| B8 | **R2 local-gate 전용**: Prior-Day Analytics(`PriorDayAnalyzer`)는 현재 WPF portable UI call site 없음. Test PC에서 PASS로 봉인하지 말고, 후속 UI/harness가 생기기 전까지 local-gate 증거로만 유지 | N/A (local-gate) | SmokeTest `Limit`/`Reconciliation` 도메인 + #84 local-gate |
| B9 | **R2-신규**: **`RISK_VISUAL` 시트 생성**(7상태 분포·TopN·집중도 HHI·Heatmap·`MIXED_CURRENCY`) + **Exception Count = 정확 숫자**(COUNTA 아님, Number SoT) | ⬜ PENDING (B′ B-8 — 다음 사용자 액션) | 리포트/화면 |
| B10 | **R2-신규**: **WPF Shapes/Canvas 화면 차트** 렌더(외부 charting NuGet 0, 다양 창크기) | ⬜ | 캡처 |
| B11 | R3: **KB 검색** → 인용(문서명·버전·시행일·조항·출처·검색기준일·검토필요) | ⬜ | 화면 |
| B12 | R3: 내부/NCR = **메타+표식만(원문 0)** · **NCR Rule Set 구조** 설명(검토용 초안, 계산 아님) | ⬜ | 화면 |
| B13 | SQL/VBA/Excel **검사**(자동실행 0) 동작 | 🟡 user-reported PARTIAL (B′ B-5) | 정상/비정상 입력 검사결과 표시 user-reported; **Excel 함수 상세설명/예시/대체식 부재 → UX-WP-04**; Smart Assist 부재 → UX Enhancement(blocker 아님) |
| B14 | **History** 기록 + **Audit JSONL(해시)** | 🟢 user-reported (B′ B-9) — 증거 첨부 시 봉인 | 캡처 |
| B15 | 종료/재실행 정상 | 🟢 user-reported (B′ B-10) — 증거 첨부 시 봉인 | — |

## B′. 사용자 수동 Gate B 검증 라운드 (2026-06-30, 실제 Release ZIP) — 부분 수행
> `docs/47 §3` Gate B 10항목 기준 실제 사용자 수동 검증. **본 라운드는 일부만 수행**(B-6·B-8 PENDING) → 전체 Gate B 봉인 아님(상단 🔴 BLOCKED 유지). 위 granular B0~B15와 매핑.
> **⚠️ 아래 ✅는 전부 `user-reported`** — 실 증거(`Get-FileHash` 출력·스크린샷·기동/Audit 로그)가 repo에 미첨부이므로 **정식 봉인 PASS가 아니다**(`CLAUDE.md §11.4`, §판정 규칙). 증거 첨부 시 항목별 봉인 PASS로 승격.

| 항목(`docs/47 §3`) | 결과 | 비고 | granular 매핑 |
|---|---|---|---|
| B-1 SHA256 확인 | ✅ PASS | — | B0 |
| B-2 ZIP 압축 해제 | ✅ PASS | (필수 트리 enumerate는 후속) | B1 |
| B-3 오프라인 실행 | ✅ PASS | 인터넷 차단 기동 | B3 |
| B-4 NoModelMode 기동 | ✅ PASS | — | B3 |
| B-5 SQL/VBA/Excel 검사 | 🟡 **PARTIAL PASS** | 정상입력=문제없음 표시·비정상입력=변수누락 등 검사결과 표시 **정상**. **잔여** ① Smart Assist/입력중 추천문구/snippet 자동완성 **없음** → **UX Enhancement**(Gate B blocker 아님). **코드 조사(2026-06-30) 결과 = Ctrl+Space-only by-design**(`RegisterCompletionTextBox`가 `TextChanged` 미연결) → **B-5는 회귀 아님**. 정적 범위 입력중 추천은 **UX-WP-05 as-you-type 트리거(#103 DONE, local-gate)**·**UX-WP-06 팝업 표시 확장(#104 DONE, local-gate)**, 실시간 LLM 랭킹 = R4 미구현 ② Excel 검사가 단순 함수 차단 수준 — 함수 상세설명·사용예시·Excel 2021 대체식 → **UX-WP-04 Excel Function Helper(#102 DONE, local-gate)**. **후속 개선 3건 구현 완료(local-gate)** — 단 실행 Release UI에서 실제 표면화(as-you-type 팝업·Excel Helper view) 재확인은 **Gate B(Test PC)** 대기 → B-5 재검증 필요(현 PARTIAL 유지). | B13 |
| B-6 CSV/XLSX 샘플 분석 | ⬜ **PENDING** | **다음 사용자 액션** | B4 입력/DataProfile |
| B-7 한도분석 실행 | ✅ PASS | 7상태/`DUPLICATE_LIMIT` 상세 캡처는 후속 | B4 |
| B-8 Excel Report 생성 | ⬜ **PENDING** | **다음 사용자 액션** | B9 (`RISK_VISUAL`) |
| B-9 History/Audit Log | ✅ PASS | 해시 Audit | B14 |
| B-10 종료 후 재실행 | ✅ PASS | — | B15 |

**분류 결정**: ① Smart Assist 부재 = **UX Enhancement**(Gate B blocker 아님) → **UX-WP-05/06 DONE**(#103/#104, local-gate). ② Excel 함수 상세설명/예시/대체식 = **UX-WP-04 Excel Function Helper DONE**(#102, local-gate). **3건 모두 구현 완료(local-gate)** — 실 Release UI 표면화 재확인 = Gate B(Test PC) 대기이므로 B-5는 재검증 전까지 PARTIAL 유지(과대표기 금지).
**다음 사용자 액션**: **B-6 CSV/XLSX 샘플 분석**, **B-8 Excel Report 생성**(→ granular B4·B9 증거 보강). 완료 시 본 §B′ 표 + granular 행 갱신 후 Gate B 봉인 재판정.

## B″. Gate B 실행 런북 (사용자 Test PC — 턴키 실행 순서)
> 목적: 미결 B 항목(B-6·B-8 PENDING, B-5 재검증)을 실 오프라인 Test PC에서 실행해 **증거**를 확보한다. 각 단계는 스크린샷/로그/해시 텍스트를 남겨 `evidence/gateB/`에 저장하고, 아래 §B/§B′ 표의 해당 행을 갱신한다. **실 증거 없이는 PASS로 적지 않는다(§11.4).**
>
> ⚠️ **어느 ZIP으로 하나 (중요)**:
> - **현 published v0.7.0 ZIP(`30c1cfb`)로 지금 가능** → **B-6 CSV/XLSX·B-8 Excel Report/RISK_VISUAL** + 이미 user-reported인 B-1/B-3/B-4/B-7/B-9/B-10 **증거 봉인**. (R1 입력·한도·대사 + R2 RISK_VISUAL은 `30c1cfb`에 포함.)
> - **신규 빌드 필요 → B-5 재검증**: Excel Function Helper(UX-WP-04)·Smart Assist as-you-type/팝업/포커스(UX-WP-05~11)는 `30c1cfb` **이후** 머지라 현 v0.7.0 ZIP에 **미포함**. 실 UI 재확인은 main `7094d91`에서 `build/00~03` 재패키징한 **신규 ZIP**에서만 가능(**Test PC 증거용 로컬 빌드 절차 = 아래 §5a, 승인 불요**; 이 빌드는 공개 게시하지 않는다. 공개 v0.7.1 릴리스 컷은 별도 REL WP + 버전 범프 + 승인 필요, §11.5).

### 0. 준비
- Test PC = Windows 11 · **인터넷 차단** · Excel 2021(Gate C). ZIP 반입 후 `evidence/gateB/` 폴더 생성.

### 1. 무결성·기동 봉인 (B0/B1/B2/B3 — 현 v0.7.0 ZIP)
```powershell
Get-FileHash .\RiskManagementAI_v0.7.0.zip -Algorithm SHA256   # = 42C835983127...E09DD5 대조 → 출력 저장
Expand-Archive .\RiskManagementAI_v0.7.0.zip -DestinationPath .\rmai
Get-ChildItem -Recurse .\rmai | Select-Object FullName | Out-File evidence\gateB\tree.txt   # 필수 트리 + 금지파일 0 확인
# 네트워크 차단 확인 후:
.\rmai\run.bat                                                 # NoModelMode 기동·manifest version 0.7.0 검증
```
증거: 해시 출력 · `tree.txt`(모델/원문/민감정보 0) · 차단 캡처 · 기동 로그. → §B B0/B1/B2/B3 봉인.

### 2. B-6 CSV/XLSX 샘플 분석 (PENDING → 봉인 대상)
- `samples/`의 CSV(CP949·UTF-8)·XLSX를 로드 → DataProfile·한도분석 실행.
- 캡처: 입력별 로드 성공 · 프로파일 결과 · 7상태(incl `DUPLICATE_LIMIT`). → §B B4 봉인.

### 3. B-8 Excel Report / RISK_VISUAL (PENDING → 봉인 대상)
- 리포트 생성 → `RISK_VISUAL` 시트(7상태 분포·TopN·집중도 HHI·Heatmap·`MIXED_CURRENCY`) · **Exception Count = 정확 숫자**(COUNTA 아님).
- **화면=리포트 동일 수치** 캡처 2장(대시보드 ↔ LIMIT_MONITORING). → §B B6/B9 봉인.

### 4. Gate C — Excel 2021 + 환경 (C1/C2/C3/C4b/C6/C7)
- **C1/C2**: 생성 리포트를 Excel 2021로 열기(**`RISK_VISUAL` 수동열기 포함**) → **Formula Error 0 · External Link 0 · Macro 0 · Formula Injection 0**. → §C C1/C2 봉인.
- **C3**: 백신/EDR 스캔 통과 로그 → `evidence/gateB/c3-edr.txt`. → §C C3.
- **C4b**: 앱 기동 시 런타임 Fail-Closed 무결성 게이트 동작(정상=기동·변조/부재/축소=차단) 캡처 → `c4b-boot.png`. (코드=VERIFIED, 실 기동 증거만 대기.) → §C C4b.
- **C6**: 대용량 입력 처리 시간·메모리 측정값 → `c6-perf.txt`. → §C C6.
- **C7**: Rollback(이전 릴리스 복귀) 절차 확인 기록 → `c7-rollback.txt`. → §C C7.
> C4(PDB/개인경로 0)=build/01·03 자동가드로 충족 · C5(코드서명)=미서명 `ACCEPTED_RISK`/BLOCKED(STAB-WP-05 후속) — 실행 항목 아님.

### 5. B-5 재검증 (⚠️ 신규 빌드 후에만)
현 published v0.7.0 ZIP(`30c1cfb`)에는 UX-WP-04~11(Excel Helper·Smart Assist as-you-type/팝업/포커스)이 **미포함**이므로, B-5 실 UI 재확인은 **main `7094d91`에서 새로 빌드한 ZIP**에서만 가능하다.

#### 5a. Dev PC에서 Gate B 검증용 ZIP 굽기 (test-only 로컬 빌드 — 승인 불요)
> **Dev PC**(.NET 8 SDK + PowerShell, 인터넷 허용)에서 실행. 이 ZIP은 **Test PC 증거용 로컬 빌드**이지 GitHub Release가 아니다 — Dev→Test 파이프라인 정상 흐름(§2 환경분리). 새 컷을 **공개 릴리스로 게시하지 않는다**(공개 v0.7.1 릴리스는 별도 REL WP + 버전 범프 + 승인 필요, §11.5).
```powershell
# Windows PowerShell 5.1(내장 powershell.exe) 또는 PowerShell 7 모두 동작. 기존 릴리스 문서(docs/42)와 동일 호출 규약.
git fetch origin main; git checkout 7094d91           # 정본 HEAD(=main). VERSION 파일 = 0.7.0
./build/00_check-prereqs.ps1                           # SDK/도구 확인
./build/01_publish-win-x64.ps1                         # self-contained win-x64 (PDB/Debug 0)
./build/02_package-release.ps1                         # → artifacts\release\RiskManagementAI-v0.7.0-win-x64-portable.zip (+ .sha256)
./build/03_verify-package.ps1                          # manifest·금지파일 0·원문 스캔 PASS 확인
Get-Content artifacts\release\RiskManagementAI-v0.7.0-win-x64-portable.zip.sha256   # ← 이 빌드의 SHA256 (기록)
```
> 실행 정책 차단 시(기관 PC): `powershell -ExecutionPolicy Bypass -File .\build\01_publish-win-x64.ps1` 형태로 개별 호출. `pwsh`(PowerShell 7) 미설치를 전제하지 않는다 — Windows 내장 5.1로 충분.
> ⚠️ **버전 충돌 주의**: 이 ZIP도 VERSION 파일 기준 `0.7.0`으로 라벨되지만 published `30c1cfb` v0.7.0과 **콘텐츠가 다르다**(UX-WP-04~11 포함). 따라서 **B-0 해시 대조는 `42C835…`가 아니라 이 빌드 자신의 `.sha256` 출력**으로 한다. 혼동을 막으려면 ZIP 파일명에 `-gateB-7094d91` 접미사를 붙여 보관(예: `RiskManagementAI-v0.7.0-gateB-7094d91-win-x64-portable.zip`)하고 published 릴리스 ZIP과 별도 폴더에 둔다.

#### 5b. Test PC에서 B-5 실 UI 확인
- 위 5a ZIP을 Test PC(인터넷 차단)로 반입 → `Get-FileHash`로 5a의 `.sha256` 값과 대조 → `run.bat` NoModelMode 기동(무결성 PASS).
- **Excel Function Helper view**(검색·함수 상세·인수·리스크예시·Excel 2021 대체식) · **Smart Assist as-you-type 팝업**(입력중 추천, 자동삽입 0) · **Esc/Close 포커스 복원** 실 UI 확인. 캡처 → `evidence/gateB/B5-*.png`.
> ⚠️ **B-5는 5a 빌드로 "봉인(SEAL)"하지 않는다 — 이건 출하 아티팩트가 아니다.** 공개 v0.7.0 출하본(`30c1cfb`)에는 UX-WP-04~11이 **미포함**이므로, 출하본 기준 B-5(B13)는 **PARTIAL 유지**가 정답이다. 5a 라운드 결과는 §B B13 상태를 PASS로 바꾸는 게 아니라 **"조건부 — 코드 실 UI 표면화 확인(빌드 `7094d91`, test-only), 출하 봉인은 공개 v0.7.1 컷 후"**로만 기록한다(§11.4: 출하되지 않는 빌드로 출하본 게이트를 PASS 표기 금지).
> 즉 5a 증거의 의미 = **"이 기능 코드가 실 WPF UI에서 동작함"의 회귀 근거**이지, **"사용자가 받는 v0.7.0에서 동작함"의 봉인이 아니다.** 후자는 그 UI를 실제로 담은 공개 컷(v0.7.1, REL WP+승인) 출하 시에만 봉인. §B B0(published `42C835…`) 봉인은 현 published ZIP 라운드(R1)에서 그대로 유지. 두 라운드 증거는 §증거 워크시트에 분리 기입.

### 6. 회신
- 각 단계 완료 시 §B/§B′ 표의 상태 + 증거 파일명 기입 후 회신 → 항목 단위 재판정 → Gate B 봉인 여부 갱신. (Test PC 대상 PASS + 명시 예외(B7/B8/C5) 수용 시 봉인.)

## C. Gate C — 운영 반입 / Excel 2021 (`docs/41 §4`·`docs/28`)
| # | 항목 | 상태 | 증거 |
|---|---|---|---|
| C1 | Excel 2021에서 Report Open(**`RISK_VISUAL` 시트 수동열기 포함**) · **Formula Error 0** | ⬜ | 캡처 |
| C2 | **External Link 0 · Macro 0 · Formula Injection 0**(`RISK_VISUAL`은 Number/text 정적값만) | ⬜ | 검사 |
| C3 | 백신/EDR 통과 | ⬜ | 로그 |
| C4 | **PDB/개인경로 0**(자동 가드 + Integrity Manifest 생성/검증 = STAB-WP-03a, build/01·03) | ⬜ / 자동분 03a로 충족 | 스캔 |
| C4b | **런타임 Fail-Closed 무결성 게이트**(STAB-WP-03b, #61 — 앱 시작 시 manifest version `0.7.0` 검증, 변조/부재/축소/co-deletion=차단). **local-gate VERIFIED**; 실 Test PC 기동 증거만 대기 | ⬜ (코드=VERIFIED; Test PC 기동 BLOCKED) | 기동 로그·차단 캡처 |
| C5 | Code Signing 상태 = **미서명(placeholder)**. v0.7.0 범위에서는 `ACCEPTED_RISK`/`N/A` 예외로 기록 가능하나, 사내 반입 정책이 서명 필수이면 Gate C는 **BLOCKED**로 유지한다. 독립 신뢰 앵커 = **STAB-WP-05 APPROVAL_REQUIRED**(`docs/40` ADR-012 / `docs/41 §6`; 인증서 경로 A~D 결정 선행). 03b 잔여 = **콘텐츠 co-tamper + 런타임 DLL 미해시 + 폴더 동반 변조**, 서명 후 폐쇄 | N/A / ACCEPTED_RISK(v0.7.0) | 미서명 반입 정책 확인 |
| C6 | Performance / Memory 측정(현재 WPF 공개 경로 기준; streaming 대용량 경로는 B7처럼 local-gate 전용, UI/harness 전까지 Test PC PASS 대상 아님) | ⬜ | 측정값 |
| C7 | Rollback 절차 확인(미서명 ZIP 반입 정책 → 서명본 대체 가능 시 STAB-WP-05) | ⬜ | 기록 |

---

## 증거 메타 (각 항목 공통 기입)
`PASS/FAIL/BLOCKED · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.

### 증거 워크시트 (복사해서 채운 뒤 회신 — 실 PC 실행분만 PASS)
> 실행 라운드별로 분리 기입. **R1 = 현 published ZIP(`30c1cfb`, SHA `42C835…`)**, **R2 = 5a 로컬 빌드(`7094d91`, 자체 `.sha256`)**. 미실행 항목은 공란 유지(임의 PASS 금지, §11.4).

| 항목 | 라운드 | 실행? | 결과(PASS/FAIL/BLOCKED) | 증거 파일(`evidence/gateB/…`) | 해시/측정값 | 검증자·시각 |
|---|---|---|---|---|---|---|
| B0 ZIP 해시 대조 | R1 | ☐ | | hash-R1.txt | =`42C835…`? | |
| B1 필수 트리 | R1 | ☐ | | tree.txt | — | |
| B2 금지파일 0 | R1 | ☐ | | tree.txt / build03.txt | 0건 | |
| B3 오프라인 NoModel 기동 | R1 | ☐ | | boot-log.txt / net-block.png | manifest 0.7.0 | |
| B4 CSV/XLSX 입력·7상태 (B-6) | R1 | ☐ | | b6-*.png | — | |
| B5 대사 9종 | R1 | ☐ | | b5recon-*.png | — | |
| B6 화면=리포트 동일수치 | R1 | ☐ | | b6-dash.png/b6-report.png | — | |
| B9 RISK_VISUAL + Exception Count (B-8) | R1 | ☐ | | b8-riskvisual.png | 정확 숫자 | |
| B10 WPF 화면차트 | R1 | ☐ | | b10-chart.png | — | |
| B11 KB 검색 인용 | R1 | ☐ | | b11-*.png | — | |
| B12 NCR 메타/구조(원문 0) | R1 | ☐ | | b12-*.png | — | |
| B14 History + Audit(해시) | R1 | ☐ | | b14-*.png | hash-only | |
| B15 종료/재실행 | R1 | ☐ | | b15-*.png | — | |
| C1/C2 Excel 2021 열기(RISK_VISUAL)·Formula/Link/Macro/Injection 0 | R1 | ☐ | | c1-*.png | Formula/Link/Macro 0 | |
| C3 백신/EDR 통과 | R1 | ☐ | | c3-edr.txt | — | |
| C4b 런타임 Fail-Closed 기동(변조/부재/축소 차단) | R1 | ☐ | | c4b-boot.png | manifest 0.7.0 (코드=VERIFIED, 기동증거만) | |
| C6 Performance/Memory 측정(WPF 공개 경로) | R1 | ☐ | | c6-perf.txt | 시간·메모리 | |
| C7 Rollback 절차 확인 | R1 | ☐ | | c7-rollback.txt | — | |
| B13 Excel Helper·Smart Assist(B-5) | R2 | ☐ | | b13-*.png | **조건부(코드 표면화 확인) — 출하 봉인 아님; 공개 v0.7.1 후 봉인** | |
| B0' 5a 빌드 해시 | R2 | ☐ | | hash-R2.txt | =5a `.sha256` | |

> **명시 예외(워크시트 PASS 대상 아님)**: B7/B8(대용량 streaming·Prior-Day = local-gate 전용, WPF UI 미노출) · C4(PDB/개인경로 = build/01·03 자동가드로 충족) · C5(코드서명 = 미서명 `ACCEPTED_RISK`/BLOCKED, STAB-WP-05 후속). 위 §B/§C 참조.

## 회신 → 판정
운영자가 표를 채워 회신하면 항목 단위로 PASS/BLOCKED/N/A/ACCEPTED_RISK 재판정 → 본 문서 상단 상태 갱신. Test PC 대상 항목이 PASS이고 명시 예외(B7/B8/C5)가 수용되면 `docs/47 §3` Gate B + Gate C를 봉인한다. 예외가 사내 정책상 수용되지 않으면 전체 상태는 BLOCKED 유지(`CLAUDE.md §11.4` 준수 — 실 PC 증거 전까지 PASS 금지).

> 관련: `docs/41`(게이트)·`docs/47`(v0.7.0 릴리스)·`docs/45`(v0.6 증거·양식 출처)·`docs/40`(ADR-008 무결성·ADR-012 코드서명)·`docs/28`(보안검토)·`docs/39`(REL-v0.7.0·STAB-WP-05).
