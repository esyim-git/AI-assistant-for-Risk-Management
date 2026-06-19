using System.Text.RegularExpressions;

namespace RiskManagementAI.Core.Safety;

public sealed class VbaSafetyChecker
{
    private readonly SafetyRuleSet ruleSet;

    public VbaSafetyChecker()
        : this(RuleLoader.LoadDefault())
    {
    }

    public VbaSafetyChecker(SafetyRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
    }

    public IEnumerable<SafetyFinding> Check(string vbaText)
    {
        if (string.IsNullOrWhiteSpace(vbaText))
        {
            yield return new SafetyFinding("VBA_EMPTY", SafetySeverity.Low, "VBA 텍스트가 비어 있습니다.");
            yield break;
        }

        if (ruleSet.UsedFallback)
        {
            yield return CreateFallbackFinding(ruleSet);
        }

        foreach (var rule in ruleSet.VbaDenyRules.Concat(ruleSet.VbaWarnRules))
        {
            var match = Regex.Match(vbaText, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(rule.Code, rule.Severity, rule.Message, match.Value, match.Index);
            }
        }

        foreach (var rule in ruleSet.VbaRequiredPresentRules)
        {
            if (!Regex.IsMatch(vbaText, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
            {
                yield return new SafetyFinding(rule.Code, rule.Severity, rule.Message);
            }
        }
    }

    private static SafetyFinding CreateFallbackFinding(SafetyRuleSet ruleSet)
    {
        var detail = ruleSet.LoadWarnings.Count > 0 ? $" 사유: {ruleSet.LoadWarnings[0]}" : string.Empty;
        return new SafetyFinding("RULESET_FALLBACK", SafetySeverity.Info, $"룰 파일을 로드하지 못해 내장 기본 룰셋을 사용했습니다.{detail}");
    }
}
