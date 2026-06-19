using System.Text.RegularExpressions;

namespace RiskManagementAI.Core.Safety;

public sealed class SqlSafetyChecker
{
    private readonly SafetyRuleSet ruleSet;

    public SqlSafetyChecker()
        : this(RuleLoader.LoadDefault())
    {
    }

    public SqlSafetyChecker(SafetyRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
    }

    public IEnumerable<SafetyFinding> Check(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            yield return new SafetyFinding("SQL_EMPTY", SafetySeverity.Low, "SQL 텍스트가 비어 있습니다.");
            yield break;
        }

        if (ruleSet.UsedFallback)
        {
            yield return CreateFallbackFinding(ruleSet);
        }

        foreach (var rule in ruleSet.SqlDenyRules.Concat(ruleSet.SqlWarnRules))
        {
            var match = Regex.Match(sqlText, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(rule.Code, rule.Severity, rule.Message, match.Value, match.Index);
            }
        }

        if (!Regex.IsMatch(sqlText, @"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return new SafetyFinding("SQL_NO_SELECT", SafetySeverity.Medium, "조회 SQL로 보이지 않습니다. 초기 MVP는 SELECT 계열만 권장합니다.");
        }
    }

    private static SafetyFinding CreateFallbackFinding(SafetyRuleSet ruleSet)
    {
        var detail = ruleSet.LoadWarnings.Count > 0 ? $" 사유: {ruleSet.LoadWarnings[0]}" : string.Empty;
        return new SafetyFinding("RULESET_FALLBACK", SafetySeverity.Info, $"룰 파일을 로드하지 못해 내장 기본 룰셋을 사용했습니다.{detail}");
    }
}
