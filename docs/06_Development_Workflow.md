# Development Workflow

## 환경 구분

```text
Dev  : GitHub + 개발 PC
Test : Local 테스트 PC
Prod : 회사 업무망/개발망 PC
```

## 개발 흐름

```text
GitHub Repo
  ↓ clone
Dev PC
  ↓ 구현/테스트
Self-contained Release ZIP 생성
  ↓
Local Test PC
  ↓ 해시 검증/실행 검증
회사 반입 신청
  ↓
Prod PC
  ↓ 압축 해제/실행
운영 피드백
```

## Branch 전략

```text
main                    : 안정화된 기본 브랜치
develop                 : 통합 개발 브랜치
feature/mvp1-rule-engine: 기능 브랜치
release/v0.2.0          : 배포 브랜치
hotfix/...              : 긴급 수정
```

## Commit 규칙

```text
feat: 기능 추가
fix: 버그 수정
docs: 문서 수정
chore: 구조/빌드 변경
test: 테스트 추가
refactor: 리팩토링
security: 보안 관련 수정
```
