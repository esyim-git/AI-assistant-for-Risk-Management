using System.Text.Json;

namespace RiskManagementAI.Core.Mapping;

public static class ColumnMappingLoader
{
    private const string DefaultMappingPath = "config/column_mapping.json";

    private static readonly JsonDocumentOptions JsonDocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly LogicalColumn[] RequiredColumns =
    [
        LogicalColumn.BaseDate,
        LogicalColumn.PortfolioId,
        LogicalColumn.RiskFactor,
        LogicalColumn.ExposureAmount,
        LogicalColumn.LimitAmount,
        LogicalColumn.UseYn
    ];

    public static ColumnMappingLoadResult LoadDefault()
    {
        return LoadFromFile(DefaultMappingPath);
    }

    public static ColumnMappingLoadResult LoadFromFile(string relativeMappingPath)
    {
        if (!IsSafeRelativeConfigPath(relativeMappingPath))
        {
            throw new ArgumentException("Column mapping path must be a config-relative JSON file.", nameof(relativeMappingPath));
        }

        var resolvedPath = ResolveMappingPath(relativeMappingPath);
        if (resolvedPath is null)
        {
            return CreateFallback($"Column mapping file '{relativeMappingPath}' was not found.");
        }

        try
        {
            var json = File.ReadAllText(resolvedPath);
            return LoadFromJson(json, relativeMappingPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return CreateFallback($"Column mapping file '{relativeMappingPath}' could not be loaded safely: {ex.Message}");
        }
    }

    private static ColumnMappingLoadResult LoadFromJson(string json, string relativeMappingPath)
    {
        using var document = JsonDocument.Parse(json, JsonDocumentOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return CreateFallback($"Column mapping file '{relativeMappingPath}' must contain a JSON object.");
        }

        if (!TryGetObjectProperty(document.RootElement, "Mappings", out var mappingsElement))
        {
            return CreateFallback($"Column mapping file '{relativeMappingPath}' is missing the Mappings object.");
        }

        var mappingValues = new Dictionary<LogicalColumn, string>();
        var seenLogicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in mappingsElement.EnumerateObject())
        {
            if (!seenLogicalNames.Add(property.Name))
            {
                return CreateFallback($"Column mapping file '{relativeMappingPath}' contains a duplicate logical column '{property.Name}'.");
            }

            if (!Enum.TryParse<LogicalColumn>(property.Name, ignoreCase: true, out var logicalColumn))
            {
                return CreateFallback($"Column mapping file '{relativeMappingPath}' contains an unknown logical column '{property.Name}'.");
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                return CreateFallback($"Column mapping '{property.Name}' must be a string physical column name.");
            }

            mappingValues[logicalColumn] = (property.Value.GetString() ?? string.Empty).Trim();
        }

        var validationWarning = ValidateCompleteMapping(mappingValues, relativeMappingPath);
        return validationWarning is not null
            ? CreateFallback(validationWarning)
            : new ColumnMappingLoadResult(new ColumnMapping(mappingValues), UsedFallback: false, Warnings: Array.Empty<string>());
    }

    private static bool TryGetObjectProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return value.ValueKind == JsonValueKind.Object;
            }
        }

        value = default;
        return false;
    }

    private static string? ValidateCompleteMapping(
        IReadOnlyDictionary<LogicalColumn, string> mappingValues,
        string relativeMappingPath)
    {
        foreach (var requiredColumn in RequiredColumns)
        {
            if (!mappingValues.TryGetValue(requiredColumn, out var physicalColumn)
                || string.IsNullOrWhiteSpace(physicalColumn))
            {
                return $"Column mapping file '{relativeMappingPath}' is missing required logical column '{requiredColumn}'.";
            }
        }

        var duplicatePhysicalColumn = mappingValues.Values
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        return duplicatePhysicalColumn is null
            ? null
            : $"Column mapping file '{relativeMappingPath}' maps multiple logical columns to physical column '{duplicatePhysicalColumn.Key}'.";
    }

    private static ColumnMappingLoadResult CreateFallback(string warning)
    {
        return new ColumnMappingLoadResult(ColumnMapping.SafeDefaults(), UsedFallback: true, Warnings: [warning]);
    }

    private static bool IsSafeRelativeConfigPath(string relativeMappingPath)
    {
        if (string.IsNullOrWhiteSpace(relativeMappingPath)
            || Path.IsPathRooted(relativeMappingPath)
            || !string.Equals(Path.GetExtension(relativeMappingPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = relativeMappingPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2
            && string.Equals(segments[0], "config", StringComparison.OrdinalIgnoreCase)
            && segments.All(segment => segment != "." && segment != "..");
    }

    private static string? ResolveMappingPath(string relativeMappingPath)
    {
        var appBaseCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeMappingPath));
        if (File.Exists(appBaseCandidate))
        {
            return appBaseCandidate;
        }

        var currentDirectoryCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativeMappingPath));
        if (File.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        return null;
    }
}
