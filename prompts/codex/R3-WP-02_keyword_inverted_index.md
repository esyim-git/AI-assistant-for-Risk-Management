# Codex R3-WP-02 — Keyword / Inverted Index 검색 엔진 (NuGet 0)

> 권위 스펙: `docs/17`(R3 검색 엔진), `docs/41 §2`. Release: R3. 선행: R3-WP-01(메타 확장) 권장.

## 목표
현 `KbSearch`의 **linear Contains 스캔**을 **역색인(inverted index)** 기반 후보 추출로 바꾼다. **NuGet 0·인박스·결정적**. **결과 집합·순서·점수는 현 동작과 동일**해야 한다(역색인은 구조화/가속이지 의미 변경이 아니다). Vector/Embedding은 **STOP**.

## 먼저 읽기
`AGENTS.md`, `CLAUDE.md §3·§10`, `docs/17`(R3), `docs/41 §2`, 기존 `Core/Kb/KbSearch.cs`(현 `Score`·필드 가중치·tie-break), `Core/Kb/RegulationCatalog.cs`.

## 브랜치/동기화
```bash
git fetch origin && git switch -c feature/r3-wp-02-kb-index origin/main
dotnet build RiskManagementAI.sln && dotnet run --project tests/RiskManagementAI.SmokeTests
```
- PR→main(squash, `(#PR)`), 게이트 A, NuGet 0.

## 작업 범위 / 제외
- `Core/Kb/KbIndex.cs`(신규): catalog entries → **결정적 토큰화** → 역색인(token → posting: entry + 필드). 빌드는 결정적.
- `KbSearch`가 KbIndex로 **후보를 추린 뒤** 기존 필드 가중치로 점수·정렬(현 `Score` 의미 유지). 외부 시그니처(`Search`) 불변.
- 제외: **Vector/Embedding(STOP)**, 인용형식(WP-03), 적재가드(WP-04).

## Public Interface
- `KbIndex` (catalog로 build, KbSearch 내부 사용). `KbSearchResponse`/`Search(...)` **시그니처 불변**.

## 구현 세부 / 결정성 / 회귀
- **토큰화(결정적)**: 공백·구두점 분리 + `OrdinalIgnoreCase`. 
- ⚠️ **한글 substring 보존**: 현 검색은 `Contains`(부분일치)라 한글 복합어 부분일치를 잡는다. 공백 토큰만으론 한글에서 **회귀 발생** → **char n-gram(예: bigram) 색인 또는 substring fallback**으로 **현 결과를 보존**한다. (형태소 분석기 등 외부 라이브러리 = **STOP**, 도입 금지.)
- **동일성 보장**: 역색인 도입 후에도 동일 쿼리 → **현 linear `KbSearch`와 동일한 결과·순서·점수**. tie-break = `SourceId` ordinal(현행).
- NuGet 0·외부 호출 0.

## 테스트(필수)
- **회귀 동일성**: 쿼리 모음(영문·한글·다중토큰·부분일치)에 대해 **역색인 KbSearch 결과 == 현 동작**(순서·점수 포함). 한글 부분일치 포함 필수.
- 역색인 빌드 결정적(동일 catalog → 동일 색인).
- 빈 쿼리·미일치 → 현행과 동일(0건 + 경고).
- NuGet 0 / 기존 SmokeTest 유지.

## 완료/보고
KbSearch가 역색인 기반이면서 **결과 무변경**. build 0/0 · SmokeTest 유지+신규 · NuGet 0 · 게이트 A 0건 · `docs/17` 진행표 갱신.

## ⚠️ STOP
Vector DB/Embedding Runtime/형태소·검색 외부 라이브러리/모델이 필요해지는 순간 **STOP** → 구현 말고 보고(docs/41 §2 승인).

## Claude Review Checklist
역색인 도입 + **결과·순서·점수 현행 동일(회귀 고정, 한글 부분일치 포함)** / 결정적 토큰화·빌드 / NuGet 0·외부 0·Vector 미도입 / 기존 SmokeTest 유지 / Gate A.
