using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Feedback;

public sealed record FeedbackDraftBodyInput(
    string FeedbackId,
    string TaskId,
    string DraftBody,
    string Kind)
{
    public const string KindSql = "Sql";
    public const string KindVba = "Vba";
    public const string KindGeneral = "General";
}

public sealed record PromotedExample(
    string ExampleId,
    string FeedbackId,
    string TaskId,
    DateTime PromotedAt,
    string UserIdHash,
    string FeedbackCode,
    string ReviewStatus,
    string PromotionMode,
    string? ExampleBody = null,
    string ExampleBodyKind = "None",
    int ExampleBodyLength = 0,
    string? ExampleBodyHash = null);

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
        return PromoteApproved(feedbackEntries, Array.Empty<FeedbackDraftBodyInput>(), RuleLoader.LoadDefault(), promotedAt);
    }

    public ExamplePromotionResult PromoteApproved(
        IEnumerable<FeedbackLogEntry> feedbackEntries,
        IEnumerable<FeedbackDraftBodyInput>? draftBodyInputs,
        SafetyRuleSet? ruleSet = null,
        DateTime? promotedAt = null)
    {
        ArgumentNullException.ThrowIfNull(feedbackEntries);

        var promotedExamples = new List<PromotedExample>();
        var skippedEntries = new List<FeedbackLogEntry>();
        var warnings = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var promotionTime = promotedAt ?? DateTime.UtcNow;
        var bodyInputsByKey = BuildBodyInputLookup(draftBodyInputs, warnings);
        var activeRuleSet = ruleSet ?? RuleLoader.LoadDefault();
        var sqlChecker = new SqlSafetyChecker(activeRuleSet);
        var vbaChecker = new VbaSafetyChecker(activeRuleSet);
        var forbiddenTermScanner = new ForbiddenTermScanner();

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

            bodyInputsByKey.TryGetValue(BuildDedupeKey(entry.FeedbackId, entry.TaskId), out var bodyInput);
            var bodyResolution = ResolveBody(entry, bodyInput, sqlChecker, vbaChecker, forbiddenTermScanner, warnings);
            promotedExamples.Add(new PromotedExample(
                CreateExampleId(entry),
                entry.FeedbackId,
                entry.TaskId,
                promotionTime,
                entry.UserId,
                entry.FeedbackCode,
                entry.ReviewStatus,
                PromotionModeName,
                bodyResolution.Body,
                bodyResolution.Kind,
                bodyResolution.Length,
                bodyResolution.Hash));
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

    private static IReadOnlyDictionary<string, FeedbackDraftBodyInput> BuildBodyInputLookup(
        IEnumerable<FeedbackDraftBodyInput>? draftBodyInputs,
        List<string> warnings)
    {
        if (draftBodyInputs is null)
        {
            return new Dictionary<string, FeedbackDraftBodyInput>(StringComparer.Ordinal);
        }

        var lookup = new Dictionary<string, FeedbackDraftBodyInput>(StringComparer.Ordinal);
        foreach (var input in draftBodyInputs)
        {
            var key = BuildDedupeKey(input.FeedbackId, input.TaskId);
            if (!lookup.TryAdd(key, input))
            {
                warnings.Add($"Duplicate FeedbackDraftBodyInput skipped: {input.FeedbackId}");
            }
        }

        return lookup;
    }

    private static PromotedExampleBodyResolution ResolveBody(
        FeedbackLogEntry entry,
        FeedbackDraftBodyInput? bodyInput,
        SqlSafetyChecker sqlChecker,
        VbaSafetyChecker vbaChecker,
        ForbiddenTermScanner forbiddenTermScanner,
        List<string> warnings)
    {
        if (bodyInput is null || string.IsNullOrWhiteSpace(bodyInput.DraftBody))
        {
            return PromotedExampleBodyResolution.None;
        }

        var kind = NormalizeKind(bodyInput.Kind, bodyInput.DraftBody);
        if (kind is null)
        {
            warnings.Add($"Feedback draft body skipped because its kind is uncertain: {entry.FeedbackId}");
            return PromotedExampleBodyResolution.None;
        }

        var safetyFindings = sqlChecker.Check(bodyInput.DraftBody)
            .Concat(vbaChecker.Check(bodyInput.DraftBody))
            .Where(finding => finding.Severity == SafetySeverity.Blocker)
            .ToList();
        var forbiddenFindings = forbiddenTermScanner.ScanText(bodyInput.DraftBody).ToList();
        if (safetyFindings.Count > 0 || forbiddenFindings.Count > 0)
        {
            var codes = safetyFindings.Concat(forbiddenFindings)
                .Select(finding => finding.Code)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal);
            warnings.Add($"Feedback draft body blocked by ingest gate: {entry.FeedbackId} ({string.Join(", ", codes)})");
            return PromotedExampleBodyResolution.None;
        }

        return new PromotedExampleBodyResolution(
            bodyInput.DraftBody,
            kind,
            bodyInput.DraftBody.Length,
            LogHash.Sha256Hex(bodyInput.DraftBody));
    }

    private static string? NormalizeKind(string kind, string body)
    {
        var trimmedKind = kind.Trim();
        if (IsKind(trimmedKind, FeedbackDraftBodyInput.KindSql, "SQL"))
        {
            return FeedbackDraftBodyInput.KindSql;
        }

        if (IsKind(trimmedKind, FeedbackDraftBodyInput.KindVba, "VBA", "Macro"))
        {
            return FeedbackDraftBodyInput.KindVba;
        }

        if (IsKind(trimmedKind, FeedbackDraftBodyInput.KindGeneral, "Other", "Text", "Draft"))
        {
            return FeedbackDraftBodyInput.KindGeneral;
        }

        if (LooksLikeSql(body))
        {
            return FeedbackDraftBodyInput.KindSql;
        }

        if (LooksLikeVba(body))
        {
            return FeedbackDraftBodyInput.KindVba;
        }

        return null;
    }

    private static bool IsKind(string value, params string[] acceptedValues)
    {
        return acceptedValues.Any(accepted => string.Equals(value, accepted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeSql(string body)
    {
        return ContainsSqlKeyword(body, "SELECT")
            || ContainsSqlKeyword(body, "WITH")
            || ContainsSqlKeyword(body, "INSERT")
            || ContainsSqlKeyword(body, "UPDATE")
            || ContainsSqlKeyword(body, "DELETE")
            || ContainsSqlKeyword(body, "DROP")
            || ContainsSqlKeyword(body, "MERGE");
    }

    private static bool LooksLikeVba(string body)
    {
        return body.Contains("Option Explicit", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Sub ", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Function ", StringComparison.OrdinalIgnoreCase)
            || body.Contains("End Sub", StringComparison.OrdinalIgnoreCase)
            || body.Contains("End Function", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSqlKeyword(string body, string keyword)
    {
        var startIndex = 0;
        while (startIndex < body.Length)
        {
            var index = body.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var beforeIsWord = index > 0 && IsKeywordCharacter(body[index - 1]);
            var afterIndex = index + keyword.Length;
            var afterIsWord = afterIndex < body.Length && IsKeywordCharacter(body[afterIndex]);
            if (!beforeIsWord && !afterIsWord)
            {
                return true;
            }

            startIndex = index + keyword.Length;
        }

        return false;
    }

    private static bool IsKeywordCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static string BuildDedupeKey(string feedbackId, string taskId)
    {
        return $"{feedbackId}|{taskId}";
    }

    private sealed record PromotedExampleBodyResolution(
        string? Body,
        string Kind,
        int Length,
        string? Hash)
    {
        public static PromotedExampleBodyResolution None { get; } = new(null, "None", 0, null);
    }
}
