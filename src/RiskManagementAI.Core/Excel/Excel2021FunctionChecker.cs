using System.Text.RegularExpressions;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Excel;

public sealed class Excel2021FunctionChecker
{
    private static readonly string[] BlockedFunctions =
    [
        "VSTACK", "HSTACK", "TOCOL", "TOROW", "TAKE", "DROP", "CHOOSECOLS",
        "TEXTSPLIT", "TEXTBEFORE", "TEXTAFTER", "GROUPBY", "PIVOTBY",
        "MAP", "REDUCE", "BYROW", "BYCOL", "REGEXTEST", "REGEXEXTRACT", "REGEXREPLACE"
    ];

    public IEnumerable<SafetyFinding> CheckFormula(string formulaOrText)
    {
        if (string.IsNullOrWhiteSpace(formulaOrText))
        {
            yield break;
        }

        foreach (var functionName in BlockedFunctions)
        {
            var pattern = $@"\b{Regex.Escape(functionName)}\s*\(";
            var match = Regex.Match(formulaOrText, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(
                    "EXCEL_365_FUNCTION",
                    SafetySeverity.High,
                    $"{functionName} 함수는 Excel 2021 기본 호환 범위에서 제외됩니다. 보조열, INDEX/MATCH, PivotTable, VBA 등으로 대체하세요.",
                    match.Value,
                    match.Index);
            }
        }
    }
}
