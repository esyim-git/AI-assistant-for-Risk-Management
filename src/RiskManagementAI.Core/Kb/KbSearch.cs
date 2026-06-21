using System.Globalization;
using System.Text;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Kb;

public interface IClock
{
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}

public sealed record KbSearchResult(
    string SourceId,
    string Category,
    string Title,
    string SourceOrg,
    string SourceType,
    string Status,
    string Note,
    string Source,
    string Version,
    string EffectiveDate,
    string RepealDate,
    string FileHash,
    string LoadedDate,
    string ApprovalStatus,
    string SupersededBy,
    string LicenseStatus,
    string Clause,
    KbDisclosure Disclosure,
    string DisclosureReason,
    int Score);

public sealed record KbSearchResponse(
    string Query,
    string DraftAnswer,
    IReadOnlyList<KbSearchResult> Results,
    bool AuditLogWritten,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SafetyFinding> Findings);

public sealed class KbSearch
{
    private const string ReviewDraftNotice = "검토용 초안";

    private readonly RegulationCatalog catalog;
    private readonly KbIndex index;
    private readonly TaskLogWriter? auditLogWriter;
    private readonly string? auditRuleVersion;
    private readonly IClock clock;

    public KbSearch(
        RegulationCatalog catalog,
        TaskLogWriter? auditLogWriter = null,
        string? auditRuleVersion = null,
        IClock? clock = null)
    {
        this.catalog = catalog;
        index = KbIndex.Build(catalog.Entries);
        this.auditLogWriter = auditLogWriter;
        this.auditRuleVersion = auditRuleVersion;
        this.clock = clock ?? new SystemClock();
    }

    public KbSearchResponse Search(string query, string userId = "anonymous", int maxResults = 5, string? asOfDate = null)
    {
        var normalizedQuery = query.Trim();
        var warnings = new List<string>(catalog.Warnings);
        var findings = new List<SafetyFinding>();
        var searchDate = NormalizeSearchDate(asOfDate, warnings);
        var results = string.IsNullOrWhiteSpace(normalizedQuery)
            ? []
            : index.FindCandidates(normalizedQuery)
                .Select(entry => (Entry: entry, Score: Score(entry, normalizedQuery)))
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Entry.SourceId, StringComparer.Ordinal)
                .Take(Math.Max(1, maxResults))
                .Select(item => ToSearchResult(item.Entry, item.Score, findings))
                .ToList();

        AddCitationMetadataWarnings(results, warnings);

        if (results.Count == 0)
        {
            warnings.Add("공개 catalog에서 일치 항목을 찾지 못했습니다. 운영 KB 적재 전 최신 시행일과 문서오너 승인을 확인해야 합니다.");
        }

