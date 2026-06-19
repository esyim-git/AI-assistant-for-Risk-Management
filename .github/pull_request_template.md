<!-- 이 템플릿은 docs/32_Branch_Governance.md 규약을 따른다. 해당 없는 항목은 N/A로 둔다. -->

## 목적 / 변경 요약

<!-- 무엇을, 왜 바꿨는지 1~3줄 -->

## 관련 백로그 / 이슈

<!-- 예: docs/21 B-xx, docs/33 M2-xx, R-xx, #이슈번호 -->

## 변경 유형

- [ ] feat (기능) / [ ] fix (버그) / [ ] docs / [ ] chore / [ ] refactor / [ ] test / [ ] security

## 검증

- [ ] `dotnet build RiskManagementAI.sln` 성공 (0 warnings 권장)
- [ ] `dotnet run --project tests/RiskManagementAI.SmokeTests` 전부 PASS
- [ ] (CI) `ci / build` green

## 보안 게이트 A (docs/28) — 커밋/푸시 전 필수

- [ ] 실제 회사 데이터 없음 / 내부규정 원문 없음 / 실제 테이블·시스템명 없음
- [ ] 비밀번호·토큰·API key·접속문자열·인증서 없음
- [ ] 대용량 모델 가중치 파일 없음 (`*.gguf` `*.safetensors` `*.onnx` 등)
- [ ] 로그/리포트에 민감정보 평문 없음 (해시만)

## 절대원칙 (CLAUDE.md §3) 준수

- [ ] 외부 API / 자동 업데이트 / telemetry 코드 없음
- [ ] SQL/VBA 자동 실행 없음 (조회·초안 전용)
- [ ] 운영환경 반입물은 source가 아니라 portable Release ZIP (해당 시)

## 비고 / 남은 리스크

<!-- 후속 작업, 알려진 한계 등 -->
