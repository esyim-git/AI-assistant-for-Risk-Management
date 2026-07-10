# Branch Governance — 브랜치·머지·금지조작·Local-Gate 상세 규칙

> git 브랜치/PR/머지가 거버넌스를 따르는지 점검하는 상세 체크리스트.
> 점검은 **읽기 전용**(`git status`/`git branch`/`git log`/Read/Grep)으로 수행한다.
> **Claude는 main을 직접 수정/병합하지 않는다**(`CLAUDE.md §11.1`). 계획 작업은 `planning/*`에서만 한다.
> 정본: `docs/32_Branch_Governance.md`, `docs/35_Private_Free_Soft_Guard.md`, `AGENTS.md §6`, `CLAUDE.md §8·§11`.

---

## 0. 현재 운영 전제 (public / Hard Protection Migration)

- Repository는 public이다. 이 audit change가 PR `test`/`wpf-build`와 main-push soft guard trigger를 복원한다.
- `main` hard protection은 아직 미적용이다. 첫 hosted green run으로 check 이름을 확인한 뒤 `docs/32` Phase A를 적용한다.
- 현재 단일 계정 workflow에서는 required approval=0/Code Owner review OFF로 self-review 교착을 피한다. 독립 reviewer가 생기면 Phase B(approval 1/Code Owner)를 적용한다.
- `governance-soft-guard`는 hard protection의 대체가 아니라 main push 후 provenance 백업 신호다.

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

## 4. Local + Hosted 머지 게이트

> 로컬 빌드/SmokeTest와 Claude 리뷰는 계속 정본이다. 자동 PR workflow가 활성화된 PR은 `test`·`wpf-build`도 확인하며, Phase A protection 이후에는 둘 다 required check다. queued/skipped/not-run은 success가 아니다.

- [ ] 로컬 `dotnet build`(Release) 성공 증거
- [ ] 로컬 SmokeTest 보고에 합계 줄 **`Total=N PASS / 0 FAIL`** 포함, FAIL=0
- [ ] 이전 Total 보존(직전 기준선 이상), 기존 단언 삭제·약화 0 (`AGENTS.md §3`)
- [ ] 보안 축 통과: `/risk-security-guard`(Gate A, `docs/28`) — NuGet 0·외부 API 0·민감정보 0
- [ ] 4축 리뷰 통과: `/risk-codex-review`(Diff·보안·테스트·문서)
- [ ] 활성 hosted checks `test`·`wpf-build` green(워크플로 변경 PR 자체도 확인)
- [ ] 상태 어휘만 사용, 증거 없는 PASS/VERIFIED 없음 (`CLAUDE.md §11.4`)

> 재현 대조(보고 검증용): `dotnet build RiskManagementAI.sln -c Release` → `dotnet run --project tests/RiskManagementAI.SmokeTests` → 합계 줄 `Total=N PASS / 0 FAIL` 확인.

---

## 5. 머지 후 정합

- [ ] `/risk-doc-truth-sync`로 `docs/38`(Roadmap·Traceability)·`docs/39`(Resume Brief 기준선 SHA·NEXT UP·Total)·`docs/40`(ADR)·`docs/48`(현재 Gate 증거, `docs/44/45` historical) 갱신
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

- 증거 없는 PASS는 적지 않는다. 활성 workflow의 red/queued/skipped/not-run을 green으로 간주하지 않는다.
- 위반은 force push/hard reset이 아니라 **새 PR**로 정정한다.

---

## 참조
- `docs/32_Branch_Governance.md`(public Phase A/B 보호·리뷰·머지) · `docs/35_Private_Free_Soft_Guard.md`(private-Free soft guard 역사·현재 백업 역할).
- `AGENTS.md §6`(Release/Branch) · `CLAUDE.md §8`(Git 원칙) · `§11.1`(main 직접 수정 금지·`planning/*`) · `§11.6`(Local-Gate).
- 연계 스킬: `/risk-security-guard` · `/risk-codex-review` · `/risk-doc-truth-sync`.
