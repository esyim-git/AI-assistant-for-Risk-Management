# Codex 프롬프트 스켈레톤

> 이 스켈레톤으로 **`prompts/codex/<WP-ID>_<slug>.md`** 1개를 Write 한다(반드시 `prompts/codex/` 아래).
> 기존 톤은 `prompts/codex/STAB-WP-03_integrity_manifest.md` 참조. 한 프롬프트 = WP 1개.
> 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보·외부 NuGet/다운로드 지시 금지. 더미명만.

---

```markdown
# Codex <WP-ID> — <제목>

> 권위 스펙: `docs/39 §B`(<WP-ID>), `docs/40`(ADR-???), `docs/38`(RR-??). 선행: <선행 WP-ID 또는 없음>.
>
> **NEXT UP = 이 WP 1개만.** 다른 WP 건드리지 않는다. 충돌 시 우선순위: `AGENTS.md` > `docs/39` WP > 본 프롬프트.

## 현재 문제
<왜 이 WP가 필요한가. 근거 RR/ADR 1~3줄.>

## 목표
<단일 결과. docs/39 WP 목표와 일치.>

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3`(절대원칙), `docs/40`(ADR-???), `docs/28`(보안검토), <관련 소스 경로>.

## 브랜치/동기화
\`\`\`bash
git fetch origin && git switch -c feature/<wp-id>-<slug> origin/main
\`\`\`
- .NET 8. PR→main(squash, Commit Subject에 `(#PR)`), 게이트 A. **NuGet 0**(인박스 BCL만 — 예: `System.Security.Cryptography`).
- 작은 Diff. main 직접 push 금지·force push 금지(`docs/32`·`docs/35`, [/risk-branch-governance](../risk-branch-governance/SKILL.md)).

## 작업 범위
1. <구체 작업 1>
2. <구체 작업 2>
- **제외**: <명시적으로 하지 않는 것 — 인접 WP로>.

## 구현 세부 / 보안
- 결정적 동작. 외부 호출 0·Telemetry 0·자동실행 0. 쓰기 경로는 `config/`·`logs/`·`reports/`만, 경로 가드·상대경로.
- 해시 전용(원문 미저장). 민감정보 로그 금지. NoModelMode 유지.
- **STOP**: 외부 라이브러리·NuGet·Vector DB·Embedding·Local LLM Runtime·모델파일이 필요해지면 **즉시 STOP** → 승인 문서(`docs/41`·`docs/40` ADR) 작성 전까지 의존성 추가 금지(`AGENTS.md §4`).

## 테스트 (Windows)
- 양성: <정상 통과 케이스>. 음성: <차단/예외 케이스(graceful)>.
- 기존 SmokeTest 단언·이름 **보존**(삭제·약화 금지). WP별 회귀를 SmokeTest에 추가.
- 로컬 검증: `dotnet build RiskManagementAI.sln -c Release` + `dotnet run --project tests/RiskManagementAI.SmokeTests`.

## 완료/보고
보고에 다음을 포함한다(머지 게이트 = 로컬 증거 + Claude 코드리뷰 + 활성 hosted exact-head checks):
- build 결과(0/0) · SmokeTest **합계 줄 `Total=N PASS / 0 FAIL`** · Gate A 결과 · 변경 파일 목록 · 양성 케이스.
- `docs/39` <WP-ID> 상태 갱신(DONE 등 정본 어휘) 요청. 과대표기 금지(`CLAUDE.md §11.4`).

## Claude Review Checklist
<검토 포인트> / NuGet 0 / 경로 가드 / 해시 전용 / 외부·자동실행 0 / 기존 SmokeTest 불변 / Gate A.
```

---

## 채울 때 주의

- `<WP-ID>`·`<slug>`·브랜치·Commit 을 docs/39 WP와 **동일하게** 맞춘다.
- 작업범위는 docs/39 WP의 작업범위/제외범위를 그대로 반영(둘이 어긋나지 않게).
- 보안·STOP·테스트·보고 섹션은 절대원칙이므로 **삭제하지 않는다**. WP에 무관한 항목만 줄인다.
- 보고형식의 `Total=N PASS / 0 FAIL` 합계 줄 지시는 항상 유지(`AGENTS.md §5`).
- 검토는 [/risk-codex-review](../risk-codex-review/SKILL.md), 게이트 A는 [/risk-security-guard](../risk-security-guard/SKILL.md)로 연결된다.
