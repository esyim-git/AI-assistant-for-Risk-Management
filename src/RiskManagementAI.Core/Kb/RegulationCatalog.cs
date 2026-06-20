using RiskManagementAI.Core.Data;

namespace RiskManagementAI.Core.Kb;

public sealed record RegulationCatalogEntry(
    string SourceId,
    string Category,
    string Title,
    string SourceOrg,
    string SourceType,
    string Status,
    string Note);

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

    private RegulationCatalog(string sourcePath, IReadOnlyList<RegulationCatalogEntry> entries)
    {
        SourcePath = sourcePath;
        Entries = entries;
    }

    public string SourcePath { get; }

    public IReadOnlyList<RegulationCatalogEntry> Entries { get; }

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

        var entries = new List<RegulationCatalogEntry>();
        foreach (var row in table.Rows)
        {
            if (row.RawFieldCount != table.Columns.Count)
            {
                throw new InvalidDataException($"Line {row.LineNumber}: Regulation catalog column count mismatch. expected={table.Columns.Count}, actual={row.RawFieldCount}");
            }

            entries.Add(new RegulationCatalogEntry(
                row.GetValue("source_id"),
                row.GetValue("category"),
                row.GetValue("title"),
                row.GetValue("source_org"),
                row.GetValue("source_type"),
                row.GetValue("status"),
                row.GetValue("note")));
        }

        return new RegulationCatalog(table.SourcePath, entries);
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
