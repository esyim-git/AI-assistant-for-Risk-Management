using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Feedback;

public sealed record PromotedExample(
    string ExampleId,
    string FeedbackId,
    string TaskId,
    DateTime PromotedAt,
    string UserIdHash,
    string FeedbackCode,
    string ReviewStatus,
    string PromotionMode);

public sealed record ExamplePromotionResult(
    IReadOnlyList<PromotedExample> PromotedExamples,
    IReadOnlyList<FeedbackLogEntry> SkippedEntries,
    IReadOnlyList<string> Warnings);

public sealed class ExamplePromotion
{
    public const string PromotionModeName = "ExampleCurationOnly";

    public ExamplePromotionResult PromoteApproved(
        IEnumerable<FeedbackLogEntry> feedbackEntries,
        DateTime? promotedAt = null)
    {
        ArgumentNullException.ThrowIfNull(feedbackEntries);

        var promotedExamples = new List<PromotedExample>();
        var skippedEntries = new List<FeedbackLogEntry>();
        var warnings = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var promotionTime = promotedAt ?? DateTime.UtcNow;

        foreach (var entry in feedbackEntries)
        {
            if (!LogHash.IsSha256Hex(entry.UserId))
            {
                skippedEntries.Add(entry);
                warnings.Add($"Feedback entry skipped because UserId is not a SHA-256 hash: {entry.FeedbackId}");
                continue;
            }

            if (!IsApproved(entry))
            {
                skippedEntries.Add(entry);
                continue;
            }

            var dedupeKey = $"{entry.FeedbackId}|{entry.TaskId}";
            if (!seen.Add(dedupeKey))
            {
                skippedEntries.Add(entry);
                warnings.Add($"Duplicate approved feedback skipped: {entry.FeedbackId}");
                continue;
            }

            promotedExamples.Add(new PromotedExample(
                CreateExampleId(entry),
                entry.FeedbackId,
                entry.TaskId,
                promotionTime,
                entry.UserId,
                entry.FeedbackCode,
                entry.ReviewStatus,
                PromotionModeName));
        }

        return new ExamplePromotionResult(promotedExamples, skippedEntries, warnings);
    }

    private static bool IsApproved(FeedbackLogEntry entry)
    {
        return string.Equals(entry.FeedbackCode, "APPROVED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.FeedbackCode, "ACCEPTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.ReviewStatus, "APPROVED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.ReviewStatus, "REVIEWER_APPROVED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.ReviewStatus, "ReviewerApproved", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateExampleId(FeedbackLogEntry entry)
    {
        return $"example-{LogHash.Sha256Hex($"{entry.FeedbackId}|{entry.TaskId}")[..12]}";
    }
}
