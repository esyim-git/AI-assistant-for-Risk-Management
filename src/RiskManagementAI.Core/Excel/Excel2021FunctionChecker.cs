using System.Text.RegularExpressions;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Excel;

public sealed class Excel2021FunctionChecker
{
    private readonly SafetyRuleSet ruleSet;

    public Excel2021FunctionChecker()
        : this(RuleLoader.LoadDefault())
    {
    }

    public Excel2021FunctionChecker(SafetyRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
    }

    public IEnumerable<SafetyFinding> CheckFormula(string formulaOrText)
    {
        if (string.IsNullOrWhiteSpace(formulaOrText))
        {
            yield break;
        }

        if (ruleSet.UsedFallback)
        {
            yield return CreateFallbackFinding(ruleSet);
        }

        foreach (var functionName in ruleSet.ExcelBlockedFunctions)
        {
            var pattern = $@"\b{Regex.Escape(functionName)}\s*\(";
            var match = Regex.Match(formulaOrText, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(
                    "EXCEL_365_FUNCTION",
                    SafetySeverity.High,
                    $"{functionName} 함수는 Excel 2021 기본 호환 범위에서 제외됩니다. 권장 대체 범위: {BuildPreferredFunctionText(ruleSet)}.",
                    match.Value,
                    match.Index);
            }
        }
    }

    private static string BuildPreferredFunctionText(SafetyRuleSet ruleSet)
    {
        return ruleSet.ExcelPreferredFunctions.Count == 0
            ? "보조열, INDEX/MATCH, PivotTable, VBA"
            : string.Join(", ", ruleSet.ExcelPreferredFunctions);
    }

    private static SafetyFinding CreateFallbackFinding(SafetyRuleSet ruleSet)
    {
        var detail = ruleSet.LoadWarnings.Count > 0 ? $" 사유: {ruleSet.LoadWarnings[0]}" : string.Empty;
        return new SafetyFinding("RULESET_FALLBACK", SafetySeverity.Info, $"룰 파일을 로드하지 못해 내장 기본 룰셋을 사용했습니다.{detail}");
    }
}
