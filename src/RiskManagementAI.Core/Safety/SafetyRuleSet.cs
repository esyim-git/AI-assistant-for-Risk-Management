namespace RiskManagementAI.Core.Safety;

public sealed record SafetyRuleSet(
    IReadOnlyList<RulePattern> SqlDenyRules,
    IReadOnlyList<RulePattern> SqlWarnRules,
    IReadOnlyList<RulePattern> VbaDenyRules,
    IReadOnlyList<RulePattern> VbaWarnRules,
    IReadOnlyList<RulePattern> VbaRequiredPresentRules,
    IReadOnlyList<string> ExcelBlockedFunctions,
    IReadOnlyList<string> ExcelPreferredFunctions,
    IReadOnlyList<string> ExcelCompletionAllowFunctions,
    string RuleVersion,
    bool UsedFallback,
    IReadOnlyList<string> LoadWarnings
);
