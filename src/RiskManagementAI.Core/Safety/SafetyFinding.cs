namespace RiskManagementAI.Core.Safety;

public sealed record SafetyFinding(
    string Code,
    SafetySeverity Severity,
    string Message,
    string? MatchedText = null,
    int? Position = null
);
