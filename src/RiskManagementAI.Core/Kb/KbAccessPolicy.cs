using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Kb;

public enum KbDisclosure
{
    PublicCited,
    MetadataOnly,
    ApprovalRequired
}

public sealed record KbAccessDecision(
    KbDisclosure Disclosure,
    bool SourceTextAllowed,
    string Reason,
    IReadOnlyList<SafetyFinding> Findings);

public static class KbAccessPolicy
{
    private static readonly HashSet<string> PublicStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "CATALOG_ONLY",
        "PUBLIC_APPROVED",
        "APPROVED_PUBLIC"
    };

    public static KbAccessDecision Evaluate(RegulationCatalogEntry entry)
    {
        var findings = new List<SafetyFinding>();
        var normalizedStatus = entry.Status.Trim();
        var disclosure = KbDisclosure.MetadataOnly;
        var reason = "알 수 없는 status이므로 원문 없이 catalog metadata만 노출합니다.";

        if (PublicStatuses.Contains(normalizedStatus))
        {
            disclosure = KbDisclosure.PublicCited;
            reason = "공개 catalog metadata와 출처 locator를 인용할 수 있습니다.";
        }
        else if (normalizedStatus.Equals("PROD_ONLY", StringComparison.OrdinalIgnoreCase))
        {
            reason = "원문 미적재 - Prod 권한통제 KB에서만 문서오너 승인 후 확인합니다.";
            findings.Add(new SafetyFinding(
                "KB_PROD_ONLY_METADATA",
                SafetySeverity.Info,
                $"source_id={entry.SourceId}: 내부규정은 repo에서 원문을 노출하지 않고 metadata 표식만 제공합니다."));
        }
        else if (normalizedStatus.Equals("MANUAL_APPROVAL_REQUIRED", StringComparison.OrdinalIgnoreCase))
        {
            disclosure = KbDisclosure.ApprovalRequired;
            reason = "원문 미적재 - 문서오너 승인 및 수동 적재 전까지 metadata 표식만 노출합니다.";
            findings.Add(new SafetyFinding(
                "KB_APPROVAL_REQUIRED",
                SafetySeverity.Medium,
                $"source_id={entry.SourceId}: 문서오너 승인 전에는 원문을 노출하지 않습니다."));
        }
        else
        {
            findings.Add(new SafetyFinding(
                "KB_UNKNOWN_STATUS",
                SafetySeverity.High,
                $"source_id={entry.SourceId}: 알 수 없는 KB status '{entry.Status}'입니다. 원문 없이 metadata만 노출합니다."));
        }

        if (IsMissingGateMetadata(entry.LicenseStatus))
        {
            findings.Add(new SafetyFinding(
                "KB_LICENSE_MISSING",
                SafetySeverity.Medium,
                $"source_id={entry.SourceId}: license_status metadata 확인이 필요합니다."));
        }

        if (IsMissingGateMetadata(entry.ApprovalStatus))
        {
            findings.Add(new SafetyFinding(
                "KB_APPROVAL_MISSING",
                SafetySeverity.Medium,
                $"source_id={entry.SourceId}: approval_status metadata 확인이 필요합니다."));
        }

        return new KbAccessDecision(disclosure, SourceTextAllowed: false, reason, findings);
    }

    private static bool IsMissingGateMetadata(string value)
    {
        var normalized = value.Trim();
        return normalized.Length == 0
            || normalized.Equals("NOT_LOADED", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("CONFIRM_", StringComparison.OrdinalIgnoreCase);
    }
}
