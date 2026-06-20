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

> 관련: `docs/08_NCR_Module_Design.md`, `docs/09_Internal_Regulation_Onboarding.md`, `docs/41`(RAG/NCR 게이트), `CLAUDE.md §10`.
