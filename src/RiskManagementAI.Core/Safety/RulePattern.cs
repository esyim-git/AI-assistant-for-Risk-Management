namespace RiskManagementAI.Core.Safety;

public sealed record RulePattern(
    string Code,
    string Pattern,
    SafetySeverity Severity,
    string Message
);
