using System.Text.RegularExpressions;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Feedback;

public sealed class ForbiddenTermScanner
{
    private static readonly string[] ForbiddenTerms =
    [
        "INTERNAL_REGULATION_ORIGINAL",
        "NCR_OFFICIAL_ORIGINAL",
        "REAL_CUSTOMER_DATA",
        "REAL_ACCOUNT_NUMBER",
        "REAL_TABLE_NAME",
        "REAL_COLUMN_NAME",
        "PRODUCTION_CONNECTION_STRING",
        "MODEL_WEIGHT_FILE"
    ];

    private static readonly Regex ResidentRegistrationNumberPattern = new(
        @"(?<!\d)\d{6}-[1-4]\d{6}(?!\d)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public IReadOnlyList<SafetyFinding> ScanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<SafetyFinding>();
        }

        var findings = new List<SafetyFinding>();
        foreach (var term in ForbiddenTerms)
        {
            var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                findings.Add(new SafetyFinding(
                    "FEEDBACK_FORBIDDEN_TERM",
                    SafetySeverity.Blocker,
                    "승인 Example 본문에 반입 금지 토큰이 포함되어 저장하지 않았습니다.",
                    term,
                    index));
            }
        }

        var rrnMatch = ResidentRegistrationNumberPattern.Match(text);
        if (rrnMatch.Success)
        {
            findings.Add(new SafetyFinding(
                "FEEDBACK_PII_PATTERN",
                SafetySeverity.Blocker,
                "승인 Example 본문에 PII 유사 패턴이 포함되어 저장하지 않았습니다.",
                "RRN_PATTERN",
                rrnMatch.Index));
        }

        return findings;
    }
}
