---
name: risk-gate-bc
description: Prepare and record offline Test PC Gate B/C evidence for a release package.
argument-hint: "[version]"
arguments: [version]
disable-model-invocation: true
allowed-tools: Read Grep Glob Bash(git status *)
---

# Gate B/C Evidence

## 목적
Gate B(오프라인 Test PC)·Gate C(Excel 2021/운영 반입) 증거 원장(현재 정본 `docs/54`, historical `docs/48`/`docs/45`/`docs/44`)을 항목별로 채우고, 실 PC 증거가 없는 항목은 `BLOCKED`로 유지한다. **증거 없는 `PASS` 금지.**

## 언제 사용
- **수동 호출 전용 (`/risk-gate-bc`).** 모델 자동 호출 비활성(`disable-model-invocation: true`).
- 운영자가 오프라인 Test PC / Excel 2021 / 반입 검증 증거를 회신해 와서 원장을 갱신·봉인할 때.
- 릴리스 핸드오프 직전 Gate B/C 상태를 재판정할 때.

## 절대 원칙
- **실 오프라인 Test PC 증거가 없으면 항목 `BLOCKED` 유지** — `PASS`로 적지 않는다. user-reported(증거 미첨부)는 봉인 PASS가 아니라 `user-reported`로만 기록한다(`docs/54 §2·§4`). (CLAUDE.md §11.4)
- 상태 어휘는 정본만: `VERIFIED`/`PARTIAL`/`SCAFFOLD_ONLY`/`PLACEHOLDER`/`BLOCKED`/`NOT_IMPLEMENTED`/`APPROVAL_REQUIRED`. **과대표기 금지.**
- 전체 `PASS` 조건: **모든 적용 대상 항목 `PASS`**(문서에 명시된 `N/A`/`ACCEPTED_RISK` 예외 수용 포함). 하나라도 누락/불일치면 해당 항목 + 전체 `BLOCKED` 유지. (`docs/54 §4`)
- **증거 민감정보 금지**: 원본 증거 파일(`evidence/gateBC/*` — 해시 출력·스크린샷·로그)은 gitignored local-only이며 force-add/commit 금지다. **실거래/실포지션/고객정보·실 테이블/컬럼/시스템명·내부규정 원문·NCR 공식본 원문·계정/비밀정보·secret·모델파일**을 포함하지 않는다. 검증 입력은 `samples/` **dummy 데이터만** 사용하고, 캡처는 masking한다. 별도 정제 요약을 PR에 첨부할 때만 Gate A(`risk-security-guard`)를 적용한다. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`만.
- **릴리스 분리 규칙**: v0.7.1은 `docs/54`, v0.7.0은 `docs/48`에 기록한다. 서로 다른 릴리스의 PASS/user-reported 결과를 승계하지 않으며, **test-only 로컬 빌드로 출하본 게이트를 봉인(PASS)하지 않는다**.
- 이 스킬은 **프로세스/체크리스트 가이드**다. 코드·산출물 동작을 바꾸지 않으며, 원장 `.md`만 `Edit`한다.

## 절차
1. 대상 릴리스 Gate 시트 식별: **현재 정본 = `docs/54_GateBC_v0.7.1_Evidence.md`**. `docs/48`(v0.7.0)·`docs/45`(v0.6.0)·`docs/44`(v0.5.0)는 historical. 신규 릴리스는 별도 successor 원장을 만들고 초기 상태를 `BLOCKED`로 둔다.
2. 시트의 그룹별 항목 상태·증거 칸 확인: **A(선행 패키지 컷) / B(Gate B 오프라인) / C(Gate C Excel 2021·반입)**.
3. 항목별 증거 유무 판정: 스크린샷·콘솔/앱 로그·File Hash(`Get-FileHash` 대조)·측정값·검증자·검증시각·Test PC 사양이 **모두** 있으면 `PASS`, 하나라도 없으면 `BLOCKED` 유지.
4. Gate C(Excel 2021): **수식오류 0 · 외부링크 0 · Macro 0 · Formula Injection 0** 확인 항목을 별도 반영(`docs/41 §4`, `docs/28`). PDB/개인경로 0·성능·메모리·Rollback 포함.
5. ZIP SHA256 = ReleaseNote SHA256 대조(A2)는 **Test PC 재대조** 증거여야 인정. 데스크탑 대조만이면 `PARTIAL`.
6. 항목 상태를 `Edit`로 갱신. **전부 `PASS`일 때만** 시트 상단 판정을 봉인(`PASS`). 부분 충족은 항목 단위로 기록하고 전체 `BLOCKED` 유지.
7. 양식·항목 상세는 지원 파일 참조: [gate-b-template.md](gate-b-template.md), [gate-c-template.md](gate-c-template.md).

## 산출물/보고
- 항목별 `PASS`/`BLOCKED`(필요 시 `PARTIAL`) 원장 + 증거 메타:
  `상태 · Screenshot · Log · File Hash · 측정값 · 검증자 · 검증시각 · Test PC 사양`.
- 보고 한 줄 예: `Gate B = BLOCKED (B1~B6 PENDING, 실 Test PC 증거 대기) / Gate C = BLOCKED`.
- 봉인 시: 현재 Gate 시트(`docs/54` 또는 후속 릴리스별 successor) 상단 판정 갱신 + 전체 `PASS` 사유(적용 대상 증거 충족)를 명시.

## 체크리스트
- Gate B(오프라인 실행) 항목·증거 양식: see [gate-b-template.md](gate-b-template.md)
- Gate C(Excel 2021/반입) 항목·증거 양식: see [gate-c-template.md](gate-c-template.md)

## 참조
- **`docs/54_GateBC_v0.7.1_Evidence.md` (현재 정본 원장)** · `docs/48`(v0.7.0)·`docs/45`(v0.6.0)·`docs/44`(v0.5.0) historical
- `docs/41_Approval_and_Pilot_Gates.md §4` (Pilot Gate B/C 실행계획·결과양식), `docs/28_Security_Review_Checklist.md`
- `docs/52 §3` (v0.7.1 Gate B/C 연결), CLAUDE.md §11.4 (상태 어휘·과대표기 금지)
- 연계 스킬: `/risk-release-verify` (패키지 컷·SHA·금지파일 검증 → 본 원장 A그룹 선행) · `/risk-release-cut`(신규 컷 시 원장 생성) · `/risk-security-guard`(증거 파일 민감정보 스캔) · `/risk-team-pilot`(봉인 후 파일럿 진입)
