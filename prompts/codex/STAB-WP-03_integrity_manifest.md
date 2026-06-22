# Codex STAB-WP-03 — Release Security + Integrity Manifest (Fail-Closed)

> 권위 스펙: `docs/39 §B`(STAB-WP-03), `docs/40`(ADR-008), `docs/38`(RR-13, RR-14). 선행: STAB-WP-01. **NEXT UP 지정 시에만 착수**(큐 3순위).

## 현재 문제
ZIP SHA만으로는 운영 중 **핵심 파일 변조**(security_policy/rules/template/column_mapping/KB catalog/NCR placeholder/CP949 매핑)를 못 잡는다(RR-14). Release에 **PDB·개인경로·Debug/Test config**가 섞일 위험(RR-13).

## 목표
(a) Release 산출물에서 PDB/개인경로/SourceLink/Debug·Test config/Unsafe BinaryFormatter **부재 보장**, (b) **`approved_manifest.json`** 생성 + ZIP 동봉 + **앱 시작 시 무결성 검증**(개발=Fallback 경고, 운영=**Fail-Closed**).

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3`, `docs/40`(ADR-008), `docs/28`(보안검토), `build/01_publish-win-x64.ps1`·`03_verify-package.ps1`, App 시작부(`App/`), `Core/`의 정책/룰/템플릿 로더, `Data/Resources/cp949-uhc-map.txt`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/stab-wp-03-integrity origin/main
```
- .NET 8 self-contained. PR→main(squash, `(#PR)`), 게이트 A, **NuGet 0**(인박스 `System.Security.Cryptography`만).

## 작업 범위
1. **Release 보안(build/01·03)**: publish에 `-p:DebugSymbols=false -p:DebugType=none`(PDB 제거), SourceLink/개인 절대경로 0 검증, `<EnableUnsafeBinaryFormatterSerialization>false</>` 명시, Dev/Test config 미포함, **Production assets allowlist**(허용 외 파일 차단).
2. **Integrity Manifest**: 핵심 파일(`RiskManagementAI.exe`·Core DLL·`config/security_policy.json`·`rules/*`·`config/column_mapping.json`·KB catalog·NCR placeholder·report templates·`cp949-uhc-map.txt`)의 path·size·SHA256·version·required/optional·security class를 담은 `approved_manifest.json` 생성(build 단계) + ZIP 동봉. `build/03`이 manifest와 ZIP 실제 파일 일치 검증.
3. **시작 시 검증 + 모드 분기**(인박스): 핵심 파일 Hash가 manifest와 다르면 — **운영(Fail-Closed)**: security_policy 불일치→기동/기능 차단, rules→검사 차단, template→Report 차단, KB→검색 차단. **개발(Fallback)**: 경고 후 진행. 모드 판별은 명시 플래그/환경(예: manifest 부재=개발).
4. **Placeholder**: Code Signing은 **운영 절차 문서 placeholder**(자동 서명 미구현), Rollback 절차·Release Approval 기록 항목 추가.
- **제외**: 실제 인증서 서명, 기능 로직 변경, 새 NuGet.

## 구현 세부 / 보안
- 해시 전용(SHA256), 결정적. 외부 0. manifest 경로 가드(`config/` 또는 ZIP 루트). 시작 검증 실패 메시지는 사용자 친화적·감사 가능(해시 prefix만).
- 운영/개발 분기는 **명확·테스트 가능**해야 하며, 운영에서 우회 경로가 없어야 한다(Fail-Closed).

## 테스트 (Windows)
- 정상 패키지 = 검증 PASS·정상 기동. 핵심 파일 **변조** → 도메인별 차단(policy/rules/template/KB) — 운영 모드. 개발 모드 = 경고 후 진행.
- ZIP에 PDB/개인경로/Debug config **0**(스캔). manifest가 ZIP에 포함·일치. `build/00~03 -Version 0.6.0` PASS, SmokeTest 유지(PackagingTests/IntegrityTests 회귀 추가).

## 완료/보고
Release 보안(PDB/경로/Debug 0) + manifest 생성·검증 + 시작 Fail-Closed/Fallback. 변조 차단 양성 케이스 + clean PASS 보고. `docs/39` STAB-WP-03 DONE, `docs/45` C4 연결 갱신 요청.

## Claude Review Checklist
PDB/개인경로/Debug 0 / Unsafe BinaryFormatter false / allowlist / manifest 생성·ZIP 검증 / 시작 검증 운영=Fail-Closed·개발=Fallback / 핵심파일 분류 / 해시 전용·NuGet 0 / 기존 SmokeTest 불변 / Gate A.
