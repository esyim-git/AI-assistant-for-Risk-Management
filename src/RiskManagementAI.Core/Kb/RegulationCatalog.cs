using System.Globalization;

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

        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidDataException("Regulation catalog header is empty.");
        }

        var headers = SplitCsvLine(headerLine);
        var headerMap = headers
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
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            entries.Add(new RegulationCatalogEntry(
                GetField(fields, headerMap["source_id"]),
                GetField(fields, headerMap["category"]),
                GetField(fields, headerMap["title"]),
                GetField(fields, headerMap["source_org"]),
                GetField(fields, headerMap["source_type"]),
                GetField(fields, headerMap["status"]),
                GetField(fields, headerMap["note"])));
        }

        return new RegulationCatalog(Path.GetFullPath(path), entries);
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

    private static string GetField(IReadOnlyList<string> fields, int index)
    {
        return index < fields.Count ? fields[index].Trim() : string.Empty;
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringWriter(CultureInfo.InvariantCulture);
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Write('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.GetStringBuilder().Clear();
            }
            else
            {
                current.Write(ch);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
