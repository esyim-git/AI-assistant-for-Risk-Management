# Branch Governance — 브랜치·머지·금지조작·Local-Gate 상세 규칙

> git 브랜치/PR/머지가 거버넌스를 따르는지 점검하는 상세 체크리스트.
> 점검은 **읽기 전용**(`git status`/`git branch`/`git log`/Read/Grep)으로 수행한다.
> **Claude는 main을 직접 수정/병합하지 않는다**(`CLAUDE.md §11.1`). 계획 작업은 `planning/*`에서만 한다.
> 정본: `docs/32_Branch_Governance.md`, `docs/35_Private_Free_Soft_Guard.md`, `AGENTS.md §6`, `CLAUDE.md §8·§11`.

---

## 0. 현재 운영 전제 (private Free / Soft Guard)

- Repository는 private + GitHub Free → branch protection/rulesets를 GitHub가 **강제하지 못한다**(`docs/35`).
- 강제 보호 대신 **soft guard**로 우회를 감지: squash only 머지 설정 + `governance-soft-guard` 워크플로(`main` push head commit이 PR 형식이 아니면 실패 신호).
- soft guard는 push 자체를 막지 못한다 → 실패를 **신호**로 보고 즉시 정정한다.
- Pro/Team 업그레이드 또는 public 전환 시 `docs/32 §2·§6`의 hard branch protection으로 즉시 전환.

---

## 1. 브랜치 명명 점검

```bash
git branch --show-current          # 현재 브랜치
git status                         # 작업 트리·추적 상태
git branch -a                      # 로컬/원격 브랜치 목록
```

- [ ] Codex 구현 브랜치 = `feature/<WP-ID>-*` (지정 단일 WP, `AGENTS.md §6`)
- [ ] Claude 계획 작업 브랜치 = `planning/*` (`CLAUDE.md §11.1`)
- [ ] 그 외 허용 prefix: `chore/`·`release/vX.Y.Z`·`hotfix/`·`docs/`·`fix/` (`docs/32 §1`, `docs/35`)
- [ ] `main`/`develop` **위에서 직접 작업·직접 커밋 중이 아님**
- [ ] 신규 작업은 최신 `develop`(또는 `main`)에서 분기

---

## 2. 금지 조작 점검

```bash
git log --oneline origin/main..HEAD     # 브랜치 커밋(작은 Diff·Subject 규약 확인)
git log --oneline -n 20 main            # main 히스토리(직접 변경 흔적 확인)
git reflog -n 30                        # force push/hard reset 흔적(로컬)
```

- [ ] `--force` / force push **없음** (`CLAUDE.md §8`, `docs/35`)
- [ ] `reset --hard`로 히스토리 폐기 **없음**
- [ ] `main`/`develop` **직접 push·직접 수정 없음** (PR 경유만)
- [ ] 브랜치 deletion·force update로 PR 히스토리 훼손 없음
- [ ] Diff가 작음(불필요한 대량 리포맷/무관 파일 변경 없음)

> 위반 발견 시: 절대 force push/hard reset으로 "정정"하지 않는다. 사용자에게 보고하고 새 PR로 정정한다.

---

## 3. 머지 정책 점검 (Squash · `(#PR)`)

- [ ] 머지 방식 = **Squash merge만** (merge commit·rebase merge OFF, `docs/35`)
- [ ] Commit Subject 끝에 **`(#<PR번호>)`** 포함 (soft guard가 PR 표식 확인, `docs/32 §4`)
- [ ] `gh pr merge --squash` 기본 subject 유지, custom subject도 `(#N)` 필수
- [ ] 머지 후 feature 브랜치 삭제(권장, delete branch on merge ON)
- [ ] 커밋 타입 규약: `feat`/`fix`/`docs`/`chore`/`refactor`/`test`/`security`

---

## 4. Local-Gate 머지 게이트 (현재 운영 모델)

> 머지 게이트 = **① 로컬 빌드+SmokeTest 증거 ＋ ② Claude 코드리뷰**. GitHub CI green을 머지 전제로 **요구하지 않는다**(분 가용 시 보조망). (`CLAUDE.md §11.6`, `AGENTS.md §5`)

- [ ] 로컬 `dotnet build`(Release) 성공 증거
- [ ] 로컬 SmokeTest 보고에 합계 줄 **`Total=N PASS / 0 FAIL`** 포함, FAIL=0
- [ ] 이전 Total 보존(직전 기준선 이상), 기존 단언 삭제·약화 0 (`AGENTS.md §3`)
- [ ] 보안 축 통과: `/risk-security-guard`(Gate A, `docs/28`) — NuGet 0·외부 API 0·민감정보 0
- [ ] 4축 리뷰 통과: `/risk-codex-review`(Diff·보안·테스트·문서)
- [ ] 상태 어휘만 사용, 증거 없는 PASS/VERIFIED 없음 (`CLAUDE.md §11.4`)

> 재현 대조(보고 검증용): `dotnet build RiskManagementAI.sln -c Release` → `dotnet run --project tests/RiskManagementAI.SmokeTests` → 합계 줄 `Total=N PASS / 0 FAIL` 확인.

---

## 5. 머지 후 정합

- [ ] `/risk-doc-truth-sync`로 `docs/38`(Roadmap·Traceability)·`docs/39`(Resume Brief 기준선 SHA·NEXT UP·Total)·`docs/40`(ADR)·`docs/45`(Gate 증거) 갱신
- [ ] 문서 상태 어휘가 실제 main과 일치(과대표기 없음)
- [ ] `governance-soft-guard` 워크플로 실패가 없는지 확인, 실패 시 원인 정정

---

## 6. 판정 템플릿

```
[Branch Governance] <branch>
- 브랜치명      : PASS | 위반 — <근거>
- 금지조작      : PASS | 위반 — force push/hard reset/main 직접 변경
- Squash·(#PR)  : PASS | 위반 — <근거>
- Local-Gate    : PASS | 위반 — Total=N PASS / 0 FAIL 증거 / Claude 리뷰
- 문서 정합     : PASS | 후속(/risk-doc-truth-sync)

위반(있으면):
- <항목> — <근거> — <정정 조치>

판정: 머지 가능(거버넌스 PASS) | 머지 불가(위반 N건)
```

- 증거 없는 PASS는 적지 않는다. GitHub CI green은 머지 전제가 아니다(보조망).
- 위반은 force push/hard reset이 아니라 **새 PR**로 정정한다.

---

## 참조
- `docs/32_Branch_Governance.md`(브랜치 모델·보호·리뷰·머지·hard protection 전환) · `docs/35_Private_Free_Soft_Guard.md`(soft guard·squash only·`(#PR)`).
- `AGENTS.md §6`(Release/Branch) · `CLAUDE.md §8`(Git 원칙) · `§11.1`(main 직접 수정 금지·`planning/*`) · `§11.6`(Local-Gate).
- 연계 스킬: `/risk-security-guard` · `/risk-codex-review` · `/risk-doc-truth-sync`.