        var draftAnswer = BuildDraftAnswer(normalizedQuery, searchDate, results, catalog.SourcePath, warnings);
        var auditLogWritten = TryAppendAuditLog(normalizedQuery, userId, draftAnswer, warnings);
        return new KbSearchResponse(normalizedQuery, draftAnswer, results, auditLogWritten, warnings, findings);
    }

    private string NormalizeSearchDate(string? asOfDate, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(asOfDate))
        {
            return clock.Today.ToString("yyyy-MM-dd");
        }

        var trimmed = asOfDate.Trim();
        if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate.ToString("yyyy-MM-dd");
        }

        var fallbackDate = clock.Today.ToString("yyyy-MM-dd");
        warnings.Add($"검색 기준일 입력이 yyyy-MM-dd 형식이 아니어서 {fallbackDate}로 대체했습니다.");
        return fallbackDate;
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
        string searchDate,
        IReadOnlyList<KbSearchResult> results,
        string catalogSourcePath,
        IReadOnlyList<string> warnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ReviewDraftNotice}: 공개 regulation catalog 기준 검색 결과입니다. 공식 해석이 아니므로 검토 필요입니다. 내부규정 원문과 NCR 공식본 원문은 포함하지 않습니다.");
        sb.AppendLine($"질의: {(string.IsNullOrWhiteSpace(query) ? "(empty)" : query)}");
        sb.AppendLine($"검색 기준일: {searchDate}");
        sb.AppendLine($"출처(Catalog 파일): {catalogSourcePath}");
        sb.AppendLine("검토 필요: 운영 KB 적재 전 최신 시행일, 조항 단위 원문, 문서오너 승인을 확인해야 합니다.");

        if (results.Count == 0)
        {
            sb.AppendLine("검색 결과: 0건");
        }
        else
        {
            sb.AppendLine("검색 결과:");
            foreach (var result in results)
            {
                sb.AppendLine($"- [{result.SourceId}] {result.Title} (버전: {DisplayValue(result.Version)}, 시행일: {DisplayValue(result.EffectiveDate)})");
                sb.AppendLine($"  문서명: {DisplayValue(result.Title)}");
                sb.AppendLine($"  문서ID: {DisplayValue(result.SourceId)}");
                sb.AppendLine($"  조항: {DisplayValue(result.Clause)}");
                sb.AppendLine($"  출처: {DisplayValue(result.Source)}");
                sb.AppendLine($"  출처기관: {DisplayValue(result.SourceOrg)} / 유형: {DisplayValue(result.SourceType)}");
                sb.AppendLine($"  상태: {DisplayValue(result.Status)} / 라이선스: {DisplayValue(result.LicenseStatus)} / 분류: {DisplayValue(result.Category)}");
                sb.AppendLine($"  노출등급: {result.Disclosure} / 사유: {result.DisclosureReason}");
                sb.AppendLine($"  폐기일: {DisplayValue(result.RepealDate)} / 적재일: {DisplayValue(result.LoadedDate)} / 대체문서: {DisplayValue(result.SupersededBy)}");
                sb.AppendLine($"  비고: {result.Note}");
            }
        }

        foreach (var warning in warnings)
        {
            sb.AppendLine($"주의: {warning}");
        }

        return sb.ToString();
    }

    private static string DisplayValue(string value)
    {
        if (IsPlaceholderMetadata(value))
        {
            return "(확인 필요)";
        }

        return string.IsNullOrWhiteSpace(value) ? "(미기재)" : value;
    }

    private static void AddCitationMetadataWarnings(IReadOnlyList<KbSearchResult> results, List<string> warnings)
    {
        foreach (var result in results)
        {
            AddPlaceholderWarning(result.SourceId, "version", result.Version, warnings);
            AddPlaceholderWarning(result.SourceId, "effective_date", result.EffectiveDate, warnings);
            AddPlaceholderWarning(result.SourceId, "repeal_date", result.RepealDate, warnings);
            AddPlaceholderWarning(result.SourceId, "loaded_date", result.LoadedDate, warnings);
            AddPlaceholderWarning(result.SourceId, "license_status", result.LicenseStatus, warnings);
        }
    }

    private static void AddPlaceholderWarning(string sourceId, string fieldName, string value, List<string> warnings)
    {
        if (IsPlaceholderMetadata(value))
        {
            warnings.Add($"인용 metadata 확인 필요: source_id={sourceId}, field={fieldName}.");
        }
    }

    private static bool IsPlaceholderMetadata(string value)
    {
        var normalized = value.Trim();
        return normalized.StartsWith("CONFIRM_", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("NOT_LOADED", StringComparison.OrdinalIgnoreCase);
    }

    private static KbSearchResult ToSearchResult(RegulationCatalogEntry entry, int score, List<SafetyFinding> findings)
    {
        var accessDecision = KbAccessPolicy.Evaluate(entry);
        findings.AddRange(accessDecision.Findings);
        return new KbSearchResult(
            entry.SourceId,
            entry.Category,
            entry.Title,
            entry.SourceOrg,
            entry.SourceType,
            entry.Status,
            entry.Note,
            entry.Source,
            entry.Version,
            entry.EffectiveDate,
            entry.RepealDate,
            entry.FileHash,
            entry.LoadedDate,
            entry.ApprovalStatus,
            entry.SupersededBy,
            entry.LicenseStatus,
            "catalog 단위 - 조항별 원문은 Prod 권한통제 KB에서 확인",
            accessDecision.Disclosure,
            accessDecision.Reason,
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
