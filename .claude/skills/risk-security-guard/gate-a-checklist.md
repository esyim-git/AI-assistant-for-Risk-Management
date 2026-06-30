# Gate A 체크리스트 — 커밋/푸시 전 (Dev)

정본: `docs/28_Security_Review_Checklist.md` 게이트 A. 기본: `docs/19`. 불변: `AGENTS.md §3·§4`.
**하나라도 ❌이면 commit/push 중단** → 원인 제거 → unstage/제거 → `.gitignore`/룰 갱신. **force push·hard reset 금지** (`CLAUDE.md §8`).

---

## 1. 스테이징 전수 (의도치 않은 파일 0)
- [ ] `git status`로 추적될 파일 전수 확인
- [ ] `git diff --cached --name-only`로 staged 파일 목록 검토
- [ ] `.gitignore`가 금지 항목(로그/리포트/모델/키)을 차단

## 2. 실데이터 / 실명칭 0
- [ ] 실거래/포지션/고객/계정 등 회사 실데이터 없음
- [ ] 실 테이블/컬럼/시스템명 없음 — 더미는 일반명만(`RISK_EXPOSURE_DAILY`, `RISK_LIMIT_MASTER` 등, `docs/03`)
- [ ] 주민등록번호 등 개인정보 패턴 없음

## 3. 원문 0 (내부규정 / NCR)
- [ ] 내부규정 **원문** 없음 (공개 catalog/placeholder만 허용)
- [ ] NCR 공식본 **원문** 없음 (운영환경 권한통제 KB에서만 적재)

## 4. 비밀/키/접속정보 0
- [ ] 비밀번호/토큰/API key/접속 문자열 없음
- [ ] 인증서/키 파일 없음: `*.pem` `*.key` `*.pfx` `*.cer` `*.crt`

## 5. 모델 / 대용량 / 운영 로그 0
- [ ] 모델 가중치 없음: `*.gguf` `*.bin` `*.safetensors` `*.onnx`
- [ ] 운영 로그/Export 실데이터 없음(`logs/`, `reports/`, `exports/`)

## 6. 불변 원칙
- [ ] 외부 NuGet `PackageReference` 추가 = 0 (`AGENTS.md §3`; 필요 시 STOP → 승인 문서)
- [ ] 외부 API 호출 0 · Telemetry 0 · 자동 업데이트 0
- [ ] 쓰기 경로는 `logs/`·`reports/`·`config/`만 (경로 가드 우선)
- [ ] Audit 로그는 원문 미저장, 사번/사용자 ID는 **해시만** (`docs/05`)

---

## 점검 명령 (PowerShell, Dev)

> 게이트 A 명령은 **새 변경분에 대해 0건**을 확인하는 용도다.
> 스캔 2)는 정책/룰/문서 파일을 제외(`:!...`)한다 — 해당 파일의 금지어 "설명"은 위반이 아니라 오탐이다.

```powershell
# 1) 추적 대상 미리보기 (.gitignore 반영)
git add -A --dry-run
git diff --cached --name-only

# 2) 비밀/접속정보 스캔 (정책·룰·문서 제외; 문맥 확인)
git grep -nIE "(-----BEGIN|password\s*[:=]|pwd\s*=|api[_-]?key|secret|token|Data Source\s*=|User Id\s*=|Initial Catalog)" -- . ':!rules' ':!config/security_policy.json' ':!docs' ':!.gitignore'

# 3) 주민등록번호 패턴 스캔 (있으면 즉시 중단)
git grep -nIE "[0-9]{6}-[1-4][0-9]{6}"

# 4) 금지 확장자 스테이징 여부
git diff --cached --name-only | Select-String -Pattern "\.(gguf|bin|safetensors|onnx|pem|key|pfx|cer|crt|env)$"

# 5) NuGet PackageReference 추가 여부 (Diff 기준)
git diff --cached -- "*.csproj" | Select-String -Pattern "PackageReference"
```

### 제외 규칙 (오탐 방지)
- 금지어 "설명"이 정상인 파일은 스캔에서 제외한다: `rules/`, `config/security_policy.json`, `docs/`, `.gitignore`.
- 더미 명칭(`RISK_EXPOSURE_DAILY`/`RISK_LIMIT_MASTER` 등 일반명)은 위반이 아니다.
- `*.csproj`의 `PackageReference`는 **추가된 라인(`+`)만** 위반 후보다. 기존 in-box BCL 참조는 정상.

---

## 위반 발견 시 대응 (`docs/28`)
1. commit/push를 **즉시 중단**.
2. 해당 파일 제거 → staged면 `git restore --staged <file>`로 unstage. 이미 commit되었으면 사용자에게 보고 후 안전한 정정 방법 협의(**force push 금지**).
3. 재발 방지: `.gitignore`/룰/체크리스트 갱신.

## 산출물 형식
- 항목별 `PASS`/`FAIL` 표.
- FAIL: `항목 — 위반 파일(경로:라인) — 사유 — 정정 조치`.
- 최종 한 줄: **게이트 A PASS(commit/push 가능)** 또는 **게이트 A FAIL(중단, 위반 N건)**.

> 연계: `/risk-codex-review`(보안 축 동일 기준 + 4축) · `/risk-branch-governance`(머지·브랜치 규약) · `/risk-gate-bc`(릴리스·반입 게이트 B/C).
