namespace RiskManagementAI.Core.Logging;

public sealed record FeedbackLogEntry(
    string FeedbackId,
    string TaskId,
    DateTime CreatedAt,
    string UserId,
    string FeedbackCode,
    string ReviewStatus
);
