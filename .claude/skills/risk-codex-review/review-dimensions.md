# Codex Result Review — 4축 상세 체크리스트

> Codex `feature/<WP-ID>-*` 브랜치 결과를 머지 전에 검토하는 4축(Diff/보안/테스트/문서) 상세 점검표.
> 검토는 **읽기 전용**(`git diff`/`git log`/Read/Grep/Glob)으로 수행한다. Claude는 main을 직접 수정/병합하지 않는다(`CLAUDE.md §11.1`).
> 우선순위: `AGENTS.md` > 지정 WP(`docs/39`) > Codex Prompt.

---

## 0. 사전 — 변경 범위 파악

```bash
git log --oneline origin/main..<branch>           # 커밋 수·메시지(Commit Subject에 (#PR) 규약)
git diff --stat origin/main..<branch>             # 변경 파일·라인 규모(작은 Diff)
git diff --name-only origin/main..<branch>        # 변경 파일 목록
```

- [ ] 지정된 단일 WP만 다루는가 (`docs/39` Resume Brief NEXT UP과 일치, 여러 WP 혼합 아님)
- [ ] Diff가 작은가 (불필요한 대량 리포맷/무관 파일 변경 없음)

---

## 1. Diff 축 — 범위·정확성·회귀

`git diff origin/main..<branch>` 전수 확인.

- [ ] 변경이 해당 WP **작업범위** 안이고 **제외범위**를 침범하지 않음(`docs/39` 해당 WP)
- [ ] WP의 **Public Interface** 정의와 시그니처 일치(임의 시그니처 변경 없음)
- [ ] 기존 동작 회귀 없음(삭제·치환된 분기의 대체 경로 확인)
- [ ] 쓰기 경로는 `logs/`·`reports/`·`config/`만(그 외 경로 쓰기 없음), 경로는 상대경로·경로 가드 우선(`AGENTS.md §3`)
- [ ] C# `nullable enable` 유지, 예외 메시지에 민감정보 없음
- [ ] 위험 검사 결과는 코드/심각도/메시지/위치 포함 형식 유지
- [ ] SQL/VBA/Golden6 **자동실행 경로 신설 0**, 운영 DB 접속문자열 0

---

## 2. 보안 축 — Gate A (`docs/28` 게이트 A)

Diff·신규 파일에 대해 점검. `/risk-security-guard` 스킬과 동일 기준.

- [ ] 외부 NuGet `PackageReference` 추가 **0** (`*.csproj` Diff 확인) — 1건이라도 있으면 STOP·승인문서(`AGENTS.md §4`, `docs/41`)
- [ ] 외부 API 호출 0 · Telemetry 0 · 자동 업데이트 0
- [ ] 실제 회사 데이터 없음(실거래/포지션/고객/계정)
- [ ] 실 테이블/컬럼/시스템명 없음 — 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 등 일반명만
- [ ] 내부규정 **원문** / NCR 공식본 **원문** 없음(공개 catalog/placeholder만)
- [ ] 비밀번호/토큰/API key/접속 문자열/인증서·키 파일 없음(`*.pem *.key *.pfx *.cer *.crt`)
- [ ] 모델 가중치 파일 없음(`*.gguf *.bin *.safetensors *.onnx`)
- [ ] `logs/`·`reports/`·`exports/` 실데이터 미포함, `.gitignore`가 차단
- [ ] Audit Log는 해시 기반(원문 미저장), NoModelMode 기본 유지

> Grep 보조(문서/룰/정책 파일의 "금지어 설명"은 오탐 — 문맥 확인): `docs/28` 게이트 A의 점검 명령 참조.

---

## 3. 테스트 축 — SmokeTest 보존 + 회귀

- [ ] 외부 테스트 프레임워크 추가 0 (인박스 SmokeTest 유지, `AGENTS.md §5`)
- [ ] **이전 Total 보존**: 직전 기준선 Total(예: `docs/39` Resume Brief의 `Total=N`) 이상
- [ ] WP별 **양성/음성 회귀** 신규 추가됨(기능 검증 단언 포함)
- [ ] 기존 단언 **삭제·약화 0**. 총수 감소 시 **사유·매핑** 명시(`AGENTS.md §3`)
- [ ] 미분류 도메인(`Unclassified`) 없음(STAB-WP-02 분류 규약)
- [ ] 보고에 합계 줄 **`Total=N PASS=N FAIL=0`** 포함, `FAIL=0`
- [ ] 머지 게이트 증거 = 로컬 `dotnet build`(Release) + SmokeTest 결과(GitHub-CI-green을 전제로 요구하지 않음, `CLAUDE.md §11.6`)

> 재현 검증(보고 대조용): `dotnet build RiskManagementAI.sln -c Release` → `dotnet run --project tests/RiskManagementAI.SmokeTests` → `=== SmokeTest Summary ===` 및 `Total=N PASS=N FAIL=0` 두 줄 확인.

---

## 4. 문서 축 — WP Checklist·docs 정합

- [ ] 해당 WP의 **Claude Review Checklist**(`docs/39`) 항목 전부 충족
- [ ] `docs/38`(Roadmap·Capability·Traceability) 영향 항목과 모순 없음
- [ ] `docs/40`(ADR) 결정과 모순되는 구현 없음
- [ ] 상태 표기는 정본 어휘만: `VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED`
- [ ] **과대표기 없음**: 구조만이면 `SCAFFOLD_ONLY`, 미적재면 `PLACEHOLDER`/`APPROVAL_REQUIRED`. 실 Test PC 증거 없는 Gate는 `BLOCKED`(PASS 아님)
- [ ] 문서 정합 정정이 필요하면 코드 머지와 분리해 `/risk-doc-truth-sync`로 처리

---

## 5. 판정 템플릿

```
[Codex Result Review] <branch> (WP-ID)
- 범위: 커밋 N개 / 변경 파일 M개 / Total <이전>→<이후>
- Diff    : PASS | 수정요청  — <근거 한 줄>
- 보안(GateA): PASS | 수정요청 — <근거 한 줄>
- 테스트  : PASS | 수정요청  — Total=N PASS=N FAIL=0
- 문서    : PASS | 수정요청  — <근거 한 줄>

수정요청(있으면):
- <항목> — <파일:라인> — <사유> — <기대>

판정: 머지 가능 | 머지 불가(수정요청 N건)
```

- 증거 없는 PASS/VERIFIED는 적지 않는다.
- 머지·브랜치 규약(PR 필수·Squash·main 직접 push 금지·force push 금지)은 `/risk-branch-governance` 참조.

---

## 참조
- `AGENTS.md §1~7` · `docs/28`(게이트 A) · `docs/39`(WP·Review Checklist) · `docs/38`/`docs/40`/`docs/48`(current Gate) · `docs/44/45`(historical Gate).
- 연계 스킬: `/risk-security-guard` · `/risk-doc-truth-sync` · `/risk-branch-governance`.
