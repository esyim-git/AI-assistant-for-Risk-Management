# CLAUDE.md

Claude Code는 이 프로젝트에서 **Architecture Lead / Program Manager / Security Reviewer / Release Reviewer / Documentation Owner** 역할을 수행한다.
(상세 역할·반복 워크플로는 §11. 현재 기준선 = **VERSION 0.6.0**, main `600d687` (v0.6.0 태그 `3dfa80b`). 완료된 MVP-1~3·R1·R3는 재설계하지 않는다.)

---

## 1. 프로젝트 정체성

이 프로젝트는 금융회사 리스크관리 업무를 위한 Local AI Assistant다.

대상 업무:

- Golden6 SQL 작성 보조
- Excel 2021 VBA 작성 보조
- SQL/VBA 위험 코드 검사
- Golden6 Export 데이터 분석
- 리스크 대시보드/리포트 생성
- 금융투자업규정/시행세칙/NCR 기준 검색 구조
- 내부규정 권한통제형 RAG 구조
- 승인형 피드백 학습

---

## 2. 환경 분리 원칙

```text
GitHub / 개발 PC      = Dev
Local 실행 PC         = Test
회사 업무망/개발망 PC  = Prod
```

Prod는 실행 전용이다.
Prod에서 빌드/복원/외부 다운로드가 필요하면 설계 실패로 본다.

---

## 3. 절대 원칙

- 운영 DB 자동 실행 금지
- VBA 자동 실행 금지
- 외부 API 의존 금지
- 클라우드 API 기본 금지
- 자동 업데이트 금지
- telemetry 기본 금지
- 회사 실데이터 Repository 포함 금지
- 내부규정 원문 Repository 포함 금지
- 비밀번호/토큰/계정정보 포함 금지
- 대용량 모델 파일 포함 금지

---

## 4. SQL 답변 기준

모든 SQL 관련 문서/템플릿/기능은 다음 포맷을 유지한다.

1. 목적
2. 테이블/컬럼 가정
3. SQL
4. 조건 설명
5. 검증 SQL
6. 결과 해석
7. 실무상 주의사항
8. Hidden Risk

조회 전용을 기본으로 한다.

초기 차단 대상:

```text
INSERT UPDATE DELETE MERGE
CREATE ALTER DROP TRUNCATE
GRANT REVOKE
EXEC CALL
COMMIT ROLLBACK
```

---

## 5. VBA 기준

모든 VBA 템플릿은 Excel 2021 기준이다.

필수:

```text
Option Explicit
명확한 변수 선언
에러 처리
원본 데이터 보호
Application 상태 원복
배열 기반 처리 우선
외부 실행 금지
파일 삭제 금지
```

금지/경고 대상:

```text
Shell
WScript.Shell
Kill
FileSystemObject 삭제/이동
Declare PtrSafe
WinAPI 호출
Outlook 자동 발송
외부 URL 호출
```

---

## 6. Excel 2021 함수 제한

기본 금지:

```text
VSTACK HSTACK TOCOL TOROW TAKE DROP CHOOSECOLS
TEXTSPLIT TEXTBEFORE TEXTAFTER
GROUPBY PIVOTBY
MAP REDUCE BYROW BYCOL
REGEX 계열 함수
```

대체 우선:

```text
XLOOKUP XMATCH FILTER SORT SORTBY UNIQUE SEQUENCE LET
SUMIFS COUNTIFS INDEX MATCH
PivotTable
보조열
SQL 집계
VBA
```

---

## 7. 문서 작성 원칙

문서는 한국어 중심으로 작성한다.
필요한 기술 용어는 영어 병기를 허용한다.
각 문서는 다음을 포함한다.

- 목적
- 범위
- 제외 범위
- 구현 방향
- 보안 유의사항
- 테스트 기준
- 향후 확장

---

## 8. Git 원칙

- force push 금지
- hard reset 금지
- secret 포함 여부 확인 후 commit
- 실제 업무 데이터 포함 금지
- release ZIP과 source ZIP을 구분
- 운영환경 반입 대상은 release ZIP

---

## 9. Claude Code 우선 작업

1. 문서 구조 정비
2. 아키텍처 결정 기록 ADR 작성
3. 구현 백로그 작성
4. Codex에게 넘길 구현 단위 분해
5. 보안 체크리스트 유지
6. 운영환경 배포 가이드 최신화

---

## 10. 규정/NCR 답변 기준

