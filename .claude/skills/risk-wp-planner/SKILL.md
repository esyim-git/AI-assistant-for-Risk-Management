---
name: risk-wp-planner
description: Create a precise Codex Work Package and implementation prompt for one requested WP. Auto-applied last in Claude's planning Preflight (CLAUDE.md §13).
argument-hint: "[WP-ID]"
arguments: [wp_id]
disable-model-invocation: true
allowed-tools: Read Grep Glob Edit Write Bash(git status *) Bash(git diff *)
---

# Codex Work Package Author

## 목적
`docs/39` WP 형식(14필드)으로 **하나의 Work Package**를 정의하고, Codex가 실행할 프롬프트를 **`prompts/codex/<WP-ID>_*.md`** 아래에 생성한다. WP 분해·프롬프트 작성만 담당하며 코드 동작은 바꾸지 않는다.

## 언제 사용
- "다음 WP 만들어 / NEXT UP 착수 / WP 분해 / Codex 프롬프트 작성 / work package / codex prompt" 류 요청.
- `docs/39` Resume Brief의 NEXT UP 1개를 실제 구현 단위로 분해할 때.
- [/risk-status-sync](../risk-status-sync/SKILL.md)가 확정한 NEXT UP을 이어받아 구현 착수할 때.

## 절대 원칙
- **NEXT UP 1개만** 분해한다. 여러 WP를 한 프롬프트에 묶지 않는다(`AGENTS.md §0`).
- **Codex 프롬프트 결과물은 반드시 `prompts/codex/<WP-ID>_<slug>.md` 아래에 쓴다.** 다른 위치 금지.
- 외부 NuGet PackageReference = 0, 외부 API/Telemetry/자동업데이트 = 0 을 프롬프트 보안조건에 명시(`AGENTS.md §3`).
- STOP 규칙: 외부 라이브러리·NuGet·Vector DB·Embedding·Local LLM Runtime·모델파일이 필요해지면 프롬프트에 **STOP → 승인 문서(`docs/41`·`docs/40`)** 지시를 넣는다(`AGENTS.md §4`).
- 프롬프트·WP에 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보 금지. 예시는 더미명(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`)만(`AGENTS.md §3`).
- 기존 테스트 삭제·약화 금지. 머지 게이트 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL` + Claude 코드리뷰(`CLAUDE.md §11.6`).
- 상태 어휘 정본만 사용: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED. 과대표기 금지(`CLAUDE.md §11.4`).

## 절차
1. **NEXT UP 선택**: `docs/39` ★ Resume Brief의 `NEXT UP` WP **1개**만 읽는다. 여러 WP 동시 작성 금지. WP-ID·슬러그 확정.
2. **기존 형식 확인**: `docs/39 §0`의 WP 형식과 기존 WP 예시(예: `STAB-WP-03`), 참고 프롬프트 `prompts/codex/STAB-WP-03_integrity_manifest.md`를 읽어 톤·구조를 맞춘다.
3. **WP 14필드 작성**: 목표·선행조건·작업범위·제외범위·읽을문서·수정예상파일·Public Interface·구현세부·보안조건·테스트·완료조건·Branch·Commit·Claude Review Checklist 를 채운다. 빈 양식은 [wp-format.md](wp-format.md) 참조.
4. **Codex 프롬프트 Write**: [codex-prompt-template.md](codex-prompt-template.md) 스켈레톤으로 `prompts/codex/<WP-ID>_<slug>.md` 를 새로 쓴다. 권위 스펙(`docs/39`·`docs/40`·`docs/38`) 링크, 브랜치/동기화(`feature/<WP-ID>-*`), 작업범위/제외, 보안(NuGet 0·경로 가드), 테스트(회귀 추가·기존 보존), Gate A, 보고형식(`Total=N PASS / 0 FAIL`), Claude Review Checklist 를 포함한다.
5. **docs/39 갱신**: 새 WP 항목을 `docs/39`에 추가하거나 기존 항목을 갱신하고, Resume Brief의 NEXT UP/프롬프트 경로를 정렬한다. main은 수정하지 않으며 계획 작업은 `planning/*` 브랜치에서(`CLAUDE.md §11.1`).

## 산출물/보고
- `docs/39`에 추가/갱신된 **WP 항목 1개**(14필드).
- 새 파일 **`prompts/codex/<WP-ID>_<slug>.md`** (Codex 실행 프롬프트).
- 보고에는 WP-ID · 프롬프트 경로 · Branch 명 · 핵심 양성/음성 테스트 케이스를 요약하고, Codex 보고 합계는 `Total=N PASS / 0 FAIL` 형식을 따르게 지시했음을 명시한다.

## 체크리스트
WP 14필드 빈 양식은 [wp-format.md](wp-format.md), Codex 프롬프트 스켈레톤은 [codex-prompt-template.md](codex-prompt-template.md) 참조.

## 참조
- `docs/39_Work_Package_Backlog.md` (§0 WP 형식 · ★ Resume Brief NEXT UP · 기존 WP 예시)
- `docs/38_v1_Master_Roadmap.md` (Release Train · Traceability) · `docs/40_ADR_Architecture_Evolution.md` (ADR) · `docs/41` (게이트)
- `AGENTS.md` (§0 우선순위·NEXT UP 1개 · §3 절대원칙 · §4 STOP · §5 Gate/보고) · `CLAUDE.md §11`(Claude↔Codex Workflow)
- 예시 프롬프트: `prompts/codex/STAB-WP-03_integrity_manifest.md`
- 관련 스킬: [/risk-status-sync](../risk-status-sync/SKILL.md)(NEXT UP 확정) → [/risk-codex-review](../risk-codex-review/SKILL.md)(결과 검토) · [/risk-branch-governance](../risk-branch-governance/SKILL.md)(브랜치/PR 규약) · [/risk-security-guard](../risk-security-guard/SKILL.md)(보안 게이트 A)
