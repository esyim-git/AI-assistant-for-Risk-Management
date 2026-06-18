namespace RiskManagementAI.Core.Logging;

public sealed record TaskLogEntry(
    string TaskId,
    DateTime CreatedAt,
    string UserId,
    string TaskType,
    string ToolType,
    string RequestHash,
    string? OutputHash,
    string SafetyResult,
    string RuleVersion
);
