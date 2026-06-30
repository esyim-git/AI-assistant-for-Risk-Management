# WP 14필드 빈 양식 (docs/39 §0 정본)

> `docs/39`에 추가할 단일 Work Package 양식. **하나의 WP는 하나의 명확한 목표만** 가진다.
> 아래 14필드를 모두 채운다. 예시 값은 더미명(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER`)만 사용한다.
> 실데이터·실 테이블/컬럼명·내부규정/NCR 원문·비밀정보·외부 NuGet/다운로드 지시 금지(`AGENTS.md §3·§4`).

---

## <WP-ID>. <한 줄 제목> (<RR-?? 또는 근거>)

- **목표**: <달성할 단일 결과. 측정 가능하게. "무엇을 바꿔 무엇을 보장한다">
- **선행조건**: <선행 WP-ID 또는 "없음(즉시)">
- **작업범위**: <이 WP에서 실제로 하는 일. 범위를 좁게>
- **제외범위**: <명시적으로 하지 않는 것. 인접 WP-ID로 미루는 항목>
- **읽을문서**: <`AGENTS.md`, `CLAUDE.md §?`, `docs/38`/`docs/40`(ADR-?)/`docs/41`, 관련 소스 경로>
- **수정예상파일**: <`Core/...`(신규/수정), `App/...`, `tests/RiskManagementAI.SmokeTests/...`, 더미 샘플 경로>
- **Public Interface**: <새/변경 공개 시그니처. 없으면 "없음(내부 동작 변경)". 반환형·enum 값 명시>
- **구현세부**: <결정적 동작, fallback 정책, 경로 가드, 상태/코드 정의 등 구체 지시>
- **보안조건**: <외부 0(NuGet 0)·읽기 전용·경로 가드(`config/`·`logs/`·`reports/`만)·해시 전용·민감정보 로그 금지>
- **테스트**: <양성/음성 회귀(Windows). 기존 단언 보존. 결정성·경계·graceful 실패 케이스>
- **완료조건**: <코드베이스에 남아야/사라져야 하는 상태. build + SmokeTest(기존 유지 + 신규)>
- **Branch**: `feature/<wp-id>-<slug>` · **Commit**: `<type>: <subject> (<WP-ID>)`
- **Claude Review Checklist**: <검토 포인트 / NuGet 0 / 기존 SmokeTest 유지 / Gate A>

---

## 작성 규칙

- **목표**는 한 문장, 단일 결과. 둘 이상이면 WP를 쪼갠다.
- **제외범위**로 인접 작업과 경계를 분명히 한다(범위 누수 방지).
- **테스트**는 양성(정상 통과)과 음성(차단/예외) 케이스를 함께. "기존 N개 SmokeTest 유지 + 신규 추가" 명시.
- **보안조건**은 절대원칙 중 이 WP에 해당하는 것만 구체적으로(전부 나열하지 말 것).
- 외부 의존이 필요해지면 STOP 표시 + 승인 게이트(`docs/41`·`docs/40`) 지시를 작업범위에 넣는다.
- 상태 표기는 정본 어휘만: VERIFIED · PARTIAL · SCAFFOLD_ONLY · PLACEHOLDER · BLOCKED · NOT_IMPLEMENTED · APPROVAL_REQUIRED.

## Codex 결과 추기 (구현 후 Codex가 채움)

- **Codex 결과(<날짜>)**: <추가/수정 파일 · 동작 요약 · 회귀 케이스 · build 0/0 · SmokeTest `Total=N PASS / 0 FAIL` · NuGet 0 유지>
  - 이 줄은 Codex가 구현 보고 시 추기한다. Claude는 [/risk-codex-review](../risk-codex-review/SKILL.md)로 검토한다.
