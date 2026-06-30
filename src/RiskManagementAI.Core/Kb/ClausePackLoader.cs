using System.Globalization;
using System.Text;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Kb;

public static class ClausePackLoader
{
    public const string DefaultSamplePath = "kb/clause_pack_sample/public_clause_pack_sample.csv";

    private static readonly string[] RequiredColumns =
    [
        "clause_ref",
        "clause_body",
        "source_id",
        "effective_date",
        "repeal_date",
        "pack_version"
    ];

    private static readonly string[] RequiredValueColumns =
    [
        "clause_ref",
        "clause_body",
        "source_id",
        "effective_date",
        "pack_version"
    ];

    public static ClausePackLoadResult LoadDefault()
    {
        return Load(DefaultSamplePath);
    }

    public static ClausePackLoadResult Load(string relativeClausePackPath)
    {
        string? resolvedPath;
        try
        {
            if (!IsSafeRelativeClausePackPath(relativeClausePackPath))
            {
                return CreateFallback(CreatePathRejectedFinding(
                    relativeClausePackPath,
                    "Only kb/**/*.csv relative paths are allowed."));
            }

            resolvedPath = ResolveClausePackPath(relativeClausePackPath);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
        {
            return CreateFallback(CreatePathRejectedFinding(
                relativeClausePackPath,
                $"Path could not be normalized safely: {ex.Message}"));
        }

        if (resolvedPath is null)
        {
            return CreateFallback(new SafetyFinding(
                "KB_CLAUSE_PACK_MISSING",
                SafetySeverity.Medium,
                $"Clause pack file '{relativeClausePackPath}' was not found. Catalog-only fallback is used."));
        }

        try
        {
            var table = CsvReader.Read(resolvedPath);
            var missingColumns = RequiredColumns
                .Where(column => !table.Columns.Contains(column, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (missingColumns.Count > 0)
            {
                return CreateFallback(new SafetyFinding(
                    "KB_CLAUSE_PACK_HEADER_MISSING",
                    SafetySeverity.Medium,
                    $"Clause pack file '{relativeClausePackPath}' is missing required columns: {string.Join(", ", missingColumns)}."));
            }

            var findings = new List<SafetyFinding>();
            var clausesByNaturalKey = new SortedDictionary<string, RegulationClause>(StringComparer.Ordinal);
            foreach (var row in table.Rows)
            {
                if (row.RawFieldCount > table.Columns.Count)
                {
                    findings.Add(new SafetyFinding(
                        "KB_CLAUSE_PACK_ROW_SKIPPED",
                        SafetySeverity.Medium,
                        $"Line {row.LineNumber}: Clause pack row has too many fields and was skipped."));
                    continue;
                }

                var missingValueColumn = RequiredValueColumns.FirstOrDefault(column => string.IsNullOrWhiteSpace(row.GetValue(column)));
                if (missingValueColumn is not null)
                {
                    findings.Add(new SafetyFinding(
                        "KB_CLAUSE_PACK_ROW_SKIPPED",
                        SafetySeverity.Medium,
                        $"Line {row.LineNumber}: Clause pack required value '{missingValueColumn}' is empty and was skipped."));
                    continue;
                }

                var clause = CreateClause(row);
                var naturalKey = BuildNaturalKey(clause.SourceId, clause.ClauseRef, clause.PackVersion);
                if (clausesByNaturalKey.TryGetValue(naturalKey, out var existing))
                {
                    if (!string.Equals(existing.SourceTextHash, clause.SourceTextHash, StringComparison.Ordinal))
                    {
                        findings.Add(new SafetyFinding(
                            "KB_CLAUSE_PACK_DUPLICATE_NATURAL_KEY",
                            SafetySeverity.Medium,
                            $"Line {row.LineNumber}: 동일 SourceId/ClauseRef/PackVersion에 상이한 ClauseText가 있어 후속 행을 거부했습니다. source_id={clause.SourceId}, clause_ref={clause.ClauseRef}, pack_version={clause.PackVersion}."));
                    }

                    continue;
                }

                clausesByNaturalKey[naturalKey] = clause;
            }

            if (findings.Count > 0)
            {
                return CreateFallback(findings);
            }

            var clauses = clausesByNaturalKey.Values
                .OrderBy(clause => clause.SourceId, StringComparer.Ordinal)
                .ThenBy(clause => clause.ClauseRef, StringComparer.Ordinal)
                .ThenBy(clause => clause.PackVersion, StringComparer.Ordinal)
                .ThenBy(clause => clause.ChunkId, StringComparer.Ordinal)
                .ToList();
            return new ClausePackLoadResult(clauses, UsedFallback: clauses.Count == 0, findings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            return CreateFallback(new SafetyFinding(
                "KB_CLAUSE_PACK_LOAD_FAILED",
                SafetySeverity.Medium,
                $"Clause pack file '{relativeClausePackPath}' could not be loaded safely: {ex.Message}"));
        }
    }

    private static RegulationClause CreateClause(CsvRow row)
    {
        var sourceId = row.GetValue("source_id");
        var clauseRef = row.GetValue("clause_ref");
        var clauseText = row.GetValue("clause_body");
        var effectiveDate = row.GetValue("effective_date");
        var repealDate = row.GetValue("repeal_date");
        var packVersion = row.GetValue("pack_version");
        var sourceTextHash = LogHash.Sha256Hex(clauseText);
        var chunkIdSeed = BuildLengthPrefixedKey(sourceId, clauseRef, packVersion, sourceTextHash);
        var chunkId = $"clause-{LogHash.Sha256Hex(chunkIdSeed)[..12]}";
        return new RegulationClause(
            chunkId,
            sourceId,
            clauseRef,
            clauseText,
            effectiveDate,
            repealDate,
            packVersion,
            sourceTextHash);
    }

    private static string BuildNaturalKey(string sourceId, string clauseRef, string packVersion)
    {
        return BuildLengthPrefixedKey(sourceId, clauseRef, packVersion);
    }

    private static string BuildLengthPrefixedKey(params string[] components)
    {
        var builder = new StringBuilder();
        foreach (var component in components)
        {
            builder.Append(component.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(component);
        }

        return builder.ToString();
    }

    private static SafetyFinding CreatePathRejectedFinding(string relativeClausePackPath, string reason)
    {
        return new SafetyFinding(
            "KB_CLAUSE_PACK_PATH_REJECTED",
            SafetySeverity.High,
            $"Clause pack path '{relativeClausePackPath}' is not allowed. {reason}");
    }

    private static ClausePackLoadResult CreateFallback(SafetyFinding finding)
    {
        return CreateFallback([finding]);
    }

    private static ClausePackLoadResult CreateFallback(IReadOnlyList<SafetyFinding> findings)
    {
        return new ClausePackLoadResult([], UsedFallback: true, findings);
    }

    private static bool IsSafeRelativeClausePackPath(string relativeClausePackPath)
    {
        if (string.IsNullOrWhiteSpace(relativeClausePackPath)
            || Path.IsPathRooted(relativeClausePackPath)
            || !string.Equals(Path.GetExtension(relativeClausePackPath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = relativeClausePackPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2
            && string.Equals(segments[0], "kb", StringComparison.OrdinalIgnoreCase)
            && segments.All(segment => segment != "." && segment != "..");
    }

    private static string? ResolveClausePackPath(string relativeClausePackPath)
    {
        var appBaseCandidate = ResolveClausePackPathUnderRoot(AppContext.BaseDirectory, relativeClausePackPath);
        if (appBaseCandidate is not null)
        {
            return appBaseCandidate;
        }

        var currentDirectoryCandidate = ResolveClausePackPathUnderRoot(Environment.CurrentDirectory, relativeClausePackPath);
        if (currentDirectoryCandidate is not null)
        {
            return currentDirectoryCandidate;
        }

        return null;
    }

    private static string? ResolveClausePackPathUnderRoot(string root, string relativeClausePackPath)
    {
        var rootPath = EnsureTrailingSeparator(Path.GetFullPath(root));
        var resolvedPath = Path.GetFullPath(Path.Combine(rootPath, relativeClausePackPath));
        if (!resolvedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(resolvedPath) ? resolvedPath : null;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}
