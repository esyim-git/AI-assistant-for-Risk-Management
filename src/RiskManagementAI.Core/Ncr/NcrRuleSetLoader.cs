using System.Text.Json;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Ncr;

public static class NcrRuleSetLoader
{
    private const string DefaultRuleSetPath = "config/ncr/ncr_ruleset_sample.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static NcrRuleSetLoadResult LoadDefault()
    {
        return LoadFromFile(DefaultRuleSetPath);
    }

    public static NcrRuleSetLoadResult LoadFromFile(string relativeRuleSetPath)
    {
        if (!IsSafeRelativeNcrConfigPath(relativeRuleSetPath))
        {
            return CreateFallback(new SafetyFinding(
                "NCR_RULESET_PATH_REJECTED",
                SafetySeverity.High,
                $"NCR rule set path '{relativeRuleSetPath}' is not allowed. Only config/ncr/*.json paths are allowed."));
        }

        var resolvedPath = ResolveRuleSetPath(relativeRuleSetPath);
        if (resolvedPath is null)
        {
            return CreateFallback(new SafetyFinding(
                "NCR_RULESET_MISSING",
                SafetySeverity.Medium,
                $"NCR rule set file '{relativeRuleSetPath}' was not found. Safe fallback structure is used."));
        }

        try
        {
            var json = File.ReadAllText(resolvedPath);
            var ruleSet = JsonSerializer.Deserialize<NcrRuleSet>(json, JsonOptions);
            if (ruleSet is null)
            {
                return CreateFallback(new SafetyFinding(
                    "NCR_RULESET_INVALID",
                    SafetySeverity.Medium,
                    $"NCR rule set file '{relativeRuleSetPath}' was empty or invalid."));
            }

            var findings = Validate(ruleSet).ToList();
            return findings.Any(IsUnsafe)
                ? CreateFallback(findings)
                : new NcrRuleSetLoadResult(ruleSet, UsedFallback: false, findings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return CreateFallback(new SafetyFinding(
                "NCR_RULESET_LOAD_FAILED",
                SafetySeverity.Medium,
                $"NCR rule set file '{relativeRuleSetPath}' could not be loaded safely: {ex.Message}"));
        }
    }

    private static IEnumerable<SafetyFinding> Validate(NcrRuleSet ruleSet)
    {
        foreach (var finding in ValidateRequiredStructure(ruleSet))
        {
            yield return finding;
        }

        var checker = new SqlSafetyChecker();
        foreach (var sqlTemplate in ruleSet.ValidationSqlTemplates ?? [])
        {
            foreach (var finding in checker.Check(sqlTemplate).Where(finding => finding.Severity >= SafetySeverity.Medium))
            {
                yield return finding;
            }
        }
    }

    private static IEnumerable<SafetyFinding> ValidateRequiredStructure(NcrRuleSet ruleSet)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.RuleSetId))
        {
            yield return Missing("RuleSetId");
        }

        if (string.IsNullOrWhiteSpace(ruleSet.RuleSetVersion))
        {
            yield return Missing("RuleSetVersion");
        }

        if (!DateOnly.TryParseExact(ruleSet.EffectiveDate ?? string.Empty, "yyyy-MM-dd", out _))
        {
            yield return new SafetyFinding(
                "NCR_RULESET_EFFECTIVE_DATE_INVALID",
                SafetySeverity.Medium,
                "NCR rule set EffectiveDate must use YYYY-MM-DD.");
        }

        if (ruleSet.Components is null || ruleSet.Components.Count == 0)
        {
            yield return Missing("Components");
        }

        if (ruleSet.ComponentMap is null || ruleSet.ComponentMap.Count == 0)
        {
            yield return Missing("ComponentMap");
        }

        if (string.IsNullOrWhiteSpace(ruleSet.FormulaDescription))
        {
            yield return Missing("FormulaDescription");
        }

        if (ruleSet.ValidationSqlTemplates is null || ruleSet.ValidationSqlTemplates.Count == 0)
        {
            yield return Missing("ValidationSqlTemplates");
        }

        if (string.IsNullOrWhiteSpace(ruleSet.RegulationBasis))
        {
            yield return Missing("RegulationBasis");
        }

        if (ruleSet.ApprovalHistory is null || ruleSet.ApprovalHistory.Count == 0)
        {
            yield return Missing("ApprovalHistory");
        }

        foreach (var component in ruleSet.Components ?? [])
        {
            if (string.IsNullOrWhiteSpace(component.ComponentId)
                || string.IsNullOrWhiteSpace(component.Name)
                || string.IsNullOrWhiteSpace(component.Category)
                || string.IsNullOrWhiteSpace(component.ValuePolicy))
            {
                yield return new SafetyFinding(
                    "NCR_COMPONENT_INCOMPLETE",
                    SafetySeverity.Medium,
                    "NCR component entries must include ComponentId, Name, Category, and ValuePolicy.");
            }
        }

        foreach (var map in ruleSet.ComponentMap ?? [])
        {
            if (string.IsNullOrWhiteSpace(map.ComponentId)
                || string.IsNullOrWhiteSpace(map.SourceName)
                || string.IsNullOrWhiteSpace(map.ColumnName)
                || string.IsNullOrWhiteSpace(map.DataType))
            {
                yield return new SafetyFinding(
                    "NCR_COMPONENT_MAP_INCOMPLETE",
                    SafetySeverity.Medium,
                    "NCR component map entries must include ComponentId, SourceName, ColumnName, and DataType.");
            }
        }
    }

    private static SafetyFinding Missing(string fieldName)
    {
        return new SafetyFinding(
            "NCR_RULESET_REQUIRED_FIELD_MISSING",
            SafetySeverity.Medium,
            $"NCR rule set is missing required field '{fieldName}'.");
    }

    private static bool IsUnsafe(SafetyFinding finding)
    {
        return finding.Severity >= SafetySeverity.Medium;
    }

    private static NcrRuleSetLoadResult CreateFallback(SafetyFinding finding)
    {
        return CreateFallback([finding]);
    }

    private static NcrRuleSetLoadResult CreateFallback(IReadOnlyList<SafetyFinding> findings)
    {
        return new NcrRuleSetLoadResult(NcrRuleSet.SafeFallback(), UsedFallback: true, findings);
    }

    private static bool IsSafeRelativeNcrConfigPath(string relativeRuleSetPath)
    {
        if (string.IsNullOrWhiteSpace(relativeRuleSetPath)
            || Path.IsPathRooted(relativeRuleSetPath)
            || !string.Equals(Path.GetExtension(relativeRuleSetPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = relativeRuleSetPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 3
            && string.Equals(segments[0], "config", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[1], "ncr", StringComparison.OrdinalIgnoreCase)
            && segments.All(segment => segment != "." && segment != "..");
    }

    private static string? ResolveRuleSetPath(string relativeRuleSetPath)
    {
        var appBaseCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeRuleSetPath));
        if (File.Exists(appBaseCandidate))
        {
            return appBaseCandidate;
        }

        var currentDirectoryCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativeRuleSetPath));
        if (File.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        return null;
    }
}