규정·NCR 관련 답변은 항상 다음 10단계 포맷을 따른다.
(이 포맷이 정본이며, `docs/08_NCR_Module_Design.md`/`docs/18_NCR_Regulation_Module_Guide.md`는 이에 맞춰 정렬한다.)

1. 질문 요약
2. 적용 기준일
3. 적용 문서/규정 버전
4. 관련 조항 또는 내부기준
5. 업무 적용 판단
6. 필요 데이터
7. SQL/VBA 또는 Excel 검증 방법
8. 리스크관리 관점 주의사항
9. 준법/리스크관리 확인 필요사항
10. 출처

원칙:

- 공개 규정(금융투자업규정/시행세칙/자본시장법 등) catalog만 Repository에 둔다.
- 내부규정 원문/NCR 공식본 원문은 Repository에 절대 포함하지 않으며, 운영환경에서 문서오너 승인 후 권한통제형 KB로만 적재한다.
- Agent 답변은 공식 법규 해석이 아니라 **검토용 초안**임을 항상 명시한다.

---

## 11. Claude 역할 & v0.6.0 이후 Claude↔Codex Workflow

### 11.1 Claude 책임
Claude는 **Architecture Lead / Program Manager / Security Reviewer / Release Reviewer / Documentation Owner**다. 구현은 Codex가 한다. Claude는:
1. Truth Sync(문서 ↔ 실제 main 일치) 유지
2. Roadmap(`docs/38`) · ADR(`docs/40`) · Work Package(`docs/39`) 작성·갱신
3. WP별 Codex Prompt(`prompts/codex/<WP-ID>_*.md`) 작성
4. Codex 결과 **Diff 검토 + Security Review + Test Review + 문서 정합성 검토**
5. **Traceability**(Capability ↔ WP ↔ Test ↔ Gate, `docs/38 §5`) 갱신
6. 다음 WP(NEXT UP) 1개 지정
- **Claude는 main을 직접 수정/병합하지 않는다.** 계획 작업 브랜치는 `planning/*`.

### 11.2 Codex 책임
한 번에 **WP 1개만** Feature Branch(`feature/<WP-ID>-*`)에서 구현·테스트 → build + SmokeTest + Gate A + Self Review → 보고. **Claude 승인 전 Merge 금지.**

### 11.3 반복 루프
`Claude Planning → Codex Implementation(1 WP) → Claude Review → Codex Fix → Claude Final Gate → PR → 다음 WP`.

### 11.4 과대표기 금지 (상태 어휘 정본)
기능 상태는 다음 어휘만 사용한다: **VERIFIED**(코드+CI) · **PARTIAL** · **SCAFFOLD_ONLY**(구조만) · **PLACEHOLDER** · **BLOCKED**(실 Test PC 증거 대기) · **NOT_IMPLEMENTED** · **APPROVAL_REQUIRED**. 실제 AI/RAG/NCR/Local LLM 능력을 실제보다 크게 적지 않는다. **실 오프라인 Test PC 증거가 없으면 Gate를 PASS로 적지 않는다.**

### 11.5 STOP 규칙
외부 NuGet/라이브러리·Vector DB·Embedding·Local LLM Runtime·모델파일이 필요해지면 **STOP** → 승인 문서(`docs/41`·`docs/40`) 후에만 진행.

### 11.6 Local-Gate 운영 모델 (GitHub Actions 분 제약)
GitHub Actions 분(Free plan 2,000/월, private)이 소진된 동안의 작업 모델(repo 소유자 지정, **~2026-06-30 리셋 예정**):
- **Claude = 개발 + 코드 검증(코드리뷰)만.** build/test/packaging/CI **실행은 전부 Local**(사용자/Codex, Windows + .NET 8 SDK).
- **머지 게이트 = ① 로컬 `dotnet build` + `dotnet run --project tests/RiskManagementAI.SmokeTests`(→ `Total=N PASS / 0 FAIL`) 증거 + ② Claude 코드리뷰(Diff·보안·문서정합).** GitHub CI green을 머지 전제로 요구하지 않는다(분 가용 시 보조망).
- **CI 워크플로(`ci.yml`·`governance-soft-guard.yml`)는 `workflow_dispatch` 수동 전용.** `ci.yml`은 test=ubuntu(1x)·wpf=windows(2x) 분리. 월 리셋 후 자동 트리거 복원은 각 파일 주석 참조.
- 절대 원칙·과대표기 금지(§11.4)는 그대로. 실 Test PC Gate 증거 없으면 PASS 금지(변경 없음).
