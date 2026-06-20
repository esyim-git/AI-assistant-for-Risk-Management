# Codex MVP-3 Prompt — Review UI Screens (Goal Mode)

> MVP-3(좌측 메뉴 stub 화면 실구현 + 내비게이션 견고화)을 목표 추진으로 구현한다.
> 권위 스펙·결정은 `docs/37_MVP3_UI_Backlog.md`(U3-00~06, DU-01~06). 진행/핸드백은 docs/37 §상태원장+Resume Brief에 기록.

## 0. 역할 / 모드
너는 Implementation/Test Engineer(Codex)다. `docs/37`의 U3 항목을 우선순위대로 구현한다.
막히면 멈춰 기다리지 말고: 가역적 설계 선택은 **안전 기본값으로 진행(자동결정 ⚠️ 기록)**, NuGet/외부/되돌리기 어려운 것은 **건너뛰고 BLOCKED 기록 후 다음 항목**.

## 1. 먼저 읽기
`AGENTS.md`, `CLAUDE.md`(§3 절대원칙, Excel 함수 제한), `docs/37`(백로그·DU 결정), `docs/30`(한도모니터링 데모), `docs/14`(UI 스펙), `docs/32`(거버넌스), `docs/28`(보안 게이트 A).

## 2. 브랜치·동기화 (docs/32 — develop 삭제됨, main이 정본)
```bash
git fetch origin && git switch -c feature/mvp3-<item> origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests   # 기준선 PASS
```
- 항목별 `feature/mvp3-<item>` → **PR → main**(squash). private Free soft guard 때문에 머지 커밋 subject에 **`(#<PR번호>)` 유지 필수**(docs/32 §4). `main`/태그/삭제 직접 조작 금지.
- **1 단위 = 1 커밋 = 즉시 push**, develop 항상(=main 항상) green 유지, 매 커밋 보안 게이트 A.

## 3. 구현 순서
1. **U3-00** 내비게이션 견고화(하드코딩 인덱스 → 이름/키 매핑) + 메뉴→탭 정확성 회귀 테스트
2. **U3-01** Risk Dashboard/한도 모니터링 (더미 노출 vs 한도 조인, 사용률·초과, 인박스 시각화)
3. **U3-02** History 감사로그 뷰어 (`logs/*.jsonl` read-only, 해시/메타만)
4. **U3-03** Settings 정책 뷰어 (read-only)
5. **U3-04** Feedback Center (기존 ExamplePromotion 위 승인 UI, 재학습 없음)
6. **U3-05** Dashboard home (상태 요약)
7. **U3-06** SmokeTest 확장

## 4. 결정·가드 (DU-01~06 + 절대원칙)
- 모든 화면 **review-only**(데이터 변형·자동실행·외부호출 없음). Settings **뷰 전용**(런타임 정책 쓰기 금지).
- **외부 NuGet 0 유지** — 차트 라이브러리 등 필요하면 **STOP·BLOCKED**(승인 게이트). WPF 인박스만.
- History는 **해시/메타만**(원문 복원 금지), 경로 가드, 빈/누락 graceful.
- Feedback 승격 = 해시·승인 게이트·**모델 재학습 없음**.
- 쓰기 경로 `logs/`·`reports/`·`config/`만. 동작 시 audit log(해시). NoModel/오프라인 기동 유지.
- 금지: 외부 API/자동실행/telemetry/모델파일/실데이터/내부규정, `git push --force`/`reset --hard`/main 직접 push.

## 5. 단위 루프
```
docs/37 스펙 확인 → 작은 단위 구현 → dotnet build + SmokeTest(신규 회귀 추가) →
보안 게이트 A → docs/37 상태원장+Resume Brief 갱신 → commit → push → PR(→main) → CI green → squash merge(주제에 (#PR)) → 다음
```

## 6. 완료 조건 (docs/37 DoD)
- build + SmokeTest 전부 PASS, 메뉴 5개 화면 실동작(stub 0), 내비 정확성 테스트
- 모델/인터넷 없이 기동, NuGet 0/Interop·OpenXML 없음, 정책 런타임 쓰기 없음, 게이트 A 0건

## 7. 최종 보고
1. 구현 U3 항목 + 변경 파일
2. build/SmokeTest 결과(총 PASS 수)
3. NuGet 추가(없음 기대)/보안 게이트 A(0건)
4. PR/머지 결과(번호·commit)
5. docs/37 Resume Brief 갱신 확인 + 남은 BLOCKED/⚠️

> 완료 후 Claude(Tech Lead)가 `git fetch origin main` + docs/37 Resume Brief로 재검증하고 다음 단계를 잡는다.
