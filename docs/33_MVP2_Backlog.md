# 33. MVP-2 Backlog (Local LLM · 규정/NCR RAG · Excel 리포트 · 승인형 피드백)

## 목적

MVP-1(룰 엔진·프로파일러·로깅·정책)을 기반으로, **오프라인 보조 생성·검색·리포트** 기능을 작은 단위로 추가한다.
MVP-1과 동일하게 안전성·감사가능성·오프라인을 최우선으로 한다. (출처: `docs/10_Roadmap.md` v0.4~v1.0, `docs/21` 향후확장)

## 적용 범위

- C#/.NET/WPF self-contained 앱. 모든 신규 기능은 **모델/인터넷 없이도 앱이 기동**해야 한다(NoModel 폴백).

## 제외 범위 (절대 금지)

- 외부/클라우드 API, 자동 업데이트, telemetry
- 모델 가중치 파일 repo 포함, 모델 자동 재학습
- 실제 회사 데이터·내부규정 원문·NCR 공식본 repo 포함
- SQL/VBA 자동 실행, Golden6 자동 접속

---

## 결정 핀다운 (DM-01~DM-05) — 구현자는 그대로 따른다

| ID | 주제 | 결정 |
|---|---|---|
| **DM-01** | Local LLM | **오프라인 전용·선택적.** 모델 없으면 `NoModelMode`로 생성 기능 비활성(룰/프로파일/검색은 동작). 모델 파일은 `model_pack/`(gitignored)에 운영환경에서만 적재, repo 미포함. 추론 라이브러리는 오프라인 동작 가능한 것만, 추가 시 STOP·승인. **생성 결과(SQL/VBA)는 반드시 MVP-1 Safety Checker 통과 + audit log 기록.** 출력은 CLAUDE.md SQL 8단계 / 규정 10단계 포맷. |
| **DM-02** | 규정/NCR RAG | repo에는 **공개 catalog만**(`kb/public_regulation_catalog.csv`). 내부규정/NCR 원문은 **Prod에서 문서오너 승인 후 권한통제형 KB로만** 적재(`docs/09`,`docs/17`,`CLAUDE.md §10`). 초기 구현=catalog 키워드 검색 + 버전/시행일/출처 표기. 답변은 항상 **"검토용 초안"** 명시 + 출처. |
| **DM-03** | Excel 리포트 생성 | 산출 수식/함수는 **Excel 2021 호환**만(생성물 자체를 `Excel2021FunctionChecker`로 검사). VBA 자동 실행 없음. 생성 방식은 **인박스 xlsx 직접 패키징**으로 확정한다: `System.IO.Compression` + `templates/report` XML 템플릿 치환, **NuGet 0**, Interop 금지, OpenXML SDK 미도입. 쓰기 경로는 `reports/`만 허용하고 생성 1회=hash-only audit log 1줄. 풍부한 서식이 꼭 필요해 이 방식으로 충족 불가하면 강행하지 말고 다시 BLOCKED로 둔다. |
| **DM-04** | 승인형 피드백 | **모델 재학습 아님.** 검토자 승인 → 팀 표준 **예제/프롬프트 KB로 승격**(curation)만 한다. 기존 `FeedbackLogEntry`(해시·승인상태) 활용, 원문 평문 저장 금지. |
| **DM-05** | 공통 | 모든 기능에 audit log(`TaskLogWriter`, 해시 전용) + 정책(`PolicyLoader`) 강제 + NoModel/오프라인 폴백 적용. |

---

## 백로그 항목

> 형식: 목적 / 입력 / 처리 / 출력 / 완료조건 / 테스트 / 보안 / 예상파일. 우선순위 순.

### M2-01. LLM 추상화 + NoModelMode (생성 게이트)
- **목적**: LLM 유무와 무관히 동작하는 추상화 계층. 모델 없으면 비활성, 있으면 로컬 추론.
- **입력**: 사용자 요청 텍스트, (선택) `model_pack/` 내 승인 모델
- **처리**: `ILocalDraftService` 인터페이스 + `NoModelDraftService`(기본, "모델 없음" 안내) / 실제 추론 구현은 후속. 정책에서 외부통신 차단 확인.
- **출력**: 초안 텍스트 또는 "모델 미탑재" 안내 finding
- **완료조건**: 모델 없이 앱 기동·동작, 인터페이스로 주입 가능
- **테스트**: NoModelMode에서 생성 호출 → 안전 안내 반환(예외 없음)
- **보안**: 외부 API 없음. 모델 경로는 `model_pack/` 상대경로만
- **예상파일**: `Core/Generation/ILocalDraftService.cs`, `NoModelDraftService.cs`, tests

