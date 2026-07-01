using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Feedback;

public sealed record PromotedExampleSearchResult(
    PromotedExample Example,
    int Score);

public sealed record PromotedExampleSearchResponse(
    string Query,
    IReadOnlyList<PromotedExampleSearchResult> Results,
    bool AuditLogWritten,
    IReadOnlyList<string> Warnings);

public sealed class PromotedExampleRetriever
{
    private readonly PromotedExampleStore store;
    private readonly TaskLogWriter? auditLogWriter;
    private readonly string? auditRuleVersion;

    public PromotedExampleRetriever(
        PromotedExampleStore store,
        TaskLogWriter? auditLogWriter = null,
        string? auditRuleVersion = null)
    {
        this.store = store;
        this.auditLogWriter = auditLogWriter;
        this.auditRuleVersion = auditRuleVersion;
    }

    public PromotedExampleSearchResponse Search(string query, string userId = "anonymous", int maxResults = 5)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var warnings = new List<string>();
        var results = string.IsNullOrWhiteSpace(normalizedQuery)
            ? []
            : store.ReadAll()
                .Select(example => new PromotedExampleSearchResult(example, Score(example, normalizedQuery)))
                .Where(result => result.Score > 0)
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Example.ExampleId, StringComparer.Ordinal)
                .Take(Math.Max(1, maxResults))
                .ToList();

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            warnings.Add("PromotedExample retrieval query is empty.");
        }
        else if (results.Count == 0)
        {
            warnings.Add("PromotedExample retrieval returned no matches.");
        }

        var auditLogWritten = TryAppendAuditLog(normalizedQuery, userId, results, warnings);
        return new PromotedExampleSearchResponse(normalizedQuery, results, auditLogWritten, warnings);
    }

    private bool TryAppendAuditLog(
        string query,
        string userId,
        IReadOnlyList<PromotedExampleSearchResult> results,
        List<string> warnings)
    {
        if (auditLogWriter is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(auditRuleVersion))
        {
            warnings.Add("PromotedExample retrieval audit log was not written because rule version is missing.");
            return false;
        }

        try
        {
            auditLogWriter.Append(new TaskLogEntry(
                $"task-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                LogHash.Sha256Hex(string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId),
                "PromotedExampleRetrieval",
                nameof(PromotedExampleRetriever),
                LogHash.Sha256Hex(query),
                LogHash.Sha256Hex(BuildOutputPayload(results)),
                "PASS",
                auditRuleVersion));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            warnings.Add($"PromotedExample retrieval audit log write failed: {ex.Message}");
            return false;
        }
    }

    private static string BuildOutputPayload(IReadOnlyList<PromotedExampleSearchResult> results)
    {
        return string.Join(
            '\n',
            results.Select(result => $"{result.Example.ExampleId}|{result.Score}"));
    }

    private static int Score(PromotedExample example, string query)
    {
        var score = 0;
        score += Contains(example.ExampleBody, query) ? 10 : 0;
        score += Contains(example.FeedbackId, query) ? 6 : 0;
        score += Contains(example.TaskId, query) ? 6 : 0;
        score += Contains(example.FeedbackCode, query) ? 4 : 0;
        score += Contains(example.ReviewStatus, query) ? 4 : 0;
        score += Contains(example.PromotionMode, query) ? 3 : 0;
        score += Contains(example.ExampleBodyKind, query) ? 2 : 0;

        foreach (var term in SplitTerms(query))
        {
            if (term.Length < 2)
            {
                continue;
            }

            score += Contains(example.ExampleBody, term) ? 3 : 0;
            score += Contains(example.FeedbackId, term) ? 2 : 0;
            score += Contains(example.TaskId, term) ? 2 : 0;
            score += Contains(example.FeedbackCode, term) ? 1 : 0;
            score += Contains(example.ReviewStatus, term) ? 1 : 0;
        }

        return score;
    }

    private static bool Contains(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SplitTerms(string query)
    {
        return query.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '/', '\\', '|', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
