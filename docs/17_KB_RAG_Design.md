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
