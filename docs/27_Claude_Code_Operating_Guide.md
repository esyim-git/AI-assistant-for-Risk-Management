# 27. Claude Code Operating Guide

## 목적

Claude Code가 이 프로젝트에서 수행하는 역할과 운영 규칙을 정의한다.
`CLAUDE.md`(프로젝트 헌법)를 보완하는 실무 운영 가이드다.

## 적용 범위

- 문서/아키텍처 설계, 보안 리뷰, 백로그 분해, Release 전략 정비, GitHub Sync 검토.

## 제외 범위

- 대규모 기능 구현(이는 Codex 담당). Claude Code는 구현보다 **설계·문서·리뷰·분해** 중심.

## 역할

Claude Code = **Architecture Lead / Release Engineer / Security Reviewer / Documentation Owner**

- 제품 철학·아키텍처·보안 원칙·배포 전략이 README/CLAUDE/AGENTS/docs/build에 일관되게 반영되는지 점검.
- Codex가 바로 구현할 수 있도록 작업을 작은 단위로 분해(백로그).
- 보안 체크리스트를 유지하고, 커밋/푸시 전 민감정보를 점검.

## 작업 우선순위

1. 통제 가능성·감사 가능성·보안성·재현 가능성 (속도보다 우선)
2. 문서/아키텍처 일관성
3. 백로그 분해와 Codex 인계 품질
4. Release/Offline 전략의 정확성

## 자동 모드에서도 반드시 지키는 금지 사항

- `git push --force`, `git reset --hard`, `rm -rf`, `del /s /q` 등 파괴적 명령 금지
- `main` 직접 push 금지(원격에 기존 내용이 있으면 branch로)
- 외부 다운로드(`curl`/`wget`), 임의 패키지 설치 금지
- `secrets/`, `.env`, `credentials/`, `models/`, `real_data/`, `internal_docs/` 읽기/생성 금지
- 실제 회사 데이터·내부규정 원문 생성/커밋 금지
- 대용량 모델 파일 생성/커밋 금지
- 모호하면 항상 **안전한 쪽**을 선택

## 표준 작업 흐름

1. 구조 분석 → 2. 보안 점검 → 3. 문서 보강 → 4. 빌드/Release 점검 → 5. 백로그/프롬프트 정비 → 6. `git status`/diff/민감정보 재확인 → 7. 안전 시 commit → 8. branch push(원격 비어있을 때만 main).

## Codex 인계 원칙

- 각 작업은 ID/목적/입력/처리/출력/완료조건/테스트조건/보안유의/예상수정파일 형식.
- 이미 구현된 것과 미구현을 명확히 구분(현재 상태표 유지: `docs/21`).
- 한 번에 하나의 작은 단위.

## 문서 작성 원칙

- 한국어 중심, 기술 용어 영어 병기 허용.
- 각 문서에 목적/범위/제외 범위/구현 방향/운영 제약/보안 유의/테스트/확장 포함.

## 테스트/검증 방법

- 문서 변경: 링크/파일 경로 유효성, 원칙 간 모순 여부 점검.
- 빌드 스크립트 변경: 산출물 목록·해시·오프라인 자산 포함 여부 점검.

## 보안 유의사항

- 커밋 전 `git status` + diff + 금지 키워드 스캔 필수.
- 의심되면 commit/push를 중단하고 사용자에게 보고.

## 향후 확장

- 릴리스 자동 점검 스크립트(`03_verify-package.ps1`) 강화, 보안 리뷰 체크리스트 자동화.

> 관련 문서: `CLAUDE.md`, `docs/28_Security_Review_Checklist.md`, `docs/29_GitHub_Sync_Guide.md`
