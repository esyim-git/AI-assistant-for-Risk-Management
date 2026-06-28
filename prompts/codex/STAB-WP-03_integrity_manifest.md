# Codex STAB-WP-03 — Release Security + Integrity Manifest (Fail-Closed)

> 권위 스펙: `docs/39 §B`(STAB-WP-03), `docs/40`(ADR-008), `docs/38`(RR-13, RR-14). 선행: STAB-WP-01.
>
> **분할 상태**: **03a(build측 — Release 보안 PDB/Debug 제거 + `approved_manifest.json` 생성(build/01)·검증(build/03))는 구현 완료**(local-gate 검증 대기). 본 프롬프트의 **NEXT UP = 03b(runtime)**: 앱 시작 시 Fail-Closed 검증 + manifest 독립 신뢰 앵커(서명 어셈블리에 expected-hash 임베드/공개키) + 운영 모드 분기(아래 작업범위 3). 03b는 C#/App·Core 변경이라 **로컬 build+run 검증** 필수.

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
2. **Integrity Manifest**: 핵심 파일의 path·size·SHA256·version·required/optional·security class를 담은 `approved_manifest.json` 생성(build 단계) + ZIP 동봉. `build/03`이 manifest와 ZIP 실제 파일 일치 검증.
   - **필수 대상(실제 패키징되는 파일만)**: `RiskManagementAI.exe`(apphost) · **`RiskManagementAI.dll`(관리 앱 어셈블리 — WPF/시작/검증 코드 실체; `PublishSingleFile=false`라 exe는 호스트일 뿐)** · `RiskManagementAI.Core.dll`(Core 로직) · `config/security_policy.json` · `rules/*` · `config/column_mapping.json` · KB catalog · NCR placeholder · report templates.
   - **CP949 매핑표(`cp949-uhc-map.txt`)는 loose 파일이 아니라 `RiskManagementAI.Core.dll`에 임베디드 리소스**다 → manifest에 ZIP 경로로 넣지 말 것(없는 파일 → clean 패키지 검증 실패). **Core DLL 해시로 커버** + 런타임은 `Cp949Decoder`의 임베디드 리소스 해시(`ExpectedMappingSha256`) 검증에 의존.
3. **시작 시 검증 + 모드 분기**(인박스): 핵심 파일 Hash가 manifest와 다르면 — **운영(Fail-Closed)**: security_policy 불일치→기동/기능 차단, rules→검사 차단, template→Report 차단, KB→검색 차단.
   - **manifest 부재 = 개발로 간주 금지**: 패키지/운영 실행은 **manifest가 없거나 읽기 실패 시에도 Fail-Closed**(부재로 검사 우회 불가). 개발 Fallback은 **패키지 릴리스에 존재하지 않는 명시적 dev 전용 스위치/환경**(예: `RMAI_DEV=1` + 소스 트리 표식)으로만 활성. ZIP에는 그 스위치가 없어야 한다.
   - **manifest 신뢰 앵커(P1)**: 압축 해제 디렉터리가 쓰기 가능하면 공격자가 파일 + `approved_manifest.json`을 같은 폴더에서 동시에 고쳐 해시를 맞출 수 있다 → manifest 자체를 **독립 신뢰 앵커**로 검증한다: manifest의 기대 해시를 **서명된 관리 어셈블리(`RiskManagementAI.dll`)에 임베드**하거나 공개키 서명 검증 후에만 manifest를 신뢰(ADR-008). 폴더 내 manifest만으로는 post-release 변조 미탐지 → 불충분.
4. **Placeholder**: Code Signing은 **운영 절차 문서 placeholder**(자동 서명 미구현), Rollback 절차·Release Approval 기록 항목 추가.
- **제외**: 실제 인증서 서명, 기능 로직 변경, 새 NuGet.

## 구현 세부 / 보안
- 해시 전용(SHA256), 결정적. 외부 0. manifest 경로 가드(`config/` 또는 ZIP 루트). 시작 검증 실패 메시지는 사용자 친화적·감사 가능(해시 prefix만).
- **운영 = manifest 부재/불일치 모두 Fail-Closed**(우회 경로 0). 개발 Fallback은 패키지에 없는 dev 전용 스위치로만. manifest는 임베드 expected-hash/서명 등 **독립 앵커**로 먼저 신뢰 확립 후 사용.

## 테스트 (Windows)
- 정상 패키지 = 검증 PASS·정상 기동. 핵심 파일 **변조** → 도메인별 차단(policy/rules/template/KB) — 운영 모드. 개발(dev 스위치) = 경고 후 진행.
- **manifest 삭제/누락** → 운영 **Fail-Closed**(개발로 오인 금지). **`RiskManagementAI.dll`(앱 어셈블리) 변조** → 차단(검증기 자체 보호). **파일+폴더내 manifest 동시 변조** → 독립 앵커로 **차단**. **Core DLL 변조**(CP949 매핑 포함) → 차단.
- ZIP에 PDB/개인경로/Debug config **0**(스캔). manifest가 ZIP에 포함·일치. CP949 매핑표는 loose 파일로 요구하지 않음(Core DLL 해시로 커버). `build/00~03 -Version 0.6.0` PASS, SmokeTest 유지(PackagingTests/IntegrityTests 회귀 추가).

## 완료/보고
Release 보안(PDB/경로/Debug 0) + manifest 생성·검증 + 시작 Fail-Closed/Fallback. 변조 차단 양성 케이스 + clean PASS 보고. `docs/39` STAB-WP-03 DONE, `docs/45` C4 연결 갱신 요청.

## Claude Review Checklist
PDB/개인경로/Debug 0 / Unsafe BinaryFormatter false / allowlist / 필수대상에 **앱 DLL(`RiskManagementAI.dll`)+Core DLL** 포함·CP949는 loose 파일 아님 / manifest **부재도 운영 Fail-Closed**(dev 스위치는 패키지에 부재) / manifest **독립 신뢰 앵커**(임베드 expected-hash/서명) / 도메인별 차단 / 해시 전용·NuGet 0 / 기존 SmokeTest 불변 / Gate A.
