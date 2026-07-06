# Truth-Sync 점검 체크리스트 (docs/38·39·40·48)

문서별 정합 점검 항목. 각 항목은 **증거**(커밋 SHA / SmokeTest `Total=N PASS / 0 FAIL` / Gate 증거 문서 PATH)와 대조한다.
어휘는 [status-vocabulary.md](status-vocabulary.md) 7개만. 민감정보(실데이터·실 테이블/컬럼명·내부규정/NCR 원문·secret·모델파일·외부 NuGet/다운로드)는 문서에 넣지 않는다.

## 0. 공통(머지 직후)
- [ ] `git log`로 직전 머지 SHA·PR·WP-ID·테스트 총수 변화를 파악했다.
- [ ] 같은 사실(기준선 SHA, SmokeTest 합계, VERSION)이 여러 문서에서 **하나의 정본**으로 일치한다.
- [ ] 상태 표기마다 증거가 붙어 있다(증거 없는 `VERIFIED`/`PASS` 0건).
- [ ] 외부 의존성 필요 능력은 `APPROVAL_REQUIRED` + STOP로 표기(`docs/41`).

## 1. docs/38 — Master Roadmap & Capability/Traceability
- [ ] 기준선(현재 버전·main SHA·SmokeTest 정본 합계)이 실제 main과 일치한다.
- [ ] Release Train 표의 각 Release 상태가 실제 머지 상태와 일치(DONE/NEXT/BLOCKED/설계).
- [ ] Capability → Release 상태가 7개 어휘만 사용하고 증거와 일치한다.
- [ ] Traceability(Capability ↔ WP ↔ Test ↔ Gate, `§5`)에서 끊긴 링크/누락 WP가 없다.
- [ ] "재설계 금지" 완료 기능(MVP-1~3·R1·R3)을 다시 설계 중인 항목이 없다.

## 2. docs/39 — Work Package Backlog
- [ ] **Resume Brief**의 기준선 SHA·VERSION·SmokeTest 합계·NEXT UP이 최신이다.
- [ ] 머지된 WP가 진행 원장에 DONE + PR/커밋 + SmokeTest 수치로 기록됐다.
- [ ] NEXT UP은 **정확히 1개**이며, 그다음 후보와 구분된다.
- [ ] Archived(재실행 금지) 프롬프트 목록이 최신이다(완료 WP 프롬프트 추가).
- [ ] 테스트 총수 감소가 있으면 사유·매핑이 기록됐다(삭제·약화 금지).
- [ ] 재현 검증 명령(빌드 + SmokeTest, `Total=` grep)이 현행과 맞다.

## 3. docs/40 — ADR (Architecture Evolution)
- [ ] 새 아키텍처 결정이 ADR로 기록됐고 상태(채택/설계만/STOP)가 맞다.
- [ ] ADR이 절대 원칙(Offline·NuGet 0·NoModel·해시 Audit)과 모순되지 않는다.
- [ ] 승인 필요 결정(예: Local LLM Runtime, 모델파일)은 `APPROVAL_REQUIRED` + 승인 문서 PATH(`docs/41`)로 묶여 있다.
- [ ] 기각된 대안/결과가 코드 현실과 일치한다.

## 4. docs/48 — Gate B/C 증거 (현재 v0.7.0)
- [ ] 전체 판정이 실 증거와 일치한다(실 오프라인 Test PC 증거 없으면 `BLOCKED` 유지).
- [ ] 각 항목 상태가 `PASS`/`BLOCKED`/`PENDING` 등으로 증거(트리·로그·캡처·`Get-FileHash`)와 함께 기록됐다.
- [ ] **하나라도 누락/불일치면 전체 BLOCKED 유지** 규칙을 어기지 않았다.
- [ ] ZIP 금지파일 0(모델·real_data·internal_*·secrets·*.pem/key/pfx·내부규정/NCR 원문) 항목이 살아 있다.
- [ ] historical 문서(예: `docs/44` v0.5, `docs/45` v0.6)와 현재 정본(`docs/48` v0.7)의 역할 구분이 유지된다.

## 5. 마감
- [ ] 작업은 `planning/*` 브랜치에서 했다(main 직접 수정/병합 0).
- [ ] 정정 요약을 `<문서 §항목>: <기존> → <정정> (근거: <SHA/Total/문서PATH>)` 줄 목록으로 정리했다.
- [ ] 후속: `/risk-status-sync`로 기준선/Resume Brief 반영, 필요 시 `/risk-codex-review`와 연계.
