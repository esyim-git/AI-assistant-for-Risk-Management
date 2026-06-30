# Status Sync Checklist (상태 동기화 점검 항목)

> `/risk-status-sync` 보조 체크리스트. **읽기·진단 전용** — 이 스킬은 문서/코드를 수정하지 않는다. 불일치 발견 시 `/risk-doc-truth-sync`로 위임한다.
> 모든 비교는 실제 `main` 기준. 보고에 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보 금지. 예시는 더미명(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`)만 사용한다.

## 1. 기준선 (Baseline)
- [ ] `git log --oneline -10`으로 실제 main HEAD SHA 확보.
- [ ] `git status --short`로 미커밋 변경/현재 브랜치 확인(계획 작업은 `planning/*`, main 직접 수정 금지 — `CLAUDE.md §11.1`).
- [ ] `docs/39` ★ Resume Brief의 `현재 기준선` SHA·VERSION = 실제 main HEAD인가.
- [ ] `docs/38 §0` 기준선(버전·정본 SmokeTest 합계) = Resume Brief와 일치하는가.
- [ ] `AGENTS.md §0` 현재 기준선 버전 = 위와 일치하는가.
- [ ] (해당 시) v0.6.0 정식 릴리스 태그 SHA가 문서 표기와 일치하는가.

## 2. SmokeTest 정본 합계
- [ ] 정본 근거는 `Total=` 라인 단일 출처(`docs/39` 재현 검증). 임의 추정치(과거 미집계 수치) 사용 금지.
- [ ] Resume Brief의 `SmokeTest Total=N PASS / 0 FAIL` = `docs/38 §0` 정본 합계와 일치하는가.
- [ ] 테스트 총수 변경이 있었다면 사유·매핑이 기록되었는가(삭제·약화 금지 — `AGENTS.md §3`).
- [ ] 미분류 도메인(`Unclassified`) 잔존 여부(있으면 실패 신호).

## 3. Release Train (docs/38 §2)
- [ ] R1 / R3 상태 표기(DONE 등)가 실제 머지 이력과 일치하는가.
- [ ] STAB 진행 상태(NEXT / READY_FOR_CODEX / 부분 DONE)가 머지된 STAB-WP-* 와 일치하는가.
- [ ] PILOT(Gate B/C)은 실 Test PC 증거 없으면 **BLOCKED** 유지인가(과대표기 금지).
- [ ] 후속 Release(R2/KB/NCR/R4/R5/R6)가 "설계/계획"을 넘어 PASS·VERIFIED로 과대 표기되지 않았는가.

## 4. Traceability Matrix (docs/38 §5)
- [ ] 머지된 WP가 해당 Cap-ID 상태에 반영되었는가(예: STAB-WP-* DONE → C-12 갱신).
- [ ] 각 Cap-ID 상태 어휘가 정본만 사용하는가: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED (`CLAUDE.md §11.4`).
- [ ] NCR Rule Set 등 구조만 있는 항목이 SCAFFOLD_ONLY로 유지되는가(계수/원문 미적재).
- [ ] Gate 컬럼이 실 Test PC 증거 없이 PASS로 적히지 않았는가(`docs/45`).
- [ ] Risk Register(§6) RR 상태가 머지된 완화책과 일치하는가(필요 시 메모만).

## 5. NEXT UP
- [ ] Resume Brief NEXT UP이 **정확히 1개**의 WP를 가리키는가(여러 개 동시 지정 금지).
- [ ] NEXT UP이 이미 머지된 WP를 가리키고 있지 않은가(머지 후 갱신 누락 신호).
- [ ] NEXT UP 프롬프트 경로(`prompts/codex/<WP-ID>_*.md`)가 존재하는가(Glob 확인).
- [ ] Archived/재실행 금지 프롬프트를 NEXT UP으로 잘못 지목하지 않았는가.

## 6. 과대표기 가드 (최종)
- [ ] 실제 AI/RAG/NCR/Local LLM 능력을 코드/CI 증거보다 크게 표기한 곳이 없는가.
- [ ] "Gate PASS" 표기에 실 오프라인 Test PC 증거가 첨부되어 있는가(없으면 BLOCKED).
- [ ] STOP 규칙 대상(외부 NuGet/Vector/Embedding/LLM Runtime/모델파일)이 승인 문서 없이 "도입됨"으로 표기되지 않았는가(`AGENTS.md §4`).
- [ ] Local-Gate 모델(로컬 build+SmokeTest 증거 + Claude 코드리뷰)을 "GitHub CI green"으로 잘못 기술하지 않았는가(`CLAUDE.md §11.6`).

## 보고 (산출물 형태)
```text
현재 기준선 : main <SHA> · VERSION <x.y.z>
SmokeTest   : Total=<N> PASS / 0 FAIL
머지된 WP   : <WP-ID ...> (최근 PR #N)
NEXT UP     : <WP-ID 1개>
불일치      : - <문서:위치> 현재=<...> / 실제=<...> → 제안
              (없으면 "불일치 없음 (truth-sync 불필요)")
```
- 불일치가 있으면 항목별로 `/risk-doc-truth-sync`에 넘긴다(이 스킬은 직접 수정하지 않음).
- 착수할 WP가 정해지면 `/risk-wp-planner`로 연결한다.