### M2-02. SQL/VBA 초안 생성 파이프라인 (안전 게이트 연동)
- **목적**: 생성 초안을 **반드시 Safety Checker + audit log**를 거쳐 제공.
- **입력**: 요청 + 템플릿(`templates/`)
- **처리**: 초안 생성(M2-01) → `SqlSafetyChecker`/`VbaSafetyChecker` 통과 → Blocker면 반려·사유 → `TaskLogWriter` 기록(해시)
- **출력**: 검증된 초안 + Finding 목록 + 8단계/표준 포맷
- **완료조건**: Blocker 포함 초안은 그대로 제공되지 않음. 모든 생성 1회=TaskLog 1줄
- **테스트**: 위험 구문 포함 생성 시 Blocker 반려, 정상 초안 통과 + 로그 기록
- **보안**: 자동 실행 금지. 로그 해시 전용
- **예상파일**: `Core/Generation/DraftPipeline.cs`, tests, (UI 연동)

### M2-03. 규정/NCR Catalog 검색 (권한통제형 placeholder)
- **목적**: 공개 catalog 키워드 검색 + 출처/버전 표기. 내부 원문은 Prod 승인 적재 자리만.
- **입력**: `kb/public_regulation_catalog.csv`, 질의어
- **처리**: 키워드/토픽 매칭, 시행일·버전·출처 부착. 내부 KB는 권한·승인 게이트(placeholder)
- **출력**: 후보 조항 목록(검토용 초안 명시) + 10단계 포맷 골격
- **완료조건**: catalog 검색 동작, 답변에 "검토용 초안"·출처 항상 포함
- **테스트**: 더미 catalog 질의 → 결과·출처 반환. 내부원문 미포함 확인
- **보안**: 내부규정 원문 repo 미포함(DM-02). 권한/감사 로그 자리 마련
- **예상파일**: `Core/Kb/RegulationCatalog.cs`, `KbSearch.cs`, tests

### M2-04. Excel 2021 리포트 생성 (호환 검사 연동)
- **목적**: 분석/한도 모니터링 결과를 Excel 2021 호환 리포트로 산출.
- **입력**: `DataProfileResult`, 한도 샘플, 검사 결과
- **처리**: 시트 구성(README/RAW_DATA/DATA_PROFILE/VALIDATION/SUMMARY/LIMIT_MONITORING/EXCEPTION_LIST/SQL_USED/CHANGE_LOG/AI_COMMENTARY). 수식은 `Excel2021FunctionChecker` 통과만
- **출력**: `reports/` 하위 xlsx
- **완료조건**: 생성물에 365 전용 함수 없음, 오프라인 생성, NuGet/Interop/OpenXML SDK 없음
- **테스트**: 더미 입력 → 리포트 생성, 함수 호환 검사 PASS, xlsx ZIP 필수 part 확인
- **보안**: 쓰기 경로 `reports/`만. VBA 자동실행 없음. 생성 1회=hash-only audit log 1줄
- **예상파일**: `Core/Report/ExcelReportBuilder.cs`, tests

### M2-05. 승인형 피드백 승격 (예제 KB)
- **목적**: 검토자 승인 시 초안을 팀 표준 예제로 승격(재학습 아님).
- **입력**: `FeedbackLogEntry`(승인상태)
- **처리**: 승인된 항목을 예제 KB(구조/해시)로 승격, 미승인은 보류
- **출력**: 예제 KB 엔트리(원문 평문 미저장)
- **완료조건**: 승인 흐름 동작, 로그 감사 가능
- **테스트**: 승인→승격, 미승인→제외
- **보안**: 모델 재학습 없음(DM-04). 해시 전용
- **예상파일**: `Core/Feedback/ExamplePromotion.cs`, tests

### M2-06. UI 연동 + SmokeTest 확장
- **목적**: 위 기능 WPF 탭/패널 연동 + 회귀 테스트.
- **완료조건**: 모델/인터넷 없이 기동, 기존 + 신규 SmokeTest 전부 PASS
- **예상파일**: `App/MainWindow.xaml(.cs)`, `tests/.../Program.cs`

---

## MVP-2 전체 완료 조건 (DoD)

- [ ] `dotnet build RiskManagementAI.sln` 성공, SmokeTest 전부 PASS
- [ ] **모델/인터넷 없이 앱 기동·동작**(NoModelMode)
- [ ] 생성 초안은 Safety Checker 통과 + audit log(해시) 기록
- [ ] 규정 답변에 "검토용 초안" + 출처 항상 포함, 내부원문 repo 미포함
- [ ] Excel 리포트는 2021 호환(365 함수 0건), `reports/`에만 생성
- [ ] 승인형 피드백은 예제 승격만(재학습 없음)
- [ ] 외부 API/자동실행/telemetry/모델파일 커밋 0건

## 진행/핸드백

- 시작 프롬프트: `prompts/codex_mvp2_goal_mode_prompt.md`
- 핸드백·상태 원장: docs/31 패턴 재사용(MVP-2용 worklog 또는 본 문서 하단 원장). 작업 브랜치 `feature/mvp2-<item>`.

> 관련 문서: `docs/10_Roadmap.md`, `docs/17_KB_RAG_Design.md`, `docs/08_NCR_Module_Design.md`, `docs/16_Excel2021_VBA_Guide.md`, `docs/07_Plugin_Recommendation.md`, `docs/21_Implementation_Backlog.md`
