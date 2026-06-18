# 28. Security Review Checklist (Environment-Split Era)

## 목적

커밋/푸시/릴리스/반입 각 단계에서 실행하는 **실무 게이트형 보안 점검 체크리스트**다.
`docs/19_Security_Review_Checklist.md`(기본 체크리스트), `docs/05_Security_Policy.md`, `docs/03_DataCatalog.md`, `CLAUDE.md` §3에 흩어진 보안 원칙을 **하나의 실행 가능한 게이트**로 통합한다. (중복 작성이 아니라 통합·명령 수준 보강)

## 적용 범위

- Dev에서의 commit/push 전 점검
- Release ZIP 생성/검증 전 점검
- 운영환경(Prod) 반입 전 점검

## 제외 범위

- 회사 내부 정보보안 규정 자체(별도 사규를 우선). 본 문서는 Repository/Release 관점 기술 점검에 한정.

---

## 게이트 A — 커밋/푸시 전 (Dev)

> 하나라도 ❌이면 **commit/push 중단**하고 원인을 제거한다.

- [ ] `git status`로 추적될 파일 전수 확인 (의도치 않은 파일 없음)
- [ ] 실제 회사 데이터 없음 (실거래/포지션/고객/계정)
- [ ] 내부규정 **원문** 없음 (공개 catalog/placeholder만 허용)
- [ ] 실제 테이블/컬럼/시스템명 없음 (더미는 `RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 등 일반명만)
- [ ] 비밀번호/토큰/API key/접속 문자열 없음
- [ ] 인증서/키 파일 없음 (`*.pem` `*.key` `*.pfx` `*.cer` `*.crt`)
- [ ] 대용량 모델 가중치 없음 (`*.gguf` `*.bin` `*.safetensors` `*.onnx`)
- [ ] 운영 로그/Export 실데이터 없음 (`logs/`, `reports/`, `exports/`)
- [ ] `.gitignore`가 위 항목을 차단하고 있음

### 점검 명령 (PowerShell, Dev)

```powershell
# 1) 추적 대상 미리보기 (.gitignore 반영)
git add -A --dry-run

# 2) 비밀/접속정보 스캔 (정책·룰 파일의 '금지어 설명'은 오탐이므로 문맥 확인)
git grep -nIE "(-----BEGIN|password\s*[:=]|pwd\s*=|api[_-]?key|secret|token|Data Source\s*=|User Id\s*=|Initial Catalog)" -- . ':!rules' ':!config/security_policy.json' ':!docs' ':!.gitignore'

# 3) 주민등록번호 패턴 스캔 (있으면 즉시 중단)
git grep -nIE "[0-9]{6}-[1-4][0-9]{6}"

# 4) 금지 확장자 스테이징 여부
git diff --cached --name-only | Select-String -Pattern "\.(gguf|bin|safetensors|onnx|pem|key|pfx|cer|crt|env)$"
```

> 위 2)는 정책/룰/문서 파일을 제외(`:!...`)하고 스캔한다. 해당 파일들의 금지어 "설명"은 위반이 아니다.

---

## 게이트 B — Release ZIP 생성/검증 전 (Dev → Test)

- [ ] Self-contained win-x64 publish (`build/01_publish-win-x64.ps1`)
- [ ] portable ZIP + `.sha256` 생성 (`build/02_package-release.ps1`)
- [ ] `build/03_verify-package.ps1` 통과 (해시 + 내용 검증 + 금지파일 부재)
- [ ] ZIP 내부에 `rules/kb/templates/samples/config/deploy` 자산 포함
- [ ] ZIP 내부에 `logs/`, `reports/`, `run.bat` 존재
- [ ] ZIP 내부에 모델 파일 없음, secrets/real_data/internal_docs 없음
- [ ] 외부 인터넷 없이 실행됨(Test PC에서 확인)
- [ ] 자동 업데이트/telemetry/외부 API 동작 없음
- [ ] ReleaseNote / DependencyList 작성됨

---

## 게이트 C — 운영환경 반입 전 (Test → Prod)

- [ ] 승인된 반입 절차 준수 (사내 절차 우선)
- [ ] 반입물은 **portable Release ZIP**만 (source/Git/SDK/installer 반입 금지)
- [ ] 반입 직전 SHA256 재검증 (`Get-FileHash`로 ReleaseNote 값과 대조)
- [ ] 백신 검사 통과
- [ ] 사용자 권한/쓰기 경로(`logs/`,`reports/`,`config/`) 확인
- [ ] 내부규정/NCR/테이블 사전은 운영환경 내에서 **문서오너·검토자 승인 후** 별도 구성
- [ ] 작업 로그 정책 확인 (원문 저장 금지, 해시만)

---

## 운영 로그 내용 규칙 (요약)

| 저장 금지 | 저장 허용 |
|---|---|
| 고객명/계좌번호/개인정보 | 작업 ID |
| 비밀번호/접속정보 | 사번/사용자 ID **해시** |
| 내부규정 원문 | 기능명, 룰 검사 결과 |
| 대량 운영 데이터 원문 | 템플릿/룰/모델 버전, 결과 **해시** |

(상세: `docs/05_Security_Policy.md`)

## 문제 발견 시 대응

1. commit/push/릴리스/반입을 **즉시 중단**한다.
2. 해당 파일을 제거하고, 이미 staged면 unstage, 이미 commit되었으면 사용자에게 보고 후 안전한 정정 방법을 협의(force push 금지).
3. 재발 방지를 위해 `.gitignore`/룰/체크리스트를 갱신한다.

## 테스트 방법

- 게이트 A 명령을 새 변경분에 대해 실행하여 0건 확인.
- 게이트 B는 `build/03_verify-package.ps1`로 자동 검증.

## 향후 확장

- 게이트 A 명령을 `build/`에 pre-commit 검증 스크립트로 추가.
- CI에서 secret 스캔 + 금지 확장자 검사 자동화.

> 관련 문서: `docs/19_Security_Review_Checklist.md`, `docs/05_Security_Policy.md`, `docs/03_DataCatalog.md`, `docs/29_GitHub_Sync_Guide.md`
