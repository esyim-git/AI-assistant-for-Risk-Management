# KB RAG Design

## 목적

규정, NCR, 리스크 이론, 승인 예제를 모델 재학습 없이 검색 기반으로 활용한다.

## 구조

```text
Document Master
  ↓
Clause/Chunk
  ↓
Keyword/Topic/Risk Category
  ↓
Search Result
  ↓
Prompt Context
  ↓
Answer with Source
```

## 초기 구현

- CSV/Markdown catalog
- 키워드 검색
- 문서 버전/시행일 표시

## 운영 구현

- 권한통제
- 감사로그
- 내부 문서 오너 승인
- 버전 관리

---

## (심화 R3) 공개 규정 인용형 RAG 상세 설계

> 대상: 자본시장법·시행령·금융투자업규정·시행세칙·NCR 산정기준 해설·감독기관 공개 FAQ·**승인된** 내부규정. 승인/적재 게이트: `docs/41 §2`.

### 문서 Metadata (공개 규정)
문서ID · 문서명 · 출처기관 · 출처 · 버전 · 시행일 · 폐기일 · **파일 Hash** · 적재일 · 승인상태 · 대체문서 · **라이선스 상태**

### 검색 답변 표시 항목
문서명 · 버전 · 시행일 · 조항 · 출처 · **검색 기준일** · **"검토 필요" 문구**(항상 검토용 초안 명시)

### 검색 엔진 (NuGet 0 우선)
- 초기: **Keyword / Inverted Index**(in-box, 외부 라이브러리 0). 기존 `RegulationCatalog`/`KbSearch` 확장.
- **Vector DB / Embedding Runtime / 외부 라이브러리가 필요해지면 구현 STOP** → 승인 문서(라이선스·크기·오프라인 동작·보안·반입) 작성 후에만(`docs/41 §2`).

### 내부규정 (권한통제형)
- **원문은 Repository 미포함.** Prod에서만 문서오너 승인 + 보안등급 + 역할기반 권한 + **조회로그** 적용.
- 답변은 공식 해석이 아닌 **검토용 초안**.

---

## (R3 실행) RAG/NCR WP 분해

> 기존 `RegulationCatalog`/`KbSearch`(공개 catalog·linear Contains·해시감사·"검토용 초안")를 **점진 확장**한다. 절대원칙 유지(오프라인·NuGet 0·외부 0·내부원문 미포함·모델 미도입). Codex 프롬프트: `prompts/codex/R3-WP-XX_*.md`. Claude가 WP별 검증. 게이트: `docs/41 §2`.

| WP | 목표 | 상태 | 비고 |
|---|---|---|---|
| **R3-WP-01** | KB Document **Metadata 확장**(catalog 9 메타필드) | **NEXT** | **출처(locator,≠출처기관)**·버전·시행일·폐기일·파일Hash·적재일·승인상태·대체문서·라이선스. 공개 메타만, 원문 미포함 |
| R3-WP-02 | **Keyword/Inverted Index** 검색엔진 | TODO | linear Contains → 역색인(NuGet 0, 결정적). **Vector/Embedding 필요 시 STOP+승인** |
| R3-WP-03 | **인용형 답변 강화** | TODO | 문서명·버전·시행일·**조항**·출처·**검색 기준일**·"검토 필요" 완비 |
| R3-WP-04 | **적재 게이트 가드** | TODO | 공개/승인 status만 검색 노출, `PROD_ONLY`/`MANUAL_APPROVAL_REQUIRED`는 원문 비노출(catalog-only), 라이선스·승인 검증 finding |
| R3-WP-05 | **NCR Rule Set 구조**(`docs/08` 심화) | TODO(RAG 후) | Rule Set/Version/Effective Date/Component Map/Formula Description/Validation SQL/Regulation Basis/Approval History. **모델 산식 암기 금지** |

- **순서**: RAG(WP-01→04) 먼저, 그다음 NCR(WP-05). 각 WP는 자기 DoD+게이트 A+자기 테스트로 머지(big-bang 금지).
- **STOP 규칙**: Vector DB/Embedding Runtime/외부 라이브러리/모델파일이 필요해지는 순간 **작업 STOP** → 승인 문서(`docs/41 §2`) 후에만.

> 관련: `docs/08_NCR_Module_Design.md`, `docs/09_Internal_Regulation_Onboarding.md`, `docs/41`(RAG/NCR 게이트), `CLAUDE.md §10`.
