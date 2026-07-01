---
name: risk-smoke-governance
description: Manage SmokeTest suite governance, prevent assertion loss, test weakening, unclassified tests, and baseline confusion.
allowed-tools: Read, Grep, Glob, Bash(dotnet run:*), Bash(git diff:*)
paths:
  - tests/**
---

# Smoke Governance

## 목적
인박스 SmokeTest suite의 **구조·총수·단언·도메인 분류**가 일관되게 유지되도록 관리하고, 테스트 회귀(삭제·약화·미분류·기준선 혼동)를 막는다. 이 스킬은 **점검/거버넌스 가이드**이며 코드 동작을 바꾸지 않는다.

## 언제 사용
- `tests/**` 파일을 작업할 때 **자동 적용**된다(`paths`).
- WP 구현 결과에서 SmokeTest 변화(총수 증감·단언 변경·도메인 분류)를 검토할 때.
- 트리거 예: "SmokeTest 검토", "테스트 약화됐나", "Total 줄었는데", "Unclassified", "도메인 분류", "smoke governance", "test regression".

## 절대 원칙
- **기존 테스트 삭제 금지 · 단언(Assert) 약화 금지.** 총수 감소·단언 제거/완화 시 **사유·매핑** 없으면 회귀로 본다(`AGENTS.md §3`).
- **정본 Total 보존**: 직전 기준선(현 정본 `Total=829`, `docs/39` Resume Brief 값) **이상**. 줄면 WP·사유 명시.
- **Unclassified = 실패**: `SmokeTestContext.ClassifyDomain`이 도메인을 못 잡으면 러너가 `exit 1`. 신규 테스트명은 분류 가능한 키워드를 포함해야 한다.
- **신규 기능은 해당 suite에 테스트 추가**(additive). WP별 **양성/음성 회귀** 동반.
- **외부 테스트 프레임워크 0**: xUnit/NUnit/MSTest 등 추가 금지. 단일 콘솔 러너 유지(`AGENTS.md §5`).
- **과대표기 금지**: `FAIL=0`이 아니면 PASS로 적지 않는다. 실 Test PC 증거 없는 Gate는 `BLOCKED`(`CLAUDE.md §11.4`).

## 실제 구조 (정본)
- 단일 콘솔 러너: `tests/RiskManagementAI.SmokeTests`(`OutputType=Exe`, `ProjectReference`만, 외부 프레임워크 0).
- `Program.cs` → `TestRunner.Run()` → 각 `*Tests.Run(context)` 순차 호출 → `PrintSummaryAndGetExitCode()`.
- 종료부 출력: `=== SmokeTest Summary ===` 다음 줄 **`Total=N PASS=N FAIL=N Duration=...`** + 도메인별 `  <Domain>: PASS=p FAIL=f` 요약.
- 도메인 분류기 `SmokeTestContext.SmokeDomain(name)`: 테스트명 키워드로 분류(`Xlsx/Csv/Mapping/Reconciliation/Report/Limit/Ncr/Kb/Packaging/Assist/Audit/Generation/UiContract/DataProfile/Safety`). 미매칭은 `Unclassified` → `exit 1`.
- 종료코드: `Unclassified>0` → 1, `failed>0` → 1, 그 외 0.

## 절차
1. **Diff 확인**: `git diff <base>..<branch> -- tests/`로 추가/삭제/변경 테스트와 단언 변화를 본다. 삭제·약화 라인을 우선 확인.
2. **총수 대조**: 러너 실행 또는 보고의 합계 줄 `Total=N`을 직전 기준선(`829`/`docs/39`)과 대조. 감소 시 사유·매핑 요구.
3. **단언 보존**: 변경된 `AssertTrue(...)`가 조건을 완화(예: `==`→`!= null`, 범위 확대)하지 않았는지 확인. 신규 기능에 양성/음성 회귀가 추가됐는지 확인.
4. **도메인 분류**: 신규 테스트명이 해당 도메인 키워드를 포함해 `Unclassified`가 0인지 확인. 분류기 수정이 필요하면 키워드 추가만(기존 분류 약화 금지).
5. **재현**: `dotnet run --project tests/RiskManagementAI.SmokeTests` → `Total=N PASS=N FAIL=0` + `Unclassified` 없음 확인.

## 출력
- **SmokeTest 거버넌스 결과**: `Total <이전>→<이후> · 삭제/약화 단언(0/N) · Unclassified(0/N) · 신규 회귀(양성/음성 추가 여부) · 외부 프레임워크(0/N)`.
- 위반 항목은 `항목 — 파일:라인 — 사유 — 기대` 줄 목록.
- 최종 한 줄: **회귀 0건(보존됨)** 또는 **회귀 N건(보완 필요)**. 증거 없는 PASS/VERIFIED는 적지 않는다.

## 절대원칙
- 본 스킬은 읽기 전용 점검이다. 테스트 삭제·약화·분류 약화를 **승인하지 않는다**.
- 머지 게이트 증거 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL`(`CLAUDE.md §11.6`).

## 참조
- 상세 점검 항목·도메인 키워드표·판정 양식은 [smoke-governance-checklist.md](smoke-governance-checklist.md).
- 코드: `tests/RiskManagementAI.SmokeTests/SmokeTestContext.cs`(분류·요약) · `TestRunner.cs`(suite 호출 순서) · 각 `*Tests.cs`.
- 문서: `AGENTS.md §3·§5`(테스트 표준) · `docs/39`(WP·기준선 Total) · `docs/38`(Roadmap·Traceability) · `CLAUDE.md §11.4·§11.6`.
- 연계 스킬: `/risk-codex-review`(테스트 축 포함 4축 리뷰) · `/risk-doc-truth-sync`(기준선 Total 문서 정합).
