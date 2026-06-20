using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Dashboard;

public sealed class DashboardSnapshotBuilder
{
    public DashboardSnapshot Build(DashboardSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RuleVersion))
        {
            throw new ArgumentException("RuleVersion이 비어 있습니다.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ModelMode))
        {
            throw new ArgumentException("모델 모드가 비어 있습니다.", nameof(request));
        }

        var rows = new List<DashboardMetricRow>
        {
            new("Offline Mode", "Enabled", "외부 통신 없이 동작"),
            new("Local Model", request.ModelMode, "NoModelMode면 생성 비활성"),
            new("Security Policy", request.PolicyLoadResult.UsedFallback ? "Fallback" : "Loaded", request.PolicyLoadResult.UsedFallback ? "safe fallback active" : "config/security_policy.json"),
            new("RuleVersion", request.RuleVersion, "rules/*.txt deterministic version"),
            new("Audit Records", request.AuditLog.Records.Count.ToString("N0"), "TaskLog + FeedbackLog read-only count"),
            new("Promoted Examples", request.PromotedExampleCount.ToString("N0"), "config/promoted_examples.jsonl"),
            new("Reports", request.ReportCount.ToString("N0"), "reports/*.xlsx count")
        };

        var findings = new List<SafetyFinding>
        {
            new("DASHBOARD_READY", SafetySeverity.Info, "Dashboard snapshot이 read-only로 구성되었습니다.")
        };

        if (request.PolicyLoadResult.UsedFallback)
        {
            findings.Add(new SafetyFinding("DASHBOARD_POLICY_FALLBACK", SafetySeverity.Medium, "Security policy fallback이 활성화되어 있습니다."));
        }

        findings.AddRange(request.AuditLog.Findings.Select(finding =>
            new SafetyFinding("DASHBOARD_AUDIT_LOG_WARNING", SafetySeverity.Low, finding.Message)));

        return new DashboardSnapshot(rows, findings);
    }
}

public sealed record DashboardSnapshotRequest(
    PolicyLoadResult PolicyLoadResult,
    string RuleVersion,
    string ModelMode,
    AuditLogReadResult AuditLog,
    int PromotedExampleCount,
    int ReportCount);

public sealed record DashboardSnapshot(
    IReadOnlyList<DashboardMetricRow> Rows,
    IReadOnlyList<SafetyFinding> Findings);

public sealed record DashboardMetricRow(
    string Metric,
    string Value,
    string Detail);
