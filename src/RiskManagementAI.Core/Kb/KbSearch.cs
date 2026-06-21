using System.Text;
using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Kb;

public sealed record KbSearchResult(
    string SourceId,
    string Category,
    string Title,
    string SourceOrg,
    string SourceType,
    string Status,
    string Note,
    int Score);

public sealed record KbSearchResponse(
    string Query,
    string DraftAnswer,
    IReadOnlyList<KbSearchResult> Results,
    bool AuditLogWritten,
    IReadOnlyList<string> Warnings);

public sealed class KbSearch
{
    private const string ReviewDraftNotice = "검토용 초안";

    private readonly RegulationCatalog catalog;
    private readonly KbIndex index;
    private readonly TaskLogWriter? auditLogWriter;
    private readonly string? auditRuleVersion;

    public KbSearch(
        RegulationCatalog catalog,
        TaskLogWriter? auditLogWriter = null,
        string? auditRuleVersion = null)
    {
        this.catalog = catalog;
        index = KbIndex.Build(catalog.Entries);
        this.auditLogWriter = auditLogWriter;
        this.auditRuleVersion = auditRuleVersion;
    }

    public KbSearchResponse Search(string query, string userId = "anonymous", int maxResults = 5)
    {
        var normalizedQuery = query.Trim();
        var warnings = new List<string>();
        var results = string.IsNullOrWhiteSpace(normalizedQuery)
            ? []
            : FindCandidatesWithFallback(normalizedQuery)
                .Select(entry => (Entry: entry, Score: Score(entry, normalizedQuery)))
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Entry.SourceId, StringComparer.Ordinal)
                .Take(Math.Max(1, maxResults))
                .Select(item => ToSearchResult(item.Entry, item.Score))
                .ToList();

        if (results.Count == 0)
        {
            warnings.Add("공개 catalog에서 일치 항목을 찾지 못했습니다. 운영 KB 적재 전 최신 시행일과 문서오너 승인을 확인해야 합니다.");
        }

        var draftAnswer = BuildDraftAnswer(normalizedQuery, results, catalog.SourcePath, warnings);
        var auditLogWritten = TryAppendAuditLog(normalizedQuery, userId, draftAnswer, warnings);
        return new KbSearchResponse(normalizedQuery, draftAnswer, results, auditLogWritten, warnings);
    }

    private IReadOnlyList<RegulationCatalogEntry> FindCandidatesWithFallback(string normalizedQuery)
    {
        var candidates = new SortedDictionary<string, RegulationCatalogEntry>(StringComparer.Ordinal);
        foreach (var entry in index.FindCandidates(normalizedQuery))
        {
            candidates[entry.SourceId] = entry;
        }

        foreach (var entry in catalog.Entries)
        {
            if (!candidates.ContainsKey(entry.SourceId) && Score(entry, normalizedQuery) > 0)
            {
                candidates[entry.SourceId] = entry;
            }
        }

        return candidates.Values.ToList();
    }

    private bool TryAppendAuditLog(string query, string userId, string draftAnswer, List<string> warnings)
    {
        if (auditLogWriter is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(auditRuleVersion))
        {
            warnings.Add("KB 검색 audit log를 쓰지 않았습니다. auditRuleVersion이 없습니다.");
            return false;
        }

        try
        {
            auditLogWriter.Append(new TaskLogEntry(
                $"task-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                LogHash.Sha256Hex(string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId),
                "KbCatalogSearch",
                nameof(KbSearch),
                LogHash.Sha256Hex(query),
                LogHash.Sha256Hex(draftAnswer),
                "PASS",
                auditRuleVersion));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            warnings.Add($"KB 검색 audit log 쓰기 실패: {ex.Message}");
            return false;
        }
    }

    private static string BuildDraftAnswer(
        string query,
        IReadOnlyList<KbSearchResult> results,
        string catalogSourcePath,
        IReadOnlyList<string> warnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ReviewDraftNotice}: 공개 regulation catalog 기준 검색 결과입니다. 내부규정 원문과 NCR 공식본 원문은 포함하지 않습니다.");
        sb.AppendLine($"질의: {(string.IsNullOrWhiteSpace(query) ? "(empty)" : query)}");
        sb.AppendLine($"출처: {catalogSourcePath}");
        sb.AppendLine("버전/시행일: Dev/Test catalog metadata only. 운영 KB 적재 전 최신 시행일과 문서오너 승인을 확인해야 합니다.");

        if (results.Count == 0)
        {
            sb.AppendLine("검색 결과: 0건");
        }
        else
        {
            sb.AppendLine("검색 결과:");
            foreach (var result in results)
            {
                sb.AppendLine($"- [{result.SourceId}] {result.Title}");
                sb.AppendLine($"  출처: {result.SourceOrg} / {result.SourceType}");
                sb.AppendLine($"  상태: {result.Status} / 분류: {result.Category}");
                sb.AppendLine($"  비고: {result.Note}");
            }
        }

        foreach (var warning in warnings)
        {
            sb.AppendLine($"주의: {warning}");
        }

        return sb.ToString();
    }

    private static KbSearchResult ToSearchResult(RegulationCatalogEntry entry, int score)
    {
        return new KbSearchResult(
            entry.SourceId,
            entry.Category,
            entry.Title,
            entry.SourceOrg,
            entry.SourceType,
            entry.Status,
            entry.Note,
            score);
    }

    private static int Score(RegulationCatalogEntry entry, string query)
    {
        var score = 0;
        score += Contains(entry.SourceId, query) ? 10 : 0;
        score += Contains(entry.Title, query) ? 8 : 0;
        score += Contains(entry.Category, query) ? 5 : 0;
        score += Contains(entry.SourceOrg, query) ? 3 : 0;
        score += Contains(entry.SourceType, query) ? 3 : 0;
        score += Contains(entry.Status, query) ? 2 : 0;
        score += Contains(entry.Note, query) ? 1 : 0;

        foreach (var term in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (term.Length < 2)
            {
                continue;
            }

            score += Contains(entry.Title, term) ? 2 : 0;
            score += Contains(entry.Note, term) ? 1 : 0;
        }

        return score;
    }

    private static bool Contains(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
