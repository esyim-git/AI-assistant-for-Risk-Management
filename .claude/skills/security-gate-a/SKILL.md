---
name: security-gate-a
description: "Gate A 커밋/푸시 전 보안 점검(docs/28) — NuGet 0 · 외부 API/telemetry/자동업데이트 0 · secrets/키/모델가중치/실데이터/내부규정 원문 0 · 경로가드 · 해시 audit. 수동 호출 전용."
allowed-tools: Read, Grep, Glob, Bash(git status:*), Bash(git diff:*)
disable-model-invocation: true
---

# Security Gate A

## 목적
커밋/푸시 전 **스테이징 변경분**에 금지 항목이 없는지 `docs/28` 게이트 A를 실행한다. 하나라도 위반이면 commit/push를 **즉시 중단**하고 정정한다. 이 스킬은 점검(읽기) 전용이며 코드 동작을 바꾸지 않는다.

## 언제 사용
- **수동 호출 전용**: `/security-gate-a`. 모델이 자동 호출하지 않는다(`disable-model-invocation: true`).
- commit/push 직전, 또는 Codex 결과를 머지 게이트로 올리기 전 보안 축을 단독으로 돌릴 때.
- 게이트 B/C(릴리스·반입)는 범위 밖이다 — `/gate-bc-evidence`로 이어간다.

## 절대 원칙
점검 기준은 `docs/28` 게이트 A + `AGENTS.md §3`(불변). 하나라도 위반이면 **FAIL → 중단**.
- **외부 NuGet PackageReference = 0** · 외부 API 0 · Telemetry 0 · 자동 업데이트 0 (`AGENTS.md §3·§4`). 추가가 보이면 STOP → `/local-model-approval` 등 승인 문서 먼저.
- **민감정보 0**: 실데이터·실 테이블/컬럼/시스템명·내부규정 원문·NCR 공식본 원문·비밀번호/토큰/API key/접속문자열·인증서/키 파일·모델 가중치. 더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 같은 일반명만 (`docs/28` 게이트 A, `docs/03` DataCatalog).
- **금지 확장자 스테이징 0**: `*.gguf` `*.bin` `*.safetensors` `*.onnx` `*.pem` `*.key` `*.pfx` `*.cer` `*.crt` `*.env`.
- **경로 가드**: 쓰기 경로는 `logs/`·`reports/`·`config/`만. 운영 로그/Export 실데이터 미포함, `.gitignore`가 위 항목을 차단.
- **해시 기반 Audit**: 원문 미저장, 사번/사용자 ID는 해시만 (`docs/05` Security Policy).
- 위반 시: 즉시 중단 → 파일 제거/unstage → 이미 commit되었으면 사용자 보고 후 협의. **force push·hard reset 금지**.

## 절차
1. **스테이징 전수 확인**: `git status`와 `git diff --cached --name-only`로 추적될 파일을 전수 확인한다(의도치 않은 파일 0). `git add -A --dry-run`으로 `.gitignore` 반영 결과를 미리 본다.
2. **민감정보 스캔**: 변경분에 대해 secret/접속문자열/주민번호 패턴, 실 테이블/컬럼명, 내부규정·NCR 원문 흔적을 스캔한다. 정책/룰/문서 파일(`rules/`·`config/security_policy.json`·`docs/`·`.gitignore`)의 금지어 "설명"은 오탐이므로 제외(`:!...`). 명령은 [gate-a-checklist.md](gate-a-checklist.md).
3. **불변 원칙 확인**: Diff에 NuGet `PackageReference` 추가 0, 외부 API/telemetry/자동업데이트 코드 0, 금지 확장자 스테이징 0 확인.
4. **경로·audit 확인**: 새 쓰기 경로가 `logs/`·`reports/`·`config/` 한정인지, audit 로그가 원문 대신 해시만 남기는지 확인.
5. **판정·정정**: 항목별 PASS/FAIL 정리. FAIL이 하나라도 있으면 commit/push 중단 → unstage/제거 → `.gitignore`/룰/체크리스트 갱신(force push 금지).

## 산출물/보고
- **게이트 A 항목별 판정표**: 각 항목 `PASS` 또는 `FAIL`.
- FAIL은 `항목 — 위반 파일(경로:라인) — 사유 — 정정 조치` 줄 목록.
- 최종 한 줄: **게이트 A PASS(commit/push 가능)** 또는 **게이트 A FAIL(중단, 위반 N건)**. 증거 없는 PASS는 적지 않는다.

## 체크리스트
게이트 A 항목 + 점검 명령(PowerShell `git grep` 스캔, 정책/룰/문서 제외 규칙 포함)은 [gate-a-checklist.md](gate-a-checklist.md).

## 참조
- `docs/28_Security_Review_Checklist.md`(게이트 A 정본) · `docs/19_Security_Review_Checklist.md`(기본 체크리스트) · `AGENTS.md §3·§4`(불변·STOP) · `docs/05`(Security Policy·로그 규칙) · `docs/03`(DataCatalog 더미명).
- `CLAUDE.md §3`(절대 원칙) · `§8`(Git 원칙·force push 금지) · `§11.4`(상태 어휘·과대표기 금지).
- 연계 스킬: `/codex-result-review`(보안 축 포함 4축 리뷰) · `/branch-governance`(머지·브랜치 규약).
