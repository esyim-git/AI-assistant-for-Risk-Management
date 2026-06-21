using System.Text;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Ncr;

public sealed record NcrRuleSet(
    string RuleSetId,
    string RuleSetVersion,
    string EffectiveDate,
    IReadOnlyList<NcrComponent> Components,
    IReadOnlyList<NcrComponentMap> ComponentMap,
    string FormulaDescription,
    IReadOnlyList<string> ValidationSqlTemplates,
    string RegulationBasis,
    IReadOnlyList<NcrApprovalRecord> ApprovalHistory)
{
    public static NcrRuleSet SafeFallback()
    {
        return new NcrRuleSet(
            "NCR_RULESET_PLACEHOLDER",
            "UNAPPROVED-PLACEHOLDER",
            "1900-01-01",
            [
                new NcrComponent(
                    "NCR_COMPONENT_PLACEHOLDER",
                    "Approval-required NCR component",
                    "PLACEHOLDER",
                    "APPROVAL_REQUIRED_NO_REAL_COEFFICIENT")
            ],
            [
                new NcrComponentMap(
                    "NCR_COMPONENT_PLACEHOLDER",
                    "PROD_APPROVED_SOURCE_REQUIRED",
                    "PROD_APPROVED_COLUMN_REQUIRED",
                    "DECIMAL",
                    Required: true)
            ],
            "Placeholder rule set only. Prod-approved NCR rule data is required before calculation.",
            [
                "SELECT base_dt, component_id, calculated_amount FROM approved_ncr_validation_view WHERE base_dt = @BASE_DT"
            ],
            "PUBLIC_OR_PROD_APPROVED_BASIS_REQUIRED",
            [
                new NcrApprovalRecord(
                    "UNAPPROVED_PLACEHOLDER",
                    "DOCUMENT_OWNER_REQUIRED",
                    "1900-01-01T00:00:00Z",
                    "Fallback structure only; no real NCR coefficients or official source text.")
            ]);
    }
}

public sealed record NcrComponent(
    string ComponentId,
    string Name,
    string Category,
    string ValuePolicy);

public sealed record NcrComponentMap(
    string ComponentId,
    string SourceName,
    string ColumnName,
    string DataType,
    bool Required);

public sealed record NcrApprovalRecord(
    string Status,
    string ReviewerRole,
    string ApprovedAt,
    string Note);

public sealed record NcrRuleSetLoadResult(
    NcrRuleSet RuleSet,
    bool UsedFallback,
    IReadOnlyList<SafetyFinding> Findings);

public static class NcrExplain
{
    public const string DraftNotice = "검토용 초안입니다. 공식 해석이나 감독기관 보고용 최종 판단이 아니며, 준법/리스크관리 확인이 필요합니다.";

    public static string Build(NcrRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);

        var builder = new StringBuilder();
        builder.AppendLine(DraftNotice);
        builder.AppendLine($"Rule Set: {ruleSet.RuleSetId}");
        builder.AppendLine($"Rule Set Version: {ruleSet.RuleSetVersion}");
        builder.AppendLine($"Effective Date: {ruleSet.EffectiveDate}");
        builder.AppendLine($"Formula Description: {ruleSet.FormulaDescription}");
        builder.AppendLine($"Regulation Basis: {ruleSet.RegulationBasis}");
        builder.AppendLine("Component Map:");

        foreach (var map in ruleSet.ComponentMap.OrderBy(item => item.ComponentId, StringComparer.Ordinal))
        {
            builder.AppendLine($"- {map.ComponentId}: {map.SourceName}.{map.ColumnName} ({map.DataType}, required={map.Required})");
        }

        builder.AppendLine("Validation SQL Templates:");
        foreach (var sql in ruleSet.ValidationSqlTemplates)
        {
            builder.AppendLine($"- {sql}");
        }

        builder.AppendLine("Approval History:");
        foreach (var approval in ruleSet.ApprovalHistory)
        {
            builder.AppendLine($"- {approval.Status} / {approval.ReviewerRole} / {approval.ApprovedAt}");
        }

        return builder.ToString();
    }
}
