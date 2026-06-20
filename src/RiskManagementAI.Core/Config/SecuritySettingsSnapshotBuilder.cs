using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Config;

public sealed class SecuritySettingsSnapshotBuilder
{
    public SecuritySettingsSnapshot Build(PolicyLoadResult policyLoadResult, string ruleVersion, string modelMode)
    {
        if (string.IsNullOrWhiteSpace(ruleVersion))
        {
            throw new ArgumentException("RuleVersion이 비어 있습니다.", nameof(ruleVersion));
        }

        if (string.IsNullOrWhiteSpace(modelMode))
        {
            throw new ArgumentException("모델 모드가 비어 있습니다.", nameof(modelMode));
        }

        var policy = policyLoadResult.Policy;
        var rows = new List<SecuritySettingRow>
        {
            Row("Environment", "RuleVersion", ruleVersion, "룰셋 버전"),
            Row("Environment", "LocalModelMode", modelMode, "로컬 모델 상태"),
            Row("PolicyLoad", "UsedFallback", policyLoadResult.UsedFallback, policyLoadResult.UsedFallback ? "Fallback active" : "Loaded from config"),
            Row("Network", "AllowExternalApi", policy.Network.AllowExternalApi, BlockMeaning(policy.Network.AllowExternalApi)),
            Row("Network", "AllowAutoUpdate", policy.Network.AllowAutoUpdate, BlockMeaning(policy.Network.AllowAutoUpdate)),
            Row("Network", "AllowTelemetry", policy.Network.AllowTelemetry, BlockMeaning(policy.Network.AllowTelemetry)),
            Row("Sql", "AllowAutoExecute", policy.Sql.AllowAutoExecute, BlockMeaning(policy.Sql.AllowAutoExecute)),
            Row("Sql", "DefaultMode", policy.Sql.DefaultMode, "SQL 초안/검토 모드"),
            Row("Vba", "AllowAutoExecute", policy.Vba.AllowAutoExecute, BlockMeaning(policy.Vba.AllowAutoExecute)),
            Row("Vba", "BlockDangerousApi", policy.Vba.BlockDangerousApi, policy.Vba.BlockDangerousApi ? "Dangerous API blocked" : "Review required"),
            Row("Data", "AllowRealDataInRepo", policy.Data.AllowRealDataInRepo, BlockMeaning(policy.Data.AllowRealDataInRepo)),
            Row("Data", "AllowInternalRegulationOriginalInRepo", policy.Data.AllowInternalRegulationOriginalInRepo, BlockMeaning(policy.Data.AllowInternalRegulationOriginalInRepo)),
            Row("Data", "AllowModelFileInRepo", policy.Data.AllowModelFileInRepo, BlockMeaning(policy.Data.AllowModelFileInRepo))
        };

        rows.AddRange(policyLoadResult.Warnings.Select((warning, index) =>
            Row("PolicyWarning", $"Warning{index + 1}", warning, "로드 경고")));

        var findings = new List<SafetyFinding>
        {
            new(
                policyLoadResult.UsedFallback ? "SETTINGS_POLICY_FALLBACK" : "SETTINGS_POLICY_LOADED",
                policyLoadResult.UsedFallback ? SafetySeverity.Medium : SafetySeverity.Info,
                policyLoadResult.UsedFallback ? "Security policy fallback이 활성화되어 있습니다." : "Security policy가 읽기 전용으로 로드되었습니다.")
        };

        if (policy.Network.AllowExternalApi || policy.Network.AllowTelemetry || policy.Network.AllowAutoUpdate)
        {
            findings.Add(new SafetyFinding("SETTINGS_NETWORK_REVIEW_REQUIRED", SafetySeverity.High, "Network 정책에 허용 항목이 있어 검토가 필요합니다."));
        }

        if (policy.Sql.AllowAutoExecute || policy.Vba.AllowAutoExecute)
        {
            findings.Add(new SafetyFinding("SETTINGS_AUTO_EXECUTE_REVIEW_REQUIRED", SafetySeverity.High, "자동 실행 정책에 허용 항목이 있어 검토가 필요합니다."));
        }

        return new SecuritySettingsSnapshot(ruleVersion, modelMode, policyLoadResult.UsedFallback, rows, findings);
    }

    private static SecuritySettingRow Row(string section, string name, bool value, string meaning)
        => Row(section, name, value ? "True" : "False", meaning);

    private static SecuritySettingRow Row(string section, string name, string value, string meaning)
        => new(section, name, value, meaning);

    private static string BlockMeaning(bool allowed)
        => allowed ? "Allowed - review required" : "Blocked";
}

public sealed record SecuritySettingsSnapshot(
    string RuleVersion,
    string ModelMode,
    bool UsedFallback,
    IReadOnlyList<SecuritySettingRow> Rows,
    IReadOnlyList<SafetyFinding> Findings);

public sealed record SecuritySettingRow(
    string Section,
    string Name,
    string Value,
    string Meaning);
