using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Generation;

public sealed class NoModelDraftService : ILocalDraftService
{
    public const string ModeName = "NoModelMode";

    private readonly SecurityPolicy _policy;

    public NoModelDraftService(SecurityPolicy? policy = null)
    {
        _policy = policy ?? SecurityPolicy.SafeDefaults();
    }

    public DraftResponse GenerateDraft(DraftRequest? request)
    {
        var findings = BuildPolicyFindings().ToList();
        findings.Insert(0, new SafetyFinding(
            "DRAFT_NO_MODEL_MODE",
            SafetySeverity.Info,
            "로컬 모델이 구성되지 않아 생성 기능은 비활성화되어 있습니다. 안전 검사와 데이터 프로파일링은 계속 사용할 수 있습니다."));

        if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            findings.Add(new SafetyFinding(
                "DRAFT_PROMPT_EMPTY",
                SafetySeverity.Low,
                "생성 요청이 비어 있어 NoModelMode 안내만 반환했습니다."));
        }

        var requiresPolicyReview = findings.Any(f => f.Severity is SafetySeverity.Blocker or SafetySeverity.High);
        var message = requiresPolicyReview
            ? "보안 정책 검토가 필요하므로 생성 기능을 비활성 상태로 유지합니다."
            : "NoModelMode입니다. 로컬 모델이 설치되기 전까지 생성 초안 대신 안전 안내를 반환합니다.";

        return new DraftResponse(
            IsAvailable: false,
            Mode: ModeName,
            Message: message,
            DraftText: null,
            Findings: findings);
    }

    private IEnumerable<SafetyFinding> BuildPolicyFindings()
    {
        if (_policy.Network.AllowExternalApi || _policy.Network.AllowAutoUpdate || _policy.Network.AllowTelemetry)
        {
            yield return new SafetyFinding(
                "DRAFT_POLICY_UNSAFE_NETWORK",
                SafetySeverity.High,
                "생성 정책 검토 필요: 외부 API, 자동 업데이트, 텔레메트리는 모두 차단되어야 합니다.");
        }
        else
        {
            yield return new SafetyFinding(
                "DRAFT_EXTERNAL_COMM_BLOCKED",
                SafetySeverity.Info,
                "보안 정책상 외부통신, 자동 업데이트, 텔레메트리는 차단되어 있습니다.");
        }

        if (_policy.Sql.AllowAutoExecute || _policy.Vba.AllowAutoExecute)
        {
            yield return new SafetyFinding(
                "DRAFT_POLICY_AUTO_EXECUTE_ENABLED",
                SafetySeverity.High,
                "생성 정책 검토 필요: SQL/VBA 자동 실행은 차단되어야 합니다.");
        }
        else
        {
            yield return new SafetyFinding(
                "DRAFT_AUTO_EXECUTE_BLOCKED",
                SafetySeverity.Info,
                "보안 정책상 SQL/VBA 자동 실행은 차단되어 있습니다.");
        }
    }
}
