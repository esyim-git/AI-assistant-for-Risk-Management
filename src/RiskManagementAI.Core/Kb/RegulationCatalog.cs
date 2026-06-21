using RiskManagementAI.Core.Data;

namespace RiskManagementAI.Core.Kb;

public sealed record RegulationCatalogEntry(
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
    string LicenseStatus);

public sealed class RegulationCatalog
{
    private const string DefaultRelativePath = "kb/public_regulation_catalog.csv";
    private static readonly string[] RequiredColumns =
    [
        "source_id",
        "category",
        "title",
        "source_org",
        "source_type",
        "status",
        "note"
    ];
    private static readonly string[] MetadataColumns =
    [
        "source",
        "version",
        "effective_date",
        "repeal_date",
        "file_hash",
        "loaded_date",
        "approval_status",
        "superseded_by",
        "license_status"
    ];
    private static readonly string[] RequiredMetadataValues =
    [
        "source",
        "version",
        "effective_date",
        "file_hash",
        "loaded_date",
        "approval_status",
        "license_status"
    ];

    private RegulationCatalog(
        string sourcePath,
        IReadOnlyList<RegulationCatalogEntry> entries,
        IReadOnlyList<string> warnings)
    {
        SourcePath = sourcePath;
        Entries = entries;
        Warnings = warnings;
    }

    public string SourcePath { get; }

    public IReadOnlyList<RegulationCatalogEntry> Entries { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static RegulationCatalog LoadDefault()
    {
        return LoadFromFile(ResolveDefaultPath());
    }

    public static RegulationCatalog LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Catalog path is empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Regulation catalog file was not found.", path);
        }

        var table = CsvReader.Read(path);
        var headerMap = table.Columns
            .Select((name, index) => new { Name = name.Trim(), Index = index })
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        foreach (var requiredColumn in RequiredColumns)
        {
            if (!headerMap.ContainsKey(requiredColumn))
            {
                throw new InvalidDataException($"Regulation catalog is missing required column: {requiredColumn}");
            }
        }

        var warnings = new List<string>();
        foreach (var metadataColumn in MetadataColumns)
        {
            if (!headerMap.ContainsKey(metadataColumn))
            {
                warnings.Add($"Regulation catalog metadata incomplete: missing optional column '{metadataColumn}'.");
            }
        }

        var entries = new List<RegulationCatalogEntry>();
        foreach (var row in table.Rows)
        {
            if (row.RawFieldCount < RequiredColumns.Length)
            {
                throw new InvalidDataException($"Line {row.LineNumber}: Regulation catalog required column count mismatch. expected-at-least={RequiredColumns.Length}, actual={row.RawFieldCount}");
            }

            if (row.RawFieldCount > table.Columns.Count)
            {
                throw new InvalidDataException($"Line {row.LineNumber}: Regulation catalog column count mismatch. expected={table.Columns.Count}, actual={row.RawFieldCount}");
            }

            if (row.RawFieldCount < table.Columns.Count)
            {
                warnings.Add($"Line {row.LineNumber}: Regulation catalog metadata incomplete: missing trailing fields are treated as empty.");
            }

            var entry = new RegulationCatalogEntry(
                row.GetValue("source_id"),
                row.GetValue("category"),
                row.GetValue("title"),
                row.GetValue("source_org"),
                row.GetValue("source_type"),
                row.GetValue("status"),
                row.GetValue("note"),
                GetOptionalValue(row, headerMap, "source"),
                GetOptionalValue(row, headerMap, "version"),
                GetOptionalValue(row, headerMap, "effective_date"),
                GetOptionalValue(row, headerMap, "repeal_date"),
                GetOptionalValue(row, headerMap, "file_hash"),
                GetOptionalValue(row, headerMap, "loaded_date"),
                GetOptionalValue(row, headerMap, "approval_status"),
                GetOptionalValue(row, headerMap, "superseded_by"),
                GetOptionalValue(row, headerMap, "license_status"));
            ValidateRequiredValues(entry, row.LineNumber);
            entries.Add(entry);
            AddMetadataWarnings(entry, row.LineNumber, warnings);
        }

        return new RegulationCatalog(table.SourcePath, entries, warnings);
    }

    private static string GetOptionalValue(
        CsvRow row,
        IReadOnlyDictionary<string, int> headerMap,
        string columnName)
    {
        return headerMap.ContainsKey(columnName) && row.TryGetValue(columnName, out var value)
            ? value
            : string.Empty;
    }

    private static void ValidateRequiredValues(RegulationCatalogEntry entry, int lineNumber)
    {
        var missingColumn = RequiredColumns.FirstOrDefault(columnName => string.IsNullOrWhiteSpace(GetRequiredValue(entry, columnName)));
        if (missingColumn is not null)
        {
            throw new InvalidDataException($"Line {lineNumber}: Regulation catalog required column '{missingColumn}' is empty.");
        }
    }

    private static string GetRequiredValue(RegulationCatalogEntry entry, string columnName)
    {
        return columnName switch
        {
            "source_id" => entry.SourceId,
            "category" => entry.Category,
            "title" => entry.Title,
            "source_org" => entry.SourceOrg,
            "source_type" => entry.SourceType,
            "status" => entry.Status,
            "note" => entry.Note,
            _ => string.Empty
        };
    }

    private static void AddMetadataWarnings(
        RegulationCatalogEntry entry,
        int lineNumber,
        List<string> warnings)
    {
        foreach (var columnName in RequiredMetadataValues)
        {
            if (string.IsNullOrWhiteSpace(GetMetadataValue(entry, columnName)))
            {
                warnings.Add($"Line {lineNumber}: Regulation catalog metadata incomplete: '{columnName}' is empty for source_id={entry.SourceId}.");
            }
        }

        AddDateWarning(entry.SourceId, "effective_date", entry.EffectiveDate, required: true, lineNumber, warnings);
        AddDateWarning(entry.SourceId, "repeal_date", entry.RepealDate, required: false, lineNumber, warnings);
        AddDateWarning(entry.SourceId, "loaded_date", entry.LoadedDate, required: true, lineNumber, warnings);
    }

    private static string GetMetadataValue(RegulationCatalogEntry entry, string columnName)
    {
        return columnName switch
        {
            "source" => entry.Source,
            "version" => entry.Version,
            "effective_date" => entry.EffectiveDate,
            "file_hash" => entry.FileHash,
            "loaded_date" => entry.LoadedDate,
            "approval_status" => entry.ApprovalStatus,
            "license_status" => entry.LicenseStatus,
            _ => string.Empty
        };
    }

    private static void AddDateWarning(
        string sourceId,
        string columnName,
        string value,
        bool required,
        int lineNumber,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                warnings.Add($"Line {lineNumber}: Regulation catalog metadata incomplete: '{columnName}' is empty for source_id={sourceId}.");
            }

            return;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", out _))
        {
            warnings.Add($"Line {lineNumber}: Regulation catalog metadata warning: '{columnName}' should use YYYY-MM-DD for source_id={sourceId}.");
        }
    }

    private static string ResolveDefaultPath()
    {
        var baseCandidate = Path.Combine(AppContext.BaseDirectory, DefaultRelativePath);
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, DefaultRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return DefaultRelativePath;
    }

}
