# 상태 어휘 (Status Vocabulary) — 정본

`CLAUDE.md §11.4`의 7개 어휘만 사용한다. 그 외 표현(예: "거의 완료", "사실상 동작", "DONE-ish", "PASS 예상")은 금지.
모든 상태 표기에는 **증거**(커밋 SHA / SmokeTest `Total=N PASS / 0 FAIL` / Gate 증거 문서 PATH)를 붙인다.

## 1. 7개 어휘 정의·충족 기준

| 어휘 | 의미 | 표기 허용 조건(증거) |
|---|---|---|
| `VERIFIED` | 코드 구현 + 테스트로 검증됨 | 머지된 커밋 SHA + SmokeTest `Total=N PASS / 0 FAIL` 또는 해당 도메인 테스트 통과 증거 |
| `PARTIAL` | 일부만 구현/검증, 나머지 미완 | 무엇이 되고(SHA) 무엇이 남았는지(미구현 항목) 둘 다 명시 |
| `SCAFFOLD_ONLY` | 구조/인터페이스만 존재, 실제 능력/데이터 미적재 | 구조 커밋 SHA + "계수/데이터/Rule Pack 미적재" 명시 |
| `PLACEHOLDER` | 자리표시자(스텁/더미)일 뿐 | 실제 로직 없음 명시. 능력 있는 것처럼 적지 않는다 |
| `BLOCKED` | 선행(특히 실 오프라인 Test PC 증거) 대기로 진행 불가 | 무엇을 기다리는지 + 증거 문서 PATH(예: `docs/45`) |
| `NOT_IMPLEMENTED` | 아직 구현 안 됨 | 설계/계획 위치(예: `docs/38` Release, `docs/39` WP) |
| `APPROVAL_REQUIRED` | 문서오너/Gate 승인 전 진행 금지 | 승인 문서 PATH(예: `docs/41`) + STOP 사유 |

## 2. 사용 규칙
- **증거 없으면 `VERIFIED`/`PASS` 금지** → 더 약한 어휘로 내린다.
- **Gate는 실 오프라인 Test PC 증거 없이 PASS 금지** → `BLOCKED` 유지(`docs/45`).
- 머지 Gate 증거 = 로컬 `dotnet build` + SmokeTest `Total=N PASS / 0 FAIL` + Claude 코드리뷰. GitHub-CI-green은 전제 아님(`CLAUDE.md §11.6`).
- 외부 의존성(NuGet/Vector DB/Embedding/Local LLM Runtime/모델파일)이 필요해지는 능력은 `APPROVAL_REQUIRED` + STOP. 구현된 것처럼 적지 않는다.
- 테스트 총수 감소 시 사유·매핑 기록(삭제·약화 금지, `docs/39` 규약).

## 3. 과대표기 금지 예시 (정정 전 → 후)

> 아래 수치/이름은 모두 형식 예시이며 더미다. 실제 기준선은 `docs/38·39`를 정본으로 확인한다.

- 나쁨: "RAG 검색 동작, AI가 규정 해석 제공."
  - 좋음: "공개 규정 KB Keyword/Inverted Index **인용형 답변** = `VERIFIED`(커밋 `<SHA>`, SmokeTest `Total=N PASS / 0 FAIL`). 답변은 **검토용 초안**, 공식 해석 아님."
- 나쁨: "NCR Rule Set 구현 완료."
  - 좋음: "NCR Rule Set 8요소 **구조** = `SCAFFOLD_ONLY`(승인 Rule Pack·계수 미적재). 적재는 `APPROVAL_REQUIRED`(`docs/41`)."
- 나쁨: "Local LLM 답변 생성 가능."
  - 좋음: "Local LLM Adapter = **설계만**, Runtime `APPROVAL_REQUIRED` + STOP(모델파일 repo 미포함, `docs/40` ADR-003)."
- 나쁨: "Gate B/C PASS."
  - 좋음: "Gate B/C = `BLOCKED`(실 오프라인 Test PC 증거 대기, `docs/45`). 실 PC 증거 없이 PASS로 적지 않는다."
- 나쁨: "테스트 다 통과."
  - 좋음: "SmokeTest `Total=N PASS / 0 FAIL`(로컬 `dotnet build -c Release` + `dotnet run --project tests/RiskManagementAI.SmokeTests`, 커밋 `<SHA>`)."
